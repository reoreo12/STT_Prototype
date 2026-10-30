// 엔진 A: Windows 내장 받아쓰기(DictationRecognizer)로 변환하는 구현을 담는다. 모델 파일 없이 OS 음성 인식을 쓴다.

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

namespace VoxelMagic.Voice
{
    /// <summary>
    /// 엔진 A: Windows 내장 받아쓰기(DictationRecognizer).
    /// 언어는 코드로 정할 수 없고 Windows의 음성 언어 설정을 따른다. 마이크는 Windows 기본 장치만 쓴다.
    /// 키를 뗀 뒤 최종 결과를 dictationFinalResultWaitMs까지 기다리고, 오지 않으면 마지막 중간 결과를 쓴다.
    /// </summary>
    public sealed class WindowsDictationStt : SpeechToTextEngineBase
    {
        private readonly int finalResultWaitMs; // 뗀 뒤 최종 결과를 기다리는 시간 (ms)
        private readonly float silenceTimeoutSeconds; // 받아쓰기 무음 제한 시간 (초, 최대 녹음 시간 + 5초)
        private readonly MicrophoneRecorder recorder; // 음량 미터·WAV용 보조 녹음기 (null이면 끔)
        private readonly List<string> segments = new List<string>(); // 이번 녹음에서 확정된 문장들

        private string currentHypothesis = ""; // 아직 확정되지 않은 마지막 중간 결과
        private string lastEngineError = ""; // 받아쓰기가 알린 마지막 오류·종료 이유
        private bool recognizerStopped; // 받아쓰기가 스스로 끝났는지 (완료·오류 이벤트)
        private AudioSamples recordedAudio; // 보조 녹음기로 얻은 샘플 (없으면 null)
        private TaskCompletionSource<EngineTranscription> finishSignal; // 최종 결과 대기 중일 때만 존재하는 완료 신호
        private double finishDeadlineMs; // 최종 결과 대기 종료 시각 (ms)

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private DictationRecognizer recognizer; // Windows 받아쓰기 인식기 (처음 들을 때 생성)
#endif

        /// <param name="recorder">음량 미터·WAV용 보조 녹음기. 마이크 충돌이 있으면 null로 끈다.</param>
        public WindowsDictationStt(ITimeSource time, float maxRecordSeconds, float transcribeTimeoutSeconds,
            int finalResultWaitMs, MicrophoneRecorder recorder)
            : base(time, maxRecordSeconds, transcribeTimeoutSeconds)
        {
            this.finalResultWaitMs = Mathf.Max(0, finalResultWaitMs);
            silenceTimeoutSeconds = maxRecordSeconds + 5f;
            this.recorder = recorder;
        }

        public override string EngineName => "WindowsDictation";
        public override string ModelName => "Windows Speech (OS)";
        public override bool SupportsDeviceSelection => false;
        public override float InputLevel => recorder != null && recorder.IsCapturing ? recorder.Level : -1f;

        protected override string DescribeDevice(string requested)
        {
            return "Windows 기본 마이크";
        }

        protected override bool CheckAvailability(out string reason)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (!PhraseRecognitionSystem.isSupported)
            {
                reason = "이 Windows에서는 음성 인식을 지원하지 않습니다.";
                return false;
            }
            if (Microphone.devices.Length == 0)
            {
                reason = "연결된 마이크가 없습니다.";
                return false;
            }
            reason = "";
            return true;
#else
            reason = "Windows 전용 엔진입니다.";
            return false;
#endif
        }

        protected override void OnStartListening(string deviceName)
        {
            segments.Clear();
            currentHypothesis = "";
            lastEngineError = "";
            recognizerStopped = false;
            recordedAudio = null;
            finishSignal = null;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (PhraseRecognitionSystem.Status == SpeechSystemStatus.Running) PhraseRecognitionSystem.Shutdown();
            EnsureRecognizer();
            if (recognizer.Status != SpeechSystemStatus.Running) recognizer.Start();
#endif
            recorder?.Start(deviceName);
        }

        protected override Task<EngineTranscription> OnStopAndTranscribeAsync()
        {
            recordedAudio = recorder?.Stop();
            finishSignal = new TaskCompletionSource<EngineTranscription>();
            finishDeadlineMs = Now + finalResultWaitMs;

            Task<EngineTranscription> task = finishSignal.Task;
            bool alreadyFinal = segments.Count > 0 && currentHypothesis.Length == 0;
            if (alreadyFinal || recognizerStopped) CompleteTranscription();
            return task;
        }

