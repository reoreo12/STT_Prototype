using System;
using NUnit.Framework;

namespace VoxelMagic.Voice.Tests
{
    public sealed class AudioLevelTests
    {
        [Test]
        public void FromRms_MapsDecibelRange()
        {
            Assert.AreEqual(1f, AudioLevel.FromRms(1.0), 1e-4);
            Assert.AreEqual(0f, AudioLevel.FromRms(0.0), 1e-4);
            Assert.AreEqual(0.5f, AudioLevel.FromRms(Math.Pow(10, -30.0 / 20.0)), 1e-3); // -30 dB
        }

        [Test]
        public void Peak_SilenceIsZero()
        {
            var audio = new AudioSamples(new float[16000], 16000, 1);
            Assert.AreEqual(0f, AudioLevel.Peak(audio), 1e-4);
        }

        [Test]
        public void Peak_FindsLoudPartInsideQuietRecording()
        {
            var data = new float[16000];
            for (int i = 8000; i < 8800; i++) data[i] = (float)Math.Sin(i * 0.1) * 0.5f; // 50ms 정도의 큰 소리
            float peak = AudioLevel.Peak(new AudioSamples(data, 16000, 1));
            Assert.Greater(peak, 0.75f);
        }

        [Test]
        public void Peak_NullOrEmptyIsZero()
        {
            Assert.AreEqual(0f, AudioLevel.Peak(null));
            Assert.AreEqual(0f, AudioLevel.Peak(new AudioSamples(new float[0], 16000, 1)));
        }
    }
}
