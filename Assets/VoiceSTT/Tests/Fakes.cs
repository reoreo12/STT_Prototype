// 테스트에서 시간과 인식 결과를 직접 조종하기 위한 가짜 시계와 가짜 STT 엔진이다.

using System.Threading.Tasks;

namespace VoxelMagic.Voice.Tests
{
    /// <summary>시간을 직접 움직이는 가짜 시계.</summary>
    internal sealed class FakeTimeSource : ITimeSource
    {
        public double NowMs { get; private set; }

        public void Advance(double ms)
        {
            NowMs += ms;
        }
    }

    /// <summary>테스트용 엔진. 결과를 돌려주는 시점을 테스트가 정한다.</summary>
    internal sealed class FakeSpeechToText : SpeechToTextEngineBase
    {
        private TaskCompletionSource<EngineTranscription> pending; // 변환 요청 때 만들고 Complete로 결과를 넣는 대기 작업

        public FakeSpeechToText(ITimeSource time, float maxRecordSeconds = 10f, float transcribeTimeoutSeconds = 8f)
            : base(time, maxRecordSeconds, transcribeTimeoutSeconds)
        {
        }

        public bool Available { get; set; } = true;
        public int CancelCount { get; private set; }

        public override string EngineName => "Fake";

        public void Complete(string text)
        {
            pending.SetResult(EngineTranscription.Success(text, false, null));
        }

        public void Hypothesis(string text)
        {
            ReportHypothesis(text);
        }

        protected override bool CheckAvailability(out string reason)
        {
            reason = Available ? "" : "가짜 엔진 사용 불가";
            return Available;
        }

        protected override void OnStartListening(string deviceName)
        {
            MarkListeningStarted();
        }

        protected override Task<EngineTranscription> OnStopAndTranscribeAsync()
        {
            pending = new TaskCompletionSource<EngineTranscription>();
            return pending.Task;
        }

        protected override void OnCancel()
        {
            CancelCount++;
        }
    }
}
