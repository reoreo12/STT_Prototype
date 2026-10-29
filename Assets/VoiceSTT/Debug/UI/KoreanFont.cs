using System.Collections.Generic;
using UnityEngine;

namespace VoxelMagic.Voice.Debug
{
    /// <summary>
    /// 한글을 표시할 OS 폰트를 한 번 만들어 공유한다. (다운로드·에셋 없이 레거시 Text에서 사용)
    /// OS 폰트이므로 맑은 고딕이 없는 OS에서는 후보 목록의 다른 폰트를 쓴다.
    /// </summary>
    public static class KoreanFont
    {
        private static readonly string[] Candidates =
        {
            "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Noto Sans KR", "NanumGothic",
        };

        private static Font cached;

        public static string ResolvedName { get; private set; } = "";

        public static Font Get()
        {
            if (cached != null) return cached;

            var installed = new HashSet<string>(Font.GetOSInstalledFontNames());
            foreach (string name in Candidates)
            {
                if (!installed.Contains(name)) continue;
                cached = Font.CreateDynamicFontFromOSFont(name, 32);
                ResolvedName = name;
                return cached;
            }

            UnityEngine.Debug.LogWarning("[VoiceSTT] 한글 OS 폰트를 찾지 못했습니다. 한글이 네모로 보일 수 있습니다.");
            cached = Font.CreateDynamicFontFromOSFont(Candidates, 32);
            ResolvedName = "(대체 폰트)";
            return cached;
        }
    }
}
