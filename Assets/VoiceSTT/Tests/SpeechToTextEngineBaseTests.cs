using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace VoxelMagic.Voice.Tests
{
    /// <summary>
    /// 기반 클래스의 상태 전이·시간 측정·시간 제한을 가짜 시계와 가짜 엔진으로 검증한다.
    /// 동기화 컨텍스트를 비워 두면 await 이후 코드가 결과를 넣는 즉시 같은 호출 안에서 실행된다.
    /// </summary>
    public sealed class SpeechToTextEngineBaseTests
    {
        private SynchronizationContext previousContext;
        private FakeTimeSource time;
        private FakeSpeechToText engine;
        private List<SttState> states;
        private SttResult completed;

        [SetUp]
        public void SetUp()
        {
            previousContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);

            time = new FakeTimeSource();
            engine = new FakeSpeechToText(time, maxRecordSeconds: 5f, transcribeTimeoutSeconds: 2f);
            states = new List<SttState>();
            completed = null;
            engine.StateChanged += states.Add;
            engine.Completed += r => completed = r;
        }

        [TearDown]
        public void TearDown()
        {
            engine.Dispose();
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }

        [Test]
        public void FullFlow_MeasuresTimes()
        {
            engine.StartListening("");
            Assert.AreEqual(SttState.Listening, engine.State);

            time.Advance(1000);
            engine.Hypothesis("불꽃");
            engine.StopAndTranscribeAsync();
            Assert.AreEqual(SttState.Transcribing, engine.State);

            time.Advance(250);
            engine.Complete("불꽃아.");

            Assert.AreEqual(SttState.Completed, engine.State);
            CollectionAssert.AreEqual(new[] { SttState.Listening, SttState.Transcribing, SttState.Completed }, states);
            Assert.IsNotNull(completed);
            Assert.IsTrue(completed.Succeeded);
            Assert.AreEqual("불꽃아.", completed.RawText);
            Assert.AreEqual("불꽃아", completed.NormalizedText);
            Assert.AreEqual(1000, completed.RecordMs, 1e-6);
            Assert.AreEqual(250, completed.TranscribeMs, 1e-6);
            Assert.AreEqual(1250, completed.TotalLatencyMs, 1e-6);
            Assert.AreEqual(1.0, completed.AudioSec, 1e-6);
            Assert.AreEqual(0.25, completed.RealtimeFactor, 1e-6);
            Assert.AreEqual(0, completed.EngineStartMs, 1e-6);
            CollectionAssert.AreEqual(new[] { "불꽃" }, completed.Hypotheses);
        }

        [Test]
        public void EmptyText_Fails()
        {
            engine.StartListening("");
            engine.StopAndTranscribeAsync();
            engine.Complete("  ...  ");

            Assert.AreEqual(SttState.Failed, engine.State);
            Assert.AreEqual("인식 결과 없음", completed.Error);
        }

        [Test]
        public void TranscribeTimeout_FailsOnTick()
        {
            engine.StartListening("");
            engine.StopAndTranscribeAsync();

            time.Advance(1999);
            engine.Tick();
            Assert.AreEqual(SttState.Transcribing, engine.State);

            time.Advance(1);
            engine.Tick();
            Assert.AreEqual(SttState.Failed, engine.State);
            StringAssert.Contains("시간 초과", completed.Error);
            Assert.AreEqual(1, engine.CancelCount);
        }

        [Test]
        public void MaxRecordTime_StopsAutomatically()
        {
            engine.StartListening("");
            time.Advance(5000);
            engine.Tick();
            Assert.AreEqual(SttState.Transcribing, engine.State);
        }

        [Test]
        public void Unavailable_FailsImmediately()
        {
            engine.Available = false;
            engine.StartListening("");

            Assert.IsFalse(engine.IsAvailable);
            Assert.AreEqual(SttState.Failed, engine.State);
            StringAssert.Contains("사용 불가", completed.Error);
        }

        [Test]
        public void CancelWhileListening_ReturnsToIdle()
        {
            engine.StartListening("");
            engine.Cancel();

            Assert.AreEqual(SttState.Idle, engine.State);
            Assert.IsNull(completed);
        }

        [Test]
        public void SecondAttempt_ResetsHypotheses()
        {
            engine.StartListening("");
            engine.Hypothesis("빛");
            engine.StopAndTranscribeAsync();
            engine.Complete("빛나라");

            engine.StartListening("");
            engine.StopAndTranscribeAsync();
            engine.Complete("찾아라");

            Assert.AreEqual(0, completed.Hypotheses.Count);
            Assert.AreEqual("찾아라", completed.RawText);
        }
    }
}
