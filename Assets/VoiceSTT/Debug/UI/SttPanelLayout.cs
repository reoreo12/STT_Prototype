// 프리팹 없이 테스트 패널 UI를 코드로 짜기 위해 uGUI 요소 생성 코드를 모아 둔다.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelMagic.Voice.Debug
{
    /// <summary>uGUI 요소를 코드로 만드는 도우미. 모든 Text에 한글 폰트를 넣는다.</summary>
    internal static class SttPanelLayout
    {
        public const int BodyFontSize = 20; // 본문·컨트롤 기본 글자 크기

        private static readonly DefaultControls.Resources Resources = new DefaultControls.Resources(); // 기본 컨트롤 생성용 빈 리소스 (스프라이트 없음)
        private static readonly Color ControlTextColor = new Color(0.12f, 0.12f, 0.12f); // 밝은 컨트롤(버튼·드롭다운·입력칸) 안 글자색

        public static RectTransform CreateRoot(Transform parent, Color background)
        {
            var go = new GameObject("Root", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = background;

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            ConfigureGroup(go.AddComponent<VerticalLayoutGroup>(), true, 10, 16);
            return rt;
        }

        public static RectTransform CreateGroup(Transform parent, string name, bool vertical, float spacing, int padding, Color? background = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            if (background.HasValue) go.AddComponent<Image>().color = background.Value;

            HorizontalOrVerticalLayoutGroup group = vertical
                ? go.AddComponent<VerticalLayoutGroup>()
                : (HorizontalOrVerticalLayoutGroup)go.AddComponent<HorizontalLayoutGroup>();
            ConfigureGroup(group, vertical, spacing, padding);
            return (RectTransform)go.transform;
        }

        public static LayoutElement SetSize(Component target, float width = -1, float height = -1, float flexibleWidth = -1, float flexibleHeight = -1)
        {
            LayoutElement element = target.GetComponent<LayoutElement>();
            if (element == null) element = target.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.minWidth = width;
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            return element;
        }

        public static Text CreateLabel(Transform parent, string text, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.font = KoreanFont.Get();
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.text = text;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        public static Button CreateButton(Transform parent, string text, float width, float height, Action onClick)
        {
            GameObject go = DefaultControls.CreateButton(Resources);
            go.transform.SetParent(parent, false);
            Text label = go.GetComponentInChildren<Text>(true);
            label.text = text;
            label.fontSize = BodyFontSize;
            label.color = ControlTextColor;
            ApplyFont(go);
            SetSize(go.transform, width, height);

            var button = go.GetComponent<Button>();
            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        public static Dropdown CreateDropdown(Transform parent, IEnumerable<string> options, float width, float height)
        {
            GameObject go = DefaultControls.CreateDropdown(Resources);
            go.transform.SetParent(parent, false);
            var dropdown = go.GetComponent<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            dropdown.captionText.fontSize = BodyFontSize;
            dropdown.captionText.color = ControlTextColor;
            dropdown.itemText.fontSize = BodyFontSize;
            dropdown.itemText.color = ControlTextColor;

            var item = dropdown.itemText.transform.parent as RectTransform;
            if (item != null) item.sizeDelta = new Vector2(item.sizeDelta.x, 34);
            dropdown.template.sizeDelta = new Vector2(dropdown.template.sizeDelta.x, 320);

            Transform arrow = go.transform.Find("Arrow");
            if (arrow != null) arrow.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f);

            ApplyFont(go);
            SetSize(go.transform, width, height);
            return dropdown;
        }

        public static Slider CreateSlider(Transform parent, float width, float height, Color fill)
        {
            GameObject go = DefaultControls.CreateSlider(Resources);
            go.transform.SetParent(parent, false);
            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            Transform background = go.transform.Find("Background");
            if (background != null) background.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.35f);
            slider.fillRect.GetComponent<Image>().color = fill;
            slider.handleRect.GetComponent<Image>().color = Color.white;

            SetSize(go.transform, width, height);
            return slider;
        }

        public static InputField CreateInputField(Transform parent, string placeholder, float width, float height)
        {
            GameObject go = DefaultControls.CreateInputField(Resources);
            go.transform.SetParent(parent, false);
            var field = go.GetComponent<InputField>();
            field.textComponent.fontSize = BodyFontSize;
            field.textComponent.color = ControlTextColor;
            if (field.placeholder is Text hint)
            {
                hint.text = placeholder;
                hint.fontSize = BodyFontSize;
            }
            ApplyFont(go);
            SetSize(go.transform, width, height);
            return field;
        }

        /// <summary>세로 스크롤 목록. 반환값은 행을 넣을 content.</summary>
        public static RectTransform CreateScrollList(Transform parent)
        {
            GameObject go = DefaultControls.CreateScrollView(Resources);
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

            var scroll = go.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30f;
            if (scroll.horizontalScrollbar != null)
            {
                UnityEngine.Object.Destroy(scroll.horizontalScrollbar.gameObject);
                scroll.horizontalScrollbar = null;
            }
            if (scroll.verticalScrollbar != null)
            {
                scroll.verticalScrollbar.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
                scroll.verticalScrollbar.handleRect.GetComponent<Image>().color = new Color(0.6f, 0.6f, 0.6f);
            }

            RectTransform content = scroll.content;
            var group = content.gameObject.AddComponent<VerticalLayoutGroup>();
            ConfigureGroup(group, true, 2, 4);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            SetSize(go.transform, -1, -1, 1, 1);
            return content;
        }

        /// <summary>왼쪽부터 채워지는 막대. 반환값의 anchorMax.x(0~1)로 길이를 바꾼다.</summary>
        public static RectTransform CreateBar(Transform parent, float height, Color background, Color fill)
        {
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            bar.GetComponent<Image>().color = background;
            SetSize(bar.transform, -1, height, 1);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(bar.transform, false);
            fillGo.GetComponent<Image>().color = fill;
            var rt = (RectTransform)fillGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static void ApplyFont(GameObject root)
        {
            Font font = KoreanFont.Get();
            foreach (Text text in root.GetComponentsInChildren<Text>(true)) text.font = font;
        }

        private static void ConfigureGroup(HorizontalOrVerticalLayoutGroup group, bool vertical, float spacing, int padding)
        {
            group.spacing = spacing;
            group.padding = new RectOffset(padding, padding, padding, padding);
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = vertical;
            group.childForceExpandHeight = false;
            group.childAlignment = vertical ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
        }
    }
}
