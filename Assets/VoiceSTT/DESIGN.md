# VoiceSTT 설계 문서

> 상태: **승인됨, 5단계 구현 완료** (구현 중 보완한 내용은 11번 참고)
> 대상: Unity 6000.0.38f1 / URP 17.0.3 / Windows Standalone

## 0. 한눈에 보기

```
[입력]  KeyboardPushToTalk / ScreenButtonPushToTalk      (IPushToTalkSource)
           │ Pressed / Released
           ▼
[Runtime] VoiceSpellInputSource ──► ISpeechToText (WindowsDictationStt, 나중에 WhisperStt)
           │ SpellInputReceived(SpellInput + SttResult)
           ▼
[Debug]   SttTestSession ──► ISpellMatcher (JamoSimilarityMatcher)  판정
           │            └──► SttLogger ──► ISttLogSink (Csv, Jsonl)  기록
           ▼ AttemptRecorded
          SttTestPanel (UI)  ← 엔진·세션의 이벤트만 구독
```

- **Runtime**: 게임에 실제로 들어가는 부분. 엔진, 판정, 입력, 설정.
- **Debug**: 테스트 화면, 세션 통계, 파일 기록. 게임에서는 개발 빌드에서만 사용.
- 조립(누가 무엇을 만들지)은 `SttBootstrap` 한 곳에서만 한다.

---

## 1. 클래스 목록

### Runtime — `VoxelMagic.Voice` (어셈블리 `VoxelMagic.Voice`)

| 이름 | 종류 | 폴더 | 역할 |
|---|---|---|---|
| `ISpeechToText` | 인터페이스 | `/` | STT 엔진의 공통 계약. 상태·이벤트·`StartListening`·`StopAndTranscribeAsync`·`Cancel` |
| `SttState` | enum | `/` | `Idle`, `Listening`, `Transcribing`, `Completed`, `Failed` |
| `SttResult` | 클래스(불변) | `/` | 한 번 변환한 결과: 원문, 정규화 텍스트, 중간 결과 목록, 시간 측정값, 오류, 엔진·모델 이름 |
| `SpeechToTextEngineBase` | 추상 클래스 | `Engines/` | 상태 전이, 시간 측정, 시간 제한, 이벤트 발생을 담당. 엔진별로 다른 부분만 `protected abstract`로 남김 |
| `WindowsDictationStt` | 클래스 | `Engines/` | 엔진 A. `DictationRecognizer` 래퍼. Windows 전용 코드는 `#if`로 감쌈 |
| `SttEngineType` | enum | `Engines/` | `WindowsDictation`, `Whisper`(자리만 둠) |
| `SttEngineFactory` | 클래스 | `Engines/` | `SttEngineType`과 설정값으로 엔진 인스턴스를 만듦. 엔진을 추가할 때 고치는 유일한 곳 |
| `ITimeSource` / `StopwatchTimeSource` | 인터페이스 / 클래스 | `/` | 현재 시각(ms) 제공. 테스트에서는 가짜 시계로 바꿔 시간 측정을 검증 |
| `MicrophoneRecorder` | 클래스 | `Audio/` | Unity `Microphone`으로 녹음, 입력 음량(RMS), 음성 길이, 샘플 제공. 엔진 B가 사용하고, 엔진 A에서는 레벨 미터와 WAV용으로 시험 사용 |
| `IPushToTalkSource` | 인터페이스 | `Input/` | 누르고 말하기 입력. `Pressed`/`Released` 이벤트, `IsHeld` |
| `KeyboardPushToTalk` | MonoBehaviour | `Input/` | Input System 키보드로 감지. 키는 `SttSettings`에서 지정 |
| `ScreenButtonPushToTalk` | MonoBehaviour | `Input/` | 화면 버튼의 누름/뗌(`IPointerDownHandler`/`IPointerUpHandler`) |
| `ISpellMatcher` | 인터페이스 | `Matching/` | `Match(인식 텍스트, 목표 주문) → SpellMatchResult` |
| `SpellMatchResult` | struct | `Matching/` | 유사도(0~1), 통과 여부, 사용한 기준값 |
| `JamoSimilarityMatcher` | 클래스 | `Matching/` | 자모 레벤슈타인 유사도 판정. 기준값을 갖고 `SetThreshold()`로 변경 |
| `HangulJamo` | static 클래스 | `Matching/` | 한글 음절을 초성·중성·종성 자모로 분해 (0xAC00 기준) |
| `TextNormalizer` | static 클래스 | `Matching/` | 공백·문장부호 제거, 영문 소문자화 |
| `Levenshtein` | static 클래스 | `Matching/` | 배열 간 편집 거리 |
| `ISpellInputSource` | 인터페이스 | `Spells/` | 주문 입력 출처 추상화 (음성/HUD/마법진). `SpellInputReceived` 이벤트, `IsAvailable` |
| `SpellInput` | 클래스(불변) | `Spells/` | 입력된 주문 텍스트, `SpellInputKind`, 음성일 때 `SttResult` |
| `SpellInputKind` | enum | `Spells/` | `Voice`, `Hud`, `MagicCircle` |
| `VoiceSpellInputSource` | 클래스 | `Spells/` | 음성용 `ISpellInputSource` 구현. 누르고 말하기 입력과 엔진을 연결하고, 엔진 교체를 처리 |
| `SttSettings` | ScriptableObject | `/` | 모든 설정값 (아래 표) |

### Debug — `VoxelMagic.Voice.Debug` (Runtime 참조)

