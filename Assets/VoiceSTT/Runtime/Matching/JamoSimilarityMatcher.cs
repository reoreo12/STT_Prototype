using System;

namespace VoxelMagic.Voice
{
    /// <summary>
    /// 한글 자모 레벤슈타인 유사도로 판정한다.
    /// 유사도 = 1 - 거리 / 더 긴 자모 배열 길이. 예: "불꼬차" vs "불꽃아" = 1 - 1/8 = 0.875
    /// </summary>
    public sealed class JamoSimilarityMatcher : ISpellMatcher
    {
        private float threshold;

        public JamoSimilarityMatcher(float threshold)
        {
            SetThreshold(threshold);
        }

        public float Threshold => threshold;

        public void SetThreshold(float value)
        {
            threshold = Math.Max(0f, Math.Min(1f, value));
        }

        public float Similarity(string recognized, string target)
        {
            var a = HangulJamo.Decompose(TextNormalizer.Normalize(recognized));
            var b = HangulJamo.Decompose(TextNormalizer.Normalize(target));
            int longer = Math.Max(a.Count, b.Count);
            if (longer == 0) return a.Count == b.Count ? 1f : 0f;
            if (a.Count == 0 || b.Count == 0) return 0f;

            int distance = Levenshtein.Distance<char>(a, b);
            return 1f - distance / (float)longer;
        }

        public SpellMatchResult Match(string recognized, string target)
        {
            float similarity = Similarity(recognized, target);
            return new SpellMatchResult(similarity, similarity >= threshold, threshold);
        }
    }
}
