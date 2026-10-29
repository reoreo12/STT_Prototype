using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VoxelMagic.Voice.Debug
{
    /// <summary>
    /// 의존성 조립 담당. 설정 에셋을 읽어 엔진·입력·판정·기록·세션을 만들고 테스트 패널에 연결한다.
    /// 객체끼리의 연결은 모두 여기서만 한다 (싱글턴·FindObjectOfType 사용 안 함).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SttBootstrap : MonoBehaviour
    {
        [SerializeField] private SttSettings settings;
        [SerializeField] private KeyboardPushToTalk keyboardInput;
        [SerializeField] private SttTestPanel panel;
        [Tooltip("Unity 창이 포커스를 잃어도 게임 루프를 계속 돌린다. 끄면 다른 창을 보는 동안 입력·시간 측정이 멈춘다.")]
        [SerializeField] private bool keepRunningInBackground = true;

        private VoiceSpellInputSource voice;
        private SttLogger logger;
        private SttTestSession session;

        /// <summary>게임 코드에서 음성 입력을 받아 쓰고 싶을 때 사용.</summary>
        public ISpellInputSource VoiceInput => voice;
        public SttTestSession Session => session;

        private void Awake()
        {
            if (settings == null || keyboardInput == null || panel == null)
            {
                UnityEngine.Debug.LogError("[VoiceSTT] SttBootstrap의 settings, keyboardInput, panel 참조를 채워야 합니다.", this);
                enabled = false;
                return;
            }

            // 프로젝트 설정은 바꾸지 않고 실행 중에만 적용한다.
            if (keepRunningInBackground) Application.runInBackground = true;

            var time = new StopwatchTimeSource();
            var factory = new SttEngineFactory(settings, time);
            SttEngineOption engineOption = factory.DefaultOption;

            panel.Build(settings);
            keyboardInput.SetKey(settings.PushToTalkKey);
            keyboardInput.SetBlocker(() => panel.IsTextInputFocused);

            voice = new VoiceSpellInputSource(factory.Create(engineOption), new IPushToTalkSource[] { keyboardInput, panel.PushButton });
            voice.SetMicDevice(settings.PreferredMicDevice);

            var sinks = new List<ISttLogSink>();
            if (settings.WriteCsv) sinks.Add(new CsvLogSink());
            if (settings.WriteJsonl) sinks.Add(new JsonlLogSink());
            logger = new SttLogger(Path.Combine(Application.persistentDataPath, settings.LogFolderName),
                settings.LoggingEnabled, settings.SaveWav, sinks);

            string initialTarget = settings.TargetPhrases.Count > 0 ? settings.TargetPhrases[0] : "";
            session = new SttTestSession(voice, new JamoSimilarityMatcher(settings.SimilarityThreshold), logger, factory,
                engineOption, initialTarget, Application.unityVersion, DescribeBuildType());

            panel.Bind(session);
            UnityEngine.Debug.Log($"[VoiceSTT] 준비 완료. 엔진={engineOption.DisplayName}, 사용 가능={voice.IsAvailable}, 폰트={KoreanFont.ResolvedName}, 로그={logger.FolderPath}");
        }

        private void Start()
        {
            if (EventSystem.current == null)
            {
                UnityEngine.Debug.LogWarning("[VoiceSTT] 씬에 EventSystem이 없어 화면 버튼·드롭다운이 동작하지 않습니다.", this);
            }
        }

        private void Update()
        {
            voice?.Tick();
        }

        private void OnDestroy()
        {
            session?.Dispose();
            voice?.Dispose();
            logger?.Dispose();
        }

        private static string DescribeBuildType()
        {
            if (Application.isEditor) return "Editor";
            return UnityEngine.Debug.isDebugBuild ? "DevelopmentBuild" : "ReleaseBuild";
        }
    }
}
