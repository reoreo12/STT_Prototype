// TextNormalizer가 공백·문장부호를 없애고 소문자로 바꾸는지 검증한다.

using NUnit.Framework;

namespace VoxelMagic.Voice.Tests
{
    public sealed class TextNormalizerTests
    {
        [TestCase(" 빛나라. ", "빛나라")]
        [TestCase("불꽃 아!?", "불꽃아")]
        [TestCase("Hello, World!", "helloworld")]
        [TestCase("숨어라 2번", "숨어라2번")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void Normalize_RemovesSpacesAndPunctuation(string input, string expected)
        {
            Assert.AreEqual(expected, TextNormalizer.Normalize(input));
        }
    }
}
