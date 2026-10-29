using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using VoxelMagic.Voice.Debug;

namespace VoxelMagic.Voice.Tests
{
    public sealed class CsvLogSinkTests
    {
        private string folder;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "VoiceSTT_Tests_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [TestCase("plain", "plain")]
        [TestCase("a,b", "\"a,b\"")]
        [TestCase("say \"hi\"", "\"say \"\"hi\"\"\"")]
        [TestCase("line\nbreak", "\"line\nbreak\"")]
        [TestCase("", "")]
        public void Escape_QuotesWhenNeeded(string input, string expected)
        {
            Assert.AreEqual(expected, CsvLogSink.Escape(input));
        }

        [Test]
        public void File_HasBomHeaderAndRow()
        {
            var result = new SttResult("Fake", "m", "mic", "불꽃아, 가자", "불꽃아가자", new[] { "불꽃" }, "",
                1000, 250, 1250, 1, 0.25, 10, false, null);
            SttAttemptRecord record = SttAttemptRecord.Create(1, new DateTime(2026, 10, 29), result, "불꽃아",
                new SpellMatchResult(0.8f, true, 0.75f), "", "6000.0.38f1", "Editor");

            var sink = new CsvLogSink();
            sink.Open(folder, "test");
            sink.Write(record);
            sink.Dispose();

            byte[] bytes = File.ReadAllBytes(sink.FilePath);
            Assert.AreEqual(new byte[] { 0xEF, 0xBB, 0xBF }, new[] { bytes[0], bytes[1], bytes[2] });

            string[] lines = File.ReadAllLines(sink.FilePath, Encoding.UTF8);
            Assert.AreEqual(2, lines.Length);
            StringAssert.StartsWith("id,timestamp,engine", lines[0]);
            StringAssert.Contains("\"불꽃아, 가자\"", lines[1]);
        }
    }
}
