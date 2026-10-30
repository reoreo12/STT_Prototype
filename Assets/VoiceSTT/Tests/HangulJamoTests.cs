// HangulJamo의 음절→자모 분해와 한글 음절 범위 판별을 검증한다.

using NUnit.Framework;

namespace VoxelMagic.Voice.Tests
{
    public sealed class HangulJamoTests
    {
        [TestCase("꽃", "ㄲㅗㅊ")]
        [TestCase("가", "ㄱㅏ")]
        [TestCase("불꽃아", "ㅂㅜㄹㄲㅗㅊㅇㅏ")]
        [TestCase("닭", "ㄷㅏㄺ")]
        [TestCase("힣", "ㅎㅣㅎ")]
        public void Decompose_SplitsSyllables(string input, string expected)
        {
            Assert.AreEqual(expected, HangulJamo.DecomposeToString(input));
        }

        [Test]
        public void Decompose_KeepsNonHangul()
        {
            Assert.AreEqual("a1ㄱ", HangulJamo.DecomposeToString("a1ㄱ"));
        }

        [Test]
        public void Decompose_EmptyAndNull()
        {
            Assert.AreEqual("", HangulJamo.DecomposeToString(""));
            Assert.AreEqual("", HangulJamo.DecomposeToString(null));
        }

        [Test]
        public void IsSyllable_Boundaries()
        {
            Assert.IsTrue(HangulJamo.IsSyllable('가'));
            Assert.IsTrue(HangulJamo.IsSyllable('힣'));
            Assert.IsFalse(HangulJamo.IsSyllable('ㄱ'));
            Assert.IsFalse(HangulJamo.IsSyllable('A'));
        }
    }
}
