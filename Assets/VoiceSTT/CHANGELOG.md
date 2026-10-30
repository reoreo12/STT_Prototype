# CHANGELOG

## 2026-10-29 — 1단계: 환경 확인
- MCP for Unity 패키지 `com.coplaydev.unity-mcp` 추가. 처음에는 버전이 고정되지 않은 Git URL로 들어가 main(10.3.1-beta.11)으로 받아졌기 때문에, MCP 서버 버전에 맞춰 `#v10.3.0` 태그로 고정함 (`Packages/manifest.json`, `packages-lock.json`)
- Unity 버전, 렌더 파이프라인, 플랫폼, 마이크, 언어를 확인하고 SETUP.md의 "환경" 항목에 기록함

## 2026-10-29 — 2단계: 설계
- `DESIGN.md` 작성 (클래스 목록, 구조도, 의존 관계, 씬 구성, 동작 흐름, 확장 지점, 선택지, 위험 요소). 사용자 승인("제작 시작")

## 2026-10-29 — 4단계: 뼈대
- 폴더 생성: `Runtime/{Engines,Matching,Input,Audio,Spells}`, `Debug/Prefabs`, `Editor`, `Tests`, `Settings`, `Scenes`
- asmdef 4개 생성: `VoxelMagic.Voice`, `VoxelMagic.Voice.Debug`, `VoxelMagic.Voice.Editor`, `VoxelMagic.Voice.Tests`
- `SttEngineType.cs`, `SttSettings.cs` 작성, 기본값 에셋 `Settings/SttSettings.asset` 생성
- SETUP.md 뼈대 정리 (의존 패키지, 폴더·어셈블리 구조, 입력 처리 방식)
- 프로젝트 설정 변경 없음, 패키지 추가 없음

## 2026-10-29 — 5단계: 엔진 A + 판정 + 테스트 UI + 기록
- Runtime: `ISpeechToText`, `SpeechToTextEngineBase`, `WindowsDictationStt`, `SttEngineFactory`, `SttResult`, `EngineTranscription`, `SttState`, `ITimeSource`, `MicrophoneRecorder`, `AudioSamples`, `HangulJamo`, `TextNormalizer`, `Levenshtein`, `ISpellMatcher`, `JamoSimilarityMatcher`, `IPushToTalkSource`, `KeyboardPushToTalk`, `ScreenButtonPushToTalk`, `ISpellInputSource`, `SpellInput`, `VoiceSpellInputSource`
- Debug: `SttBootstrap`, `SttTestSession`, `SttSessionStats`, `SttAttemptRecord`, `ISttLogSink`, `CsvLogSink`, `JsonlLogSink`, `SttLogger`, `WavFileWriter`, `SttTestPanel`, `SttPanelLayout`, `KoreanFont`
- Editor: `SimilarityCheckWindow` (`Tools/VoiceSTT/Similarity Check`)
- Tests: 자모 분해, 유사도, 정규화, 기반 클래스 상태 전이·시간 측정, CSV 이스케이프·BOM (`Fakes.cs`에 가짜 시계·가짜 엔진)
- `SttSettings`에 `dictationUseUnityMicrophone` 추가 (엔진 A의 음량 미터·WAV용 보조 녹음 켜기/끄기)
- 프리팹 `Debug/Prefabs/SttTestPanel.prefab`, 씬 `Scenes/STTTest.unity` 생성
- 컴파일 에러·경고 0개, 플레이 모드에서 UI 생성과 한글 표시 확인 (음성 테스트는 7단계)
- Unity가 새 씬을 만들면서 `ProjectSettings/SceneTemplateSettings.json`을 자동 생성함 (기본값만 있음, 직접 바꾼 설정 아님)
- 설계 보완 내용은 DESIGN.md 11번에 정리

## 2026-10-29 — 6단계: 점검
- EditMode 단위 테스트 `VoxelMagic.Voice.Tests` **37개 모두 통과** (실패 0, 건너뜀 0, 약 1.6초)
- 콘솔: 프로젝트 코드에서 나온 에러·경고 0개 (MCP 브리지 안내 메시지만 있음)
- `Scenes/STTTest.unity` 저장 상태 확인 (저장 안 된 변경 없음). 구성: Main Camera, Directional Light, EventSystem(InputSystemUIInputModule), SttTestPanel 프리팹 인스턴스

