using System.Text;

namespace VoxelMagic.Voice
{
    /// <summary>판정 전에 텍스트를 정리한다: 공백·문장부호 제거, 영문 소문자화.</summary>
    public static class TextNormalizer
    {
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
    }
}
