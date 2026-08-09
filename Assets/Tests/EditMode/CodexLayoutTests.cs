using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.UI;
using SowurShield.Dialogue;

namespace SowurShield.Tests
{

/// <summary>
/// Guards the codex panel's layout against the two defects found on 2026-08-09, both of
/// which were invisible to every check the project had.
///
/// The panel is drawn on panel_wood_generic, whose painted frame covers roughly an eighth
/// of the sprite per side — about 90px at the 720px panel width. Layout padding smaller
/// than that band puts text ON the wood: it still measures perfectly inside the panel's
/// rect, every RectTransform is on screen, no warning is logged, and the screen is simply
/// unreadable. A geometric sweep for zero-size or off-screen rects passes it every time.
/// Only a screenshot caught it.
///
/// So these tests assert the thing the rect check cannot: that the readable area is inset
/// past the frame art, and that the type scale is actually used rather than re-invented.
/// </summary>
public class CodexLayoutTests
{
    private const float PanelWidth = 720f;
    private const float FrameRatio = 0.125f;   // matches RelationshipUI
    private static float FrameBand => PanelWidth * FrameRatio;

    private GameObject host;
    private RelationshipUI ui;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("RelationshipUIHost", typeof(RectTransform));
        var canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ui = host.AddComponent<RelationshipUI>();
    }

    [TearDown]
    public void TearDown()
    {
        if (host != null) Object.DestroyImmediate(host);
    }

    private const System.Reflection.BindingFlags Priv =
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

    /// <summary>
    /// Builds the panel through the component's own path, so the test exercises the real
    /// construction code rather than a copy of it.
    ///
    /// The lore rows have to be added explicitly: TryBuildUI only creates the empty
    /// container, and the four smallest sizes in the old panel (the lore titles and bodies
    /// at 12 and 11) live in CreateLoreRow. A test that skipped them passed happily with
    /// an 11px body still in place — verified by reintroducing exactly that.
    /// </summary>
    private RectTransform BuildPanel(bool withLoreRows = true)
    {
        var build = typeof(RelationshipUI).GetMethod("TryBuildUI", Priv);
        Assert.IsNotNull(build, "TryBuildUI is gone — this test needs updating alongside it.");
        build.Invoke(ui, null);

        var panel = host.transform.Find("RelationshipPanel") as RectTransform;
        Assert.IsNotNull(panel, "The codex panel was not built.");

        if (withLoreRows)
        {
            var createRow = typeof(RelationshipUI).GetMethod("CreateLoreRow", Priv);
            Assert.IsNotNull(createRow, "CreateLoreRow is gone — this test needs updating.");

            var entry = new NpcLoreEntry
            {
                title = "A Seca do Vale Oriental",
                body  = "Maren perdeu tres colheitas seguidas para uma seca devastadora.",
                requiredRelationship = 40f,
            };

            // One of each: unlocked rows show the body, locked rows the requirement, and
            // they take different height floors.
            createRow.Invoke(ui, new object[] { entry, false });
            createRow.Invoke(ui, new object[] { entry, true });
        }

        return panel;
    }

    [Test]
    public void CodexPadding_ClearsTheWoodFrameOnEverySide()
    {
        var panel = BuildPanel();
        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        Assert.IsNotNull(vlg, "The panel lost its VerticalLayoutGroup.");

        // The bottom was 74 and the top 78 against a ~90px band, which is exactly how the
        // close button and the tastes list came to be drawn on the painted wood.
        var sides = new Dictionary<string, int>
        {
            { "left",   vlg.padding.left },
            { "right",  vlg.padding.right },
            { "top",    vlg.padding.top },
            { "bottom", vlg.padding.bottom },
        };

        foreach (var side in sides)
        {
            Assert.GreaterOrEqual(side.Value, FrameBand,
                $"Codex padding.{side.Key} is {side.Value}px, inside the ~{FrameBand:F0}px " +
                "frame band. Text there renders on top of the wood art and is unreadable, " +
                "even though it measures inside the panel rect.");
        }
    }

    [Test]
    public void CodexFontSizes_AllComeFromTheThemeTypeScale()
    {
        var theme = Resources.Load<UITheme>("UI/CozyUITheme");
        Assert.IsNotNull(theme, "CozyUITheme is missing — the codex would fall back to literals.");

        var panel = BuildPanel();

        // The panel carried nine hand-picked sizes (22/18/15/14/13/13/13/12/11), four of them
        // below 13px. Anything not on the scale is a size someone invented locally.
        var scale = new HashSet<float>
        {
            theme.fontSizeH1, theme.fontSizeH2, theme.fontSizeBody,
            theme.fontSizeButton, theme.fontSizeSmall, theme.fontSizeCaption,
        };

        var offScale = new List<string>();
        foreach (var label in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (!scale.Contains(label.fontSize))
                offScale.Add($"{label.gameObject.name}={label.fontSize}");
        }

        Assert.IsEmpty(offScale,
            "Codex labels using a font size outside UITheme's scale " +
            $"({string.Join("/", scale.OrderByDescending(s => s))}): {string.Join(", ", offScale)}. " +
            "Pick the nearest step instead of introducing a new size.");
    }

    [Test]
    public void CodexBodyText_IsNeverSmallerThanTheCaptionStep()
    {
        var theme = Resources.Load<UITheme>("UI/CozyUITheme");
        Assert.IsNotNull(theme);

        var panel = BuildPanel();

        // Caption (12) is the floor for anything a player is expected to read. The codex had
        // lore bodies at 11 and titles at 12, which is what made it read as a different,
        // smaller screen than the rest of the game.
        var tooSmall = panel.GetComponentsInChildren<TextMeshProUGUI>(true)
            .Where(l => l.fontSize < theme.fontSizeCaption)
            .Select(l => $"{l.gameObject.name}={l.fontSize}")
            .ToList();

        Assert.IsEmpty(tooSmall,
            $"Codex labels below the {theme.fontSizeCaption}px caption step: " +
            string.Join(", ", tooSmall));
    }
}

} // namespace SowurShield.Tests