| 이름 | 종류 | 역할 |
|---|---|---|
| `SttBootstrap` | MonoBehaviour | 의존성 조립. 설정 에셋을 받아 엔진, 입력, 판정, 세션, 기록기, 패널을 만들고 연결함. `Update`에서 `Tick()`을 호출하고 `OnDestroy`에서 정리 |
| `SttTestSession` | 클래스 | 테스트 세션의 핵심 로직. 음성 입력 결과 → 판정 → 기록 데이터 생성 → 기록기 → `AttemptRecorded` 이벤트. 목표 주문 선택 상태도 가짐 |
| `SttSessionStats` | 클래스 | 시도 수, 평균 변환 시간, 평균 유사도, 성공률 계산 |
| `SttAttemptRecord` | 클래스(직렬화용) | 기록 한 줄. 스펙 6번 항목 전체 + `engineStartMs`, `wavFile` |
| `ISttLogSink` | 인터페이스 | 기록 저장 방식. `Open(sessionId)`, `Write(record)`, `Dispose()` |
| `CsvLogSink` | 클래스 | UTF-8 BOM CSV, 세션마다 파일 1개 |
| `JsonlLogSink` | 클래스 | JSONL, 한 줄에 시도 하나 |
| `SttLogger` | 클래스 | 여러 sink에 나눠 쓰기, 로그 폴더 경로 관리, WAV 저장 위임. 설정이 꺼져 있으면 아무것도 하지 않음 |
| `WavFileWriter` | static 클래스 | float 샘플 → 16bit PCM WAV |
| `SttTestPanel` | MonoBehaviour | 테스트 UI(뷰). 세션·엔진 이벤트를 구독해 화면을 갱신하고, 버튼·드롭다운 조작을 세션 메서드로 전달 |
| `SttPanelLayout` | static 클래스 | uGUI 요소(라벨, 버튼, 드롭다운, 슬라이더, 표)를 코드로 만드는 도우미 |
| `KoreanFont` | static 클래스 | `Font.CreateDynamicFontFromOSFont("Malgun Gothic")`을 한 번 만들어 공유. 없으면 대체 폰트 목록으로 시도 |

### Editor — `VoxelMagic.Voice.Editor` (Editor 전용, Runtime 참조)

| 이름 | 종류 | 역할 |
|---|---|---|
| `SimilarityCheckWindow` | EditorWindow | `Tools/VoiceSTT/Similarity Check`: 두 문자열의 자모 분해 결과와 유사도를 바로 확인 |
| `VoiceSttPackageExporter` | static 클래스 | `Tools/VoiceSTT/Export Package` (11단계에서 구현) |

### Tests — `VoxelMagic.Voice.Tests` (EditMode, Runtime·Debug 참조)

| 이름 | 역할 |
|---|---|
| `HangulJamoTests` | 분해 결과 검증 (예: `꽃` → ㄲ ㅗ ㅊ) |
| `JamoSimilarityMatcherTests` | `불꼬차`/`불꽃아`는 0.75 이상, 무관한 단어는 낮게, 같은 문자열은 1.0 |
| `TextNormalizerTests` | 공백·문장부호 제거 |
| `SpeechToTextEngineBaseTests` | 가짜 엔진과 가짜 시계로 상태 전이, 시간 측정, 시간 제한, 실패 처리를 검증 |
| `FakeSpeechToText` | 테스트 전용 엔진 (`SpeechToTextEngineBase` 상속) |
| `CsvLogSinkTests` | 쉼표·따옴표·줄바꿈 이스케이프, BOM 포함 여부 |

> 클래스 수를 줄이려고 `Levenshtein`과 `TextNormalizer`를 matcher에 합치는 것도 가능하다. 다만 이 둘은 HUD 입력 검사나 에디터 창에서도 따로 쓰이므로 분리해 두었다.

---

## 2. 상속·구현 구조도

```
ISpeechToText  «interface»
└─ SpeechToTextEngineBase  «abstract»      상태 전이·시간 측정·시간 제한·이벤트 (템플릿 메서드)
   ├─ WindowsDictationStt                  엔진 A: DictationRecognizer
   ├─ WhisperStt                           엔진 B (나중에, 승인 후)
   └─ FakeSpeechToText      [Tests]        단위 테스트용

ISpellMatcher  «interface»
└─ JamoSimilarityMatcher                   자모 레벤슈타인 유사도

ISpellInputSource  «interface»
├─ VoiceSpellInputSource                   이번에 구현
├─ (HudSpellInputSource)                   자리만 둠, 게임 쪽에서 구현
└─ (MagicCircleSpellInputSource)           자리만 둠

IPushToTalkSource  «interface»
├─ KeyboardPushToTalk   «MonoBehaviour»
└─ ScreenButtonPushToTalk «MonoBehaviour»

ISttLogSink  «interface»   [Debug]
├─ CsvLogSink
└─ JsonlLogSink

ITimeSource  «interface»
└─ StopwatchTimeSource      (테스트에서는 FakeTimeSource)
```

상속 깊이는 최대 2단계(인터페이스 → 추상 기반 → 구체 엔진)다. 녹음 기능(`MicrophoneRecorder`)은 상속하지 않고 필요한 엔진이 **가지고 쓰는(조합)** 방식이다.

### `SpeechToTextEngineBase` 내부 흐름 (템플릿 메서드)

