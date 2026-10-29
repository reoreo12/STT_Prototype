using System;
using System.Collections.Generic;

namespace VoxelMagic.Voice
{
    /// <summary>두 배열 사이의 편집 거리(삽입·삭제·치환 각 1).</summary>
    public static class Levenshtein
    {
        public static int Distance<T>(IReadOnlyList<T> a, IReadOnlyList<T> b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (a.Count == 0) return b.Count;
            if (b.Count == 0) return a.Count;

            var comparer = EqualityComparer<T>.Default;
            var previous = new int[b.Count + 1];
            var current = new int[b.Count + 1];
            for (int j = 0; j <= b.Count; j++) previous[j] = j;

            for (int i = 1; i <= a.Count; i++)
            {
                current[0] = i;
                for (int j = 1; j <= b.Count; j++)
                {
                    int cost = comparer.Equals(a[i - 1], b[j - 1]) ? 0 : 1;
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }

                int[] swap = previous;
                previous = current;
                current = swap;
            }
            return previous[b.Count];
        }

        public static int Distance(string a, string b)
        {
            return Distance<char>((a ?? "").ToCharArray(), (b ?? "").ToCharArray());
        }
    }
}
