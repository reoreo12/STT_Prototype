using System;
using System.Collections.Generic;
using System.IO;

namespace VoxelMagic.Voice.Debug
{
    /// <summary>
    /// 여러 기록 방식(sink)에 나눠 쓰고, 세션 폴더와 WAV 저장을 관리한다.
    /// 첫 기록이 들어올 때 파일을 연다(빈 파일이 생기지 않게). 꺼져 있으면 아무것도 하지 않는다.
    /// </summary>
    public sealed class SttLogger : IDisposable
    {
        private readonly List<ISttLogSink> sinks;
        private readonly bool saveWav;
        private bool opened;

        public SttLogger(string folderPath, bool enabled, bool saveWav, IEnumerable<ISttLogSink> sinks)
        {
            FolderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
            IsEnabled = enabled;
            this.saveWav = saveWav;
            this.sinks = sinks == null ? new List<ISttLogSink>() : new List<ISttLogSink>(sinks);
            SessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        public bool IsEnabled { get; }
        public string FolderPath { get; }
        public string SessionId { get; }

        /// <summary>시도의 녹음을 WAV로 저장하고 파일 이름을 돌려준다. 저장하지 않으면 빈 문자열.</summary>
        public string SaveWav(int attemptId, AudioSamples audio)
        {
            if (!IsEnabled || !saveWav || audio == null || audio.FrameCount == 0) return "";
            try
            {
                string folder = Path.Combine(FolderPath, $"wav_{SessionId}");
                Directory.CreateDirectory(folder);
                string fileName = $"attempt_{attemptId:000}.wav";
                WavFileWriter.Write(Path.Combine(folder, fileName), audio);
                return $"wav_{SessionId}/{fileName}";
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[VoiceSTT] WAV 저장 실패: {e.Message}");
                return "";
            }
        }

        public void Write(SttAttemptRecord record)
        {
            if (!IsEnabled || record == null) return;
            EnsureOpened();
            foreach (ISttLogSink sink in sinks)
            {
                try
                {
                    sink.Write(record);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[VoiceSTT] 기록 실패 ({sink.GetType().Name}): {e.Message}");
                }
            }
        }

        public void Dispose()
        {
            foreach (ISttLogSink sink in sinks) sink.Dispose();
        }

        private void EnsureOpened()
        {
            if (opened) return;
            opened = true;
            foreach (ISttLogSink sink in sinks)
            {
                try
                {
                    sink.Open(FolderPath, SessionId);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[VoiceSTT] 기록 파일 열기 실패 ({sink.GetType().Name}): {e.Message}");
                }
            }
        }
    }
}