```
public  StartListening(device)          ← 공개 진입점 (base가 처리)
        ├ 상태 검사 (Idle/Completed/Failed일 때만)
        ├ 시간 기록 시작, 중간 결과 목록 초기화
        ├ SetState(Listening) → StateChanged 이벤트
        └ OnStartListening(device)      ← protected abstract (엔진마다 다름)

public  StopAndTranscribeAsync()        ← 공개 진입점
        ├ 뗀 시각 기록, SetState(Transcribing)
        ├ await OnStopAndTranscribeAsync(token)   ← protected abstract
        │   (설정의 시간 제한을 넘기면 base가 취소 후 Failed 처리)
        ├ 정규화, SttResult 생성 (모든 시간값 계산)
        ├ SetState(Completed 또는 Failed)
        └ Completed 이벤트 + 결과 반환

protected ReportHypothesis(text)        엔진이 중간 결과를 알릴 때 호출 → HypothesisReceived 이벤트
protected abstract bool CheckAvailability(out string reason)
protected virtual  void OnCancel(), OnTick(), OnDispose()
public    void Tick()                   최대 녹음 시간 초과 시 자동으로 Stop 요청
```

공개 읽기 전용 값: `State`, `IsAvailable`, `UnavailableReason`, `EngineName`, `ModelName`, `ListeningElapsedMs`, `TranscribingElapsedMs`, `InputLevel`(지원 안 하면 -1)

---

## 3. 의존 관계도

`A ──► B`는 "A가 B를 알고 있다(참조)", `A ··► B`는 "A가 B의 이벤트를 구독한다"는 뜻이다. 생성 관계는 `[생성]`으로 표시했다.

```
SttBootstrap (Debug, 조립 담당) ──[생성]──► SttEngineFactory, VoiceSpellInputSource,
                                          JamoSimilarityMatcher, SttLogger(+Csv/Jsonl sink),
                                          SttTestSession
             ──[참조 연결]──► SttSettings(에셋), KeyboardPushToTalk, ScreenButtonPushToTalk, SttTestPanel

VoiceSpellInputSource ──► ISpeechToText, IPushToTalkSource
                      ··► IPushToTalkSource.Pressed/Released
                      ··► ISpeechToText.Completed

SttEngineFactory ──[생성]──► WindowsDictationStt (──► MicrophoneRecorder)
SpeechToTextEngineBase ──► ITimeSource, TextNormalizer

SttTestSession ──► ISpellInputSource(VoiceSpellInputSource), ISpellMatcher, SttLogger, SttSessionStats
               ··► ISpellInputSource.SpellInputReceived

SttLogger ──► ISttLogSink[], WavFileWriter

SttTestPanel ··► SttTestSession.AttemptRecorded / StatsChanged
             ··► ISpeechToText.StateChanged / HypothesisReceived  (현재 엔진; 엔진이 바뀌면 다시 구독)
             ──► SttTestSession 공개 메서드 (목표 주문 선택, 기준값 변경, 엔진 전환, 목록 지우기)
             ──► KoreanFont, SttPanelLayout

JamoSimilarityMatcher ──► HangulJamo, TextNormalizer, Levenshtein
```

- 어셈블리 방향: `Debug → Runtime`, `Editor → Runtime`, `Tests → Runtime, Debug`. **Runtime은 아무것도 참조하지 않는다** (Unity 기본 모듈과 Input System, uGUI만 참조).
- 싱글턴과 `FindObjectOfType`은 쓰지 않는다. 연결은 `SttBootstrap`의 `[SerializeField]`와 생성자 인자로만 한다.

---

## 4. 설정값 (`SttSettings`)

| 그룹 | 필드 | 기본값 |
|---|---|---|
| 엔진 | `engineType` | `WindowsDictation` |
| | `whisperModel` | `ggml-tiny` (엔진 B용, 지금은 사용 안 함) |
| | `language` | `ko` |
| 입력 | `pushToTalkKey` (Input System `Key`) | `Space` |
| | `preferredMicDevice` | 빈 값 = 기본 장치 |
| 시간 | `maxRecordSeconds` | 10 |
| | `transcribeTimeoutSeconds` | 8 |
| | `dictationFinalResultWaitMs` (엔진 A: 키를 뗀 뒤 최종 결과를 기다리는 시간) | 1500 |
| 판정 | `similarityThreshold` | 0.75 |
| | `targetPhrases` | 빛나라, 찾아라, 느려져라, 불꽃아, 자라라, 숨어라 |
| 기록 | `loggingEnabled` | true (출시 빌드에서는 false) |
| | `writeCsv` / `writeJsonl` | true / true |
| | `saveWav` | true (녹음 샘플이 있는 엔진만 해당) |
| | `logFolderName` | `STTLogs` |

실행 중에 UI에서 바꾼 값(기준값, 목표 주문 등)은 **에셋에 다시 쓰지 않고** 런타임 객체에만 반영한다. 플레이 모드에서 에셋이 바뀌는 사고를 막기 위해서다.

---

## 5. 씬 구성과 핵심 컴포넌트

### `Scenes/STTTest.unity`

```
STTTest
├─ Main Camera            Camera  (UI만 보이게 배경색만 지정)
├─ Directional Light
├─ EventSystem            EventSystem + InputSystemUIInputModule   UI 클릭·드래그 입력 처리
└─ SttTestPanel  (프리팹 인스턴스)
   ├─ [SttBootstrap]      SttSettings 에셋 참조. 모든 객체를 만들고 연결. Update에서 Tick
   ├─ [KeyboardPushToTalk] 스페이스 바 누름/뗌 → IPushToTalkSource 이벤트
   └─ Canvas              Canvas(Screen Space Overlay) + CanvasScaler(1920×1080 기준) + GraphicRaycaster
      ├─ [SttTestPanel]   UI 뷰. Awake에서 하위 UI를 코드로 생성하고 이벤트 구독
      └─ PushToTalkButton  Button 이미지 + [ScreenButtonPushToTalk]   누르는 동안 녹음
```

