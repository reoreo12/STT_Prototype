// STT 시도 기록을 스크립트로 분석하기 쉬운 JSONL 파일로 남긴다.

using System.IO;
using System.Text;
using UnityEngine;

namespace VoxelMagic.Voice.Debug
{
    /// <summary>JSONL 기록. 한 줄에 시도 하나.</summary>
    public sealed class JsonlLogSink : ISttLogSink
    {
        private StreamWriter writer; // 열린 JSONL 파일 쓰기 스트림(BOM 없는 UTF-8). Open 전·Dispose 후에는 null

        public string FilePath { get; private set; } = "";

        public void Open(string folderPath, string sessionId)
        {
            Dispose();
            Directory.CreateDirectory(folderPath);
            FilePath = Path.Combine(folderPath, $"stt_{sessionId}.jsonl");
            writer = new StreamWriter(FilePath, false, new UTF8Encoding(false)) { AutoFlush = true };
        }

        public void Write(SttAttemptRecord record)
        {
            if (writer == null) throw new System.InvalidOperationException("Open을 먼저 호출해야 합니다.");
            writer.WriteLine(JsonUtility.ToJson(record));
        }

        public void Dispose()
        {
            writer?.Dispose();
            writer = null;
        }
    }
}
