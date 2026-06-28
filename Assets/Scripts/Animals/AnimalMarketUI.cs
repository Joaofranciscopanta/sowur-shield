using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization;
using SowurShield.Core;
using SowurShield.Dialogue;
using SowurShield.Inventory;

namespace SowurShield.Animals
{

/// <summary>
/// Animal Market window — buy new animals into a non-full AnimalZone, or sell owned
/// animals back for gold. Mirrors ShopUI's shape (relationship discount, IUIWindow,
/// ISaveable stock persistence) but with two tabs (Buy/Sell) since the two lists show
/// structurally different data: catalog entries vs. owned Animal instances.
///
/// SETUP IN UNITY:
///   Built via Tools > Sowur Shield > Rebuild Animal Market UI (see AnimalMarketUIBuilder.cs).
/// </summary>
public class AnimalMarketUI : MonoBehaviour, IUIWindow, ISaveable
{
    public static AnimalMarketUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject marketPanel;
    [SerializeField] private TextMeshProUGUI marketTitleText;
    [SerializeField] private TextMeshProUGUI playerGoldText;
    [SerializeField] private TextMeshProUGUI relationshipDiscountText;
    [SerializeField] private Button closeButton;

    [Header("Tabs")]
    [SerializeField] private Button buyTabButton;
    [SerializeField] private Button sellTabButton;
    [SerializeField] private GameObject buyTabPanel;
    [SerializeField] private GameObject sellTabPanel;

    [Header("Buy Tab")]
    [SerializeField] private Transform buyListContainer;
    [SerializeField] private GameObject buyRowPrefab;

    [Header("Sell Tab")]
    [SerializeField] private Transform sellListContainer;
    [SerializeField] private GameObject sellRowPrefab;

    [Header("Confirmation Panel (sell only — buy is reversible via re-selling)")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TextMeshProUGUI confirmNameText;
    [SerializeField] private TextMeshProUGUI confirmPriceText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private float feedbackDisplaySeconds = 2.5f;

    [Header("Zone Assignment")]
    [Tooltip("Zones checked in order for the first non-full one when buying. Leave empty to auto-discover all AnimalZones in the scene.")]
    [SerializeField] private AnimalZone[] candidateZones;

    [Header("Localization")]
    [SerializeField] private LocalizedString notEnoughGoldText; // table "Animals", key "animals.market.not_enough_gold"
    [SerializeField] private LocalizedString zonesFullText; // table "Animals", key "animals.market.zones_full"
    [SerializeField] private LocalizedString welcomeText; // table "Animals", key "animals.market.welcome"
    [SerializeField] private LocalizedString sellConfirmText; // table "Animals", key "animals.market.sell_confirm"
    [SerializeField] private LocalizedString sellGoldGainText; // table "Animals", key "animals.market.sell_gold_gain"
    [SerializeField] private LocalizedString saleFailedText; // table "Animals", key "animals.market.sale_failed"
    [SerializeField] private LocalizedString soldText; // table "Animals", key "animals.market.sold"
    [SerializeField] private LocalizedString friendshipDiscountText; // table "Animals", key "animals.market.friendship_discount"
    [SerializeField] private LocalizedString goldLabelText; // table "Animals", key "animals.market.gold_label"

    private AnimalMarketData _currentMarket;
    private PlayerStats _playerStats;
    private Inventory.Inventory _inventory;

    private readonly List<GameObject> _buyRows = new List<GameObject>();
    private readonly List<GameObject> _sellRows = new List<GameObject>();
    private Animal _pendingSellAnimal;
    private int _pendingSellPrice;
    private Coroutine _feedbackCoroutine;

    // =========================================================================
    // IUIWindow
    // =========================================================================

    public string WindowName => "AnimalMarket";
    public int WindowPriority => SowurShield.Core.WindowPriority.Inventory;
    public bool IsWindowOpen => marketPanel != null && marketPanel.activeSelf;
    public bool CanCloseWithEsc => true;

    public void OpenWindow()
    {
        if (marketPanel != null) marketPanel.SetActive(true);
        HideConfirmation();
        DisablePlayerMovement();
    }

    public void CloseWindow()
    {
        if (marketPanel != null) marketPanel.SetActive(false);
        HideConfirmation();
        EnablePlayerMovement();
    }

    public void OnWindowBlocked(string blockedBy)
    {
        Debug.LogWarning($"[AnimalMarketUI] Blocked by '{blockedBy}'");
    }

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        if (marketPanel != null) marketPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(CloseMarket);
        if (buyTabButton != null) buyTabButton.onClick.AddListener(ShowBuyTab);
        if (sellTabButton != null) sellTabButton.onClick.AddListener(ShowSellTab);
        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(OnConfirmSell);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(HideConfirmation);

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
        if (!IsWindowOpen || _currentMarket == null) return;
        RefreshGoldDisplay();
        RefreshDiscountDisplay();
        BuildBuyRows();
        BuildSellRows();
    }

