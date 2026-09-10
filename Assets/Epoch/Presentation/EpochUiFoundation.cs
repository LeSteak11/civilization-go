#nullable enable
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Epoch.Presentation
{
    public enum EpochResponsiveProfile
    {
        SHORT,
        REFERENCE,
        TALL,
    }

    public enum EpochButtonStyle
    {
        PRIMARY,
        SECONDARY,
        QUIET,
    }

    public static class EpochUiTokens
    {
        public const float ReferenceWidth = 390f;
        public const float ReferenceHeight = 844f;
        public const float Gutter = 16f;
        public const float SpaceSmall = 8f;
        public const float SpaceMedium = 12f;
        public const float SpaceLarge = 20f;
        public const float MinimumTouch = 48f;
        public const float PrimaryActionHeight = 56f;
        public const float StickyRegionHeight = 76f;

        public const int TextSmall = 15;
        public const int TextBody = 17;
        public const int TextHeading = 22;
        public const int TextTitle = 30;

        public static readonly Color Background = Hex("101722");
        public static readonly Color Surface = Hex("18222E");
        public static readonly Color SurfaceRaised = Hex("243241");
        public static readonly Color Text = Hex("E8D9B6");
        public static readonly Color TextMuted = Hex("A8AFB7");
        public static readonly Color Accent = Hex("D6AE58");
        public static readonly Color Player = Hex("45B8A6");
        public static readonly Color Snapshot = Hex("D86B62");
        public static readonly Color Disabled = Hex("4A535D");
        public static readonly Color Dimmer = new Color(0.02f, 0.035f, 0.055f, 0.82f);

        public static EpochResponsiveProfile ProfileFor(float width, float height)
        {
            if (width <= 0f || height <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            float ratio = height / width;
            if (ratio <= 1.9f)
            {
                return EpochResponsiveProfile.SHORT;
            }

            return ratio >= 2.25f ? EpochResponsiveProfile.TALL : EpochResponsiveProfile.REFERENCE;
        }

        private static Color Hex(string value)
        {
            if (!ColorUtility.TryParseHtmlString("#" + value, out Color color))
            {
                throw new ArgumentException("Invalid UI color " + value + ".", nameof(value));
            }

            return color;
        }
    }

    [RequireComponent(typeof(RectTransform))]
    public sealed class EpochSafeAreaRoot : MonoBehaviour
    {
        public Rect LastSafeArea { get; private set; }

        public Vector2 AnchorMinimum { get; private set; }

        public Vector2 AnchorMaximum { get; private set; }

        public void Apply(Rect safeArea, float screenWidth, float screenHeight)
        {
            if (screenWidth <= 0f || screenHeight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(screenWidth));
            }

            LastSafeArea = safeArea;
            AnchorMinimum = new Vector2(safeArea.xMin / screenWidth, safeArea.yMin / screenHeight);
            AnchorMaximum = new Vector2(safeArea.xMax / screenWidth, safeArea.yMax / screenHeight);
            RectTransform rect = GetComponent<RectTransform>();
            rect.anchorMin = AnchorMinimum;
            rect.anchorMax = AnchorMaximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public static class EpochUiFactory
    {
        public static RectTransform Panel(
            string name,
            Transform parent,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            item.transform.SetParent(parent, false);
            RectTransform rect = item.GetComponent<RectTransform>();
            Stretch(rect, anchorMin, anchorMax);
            item.GetComponent<Image>().color = color;
            return rect;
        }

        public static Text Text(
            string name,
            string value,
            Transform parent,
            Font font,
            int size,
            Color color,
            TextAnchor alignment,
            FontStyle style,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            RectTransform rect = item.GetComponent<RectTransform>();
            Stretch(rect, anchorMin, anchorMax);
            Text text = item.GetComponent<Text>();
            text.font = font;
            text.fontSize = Math.Max(EpochUiTokens.TextSmall, size);
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        public static Button Button(
            string name,
            string label,
            Transform parent,
            Font font,
            EpochButtonStyle style,
            UnityAction action,
            Vector2 anchorMin,
            Vector2 anchorMax,
            bool interactable = true,
            bool selected = false)
        {
            Color baseColor = style == EpochButtonStyle.PRIMARY
                ? EpochUiTokens.Accent
                : style == EpochButtonStyle.SECONDARY
                    ? EpochUiTokens.SurfaceRaised
                    : EpochUiTokens.Surface;
            RectTransform panel = Panel(name, parent, baseColor, anchorMin, anchorMax);
            Button button = panel.gameObject.AddComponent<Button>();
            button.interactable = interactable;
            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Lighten(baseColor, 0.10f);
            colors.pressedColor = Lighten(baseColor, -0.12f);
            colors.selectedColor = Lighten(baseColor, 0.16f);
            colors.disabledColor = EpochUiTokens.Disabled;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.AddListener(action);

            Text text = Text(
                "Label",
                label,
                panel,
                font,
                EpochUiTokens.TextSmall,
                style == EpochButtonStyle.PRIMARY ? EpochUiTokens.Background : EpochUiTokens.Text,
                TextAnchor.MiddleCenter,
                FontStyle.Bold,
                Vector2.zero,
                Vector2.one);
            text.raycastTarget = false;

            if (selected)
            {
                Outline outline = panel.gameObject.AddComponent<Outline>();
                outline.effectColor = EpochUiTokens.Player;
                outline.effectDistance = new Vector2(3f, -3f);
            }

            return button;
        }

        public static Image SpriteImage(
            string name,
            Transform parent,
            Sprite? sprite,
            Vector2 anchorMin,
            Vector2 anchorMax,
            bool sliced = false,
            bool raycastTarget = false)
        {
            RectTransform panel = Panel(name, parent, Color.white, anchorMin, anchorMax);
            Image image = panel.GetComponent<Image>();
            image.raycastTarget = raycastTarget;
            EpochArtCatalog.ApplySprite(image, sprite, sliced);
            return image;
        }
        public static ScrollRect ScrollArea(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            out RectTransform content)
        {
            RectTransform viewport = Panel(name, parent, new Color(0f, 0f, 0f, 0f), anchorMin, anchorMax);
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 420f);
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        public static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static Color Lighten(Color color, float amount) => new Color(
            Mathf.Clamp01(color.r + amount),
            Mathf.Clamp01(color.g + amount),
            Mathf.Clamp01(color.b + amount),
            color.a);
    }
}
