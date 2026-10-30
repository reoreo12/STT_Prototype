// 두 문자열의 자모 분해·편집 거리·유사도·성공 판정을 바로 확인하는 에디터 창이다.
// 인식 결과와 목표 주문을 입력해 성공 기준 값을 조정해 볼 때 쓴다.

using UnityEditor;
using UnityEngine;

namespace VoxelMagic.Voice.Editor
{
    /// <summary>Tools/VoiceSTT/Similarity Check: 두 문자열의 자모 분해와 유사도를 바로 확인한다.</summary>
    public sealed class SimilarityCheckWindow : EditorWindow
    {
        private string recognized = "불꼬차"; // 입력한 인식 결과 문자열
        private string target = "불꽃아"; // 비교할 목표 주문 문자열
        private float threshold = 0.75f; // 성공으로 판정할 유사도 기준(0~1)

        [MenuItem("Tools/VoiceSTT/Similarity Check")]
        private static void Open()
        {
            GetWindow<SimilarityCheckWindow>("Similarity Check");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("자모 유사도 확인", EditorStyles.boldLabel);
            recognized = EditorGUILayout.TextField("인식 결과", recognized);
            target = EditorGUILayout.TextField("목표 주문", target);
            threshold = EditorGUILayout.Slider("성공 기준", threshold, 0f, 1f);

            var matcher = new JamoSimilarityMatcher(threshold);
            SpellMatchResult result = matcher.Match(recognized, target);
            string a = HangulJamo.DecomposeToString(TextNormalizer.Normalize(recognized));
            string b = HangulJamo.DecomposeToString(TextNormalizer.Normalize(target));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("자모 (인식)", $"{a}  ({a.Length})");
            EditorGUILayout.LabelField("자모 (목표)", $"{b}  ({b.Length})");
            EditorGUILayout.LabelField("편집 거리", Levenshtein.Distance(a, b).ToString());
            EditorGUILayout.LabelField("유사도", result.Similarity.ToString("0.000"));
            EditorGUILayout.LabelField("판정", result.Passed ? "성공" : "실패");
        }
    }
}