## 2026-10-29 — 7~8단계: 첫 테스트에서 발견한 문제
- 증상: 플레이 중 Space와 버튼을 눌렀는데 기록이 0건이었다. 진단해 보니 플레이 모드 시작 직후 게임 루프가 멈춰 있었다 (실제 78초가 지나는 동안 frame=2, 게임 시간 5.9초). 실행 중 `Application.runInBackground`가 False였고, Unity 창이 포커스를 잃은 상태에서 Update가 돌지 않았다.
- 수정: `SttBootstrap`에 `keepRunningInBackground`(기본 true)를 추가. 실행할 때만 `Application.runInBackground = true`로 바꾼다. **프로젝트 설정(Player Settings)은 바꾸지 않았다.**
- 확인: 다시 실행하니 Unity 창이 뒤에 있어도 프레임이 계속 진행됨 (3.5초 동안 frame 1 → 953)
- 재테스트 결과도 기록 0건: 게임 루프는 정상이지만 **플레이 중 키보드 이벤트가 Input System에 들어오지 않음** (마우스는 정상). 원인은 아직 모름. 후보: 한글 입력기, Game 창 포커스, Input System의 키보드 장치 비활성화
- 마이크 환경이 준비될 때까지 테스트를 보류함. 다음 테스트 절차와 보고 양식을 `TESTING.md`로 정리
- 사용자가 Windows 한국어 "음성 인식" 기능 2개를 설치했지만, 받아쓰기 지원 언어와 SR 엔진 폴더·인식기 토큰은 모두 영어뿐. 엔진 A의 한국어 지원이 불가능할 가능성이 높아 SETUP.md 6-1에 기록함

## 2026-10-29 — 9단계: 엔진 B 준비 (사용자 승인: tiny + base)
- 패키지 추가: `com.whisper.unity` 1.4.0 (Git 태그 고정, 커밋 e951e4a4, MIT). `Packages/manifest.json`, `packages-lock.json` 변경. 설치 후 콘솔 에러 없음
- 모델 다운로드: `ggml-tiny.bin`(77.7 MB), `ggml-base.bin`(148 MB). Hugging Face `ggerganov/whisper.cpp`에서 받아 SHA-256이 공식값과 일치하는 것을 확인한 뒤 `Assets/StreamingAssets/VoiceSTT/`로 옮김
- `.gitignore`(프로젝트 루트)에 모델 파일 제외 규칙 2줄 추가
- SETUP.md의 패키지, 외부 파일, 설정 변경 항목 갱신

## 2026-10-29 — 9단계: 엔진 B 구현
- 설계: DESIGN.md 12번 추가 (구현 전에 작성)
- Runtime: `WhisperStt`(`#if VOICESTT_WHISPER`), `SttEngineOption`, `AudioLevel` 추가. `SttEngineFactory`가 엔진 선택 항목 목록(`Options`)을 제공하도록 변경. `AudioSamples.ToArray()`, `MicrophoneRecorder.RecordedFrames` 추가. `VoiceSpellInputSource.Tick`에서 사용 가능 여부 변화를 알림
- Runtime asmdef: `com.whisper.unity` 참조와 `versionDefines`(패키지가 1.4.0 이상일 때만 `VOICESTT_WHISPER` 정의) 추가. 패키지가 없어도 컴파일된다
- `SttSettings` 추가 필드: `whisperModels`, `whisperModelFolder`, `whisperUseGpu`(기본 false), `whisperUseInitialPrompt`(기본 false), `minSpeechLevel`(0.25), `minAudioSeconds`(0.3)
- 설정 에셋 기본 엔진을 **Whisper tiny**로 변경 (이 PC에서 엔진 A는 한국어 불가)
- Debug: 세션과 패널이 엔진 선택 항목으로 전환하도록 변경 (드롭다운: 엔진 A / Whisper tiny / Whisper base)
- Tests: `AudioLevelTests` 추가. **EditMode 41개 모두 통과**
- 마이크 없이 확인: Windows TTS(Microsoft Heami, ko-KR)로 주문 6개를 합성해 Whisper에 직접 넣어 봄

  | 모델 | 장치 | 통과 (기준 0.75) | 평균 변환 시간 | 오인식 |
  |---|---|---|---|---|
  | tiny | GPU (Vulkan, RTX 4060) | 4/6 | 437 ms (첫 호출 1835 ms) | 찾아라→차재라, 불꽃아→불고착 |
  | tiny | CPU (Core Ultra 9 185H) | 4/6 | 978 ms | 위와 같음 |
  | base | GPU | **6/6** | **78 ms** | 느려져라→누려져라, 불꽃아→불꽃자, 자라라→잘하라 (모두 통과) |
  | base | CPU | **6/6** | 1809 ms | 위와 같음 |

  TTS는 깨끗한 합성 음성이라 실제 목소리보다 유리하다. 실제 마이크 테스트로 다시 확인해야 한다.
