using UnityEngine;
using UnityEngine.UI;

namespace TopClimbing.EditorTools
{
    /// <summary>
    /// Kod ile legacy uGUI öğeleri (Text/Button/Slider/Toggle/Image/Panel) oluşturan
    /// editor yardımcısı. GameBuilder bunu kullanarak sahneleri kurar.
    /// </summary>
    public static class UIBuilder
    {
        public static Sprite PanelSprite;
        private static Font _font;

        public static Font GetFont()
        {
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return _font;
        }

        // ---------- Rect yardımcıları ----------
        public static RectTransform Rect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            return rt;
        }

        public static void SetAnchor(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt, float pad = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        // ---------- Temel öğeler ----------
        public static GameObject Empty(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static Image MakeImage(Transform parent, string name, Color color, bool rounded = true)
        {
            var go = Empty(parent, name);
            var img = go.AddComponent<Image>();
            img.color = color;
            if (rounded && PanelSprite != null) { img.sprite = PanelSprite; img.type = Image.Type.Sliced; }
            return img;
        }

        public static Text MakeText(Transform parent, string name, string text, int fontSize,
                                    Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = Empty(parent, name);
            var t = go.AddComponent<Text>();
            t.font = GetFont();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button MakeButton(Transform parent, string name, string label, Color bg, int fontSize = 34)
        {
            var go = Empty(parent, name);
            var img = go.AddComponent<Image>();
            img.color = bg;
            if (PanelSprite != null) { img.sprite = PanelSprite; img.type = Image.Type.Sliced; }
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = Color.Lerp(bg, Color.white, 0.2f);
            colors.pressedColor = Color.Lerp(bg, Color.black, 0.2f);
            btn.colors = colors;

            if (!string.IsNullOrEmpty(label))
            {
                var t = MakeText(go.transform, "Text", label, fontSize, Color.white);
                Stretch(Rect(t.gameObject));
            }
            return btn;
        }

        /// <summary>İlerleme/stat çubuğu (handle yok).</summary>
        public static Slider MakeBar(Transform parent, string name, Color fillColor, bool interactable = false)
        {
            var go = Empty(parent, name);
            var slider = go.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = interactable;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            var bg = MakeImage(go.transform, "Background", new Color(0f, 0f, 0f, 0.35f));
            Stretch(Rect(bg.gameObject));

            var fillArea = Empty(go.transform, "Fill Area");
            Stretch(Rect(fillArea), 4f);

            var fill = MakeImage(fillArea.transform, "Fill", fillColor);
            var fillRt = Rect(fill.gameObject);
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(1, 1);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            slider.fillRect = fillRt;
            slider.targetGraphic = fill;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        public static Toggle MakeToggle(Transform parent, string name, string label, Color accent)
        {
            var go = Empty(parent, name);
            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = true;

            // Kutu
            var box = MakeImage(go.transform, "Background", new Color(1, 1, 1, 0.9f));
            var boxRt = Rect(box.gameObject);
            SetAnchor(boxRt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(56, 56));

            // İşaret
            var check = MakeImage(box.transform, "Checkmark", accent);
            Stretch(Rect(check.gameObject), 10f);

            // Etiket
            var t = MakeText(go.transform, "Label", label, 32, Color.white, TextAnchor.MiddleLeft);
            var tRt = Rect(t.gameObject);
            SetAnchor(tRt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(80, 0), new Vector2(-90, 0));

            toggle.targetGraphic = box;
            toggle.graphic = check;
            return toggle;
        }
    }
}
