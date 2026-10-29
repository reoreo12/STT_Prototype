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
