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
    // A 6-step scale. An audit on 2026-08-04 found 21 distinct font sizes in use
    // across the screen-space UI (10,11,12,13,14,16,17,18,20,22,24,26,28,30,36,45
    // plus six autosize ranges) because every screen picked its own — which is why
    // the game read as "tudo de tamanho diferente". Pick the nearest step here
    // instead of inventing a new value.
    public float fontSizeH1      = 32f; // screen / panel title
    public float fontSizeH2      = 24f; // section heading
    public float fontSizeBody    = 18f; // dialogue and primary reading text
    public float fontSizeButton  = 18f; // control labels
    public float fontSizeSmall   = 14f; // secondary / supporting text
    public float fontSizeCaption = 12f; // hints, counters, timestamps

    [Header("Control Sizing")]
    // Controls must not stretch to whatever container they land in. A dialogue
    // choice expanded to 1750px wide reads as a progress bar, not a button.
    public float buttonHeight      = 44f;
    public float buttonHeightSmall = 34f;
    public float buttonMinWidth    = 160f;
    public float buttonMaxWidth    = 560f;

    /// <summary>
    /// Longest comfortable line for body copy, in reference pixels. Beyond roughly
    /// this width the eye loses the start of the next line, which is what made the
    /// full-bleed 1495px dialogue text feel wrong even though it technically fit.
    /// </summary>
    public float maxTextLineWidth = 900f;
}

} // namespace SowurShield.UI
