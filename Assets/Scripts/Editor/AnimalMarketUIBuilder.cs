using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using SowurShield.Animals;
using SowurShield.UI;

namespace SowurShield.Editor
{

/// <summary>
/// Editor tool to build the AnimalMarketUI canvas (Buy + Sell tabs) and an interactable
/// NPC to open it, from scratch with correct layout and wiring.
/// Menu: Tools > Sowur Shield > Rebuild Animal Market UI
///       Tools > Sowur Shield > Create Animal Market NPC
/// </summary>
public class AnimalMarketUIBuilder : EditorWindow
{
    private static UITheme LoadTheme() => Resources.Load<UITheme>("UI/CozyUITheme");

    [MenuItem("Tools/Sowur Shield/Rebuild Animal Market UI")]
    public static void RebuildUI()
    {
        var existing = GameObject.Find("AnimalMarketCanvas");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild Animal Market UI",
                "This will DELETE the existing AnimalMarketCanvas and recreate it.\nContinue?",
                "Yes, rebuild", "Cancel"))
                return;

            Undo.DestroyObjectImmediate(existing);
        }

        UITheme theme = LoadTheme();
        Color backgroundTan = theme != null ? theme.backgroundTan : new Color(0.94f, 0.89f, 0.75f);
        Color backgroundCream = theme != null ? theme.backgroundCream : new Color(0.97f, 0.95f, 0.91f);
        Color highlightGold = theme != null ? theme.highlightGold : new Color(0.96f, 0.83f, 0.37f);
        Color textDark = theme != null ? theme.textDark : new Color(0.18f, 0.17f, 0.15f);

        // ── Canvas ──────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("AnimalMarketCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create AnimalMarketCanvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 47;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── MarketPanel ──────────────────────────────────────────────────────────
        var marketPanel = CreateStretchPanel(canvasGO.transform, "MarketPanel",
            new Vector2(0.18f, 0.10f), new Vector2(0.82f, 0.90f), backgroundTan);
        marketPanel.SetActive(false);

        var titleText = CreateTMPText(marketPanel.transform, "TitleText", "Animal Market",
            new Vector2(0, 1), new Vector2(0.6f, 1),
            new Vector2(16, -48), new Vector2(0, -8), 22, FontStyles.Bold, textDark, noWrap: true);

        var goldText = CreateTMPText(marketPanel.transform, "PlayerGoldText", "Gold: 0",
            new Vector2(0.6f, 1), new Vector2(0.85f, 1),
            new Vector2(0, -48), new Vector2(0, -8), 16, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), noWrap: true);
        goldText.alignment = TextAlignmentOptions.Right;

        var discountText = CreateTMPText(marketPanel.transform, "RelationshipDiscountText", "",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(16, -72), new Vector2(-16, -52), 13, FontStyles.Italic, textDark);

        var closeBtn = CreateButton(marketPanel.transform, "CloseButton", "X", new Color(0.5f, 0.15f, 0.15f));
        {
            var rt = closeBtn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-8, -8);
            rt.sizeDelta = new Vector2(32, 32);
        }

        var feedbackText = CreateTMPText(marketPanel.transform, "FeedbackText", "",
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(16, 8), new Vector2(-16, 36), 14, FontStyles.Bold, textDark);
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.gameObject.SetActive(false);

        // ── Tab row ──────────────────────────────────────────────────────────────
        var tabRow = new GameObject("TabRow");
        tabRow.transform.SetParent(marketPanel.transform, false);
        var tabRowRT = tabRow.AddComponent<RectTransform>();
        tabRowRT.anchorMin = new Vector2(0, 1);
        tabRowRT.anchorMax = new Vector2(1, 1);
        tabRowRT.pivot = new Vector2(0.5f, 1);
        tabRowRT.anchoredPosition = new Vector2(0, -84);
        tabRowRT.sizeDelta = new Vector2(-32, 32);
        var tabHLG = tabRow.AddComponent<HorizontalLayoutGroup>();
        tabHLG.spacing = 8;
        tabHLG.childForceExpandWidth = true;
        tabHLG.childForceExpandHeight = true;

        var buyTabBtn = CreateButton(tabRow.transform, "BuyTabButton", "Buy", highlightGold);
        var sellTabBtn = CreateButton(tabRow.transform, "SellTabButton", "Sell", backgroundCream);

        // ── Buy tab panel + scroll list ─────────────────────────────────────────
        var buyTabPanel = CreateStretchPanel(marketPanel.transform, "BuyTabPanel",
            Vector2.zero, Vector2.one, new Color(0, 0, 0, 0));
        {
            var rt = buyTabPanel.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(12, 44);
            rt.offsetMax = new Vector2(-12, -120);
        }
        Transform buyContent = CreateScrollList(buyTabPanel.transform, "BuyScrollView");

        // ── Sell tab panel + scroll list ────────────────────────────────────────
        var sellTabPanel = CreateStretchPanel(marketPanel.transform, "SellTabPanel",
            Vector2.zero, Vector2.one, new Color(0, 0, 0, 0));
        {
            var rt = sellTabPanel.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(12, 44);
            rt.offsetMax = new Vector2(-12, -120);
        }
        sellTabPanel.SetActive(false);
        Transform sellContent = CreateScrollList(sellTabPanel.transform, "SellScrollView");

        // ── Confirmation panel (sell only) ──────────────────────────────────────
        var confirmPanel = CreateStretchPanel(canvasGO.transform, "ConfirmationPanel",
            new Vector2(0.35f, 0.38f), new Vector2(0.65f, 0.62f), new Color(0.05f, 0.05f, 0.05f, 0.98f));
        confirmPanel.SetActive(false);

        var confirmNameText = CreateTMPText(confirmPanel.transform, "ConfirmNameText", "Sell Animal?",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(12, -36), new Vector2(-12, -8), 16, FontStyles.Bold, Color.white);
        confirmNameText.alignment = TextAlignmentOptions.Center;

        var confirmPriceText = CreateTMPText(confirmPanel.transform, "ConfirmPriceText", "+0 gold",
            new Vector2(0, 0.4f), new Vector2(1, 0.85f),
            new Vector2(12, 0), new Vector2(-12, 0), 14, FontStyles.Normal, Color.white);
        confirmPriceText.alignment = TextAlignmentOptions.Center;

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

        var confirmYesBtn = CreateButton(confirmButtonRow.transform, "ConfirmYesButton", "Sell", new Color(0.55f, 0.15f, 0.15f));
        var confirmNoBtn = CreateButton(confirmButtonRow.transform, "ConfirmNoButton", "Cancel", new Color(0.3f, 0.3f, 0.3f));

        // ── Row prefabs ──────────────────────────────────────────────────────────
        GameObject buyRowPrefab = CreateBuyRowPrefab(textDark);
        GameObject sellRowPrefab = CreateSellRowPrefab(textDark);

        // ── Assign AnimalMarketUI script ────────────────────────────────────────
        var uiScript = canvasGO.AddComponent<AnimalMarketUI>();

        var so = new SerializedObject(uiScript);
        so.FindProperty("marketPanel").objectReferenceValue = marketPanel;
        so.FindProperty("marketTitleText").objectReferenceValue = titleText;
        so.FindProperty("playerGoldText").objectReferenceValue = goldText;
        so.FindProperty("relationshipDiscountText").objectReferenceValue = discountText;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
        so.FindProperty("buyTabButton").objectReferenceValue = buyTabBtn;
        so.FindProperty("sellTabButton").objectReferenceValue = sellTabBtn;
        so.FindProperty("buyTabPanel").objectReferenceValue = buyTabPanel;
        so.FindProperty("sellTabPanel").objectReferenceValue = sellTabPanel;
        so.FindProperty("buyListContainer").objectReferenceValue = buyContent;
        so.FindProperty("buyRowPrefab").objectReferenceValue = buyRowPrefab;
        so.FindProperty("sellListContainer").objectReferenceValue = sellContent;
        so.FindProperty("sellRowPrefab").objectReferenceValue = sellRowPrefab;
        so.FindProperty("confirmationPanel").objectReferenceValue = confirmPanel;
        so.FindProperty("confirmNameText").objectReferenceValue = confirmNameText;
        so.FindProperty("confirmPriceText").objectReferenceValue = confirmPriceText;
        so.FindProperty("confirmYesButton").objectReferenceValue = confirmYesBtn;
        so.FindProperty("confirmNoButton").objectReferenceValue = confirmNoBtn;
        so.FindProperty("feedbackText").objectReferenceValue = feedbackText;
        so.ApplyModifiedProperties();

        Debug.Log("[AnimalMarketUIBuilder] AnimalMarketCanvas created and wired.");

        Selection.activeGameObject = canvasGO;
        EditorUtility.DisplayDialog("Done!",
            "AnimalMarketCanvas created!\n\nNext steps:\n" +
            "1. Tools > Sowur Shield > Create Animal Market NPC\n" +
            "2. Author an AnimalMarketData asset (Assets > Create > SowurShield > Animal Market Data) " +
            "with a few catalog entries, then assign it to the NPC.\n" +
            "3. Optionally wire candidateZones on AnimalMarketUI to specific AnimalZones " +
            "(leave empty to auto-discover any non-full zone).",
            "OK");
    }

    [MenuItem("Tools/Sowur Shield/Create Animal Market NPC")]
    public static void CreateNPC()
    {
        var npcGO = new GameObject("AnimalMarketNPC");
        Undo.RegisterCreatedObjectUndo(npcGO, "Create AnimalMarketNPC");
        npcGO.transform.position = Vector3.zero;

        var sr = npcGO.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.55f, 0.75f, 0.55f); // placeholder green-tan color until a sprite is assigned
        sr.sortingOrder = 5;

        var col = npcGO.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.2f);

        npcGO.AddComponent<AnimalMarketNPC>();

        Selection.activeGameObject = npcGO;
        EditorUtility.FocusProjectWindow();
        EditorGUIUtility.PingObject(npcGO);

        EditorUtility.DisplayDialog("Done!",
            "AnimalMarketNPC created at the world origin.\n\n" +
            "Move it where you want the trader to stand, assign an AnimalMarketData asset " +
            "to its Inspector field, and optionally assign a sprite to its SpriteRenderer.\n\n" +
            "Players press E nearby to open the Animal Market.",
            "OK");
    }

    // ── Row prefab creation ──────────────────────────────────────────────────

    private static GameObject CreateBuyRowPrefab(Color textDark)
    {
        const string folder = "Assets/Resources/Prefabs/UI";
        const string path = folder + "/AnimalMarketBuyRow.prefab";
        EnsureResourcesUIFolder();

        var rowGO = new GameObject("AnimalMarketBuyRow");
        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(600, 88);
        rowGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.5f);
        var rowLE = rowGO.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 88;
        rowLE.minWidth = 400;
        rowLE.flexibleWidth = 1;

        var iconGO = new GameObject("IconImage");
        iconGO.transform.SetParent(rowGO.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.enabled = false;
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(8, 0);
        iconRT.sizeDelta = new Vector2(64, 64);

        var nameText = CreateTMPText(rowGO.transform, "NameText", "Animal Name",
            new Vector2(0, 0.5f), new Vector2(0.6f, 1f),
            new Vector2(84, -4), new Vector2(-4, -4), 15, FontStyles.Bold, textDark, noWrap: true);

        var priceText = CreateTMPText(rowGO.transform, "PriceText", "0g",
            new Vector2(0.6f, 0.5f), new Vector2(0.82f, 1f),
            new Vector2(4, -4), new Vector2(-4, -4), 14, FontStyles.Bold, new Color(0.6f, 0.45f, 0.1f), noWrap: true);
        priceText.alignment = TextAlignmentOptions.Center;

        var stockText = CreateTMPText(rowGO.transform, "StockText", "",
            new Vector2(0.6f, 0f), new Vector2(0.82f, 0.5f),
            new Vector2(4, 0), new Vector2(-4, 0), 11, FontStyles.Normal, textDark, noWrap: true);
        stockText.alignment = TextAlignmentOptions.Center;

        var buyBtn = CreateButton(rowGO.transform, "BuyButton", "Buy", new Color(0.15f, 0.55f, 0.15f));
        var buyRT = buyBtn.GetComponent<RectTransform>();
        buyRT.anchorMin = new Vector2(0.82f, 0.2f);
        buyRT.anchorMax = new Vector2(1f, 0.8f);
        buyRT.offsetMin = new Vector2(4, 0);
        buyRT.offsetMax = new Vector2(-8, 0);

        var rowComponent = rowGO.AddComponent<AnimalMarketBuyRow>();
        rowComponent.iconImage = iconImg;
        rowComponent.nameText = nameText;
        rowComponent.priceText = priceText;
        rowComponent.stockText = stockText;
        rowComponent.buyButton = buyBtn;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            AssetDatabase.DeleteAsset(path);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rowGO, path);
        Object.DestroyImmediate(rowGO);

        return prefab;
    }

    private static GameObject CreateSellRowPrefab(Color textDark)
    {
        const string folder = "Assets/Resources/Prefabs/UI";
        const string path = folder + "/AnimalMarketSellRow.prefab";
        EnsureResourcesUIFolder();

        var rowGO = new GameObject("AnimalMarketSellRow");
        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(600, 88);
        rowGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.5f);
        var rowLE = rowGO.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 88;
        rowLE.minWidth = 400;
        rowLE.flexibleWidth = 1;

        var iconGO = new GameObject("IconImage");
        iconGO.transform.SetParent(rowGO.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.enabled = false;
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(8, 0);
        iconRT.sizeDelta = new Vector2(64, 64);

        var nameText = CreateTMPText(rowGO.transform, "NameText", "Animal Name",
            new Vector2(0, 0.5f), new Vector2(0.6f, 1f),
            new Vector2(84, -4), new Vector2(-4, -4), 15, FontStyles.Bold, textDark, noWrap: true);

        var zoneText = CreateTMPText(rowGO.transform, "ZoneText", "Zone",
            new Vector2(0, 0f), new Vector2(0.6f, 0.5f),
            new Vector2(84, 0), new Vector2(-4, 0), 11, FontStyles.Italic, textDark, noWrap: true);

        var priceText = CreateTMPText(rowGO.transform, "PriceText", "+0g",
            new Vector2(0.6f, 0f), new Vector2(0.82f, 1f),
            new Vector2(4, 0), new Vector2(-4, 0), 14, FontStyles.Bold, new Color(0.15f, 0.5f, 0.15f), noWrap: true);
        priceText.alignment = TextAlignmentOptions.Center;

        var sellBtn = CreateButton(rowGO.transform, "SellButton", "Sell", new Color(0.55f, 0.15f, 0.15f));
        var sellRT = sellBtn.GetComponent<RectTransform>();
        sellRT.anchorMin = new Vector2(0.82f, 0.2f);
        sellRT.anchorMax = new Vector2(1f, 0.8f);
        sellRT.offsetMin = new Vector2(4, 0);
        sellRT.offsetMax = new Vector2(-8, 0);

        var rowComponent = rowGO.AddComponent<AnimalMarketSellRow>();
        rowComponent.iconImage = iconImg;
        rowComponent.nameText = nameText;
        rowComponent.priceText = priceText;
        rowComponent.zoneText = zoneText;
        rowComponent.sellButton = sellBtn;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            AssetDatabase.DeleteAsset(path);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rowGO, path);
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
