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
    /// </summary>
    public static void StyleButton(Button button, UITheme theme, string spritePath = ButtonPrimaryPath)
    {
        if (button == null) return;

        Image img = button.GetComponent<Image>();
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (img != null && sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.color = theme != null ? theme.textDark : Color.black;
        }
        else if (img != null && theme != null)
        {
            img.color = theme.woodLight;
        }
    }

    /// <summary>Tint a TMP text with a theme color, null-safe on both sides.</summary>
    public static void TintText(TextMeshProUGUI text, Color color)
    {
        if (text != null)
            text.color = color;
    }
}

} // namespace SowurShield.UI
