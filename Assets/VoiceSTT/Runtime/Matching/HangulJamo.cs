using System.Collections.Generic;

namespace VoxelMagic.Voice
{
    /// <summary>
    /// 한글 음절을 초성·중성·종성 자모로 분해한다.
    /// 음절 = 0xAC00 + (초성 × 21 + 중성) × 28 + 종성
    /// </summary>
    public static class HangulJamo
    {
        private const int SyllableBase = 0xAC00;
        private const int SyllableLast = 0xD7A3;
        private const int JungCount = 21;
        private const int JongCount = 28;

        private static readonly char[] Cho =
        {
            'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ',
            'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ',
        };

        private static readonly char[] Jung =
        {
            'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ',
            'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ',
        };

        // 0번은 받침 없음
        private static readonly char[] Jong =
        {
            '\0', 'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ',
            'ㄻ', 'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ',
            'ㅆ', 'ㅇ', 'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ',
        };

        public static bool IsSyllable(char c)
        {
            return c >= SyllableBase && c <= SyllableLast;
        }

        /// <summary>음절이면 자모를 돌려준다. 받침이 없으면 jong은 '\0'.</summary>
        public static bool TryDecompose(char syllable, out char cho, out char jung, out char jong)
        {
            if (!IsSyllable(syllable))
            {
                cho = jung = jong = '\0';
                return false;
            }

            int index = syllable - SyllableBase;
            cho = Cho[index / (JungCount * JongCount)];
            jung = Jung[(index % (JungCount * JongCount)) / JongCount];
            jong = Jong[index % JongCount];
            return true;
        }

        /// <summary>문자열을 자모 배열로 바꾼다. 한글 음절이 아닌 글자는 그대로 둔다.</summary>
        public static List<char> Decompose(string text)
        {
            var result = new List<char>(string.IsNullOrEmpty(text) ? 0 : text.Length * 3);
            if (string.IsNullOrEmpty(text)) return result;

            foreach (char c in text)
            {
                if (TryDecompose(c, out char cho, out char jung, out char jong))
                {
                    result.Add(cho);
                    result.Add(jung);
                    if (jong != '\0') result.Add(jong);
                }
                else
                {
                    result.Add(c);
                }
            }
            return result;
        }

        public static string DecomposeToString(string text)
        {
            return new string(Decompose(text).ToArray());
        }
    }
}
