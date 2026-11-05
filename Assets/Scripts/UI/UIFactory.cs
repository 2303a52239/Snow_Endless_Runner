using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SnowRunner
{
    /// <summary>Builds uGUI elements from code so no manual UI setup (or missing references) is needed.</summary>
    public static class UIFactory
    {
        static Font font;

        public static Font DefaultFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static readonly Color PanelColor = new Color(0.08f, 0.17f, 0.3f, 0.88f);
        public static readonly Color ButtonColor = new Color(0.25f, 0.62f, 0.95f, 1f);
        public static readonly Color ButtonDanger = new Color(0.9f, 0.35f, 0.35f, 1f);
        public static readonly Color ButtonGreen = new Color(0.3f, 0.78f, 0.5f, 1f);
        public static readonly Color Gold = new Color(1f, 0.85f, 0.25f);

        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Image Panel(Transform parent, string name, Color color)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
            Vector2 anchor, Vector2 pos, Vector2 boxSize, TextAnchor align = TextAnchor.MiddleCenter, bool outline = true)
        {
            var rt = NewRect("Label_" + text, parent);
            Place(rt, anchor, pos, boxSize);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.03f, 0.1f, 0.2f, 0.9f);
                o.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        public static Button MakeButton(Transform parent, string label, Color color,
            Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick, int fontSize = 40)
        {
            var rt = NewRect("Button_" + label, parent);
            Place(rt, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = color;
            cb.highlightedColor = Color.Lerp(color, Color.white, 0.25f);
            cb.pressedColor = Color.Lerp(color, Color.black, 0.25f);
            cb.disabledColor = new Color(0.5f, 0.5f, 0.55f, 0.8f);
            cb.colorMultiplier = 1f;
            btn.colors = cb;
            // Image.color is white so the ColorBlock tints fully with the chosen color.
            var txt = Label(rt, label, fontSize, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            txt.name = "Text";
            var trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            btn.onClick.AddListener(() => AudioManager.Instance.PlayClick());
            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        public static void SetButtonText(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }
    }
}
