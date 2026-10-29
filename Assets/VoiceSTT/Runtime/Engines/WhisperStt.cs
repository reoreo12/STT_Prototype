#if VOICESTT_WHISPER
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Whisper;

namespace VoxelMagic.Voice
{
    /// <summary>
    /// 엔진 B: Whisper 로컬 실행 (whisper.unity / whisper.cpp).
    /// MicrophoneRecorder로 녹음하고, 키를 떼면 녹음 전체를 한 번에 변환한다 (중간 결과 없음).
    /// 모델은 생성할 때 비동기로 불러오며, 다 불러오기 전에는 IsAvailable이 false다.
    /// 무음·너무 짧은 녹음은 변환하지 않는다 (Whisper가 무음에서 엉뚱한 문장을 만드는 것을 막기 위해).
    /// </summary>
    public sealed class WhisperStt : SpeechToTextEngineBase
    {
        private readonly string modelName;
        private readonly string modelPath;
        private readonly string language;
        private readonly string initialPrompt;
        private readonly float minSpeechLevel;
        private readonly float minAudioSeconds;
        private readonly MicrophoneRecorder recorder;

        private WhisperWrapper whisper;
        private bool loading;
        private bool loadNotified;
        private string loadError = "";

        public WhisperStt(ITimeSource time, float maxRecordSeconds, float transcribeTimeoutSeconds,
            string modelPath, string modelName, string language, bool useGpu, string initialPrompt,
            float minSpeechLevel, float minAudioSeconds, MicrophoneRecorder recorder)
            : base(time, maxRecordSeconds, transcribeTimeoutSeconds)
        {
            this.modelPath = modelPath ?? throw new ArgumentNullException(nameof(modelPath));
            this.modelName = modelName ?? "";
            this.language = string.IsNullOrEmpty(language) ? "ko" : language;
            this.initialPrompt = initialPrompt ?? "";
            this.minSpeechLevel = minSpeechLevel;
            this.minAudioSeconds = minAudioSeconds;
            this.recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));

            if (File.Exists(modelPath)) LoadModel(useGpu);
            else loadError = $"모델 파일이 없습니다: {modelPath}";
        }

        public override string EngineName => "Whisper";
        public override string ModelName => modelName;
        public override float InputLevel => recorder.IsRecording ? recorder.Level : -1f;

        /// <summary>모델을 불러오는 데 걸린 시간(ms). 아직이면 -1.</summary>
        public double ModelLoadMs { get; private set; } = -1;

        protected override bool CheckAvailability(out string reason)
        {
            if (loadError.Length > 0)
            {
                reason = loadError;
                return false;
            }
            if (loading || whisper == null)
            {
                reason = $"Whisper 모델({modelName}) 불러오는 중…";
                return false;
            }
            if (Microphone.devices.Length == 0)
            {
                reason = "연결된 마이크가 없습니다.";
                return false;
            }
            reason = "";
            return true;
        }

        protected override void OnStartListening(string deviceName)
        {
            if (!recorder.Start(deviceName)) throw new InvalidOperationException(recorder.LastError);
        }

        protected override Task<EngineTranscription> OnStopAndTranscribeAsync()
        {
            AudioSamples audio = recorder.Stop();
            if (audio == null || audio.Seconds < minAudioSeconds)
            {
                double seconds = audio == null ? 0 : audio.Seconds;
                return Task.FromResult(EngineTranscription.Failure($"녹음이 너무 짧습니다 ({seconds:0.00}초)", audio));
            }

            float peak = AudioLevel.Peak(audio);
            if (peak < minSpeechLevel)
            {
                return Task.FromResult(EngineTranscription.Failure(
                    $"음성이 감지되지 않음 (최대 음량 {peak:0.00} < 기준 {minSpeechLevel:0.00})", audio));
            }

            return TranscribeAsync(audio);
        }

        protected override void OnTick()
        {
            recorder.Tick();
            if (State == SttState.Listening && recorder.RecordedFrames > 0) MarkListeningStarted();

            if (!loading && !loadNotified)
            {
                loadNotified = true;
                RefreshAvailability();
            }
        }

        protected override void OnCancel()
        {
            recorder.Stop();
        }

        protected override void OnDispose()
        {
            recorder.Dispose();
            whisper = null;
        }

        private async Task<EngineTranscription> TranscribeAsync(AudioSamples audio)
        {
            WhisperParams param = WhisperParams.GetDefaultParams();
            param.Language = language;
            param.Translate = false;
            param.NoContext = true;
            param.SingleSegment = true;
            if (initialPrompt.Length > 0) param.InitialPrompt = initialPrompt;

            WhisperResult result = await whisper.GetTextAsync(audio.ToArray(), audio.SampleRate, audio.Channels, param);
            if (result == null) return EngineTranscription.Failure("Whisper 변환 실패", audio);
            return EngineTranscription.Success((result.Result ?? "").Trim(), false, audio);
        }

        private async void LoadModel(bool useGpu)
        {
            loading = true;
            double startedAt = Now;
            try
            {
                WhisperContextParams contextParams = WhisperContextParams.GetDefaultParams();
                contextParams.UseGpu = useGpu;
                WhisperWrapper loaded = await WhisperWrapper.InitFromFileAsync(modelPath, contextParams);
                if (IsDisposed) return;

                whisper = loaded;
                ModelLoadMs = Now - startedAt;
                if (whisper == null) loadError = $"모델을 불러오지 못했습니다: {modelPath}";
                else UnityEngine.Debug.Log($"[VoiceSTT] Whisper 모델 {modelName} 불러옴 ({ModelLoadMs:0} ms, GPU={useGpu})");
            }
            catch (Exception e)
            {
                loadError = "모델 로딩 오류: " + e.Message;
            }
            finally
            {
                loading = false;
            }
        }
    }
}
#endif
