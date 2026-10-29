using System.Diagnostics;

namespace VoxelMagic.Voice
{
    /// <summary>현재 시각(ms)을 제공한다. 테스트에서는 가짜 시계로 바꿔 시간 측정을 검증한다.</summary>
    public interface ITimeSource
    {
        double NowMs { get; }
    }

    /// <summary>Stopwatch 기반의 실제 시계.</summary>
    public sealed class StopwatchTimeSource : ITimeSource
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        public double NowMs => stopwatch.Elapsed.TotalMilliseconds;
    }
}
