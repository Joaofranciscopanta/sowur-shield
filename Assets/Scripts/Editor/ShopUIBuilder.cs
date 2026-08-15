using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEngine.Localization;
using SowurShield.Dialogue;

namespace SowurShield.Editor
{

/// <summary>
/// Editor tool to build the ShopCanvas (general merchant shop) with correct layout and wiring.
/// Menu: Tools > Sowur Shield > Rebuild Shop UI
///
/// Context: ShopUI, ShopData, ShopItemRow and the NPC hook in NPCDialogueInteractable were all
/// implemented, and PlaceholderShopTool authored four ShopData assets and assigned them to
/// clara/isabela/rui/tomas in the scene. The one missing piece was the ShopUI object itself —
/// NPCDialogueInteractable:617 resolves it with FindFirstObjectByType and guards the result with
/// a null check, so picking "browse shop" in dialogue closed the conversation and did nothing,
/// with no console error. Every other popup UI in the project has a builder; this one did not.
///
/// Idempotent: re-running deletes and recreates ShopCanvas.
/// </summary>
public class ShopUIBuilder : EditorWindow
{
    // Between BuildingShopCanvas (45) and TeamAssembler (47) — shop modals never compete.
    private const int ShopCanvasSortingOrder = 46;

    /// <summary>
    /// Usable insets from the panel edge to the cream field, in the panel's own local units.
    ///
    /// Measured by rendering the panel with its content hidden and scanning for where the cream
    /// actually starts — NOT from the sprite's 9-slice border, which reports ~32px for art that
    /// covers far more. The scan returned 138 left and 225 right; these carry a margin on top.
    ///
    /// The asymmetry is real: this panel's art has a noticeably thicker right edge. A single
    /// symmetric inset (the first attempt used 100 everywhere) left every row bleeding out
    /// through the right-hand frame.
    /// </summary>
    private const float FrameInsetLeft = 150f;
    private const float FrameInsetRight = 195f;
    // Top and bottom sit closer than the sides: the scan put the cream field at y 163..595 of a
    // 720px render, and the header/list need the vertical room more than the frame needs margin.
    private const float FrameInsetTop = 110f;
    private const float FrameInsetBottom = 110f;

    private const float CloseButtonSize = 52f;

    /// <summary>Dark brown for text on the panel's cream field (~12:1), where cream reads ~1.1.</summary>
    private static readonly Color TextDark = new Color(0.16f, 0.12f, 0.09f);

    [MenuItem("Tools/Sowur Shield/Rebuild Shop UI")]
    public static void RebuildUI()
    {
        RebuildUI(showDialogs: true);
    }

