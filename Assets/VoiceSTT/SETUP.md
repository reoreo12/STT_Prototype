# VoiceSTT 설치·설정·이식 문서

> 작업하면서 바뀌는 즉시 갱신한다. (작성 중 — "작성 예정" 항목은 해당 단계에서 채움)

## 1. 환경

| 항목 | 값 |
|---|---|
| Unity 버전 | **6000.0.38f1** (revision 82314a941f2d) |
| 렌더 파이프라인 | **URP 17.0.3** (`com.unity.render-pipelines.universal`), 사용 중인 에셋 `Assets/Settings/PC_RPAsset` |
| 대상 플랫폼 | StandaloneWindows64 |
| 스크립팅 백엔드 | Mono (Standalone 기본값) |
| API 호환성 수준 | .NET Standard 2.1 (Unity 6 기본값, 내부 enum 이름 `NET_Standard_2_0`) |
| 입력 처리 방식 | Both (구 Input Manager + Input System 1.13.0). **VoiceSTT는 새 Input System을 사용**함 (`Keyboard.current`, `InputSystemUIInputModule`) |
| 테스트한 OS | Windows 11 Home 10.0.26200 64bit, 지역 ko-KR, 표시 언어 en-US |
| 테스트한 마이크 | 마이크 배열 (2- 디지털 마이크용 인텔® 스마트 사운드 기술), 48000 Hz |

## 2. 패키지

`Packages/manifest.json`에 추가한 항목:

```json
"com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.3.0",
"com.whisper.unity": "https://github.com/Macoron/whisper.unity.git?path=/Packages/com.whisper.unity#1.4.0",
```

| 패키지 | 버전 고정 | 용도 | 라이선스 | 게임 이식 |
|---|---|---|---|---|
| `com.coplaydev.unity-mcp` (MCP for Unity) | 태그 `v10.3.0` (커밋 aa5fc638d623f56178d50329d9ad8c541a57fe66) | Claude Code가 Unity 에디터를 제어하는 개발 도구. MCP 서버 `mcpforunityserver` 10.3.0과 버전을 맞춤 | MIT | **필수 아님**. 게임 프로젝트도 Claude Code로 작업할 경우에만 추가 |
| `com.whisper.unity` (whisper.unity, 2026-10-29 추가) | 태그 `1.4.0` (커밋 e951e4a4c6e44c781b1d36bb8dc5bf1b7bae9687, 2025-04-16 배포) | 엔진 B. whisper.cpp 1.7.5를 Unity에서 쓰는 바인딩. 로컬(오프라인) 실행, CPU 또는 Vulkan GPU. 어셈블리 `com.whisper.unity`, 네임스페이스 `Whisper` | MIT (whisper.cpp와 모델도 MIT). 출처 https://github.com/Macoron/whisper.unity | **엔진 B를 쓰면 필수**. 패키지 크기 약 80.5 MB (모든 플랫폼 네이티브 라이브러리 포함, `Library/PackageCache`에 설치) |

VoiceSTT 코드가 의존하는 **기본 포함 패키지** (새 프로젝트에 이미 있는지만 확인):

| 패키지 | 이 프로젝트 버전 | 사용하는 어셈블리 |
|---|---|---|
| `com.unity.inputsystem` | 1.13.0 | Runtime, Debug |
| `com.unity.ugui` | 2.0.0 | Runtime(`ScreenButtonPushToTalk`), Debug(UI) |
| `com.unity.test-framework` | 1.4.6 | Tests |

## 3. 프로젝트 설정 변경 목록

| 설정 | 변경 전 | 변경 후 | 이유 |
|---|---|---|---|
| (Unity 프로젝트 설정은 직접 바꾸지 않음) | | | |
| `.gitignore` (프로젝트 루트) | 모델 제외 규칙 없음 | `Assets/StreamingAssets/VoiceSTT/*.bin`, `*.bin.meta` 제외 | 모델 파일이 커서 (2026-10-29, 사용자 승인) |

- whisper.unity 런타임 어셈블리는 자체 asmdef에서 `allowUnsafeCode: true`를 쓴다. 프로젝트 전체의 "Allow unsafe code"(Player 설정)는 **바꿀 필요가 없다.**

참고: 5단계에서 새 씬을 만들 때 Unity가 `ProjectSettings/SceneTemplateSettings.json`을 **자동으로 생성**했다. 씬 템플릿의 기본값만 들어 있고 VoiceSTT 동작과는 관계없으므로, 이식할 때 옮기지 않아도 된다.

- 빌드 설정(Scenes In Build)에는 `STTTest`를 추가하지 않았다. 에디터에서 씬을 열고 실행한다. 빌드로 시험할 때는 추가 전에 확인한다.
- UI 입력 모듈은 `InputSystemUIInputModule`의 기본 액션(DefaultInputActions)을 쓴다. Submit은 `*/{Submit}`(Enter)에만 연결되어 있어 Space(녹음 키)와 겹치지 않는다. 게임 프로젝트에서 Submit에 Space를 연결했다면 녹음 키를 바꿔야 한다.