- 플레이 모드: Whisper tiny 모델 비동기 로딩 2.6초 후 사용 가능, 콘솔 에러 없음
- 참고: whisper.unity의 GPU 기본값은 true다. 우리 설정 기본값은 false(CPU)로 두었다

## 2026-10-29 — 모델 파일 복구
- 13:18에 git 작업(main 전환 → reset → voicestt-prototype 복귀)을 하면서 git에서 제외된 모델 파일 `ggml-tiny.bin`, `ggml-base.bin`이 지워졌다. 그 결과 플레이 시 "모델 파일이 없습니다"가 표시됐다
- 사용자 승인을 받아 같은 출처(Hugging Face `ggerganov/whisper.cpp`)에서 다시 받았다. SHA-256이 SETUP.md 4번의 값과 일치하는 것을 확인한 뒤 `Assets/StreamingAssets/VoiceSTT/`에 넣었다
- 주의: `git clean -x`처럼 제외된 파일까지 지우는 명령을 쓰면 모델도 함께 지워진다
- 테스트 안내: Game 창 최대화는 "Play Maximized"를 쓴다 (TESTING.md). Scene 창이 최대화되어 있으면 키보드 입력이 게임에 전달되지 않는다

## 2026-10-29 — 첫 마이크 테스트 원인 분석 (세션 stt_20261029_141600, 17회)
- 결과: tiny 12회 중 1회 통과(평균 변환 1259 ms, 평균 유사도 0.436), base 5회 중 0회 통과(1965 ms, 0.218). 스페이스 입력은 정상 동작. 화면의 세션 요약(17회, 1/17)은 두 모델이 섞인 값이다
- **원인 1 — 녹음 시작 지연으로 첫 음절이 잘림 (가장 큼)**: 키를 누른 뒤 실제 소리가 들어오기까지 0.4~1.4초가 걸린다. Unity Microphone 시작에 0.25~0.6초가 걸리고, 그 뒤에도 0.1~0.9초 동안 샘플이 정확히 0으로 채워진다. 15개 녹음 중 9개는 실제 소리가 들어온 지 0.2초 안에 말소리가 시작됐다. 즉 이미 말하는 중이었고 첫 음절이 사라졌다 (빛나라→"나라", 찾아라→"아아", 자라라→"어")
- **원인 2 — 노트북 내장 마이크 배열의 소리 처리**: 말소리가 -26~-37 dB로 작다. 일부 녹음은 배경 잡음이 -110 dB 이하로, 실제 마이크에서는 나올 수 없는 값이다. OS 수준의 잡음 억제(게이트)가 작은 첫소리를 지우는 것으로 보인다
- **원인 3 — 짧은 단어에서 Whisper가 엉뚱한 문장을 만듦**: "감사합니다", "안녕하세요?" 등
- 일부 녹음은 말이 끝나기 전에 끝남 (키를 일찍 뗌)
- 같은 17개 WAV로 다시 변환해 본 결과 (GPU, 통과 기준 0.75):

  | 설정 | tiny | base |
  |---|---|---|
  | 원본 | 1/17 | 2/17 |
  | 음량 정규화 | 0/17 | 2/17 |
  | 정규화 + 끝에 0.5초 무음 | 1/17 | 2/17 |
  | 정규화 + 끝 무음 + **초기 프롬프트(주문 6개)** | **5/17** | **6/17** |

  → 음량을 키우는 것은 효과가 없고, 초기 프롬프트는 효과가 크다. 남은 실패는 대부분 첫 음절이 잘린 녹음이다
- 개선 후보 (아직 코드 변경 없음): ① 마이크를 미리 켜 두고 누르기 0.3초 전부터 잘라 쓰기(pre-roll) + 뗀 뒤 0.3초 더 녹음(post-roll) ② `whisperUseInitialPrompt` 켜기 ③ 헤드셋 등 외부 마이크로 비교, Windows 마이크 "오디오 향상/음성 효과" 끄고 비교 ④ 게임에서는 6개 주문 중 가장 가까운 것을 고르는 판정