### `Debug/Prefabs/SttTestPanel.prefab`
- 위 `SttTestPanel` 오브젝트 전체가 프리팹이다. 게임 씬에 끌어다 놓으면 `SttBootstrap`이 알아서 조립한다.
- EventSystem은 씬마다 하나여야 하므로 프리팹에 넣지 않는다. 대신 `SttBootstrap`이 시작할 때 `EventSystem.current`가 없으면 경고를 띄운다.

### 화면 배치 (`SttTestPanel`이 코드로 생성)

```
┌──────────────────────────────── 상단 바 ────────────────────────────────┐
│ 엔진 [드롭다운]  마이크 [드롭다운]  [로그 폴더 열기] [목록 지우기]          │
├───────────── 왼쪽: 현재 시도 ─────────────┬──────── 오른쪽: 기록 ────────┤
│ ● 상태: 녹음 중 (색)   경과 1234 ms        │ 세션 요약: 시도 / 평균 ms /   │
│ 음량 ▮▮▮▮▯▯▯                              │          평균 유사도 / 성공률 │
│ 중간 결과: "불꽃..."                       │ ┌ 기록 표 (최신순) ─────────┐ │
│ 최종: 원문 / 정규화                        │ │ # 목표 결과 유사도 ms ✔/✘ │ │
│ 시간: 녹음 / 변환 / 전체 / 실시간 배율       │ │ ...                       │ │
│ 목표 주문 [드롭다운] [자유 입력]             │ └───────────────────────────┘ │
│ 기준값 [슬라이더 0.75]  유사도 0.88 ✔ 성공   │                               │
│ [  누르고 말하기 버튼 (Space)  ]            │                               │
└────────────────────────────────────────────┴───────────────────────────────┘
```

상태 색: 대기(회색) → 녹음 중(빨강) → 변환 중(노랑) → 완료(초록) / 실패(진한 빨강).
변환 중 경과 시간은 패널의 `Update`에서 엔진의 `TranscribingElapsedMs`를 읽어 매 프레임 표시한다. 매 프레임 이벤트를 쏘지 않고 읽기 전용 값만 확인한다.

---

## 6. 동작 흐름 (스페이스 바 → 화면·기록)

1. **입력**: `KeyboardPushToTalk.Update`가 `Keyboard.current[pushToTalkKey].wasPressedThisFrame`을 감지해 `Pressed` 이벤트를 발생시킨다.
2. **녹음 시작**: `VoiceSpellInputSource`가 `engine.StartListening(device)`를 호출한다.
3. **상태 변경**: base가 시작 시각을 기록하고 `Idle → Listening`으로 바꾼 뒤 `StateChanged` 이벤트를 발생시킨다. 패널은 "녹음 중"을 빨간색으로 표시한다.
4. **엔진 A 시작**: `WindowsDictationStt`가 `PhraseRecognitionSystem`을 정리하고 `DictationRecognizer.Start()`를 호출한다. 실제로 시작된 시각과의 차이는 `engineStartMs`로 기록한다. 가능하면 `MicrophoneRecorder`도 함께 켜서 음량을 표시한다.
5. **중간 결과**: `DictationHypothesis` → `ReportHypothesis()` → `HypothesisReceived` 순으로 전달되고, 패널이 인식 중인 문장을 실시간으로 표시한다.
6. **키를 뗌**: `Released` 이벤트를 받은 `VoiceSpellInputSource`가 `await engine.StopAndTranscribeAsync()`를 호출한다.
7. **변환 중**: base가 뗀 시각을 기록하고 `Transcribing`으로 바꾼다. 패널이 경과 ms를 올리기 시작한다.
8. **최종 결과 수집 (엔진 A)**: `DictationResult`를 최대 `dictationFinalResultWaitMs`까지 기다린 뒤 `Stop()`을 호출한다. 그 안에 최종 결과가 오지 않으면 **마지막 중간 결과를 대신 쓰고** `usedHypothesisFallback=true`로 표시한다.
9. **결과 생성**: base가 텍스트를 정규화하고, 시간값(녹음 ms, 변환 ms, 전체 지연, 음성 길이, 실시간 배율)을 계산해 `SttResult`를 만든다. 상태는 `Completed`(빈 결과·오류·시간 초과면 `Failed`)가 되고 `Completed` 이벤트가 발생한다.
10. **주문 입력 이벤트**: `VoiceSpellInputSource`가 `SpellInputReceived(SpellInput{ Kind=Voice, Text, SttResult })`를 발생시킨다. 게임에서도 이 이벤트만 받으면 된다.
11. **판정**: `SttTestSession`이 `matcher.Match(normalizedText, 목표 주문)`을 호출해 `SpellMatchResult`(유사도, 통과 여부)를 받는다.
12. **기록**: `SttAttemptRecord`를 만들어 `SttLogger.Write()`로 CSV와 JSONL에 한 줄씩 추가한다. 녹음 샘플이 있으면 WAV도 저장한다. 그다음 `SttSessionStats`를 갱신한다.
13. **UI 갱신**: `AttemptRecorded`와 `StatsChanged` 이벤트를 받은 패널이 최종 결과, 시간, 유사도, 성공 여부, 표, 요약을 갱신한다.