    /// <summary>
    /// showDialogs:false is required when driving this from automation — EditorUtility.DisplayDialog
    /// is modal and hard-hangs a headless/MCP session waiting for a human click.
    /// </summary>
    public static void RebuildUI(bool showDialogs)
    {
        var existing = GameObject.Find("ShopCanvas");
        if (existing != null)
        {
            if (showDialogs && !EditorUtility.DisplayDialog("Rebuild Shop UI",
                "This will DELETE the existing ShopCanvas and recreate it.\nContinue?",
                "Yes, rebuild", "Cancel"))
                return;

            Undo.DestroyObjectImmediate(existing);
        }

        // ── Canvas ──────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("ShopCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create ShopCanvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ShopCanvasSortingOrder;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // Set explicitly: ScaleWithScreenSize alone leaves the 800x600 default, which draws
        // roughly 1.8x oversized on a 1080p screen.
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── ShopPanel ───────────────────────────────────────────────────────────
        // ShopUI.ApplyTheme replaces this Image with the wood panel sprite at runtime. The
        // flat colour here is only what the Editor preview shows before Play Mode.
        //
        // Wider and shorter than a naive centred card: at 0.18–0.82 the panel measured 819x576
        // and the painted wooden frame ate it down to 627x288 of usable field, which could not
        // fit four rows (the content wanted 368px). Measured from the render, not from the
        // sprite's 9-slice border, which understates the frame badly.
        var shopPanel = CreateStretchPanel(canvasGO.transform, "ShopPanel",
            new Vector2(0.10f, 0.02f), new Vector2(0.90f, 0.98f),
            new Color(0.08f, 0.08f, 0.08f, 0.96f));
        shopPanel.SetActive(false);

        // Header insets are FrameInset, measured against the rendered frame rather than the
        // 9-slice border field, which reports ~32px for art that actually covers ~100px here.
        var titleText = CreateTMPText(shopPanel.transform, "ShopTitleText", "Shop",
            new Vector2(0, 1), new Vector2(0.55f, 1),
            new Vector2(FrameInsetLeft, -(FrameInsetTop + 52)), new Vector2(0, -FrameInsetTop),
            24, FontStyles.Bold, noWrap: true);
        // NOT cream: this heading sits on the panel's cream field, where UITheme's cream scores
        // about 1.1 against it and vanished entirely in the first render. ShopUI.ApplyTheme
        // forces cream on every Awake, so the colour is re-applied there too — changing it only
        // here would be silently repainted at runtime.
        titleText.color = TextDark;

        // The gold readout gets its own column stopping short of the close button; at full
        // width it ran underneath the X and rendered as "Ouro: 9?7".
        var goldText = CreateTMPText(shopPanel.transform, "PlayerGoldText", "Gold: 0g",
            new Vector2(0.55f, 1), new Vector2(1f, 1),
            new Vector2(8, -(FrameInsetTop + 52)),
            new Vector2(-(FrameInsetRight + CloseButtonSize + 16), -FrameInsetTop),
            18, FontStyles.Bold, noWrap: true);
        goldText.alignment = TextAlignmentOptions.Right;

        var discountText = CreateTMPText(shopPanel.transform, "RelationshipDiscountText", "",
            new Vector2(0, 1), new Vector2(1f, 1),
            new Vector2(FrameInsetLeft, -(FrameInsetTop + 88)),
            new Vector2(-FrameInsetRight, -(FrameInsetTop + 56)),
            14, FontStyles.Italic, noWrap: true);

        var closeBtn = CreateButton(shopPanel.transform, "CloseButton", "X", new Color(0.5f, 0.15f, 0.15f));
        {
            var rt = closeBtn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            // Inside the cream field, not on the frame: at the old inset it sat on the painted
            // wood, where a dark red button on dark brown all but disappeared.
            rt.anchoredPosition = new Vector2(-FrameInsetRight, -FrameInsetTop);
            rt.sizeDelta = new Vector2(CloseButtonSize, CloseButtonSize);
        }

        // ── ScrollView with item rows ───────────────────────────────────────────
        var scrollGO = new GameObject("ShopScrollView");
        scrollGO.transform.SetParent(shopPanel.transform, false);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        // Elastic with no visible scrollbar makes an overflowing list look like a clipped one.
        // The four placeholder shops fit, but a longer stock list must be reachable rather than
        // silently cut off at the frame.
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(FrameInsetLeft, FrameInsetBottom);
        scrollRT.offsetMax = new Vector2(-FrameInsetRight, -(FrameInsetTop + 92));

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

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = Vector2.zero;
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        // childControlWidth is required for childForceExpandWidth to do anything at all —
        // without it rows ignore flexibleWidth and keep their prefab width inside a wider panel.
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        // ── ShopItemRow prefab ──────────────────────────────────────────────────
        GameObject rowPrefab = CreateShopItemRowPrefab();

        // ── Assign ShopUI script ────────────────────────────────────────────────
        var uiScript = canvasGO.AddComponent<ShopUI>();

        var so = new SerializedObject(uiScript);
        so.FindProperty("shopPanel").objectReferenceValue = shopPanel;
        so.FindProperty("shopTitleText").objectReferenceValue = titleText;
        so.FindProperty("playerGoldText").objectReferenceValue = goldText;
        so.FindProperty("relationshipDiscountText").objectReferenceValue = discountText;
        so.FindProperty("shopItemContainer").objectReferenceValue = contentRT;
        so.FindProperty("shopItemRowPrefab").objectReferenceValue = rowPrefab;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
        ApplyLocalizedString(so, "friendshipDiscountText", "dialogue.shop.friendship_discount");
        ApplyLocalizedString(so, "goldLabelText", "dialogue.shop.gold_label");
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(uiScript);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvasGO.scene);

        Debug.Log("[ShopUIBuilder] ShopCanvas created and wired.");

        Selection.activeGameObject = canvasGO;
        if (showDialogs)
        {
            EditorUtility.DisplayDialog("Done!",
                "ShopCanvas created!\n\n" +
                "The four placeholder shops (clara, isabela, rui, tomas) already have their " +
                "ShopData assigned in the scene, so talking to any of them and picking " +
                "\"browse shop\" now opens this window.\n\n" +
                "Save the scene to keep it.",
                "OK");
        }
    }

