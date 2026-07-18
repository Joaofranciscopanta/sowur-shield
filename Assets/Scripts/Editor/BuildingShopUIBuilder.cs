using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using SowurShield.Core;
using SowurShield.Dialogue;

namespace SowurShield.Editor
{

/// <summary>
/// Editor tool to build the BuildingShopUI canvas (and an interactable NPC to open it)
/// from scratch with correct layout and wiring.
/// Menu: Tools > Sowur Shield > Rebuild Building Shop UI
///       Tools > Sowur Shield > Create Building Shop NPC
/// </summary>
public class BuildingShopUIBuilder : EditorWindow
{
    [MenuItem("Tools/Sowur Shield/Ensure Farm Building Manager")]
    public static void EnsureFarmBuildingManagerMenuItem()
    {
        bool created = EnsureFarmBuildingManager();

        if (created)
        {
            EditorUtility.DisplayDialog("Done!",
                "FarmBuildingManager created.\n\n" +
                "Without this component in the scene, FarmBuildingManager.Instance stays " +
                "null and every building purchase fails with 'missing references' — " +
                "this is the singleton BuildingShopUI/SoilBlockInteractable query to check " +
                "which upgrades (Silo, Workshop, Barn, Greenhouse) are built.",
                "OK");
        }
        else
        {
            Debug.Log("[BuildingShopUIBuilder] FarmBuildingManager already present in the scene — nothing to do.");
        }
    }

    /// <summary>Creates a FarmBuildingManager GameObject if one isn't already in the scene. Returns true if created.</summary>
    private static bool EnsureFarmBuildingManager()
    {
        var manager = Object.FindFirstObjectByType<FarmBuildingManager>();
        if (manager != null)
        {
            Selection.activeGameObject = manager.gameObject;
            return false;
        }

        var managerGO = new GameObject("FarmBuildingManager");
        Undo.RegisterCreatedObjectUndo(managerGO, "Create FarmBuildingManager");
        managerGO.AddComponent<FarmBuildingManager>();

        Selection.activeGameObject = managerGO;
        EditorGUIUtility.PingObject(managerGO);
        return true;
    }

    [MenuItem("Tools/Sowur Shield/Rebuild Building Shop UI")]
    public static void RebuildUI()
    {
        EnsureFarmBuildingManager();

        var existing = GameObject.Find("BuildingShopCanvas");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild Building Shop UI",
                "This will DELETE the existing BuildingShopCanvas and recreate it.\nContinue?",
                "Yes, rebuild", "Cancel"))
                return;