**실패하는 경우**: 마이크가 없거나 OS가 지원하지 않으면 `IsAvailable=false`가 되고, 패널은 "음성 인식 사용 불가 + 이유"를 표시한다. 이때 `VoiceSpellInputSource.IsAvailable`도 false가 되므로 게임은 HUD 모드로 바꾸면 된다. 변환 중 오류는 `Failed` 상태로 바뀌고, `error` 칸이 채워진 채 그대로 기록된다.

### 시간 정의

| 값 | 정의 |
|---|---|
| `recordMs` | 누른 순간 → 뗀 순간 |
| `transcribeMs` | 뗀 순간 → 최종 결과 |
| `totalLatencyMs` | 누른 순간 → 최종 결과 (= record + transcribe) |
| `audioSec` | 녹음 샘플이 있으면 샘플 길이, 없으면 `recordMs / 1000` |
| `realtimeFactor` | `transcribeMs / (audioSec × 1000)` |
| `engineStartMs` (추가) | `StartListening` 호출 → 엔진이 실제로 듣기 시작한 시점. 엔진 A는 시작 지연이 있을 수 있어서 따로 측정 |

---

## 7. 확장 지점

| 하고 싶은 것 | 새로 만들 것 | 고칠 곳 |
|---|---|---|
| **엔진 B (Whisper) 추가** | `Engines/WhisperStt.cs` (`SpeechToTextEngineBase` 상속, `MicrophoneRecorder` 사용) | `SttEngineType`에 값 추가, `SttEngineFactory`에 분기 하나. UI·기록·판정은 그대로 |
| **클라우드 STT 시험** | `Engines/CloudXxxStt.cs` + 키를 읽는 `IApiKeyProvider` (환경 변수 / `UserSettings/`) | 위와 같음 |
| **HUD 주문 입력** | 게임 쪽 `HudSpellInputSource : ISpellInputSource` | 없음. 같은 `SpellInputReceived` → `ISpellMatcher` 흐름에 연결 |
| **마법진 입력** | `MagicCircleSpellInputSource : ISpellInputSource` | 없음 |
| **게임 주문 시스템 연결** | 게임 쪽 `SpellCaster`가 `ISpellInputSource` 목록을 구독하고 `ISpellMatcher`로 6개 주문 중 가장 비슷한 것을 고름 | Debug 어셈블리는 필요 없음 |
| **판정 방식 변경** (예: 문장 안에서 주문 찾기, 첫소리 ㅇ 무시) | `ISpellMatcher` 새 구현 | `SttBootstrap`에서 생성하는 한 줄 |
| **기록 방식 추가** (예: 원격 전송) | `ISttLogSink` 새 구현 | `SttBootstrap`에서 sink 목록에 추가 |
| **입력 방식 추가** (게임패드 버튼 등) | `IPushToTalkSource` 새 구현 | `SttBootstrap` 연결 |
| **음성/HUD 모드 선택 화면** | 게임 쪽 UI | `VoiceSpellInputSource.IsAvailable`과 `AvailabilityChanged` 확인 |

---

## 8. 설계에서 고민한 선택지

1. **엔진을 MonoBehaviour로 만들까, 일반 C# 클래스로 만들까** → **일반 클래스**로 정했다. 단위 테스트가 가능하고, 씬 없이도 게임 로직에서 쓸 수 있다. Unity 업데이트 루프가 필요한 부분(시간 제한, 음량)은 `Tick()`으로 노출하고, `SttBootstrap`(게임에서는 게임 쪽 호스트)이 호출한다.
2. **비동기 처리: 코루틴, 콜백, `async Task`** → **`async Task`**로 정했다. 스펙의 `StopAndTranscribeAsync` 형태 그대로이고, Unity의 동기화 컨텍스트 덕분에 이어지는 코드가 메인 스레드에서 실행된다. 결과는 반환값과 `Completed` 이벤트 두 가지로 받을 수 있다.
3. **입력 처리: 구 Input Manager와 새 Input System** → 프로젝트가 이미 둘 다(Both) 켜져 있으므로 **설정 변경 없이 새 Input System**을 쓴다. Unity 6 기본값이고 게임 본편에도 맞다. 그 대신 Runtime 어셈블리가 `Unity.InputSystem`을 참조하므로 게임 프로젝트에도 이 패키지가 필요하다(기본 포함).
4. **UI 기술: uGUI + 레거시 Text, TMP, UI Toolkit** → **uGUI + 레거시 `Text` + OS 폰트(맑은 고딕)**로 정했다. 다운로드도, 에셋 생성도, `Assets/TextMesh Pro` 가져오기(우리 폴더 밖 파일 생성)도 필요 없다. 단점은 맑은 고딕이 없는 OS에서 한글이 깨진다는 점이고, 이 내용은 SETUP.md에 적는다. 게임 본편 UI는 TMP 폰트 에셋으로 따로 만드는 편이 좋다.
5. **UI 배치를 프리팹에서 손으로 할까, 코드로 생성할까** → **코드로 생성**하고 프리팹은 그 컴포넌트를 담는 틀로 쓴다. 폰트를 한 곳에서 넣을 수 있고, 직렬화된 참조가 깨질 일이 없으며, Claude Code로 수정하기 쉽다. 디버그 화면이라 에디터에서 눈으로 다듬을 필요가 적다는 점도 고려했다.
6. **판정: 문장 전체 비교와 문장 안에서 찾기** → 스펙대로 **문장 전체 비교**로 정했다. "어… 불꽃아"처럼 군말이 섞이는 경우는 기록을 보고, 필요하면 `ISpellMatcher` 구현만 추가한다.
7. **기록 인터페이스 위치: Runtime과 Debug** → **Debug**에 둔다. 출시 빌드에서는 기록을 끄기 때문이다. 게임에서도 기록이 필요해지면 Runtime으로 옮기면 된다.
8. **엔진 A의 시작 방식: 미리 켜 두기와 누를 때 시작** → **누를 때 시작**하고 시작 지연(`engineStartMs`)을 따로 잰다. 실제 사용감을 정직하게 측정하기 위해서다. 지연이 크면 "미리 켜 두기" 옵션을 추가한다.

