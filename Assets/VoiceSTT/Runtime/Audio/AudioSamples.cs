using System;
using System.Collections.Generic;

namespace VoxelMagic.Voice
{
    /// <summary>녹음된 PCM 샘플 (인터리브드 float, -1~1). 만든 뒤에는 바뀌지 않는다.</summary>
    public sealed class AudioSamples
    {
        private readonly float[] data;

        public AudioSamples(float[] data, int sampleRate, int channels)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (channels <= 0) throw new ArgumentOutOfRangeException(nameof(channels));
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            SampleRate = sampleRate;
            Channels = channels;
        }

        public IReadOnlyList<float> Data => data;
        public int SampleRate { get; }
        public int Channels { get; }
        public int FrameCount => data.Length / Channels;
        public double Seconds => FrameCount / (double)SampleRate;

        /// <summary>샘플 배열의 복사본. 원본은 바뀌지 않는다.</summary>
        public float[] ToArray()
        {
            return (float[])data.Clone();
        }
    }
}