    // =========================================================================
    // Public API
    // =========================================================================

    public void OpenMarket(AnimalMarketData marketData)
    {
        if (marketData == null) return;

        _currentMarket = marketData;
        _playerStats = Object.FindFirstObjectByType<PlayerStats>();
        _inventory = Object.FindFirstObjectByType<Inventory.Inventory>();

        if (marketTitleText != null)
            marketTitleText.text = marketData.marketTitle;

        ShowBuyTab();
        RefreshGoldDisplay();
        RefreshDiscountDisplay();
        BuildBuyRows();
        BuildSellRows();

        if (UIManager.Instance != null)
            UIManager.Instance.TryOpenWindow(this);
        else
            OpenWindow();
    }

    public void CloseMarket()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TryCloseWindow(this);
        else
            CloseWindow();
    }

    // =========================================================================
    // Tabs
    // =========================================================================

    private void ShowBuyTab()
    {
        if (buyTabPanel != null) buyTabPanel.SetActive(true);
        if (sellTabPanel != null) sellTabPanel.SetActive(false);
    }

    private void ShowSellTab()
    {
        if (buyTabPanel != null) buyTabPanel.SetActive(false);
        if (sellTabPanel != null) sellTabPanel.SetActive(true);
        BuildSellRows(); // owned animals can change between opens of this tab
    }

    // =========================================================================
    // Buy flow
    // =========================================================================

    private void BuildBuyRows()
    {
        foreach (var row in _buyRows)
            if (row != null) Destroy(row);
        _buyRows.Clear();

        if (buyListContainer == null || buyRowPrefab == null || _currentMarket?.catalogEntries == null)
            return;

        float discount = GetDiscount();

        foreach (var entry in _currentMarket.catalogEntries)
        {
            if (entry.animalData == null) continue;
            if (!entry.IsInStock) continue;

            int finalPrice = Mathf.Max(1, Mathf.RoundToInt(entry.buyPrice * (1f - discount)));

            GameObject rowGO = Instantiate(buyRowPrefab, buyListContainer);
            _buyRows.Add(rowGO);

            var row = rowGO.GetComponent<AnimalMarketBuyRow>();
            if (row == null) continue;
            row.Initialize(entry, finalPrice, OnBuyClicked);
        }
    }

    private void OnBuyClicked(AnimalMarketEntry entry, int finalPrice)
    {
        if (_playerStats == null || entry?.animalData == null) return;

        if (!_playerStats.HasMoney(finalPrice))
        {
            ShowFeedback(notEnoughGoldText.SafeGetLocalizedString(), true);
            return;
        }

        AnimalZone targetZone = FindAvailableZone();
        if (targetZone == null)
        {
            ShowFeedback(zonesFullText.SafeGetLocalizedString(), true);
            return;
        }

        _playerStats.SpendMoney(finalPrice);
        SpawnPurchasedAnimal(entry.animalData, targetZone);

        if (!entry.IsUnlimited)
            entry.purchasedCount++;

        RefreshGoldDisplay();
        BuildBuyRows();
        welcomeText.Arguments = new object[] { entry.animalData.animalName };
        ShowFeedback(welcomeText.SafeGetLocalizedString(), false);
    }

    private AnimalZone FindAvailableZone()
    {
        if (candidateZones != null)
        {
            foreach (var zone in candidateZones)
                if (zone != null && !zone.IsFull) return zone;
        }

        // Fallback: any non-full zone in the scene, in case candidateZones wasn't wired
        AnimalZone[] allZones = Object.FindObjectsByType<AnimalZone>(FindObjectsSortMode.None);
        foreach (var zone in allZones)
            if (!zone.IsFull) return zone;

        return null;
    }

    private void SpawnPurchasedAnimal(AnimalData data, AnimalZone zone)
    {
        GameObject go = new GameObject(data.animalName);
        go.transform.position = zone.GetRandomPointInZone();

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = data.idleSprite;
        sr.sortingOrder = 5;

        var animal = go.AddComponent<Animal>();
        animal.InitializeFromMarket(data, zone); // before Start() runs, which first reads animalData

        go.AddComponent<AnimalAI>();
    }

    // =========================================================================
    // Sell flow
    // =========================================================================

    private void BuildSellRows()
    {
        foreach (var row in _sellRows)
            if (row != null) Destroy(row);
        _sellRows.Clear();

        if (sellListContainer == null || sellRowPrefab == null || AnimalRoster.Instance == null)
            return;

        foreach (Animal animal in AnimalRoster.Instance.GetAllAnimals())
        {
            if (animal == null) continue;

            int sellPrice = GetSellPrice(animal);

            GameObject rowGO = Instantiate(sellRowPrefab, sellListContainer);
            _sellRows.Add(rowGO);

            var row = rowGO.GetComponent<AnimalMarketSellRow>();
            if (row == null) continue;
            row.Initialize(animal, sellPrice, OnSellClicked);
        }
    }