---

## 9. 알려진 위험과 확인할 점 (구현 단계에서 검증)

- **엔진 A의 한국어 인식**: `DictationRecognizer`는 언어를 코드로 지정할 수 없고 Windows **음성 언어**를 따른다. 현재 이 PC는 표시 언어가 en-US이고 한국어 음성 인식기가 없으며, 온라인 음성 인식도 꺼져 있을 가능성이 높다. 7단계 전에 사용자가 Windows 설정에서 바꿔야 할 수 있다.
- **엔진 A의 마이크 선택**: `DictationRecognizer`는 Windows 기본 마이크만 쓴다. 장치 드롭다운은 엔진 B와 음량 미터에만 적용되며, 엔진 A일 때는 UI에 그렇게 안내한다.
- **엔진 A와 `Microphone`을 동시에 쓰는 문제**: 두 쪽이 마이크를 함께 열 수 있는지 구현할 때 확인한다. 충돌하면 엔진 A에서는 음량 미터와 WAV 저장을 끈다(스펙상 생략 가능).
- **키를 뗀 뒤 최종 결과**: Dictation은 말이 멈춰야 최종 결과를 내므로, 키를 떼도 결과가 늦거나 오지 않을 수 있다. 그래서 대기 시간과 중간 결과 대체(8번 흐름)를 둔다.
- **네임스페이스 충돌**: `VoxelMagic.Voice.Debug` 네임스페이스 안에서는 `Debug.Log`가 `UnityEngine.Debug`가 아니라 이 네임스페이스로 해석된다. Debug 어셈블리에서는 `UnityEngine.Debug.Log`를 명시해서 쓴다.

---

## 10. 어셈블리 정의

| asmdef | 참조 | 플랫폼 | 비고 |
|---|---|---|---|
| `VoxelMagic.Voice` | `Unity.InputSystem`, `UnityEngine.UI` | 전체 | `ScreenButtonPushToTalk`가 EventSystems 인터페이스를 쓰기 때문에 uGUI 참조 |
| `VoxelMagic.Voice.Debug` | `VoxelMagic.Voice`, `UnityEngine.UI`, `Unity.InputSystem` | 전체 | 게임에서는 개발 빌드에서만 포함하는 것을 권장 (`#if DEVELOPMENT_BUILD \|\| UNITY_EDITOR`로 부트스트랩 비활성화 또는 Define Constraints) |
| `VoxelMagic.Voice.Editor` | `VoxelMagic.Voice` | Editor | |
| `VoxelMagic.Voice.Tests` | `VoxelMagic.Voice`, `VoxelMagic.Voice.Debug`, NUnit | Editor | Test Assemblies 옵션 |

---

## 11. 구현 중 보완한 점 (5단계)

구조(인터페이스, 상속, 의존 방향)는 바꾸지 않았고, 아래 세부 사항만 추가하거나 다듬었다.

| 항목 | 설계 | 구현 | 이유 |
|---|---|---|---|
| 엔진 → 기반 클래스 반환값 | `OnStopAndTranscribeAsync(token)` → 텍스트 | `OnStopAndTranscribeAsync()` → `EngineTranscription`(텍스트, 오류, 중간 결과 대체 여부, 녹음 샘플) | 시간 계산과 정규화는 기반 클래스가 맡고, 엔진은 결과만 채우도록 역할을 나눔 |
| 시간 제한 방식 | 기반 클래스가 시간 초과 시 취소 | `Task.Delay`를 쓰지 않고 **`Tick()`에서 가짜 시계로도 확인 가능한 방식**으로 검사. 취소는 `OnCancel()`로 알림 | 단위 테스트에서 실제로 기다리지 않고 시간 초과를 검증하기 위해 |
| 녹음 샘플 | `MicrophoneRecorder`가 샘플 제공 | 불변 클래스 `AudioSamples` 추가 | `SttResult`와 WAV 저장에서 같은 형식을 쓰기 위해 |
| 음성 입력 이벤트 | `SpellInputReceived` 하나 | `SpellInputReceived`(성공만) + `TranscriptionCompleted`(성공·실패 모두) + `EngineChanged` | 게임은 성공한 주문만 받고, 기록은 실패까지 남기기 위해 |
| 엔진 전환 | `SttTestSession`의 엔진 전환 메서드 | `TrySwitchEngine(type, out message)`. 미구현이거나 녹음 중이면 실패 이유를 돌려줌 | UI가 실패 이유를 그대로 표시하기 위해 |
| 설정 | - | `SttSettings.dictationUseUnityMicrophone` 추가 | 받아쓰기와 Unity Microphone이 충돌할 때 끌 수 있게 하기 위해 (9번 위험 항목) |
| 키 입력 차단 | - | `KeyboardPushToTalk.SetBlocker(Func<bool>)` | 자유 입력칸에 스페이스를 칠 때 녹음이 시작되지 않게 하기 위해 |
| UI 도우미 | `SttPanelLayout`, `KoreanFont` | `DefaultControls`(uGUI 기본 컨트롤 생성기)로 드롭다운·슬라이더·스크롤 목록을 만든 뒤 한글 폰트를 넣음 | 스프라이트 에셋 없이 코드만으로 컨트롤을 만들기 위해 |
| 테스트 | `FakeSpeechToText` | `FakeTimeSource` 추가 (`Tests/Fakes.cs`) | 시간 측정을 정확한 값으로 검증하기 위해 |

