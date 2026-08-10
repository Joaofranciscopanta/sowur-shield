using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.Worldmap;

namespace SowurShield.Tests
{

/// <summary>
/// Guards the world map's stage buttons against the two defects found on 2026-08-09.
///
/// The audit recorded this screen as "28% of the map is empty", which was true but was the
/// symptom rather than the fault: the 5x5 grid measures 710px inside a 1080px map and a fixed
/// top-left origin pinned it high, leaving 300px below and 70px above. Centring it fixes the
/// imbalance without covering the illustration, which is what the empty band actually is.
///
/// The worse defect was invisible to any position check. Unity's Button tints the same Image
/// the script colours, and its default disabledColor carries alpha 0.502 — so a locked stage
/// button is a half-transparent panel with the illustrated map showing through it, and the
/// dark label measured 1.5:1 against what the player actually sees. Locked stage names were
/// unreadable. These tests pin the contrast, not the appearance.
/// </summary>
public class WorldMapLayoutTests
{
    private const float ContrastFloor = 4.5f;

    /// <summary>WCAG relative luminance.</summary>
    private static double Luminance(Color c)
    {
        System.Func<double, double> ch = v => v <= 0.03928 ? v / 12.92 : Mathf.Pow((float)((v + 0.055) / 1.055), 2.4f);
        return 0.2126 * ch(c.r) + 0.7152 * ch(c.g) + 0.0722 * ch(c.b);
    }

    private static double Contrast(Color text, Color background)
    {
        double l1 = Luminance(text), l2 = Luminance(background);
        return (System.Math.Max(l1, l2) + 0.05) / (System.Math.Min(l1, l2) + 0.05);
    }

    private GameObject host;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("StageButtonHost", typeof(RectTransform), typeof(Image), typeof(Button));

        var labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(host.transform, false);
        labelObj.AddComponent<TextMeshProUGUI>();
    }

    [TearDown]
    public void TearDown()
    {
        if (host != null) Object.DestroyImmediate(host);
    }

    /// <summary>
    /// Reads the value the component will actually run with, which is not necessarily the one
    /// written in the source. These are [SerializeField] fields, so once a scene or prefab has
    /// stored a value, editing the C# default changes nothing for existing instances — the
    /// reason the first attempt at this fix appeared to do nothing at all.
    /// </summary>
    private static Color SerializedColor(StageButton button, string fieldName)
    {
        var so = new UnityEditor.SerializedObject(button);
        var prop = so.FindProperty(fieldName);
        Assert.IsNotNull(prop, $"StageButton has no serialized field '{fieldName}'.");
        return prop.colorValue;
    }

    [Test]
    public void LockedStageButton_UsesAnOpaqueDisabledTint()
    {
        var button = host.AddComponent<StageButton>();
        Color tint = SerializedColor(button, "lockedDisabledTint");

        // Unity's stock disabledColor is (0.784, 0.784, 0.784, 0.502). Anything translucent
        // lets the map's foliage through the panel and under the text.
        Assert.AreEqual(1f, tint.a, 0.001f,
            $"Locked stage buttons must be opaque; alpha is {tint.a}. A translucent panel puts " +
            "the illustrated map directly behind the stage name.");
    }

    [Test]
    public void LockedStageLabel_ClearsTheContrastFloorOnItsOwnPanel()
    {
        var button = host.AddComponent<StageButton>();

        Color ink   = SerializedColor(button, "lockedTextColor");
        Color panel = SerializedColor(button, "lockedDisabledTint");

        double ratio = Contrast(ink, panel);
        Assert.GreaterOrEqual(ratio, ContrastFloor,
            $"Locked stage label measures {ratio:F2}:1 against its own panel, below {ContrastFloor}:1. " +
            "Both are light, so the ink has to be dark — going lighter to 'fade' a locked entry " +
            "is what made this unreadable in the first place.");
    }

    [Test]
    public void UnlockedStageLabel_ClearsTheContrastFloorOnWhite()
    {
        var button = host.AddComponent<StageButton>();
        Color ink = SerializedColor(button, "unlockedTextColor");

        double ratio = Contrast(ink, Color.white);
        Assert.GreaterOrEqual(ratio, ContrastFloor,
            $"Unlocked stage label measures {ratio:F2}:1 on its white panel.");
    }

    [Test]
    public void LockedAndUnlockedLabels_StayVisuallyDistinct()
    {
        var button = host.AddComponent<StageButton>();

        Color locked   = SerializedColor(button, "lockedTextColor");
        Color unlocked = SerializedColor(button, "unlockedTextColor");

        // Both are dark for legibility, but a player still has to be able to tell a locked
        // stage from an available one at a glance. The panel behind them differs too; this
        // only asserts the inks were not collapsed into the same value.
        Assert.AreNotEqual(unlocked, locked,
            "Locked and unlocked stage labels use the same colour, so the two states are " +
            "indistinguishable by text.");
    }
}

} // namespace SowurShield.Tests
