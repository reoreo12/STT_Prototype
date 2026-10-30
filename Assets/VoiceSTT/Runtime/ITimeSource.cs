// 시간 측정을 시계 인터페이스 뒤로 숨겨, 지연 측정·시간 제한을 가짜 시계로 테스트할 수 있게 한다.

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
        private readonly Stopwatch stopwatch = Stopwatch.StartNew(); // 생성 시점부터 흐르는 기준 시계

        public double NowMs => stopwatch.Elapsed.TotalMilliseconds;
    }
}
