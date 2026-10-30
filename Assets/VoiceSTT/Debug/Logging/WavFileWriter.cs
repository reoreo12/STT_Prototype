// 시도별 녹음을 표준 WAV 파일로 저장해 인식 결과와 실제 음성을 나중에 대조해 볼 수 있게 한다.

using System;
using System.IO;
using System.Text;

namespace VoxelMagic.Voice.Debug
{
    /// <summary>float 샘플을 16bit PCM WAV 파일로 쓴다.</summary>
    public static class WavFileWriter
    {
        public static void Write(string path, AudioSamples audio)
        {
            if (audio == null) throw new ArgumentNullException(nameof(audio));

            const short bitsPerSample = 16;
            int sampleCount = audio.Data.Count;
            int dataBytes = sampleCount * (bitsPerSample / 8);
            int byteRate = audio.SampleRate * audio.Channels * (bitsPerSample / 8);

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + dataBytes);
                w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt "));
                w.Write(16);
                w.Write((short)1);
                w.Write((short)audio.Channels);
                w.Write(audio.SampleRate);
                w.Write(byteRate);
                w.Write((short)(audio.Channels * (bitsPerSample / 8)));
                w.Write(bitsPerSample);
                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write(dataBytes);

                for (int i = 0; i < sampleCount; i++)
                {
                    float clamped = Math.Max(-1f, Math.Min(1f, audio.Data[i]));
                    w.Write((short)(clamped * short.MaxValue));
                }
            }
        }
    }
}
