using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using System.Collections.Generic;
using SowurShield.Core;
using SowurShield.Inventory;
using SowurShield.UI;

namespace SowurShield.Dialogue
{

/// <summary>
/// Shop UI window — shows items for sale, applies relationship discounts, handles buy/sell.
/// Implements IUIWindow so UIManager handles ESC and player movement locking.
///
/// SETUP IN UNITY:
///   - Add to a Canvas with a panel child (shopPanel)
///   - shopItemContainer: a VerticalLayoutGroup or GridLayoutGroup
///   - shopItemRowPrefab: a prefab with ShopItemRow component
///   - Assign a ShopData asset to open different shops
/// </summary>
public class ShopUI : MonoBehaviour, IUIWindow, ISaveable
{
    [Header("UI References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI shopTitleText;
    [SerializeField] private TextMeshProUGUI playerGoldText;
    [SerializeField] private TextMeshProUGUI relationshipDiscountText;
    [SerializeField] private Transform shopItemContainer;
    [SerializeField] private GameObject shopItemRowPrefab;
    [SerializeField] private Button closeButton;

    [Header("Localization")]
    [SerializeField] private LocalizedString friendshipDiscountText; // table "Dialogue", key "dialogue.shop.friendship_discount"
    [SerializeField] private LocalizedString goldLabelText; // table "Dialogue", key "dialogue.shop.gold_label"

    private ShopData _currentShop;
    private PlayerStats _playerStats;
    private Inventory.Inventory _inventory;

    // Pool of instantiated rows
    private readonly List<ShopItemRow> _rows = new List<ShopItemRow>();

    // =========================================================================
    // IUIWindow
    // =========================================================================

    public string WindowName => "Shop";
    public int WindowPriority => SowurShield.Core.WindowPriority.Inventory;
    public bool IsWindowOpen => shopPanel != null && shopPanel.activeSelf;
    public bool CanCloseWithEsc => true;

    public void OpenWindow()
    {
        if (shopPanel != null)
            shopPanel.SetActive(true);
        DisablePlayerMovement();
    }

    public void CloseWindow()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
        EnablePlayerMovement();
    }

    public void OnWindowBlocked(string blockedBy)
    {
        Debug.LogWarning($"[ShopUI] Cannot open — blocked by '{blockedBy}'");
    }

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        ApplyTheme();

        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);

        if (UIManager.Instance != null)
            UIManager.Instance.RegisterWindow(this);

        if (SaveManager.Instance != null)
            SaveManager.Instance.RegisterSaveable(this);

        SowurShield.Core.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDestroy()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UnregisterWindow(this);

        if (SaveManager.Instance != null)
            SaveManager.Instance.UnregisterSaveable(this);

        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        if (_currentShop != null)
            PopulateUI();
    }

    /// <summary>
    /// Restyle the scene-wired shop window with the cozy sprite kit + palette
    /// (wood panel, gold close button, cream heading text) — no Editor wiring needed.
    /// </summary>
    private void ApplyTheme()
    {
        UITheme theme = UIThemeStyler.LoadTheme();

        UIThemeStyler.StylePanel(shopPanel, theme);
        UIThemeStyler.StyleButton(closeButton, theme, UIThemeStyler.ButtonSmallPath);

        if (theme != null)
        {
            UIThemeStyler.TintText(shopTitleText, theme.backgroundCream);
            UIThemeStyler.TintText(playerGoldText, theme.highlightGold);
            UIThemeStyler.TintText(relationshipDiscountText, theme.positive);
        }
    }

    // =========================================================================
    // ISaveable — persist limited stock between sessions
    // =========================================================================

    public void SaveData(GameData gameData)
    {
        if (_currentShop == null || gameData?.worldData == null) return;

        foreach (var entry in _currentShop.items)
        {
            if (!entry.IsUnlimited)
            {
                string key = $"shop_{_currentShop.name}_{entry.itemName}_stock";
                gameData.worldData.worldCounters[key] = entry.currentStock;
            }
        }
    }

    public void LoadData(GameData gameData)
    {
        if (_currentShop == null || gameData?.worldData == null) return;

        foreach (var entry in _currentShop.items)
        {
            if (!entry.IsUnlimited)
            {
                string key = $"shop_{_currentShop.name}_{entry.itemName}_stock";
                if (gameData.worldData.worldCounters.TryGetValue(key, out int stock))
                    entry.currentStock = stock;
                else
                    entry.currentStock = entry.maxStock; // First load — use max
            }
        }
    }

    // =========================================================================
    // Public API
    // =========================================================================