---

## 12. 엔진 B (Whisper) 설계 — 9단계, 2026-10-29

사용자 승인: whisper.unity 1.4.0 + 모델 `ggml-tiny`, `ggml-base`.

### 추가·변경할 클래스

| 이름 | 종류 | 어셈블리 | 역할 |
|---|---|---|---|
| `WhisperStt` | 클래스 (`SpeechToTextEngineBase` 상속) | Runtime | 엔진 B. `MicrophoneRecorder`로 녹음한 뒤 키를 떼면 whisper.unity의 `WhisperWrapper.GetTextAsync`로 한 번에 변환. 언어 `ko` 고정. 모델은 엔진을 만들 때 비동기로 불러오고, 다 불러올 때까지 `IsAvailable=false`("모델 불러오는 중") |
| `SttEngineOption` | struct | Runtime | 엔진 드롭다운의 항목 하나 = (엔진 종류, 모델 이름, 표시 이름). "Whisper tiny"와 "Whisper base"를 같은 조건에서 바로 바꿔 가며 비교하기 위해 추가 |
| `AudioLevel` | static 클래스 | Runtime | RMS → 0~1 음량 변환, 녹음 전체의 최대 음량 계산. `MicrophoneRecorder`와 무음 판정이 같은 기준을 쓰도록 분리 |
| `SttEngineFactory` | 변경 | Runtime | `Options` 목록 제공, `Create(SttEngineOption)`. 엔진을 추가할 때 고치는 곳은 여전히 이곳뿐 |
| `SttTestSession` | 변경 | Debug | `TrySwitchEngine(SttEngineOption, out message)`, `CurrentEngineOption` |
| `SttTestPanel` | 변경 | Debug | 엔진 드롭다운을 `factory.Options`로 채움 |
| `SttSettings` | 변경 | Runtime | `whisperModels`(드롭다운에 보일 모델 목록), `whisperModelFolder`(`VoiceSTT`), `whisperUseGpu`(기본 끔), `whisperUseInitialPrompt`(기본 끔), `minSpeechLevel`(무음 판정 기준, 기본 0.25 ≈ -45 dB), `minAudioSeconds`(기본 0.3) |

상속 구조는 그대로다: `ISpeechToText` → `SpeechToTextEngineBase` → `WindowsDictationStt` / `WhisperStt` (깊이 2).

### 패키지가 없어도 컴파일되게

- Runtime asmdef에 `com.whisper.unity`를 참조로 추가하고, `versionDefines`로 패키지가 있을 때만 `VOICESTT_WHISPER`를 정의한다.
- `WhisperStt`는 `#if VOICESTT_WHISPER`로 감싼다. 패키지가 없으면 드롭다운에 "Whisper (패키지 없음)"으로 표시되고 선택할 수 없다.

### 동작 흐름 (엔진 A와 다른 점만)

1. 누름 → `MicrophoneRecorder.Start(선택한 장치)`. 녹음이 실제로 시작되면 `engineStartMs`를 측정한다. 음량 미터가 동작하고, 장치 드롭다운이 적용된다.
2. 뗌 → 녹음 샘플을 꺼낸다.
   - 음성 길이가 `minAudioSeconds`보다 짧거나, 최대 음량이 `minSpeechLevel`보다 작으면 변환하지 않고 "음성이 감지되지 않음"으로 실패 처리한다. Whisper는 무음에서 "시청해 주셔서 감사합니다" 같은 문장을 만들어 내는 경향이 있어서 이를 막기 위해서다.
   - 통과하면 `GetTextAsync`(백그라운드 스레드)로 변환한다. 이어지는 처리는 메인 스레드에서 실행된다.
3. 중간 결과는 없다. 녹음이 끝난 뒤 한 번에 변환하는 방식이기 때문이다.
4. 녹음 샘플은 `SttResult.Audio`로 넘어가 WAV로 저장된다.

### 선택지