    /// <summary>
    /// A LocalizedString built from table+key names alone keeps KeyId 0 and resolves to nothing
    /// at runtime — it must carry the shared-entry id. This looks the id up from the string table
    /// collection and writes both, the same fix VillagerDialogueFactory needed.
    /// </summary>
    private static void ApplyLocalizedString(SerializedObject so, string propertyName, string key)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"[ShopUIBuilder] Property '{propertyName}' not found on ShopUI.");
            return;
        }

        var collection = UnityEditor.Localization.LocalizationEditorSettings
            .GetStringTableCollection("Dialogue");
        if (collection == null)
        {
            Debug.LogWarning("[ShopUIBuilder] String table collection 'Dialogue' not found — " +
                             $"'{key}' left unresolved. Run Tools > Sowur Shield > Setup Localization (Full).");
            return;
        }

        var entry = collection.SharedData.GetEntry(key);
        if (entry == null)
        {
            Debug.LogWarning($"[ShopUIBuilder] Localization key '{key}' missing from the Dialogue table.");
            return;
        }

        var tableRef = prop.FindPropertyRelative("m_TableReference");
        var entryRef = prop.FindPropertyRelative("m_TableEntryReference");
        if (tableRef == null || entryRef == null)
        {
            Debug.LogWarning($"[ShopUIBuilder] Could not resolve LocalizedString fields for '{propertyName}'.");
            return;
        }

        tableRef.FindPropertyRelative("m_TableCollectionName").stringValue =
            collection.SharedData.TableCollectionNameGuid.ToString("N");
        entryRef.FindPropertyRelative("m_KeyId").longValue = entry.Id;
        entryRef.FindPropertyRelative("m_Key").stringValue = key;
    }

    // ── Prefab creation ──────────────────────────────────────────────────────

    private static GameObject CreateShopItemRowPrefab()
    {
        const string folder = "Assets/Resources/Prefabs/UI";
        const string path = folder + "/ShopItemRow.prefab";

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources/Prefabs", "UI");

        var rowGO = new GameObject("ShopItemRow", typeof(RectTransform));
        var rowRT = rowGO.GetComponent<RectTransform>();
        // Explicit non-zero width: a fresh RectTransform defaults to point anchors where
        // sizeDelta.x = 0 means a literally 0-wide rect, and child anchors (fractions of this
        // rect) would all collapse before the parent layout group stretches it.
        rowRT.sizeDelta = new Vector2(600, 84);
        rowGO.AddComponent<Image>().color = new Color(0.92f, 0.87f, 0.76f, 1f);
        var rowLE = rowGO.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 84;
        rowLE.minWidth = 400;
        rowLE.flexibleWidth = 1;

        // Icon
        var iconGO = new GameObject("IconImage", typeof(RectTransform));
        iconGO.transform.SetParent(rowGO.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(10, 0);
        iconRT.sizeDelta = new Vector2(64, 64);

        // Text on a cream row must be dark — the palette's cream-on-wood defaults score
        // around 1.1 here and would be invisible.
        Color textDark = new Color(0.16f, 0.12f, 0.09f);

        var nameText = CreateTMPText(rowGO.transform, "ItemNameText", "Item Name",
            new Vector2(0, 0.5f), new Vector2(0.58f, 1f),
            new Vector2(84, -6), new Vector2(-4, -6), 18, FontStyles.Bold, noWrap: true);
        nameText.color = textDark;

        var priceText = CreateTMPText(rowGO.transform, "PriceText", "0g",
            new Vector2(0, 0f), new Vector2(0.58f, 0.5f),
            new Vector2(84, 6), new Vector2(-4, 0), 14, FontStyles.Bold, noWrap: true);
        priceText.color = new Color(0.55f, 0.40f, 0.05f);

        // Stock column. 18pt, not 12: at 12 the unlimited glyph rendered as a faint dash that
        // read as an empty cell rather than as "infinite stock".
        var stockText = CreateTMPText(rowGO.transform, "StockText", "",
            new Vector2(0.58f, 0f), new Vector2(0.80f, 1f),
            new Vector2(4, 6), new Vector2(-4, -6), 18, FontStyles.Bold, noWrap: true);
        stockText.alignment = TextAlignmentOptions.Center;
        stockText.color = textDark;

        // Buy button. The label is localized at Initialize; "Buy" here is only the prefab's
        // fallback for before the localization tables finish preloading.
        var buyBtn = CreateButton(rowGO.transform, "BuyButton", "Buy", new Color(0.15f, 0.45f, 0.15f));
        var buyRT = buyBtn.GetComponent<RectTransform>();
        buyRT.anchorMin = new Vector2(0.80f, 0.22f);
        buyRT.anchorMax = new Vector2(1f, 0.78f);
        buyRT.offsetMin = new Vector2(4, 0);
        buyRT.offsetMax = new Vector2(-10, 0);

        var rowComponent = rowGO.AddComponent<ShopItemRow>();
        var rowSO = new SerializedObject(rowComponent);
        rowSO.FindProperty("itemIcon").objectReferenceValue = iconImg;
        rowSO.FindProperty("itemNameText").objectReferenceValue = nameText;
        rowSO.FindProperty("priceText").objectReferenceValue = priceText;
        rowSO.FindProperty("stockText").objectReferenceValue = stockText;
        rowSO.FindProperty("buyButton").objectReferenceValue = buyBtn;
        rowSO.FindProperty("buyButtonLabel").objectReferenceValue =
            buyBtn.GetComponentInChildren<TextMeshProUGUI>();
        ApplyLocalizedString(rowSO, "priceLabelText", "dialogue.shop.price");
        ApplyLocalizedString(rowSO, "unlimitedStockText", "dialogue.shop.unlimited_stock");
        ApplyLocalizedString(rowSO, "stockCountText", "dialogue.shop.stock_count");
        ApplyLocalizedString(rowSO, "buyLabelText", "dialogue.shop.buy");
        rowSO.ApplyModifiedProperties();

        // Overwrite any stale prefab from a previous build
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            AssetDatabase.DeleteAsset(path);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rowGO, path);
        UnityEngine.Object.DestroyImmediate(rowGO);

        return prefab;
    }

    // ── Helpers (mirrors BuildingShopUIBuilder's style) ──────────────────────

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