    public void OpenShop(ShopData shopData)
    {
        if (shopData == null) return;

        _currentShop = shopData;
        _playerStats = Object.FindFirstObjectByType<PlayerStats>();
        _inventory   = Object.FindFirstObjectByType<Inventory.Inventory>();

        PopulateUI();

        if (UIManager.Instance != null)
            UIManager.Instance.TryOpenWindow(this);
        else
            OpenWindow();
    }

    public void CloseShop()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TryCloseWindow(this);
        else
            CloseWindow();
    }

    // =========================================================================
    // UI Population
    // =========================================================================

    private void PopulateUI()
    {
        if (_currentShop == null) return;

        if (shopTitleText != null)
            shopTitleText.text = _currentShop.shopTitle.SafeGetLocalizedString();

        RefreshGoldDisplay();

        float discount = GetDiscount();
        if (relationshipDiscountText != null)
        {
            if (discount > 0f)
            {
                friendshipDiscountText.Arguments = new object[] { Mathf.RoundToInt(discount * 100) };
                relationshipDiscountText.text = friendshipDiscountText.SafeGetLocalizedString();
            }
            else
                relationshipDiscountText.text = "";
        }

        BuildItemRows(discount);
    }

    private void BuildItemRows(float discount)
    {
        // DestroyImmediate, not Destroy: Destroy is deferred to the end of the frame, so the old
        // rows are still children while the new ones are instantiated below — switching shops
        // stacked every previous shop's stock on top of the current one (rui's 3 items rendered
        // as 15). Same deferred-destroy trap already fixed in RelationshipUI, SeedShopUI and
        // WorldMapUiController.
        foreach (var row in _rows)
            if (row != null) DestroyImmediate(row.gameObject);
        _rows.Clear();

        if (shopItemContainer == null || shopItemRowPrefab == null || _currentShop.items == null) return;

        foreach (var entry in _currentShop.items)
        {
            Item item = ItemDatabase.GetItem(entry.itemName);
            if (item == null)
            {
                Debug.LogWarning($"[ShopUI] Item '{entry.itemName}' not found in ItemDatabase — skipping.");
                continue;
            }

            // Initialize runtime stock on first open
            if (!entry.IsUnlimited && entry.currentStock == 0)
                entry.currentStock = entry.maxStock;

            if (!entry.IsInStock) continue;

            GameObject rowGO = Instantiate(shopItemRowPrefab, shopItemContainer);
            ShopItemRow row = rowGO.GetComponent<ShopItemRow>();
            if (row == null)
            {
                Destroy(rowGO);
                continue;
            }

            int finalPrice = Mathf.Max(1, Mathf.RoundToInt(entry.basePrice * (1f - discount)));
            row.Initialize(item, entry, finalPrice, OnBuyClicked);
            _rows.Add(row);
        }
    }

    private void RefreshGoldDisplay()
    {
        if (playerGoldText != null && _playerStats != null)
        {
            goldLabelText.Arguments = new object[] { _playerStats.Money };
            playerGoldText.text = goldLabelText.SafeGetLocalizedString();
        }
    }

    // =========================================================================
    // Buy Logic
    // =========================================================================

    private void OnBuyClicked(ShopItemEntry entry, Item item, int finalPrice)
    {
        if (_playerStats == null || _inventory == null) return;

        if (!_playerStats.HasMoney(finalPrice))
        {
            Debug.LogWarning("[ShopUI] Not enough gold.");
            return;
        }

        if (!_inventory.AddItem(item, 1))
        {
            Debug.LogWarning("[ShopUI] Inventory full.");
            return;
        }

        _playerStats.SpendMoney(finalPrice);

        if (!entry.IsUnlimited)
        {
            entry.currentStock--;
            if (entry.currentStock <= 0)
            {
                // Remove that row
                RebuildRowsAfterPurchase();
                return;
            }
        }

        RefreshGoldDisplay();
        // Refresh row stock display
        foreach (var row in _rows)
            row?.RefreshStock();
    }

    private void RebuildRowsAfterPurchase()
    {
        float discount = GetDiscount();
        BuildItemRows(discount);
        RefreshGoldDisplay();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>Discount: 0.2% per relationship point, max 20% at 100 affinity.</summary>
    private float GetDiscount()
    {
        if (string.IsNullOrEmpty(_currentShop?.shopkeeperNpcId)) return 0f;
        if (ConversationMemory.Instance == null) return 0f;

        float rel = ConversationMemory.Instance.GetRelationshipLevel(_currentShop.shopkeeperNpcId);
        return Mathf.Clamp01(rel * 0.002f); // 100 affinity → 20% discount
    }

    private void DisablePlayerMovement()
    {
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        if (player != null) player.DisableMovement();
    }

    private void EnablePlayerMovement()
    {
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        if (player != null) player.EnableMovement();
    }
}

} // namespace SowurShield.Dialogue