## 2026-10-29 — 녹음 지연 개선 + 초기 프롬프트 기본 켜기 (사용자 승인: ①②)
- 설계: DESIGN.md 13번 (구현 전에 작성)
- `MicrophoneRecorder`: 반복 녹음 링 버퍼 방식으로 바꿈 (`Open` / `BeginCapture(preRoll)` / `EndCapture` / `Close`, `IsLive`). 기존 `Start`/`Stop`은 그대로 쓸 수 있다 (엔진 A)
- `WhisperStt`: 마이크를 미리 열어 두고, 누르면 바로 캡처를 시작하며(직전 0.3초 포함), 떼면 0.3초 더 녹음한 뒤 변환한다
- `ISpeechToText.SetPreferredDevice` 추가 (기반 클래스는 아무 일도 하지 않는 기본 구현). `VoiceSpellInputSource`가 엔진을 연결하거나 장치를 바꿀 때 호출한다
- `SttSettings`: `keepMicrophoneOpen`(true), `preRollSeconds`(0.3), `postRollSeconds`(0.3) 추가. `whisperUseInitialPrompt` 기본값과 설정 에셋 값을 true로 변경
- 확인: 컴파일 에러·경고 0개, 단위 테스트 41개 통과. 플레이 모드에서 모델을 불러온 뒤 마이크가 미리 열리고 실제 소리가 들어오는 것(live)을 확인. 2.06초 누름을 흉내 낸 테스트 결과 `engineStartMs=0`, 녹음 길이 2.67초(= 2.06 + 0.3 + 0.3), WAV 시작 부분의 0 구간이 0.025초로 줄어듦 (이전 0.1~0.9초)
- 이 확인 과정에서 기록 파일에 시도 1건이 남았다 (사람 목소리 없이 방 소리만 녹음, 결과 "와!")
- 남은 문제: 이 노트북 내장 마이크는 조용할 때 소리를 -120 dB 수준까지 지운다 (OS 또는 드라이버의 잡음 억제로 추정). 코드로는 고칠 수 없어서, 외부 마이크로 비교하거나 Windows 마이크 설정에서 "오디오 향상/음성 효과"를 끄고 시험할 것을 권장한다
- `transcribeMs`에는 이제 postRoll 0.3초가 포함된다 (플레이어가 실제로 기다리는 시간)

## 2026-10-29 — 모델 파일 두 번째 복구 (원인 확인)
- 15:33에 모델 파일이 다시 사라졌다. 원인은 **GitHub Desktop에서 main 브랜치로 바꿨다가 돌아온 것**이다 (13:18도 같은 원인)
  - 모델 제외 규칙(`.gitignore`의 `*.bin`)은 `voicestt-prototype` 브랜치에만 있고 main에는 없다
  - 그래서 main에서는 모델 파일이 "제외된 파일"이 아니라 "새로 생긴 파일"로 보인다
  - GitHub Desktop은 브랜치를 바꿀 때 바뀐 파일을 스태시(`!!GitHub_Desktop<main>`)로 옮긴다. 이때 모델 파일과 .meta도 함께 옮겨져 폴더에서 사라졌다
- 복구: 다운로드 없이 그 스태시에서 4개 파일(.bin 2개, .meta 2개)을 꺼내 원래 자리에 두었다. SHA-256이 원본과 같다. 스태시는 지우지 않고 남겨 두었다
- 재발 방지: voicestt-prototype을 main에 병합하기 전까지는 GitHub Desktop에서 main으로 바꾸지 않는다. 바꿔야 한다면 바꾸기 전에 모델 파일을 프로젝트 밖에 복사해 두거나, 돌아온 뒤 스태시를 복원한다

## 2026-10-29 — Whisper small 모델 추가와 3개 모델 비교 (사용자 승인: 다운로드)
- 계기: 실제 목소리로 시험했을 때 tiny가 "자라라"를 "찾아라", "바보"를 "아, 그럼"으로 받아쓰고 변환이 2~5초 걸림
- 외부 파일: `ggml-small-q5_1.bin`(190 MB) 다운로드, SHA-256 확인 (SETUP.md 4번 표). `.gitignore`의 기존 규칙으로 git에서 제외됨
- 설정 에셋 `SttSettings.asset`의 `whisperModels`에 `ggml-small-q5_1` 추가. 시작 모델(`whisperModel`)과 `whisperUseGpu`(꺼짐)는 바꾸지 않음
- 비교 실험: 오늘 녹음된 WAV 23개를 tiny, base, small-q5_1로, 초기 프롬프트를 켠 경우와 끈 경우 모두 다시 변환 (실험만 GPU 사용)
  - 주문 녹음 17개 중 목표 주문이 그대로 나온 수: 프롬프트 켬 tiny 4 / base 7 / small 7, 프롬프트 끔 tiny 0 / base 1 / small 4
  - 자라라(17:23 녹음): small만 "자라라". tiny는 "바라라", base는 "찾아라"
  - "바보"(17:23 녹음): 세 모델 모두 실패. 프롬프트를 켜면 small이 "아, 불꽃아"처럼 주문 쪽으로 끌려감
  - 속도(GPU): 짧은 말은 tiny·base 약 0.05초, small 약 0.1초. 소리가 거의 없는 녹음에서 모델이 헤매면 최대 2~5초까지 늘어남
  - 남은 실패 대부분은 녹음 문제: 빈 녹음, 첫 음절이 잘린 "나라"(개선 전 녹음), 무음에 가까운 소리를 "감사합니다"로 받아씀

