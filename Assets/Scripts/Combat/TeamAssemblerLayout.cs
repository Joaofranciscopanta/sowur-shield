using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.UI;

namespace SowurShield.Combat
{

/// <summary>
/// Builds the team assembler screen in code, at runtime, every time it opens.
///
/// Why procedural: the screen was previously assembled by hand in the scene and patched by
/// three rival editor scripts (TeamAssemblerUISetup, TeamAssemblerUIManualFix,
/// TeamAssemblerUIBuilder — 1358 lines, none of them referenced by anything), while
/// TeamAssemblerUI re-anchored the same panels again at runtime. Nothing owned the layout,
/// so it drifted. Now one file does, and the scene only needs to supply the root canvas.
///
/// Measurements here come from the art, not from round numbers:
/// - card_animal is 1000x200, so cards keep a 5:1 ratio (500x100). A different ratio
///   stretches the middle of the 9-slice; see the sliced-border traps in CLAUDE.md.
/// - slot_grid_empty paints 90px of its 160px texture (56%), so a 96px slot shows a ~54px
///   tile. Slots are sized for the painted area, not the texture.
/// - panel_team_assembler has 40/56/40/40 borders and is 768x512; it is only used at sizes
///   large enough that those borders don't collide.
/// </summary>
public static class TeamAssemblerLayout
{
    // ── Metrics ───────────────────────────────────────────────────────────────

    public const float CardWidth  = 500f;
    public const float CardHeight = 100f;   // 5:1, matching card_animal
    public const float CardSpacing = 6f;

    public const float SlotSize = 96f;
    public const float SlotSpacing = 8f;

    public const float HeaderHeight = 56f;
    public const float FooterHeight = 72f;

    // ── Panel frame insets, measured on a 1920x1080 screenshot ────────────────
    // NOT the 9-slice border values, and not guesses. Measured on the rendered screen:
    // panel_wood_generic's painted cream area starts ~100px below the rect's top edge and
    // ~80px inside each side. Content inset by less than this sits ON the frame art, which
    // is exactly how the first two passes put every column title into the woodwork — a
    // title ending at y=914 against a cream area that only began at y=893.
    /// <summary>
    /// Fraction of a button's width that is transparent on each side.
    ///
    /// Measured ON SCREEN, not in the texture: a 160px-wide Cancel button paints only
    /// 104px of pill (65%), so ~17.5% of the rect is empty on each side. Reading the
    /// sprite alone gives 12% and is wrong — the 9-slice keeps its 16px borders at their
    /// texture size while the middle stretches, so the painted fraction changes with the
    /// rect's width. This is the same trap as the panel frames: measure the screenshot.
    /// </summary>
    public const float ButtonArtInset = 0.175f;

    public const float FrameInsetSide = 84f;
    public const float FrameInsetTop = 104f;
    public const float FrameInsetBottom = 46f;

    private static UITheme cachedTheme;
    public static UITheme Theme
    {
        get
        {
            if (cachedTheme == null)
                cachedTheme = Resources.Load<UITheme>("UI/CozyUITheme");
            return cachedTheme;
        }
    }

    // ── Sprite helpers ────────────────────────────────────────────────────────

    public static Sprite LoadSprite(string path) => Resources.Load<Sprite>("Sprites/UI/" + path);

    // ── Primitive builders ────────────────────────────────────────────────────

    /// <summary>Create a UI GameObject with a stretched RectTransform under parent.</summary>
    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        // Runtime RectTransforms default to point anchors (sizeDelta 0 = literally zero
        // wide), so every helper sets anchors explicitly.
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    /// <summary>A panel with the wood frame art, inset so content clears the painted border.</summary>
    public static RectTransform CreatePanel(string name, Transform parent, string spritePath = "Panels/panel_wood_generic")
    {
        var rt = CreateRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = LoadSprite(spritePath);
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        // A sliced sprite with no art still needs to read as a surface rather than a hole.
        if (img.sprite == null)
            img.color = Theme != null ? Theme.woodDark : new Color(0.42f, 0.27f, 0.14f);

        return rt;
    }

    /// <summary>A text label with the project's theme applied.</summary>
    public static TextMeshProUGUI CreateLabel(string name, Transform parent, string text,
        float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var rt = CreateRect(name, parent);
        var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;

        // Anything creating a label at runtime must take its font from the theme; the
        // engine default is not the game's font.
        var font = Theme != null ? Theme.fontPrimary : null;
        if (font != null) label.font = font;

        return label;
    }

