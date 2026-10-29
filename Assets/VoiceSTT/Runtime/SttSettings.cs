using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VoxelMagic.Voice
{
    /// <summary>
    /// VoiceSTT의 모든 설정값. 코드에 값을 하드코딩하지 않고 이 에셋에서 읽는다.
    /// 실행 중 UI에서 바꾸는 값은 이 에셋에 다시 쓰지 않고 런타임 객체에만 반영한다.
    /// </summary>
    [CreateAssetMenu(fileName = "SttSettings", menuName = "VoiceSTT/Stt Settings")]
    public sealed class SttSettings : ScriptableObject
    {
        [Header("엔진")]
        [SerializeField] private SttEngineType engineType = SttEngineType.WindowsDictation;
        [Tooltip("엔진 B(Whisper)로 시작할 때 쓸 모델 이름 (확장자 .bin 제외).")]
        [SerializeField] private string whisperModel = "ggml-tiny";
        [Tooltip("엔진 드롭다운에 보일 Whisper 모델 목록 (확장자 .bin 제외).")]
        [SerializeField] private string[] whisperModels = { "ggml-tiny", "ggml-base" };
        [Tooltip("모델 파일이 있는 StreamingAssets 아래 폴더 이름.")]
        [SerializeField] private string whisperModelFolder = "VoiceSTT";
        [Tooltip("Vulkan GPU 가속. 지원하지 않는 GPU에서는 CPU로 동작한다.")]
        [SerializeField] private bool whisperUseGpu = false;
        [Tooltip("목표 주문 목록을 초기 프롬프트로 넣어 인식을 유도한다. 원래 성능을 재려면 끈다.")]
        [SerializeField] private bool whisperUseInitialPrompt = false;
        [Tooltip("인식 언어 코드. 엔진 A는 Windows 음성 언어를 따르므로 확인용으로만 쓰인다.")]
        [SerializeField] private string language = "ko";

        [Header("입력")]
        [SerializeField] private Key pushToTalkKey = Key.Space;
        [Tooltip("비워 두면 기본 마이크. 엔진 A는 항상 Windows 기본 마이크를 쓴다.")]
        [SerializeField] private string preferredMicDevice = "";

        [Header("시간")]
        [SerializeField, Min(1f)] private float maxRecordSeconds = 10f;
        [SerializeField, Min(1f)] private float transcribeTimeoutSeconds = 8f;
        [Tooltip("엔진 A: 키를 뗀 뒤 최종 결과(DictationResult)를 기다리는 시간.")]
        [SerializeField, Min(0)] private int dictationFinalResultWaitMs = 1500;
        [Tooltip("엔진 A: 음량 미터·WAV 저장을 위해 Unity Microphone으로 함께 녹음한다. 받아쓰기와 충돌하면 끈다.")]
        [SerializeField] private bool dictationUseUnityMicrophone = true;
        [Tooltip("녹음 전체의 최대 음량이 이보다 작으면 변환하지 않는다 (0~1, 0.25 ≈ -45 dB). 녹음 샘플이 있는 엔진만 해당.")]
        [SerializeField, Range(0f, 1f)] private float minSpeechLevel = 0.25f;
        [Tooltip("이보다 짧은 녹음은 변환하지 않는다 (초).")]
        [SerializeField, Min(0f)] private float minAudioSeconds = 0.3f;

        [Header("판정")]
        [SerializeField, Range(0f, 1f)] private float similarityThreshold = 0.75f;
        [SerializeField] private string[] targetPhrases = { "빛나라", "찾아라", "느려져라", "불꽃아", "자라라", "숨어라" };

        [Header("기록 (출시 빌드에서는 끔)")]
        [SerializeField] private bool loggingEnabled = true;
        [SerializeField] private bool writeCsv = true;
        [SerializeField] private bool writeJsonl = true;
        [Tooltip("녹음 샘플이 있는 엔진에서만 WAV를 저장한다.")]
        [SerializeField] private bool saveWav = true;
        [SerializeField] private string logFolderName = "STTLogs";

        public SttEngineType EngineType => engineType;
        public string WhisperModel => whisperModel;
        public IReadOnlyList<string> WhisperModels => whisperModels;
        public string WhisperModelFolder => whisperModelFolder;
        public bool WhisperUseGpu => whisperUseGpu;
        public bool WhisperUseInitialPrompt => whisperUseInitialPrompt;
        public string Language => language;

        public Key PushToTalkKey => pushToTalkKey;
        public string PreferredMicDevice => preferredMicDevice;

        public float MaxRecordSeconds => maxRecordSeconds;
        public float TranscribeTimeoutSeconds => transcribeTimeoutSeconds;
        public int DictationFinalResultWaitMs => dictationFinalResultWaitMs;
        public bool DictationUseUnityMicrophone => dictationUseUnityMicrophone;
        public float MinSpeechLevel => minSpeechLevel;
        public float MinAudioSeconds => minAudioSeconds;

        public float SimilarityThreshold => similarityThreshold;
        public IReadOnlyList<string> TargetPhrases => targetPhrases;

        public bool LoggingEnabled => loggingEnabled;
        public bool WriteCsv => writeCsv;
        public bool WriteJsonl => writeJsonl;
        public bool SaveWav => saveWav;
        public string LogFolderName => logFolderName;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(language)) language = "ko";
            if (string.IsNullOrWhiteSpace(logFolderName)) logFolderName = "STTLogs";
            if (targetPhrases == null) targetPhrases = new string[0];
            if (whisperModels == null) whisperModels = new string[0];
            if (string.IsNullOrWhiteSpace(whisperModelFolder)) whisperModelFolder = "VoiceSTT";
        }
    }
}
