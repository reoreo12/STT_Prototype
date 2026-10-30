// JamoSimilarityMatcher의 자모 유사도·성공 기준 판정과 Levenshtein 편집 거리를 검증한다.

using NUnit.Framework;

namespace VoxelMagic.Voice.Tests
{
    public sealed class JamoSimilarityMatcherTests
    {
        private JamoSimilarityMatcher matcher; // 테스트마다 기준 0.75로 새로 만드는 매처

        [SetUp]
        public void SetUp()
        {
            matcher = new JamoSimilarityMatcher(0.75f);
        }

        [Test]
        public void SameText_IsOne()
        {
            Assert.AreEqual(1f, matcher.Similarity("빛나라", "빛나라"), 1e-6);
        }

        [Test]
        public void SpellingError_FromSpec_Passes()
        {
            // ㅂㅜㄹㄲㅗㅊㅏ(7) vs ㅂㅜㄹㄲㅗㅊㅇㅏ(8): 거리 1 → 1 - 1/8
            SpellMatchResult result = matcher.Match("불꼬차", "불꽃아");
            Assert.AreEqual(0.875f, result.Similarity, 1e-4);
            Assert.IsTrue(result.Passed);
        }

        [Test]
        public void VowelSlip_Passes()
        {
            SpellMatchResult result = matcher.Match("느려저라", "느려져라");
            Assert.AreEqual(0.875f, result.Similarity, 1e-4);
            Assert.IsTrue(result.Passed);
        }

        [Test]
        public void DifferentSpell_Fails()
        {
            SpellMatchResult result = matcher.Match("빛나라", "숨어라");
            Assert.Less(result.Similarity, 0.5f);
            Assert.IsFalse(result.Passed);
        }

        [Test]
        public void SpacesAndPunctuation_AreIgnored()
        {
            Assert.AreEqual(1f, matcher.Similarity(" 불꽃 아! ", "불꽃아"), 1e-6);
        }

        [Test]
        public void EmptyRecognition_IsZero()
        {
            Assert.AreEqual(0f, matcher.Similarity("", "불꽃아"), 1e-6);
            Assert.IsFalse(matcher.Match(null, "불꽃아").Passed);
        }

        [Test]
        public void Threshold_IsAppliedAndClamped()
        {
            matcher.SetThreshold(0.9f);
            Assert.IsFalse(matcher.Match("불꼬차", "불꽃아").Passed);

            matcher.SetThreshold(2f);
            Assert.AreEqual(1f, matcher.Threshold);
        }

        [TestCase("kitten", "sitting", 3)]
        [TestCase("", "abc", 3)]
        [TestCase("same", "same", 0)]
        public void Levenshtein_KnownDistances(string a, string b, int expected)
        {
            Assert.AreEqual(expected, Levenshtein.Distance(a, b));
        }
    }
}
