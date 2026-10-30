// 음량 미터와 무음 판정(엔진 B)이 같은 dB 기준을 쓰도록 음량 계산을 한곳에 모은다.

using System;
using System.Collections.Generic;

namespace VoxelMagic.Voice
{
    /// <summary>
    /// 음량 계산. 0~1 값은 -60 dB ~ 0 dB를 선형으로 옮긴 것이다 (예: 0.25 ≈ -45 dB).
    /// 음량 미터와 무음 판정이 같은 기준을 쓴다.
    /// </summary>
    public static class AudioLevel
    {
        private const float FloorDb = -60f; // 음량 0에 해당하는 하한 (dB)

        public static float FromRms(double rms)
        {
            double db = 20.0 * Math.Log10(Math.Max(rms, 1e-5));
            return (float)Math.Max(0.0, Math.Min(1.0, (db - FloorDb) / -FloorDb));
        }

        /// <summary>구간(window) RMS 중 가장 큰 값을 0~1로 돌려준다. 샘플이 없으면 0.</summary>
        public static float Peak(AudioSamples audio, double windowSeconds = 0.05)
        {
            if (audio == null || audio.FrameCount == 0) return 0f;

            IReadOnlyList<float> data = audio.Data;
            int window = Math.Max(1, (int)(audio.SampleRate * windowSeconds)) * audio.Channels;
            double peakRms = 0;
            for (int start = 0; start < data.Count; start += window)
            {
                int end = Math.Min(data.Count, start + window);
                double sum = 0;
                for (int i = start; i < end; i++) sum += data[i] * data[i];
                peakRms = Math.Max(peakRms, Math.Sqrt(sum / (end - start)));
            }
            return FromRms(peakRms);
        }
    }
}
