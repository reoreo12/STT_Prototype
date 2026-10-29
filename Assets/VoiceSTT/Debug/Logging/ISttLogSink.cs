using System;

namespace VoxelMagic.Voice.Debug
{
    /// <summary>기록 저장 방식. 세션마다 Open으로 파일을 하나 만들고 시도마다 Write한다.</summary>
    public interface ISttLogSink : IDisposable
    {
        /// <summary>열린 파일 경로. 열기 전에는 빈 문자열.</summary>
        string FilePath { get; }

        void Open(string folderPath, string sessionId);
        void Write(SttAttemptRecord record);
    }
}
