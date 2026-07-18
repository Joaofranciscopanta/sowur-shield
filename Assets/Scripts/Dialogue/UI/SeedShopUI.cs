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

    private void OnDestroy()
    {
        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
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

        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("SeedShopPanel");
        panelObj.transform.SetParent(transform, false);

        listPanel = panelObj.AddComponent<RectTransform>();
        listPanel.anchorMin = new Vector2(0.5f, 0.5f);
        listPanel.anchorMax = new Vector2(0.5f, 0.5f);
        listPanel.pivot = new Vector2(0.5f, 0.5f);
        listPanel.anchoredPosition = Vector2.zero;
        listPanel.sizeDelta = new Vector2(360, 0);

        Color panelTint = theme != null ? theme.woodDark : new Color(0.08f, 0.06f, 0.1f);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(panelTint.r, panelTint.g, panelTint.b, 0.95f);

        VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.spacing = 6;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Title row
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        titleObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 28);
        titleLabel = CreateLabel(titleObj.transform, seedShopTitleText.SafeGetLocalizedString());
        titleLabel.fontSize = 20;
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.alignment = TextAlignmentOptions.Center;

        // Gold display
        GameObject goldObj = new GameObject("GoldRow");
        goldObj.transform.SetParent(panelObj.transform, false);
        goldObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 22);
        goldLabel = CreateLabel(goldObj.transform, goldZeroText.SafeGetLocalizedString());
        goldLabel.fontSize = 16;
        goldLabel.alignment = TextAlignmentOptions.Center;
        goldLabel.color = theme != null ? theme.highlightGold : new Color(1f, 0.85f, 0.2f);

        // Close button
        GameObject closeButtonObj = new GameObject("CloseButton");
        closeButtonObj.transform.SetParent(panelObj.transform, false);
        closeButtonObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 36);
        closeButtonObj.AddComponent<Image>().color = theme != null ? theme.woodDark : new Color(0.3f, 0.25f, 0.25f, 0.9f);
        Button closeButton = closeButtonObj.AddComponent<Button>();
        closeButton.onClick.AddListener(OnCloseClicked);
        closeLabel = CreateLabel(closeButtonObj.transform, closeButtonText.SafeGetLocalizedString());
        closeLabel.alignment = TextAlignmentOptions.Center;

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
        tmp.fontSize = 18;
        tmp.color = theme != null ? theme.backgroundCream : Color.white;
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
        // Remove all rows except Title(0), GoldRow(1), CloseButton(last)
        for (int i = listPanel.childCount - 2; i >= 2; i--)
            Destroy(listPanel.GetChild(i).gameObject);

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
        Color rowTint = theme != null ? theme.woodLight : new Color(0.2f, 0.2f, 0.25f);
        rowObj.AddComponent<Image>().color = seed != null
            ? new Color(rowTint.r, rowTint.g, rowTint.b, 0.9f)
            : new Color(0f, 0f, 0f, 0f);

        var rowLabel = CreateLabel(rowObj.transform, label);
        rowLabel.fontSize = 15;
        rowLabel.alignment = TextAlignmentOptions.MidlineLeft;
        rowLabel.margin = new Vector4(8, 0, 70, 0);
        if (seed != null && !canAfford)
            rowLabel.color = new Color(0.5f, 0.5f, 0.5f);

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
