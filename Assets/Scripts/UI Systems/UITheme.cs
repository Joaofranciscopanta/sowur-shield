using UnityEngine;

namespace SowurShield.UI
{

/// <summary>
/// Centralized "cozy" UI design tokens (colors, spacing, typography) shared
/// across combat and farm UI. Create via Assets > Create > Sowur Shield > UI Theme,
/// then assign the shared instance (Assets/Resources/UI/CozyUITheme.asset) to
/// UI controllers that need themed colors/sizes at runtime.
/// </summary>
[CreateAssetMenu(fileName = "UITheme", menuName = "Sowur Shield/UI Theme")]
public class UITheme : ScriptableObject
{
    [Header("Background")]
    public Color backgroundCream = new Color(0.969f, 0.949f, 0.910f); // #F7F2E8
    public Color backgroundTan   = new Color(0.937f, 0.890f, 0.753f); // #EFE3C0

    [Header("Wood Tones")]
    public Color woodLight = new Color(0.651f, 0.416f, 0.247f); // #A66A3F
    public Color woodMid   = new Color(0.545f, 0.353f, 0.169f); // #8B5A2B
    public Color woodDark  = new Color(0.420f, 0.267f, 0.137f); // #6B4423

    [Header("Highlights")]
    public Color highlightGold   = new Color(0.957f, 0.827f, 0.369f); // #F4D35E
    public Color highlightYellow = new Color(1.000f, 0.820f, 0.400f); // #FFD166

    [Header("Status")]
    public Color positive = new Color(0.506f, 0.784f, 0.518f); // #81C784
    public Color warning  = new Color(1.000f, 0.722f, 0.302f); // #FFB74D
    public Color negative = new Color(0.898f, 0.451f, 0.451f); // #E57373

    [Header("Text")]
    public Color textDark = new Color(0.176f, 0.165f, 0.149f); // #2D2A26

    [Header("Spacing Scale")]
    public float spacingXS = 4f;
    public float spacingS  = 8f;
    public float spacingM  = 12f;
    public float spacingL  = 16f;
    public float spacingXL = 24f;
    public float spacingXXL = 32f;

    [Header("Typography")]
    public float fontSizeH1   = 32f;
    public float fontSizeH2   = 24f;
    public float fontSizeBody = 16f;
}

} // namespace SowurShield.UI
