using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using SowurShield.Core;
using SowurShield.Inventory;
using SowurShield.UI;

namespace SowurShield.Dialogue
{

/// <summary>
/// Self-spawning seed shop UI. Opened by NPCDialogueInteractable when the NPC has
/// enableSeedShop enabled and the player selects "Browse seeds" from the dialogue menu.
/// Lists all seed items in the ItemDatabase with their shop price (3× base value),
/// lets the player buy one stack at a time using gold from PlayerStats.
/// </summary>
public class SeedShopUI : MonoBehaviour, IUIWindow
{
    private RectTransform listPanel;
    private TextMeshProUGUI goldLabel;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI closeLabel;
    private bool isOpen = false;
    private UITheme theme;

    [SerializeField] private LocalizedString seedShopTitleText; // table "Dialogue", key "dialogue.seedshop.title"
    [SerializeField] private LocalizedString goldZeroText; // table "Dialogue", key "dialogue.seedshop.gold_zero"
    [SerializeField] private LocalizedString closeButtonText; // table "Dialogue", key "dialogue.seedshop.close"
    [SerializeField] private LocalizedString goldLabelText; // table "Dialogue", key "dialogue.seedshop.gold_label"
    [SerializeField] private LocalizedString itemRowText; // table "Dialogue", key "dialogue.seedshop.item_row"
    [SerializeField] private LocalizedString buyButtonText; // table "Dialogue", key "dialogue.seedshop.buy"
    [SerializeField] private LocalizedString noSeedsAvailableText; // table "Dialogue", key "dialogue.seedshop.no_seeds_available"

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<SeedShopUI>(FindObjectsInactive.Include) != null)
            return;

