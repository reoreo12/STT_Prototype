// Unity Microphone 녹음을 감싼다. 엔진 B의 녹음과 엔진 A의 보조 녹음(음량 미터·WAV)에 함께 쓴다.

using System;
using UnityEngine;

namespace VoxelMagic.Voice
{
    /// <summary>
    /// Unity Microphone으로 녹음하고 입력 음량을 계산한다. 엔진이 상속 대신 이 객체를 가지고 쓴다(조합).
    /// 반복(loop) 녹음을 링 버퍼처럼 켜 두고(Open), 필요한 구간만 잘라 쓴다(BeginCapture/EndCapture).
    /// 마이크를 미리 열어 두면 누르는 순간의 시작 지연이 없고, 누르기 직전(preRoll)부터 잘라 쓸 수 있다.
    /// Tick()은 매 프레임 호출해야 한다 (위치 추적, 음량 계산).
    /// </summary>
    public sealed class MicrophoneRecorder : IDisposable
    {
        private const int PreferredSampleRate = 16000; // 요청 샘플레이트 (Hz). 장치 지원 범위로 잘린다
        private const int LevelWindow = 1024; // 음량 계산에 쓰는 최근 샘플 수 (전 채널 합계)

        private readonly int clipSeconds; // 링 버퍼 클립 길이 (초, 최대 캡처 길이 + 2초 여유)
        private readonly float[] levelBuffer = new float[LevelWindow]; // 음량 계산용 버퍼 (현재 코드에서는 쓰이지 않음)
        private AudioClip clip; // 반복 녹음 중인 링 버퍼 클립. null이면 마이크가 닫힌 상태
        private string device; // 열린 마이크 장치 이름 (null이면 기본 장치)
        private int lastPosition; // 마지막으로 확인한 클립 내 녹음 위치 (프레임)
        private long writtenFrames; // 마이크를 연 뒤 누적 기록된 프레임 수 (링 순환과 무관)
        private long captureStartFrame; // 캡처 시작 지점 (writtenFrames 기준, preRoll 포함)

        /// <param name="maxCaptureSeconds">한 번에 잘라 쓸 수 있는 최대 길이(녹음 + preRoll + postRoll).</param>
        public MicrophoneRecorder(float maxCaptureSeconds)
        {
            clipSeconds = Mathf.CeilToInt(maxCaptureSeconds) + 2;
        }

        public bool IsOpen => clip != null;
        public bool IsCapturing { get; private set; }

        /// <summary>실제 소리(0이 아닌 샘플)가 들어오기 시작했는지. 장치가 막 열렸을 때는 한동안 0만 들어온다.</summary>
        public bool IsLive { get; private set; }

        /// <summary>최근 입력 음량 0~1 (AudioLevel 기준).</summary>
        public float Level { get; private set; }

        public string Device => device ?? "";
        public string LastError { get; private set; } = "";

        /// <summary>반복 녹음을 켠다. 같은 장치가 이미 열려 있으면 그대로 둔다.</summary>
        public bool Open(string deviceName)
        {
            string wanted = string.IsNullOrEmpty(deviceName) ? null : deviceName;
            if (clip != null && wanted == device && Microphone.IsRecording(device)) return true;

            Close();
            LastError = "";
            if (Microphone.devices.Length == 0)
            {
                LastError = "연결된 마이크가 없습니다.";
                return false;
            }

            Microphone.GetDeviceCaps(wanted, out int minRate, out int maxRate);
            int rate = minRate == 0 && maxRate == 0 ? PreferredSampleRate : Mathf.Clamp(PreferredSampleRate, minRate, maxRate);
            clip = Microphone.Start(wanted, true, clipSeconds, rate);
            if (clip == null)
            {
                LastError = "Microphone.Start 실패";
                return false;
            }

            device = wanted;
            lastPosition = 0;
            writtenFrames = 0;
            return true;
        }

        public void Close()
        {
            IsCapturing = false;
            IsLive = false;
            Level = 0f;
            if (clip == null) return;
            Microphone.End(device);
            DestroyClip();
        }

        /// <summary>지금부터 캡처를 시작한다. 이미 녹음된 것 중 preRollSeconds만큼을 앞에 포함한다.</summary>
        public bool BeginCapture(double preRollSeconds)
        {
            if (clip == null) return false;
            UpdatePosition();
            long preRoll = Math.Min(writtenFrames, (long)(Math.Max(0, preRollSeconds) * clip.frequency));
            captureStartFrame = writtenFrames - preRoll;
            IsCapturing = true;
            Level = 0f;
            return true;
        }

        /// <summary>캡처를 끝내고 캡처 구간의 샘플을 돌려준다. 마이크는 열린 채로 둔다. 캡처 중이 아니면 null.</summary>
        public AudioSamples EndCapture()
        {
            if (!IsCapturing || clip == null)
            {
                IsCapturing = false;
                return null;
            }

            UpdatePosition();
            IsCapturing = false;
            Level = 0f;

            long frames = Math.Min(writtenFrames - captureStartFrame, clip.samples);
            if (frames <= 0) return null;
            return new AudioSamples(ReadRing(lastPosition - (int)frames, (int)frames), clip.frequency, clip.channels);
        }

        /// <summary>한 번만 녹음할 때: 열고 바로 캡처를 시작한다.</summary>
        public bool Start(string deviceName)
        {
            return Open(deviceName) && BeginCapture(0);
        }

        /// <summary>한 번만 녹음할 때: 캡처를 끝내고 마이크를 닫는다.</summary>
        public AudioSamples Stop()
        {
            AudioSamples audio = EndCapture();
            Close();
            return audio;
        }

        public void Tick()
        {
            if (clip == null) return;
            UpdatePosition();

            int frames = LevelWindow / clip.channels;
            if (writtenFrames < frames) return;

            float[] window = ReadRing(lastPosition - frames, frames);
            double sum = 0;
            for (int i = 0; i < window.Length; i++)
            {
                sum += window[i] * window[i];
                if (!IsLive && window[i] != 0f) IsLive = true;
            }
            Level = IsCapturing ? AudioLevel.FromRms(Math.Sqrt(sum / window.Length)) : 0f;
        }

        public void Dispose()
        {
            Close();
        }

        private void UpdatePosition()
        {
            int position = Microphone.GetPosition(device);
            int delta = position - lastPosition;
            if (delta < 0) delta += clip.samples;
            writtenFrames += delta;
            lastPosition = position;
        }

        /// <summary>링 버퍼에서 start(음수 가능)부터 frames 프레임을 읽는다. 끝을 넘으면 처음부터 이어 읽는다.</summary>
        private float[] ReadRing(int start, int frames)
        {
            int length = clip.samples;
            int channels = clip.channels;
            start = ((start % length) + length) % length;

            var result = new float[frames * channels];
            int first = Math.Min(frames, length - start);
            var head = first == frames ? result : new float[first * channels];
            clip.GetData(head, start);
            if (first < frames)
            {
                Array.Copy(head, result, head.Length);
                var tail = new float[(frames - first) * channels];
                clip.GetData(tail, 0);
                Array.Copy(tail, 0, result, head.Length, tail.Length);
            }
            return result;
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
