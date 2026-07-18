using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using SowurShield.Dialogue;
using SowurShield.UI;

namespace SowurShield.Editor
{

/// <summary>
/// Editor tool to build the QuestsUI canvas (Active + Completed tabs) and a persistent
/// HUD toggle button, from scratch with correct layout and wiring.
/// Menu: Tools > Sowur Shield > Rebuild Quests UI
/// </summary>
public class QuestsUIBuilder : EditorWindow
{
    private static UITheme LoadTheme() => Resources.Load<UITheme>("UI/CozyUITheme");

    [MenuItem("Tools/Sowur Shield/Rebuild Quests UI")]
    public static void RebuildUI()
    {
        var existing = GameObject.Find("QuestsCanvas");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild Quests UI",
                "This will DELETE the existing QuestsCanvas and recreate it.\nContinue?",
                "Yes, rebuild", "Cancel"))
                return;

            Undo.DestroyObjectImmediate(existing);
        }

        UITheme theme = LoadTheme();
        Color backgroundCream = theme != null ? theme.backgroundCream : new Color(0.97f, 0.95f, 0.91f);
        Color highlightGold = theme != null ? theme.highlightGold : new Color(0.96f, 0.83f, 0.37f);
        Color backgroundTan = theme != null ? theme.backgroundTan : new Color(0.94f, 0.89f, 0.75f);
        Color textDark = theme != null ? theme.textDark : new Color(0.18f, 0.17f, 0.15f);

        // ── Canvas ──────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("QuestsCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create QuestsCanvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 44; // below BuildingShopCanvas (45) so it never competes with shop modals

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── QuestsPanel (centered card) ─────────────────────────────────────────
        var questsPanel = CreateStretchPanel(canvasGO.transform, "QuestsPanel",
            new Vector2(0.22f, 0.12f), new Vector2(0.78f, 0.88f), backgroundCream);
        questsPanel.SetActive(false);

        CreateTMPText(questsPanel.transform, "TitleText", "Quests",
            new Vector2(0, 1), new Vector2(0.7f, 1),
            new Vector2(16, -44), new Vector2(0, -8), 22, FontStyles.Bold, textDark, noWrap: true);

        var closeBtn = CreateButton(questsPanel.transform, "CloseButton", "X", new Color(0.5f, 0.15f, 0.15f));
        {
            var rt = closeBtn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-8, -8);
            rt.sizeDelta = new Vector2(32, 32);
        }

        // ── Tab row ──────────────────────────────────────────────────────────────
        var tabRow = new GameObject("TabRow");
        tabRow.transform.SetParent(questsPanel.transform, false);
        var tabRowRT = tabRow.AddComponent<RectTransform>();
        tabRowRT.anchorMin = new Vector2(0, 1);
        tabRowRT.anchorMax = new Vector2(1, 1);
        tabRowRT.pivot = new Vector2(0.5f, 1);
        tabRowRT.anchoredPosition = new Vector2(0, -52);
        tabRowRT.sizeDelta = new Vector2(-32, 36);
        var tabHLG = tabRow.AddComponent<HorizontalLayoutGroup>();
        tabHLG.spacing = 8;
        tabHLG.childForceExpandWidth = true;
        tabHLG.childForceExpandHeight = true;

        var activeTabBtn = CreateButton(tabRow.transform, "ActiveTabButton", "Active", highlightGold);
        var completedTabBtn = CreateButton(tabRow.transform, "CompletedTabButton", "Completed", backgroundTan);

        // ── Active tab panel + scroll list ─────────────────────────────────────
        var activeTabPanel = CreateStretchPanel(questsPanel.transform, "ActiveTabPanel",
            new Vector2(0, 0), new Vector2(1, 1), new Color(0, 0, 0, 0));
        {
            var rt = activeTabPanel.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(12, 12);
            rt.offsetMax = new Vector2(-12, -96);
        }

        var activeEmptyText = CreateTMPText(activeTabPanel.transform, "ActiveEmptyText", "No active quests yet.",
            new Vector2(0, 0.4f), new Vector2(1, 0.6f), Vector2.zero, Vector2.zero, 16, FontStyles.Italic, textDark);
        activeEmptyText.alignment = TextAlignmentOptions.Center;

        Transform activeContent = CreateScrollList(activeTabPanel.transform, "ActiveScrollView");

        // ── Completed tab panel + scroll list ──────────────────────────────────
        var completedTabPanel = CreateStretchPanel(questsPanel.transform, "CompletedTabPanel",
            new Vector2(0, 0), new Vector2(1, 1), new Color(0, 0, 0, 0));
        {
            var rt = completedTabPanel.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(12, 12);
            rt.offsetMax = new Vector2(-12, -96);
        }
        completedTabPanel.SetActive(false);

        var completedEmptyText = CreateTMPText(completedTabPanel.transform, "CompletedEmptyText", "Complete quests to fill your journal.",
            new Vector2(0, 0.4f), new Vector2(1, 0.6f), Vector2.zero, Vector2.zero, 16, FontStyles.Italic, textDark);
        completedEmptyText.alignment = TextAlignmentOptions.Center;

        Transform completedContent = CreateScrollList(completedTabPanel.transform, "CompletedScrollView");

        // ── Row prefabs ─────────────────────────────────────────────────────────
        GameObject activeRowPrefab = CreateActiveQuestRowPrefab(theme);
        GameObject completedRowPrefab = CreateCompletedQuestRowPrefab();

        // ── Assign QuestsUI script ─────────────────────────────────────────────
        var uiScript = canvasGO.AddComponent<QuestsUI>();

        var so = new SerializedObject(uiScript);
        so.FindProperty("questsPanel").objectReferenceValue = questsPanel;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
        so.FindProperty("activeTabButton").objectReferenceValue = activeTabBtn;
        so.FindProperty("completedTabButton").objectReferenceValue = completedTabBtn;
        so.FindProperty("activeTabPanel").objectReferenceValue = activeTabPanel;
        so.FindProperty("completedTabPanel").objectReferenceValue = completedTabPanel;
        so.FindProperty("activeListContainer").objectReferenceValue = activeContent;
        so.FindProperty("activeQuestRowPrefab").objectReferenceValue = activeRowPrefab;
        so.FindProperty("activeEmptyText").objectReferenceValue = activeEmptyText;
        so.FindProperty("completedListContainer").objectReferenceValue = completedContent;
        so.FindProperty("completedQuestRowPrefab").objectReferenceValue = completedRowPrefab;
        so.FindProperty("completedEmptyText").objectReferenceValue = completedEmptyText;
        so.ApplyModifiedProperties();

        Debug.Log("[QuestsUIBuilder] QuestsCanvas created and wired.");

        Selection.activeGameObject = canvasGO;
        EditorUtility.DisplayDialog("Done!",
            "QuestsCanvas created!\n\nPress J at any time to open/close the quest log " +
            "(change the key via the toggleKey field on the QuestsUI component).",
            "OK");
    }

    // ── Row prefab creation ──────────────────────────────────────────────────

    private static GameObject CreateActiveQuestRowPrefab(UITheme theme)
    {
        const string folder = "Assets/Resources/Prefabs/UI";
        const string path = folder + "/QuestActiveRow.prefab";
        const string lineFolder = folder;
        const string linePath = lineFolder + "/QuestObjectiveLine.prefab";
        EnsureResourcesUIFolder();

        Color textDark = theme != null ? theme.textDark : new Color(0.18f, 0.17f, 0.15f);
        Color highlightGold = theme != null ? theme.highlightGold : new Color(0.96f, 0.83f, 0.37f);

        // Objective line sub-prefab (just a single TMP text, instantiated dynamically per objective)
        var lineGO = new GameObject("QuestObjectiveLine");
        var lineRT = lineGO.AddComponent<RectTransform>();
        lineRT.sizeDelta = new Vector2(0, 22);
        var lineTMP = lineGO.AddComponent<TextMeshProUGUI>();
        lineTMP.fontSize = 13;
        lineTMP.color = textDark;
        var lineLE = lineGO.AddComponent<LayoutElement>();
        lineLE.preferredHeight = 22;
        lineLE.flexibleWidth = 1;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(linePath) != null)
            AssetDatabase.DeleteAsset(linePath);
        GameObject linePrefab = PrefabUtility.SaveAsPrefabAsset(lineGO, linePath);
        Object.DestroyImmediate(lineGO);

        // Active quest row
        var rowGO = new GameObject("QuestActiveRow");
        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(700, 140);
        rowGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.5f);
        var rowLE = rowGO.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 140;
        rowLE.minWidth = 400;
        rowLE.flexibleWidth = 1;

        var titleText = CreateTMPText(rowGO.transform, "TitleText", "Quest Title",
            new Vector2(0, 0.78f), new Vector2(1, 1),
            new Vector2(12, -4), new Vector2(-12, -4), 16, FontStyles.Bold, textDark, noWrap: true);

        var descText = CreateTMPText(rowGO.transform, "DescriptionText", "Quest description",
            new Vector2(0, 0.6f), new Vector2(1, 0.78f),
            new Vector2(12, 0), new Vector2(-12, 0), 12, FontStyles.Italic, textDark);

        // Progress bar (simple filled Image)
        var barBgGO = new GameObject("ProgressBarBg");
        barBgGO.transform.SetParent(rowGO.transform, false);
        barBgGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.15f);
        var barBgRT = barBgGO.GetComponent<RectTransform>();
        barBgRT.anchorMin = new Vector2(0, 0.5f);
        barBgRT.anchorMax = new Vector2(1, 0.6f);
        barBgRT.offsetMin = new Vector2(12, 0);
        barBgRT.offsetMax = new Vector2(-12, 0);

        var barFillGO = new GameObject("ProgressBarFill");
        barFillGO.transform.SetParent(barBgGO.transform, false);
        var barFillImg = barFillGO.AddComponent<Image>();
        barFillImg.color = highlightGold;
        barFillImg.type = Image.Type.Filled;
        barFillImg.fillMethod = Image.FillMethod.Horizontal;
        var barFillRT = barFillGO.GetComponent<RectTransform>();
        barFillRT.anchorMin = Vector2.zero;
        barFillRT.anchorMax = Vector2.one;
        barFillRT.offsetMin = Vector2.zero;
        barFillRT.offsetMax = Vector2.zero;

        // Objective container (objective lines instantiated dynamically below the bar)
        var objContainerGO = new GameObject("ObjectiveContainer");
        objContainerGO.transform.SetParent(rowGO.transform, false);
        var objRT = objContainerGO.AddComponent<RectTransform>();
        objRT.anchorMin = new Vector2(0, 0);
        objRT.anchorMax = new Vector2(1, 0.48f);
        objRT.offsetMin = new Vector2(12, 4);
        objRT.offsetMax = new Vector2(-12, 0);
        var objVLG = objContainerGO.AddComponent<VerticalLayoutGroup>();
        objVLG.spacing = 2;
        objVLG.childForceExpandWidth = true;
        objVLG.childForceExpandHeight = false;

        var rowComponent = rowGO.AddComponent<QuestActiveRow>();
        rowComponent.titleText = titleText;
        rowComponent.descriptionText = descText;
        rowComponent.progressBar = barFillImg;
        rowComponent.objectiveContainer = objRT;
        rowComponent.objectiveLinePrefab = linePrefab;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rowGO, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Object.DestroyImmediate(rowGO);

        return prefab;
    }

    private static GameObject CreateCompletedQuestRowPrefab()
    {
        const string folder = "Assets/Resources/Prefabs/UI";
        const string path = folder + "/QuestCompletedRow.prefab";
        EnsureResourcesUIFolder();

        var rowGO = new GameObject("QuestCompletedRow");
        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(700, 80);
        rowGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);
        var rowLE = rowGO.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 80;
        rowLE.minWidth = 400;
        rowLE.flexibleWidth = 1;

        var titleText = CreateTMPText(rowGO.transform, "TitleText", "Quest Title",
            new Vector2(0, 0.5f), new Vector2(1, 1),
            new Vector2(12, -4), new Vector2(-12, -4), 15, FontStyles.Bold, Color.black, noWrap: true);

        var descText = CreateTMPText(rowGO.transform, "DescriptionText", "Quest description",
            new Vector2(0, 0), new Vector2(1, 0.5f),
            new Vector2(12, 4), new Vector2(-12, 0), 12, FontStyles.Normal, new Color(0.3f, 0.3f, 0.3f));

        var rowComponent = rowGO.AddComponent<QuestCompletedRow>();
        rowComponent.titleText = titleText;
        rowComponent.descriptionText = descText;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rowGO, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Object.DestroyImmediate(rowGO);

        return prefab;
    }

    private static void EnsureResourcesUIFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Resources/Prefabs", "UI");
    }

    // ── Scroll list helper ───────────────────────────────────────────────────

    private static Transform CreateScrollList(Transform parent, string name)
    {
        var scrollGO = new GameObject(name);
        scrollGO.transform.SetParent(parent, false);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;

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

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        if (contentRT == null) contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        return contentRT;
    }

    // ── Generic helpers (mirrors BuildingShopUIBuilder's style) ──────────────

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

    private static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        float fontSize, FontStyles style, Color color, bool noWrap = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.textWrappingMode = noWrap ? TMPro.TextWrappingModes.NoWrap : TMPro.TextWrappingModes.Normal;
        if (noWrap) tmp.overflowMode = TMPro.TextOverflowModes.Ellipsis;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgColor;
        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = bgColor;
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
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
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