    private int GetSellPrice(Animal animal)
    {
        if (_currentMarket == null || animal.AnimalData == null)
            return _currentMarket?.defaultSellPriceIfNotInCatalog ?? 25;

        foreach (var entry in _currentMarket.catalogEntries)
        {
            if (entry.animalData == animal.AnimalData)
                return Mathf.Max(1, Mathf.RoundToInt(entry.buyPrice * _currentMarket.sellRateMultiplier));
        }

        return _currentMarket.defaultSellPriceIfNotInCatalog;
    }

    private void OnSellClicked(Animal animal, int sellPrice)
    {
        _pendingSellAnimal = animal;
        _pendingSellPrice = sellPrice;

        if (confirmationPanel == null)
        {
            ExecuteSell();
            return;
        }

        if (confirmNameText != null)
        {
            sellConfirmText.Arguments = new object[] { animal.GetDisplayName() };
            confirmNameText.text = sellConfirmText.SafeGetLocalizedString();
        }
        if (confirmPriceText != null)
        {
            sellGoldGainText.Arguments = new object[] { sellPrice };
            confirmPriceText.text = sellGoldGainText.SafeGetLocalizedString();
        }

        confirmationPanel.SetActive(true);
    }

    private void OnConfirmSell()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        ExecuteSell();
    }

    private void ExecuteSell()
    {
        if (_pendingSellAnimal == null || _playerStats == null)
        {
            ShowFeedback(saleFailedText.SafeGetLocalizedString(), true);
            return;
        }

        string name = _pendingSellAnimal.GetDisplayName();
        _playerStats.AddMoney(_pendingSellPrice);
        Destroy(_pendingSellAnimal.gameObject); // Animal.OnDestroy() unregisters from roster/zone/save

        _pendingSellAnimal = null;
        _pendingSellPrice = 0;

        RefreshGoldDisplay();
        BuildSellRows();
        soldText.Arguments = new object[] { name };
        ShowFeedback(soldText.SafeGetLocalizedString(), false);
    }

    private void HideConfirmation()
    {
        _pendingSellAnimal = null;
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>Discount: 0.2% per relationship point, max 20% at 100 affinity. Same formula as ShopUI.</summary>
    private float GetDiscount()
    {
        if (string.IsNullOrEmpty(_currentMarket?.marketKeeperNpcId)) return 0f;
        if (ConversationMemory.Instance == null) return 0f;

        float rel = ConversationMemory.Instance.GetRelationshipLevel(_currentMarket.marketKeeperNpcId);
        return Mathf.Clamp01(rel * 0.002f);
    }

    private void RefreshDiscountDisplay()
    {
        if (relationshipDiscountText == null) return;

        float discount = GetDiscount();
        if (discount > 0f)
        {
            friendshipDiscountText.Arguments = new object[] { Mathf.RoundToInt(discount * 100) };
            relationshipDiscountText.text = friendshipDiscountText.SafeGetLocalizedString();
        }
        else
        {
            relationshipDiscountText.text = "";
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

    private void ShowFeedback(string message, bool isError)
    {
        if (feedbackText == null) return;

        if (_feedbackCoroutine != null)
            StopCoroutine(_feedbackCoroutine);

        feedbackText.text = message;
        feedbackText.color = isError ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.2f, 0.75f, 0.3f);
        feedbackText.gameObject.SetActive(true);
        _feedbackCoroutine = StartCoroutine(HideFeedbackAfterDelay());
    }

    private System.Collections.IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDisplaySeconds);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        _feedbackCoroutine = null;
    }

    private void DisablePlayerMovement()
    {
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        player?.DisableMovement();
    }

    private void EnablePlayerMovement()
    {
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        player?.EnableMovement();
    }

    // =========================================================================
    // ISaveable — persist limited catalog stock between sessions
    // =========================================================================

    public void SaveData(GameData gameData)
    {
        if (_currentMarket == null || gameData?.worldData == null) return;

        foreach (var entry in _currentMarket.catalogEntries)
        {
            if (entry.animalData == null || entry.IsUnlimited) continue;
            string key = $"animalmarket_{_currentMarket.name}_{entry.animalData.name}_purchased";
            gameData.worldData.worldCounters[key] = entry.purchasedCount;
        }
    }

    public void LoadData(GameData gameData)
    {
        if (_currentMarket == null || gameData?.worldData == null) return;

        foreach (var entry in _currentMarket.catalogEntries)
        {
            if (entry.animalData == null || entry.IsUnlimited) continue;
            string key = $"animalmarket_{_currentMarket.name}_{entry.animalData.name}_purchased";
            if (gameData.worldData.worldCounters.TryGetValue(key, out int purchased))
                entry.purchasedCount = purchased;
        }
    }
}

} // namespace SowurShield.Animals