## 4. 외부 파일

### Whisper 모델 (엔진 B)

| 파일 | 크기 | SHA-256 | 출처 URL |
|---|---|---|---|
| `ggml-tiny.bin` | 77,691,713 B (77.7 MB) | `be07e048e1e599ad46341c8d2a135645097a538221678b7acdd1b1919c6e1b21` | https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin |
| `ggml-base.bin` | 147,951,465 B (148 MB) | `60ed5bc3dd14eea856493d334349b405782ddcaf0028d4b5df4088345fba2efe` | https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin |
| `ggml-small-q5_1.bin` (2026-10-29 추가) | 190,085,487 B (190 MB) | `ae85e4a935d7a567bd102fe55afc16bb595bdb618e11b2fc7591bc08120411bb` | https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small-q5_1.bin |

- `ggml-small-q5_1`은 small 모델을 5비트로 양자화한 것이다. 원본 small(488 MB)보다 작고 빠르며, 정확도 차이는 작다. 설정 에셋의 `whisperModels` 목록에 넣어 엔진 드롭다운에 "엔진 B: Whisper small-q5_1"로 나온다 (코드 변경 없음)
- 라이선스: MIT (OpenAI Whisper 가중치를 whisper.cpp 형식으로 변환한 것. 저장소 https://huggingface.co/ggerganov/whisper.cpp)
- 저장 위치: **`Assets/StreamingAssets/VoiceSTT/`**. Unity 규칙상 StreamingAssets는 `Assets/` 바로 아래에 있어야 하므로 `Assets/VoiceSTT/` 밖에 둔다. **이식할 때 이 폴더도 함께 옮겨야 한다.**
- **git에서 제외**: 프로젝트 루트 `.gitignore`에 아래 두 줄을 추가했다 (2026-10-29). 저장소를 새로 받으면 모델이 없으므로 아래 명령으로 받는다.

  ```
  /[Aa]ssets/[Ss]treaming[Aa]ssets/VoiceSTT/*.bin
  /[Aa]ssets/[Ss]treaming[Aa]ssets/VoiceSTT/*.bin.meta
  ```

  모델 받는 방법 (Git Bash, 프로젝트 루트에서):

  ```bash
  mkdir -p Assets/StreamingAssets/VoiceSTT
  curl -L -o Assets/StreamingAssets/VoiceSTT/ggml-tiny.bin https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin
  curl -L -o Assets/StreamingAssets/VoiceSTT/ggml-base.bin https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin
  curl -L -o Assets/StreamingAssets/VoiceSTT/ggml-small-q5_1.bin https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small-q5_1.bin
  sha256sum Assets/StreamingAssets/VoiceSTT/*.bin   # 위 표의 SHA-256과 비교
  ```

## 5. 폴더·어셈블리 구조

```
Assets/VoiceSTT/
├─ Runtime/   VoxelMagic.Voice.asmdef          (참조: Unity.InputSystem, UnityEngine.UI)
│  ├─ Engines/ Matching/ Input/ Audio/ Spells/
│  └─ SttSettings.cs
├─ Debug/     VoxelMagic.Voice.Debug.asmdef    (참조: VoxelMagic.Voice, Unity.InputSystem, UnityEngine.UI)
│  └─ Prefabs/
├─ Editor/    VoxelMagic.Voice.Editor.asmdef   (Editor 전용, 참조: VoxelMagic.Voice)
├─ Tests/     VoxelMagic.Voice.Tests.asmdef    (Editor 전용, UNITY_INCLUDE_TESTS, NUnit)
├─ Settings/  SttSettings.asset                (기본 설정값)
├─ Scenes/    STTTest.unity                    (Camera, Light, EventSystem, SttTestPanel 프리팹)
├─ DESIGN.md / SETUP.md / CHANGELOG.md
Assets/StreamingAssets/VoiceSTT/               (엔진 B 모델 파일 위치, 아직 없음)
```

- **Runtime은 Debug를 참조하지 않는다.** 게임에는 Runtime만 있어도 컴파일된다.
- 설정값은 모두 `Settings/SttSettings.asset`에서 읽는다 (엔진, 언어 `ko`, 녹음 키, 시간 제한, 기준값 0.75, 목표 주문 6개, 기록 여부).
- 현재 기본값 (2026-10-30): 엔진 B, 시작 모델 `ggml-small-q5_1`, **GPU 켬**(`whisperUseGpu`), 초기 프롬프트 켬
  - 바꾸는 곳: Project 창 `Assets/VoiceSTT/Settings/SttSettings` 선택 → Inspector "엔진" 묶음의 `Whisper Model`, `Whisper Use Gpu`
  - 플레이를 멈춘 상태에서 바꾸고 다시 플레이해야 반영된다. Console의 `Whisper 모델 … 불러옴 (… ms, GPU=…)`로 확인한다
  - GPU(Vulkan)를 지원하지 않는 PC에서는 라이브러리가 CPU로 동작한다. 그때 small은 tiny보다 몇 배 느리므로 tiny로 되돌리는 것을 검토한다
- 주의: Debug 어셈블리의 네임스페이스가 `VoxelMagic.Voice.Debug`이므로, 그 안에서는 `UnityEngine.Debug.Log`처럼 전체 이름을 써야 한다.

## 6. 한글 폰트

- 방식: **uGUI 레거시 `Text` + OS 폰트**. `Debug/UI/KoreanFont.cs`가 `Font.CreateDynamicFontFromOSFont`로 한 번 만들어 모든 Text에 넣는다. 폰트 에셋 파일이나 다운로드, TMP Essentials 가져오기는 필요 없다.
- 후보 순서: Malgun Gothic → 맑은 고딕 → Apple SD Gothic Neo → Noto Sans CJK KR → Noto Sans KR → NanumGothic. 이 PC에서는 **Malgun Gothic**이 선택되었고 한글이 정상으로 표시되는 것을 확인했다 (2026-10-29).
- 이식할 때 주의: OS 폰트이므로 **해당 폰트가 없는 OS(macOS 일부, Linux, 모바일)에서는 한글이 네모로 보일 수 있다.** 이 방식은 디버그 UI 전용이다. 게임 본편 UI는 TMP 폰트 에셋(한글 폰트 파일 포함)으로 따로 만드는 것을 권장한다.

## 6-1. 엔진 A (Windows 받아쓰기) 사용 조건

`DictationRecognizer`는 언어를 코드로 정할 수 없고 **Windows 음성 언어**를 따른다. 한국어로 인식하려면 Windows 설정을 확인해야 한다.

1. 설정 → 시간 및 언어 → 언어 및 지역: 한국어 언어 팩과 **음성 인식** 기능 설치
2. 설정 → 시간 및 언어 → 음성: **음성 언어 = 한국어**
3. 설정 → 개인 정보 및 보안 → 음성: **온라인 음성 인식 켜기** (받아쓰기는 Microsoft 온라인 서비스를 사용)
4. 설정 → 개인 정보 및 보안 → 마이크: 데스크톱 앱의 마이크 접근 허용
5. 마이크는 **Windows 기본 입력 장치**가 쓰인다 (설정 → 시스템 → 소리 → 입력)

1단계 확인 당시 이 PC는 표시 언어가 en-US이고 한국어 음성 인식기가 없었다. 테스트 결과에 따라 이 항목을 갱신한다.

- 주의: **"음성 액세스(Voice Access)"의 언어 다운로드는 다른 기능이다.** 음성 액세스 전용 모델이라 `DictationRecognizer`에는 쓰이지 않는다.
- **받아쓰기가 지원하는 언어 확인** (PowerShell, 읽기만 함). `SupportedTopicLanguages`에 `ko-KR`이 있고 `SystemSpeechLanguage`가 `ko-KR`이어야 엔진 A가 한국어로 인식한다.

  ```powershell
  [void][Windows.Media.SpeechRecognition.SpeechRecognizer, Windows.Media.SpeechRecognition, ContentType = WindowsRuntime]
  $R = [Windows.Media.SpeechRecognition.SpeechRecognizer]
  $R::SystemSpeechLanguage.LanguageTag
  ($R::SupportedTopicLanguages | ForEach-Object { $_.LanguageTag }) -join ', '
  ```

  2026-10-29 결과: `SystemSpeechLanguage = en-US`, `SupportedTopicLanguages = en-US` (한국어 없음)

  2026-10-29, 한국어 언어 옵션의 "음성 인식" 2개를 설치한 뒤 다시 확인:
  - 지원 언어는 그대로 `en-US`뿐
  - 음성 인식 엔진 폴더 `C:\Windows\Speech_OneCore\Engines\SR`에도 `en-US-N`만 있음
  - 레지스트리의 인식기 토큰도 `MS-1033`(영어)뿐
  - → 설치된 한국어 "음성 인식" 기능은 엔진 A가 쓰는 Windows 음성 인식 엔진(OneCore SR)을 추가하지 않았다. 이 PC에서는 **엔진 A로 한국어를 인식할 수 없는 것으로 보인다** (재부팅 후 최종 확인 필요)

- `SttSettings.dictationUseUnityMicrophone`(기본 켬): 음량 미터와 WAV 저장을 위해 Unity `Microphone`으로 함께 녹음한다. 받아쓰기와 마이크 충돌이 있으면 끈다.

## 7. 이식 절차

(작성 예정 — 체크리스트는 11단계에서 확정)

## 8. 게임에 붙이는 방법

(작성 예정 — 주문 입력 → `SpellMatcher` 판정 → 이벤트 최소 예시)