        protected override void OnTick()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (State == SttState.Listening && recognizer != null && recognizer.Status == SpeechSystemStatus.Running)
            {
                MarkListeningStarted();
            }
#endif
            recorder?.Tick();
            if (finishSignal != null && Now >= finishDeadlineMs) CompleteTranscription();
        }

        protected override void OnCancel()
        {
            finishSignal = null;
            StopRecognizer();
            recorder?.Stop();
        }

        protected override void OnDispose()
        {
            OnCancel();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (recognizer != null)
            {
                recognizer.DictationHypothesis -= OnDictationHypothesis;
                recognizer.DictationResult -= OnDictationResult;
                recognizer.DictationComplete -= OnDictationComplete;
                recognizer.DictationError -= OnDictationError;
                recognizer.Dispose();
                recognizer = null;
            }
#endif
            recorder?.Dispose();
        }

        private void CompleteTranscription()
        {
            TaskCompletionSource<EngineTranscription> signal = finishSignal;
            if (signal == null) return;
            finishSignal = null;
            StopRecognizer();

            var parts = new List<string>(segments);
            bool usedFallback = false;
            if (currentHypothesis.Length > 0)
            {
                parts.Add(currentHypothesis);
                usedFallback = true;
            }

            string text = string.Join(" ", parts).Trim();
            signal.TrySetResult(text.Length > 0
                ? EngineTranscription.Success(text, usedFallback, recordedAudio)
                : EngineTranscription.Failure(lastEngineError.Length > 0 ? lastEngineError : "인식 결과 없음", recordedAudio));
        }

        private bool IsActive => State == SttState.Listening || State == SttState.Transcribing;

        private string JoinWith(string tail)
        {
            if (string.IsNullOrEmpty(tail)) return string.Join(" ", segments);
            return segments.Count == 0 ? tail : string.Join(" ", segments) + " " + tail;
        }

        private void StopRecognizer()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (recognizer != null && recognizer.Status == SpeechSystemStatus.Running) recognizer.Stop();
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private void EnsureRecognizer()
        {
            if (recognizer != null) return;

            recognizer = new DictationRecognizer(ConfidenceLevel.Low, DictationTopicConstraint.Dictation)
            {
                InitialSilenceTimeoutSeconds = silenceTimeoutSeconds,
                AutoSilenceTimeoutSeconds = silenceTimeoutSeconds,
            };
            recognizer.DictationHypothesis += OnDictationHypothesis;
            recognizer.DictationResult += OnDictationResult;
            recognizer.DictationComplete += OnDictationComplete;
            recognizer.DictationError += OnDictationError;
        }

        private void OnDictationHypothesis(string text)
        {
            if (!IsActive) return;
            MarkListeningStarted();
            currentHypothesis = text ?? "";
            ReportHypothesis(JoinWith(currentHypothesis));
        }

        private void OnDictationResult(string text, ConfidenceLevel confidence)
        {
            if (!IsActive) return;
            if (!string.IsNullOrWhiteSpace(text)) segments.Add(text.Trim());
            currentHypothesis = "";
            ReportHypothesis(JoinWith(""));
            if (finishSignal != null) CompleteTranscription();
        }

        private void OnDictationComplete(DictationCompletionCause cause)
        {
            if (!IsActive) return;
            recognizerStopped = true;
            if (cause != DictationCompletionCause.Complete) lastEngineError = DescribeCause(cause);
            if (finishSignal != null) CompleteTranscription();
        }

        private void OnDictationError(string error, int hresult)
        {
            if (!IsActive) return;
            recognizerStopped = true;
            lastEngineError = $"{error} (HRESULT 0x{hresult:X8})";
            if (finishSignal != null) CompleteTranscription();
        }

        private static string DescribeCause(DictationCompletionCause cause)
        {
            switch (cause)
            {
                case DictationCompletionCause.TimeoutExceeded: return "무음 시간 초과로 받아쓰기가 끝남";
                case DictationCompletionCause.PauseLimitExceeded: return "일시 정지 한도 초과";
                case DictationCompletionCause.NetworkFailure: return "네트워크 오류 (Windows 온라인 음성 인식 필요)";
                case DictationCompletionCause.AudioQualityFailure: return "음성 품질 문제 (너무 작거나 시끄러움)";
                case DictationCompletionCause.MicrophoneUnavailable: return "마이크를 사용할 수 없음";
                case DictationCompletionCause.Canceled: return "받아쓰기가 취소됨";
                default: return "알 수 없는 받아쓰기 오류 (" + cause + ")";
            }
        }
#endif
    }
}
