using System;
using UnityEngine;
using UnityEngine.UI;

namespace OrbitFlock
{
    internal static class FlightUi
    {
        internal const int Width = 1280;
        internal const int Height = 720;
        internal const int FlightTop = 66;
        internal const int FlightBottom = 68;
        internal const int FlightHeight = Height - FlightTop - FlightBottom;
        internal static readonly float FlightWidth = FlightHeight * PlayfieldCamera.HalfWidth / PlayfieldCamera.HalfHeight;
        internal static readonly float FlightLeft = (Width - FlightWidth) / 2f;

        internal static readonly Color Ink = new Color32(17, 21, 27, 255);
        internal static readonly Color Panel = new Color32(28, 34, 43, 255);
        internal static readonly Color Line = new Color32(63, 75, 89, 255);
        internal static readonly Color Muted = new Color32(156, 173, 185, 255);
        internal static readonly Color Paper = new Color32(245, 234, 211, 255);
        internal static readonly Color PaperMuted = new Color32(96, 83, 70, 255);
        internal static readonly Color PaperLine = new Color32(191, 172, 143, 255);
        internal static readonly Color Amber = new Color32(255, 192, 97, 255);
        internal static readonly Color Mint = new Color32(131, 222, 187, 255);
        internal static readonly Color Rose = new Color32(255, 137, 146, 255);

        private static Font font;

        internal static RectTransform Page(Canvas canvas)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            RectTransform page = Rect(canvas.transform, "Flight console", 0, 0, Width, Height);
            page.anchorMin = page.anchorMax = page.pivot = new Vector2(0.5f, 0.5f);
            page.anchoredPosition = Vector2.zero;
            return page;
        }

        internal static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        internal static Image Block(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        internal static Text Label(Transform parent, string text, float x, float y, float width, float height, int size,
            Color color, FontStyle style = FontStyle.Normal, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var label = Rect(parent, text.Length == 0 ? "Value" : text, x, y, width, height).gameObject.AddComponent<Text>();
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.font = font;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = align;
            label.color = color;
            label.text = text;
            label.raycastTarget = false;
            return label;
        }

        internal static Image Icon(Transform parent, string name, Sprite sprite, float x, float y, float width, float height, Color tint)
        {
            Image image = Block(parent, name, x, y, width, height, tint);
            image.sprite = sprite;
            image.preserveAspect = true;
            return image;
        }

        internal static Button Button(Transform parent, string title, float x, float y, float width, float height,
            Color background, Color foreground, Action onClick, int fontSize = 19)
        {
            Image image = Block(parent, title, x, y, width, height, Color.white);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = new ColorBlock
            {
                normalColor = background,
                highlightedColor = Color.Lerp(background, foreground, 0.16f),
                pressedColor = Color.Lerp(background, foreground, 0.28f),
                selectedColor = Color.Lerp(background, foreground, 0.16f),
                disabledColor = Color.Lerp(background, foreground, 0.10f),
                colorMultiplier = 1,
                fadeDuration = 0.12f
            };
            Text label = Label(image.transform, title, 0, 0, width, height, fontSize, foreground, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.gameObject.name = "Label";
            button.onClick.AddListener(() => onClick());
            return button;
        }

        internal static void Slider(Transform parent, string title, float y, float value, Action<float> change)
        {
            Label(parent, title, 28, y, 129, 29, 16, Ink, FontStyle.Bold);
            Text reading = Label(parent, Percent(value), 348, y, 54, 29, 15, PaperMuted, FontStyle.Bold, TextAnchor.MiddleRight);
            RectTransform root = Rect(parent, title + " volume", 165, y, 168, 29);
            Image target = root.gameObject.AddComponent<Image>();
            target.color = new Color(1f, 1f, 1f, 0f);
            Block(root, "Volume track", 7, 12, 154, 5, PaperLine);
            Image fill = Block(root, "Volume fill", 7, 12, 154, 5, Ink);
            SetFill(fill.rectTransform, value, 154f);
            RectTransform guide = Rect(root, "Thumb area", 7, 0, 154, 29);
            Image thumb = Block(guide, "Thumb", 0, 0, 14, 18, Color.white);
            thumb.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            thumb.rectTransform.sizeDelta = new Vector2(14, -11);
            thumb.rectTransform.anchoredPosition = Vector2.zero;
            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.targetGraphic = thumb;
            slider.handleRect = thumb.rectTransform;
            slider.colors = new ColorBlock
            {
                normalColor = Ink, highlightedColor = new Color32(117, 66, 36, 255),
                pressedColor = new Color32(150, 72, 37, 255), selectedColor = new Color32(117, 66, 36, 255),
                disabledColor = PaperLine, colorMultiplier = 1f, fadeDuration = 0.12f
            };
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(next =>
            {
                reading.text = Percent(next);
                SetFill(fill.rectTransform, next, 154f);
                change(next);
            });
        }

        internal static void SetFill(RectTransform fill, float fraction, float width)
        {
            fill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width * Mathf.Clamp01(fraction));
        }

        private static string Percent(float value) => $"{Mathf.RoundToInt(value * 100)}%";
    }
}
