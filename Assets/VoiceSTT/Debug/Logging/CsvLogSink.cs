using System.IO;
using System.Linq;
using System.Text;

namespace VoxelMagic.Voice.Debug
{
    /// <summary>CSV 기록. 엑셀에서 한글이 깨지지 않도록 UTF-8 BOM을 넣는다.</summary>
    public sealed class CsvLogSink : ISttLogSink
    {
        private StreamWriter writer;

        public string FilePath { get; private set; } = "";

        public void Open(string folderPath, string sessionId)
        {
            Dispose();
            Directory.CreateDirectory(folderPath);
            FilePath = Path.Combine(folderPath, $"stt_{sessionId}.csv");
            writer = new StreamWriter(FilePath, false, new UTF8Encoding(true)) { AutoFlush = true };
            WriteRow(SttAttemptRecord.CsvHeader);
        }

        public void Write(SttAttemptRecord record)
        {
            if (writer == null) throw new System.InvalidOperationException("Open을 먼저 호출해야 합니다.");
            WriteRow(record.ToCsvFields());
        }

        public void Dispose()
        {
            writer?.Dispose();
            writer = null;
        }

        /// <summary>쉼표·따옴표·줄바꿈이 있으면 따옴표로 감싸고 따옴표는 두 번 쓴다.</summary>
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            bool needsQuote = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            return needsQuote ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }

        private void WriteRow(string[] fields)
        {
            writer.WriteLine(string.Join(",", fields.Select(Escape)));
        }
    }
}
