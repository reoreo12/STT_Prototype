using System;
using UnityEngine;

namespace VoxelMagic.Voice
{
    /// <summary>
    /// Unity Microphone으로 녹음하고 입력 음량을 계산한다.
    /// 엔진이 상속 대신 이 객체를 가지고 쓴다(조합).
    /// </summary>
    public sealed class MicrophoneRecorder : IDisposable
    {
        private const int PreferredSampleRate = 16000;
        private const int LevelWindow = 1024;

        private readonly int clipSeconds;
        private readonly float[] levelBuffer = new float[LevelWindow];
        private AudioClip clip;
        private string device;

        public MicrophoneRecorder(float maxRecordSeconds)
        {
            clipSeconds = Mathf.CeilToInt(maxRecordSeconds) + 1;
        }

        public bool IsRecording { get; private set; }

        /// <summary>최근 입력 음량 0~1 (AudioLevel 기준).</summary>
        public float Level { get; private set; }

        public string LastError { get; private set; } = "";

        /// <summary>지금까지 녹음된 프레임 수. 0보다 크면 실제로 녹음이 시작된 것이다.</summary>
        public int RecordedFrames => IsRecording ? Microphone.GetPosition(device) : 0;

        public bool Start(string deviceName)
        {
            Discard();
            LastError = "";
            Level = 0f;

            if (Microphone.devices.Length == 0)
            {
                LastError = "연결된 마이크가 없습니다.";
                return false;
            }

            device = string.IsNullOrEmpty(deviceName) ? null : deviceName;
            Microphone.GetDeviceCaps(device, out int minRate, out int maxRate);
            int rate = minRate == 0 && maxRate == 0 ? PreferredSampleRate : Mathf.Clamp(PreferredSampleRate, minRate, maxRate);

            clip = Microphone.Start(device, false, clipSeconds, rate);
            IsRecording = clip != null;
            if (!IsRecording) LastError = "Microphone.Start 실패";
            return IsRecording;
        }

        public void Tick()
        {
            if (!IsRecording || clip == null) return;

            int frames = LevelWindow / clip.channels;
            int start = Microphone.GetPosition(device) - frames;
            if (start < 0)
            {
                Level = 0f;
                return;
            }

            clip.GetData(levelBuffer, start);
            double sum = 0;
            for (int i = 0; i < levelBuffer.Length; i++) sum += levelBuffer[i] * levelBuffer[i];
            Level = AudioLevel.FromRms(Math.Sqrt(sum / levelBuffer.Length));
        }

        /// <summary>녹음을 멈추고 녹음된 부분만 돌려준다. 녹음 중이 아니면 null.</summary>
        public AudioSamples Stop()
        {
            if (!IsRecording || clip == null)
            {
                Discard();
                return null;
            }

            bool reachedEnd = !Microphone.IsRecording(device);
            int frames = reachedEnd ? clip.samples : Microphone.GetPosition(device);
            Microphone.End(device);
            IsRecording = false;
            Level = 0f;

            AudioSamples result = null;
            if (frames > 0)
            {
                var data = new float[frames * clip.channels];
                clip.GetData(data, 0);
                result = new AudioSamples(data, clip.frequency, clip.channels);
            }

            DestroyClip();
            return result;
        }

        public void Dispose()
        {
            Discard();
        }

        private void Discard()
        {
            if (IsRecording) Microphone.End(device);
            IsRecording = false;
            Level = 0f;
            DestroyClip();
        }

        private void DestroyClip()
        {
            if (clip == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(clip);
            else UnityEngine.Object.DestroyImmediate(clip);
            clip = null;
        }
    }
}
