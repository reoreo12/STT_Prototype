namespace VoxelMagic.Voice.Debug
{
    /// <summary>세션 요약: 시도 수, 평균 변환 시간(성공한 시도만), 평균 유사도, 성공률(판정 통과 비율).</summary>
    public sealed class SttSessionStats
    {
        private double transcribeMsSum;
        private double similaritySum;

        public int Count { get; private set; }
        public int PassedCount { get; private set; }
        public int SucceededCount { get; private set; }

        public double AverageTranscribeMs => SucceededCount > 0 ? transcribeMsSum / SucceededCount : 0;
        public double AverageSimilarity => Count > 0 ? similaritySum / Count : 0;
        public double SuccessRate => Count > 0 ? PassedCount / (double)Count : 0;

        public void Add(SttAttemptRecord record)
        {
            if (record == null) return;
            Count++;
            similaritySum += record.Similarity;
            if (record.Passed) PassedCount++;
            if (record.Succeeded)
            {
                SucceededCount++;
                transcribeMsSum += record.TranscribeMs;
            }
        }

        public void Clear()
        {
            Count = PassedCount = SucceededCount = 0;
            transcribeMsSum = similaritySum = 0;
        }
    }
}