- **엔진 선택 단위**: 종류(enum)만 두고 모델은 설정에서 바꾸는 방법과, (종류 + 모델)을 드롭다운 항목으로 두는 방법이 있었다. tiny와 base를 플레이 중에 바로 바꿔 비교해야 하므로 후자를 골랐다.
- **WhisperManager(MonoBehaviour)와 WhisperWrapper(일반 클래스)**: 우리 설계(엔진은 일반 C# 클래스)에 맞는 `WhisperWrapper`를 쓴다.
- **초기 프롬프트**: 주문 목록을 프롬프트로 주면 인식이 그쪽으로 기운다. 원래 성능을 재기 위해 기본은 끄고, 설정에서 켜서 비교할 수 있게 한다.

---

## 13. 녹음 지연 개선과 초기 프롬프트 — 2026-10-29

첫 마이크 테스트(CHANGELOG 참고)에서 **키를 누른 뒤 실제 소리가 들어오기까지 0.4~1.4초**가 걸려 첫 음절이 잘렸다. 같은 녹음으로 다시 시험했을 때 초기 프롬프트가 효과가 컸다. 사용자 승인을 받아 아래와 같이 바꾼다.

### 변경 내용

| 대상 | 변경 |
|---|---|
| `MicrophoneRecorder` | **상시 녹음(링 버퍼)** 방식 추가. `Open(device)`로 반복(loop) 녹음을 켜 두고, `BeginCapture(preRoll)`가 **누르기 직전 preRoll초**부터 잘라 쓰기 시작하며, `EndCapture()`가 그 구간의 샘플을 돌려준다. 실제 소리(0이 아닌 샘플)가 들어오기 시작하면 `IsLive=true`. 기존 `Start/Stop`은 Open+BeginCapture / EndCapture+Close로 유지한다 (엔진 A가 사용) |
| `WhisperStt` | 엔진을 만들거나 장치가 바뀌면 마이크를 미리 연다(`keepMicrophoneOpen`). 누르면 즉시 캡처를 시작하고, 떼면 **postRoll초 더 기다린 뒤** 캡처를 끝내고 변환한다. 기다리는 시간은 `Tick()`에서 검사한다(기존 방식과 같음) |
| `ISpeechToText` | `SetPreferredDevice(device)` 추가. 기반 클래스에서는 아무 일도 하지 않는 기본 구현이고(`OnPreferredDeviceChanged`), `WhisperStt`만 이 시점에 마이크를 연다. `VoiceSpellInputSource`가 엔진을 연결할 때와 장치를 바꿀 때 호출한다 |
| `SttSettings` | `keepMicrophoneOpen`(기본 true), `preRollSeconds`(0.3), `postRollSeconds`(0.3) 추가. `whisperUseInitialPrompt` 기본값을 **true**로 변경 |

### 측정값의 의미 변화
- `engineStartMs`: 마이크가 이미 켜져 있으면 0에 가깝다.
- `transcribeMs`: 뗀 순간부터 결과까지이므로 **postRoll(0.3초)이 포함된다.** 플레이어가 실제로 기다리는 시간이기 때문이다.
- `audioSec`: preRoll과 postRoll을 포함한 실제 녹음 길이.

### 선택지와 이유
- **상시 녹음과 누를 때 시작**: 누를 때 마이크를 켜면 장치가 준비되는 시간(이 PC에서 0.4~1.4초)을 피할 수 없다. 상시 녹음은 지연이 사라지는 대신, 플레이 중 Windows 마이크 사용 표시가 계속 켜져 있다. 게임에서는 **음성 모드를 고른 동안에만** 열어 두면 된다 (`keepMicrophoneOpen`으로 끌 수 있음).
- **엔진 A는 그대로**: 받아쓰기는 Windows가 직접 마이크를 관리하므로 상시 녹음을 쓰지 않는다. 보조 녹음기도 기존처럼 누를 때 켠다.

## 14. GPU/CPU 표시 — 2026-10-30 (승인: "제작 시작")

지금은 GPU 사용 여부가 Unity UI와 기록 파일 어디에도 나오지 않아, 스크린샷만으로는 테스트 조건을 알 수 없다. 사용자 요청으로 아래처럼 표시한다.

### 변경 내용

| 대상 | 변경 |
|---|---|
| `ISpeechToText` | 읽기 전용 `ComputeBackend` 추가 ("GPU", "CPU", 해당 없음이면 ""). 기반 클래스는 "" 기본 구현, `WhisperStt`는 생성 시 받은 `useGpu` 값으로 "GPU"/"CPU"를 돌려준다 |
| `SttResult` | `ComputeBackend` 필드 추가. 기반 클래스의 `Finish`에서 엔진 값을 그대로 담는다 (불변 유지) |
| `SttEngineFactory` | Whisper 항목의 표시 이름에 붙임: `엔진 B: Whisper small-q5_1 · GPU` (설정의 `whisperUseGpu`에서 결정). 엔진 A는 그대로 |
| `SttTestPanel` | 시도 결과의 시간 줄 끝에 `· GPU`/`· CPU` 추가: `음성 길이 1.99 s · 실시간 배율 0.22 · 엔진 시작 0 ms · GPU` |
| `SttAttemptRecord` (CSV/JSONL) | `model` 다음에 `compute` 열 추가 (`GPU`/`CPU`/빈칸). 기존 CSV와 열 순서가 달라지므로 새 파일부터 적용된다 |
| 테스트 | `CsvLogSinkTests`의 헤더·행 기대값 갱신 (`FakeSpeechToText`는 기반 클래스의 빈 값을 그대로 씀) |

### 한계
- 표시는 **설정값(요청)**이다. whisper.cpp는 Vulkan GPU를 찾지 못하면 조용히 CPU로 동작하는데, whisper.unity가 실제 사용 장치를 알려 주지 않는다. 실제 장치는 Console의 `whisper_backend_init_gpu: using Vulkan0 backend` 줄로 확인한다 (CHANGELOG 2026-10-30 참고).

### 선택지와 이유
- **드롭다운 이름에 붙이기**: 스크린샷에 항상 찍히는 곳이라 테스트 조건이 바로 남는다. 엔진을 만들기 전에도 알 수 있다.
- **CSV 열 추가**: 나중에 기록 파일만으로 GPU/CPU 결과를 나눠 비교할 수 있다.