        var go = new GameObject("SeedShopUI");
        go.AddComponent<SeedShopUI>();
        DontDestroyOnLoad(go);
    }

    // This component is created at runtime (never saved to a scene/prefab), so the
    // Tools > Sowur Shield > Auto-Wire Localized Fields editor pass can never reach it —
    // wire its LocalizedString table/key references here instead.
    private void WireLocalizedStrings()
    {
        seedShopTitleText = new LocalizedString("Dialogue", "dialogue.seedshop.title");
        goldZeroText = new LocalizedString("Dialogue", "dialogue.seedshop.gold_zero");
        closeButtonText = new LocalizedString("Dialogue", "dialogue.seedshop.close");
        goldLabelText = new LocalizedString("Dialogue", "dialogue.seedshop.gold_label");
        itemRowText = new LocalizedString("Dialogue", "dialogue.seedshop.item_row");
        buyButtonText = new LocalizedString("Dialogue", "dialogue.seedshop.buy");
        noSeedsAvailableText = new LocalizedString("Dialogue", "dialogue.seedshop.no_seeds_available");
    }

    private bool _buildSucceeded = false;

    private void Awake()
    {
        TryBuildUI();
        SowurShield.Core.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    /// <summary>
    /// Registra-se no UIManager -- ver o comentario igual no RelationshipUI. Tres janelas
    /// implementavam IUIWindow sem nunca se registarem; esta era uma delas.
    /// </summary>
    private void Start()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.RegisterWindow(this);
    }

    private void OnDestroy()
    {
        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        if (UIManager.Instance != null)
            UIManager.Instance.UnregisterWindow(this);
    }

    private void TryBuildUI()
    {
        theme = Resources.Load<UITheme>("UI/CozyUITheme");
        WireLocalizedStrings();
        try
        {
            BuildUI();
            _buildSucceeded = true;
        }
        catch (System.Exception e)
        {
            // Localization tables may not be configured yet (see MOBILE_LOCALIZATION_SETUP.md) —
            // fail safe rather than leaving a half-built panel visible over the menu, but keep
            // retrying on next Open() instead of staying broken for the rest of the session.
            Debug.LogError($"[SeedShopUI] BuildUI failed (Localization not configured?): {e}");
            _buildSucceeded = false;
            gameObject.SetActive(false);
        }
    }

    // =========================================================================
    // IUIWindow
    // =========================================================================

    public string WindowName => "SeedShopUI";
    public int WindowPriority => SowurShield.Core.WindowPriority.Dialogue;
    public bool IsWindowOpen => isOpen;
    public bool CanCloseWithEsc => true;

    public void OpenWindow()
    {
        RefreshList();
        listPanel.gameObject.SetActive(true);
        isOpen = true;
        FindFirstObjectByType<PlayerMove>()?.DisableMovement();
    }

    public void CloseWindow()
    {
        listPanel.gameObject.SetActive(false);
        isOpen = false;
        FindFirstObjectByType<PlayerMove>()?.EnableMovement();
    }

    public void OnWindowBlocked(string blockedBy) { }

    public void Open()
    {
        if (!_buildSucceeded)
        {
            gameObject.SetActive(true);
            TryBuildUI();
            if (!_buildSucceeded)
                return;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.TryOpenWindow(this);
        else
            OpenWindow();
    }

    // =========================================================================
    // UI Construction
    // =========================================================================

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        // referenceResolution defaults to 800x600 — without setting it, ScaleWithScreenSize
        // draws this panel ~1.8x oversized on a 1080p screen.
        var seedScaler = gameObject.AddComponent<CanvasScaler>();
        seedScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        seedScaler.referenceResolution = new Vector2(1920f, 1080f);
        seedScaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("SeedShopPanel");
        panelObj.transform.SetParent(transform, false);

        listPanel = panelObj.AddComponent<RectTransform>();
        listPanel.anchorMin = new Vector2(0.5f, 0.5f);
        listPanel.anchorMax = new Vector2(0.5f, 0.5f);
        listPanel.pivot = new Vector2(0.5f, 0.5f);
        listPanel.anchoredPosition = Vector2.zero;
        // Wider than the 360 it was: the wooden frame eats an eighth of the width per side, so
        // a narrow panel leaves the seed rows almost no room once they clear it. At 560 the
        // rows got 400px, which fits but leaves the Buy buttons hard against the frame art;
        // 680 gives them room to breathe without the panel dominating the screen.
        listPanel.sizeDelta = new Vector2(680, 0);

        // The shared wood panel, like every other window. This one was a flat woodDark
        // rectangle — the last screen still drawing its own background instead of wearing the
        // sprite kit, which is what made it look unfinished beside the inventory and the codex.
        UIThemeStyler.StylePanel(panelObj, theme);

        VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        // panel_wood_generic's painted border covers roughly an eighth per side and scales with
        // the panel, so a fixed 12px padding put every row on top of the wood.
        //
        // Vertical padding is deliberately deeper than the horizontal. The sprite is square and
        // drawn Sliced, so on a panel wider than it is tall the top and bottom bands take a
        // larger share of the height than the sides do of the width.
        int insetX = Mathf.RoundToInt(listPanel.sizeDelta.x * 0.125f) + 10;
        int insetY = insetX + 30;
        vlg.padding = new RectOffset(insetX, insetX, insetY, insetY);
        vlg.spacing = 6;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        // Rows still span the full width; only children with their own LayoutElement opt out.
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = panelObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Title row
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        titleObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
        titleLabel = CreateLabel(titleObj.transform, seedShopTitleText.SafeGetLocalizedString());
        titleLabel.fontSize = theme != null ? theme.fontSizeH2 : 24f;
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.alignment = TextAlignmentOptions.Center;

        // Gold display
        GameObject goldObj = new GameObject("GoldRow");
        goldObj.transform.SetParent(panelObj.transform, false);
        goldObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 22);
        goldLabel = CreateLabel(goldObj.transform, goldZeroText.SafeGetLocalizedString());
        goldLabel.fontSize = theme != null ? theme.fontSizeSmall : 14f;
        goldLabel.alignment = TextAlignmentOptions.Center;
        // A dark amber rather than the theme's highlightGold: gold is meant for dark surfaces
        // and measures 1.3:1 on this panel's cream interior. This keeps the coin association
        // and reads at 5.2:1.
        goldLabel.color = new Color(0.55f, 0.36f, 0.02f);

        // Close button, inside a full-width row that centres it.
        //
        // The button used to BE the row, and childForceExpandWidth stretched it to the whole
        // 490px. button_small_action is drawn Sliced, so at that width its end caps smear
        // across the panel and it reads as a squashed gold bar rather than a button — the same
        // "stretching to the edge is not a fix" lesson as the 1750px dialogue choice.
        float buttonHeight = theme != null ? theme.buttonHeight : 44f;

        GameObject closeRowObj = new GameObject("CloseRow");
        closeRowObj.transform.SetParent(panelObj.transform, false);
        closeRowObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, buttonHeight);
        var closeRow = closeRowObj.AddComponent<HorizontalLayoutGroup>();
        closeRow.childAlignment = TextAnchor.MiddleCenter;
        closeRow.childControlWidth = true;
        closeRow.childForceExpandWidth = false;

        GameObject closeButtonObj = new GameObject("CloseButton", typeof(RectTransform));
        closeButtonObj.transform.SetParent(closeRowObj.transform, false);
        var closeLayout = closeButtonObj.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = theme != null ? theme.buttonMinWidth : 160f;
        closeLayout.preferredHeight = buttonHeight;
        closeButtonObj.AddComponent<Image>();
        Button closeButton = closeButtonObj.AddComponent<Button>();
        closeButton.onClick.AddListener(OnCloseClicked);
        closeLabel = CreateLabel(closeButtonObj.transform, closeButtonText.SafeGetLocalizedString());
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.fontSize = theme != null ? theme.fontSizeButton : 18f;

        // Gold button art instead of a flat woodDark fill, so it reads as a control rather than
        // a darker strip of panel. StyleButton runs after the label exists — it looks the TMP
        // text up as a child to darken it for the gold sprite.
        UIThemeStyler.StyleButton(closeButton, theme, UIThemeStyler.ButtonSmallPath);

        panelObj.SetActive(false);
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        if (titleLabel != null) titleLabel.text = seedShopTitleText.SafeGetLocalizedString();
        if (closeLabel != null) closeLabel.text = closeButtonText.SafeGetLocalizedString();
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = theme != null ? theme.fontSizeBody : 16f;
        // Dark ink, not cream: panel_wood_generic's interior is cream, so the cream this used
        // to get was near-invisible on it. Only the gold row overrides, and gold reads on cream.
        tmp.color = theme != null ? theme.textDark : new Color(0.176f, 0.165f, 0.149f);
        return tmp;
    }

    // =========================================================================
    // Shop logic
    // =========================================================================

    private void OnCloseClicked()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TryCloseWindow(this);
        else
            CloseWindow();
    }

    private void RefreshList()
    {
        // Remove all rows except Title(0), GoldRow(1), CloseButton(last).
        //
        // DestroyImmediate, not Destroy: Destroy is deferred to the end of the frame, so the
        // old rows are still children while CreateRow inserts the new ones below. The sibling
        // indices then land among the doomed rows and the list rebuilds in the wrong order —
        // the same deferred-destroy trap that stacked 50 buttons on the world map.
        for (int i = listPanel.childCount - 2; i >= 2; i--)
            DestroyImmediate(listPanel.GetChild(i).gameObject);

        PlayerStats stats = FindFirstObjectByType<PlayerStats>();
        int playerGold = stats != null ? stats.Money : 0;
        if (goldLabel != null)
        {
            goldLabelText.Arguments = new object[] { playerGold };
            goldLabel.text = goldLabelText.SafeGetLocalizedString();
        }

        List<Item> seeds = ItemDatabase.GetItemsByType(ItemType.Seed)
            .Where(s => s != null)
            .OrderBy(s => s.baseValue)
            .ToList();

        if (seeds.Count == 0)
        {
            CreateRow(noSeedsAvailableText.SafeGetLocalizedString(), null, 0, 2);
            return;
        }

        int insertIndex = 2;
        foreach (Item seed in seeds)
        {
            int price = Mathf.Max(1, seed.baseValue * 3);
            bool canAfford = playerGold >= price;
            itemRowText.Arguments = new object[] { seed.GetDisplayName(), price };
            CreateRow(itemRowText.SafeGetLocalizedString(), seed, price, insertIndex++, canAfford);
        }
    }

    private void CreateRow(string label, Item seed, int price, int siblingIndex, bool canAfford = false)
    {
        GameObject rowObj = new GameObject("Row");
        rowObj.transform.SetParent(listPanel, false);
        rowObj.transform.SetSiblingIndex(siblingIndex);
        rowObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 34);
        // A light tan well, not woodLight. woodLight over the panel's cream reads as a mid
        // brown where nothing is legible: dark ink measures 3.8:1 on it and cream 3.4:1 — the
        // same trap the animal cards had. Tan keeps the row visible against the cream interior
        // while leaving dark ink at 7.8:1.
        Color rowTint = theme != null ? theme.backgroundTan : new Color(0.937f, 0.890f, 0.753f);
        rowObj.AddComponent<Image>().color = seed != null
            ? new Color(rowTint.r * 0.86f, rowTint.g * 0.84f, rowTint.b * 0.82f, 1f)
            : new Color(0f, 0f, 0f, 0f);

        var rowLabel = CreateLabel(rowObj.transform, label);
        rowLabel.fontSize = theme != null ? theme.fontSizeSmall : 14f;
        rowLabel.alignment = TextAlignmentOptions.MidlineLeft;
        rowLabel.margin = new Vector4(8, 0, 70, 0);
        // Unaffordable rows are dimmed toward the row fill rather than set to mid grey, which
        // on this lighter well would have been the least readable colour available.
        if (seed != null && !canAfford)
            rowLabel.color = new Color(0.42f, 0.40f, 0.37f);

        if (seed != null)
        {
            GameObject buyObj = new GameObject("BuyButton");
            buyObj.transform.SetParent(rowObj.transform, false);
            RectTransform buyRect = buyObj.AddComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(1, 0);
            buyRect.anchorMax = new Vector2(1, 1);
            buyRect.pivot = new Vector2(1, 0.5f);
            buyRect.sizeDelta = new Vector2(60, 0);
            buyRect.anchoredPosition = new Vector2(-4, 0);
            Color buyColor = theme != null ? theme.positive : new Color(0.2f, 0.5f, 0.2f);
            buyObj.AddComponent<Image>().color = canAfford
                ? new Color(buyColor.r, buyColor.g, buyColor.b, 0.9f)
                : new Color(0.3f, 0.3f, 0.3f, 0.7f);
            Button buyBtn = buyObj.AddComponent<Button>();
            buyBtn.interactable = canAfford;
            Item capturedSeed = seed;
            int capturedPrice = price;
            buyBtn.onClick.AddListener(() => BuySeed(capturedSeed, capturedPrice));
            var buyLabel = CreateLabel(buyObj.transform, buyButtonText.SafeGetLocalizedString());
            buyLabel.fontSize = 14;
            buyLabel.alignment = TextAlignmentOptions.Center;
            // theme.positive is a light mint green (#81C784): cream text on it measures 1.8:1.
            // Dark text reads ~9:1 on the same fill.
            buyLabel.color = theme != null ? theme.textDark : Color.black;
        }
    }

    private void BuySeed(Item seed, int price)
    {
        PlayerStats stats = FindFirstObjectByType<PlayerStats>();
        if (stats == null || !stats.SpendMoney(price))
            return;

        SowurShield.Inventory.Inventory inventory = FindFirstObjectByType<SowurShield.Inventory.Inventory>();
        if (inventory == null || !inventory.AddItem(seed, 1))
        {
            // Refund if inventory is full
            stats.AddMoney(price);
            return;
        }
        RefreshList();
    }
}

} // namespace SowurShield.Dialogue
