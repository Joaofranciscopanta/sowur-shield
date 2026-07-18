using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

namespace SowurShield.Editor
{

/// <summary>
/// Editor tool to rebuild the TeamAssemblerUI from scratch with correct layout.
/// Menu: Tools > Combat > Rebuild TeamAssembler UI
/// </summary>
public class TeamAssemblerUIBuilder : EditorWindow
{
    [MenuItem("Tools/Combat/Rebuild TeamAssembler UI")]
    public static void RebuildUI()
    {
        // Delete existing canvas if present
        var existing = GameObject.Find("TeamAssemblerCanvas");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild TeamAssembler UI",
                "This will DELETE the existing TeamAssemblerCanvas and recreate it.\nContinue?",
                "Yes, rebuild", "Cancel"))
                return;

            Undo.DestroyObjectImmediate(existing);
        }

        // ── Canvas ──────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("TeamAssemblerCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create TeamAssemblerCanvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── AssemblerPanel (full screen, dark overlay) ────────────────────────
        var assemblerPanel = CreateStretchPanel(canvasGO.transform, "AssemblerPanel",
            Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.9f));

        // Starts hidden
        assemblerPanel.SetActive(false);

        // ── ButtonContainer (bottom strip — 50px) ─────────────────────────────
        // Create this FIRST so it establishes the bottom anchor for other panels
        var buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(assemblerPanel.transform, false);
        var bcImg = buttonContainer.AddComponent<Image>();
        bcImg.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);
        var bcRT = buttonContainer.GetComponent<RectTransform>();
        bcRT.anchorMin = new Vector2(0, 0);
        bcRT.anchorMax = new Vector2(1, 0);
        bcRT.offsetMin = new Vector2(0, 0);
        bcRT.offsetMax = new Vector2(0, 50);
        var bcHLG = buttonContainer.AddComponent<HorizontalLayoutGroup>();
        bcHLG.spacing = 8;
        bcHLG.childForceExpandWidth = true;
        bcHLG.childForceExpandHeight = true;
        bcHLG.padding = new RectOffset(8, 8, 6, 6);

        var feedBtn   = CreateButton(buttonContainer.transform, "FeedAllButton",    "Feed All",    new Color(0.15f, 0.55f, 0.15f));
        var clearBtn  = CreateButton(buttonContainer.transform, "ClearGridButton",  "Clear Grid",  new Color(0.50f, 0.28f, 0.05f));
        var startBtn  = CreateButton(buttonContainer.transform, "StartBattleButton","Start Battle",new Color(0.65f, 0.08f, 0.08f));
        var cancelBtn = CreateButton(buttonContainer.transform, "CancelButton",     "Cancel",      new Color(0.25f, 0.25f, 0.25f));

        // ── Left panel: AnimalSelectionPanel (0 → 40%) ───────────────────────
        var animalPanel = CreateStretchPanel(assemblerPanel.transform, "AnimalSelectionPanel",
            new Vector2(0f, 0f), new Vector2(0.40f, 1f),
            new Color(0.10f, 0.10f, 0.10f, 0.85f));
        {
            var rt = animalPanel.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(8,  50);  // leave room for button bar
            rt.offsetMax = new Vector2(-3, -8);
        }

        // Title label
        CreateTMPText(animalPanel.transform, "TitleText", "Available Animals",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(8, -28), new Vector2(-8, 0), 18, FontStyles.Bold);

        // ScrollView filling the rest of the panel below title
        var scrollGO = new GameObject("AnimalScrollView");
        scrollGO.transform.SetParent(animalPanel.transform, false);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(4, 4);
        scrollRT.offsetMax = new Vector2(-4, -32);

        // Viewport
        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        viewportGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        viewportGO.AddComponent<RectMask2D>();
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        scrollRect.viewport = viewportRT;

        // Content (grows downward)
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        if (contentRT == null) contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        // ── Middle panel: GridPanel (40% → 72%) ──────────────────────────────
        var gridPanel = CreateStretchPanel(assemblerPanel.transform, "GridPanel",
            new Vector2(0.40f, 0f), new Vector2(0.72f, 1f),
            new Color(0.07f, 0.07f, 0.07f, 0.85f));
        {
            var rt = gridPanel.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(3, 50);
            rt.offsetMax = new Vector2(-3, -8);
        }

        // GridContainer — centered, sized for 3 col × 5 row @ 90px cells + 6px gaps
        // Width  = 3*90 + 2*6 = 282
        // Height = 5*90 + 4*6 = 474
        var gridContainerGO = new GameObject("GridContainer");
        gridContainerGO.transform.SetParent(gridPanel.transform, false);
        gridContainerGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var gcRT = gridContainerGO.GetComponent<RectTransform>();
        gcRT.anchorMin = new Vector2(0.5f, 0.5f);
        gcRT.anchorMax = new Vector2(0.5f, 0.5f);
        gcRT.pivot     = new Vector2(0.5f, 0.5f);
        gcRT.anchoredPosition = Vector2.zero;
        gcRT.sizeDelta = new Vector2(282, 474);
        var glg = gridContainerGO.AddComponent<GridLayoutGroup>();
        glg.cellSize        = new Vector2(90, 90);
        glg.spacing         = new Vector2(6, 6);
        glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment  = TextAnchor.UpperCenter;
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 3;

        // ── Right panel: InfoPanel (72% → 100%) ──────────────────────────────
        var infoPanel = CreateStretchPanel(assemblerPanel.transform, "InfoPanel",
            new Vector2(0.72f, 0f), new Vector2(1f, 1f),
            new Color(0.10f, 0.10f, 0.10f, 0.85f));
        {
            var rt = infoPanel.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(3,  50);
            rt.offsetMax = new Vector2(-8, -8);
        }

        var infoVLG = infoPanel.AddComponent<VerticalLayoutGroup>();
        infoVLG.spacing = 12;
        infoVLG.childForceExpandWidth  = true;
        infoVLG.childForceExpandHeight = false;
        infoVLG.childAlignment = TextAnchor.UpperLeft;
        infoVLG.padding = new RectOffset(12, 12, 16, 8);

        var zoneNameText   = CreateTMPText(infoPanel.transform, "ZoneNameText",          "Zone: Unknown",     fontSize: 16, style: FontStyles.Bold);
        var teamSizeText   = CreateTMPText(infoPanel.transform, "TeamSizeText",           "Team: 0/15",        fontSize: 13);
        var foodReqText    = CreateTMPText(infoPanel.transform, "FoodRequirementsText",   "All animals fed!",  fontSize: 11);
        var synergiesText  = CreateTMPText(infoPanel.transform, "SynergiesText",          "Synergies: TBD",    fontSize: 11);

        // ── Assign TeamAssemblerUI script ─────────────────────────────────────
        var uiScript = canvasGO.AddComponent<SowurShield.Combat.TeamAssemblerUI>();

        var so = new SerializedObject(uiScript);
        so.FindProperty("assemblerPanel").objectReferenceValue       = assemblerPanel;
        so.FindProperty("animalSelectionPanel").objectReferenceValue = animalPanel;
        so.FindProperty("animalCardContainer").objectReferenceValue  = contentRT;
        so.FindProperty("gridPanel").objectReferenceValue            = gridPanel;
        so.FindProperty("gridContainer").objectReferenceValue        = gcRT;
        so.FindProperty("zoneNameText").objectReferenceValue         = zoneNameText;
        so.FindProperty("teamSizeText").objectReferenceValue         = teamSizeText;
        so.FindProperty("foodRequirementsText").objectReferenceValue = foodReqText;
        so.FindProperty("synergiesText").objectReferenceValue        = synergiesText;
        so.FindProperty("feedAllButton").objectReferenceValue        = feedBtn;
        so.FindProperty("clearGridButton").objectReferenceValue      = clearBtn;
        so.FindProperty("startBattleButton").objectReferenceValue    = startBtn;
        so.FindProperty("cancelButton").objectReferenceValue         = cancelBtn;
        so.FindProperty("combatSceneName").stringValue               = "CombatScene";
        // Grid settings: 9 wide, 5 tall, 3 player columns (rightmost)
        so.FindProperty("gridWidth").intValue   = 9;
        so.FindProperty("gridHeight").intValue  = 5;
        so.FindProperty("playerColumns").intValue = 3;
        so.ApplyModifiedProperties();

        Debug.Log("[TeamAssemblerUIBuilder] TeamAssemblerCanvas created. Assign GridSlotPrefab & AnimalCardPrefab in Inspector.");

        Selection.activeGameObject = canvasGO;
        EditorUtility.DisplayDialog("Done!",
            "TeamAssemblerCanvas created!\n\nStill need in Inspector on TeamAssemblerUI:\n• Grid Slot Prefab\n• Animal Card Prefab",
            "OK");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>Full-stretch panel (anchors set via offsetMin/Max after creation)</summary>
    private static GameObject CreateStretchPanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    /// <summary>TMP text with explicit anchor/offset (for non-layout children)</summary>
    private static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        float fontSize = 14, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return tmp;
    }

    /// <summary>TMP text as VerticalLayoutGroup child (LayoutElement drives height)</summary>
    private static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text,
        float fontSize = 14, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize * 2.8f;
        le.flexibleWidth   = 1;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgColor;
        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.highlightedColor = new Color(
            Mathf.Min(bgColor.r + 0.25f, 1f),
            Mathf.Min(bgColor.g + 0.25f, 1f),
            Mathf.Min(bgColor.b + 0.25f, 1f));
        cb.pressedColor = new Color(
            Mathf.Max(bgColor.r - 0.15f, 0f),
            Mathf.Max(bgColor.g - 0.15f, 0f),
            Mathf.Max(bgColor.b - 0.15f, 0f));
        btn.colors = cb;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        return btn;
    }
}

} // namespace SowurShield.Editor
