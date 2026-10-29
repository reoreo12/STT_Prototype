namespace VoxelMagic.Voice
{
    /// <summary>
    /// 인식된 텍스트가 목표 주문과 맞는지 판정한다. 입력 출처(음성, HUD, 키보드)와 무관하게 텍스트만 받는다.
    /// </summary>
    public interface ISpellMatcher
    {
        /// <summary>통과 기준값 (0~1).</summary>
        float Threshold { get; }

        void SetThreshold(float threshold);

        /// <summary>0~1 사이 유사도. 1이면 같다.</summary>
        float Similarity(string recognized, string target);

        SpellMatchResult Match(string recognized, string target);
    }

    /// <summary>판정 결과.</summary>
    public readonly struct SpellMatchResult
    {
        public SpellMatchResult(float similarity, bool passed, float threshold)
        {
            Similarity = similarity;
            Passed = passed;
            Threshold = threshold;
        }

        public float Similarity { get; }
        public bool Passed { get; }
        public float Threshold { get; }
    }
}