    /// <summary>
    /// A button sized to its own caption. Buttons get a real label rather than an icon:
    /// the button art is 5:1, so a square icon button cannot be built from this kit.
    /// </summary>
    public static Button CreateButton(string name, Transform parent, string caption,
        string spritePath, System.Action onClick)
    {
        var rt = CreateRect(name, parent);

        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = LoadSprite(spritePath);
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        // ColorTint with a coloured normalColor repaints the sprite; white keeps the art.
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
        button.colors = colors;

        var label = CreateLabel("Label", rt, caption,
            Theme != null ? Theme.fontSizeButton : 18f,
            Theme != null ? Theme.textDark : new Color(0.18f, 0.16f, 0.15f),
            TextAlignmentOptions.Center);

        // Inset the caption against the PAINTED pill, not the rect. Measured on the art:
        // every button sprite is 600x120 (480x96 for small_action) and paints from x≈70 to
        // x≈530 — about 12% of the width is transparent on each side. A fixed 18px inset
        // was far too small at these widths, so captions were clipped by the artwork's own
        // edge while technically fitting inside the rect.
        label.rectTransform.anchorMin = new Vector2(ButtonArtInset, 0f);
        label.rectTransform.anchorMax = new Vector2(1f - ButtonArtInset, 1f);
        label.rectTransform.offsetMin = new Vector2(0f, 6f);
        label.rectTransform.offsetMax = new Vector2(0f, -6f);

        if (onClick != null)
            button.onClick.AddListener(() => onClick());

        return button;
    }

    /// <summary>Give a RectTransform a fixed size at a given anchor point.</summary>
    public static void SetSize(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPosition;
    }

    /// <summary>Stretch to fill the parent with the given insets.</summary>
    public static void Fill(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    /// <summary>
    /// A vertical scroll view. Returns the content transform to parent items into.
    /// </summary>
    public static RectTransform CreateScrollView(string name, Transform parent, float spacing,
        bool expandChildWidth = true)
    {
        var scrollRect = CreateRect(name, parent);
        var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        var viewport = CreateRect("Viewport", scrollRect);
        viewport.gameObject.AddComponent<RectMask2D>();
        var vpImage = viewport.gameObject.AddComponent<Image>();
        vpImage.color = new Color(0f, 0f, 0f, 0f); // invisible, but needed to receive drags
        scroll.viewport = viewport;

        var content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        // Text rows want the full width; the animal cards do NOT — expanding those past
        // their art's 5:1 ratio smears the middle of the 9-slice. Turning it off for
        // everything instead collapsed the synergy rows to a one-character-wide column.
        layout.childForceExpandWidth = expandChildWidth;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(4, 4, 4, 4);

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scroll.content = content;
        return content;
    }

    /// <summary>
    /// A single-line text input, used for naming a team profile.
    /// </summary>
    public static TMP_InputField CreateInputField(string name, Transform parent, string placeholder)
    {
        var rt = CreateRect(name, parent);

        // A flat cream field rather than slot_inventory: that sprite has no 9-slice border,
        // so stretching it across a wide input box smears the tile.
        var bg = rt.gameObject.AddComponent<Image>();
        bg.color = Theme != null ? Theme.backgroundCream : new Color(0.97f, 0.95f, 0.91f);

        var input = rt.gameObject.AddComponent<TMP_InputField>();

        var textArea = CreateRect("TextArea", rt);
        Fill(textArea, 12f, 4f, 12f, 4f);
        textArea.gameObject.AddComponent<RectMask2D>();

        var placeholderLabel = CreateLabel("Placeholder", textArea, placeholder,
            Theme != null ? Theme.fontSizeSmall : 14f,
            new Color(0.45f, 0.42f, 0.38f, 0.8f));
        placeholderLabel.fontStyle = FontStyles.Italic;

        var textLabel = CreateLabel("Text", textArea, "",
            Theme != null ? Theme.fontSizeSmall : 14f,
            Theme != null ? Theme.textDark : Color.black);

        input.textViewport = textArea;
        input.textComponent = textLabel;
        input.placeholder = placeholderLabel;
        input.characterLimit = 20;
        input.lineType = TMP_InputField.LineType.SingleLine;

        return input;
    }
}

} // namespace SowurShield.Combat