            Undo.DestroyObjectImmediate(existing);
        }

        // ── Canvas ──────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("BuildingShopCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create BuildingShopCanvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── BuildingPanel (centered card, ~60% width, 75% height) ─────────────
        var buildingPanel = CreateStretchPanel(canvasGO.transform, "BuildingPanel",
            new Vector2(0.18f, 0.10f), new Vector2(0.82f, 0.90f),
            new Color(0.08f, 0.08f, 0.08f, 0.96f));
        buildingPanel.SetActive(false);

        // Title bar
        CreateTMPText(buildingPanel.transform, "TitleText", "Farm Buildings",
            new Vector2(0, 1), new Vector2(0.7f, 1),
            new Vector2(16, -48), new Vector2(0, -8), 22, FontStyles.Bold, noWrap: true);

        var goldText = CreateTMPText(buildingPanel.transform, "PlayerGoldText", "Gold: 0g",
            new Vector2(0.7f, 1), new Vector2(1f, 1),
            new Vector2(0, -48), new Vector2(-16, -8), 18, FontStyles.Bold, noWrap: true);
        goldText.alignment = TextAlignmentOptions.Right;
        goldText.color = new Color(1f, 0.85f, 0.2f);

        var closeBtn = CreateButton(buildingPanel.transform, "CloseButton", "X", new Color(0.5f, 0.15f, 0.15f));
        {
            var rt = closeBtn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-8, -8);
            rt.sizeDelta = new Vector2(32, 32);
        }

        // Feedback text (success/failure)
        var feedbackText = CreateTMPText(buildingPanel.transform, "FeedbackText", "",
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(16, 8), new Vector2(-16, 36), 14, FontStyles.Bold);
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.gameObject.SetActive(false);

        // ── ScrollView with building rows ─────────────────────────────────────
        var scrollGO = new GameObject("BuildingScrollView");
        scrollGO.transform.SetParent(buildingPanel.transform, false);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(12, 44);
        scrollRT.offsetMax = new Vector2(-12, -84);

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

        // ── Confirmation panel (small centered dialog) ────────────────────────
        var confirmPanel = CreateStretchPanel(canvasGO.transform, "ConfirmationPanel",
            new Vector2(0.35f, 0.38f), new Vector2(0.65f, 0.62f),
            new Color(0.05f, 0.05f, 0.05f, 0.98f));
        confirmPanel.SetActive(false);

        var confirmNameText = CreateTMPText(confirmPanel.transform, "ConfirmNameText", "Building Name",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(12, -36), new Vector2(-12, -8), 16, FontStyles.Bold);
        confirmNameText.alignment = TextAlignmentOptions.Center;

        var confirmCostText = CreateTMPText(confirmPanel.transform, "ConfirmCostText", "0g",
            new Vector2(0, 0.4f), new Vector2(1, 0.85f),
            new Vector2(12, 0), new Vector2(-12, 0), 14, FontStyles.Normal, noWrap: false);
        confirmCostText.alignment = TextAlignmentOptions.Center;

        var confirmButtonRow = new GameObject("ConfirmButtonRow");
        confirmButtonRow.transform.SetParent(confirmPanel.transform, false);
        var cbrRT = confirmButtonRow.AddComponent<RectTransform>();
        cbrRT.anchorMin = new Vector2(0, 0);
        cbrRT.anchorMax = new Vector2(1, 0.35f);
        cbrRT.offsetMin = new Vector2(12, 8);
        cbrRT.offsetMax = new Vector2(-12, 0);
        var cbrHLG = confirmButtonRow.AddComponent<HorizontalLayoutGroup>();
        cbrHLG.spacing = 8;
        cbrHLG.childForceExpandWidth = true;
        cbrHLG.childForceExpandHeight = true;

        var confirmYesBtn = CreateButton(confirmButtonRow.transform, "ConfirmYesButton", "Build", new Color(0.15f, 0.55f, 0.15f));
        var confirmNoBtn = CreateButton(confirmButtonRow.transform, "ConfirmNoButton", "Cancel", new Color(0.4f, 0.15f, 0.15f));

        // ── BuildingRow prefab (saved to disk so Resources.LoadAll-driven rows look right) ──
        GameObject rowPrefab = CreateBuildingRowPrefab();

        // ── Assign BuildingShopUI script ───────────────────────────────────────
        var uiScript = canvasGO.AddComponent<BuildingShopUI>();

        var so = new SerializedObject(uiScript);
        so.FindProperty("buildingPanel").objectReferenceValue = buildingPanel;
        so.FindProperty("playerGoldText").objectReferenceValue = goldText;
        so.FindProperty("buildingListContainer").objectReferenceValue = contentRT;
        so.FindProperty("buildingRowPrefab").objectReferenceValue = rowPrefab;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
        so.FindProperty("confirmationPanel").objectReferenceValue = confirmPanel;
        so.FindProperty("confirmNameText").objectReferenceValue = confirmNameText;
        so.FindProperty("confirmCostText").objectReferenceValue = confirmCostText;
        so.FindProperty("confirmYesButton").objectReferenceValue = confirmYesBtn;
        so.FindProperty("confirmNoButton").objectReferenceValue = confirmNoBtn;
        so.FindProperty("feedbackText").objectReferenceValue = feedbackText;
        so.ApplyModifiedProperties();

        Debug.Log("[BuildingShopUIBuilder] BuildingShopCanvas created and wired.");

        Selection.activeGameObject = canvasGO;
        EditorUtility.DisplayDialog("Done!",
            "BuildingShopCanvas created!\n\nOpen it in-game via:\n" +
            "Tools > Sowur Shield > Create Building Shop NPC\n" +
            "(or call BuildingShopUI.Instance.OpenShop() from any script/button).",
            "OK");
    }

    [MenuItem("Tools/Sowur Shield/Create Building Shop NPC")]
    public static void CreateNPC()
    {
        var npcGO = new GameObject("BuildingShopNPC");
        Undo.RegisterCreatedObjectUndo(npcGO, "Create BuildingShopNPC");
        npcGO.transform.position = Vector3.zero;

        var sr = npcGO.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.7f, 0.55f, 0.3f); // placeholder tan color until a sprite is assigned
        sr.sortingOrder = 5;

        var col = npcGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.2f);

        npcGO.AddComponent<BuildingShopNPC>();

        Selection.activeGameObject = npcGO;
        EditorUtility.FocusProjectWindow();
        EditorGUIUtility.PingObject(npcGO);

        EditorUtility.DisplayDialog("Done!",
            "BuildingShopNPC created at the world origin.\n\n" +
            "Move it where you want the builder's signpost to be, and optionally " +
            "assign a sprite to its SpriteRenderer.\n\n" +
            "Players press E nearby to open the Building Shop.",
            "OK");
    }

    // ── Prefab creation ──────────────────────────────────────────────────────

    private static GameObject CreateBuildingRowPrefab()
    {
        const string folder = "Assets/Resources/Prefabs/UI";
        const string path = folder + "/BuildingRow.prefab";

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources/Prefabs", "UI");

        var rowGO = new GameObject("BuildingRow");
        var rowRT = rowGO.AddComponent<RectTransform>();
        // Explicit non-zero width so child anchors (fractions of this rect) resolve correctly
        // both in the Editor preview and when first instantiated, before the parent
        // VerticalLayoutGroup gets a chance to stretch it via childForceExpandWidth.
        rowRT.sizeDelta = new Vector2(600, 96);
        rowGO.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        var rowLE = rowGO.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 96;
        rowLE.minWidth = 400;
        rowLE.flexibleWidth = 1;

        // Icon
        var iconGO = new GameObject("IconImage");
        iconGO.transform.SetParent(rowGO.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(8, 0);
        iconRT.sizeDelta = new Vector2(72, 72);

        // Text block (name + effect), anchored after the icon
        var nameText = CreateTMPText(rowGO.transform, "NameText", "Building Name",
            new Vector2(0, 0.55f), new Vector2(0.62f, 1f),
            new Vector2(92, -4), new Vector2(-4, -4), 16, FontStyles.Bold, noWrap: true);

        // Effect description can legitimately wrap across two lines, but needs a
        // real starting width — keep Normal wrap here, NoWrap everywhere else.
        var effectText = CreateTMPText(rowGO.transform, "EffectText", "Effect description",
            new Vector2(0, 0f), new Vector2(0.62f, 0.55f),
            new Vector2(92, 4), new Vector2(-4, 0), 12);
        effectText.color = new Color(0.8f, 0.8f, 0.8f);

        // Cost + material + status column
        var costText = CreateTMPText(rowGO.transform, "CostText", "0g",
            new Vector2(0.62f, 0.62f), new Vector2(0.85f, 1f),
            new Vector2(4, -4), new Vector2(-4, -4), 16, FontStyles.Bold, noWrap: true);
        costText.alignment = TextAlignmentOptions.Center;
        costText.color = new Color(1f, 0.85f, 0.2f);

        var materialText = CreateTMPText(rowGO.transform, "MaterialText", "",
            new Vector2(0.62f, 0.32f), new Vector2(0.85f, 0.62f),
            new Vector2(4, 0), new Vector2(-4, 0), 11, FontStyles.Normal, noWrap: true);
        materialText.alignment = TextAlignmentOptions.Center;

        var statusText = CreateTMPText(rowGO.transform, "StatusText", "",
            new Vector2(0.62f, 0f), new Vector2(0.85f, 0.32f),
            new Vector2(4, 0), new Vector2(-4, 0), 11, FontStyles.Bold, noWrap: true);
        statusText.alignment = TextAlignmentOptions.Center;

        // Buy button
        var buyBtn = CreateButton(rowGO.transform, "BuyButton", "Build", new Color(0.15f, 0.55f, 0.15f));
        var buyRT = buyBtn.GetComponent<RectTransform>();
        buyRT.anchorMin = new Vector2(0.85f, 0.25f);
        buyRT.anchorMax = new Vector2(1f, 0.75f);
        buyRT.offsetMin = new Vector2(4, 0);
        buyRT.offsetMax = new Vector2(-8, 0);

        var rowComponent = rowGO.AddComponent<BuildingRow>();
        rowComponent.iconImage = iconImg;
        rowComponent.nameText = nameText;
        rowComponent.effectText = effectText;
        rowComponent.costText = costText;
        rowComponent.materialText = materialText;
        rowComponent.statusText = statusText;
        rowComponent.buyButton = buyBtn;

        // Overwrite any stale prefab from a previous build
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            AssetDatabase.DeleteAsset(path);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rowGO, path);
        UnityEngine.Object.DestroyImmediate(rowGO);

        return prefab;
    }

    // ── Helpers (mirrors TeamAssemblerUIBuilder's style) ─────────────────────

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
        float fontSize = 14, FontStyles style = FontStyles.Normal, bool noWrap = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
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
