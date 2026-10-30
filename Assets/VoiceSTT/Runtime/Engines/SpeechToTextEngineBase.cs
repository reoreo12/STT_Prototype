// 엔진 A/B가 공유하는 상태 전이·시간 측정·시간 제한 흐름을 담는다. 새 엔진은 이것을 상속해 엔진별 부분만 구현한다.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VoxelMagic.Voice
{
    /// <summary>
    /// 모든 STT 엔진의 공통 흐름: 상태 전이, 시간 측정, 시간 제한, 이벤트 발생.
    /// 엔진별로 다른 부분(시작, 텍스트 얻기, 가용성 검사)만 하위 클래스가 구현한다.
    /// 시간 제한은 Task.Delay 대신 Tick()에서 검사하므로 가짜 시계로 테스트할 수 있다.
    /// </summary>
    public abstract class SpeechToTextEngineBase : ISpeechToText
    {
        private readonly ITimeSource time; // 시간 측정·시간 제한에 쓰는 시계
        private readonly double maxRecordMs; // 최대 녹음 시간 (ms). 넘으면 자동으로 변환 시작
        private readonly double transcribeTimeoutMs; // 뗀 뒤 변환 제한 시간 (ms). 넘으면 실패 처리
        private readonly List<string> hypotheses = new List<string>(); // 이번 녹음에서 받은 중간 결과 (연속 중복 제외)

        private SttState state = SttState.Idle; // 현재 진행 단계
        private bool availabilityChecked; // 가용성을 한 번이라도 검사했는지
        private bool isAvailable; // 마지막 가용성 검사 결과
        private string unavailableReason = ""; // 사용 불가 이유 (사용 가능하면 빈 문자열)
        private string requestedDevice = ""; // StartListening에 받은 마이크 장치 이름
        private double pressedAtMs; // 누른 시각 (ms)
        private double releasedAtMs = -1; // 뗀 시각 (ms). 아직이면 -1
        private double listeningStartedAtMs = -1; // 엔진이 실제로 듣기 시작한 시각 (ms). 모르면 -1
        private TaskCompletionSource<string> abortSignal; // 변환 중단 신호 (시간 초과·취소 이유를 담는다)
        private Task<SttResult> pendingTranscription; // 진행 중인 변환 작업 (중복 호출 시 같은 작업을 돌려준다)
        private bool disposed; // Dispose 호출 여부

        protected SpeechToTextEngineBase(ITimeSource time, float maxRecordSeconds, float transcribeTimeoutSeconds)
        {
            this.time = time ?? throw new ArgumentNullException(nameof(time));
            maxRecordMs = maxRecordSeconds * 1000.0;
            transcribeTimeoutMs = transcribeTimeoutSeconds * 1000.0;
        }

        public event Action<SttState> StateChanged;
        public event Action<string> HypothesisReceived;
        public event Action<SttResult> Completed;

        public abstract string EngineName { get; }
        public virtual string ModelName => "";
        public virtual string ComputeBackend => "";
        public virtual bool SupportsDeviceSelection => true;
        public virtual float InputLevel => -1f;

        public SttState State => state;
        public SttResult LastResult { get; private set; }

        public bool IsAvailable
        {
            get
            {
                if (!availabilityChecked) RefreshAvailability();
                return isAvailable;
            }
        }

        public string UnavailableReason
        {
            get
            {
                if (!availabilityChecked) RefreshAvailability();
                return unavailableReason;
            }
        }

        public double ListeningElapsedMs => state == SttState.Listening ? Now - pressedAtMs : 0;
        public double TranscribingElapsedMs => state == SttState.Transcribing ? Now - releasedAtMs : 0;

        protected double Now => time.NowMs;
        protected bool IsDisposed => disposed;
        protected string LastHypothesis => hypotheses.Count > 0 ? hypotheses[hypotheses.Count - 1] : "";

        public void RefreshAvailability()
        {
            availabilityChecked = true;
            isAvailable = CheckAvailability(out string reason);
            unavailableReason = isAvailable ? "" : (reason ?? "");
        }

        public void SetPreferredDevice(string deviceName)
        {
            if (disposed) return;
            OnPreferredDeviceChanged(deviceName ?? "");
        }

        public void StartListening(string deviceName)
        {
            ThrowIfDisposed();
            if (state == SttState.Listening || state == SttState.Transcribing) return;

            hypotheses.Clear();
            requestedDevice = deviceName ?? "";
            pressedAtMs = Now;
            releasedAtMs = -1;
            listeningStartedAtMs = -1;
            pendingTranscription = null;

            RefreshAvailability();
            SetState(SttState.Listening);

            if (!isAvailable)
            {
                Finish(EngineTranscription.Failure("사용 불가: " + unavailableReason, null));
                return;
            }

            try
            {
                OnStartListening(requestedDevice);
            }
            catch (Exception e)
            {
                SafeCancel();
                Finish(EngineTranscription.Failure("시작 실패: " + e.Message, null));
            }
        }

        public Task<SttResult> StopAndTranscribeAsync()
        {
            ThrowIfDisposed();
            if (state != SttState.Listening) return pendingTranscription ?? Task.FromResult(LastResult);

            pendingTranscription = TranscribeAsync();
            return pendingTranscription;
        }

        public void Cancel()
        {
            if (state == SttState.Listening)
            {
                SafeCancel();
                SetState(SttState.Idle);
            }
            else if (state == SttState.Transcribing)
            {
                abortSignal?.TrySetResult("취소됨");
            }
        }

        public void Tick()
        {
            if (disposed) return;

            if (state == SttState.Listening && Now - pressedAtMs >= maxRecordMs)
            {
                _ = StopAndTranscribeAsync();
            }
            else if (state == SttState.Transcribing && Now - releasedAtMs >= transcribeTimeoutMs)
            {
                abortSignal?.TrySetResult($"변환 시간 초과 ({transcribeTimeoutMs / 1000.0:0.#}초)");
            }

            OnTick();
        }

        public void Dispose()
        {
            if (disposed) return;
            Cancel();
            disposed = true;
            OnDispose();
        }

        /// <summary>엔진이 실제로 듣기 시작했을 때 호출한다. engineStartMs 측정에 쓰인다.</summary>
        protected void MarkListeningStarted()
        {
            if (state == SttState.Listening && listeningStartedAtMs < 0) listeningStartedAtMs = Now;
        }

        /// <summary>중간 결과를 알린다. 같은 문장이 연속으로 오면 무시한다.</summary>
        protected void ReportHypothesis(string text)
        {
            if (state != SttState.Listening && state != SttState.Transcribing) return;
            if (string.IsNullOrWhiteSpace(text)) return;
            if (hypotheses.Count > 0 && hypotheses[hypotheses.Count - 1] == text) return;

            hypotheses.Add(text);
            HypothesisReceived?.Invoke(text);
        }

        protected abstract bool CheckAvailability(out string reason);
        protected abstract void OnStartListening(string deviceName);

        /// <summary>녹음을 멈추고 텍스트를 얻는다. 시간 제한·취소는 기반 클래스가 처리한다.</summary>
        protected abstract Task<EngineTranscription> OnStopAndTranscribeAsync();

        protected virtual void OnPreferredDeviceChanged(string deviceName) { }
        protected virtual void OnCancel() { }
        protected virtual void OnTick() { }
        protected virtual void OnDispose() { }

        protected virtual string DescribeDevice(string requested)
        {
            return string.IsNullOrEmpty(requested) ? "(기본 장치)" : requested;
        }

        private async Task<SttResult> TranscribeAsync()
        {
            releasedAtMs = Now;
            abortSignal = new TaskCompletionSource<string>();
            SetState(SttState.Transcribing);

            EngineTranscription outcome;
            try
            {
                Task<EngineTranscription> work = OnStopAndTranscribeAsync();
                Task finished = await Task.WhenAny(work, abortSignal.Task);
                if (finished == work)
                {
                    outcome = await work;
                }
                else
                {
                    string reason = await abortSignal.Task;
                    if (!disposed) SafeCancel();
                    outcome = EngineTranscription.Failure(reason, null);
                }
            }
            catch (Exception e)
            {
                outcome = EngineTranscription.Failure(e.Message, null);
            }

            return Finish(outcome);
        }

        private SttResult Finish(EngineTranscription outcome)
        {
            double completedAtMs = Now;
            if (releasedAtMs < 0) releasedAtMs = completedAtMs;

            string raw = outcome.Text;
            string normalized = TextNormalizer.Normalize(raw);
            string error = outcome.Error;
            if (error.Length == 0 && normalized.Length == 0) error = "인식 결과 없음";

            double recordMs = releasedAtMs - pressedAtMs;
            double transcribeMs = completedAtMs - releasedAtMs;
            double totalMs = completedAtMs - pressedAtMs;
            double audioSec = outcome.Audio != null && outcome.Audio.Seconds > 0 ? outcome.Audio.Seconds : recordMs / 1000.0;
            double realtimeFactor = audioSec > 0 ? transcribeMs / (audioSec * 1000.0) : 0;
            double engineStartMs = listeningStartedAtMs >= 0 ? listeningStartedAtMs - pressedAtMs : -1;

            var result = new SttResult(
                EngineName, ModelName, ComputeBackend, DescribeDevice(requestedDevice),
                raw, normalized, hypotheses, error,
                recordMs, transcribeMs, totalMs, audioSec, realtimeFactor, engineStartMs,
                outcome.UsedHypothesisFallback, outcome.Audio);

            LastResult = result;
            abortSignal = null;
            SetState(result.Succeeded ? SttState.Completed : SttState.Failed);
            Completed?.Invoke(result);
            return result;
        }

        private void SafeCancel()
        {
            try
            {
                OnCancel();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[VoiceSTT] {EngineName} 취소 중 오류: {e.Message}");
            }
        }

        private void SetState(SttState next)
        {
            if (state == next) return;
            state = next;
            StateChanged?.Invoke(next);
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(EngineName);
        }
    }
}