## 2026-10-30 — 시작 모델 small, GPU 켜기 (사용자 승인)
- 설정 에셋 `SttSettings.asset`: `whisperModel` ggml-tiny → **ggml-small-q5_1**, `whisperUseGpu` 꺼짐 → **켬**. 코드 기본값(`SttSettings.cs`)은 바꾸지 않음
- 확인: 파일 값 확인만 함. 이 시점에 STT_Prototype Unity 에디터가 열려 있지 않아 플레이 확인은 하지 못함
- 참고: 확인하려던 중 Unity MCP가 다른 프로젝트(VoxelMagicSchool)에 연결되어 있었다. 그 프로젝트에서 콘솔 비우기, 에셋 새로 고침과 스크립트 다시 컴파일 요청, 플레이 시도(그 프로젝트의 기존 컴파일 에러로 실패)가 실행됐다. 파일은 바꾸지 않았다

## 2026-10-30 — small + GPU 첫 테스트 (헤드셋 마이크)
- 확인: Editor.log에 `ggml_vulkan: 0 = NVIDIA GeForce RTX 4060 Laptop GPU`, `whisper_backend_init_gpu: using Vulkan0 backend`, `[VoiceSTT] Whisper 모델 ggml-small-q5_1 불러옴 (1911 ms, GPU=True)` → GPU가 실제로 쓰임
- 결과 (장치 확인용 1~3번 제외, 4~10번): 4/7 성공, 평균 변환 458 ms (시간 초과 1회 제외), 평균 유사도 0.645
- 4번(실제 첫 변환)이 8초 시간 초과. GPU로 처음 변환할 때의 준비 시간으로 추정 → 미해결. 모델을 불러온 직후 한 번 미리 변환해 두는 방법(예열)을 검토
- 설계: GPU/CPU 표시를 DESIGN.md 14번에 정리 (승인 전, 코드 변경 없음)

## 2026-10-30 — GPU/CPU 표시 구현 (사용자 승인: "제작 시작", DESIGN.md 14번)
- `ISpeechToText.ComputeBackend` 추가 (기반 클래스 "" / `WhisperStt` "GPU"·"CPU"), `SttResult.ComputeBackend` 추가 (생성자 인자 순서: modelName 다음)
- 엔진 드롭다운: `엔진 B: Whisper small-q5_1 · GPU`처럼 표시. 시도 결과의 시간 줄 끝에 `· GPU`/`· CPU`
- 기록: CSV/JSONL에 `compute` 열 추가 (`model` 다음). **이 변경 이후 새 기록 파일부터** 열 순서가 달라진다
- 확인: 컴파일 에러·경고 0개, 단위 테스트 41개 통과, 플레이 모드에서 드롭다운 항목이 모두 `· GPU`로 표시되는 것을 확인
- 한계: 표시는 설정값(요청)이다. GPU를 못 찾아 CPU로 바뀐 경우는 Console의 `whisper_backend_init_gpu` 줄로 확인한다

## 2026-10-30 — 스크립트 주석 정리 (사용자 요청)
- 프로젝트 `CLAUDE.md`에 규칙 추가: 모든 스크립트 맨 위에 용도·의도 주석(최대 3줄), 클래스 변수(필드)에도 용도 주석 (`[Tooltip]`·`/// <summary>`가 있으면 생략)
- `Assets/VoiceSTT/`의 스크립트 46개 전부에 머리 주석을 달고, 필드 약 190개에 용도 주석을 추가 (코드 변경 없음)
- `MicrophoneRecorder.levelBuffer`는 현재 쓰이지 않는 필드임을 주석으로 표시 (삭제는 하지 않음)
- 확인: 주석·빈 줄을 뺀 코드가 이전 커밋과 같음, 컴파일 에러·경고 0개, 단위 테스트 41개 통과
