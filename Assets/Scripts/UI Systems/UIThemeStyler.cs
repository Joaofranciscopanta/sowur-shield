using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SowurShield.UI
{

/// <summary>
/// Runtime restyling helpers for scene-wired UI that predates the cozy theme.
/// Applies the shared sprite kit (Assets/Resources/Sprites/UI/) + UITheme palette
/// to existing panels/buttons without requiring Editor re-wiring — same pattern
/// ConsumableBattleUI uses for its self-spawned UI (sliced sprite, flat-tint fallback).
/// </summary>
public static class UIThemeStyler
{
    public const string PanelWoodPath   = "Sprites/UI/Panels/panel_wood_generic";
    public const string PanelVictoryPath = "Sprites/UI/Panels/panel_victory";
    public const string PanelDefeatPath  = "Sprites/UI/Panels/panel_defeat";
    public const string ButtonPrimaryPath = "Sprites/UI/Buttons/button_primary";
    public const string ButtonDangerPath  = "Sprites/UI/Buttons/button_danger";
    public const string ButtonSmallPath   = "Sprites/UI/Buttons/button_small_action";

    public static UITheme LoadTheme() => Resources.Load<UITheme>("UI/CozyUITheme");

    /// <summary>
    /// Give a panel GameObject the wood (or custom) sliced background. If the panel
    /// has no Image, one is added so it also blocks clicks like other themed windows.
    /// Falls back to a flat woodDark tint when the sprite is missing from Resources.
    /// </summary>
    public static void StylePanel(GameObject panel, UITheme theme, string spritePath = PanelWoodPath)
    {
        if (panel == null) return;

        Image img = panel.GetComponent<Image>();
        if (img == null)
            img = panel.AddComponent<Image>();

        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            Color wood = theme != null ? theme.woodDark : new Color(0.1f, 0.1f, 0.15f);
            img.color = new Color(wood.r, wood.g, wood.b, 0.9f);
        }
    }

    /// <summary>
    /// Apply a themed sprite to a button and darken its TMP label so it stays
    /// readable on the gold/wood button art (see ConsumableBattleUI for rationale).
    ///
    /// If the button has no Image, one is added — same as StylePanel. A Button with no
    /// Graphic paints nothing AND cannot be raycast, so it is invisible *and* dead rather
    /// than merely unstyled; RelationshipUI's close button shipped that way. Adding the
    /// Image here fixes every caller instead of one.
    /// </summary>
    public static void StyleButton(Button button, UITheme theme, string spritePath = ButtonPrimaryPath)
    {
        if (button == null) return;

        Image img = button.GetComponent<Image>();
        if (img == null) img = button.gameObject.AddComponent<Image>();
        if (button.targetGraphic == null) button.targetGraphic = img;
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (img != null && sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            // A ColorTint button multiplies its colour block over the sprite, so a builder's
            // flat tint survives being given gold art and repaints it — the quests close button
            // stayed dark red under the gold plaque, with textDark on it unreadable. Setting the
            // Image white is not enough; the tint block has to be neutralised too. Kept as
            // ColorTint (not None) so the button still responds to hover and press.
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.color = theme != null ? theme.textDark : Color.black;
        }
        else if (img != null && theme != null)
        {
            img.color = theme.woodLight;
        }
    }

    /// <summary>
    /// Style one row of a runtime-populated list (shop entries, build options).
    ///
    /// Rows come from a prefab whose colours predate the theme — the building shop's rows are
    /// a hardcoded 0.15 grey with a flat green button, which reads as a different application
    /// sitting on top of the cream panel. Applied per row after Instantiate, since the prefab
    /// itself is Editor-owned.
    /// </summary>
    public static void StyleListRow(GameObject row, UITheme theme)
    {
        if (row == null || theme == null) return;

        Image background = row.GetComponent<Image>();
        if (background != null && background.sprite == null)
        {
            // Tan rather than the panel's cream, so rows stay distinguishable from the panel
            // they sit on without going dark.
            background.color = new Color(
                theme.backgroundTan.r, theme.backgroundTan.g, theme.backgroundTan.b, 0.9f);
        }

        // The row's labels were light-on-dark. Flipping the background without flipping these
        // would trade one unreadable row for another, so anything pale goes dark; colours that
        // carry meaning (cost, status) are darkened rather than replaced, keeping the hue.
        foreach (TextMeshProUGUI text in row.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.GetComponentInParent<Button>() != null) continue;   // StyleButton owns it

            // 0.45, not something gentler: the gold cost label only reaches a 3.43 contrast
            // ratio against the tan row at 0.55, under the 4.5 wanted for body text. At 0.45
            // it clears it (4.73) and the red status label stays well clear too (8.93).
            text.color = IsPale(text.color)
                ? theme.textDark
                : Darken(text.color, 0.45f);
        }

        foreach (Button button in row.GetComponentsInChildren<Button>(true))
            StyleButton(button, theme, ButtonPrimaryPath);
    }

    /// <summary>Near-white/grey, i.e. carrying no meaning beyond "readable on a dark row".</summary>
    private static bool IsPale(Color c)
    {
        const float spread = 0.12f;
        float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
        return max - min < spread && max > 0.6f;
    }

    /// <summary>Scale a colour toward black, preserving its hue.</summary>
    private static Color Darken(Color c, float factor)
    {
        return new Color(c.r * factor, c.g * factor, c.b * factor, c.a);
    }

    /// <summary>Tint a TMP text with a theme color, null-safe on both sides.</summary>
    public static void TintText(TextMeshProUGUI text, Color color)
    {
        if (text != null)
            text.color = color;
    }

    /// <summary>
    /// Recolour a panel's heading to cream so it reads against the wood art.
    ///
    /// A panel title sits on the sprite's border, where the dark label colour used on gold
    /// buttons disappears — the pause menu's "Game Menu" heading measured a 1.06 contrast
    /// ratio against woodDark, i.e. invisible.
    ///
    /// Cream rather than the gold used by saveSlotPanelTitle: a heading can land on any wood
    /// tone depending on the panel sprite, and gold only clears the WCAG 3.0 bar for large
    /// text on the darkest one (5.77 on woodDark, but 3.01 on woodLight). Cream holds up
    /// across the range — 7.59 / 5.24 / 3.96 on dark / mid / light.
    ///
    /// Found by name so a panel whose heading has no serialized field still gets themed,
    /// rather than needing a new Inspector reference wired by hand in every scene. Panels that
    /// do expose the field should keep calling <see cref="TintText"/> directly.
    /// </summary>
    /// <param name="nameHint">
    /// Substring matched case-insensitively against child object names, e.g. "Title".
    /// </param>
    public static void StylePanelTitle(GameObject panel, UITheme theme, string nameHint = "Title")
    {
        if (panel == null || theme == null) return;

        foreach (TextMeshProUGUI text in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            // Only direct headings — a title nested inside a button is that button's label,
            // and StyleButton has already given it the right colour for gold art.
            if (text.GetComponentInParent<Button>() != null) continue;

            if (text.gameObject.name.IndexOf(nameHint, System.StringComparison.OrdinalIgnoreCase) >= 0)
                text.color = theme.backgroundCream;
        }
    }
}

} // namespace SowurShield.UI
