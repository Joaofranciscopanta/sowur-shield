using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using SowurShield.Inventory;

namespace SowurShield.Core
{

/// <summary>
/// UI window for purchasing farm buildings (Barn, Greenhouse, etc.).
/// Reads all FarmBuildingData assets from Resources/Buildings/.
///
/// SETUP IN UNITY:
///   1. Create FarmBuildingData assets in Assets/Resources/Buildings/
///      (use Tools > Sowur Shield > Building Creator)
///   2. Add this script to a Canvas child panel
///   3. Assign: buildingPanel, playerGoldText, buildingListContainer,
///      buildingRowPrefab, closeButton
///   4. Optionally assign confirmationPanel, confirmNameText,
///      confirmCostText, confirmYesButton, confirmNoButton,
///      feedbackText for purchase confirmation + feedback
///   5. Open via BuildingShopUI.Instance.OpenShop() or from an IInteractable NPC
///
/// BuildingRow prefab requires a BuildingRow component with its
/// fields wired in the Inspector (see BuildingRow class below).
/// </summary>
public class BuildingShopUI : MonoBehaviour, IUIWindow
{
    public static BuildingShopUI Instance { get; private set; }

    [Header("Main Panel")]
    [SerializeField] private GameObject buildingPanel;
    [SerializeField] private TextMeshProUGUI playerGoldText;
    [SerializeField] private Transform buildingListContainer;
    [SerializeField] private GameObject buildingRowPrefab;
    [SerializeField] private Button closeButton;

    [Header("Confirmation Panel (optional)")]
    [Tooltip("A child panel shown when the player clicks Buy. Leave null to skip confirmation.")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TextMeshProUGUI confirmNameText;
    [SerializeField] private TextMeshProUGUI confirmCostText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Feedback")]
    [Tooltip("Text label for success/failure messages. Leave null to skip.")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private float feedbackDisplaySeconds = 2.5f;

    // Runtime refs
    private PlayerStats _playerStats;
    private Inventory.Inventory _inventory;
    private readonly List<BuildingRow> _rows = new List<BuildingRow>();
    private FarmBuildingData _pendingPurchase;
    private Coroutine _feedbackCoroutine;

    // =========================================================================
    // IUIWindow
    // =========================================================================

    public string WindowName    => "BuildingShop";
    public int    WindowPriority => Core.WindowPriority.Inventory;
    public bool   IsWindowOpen  => buildingPanel != null && buildingPanel.activeSelf;
    public bool   CanCloseWithEsc => true;

    public void OpenWindow()
    {
        if (buildingPanel != null) buildingPanel.SetActive(true);
        HideConfirmation();
        DisablePlayerMovement();
    }

    public void CloseWindow()
    {
        if (buildingPanel != null) buildingPanel.SetActive(false);
        HideConfirmation();
        EnablePlayerMovement();
    }

    public void OnWindowBlocked(string blockedBy)
    {
        Debug.LogWarning($"[BuildingShopUI] Blocked by '{blockedBy}'");
    }

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        if (buildingPanel  != null) buildingPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (feedbackText   != null) feedbackText.gameObject.SetActive(false);

        if (closeButton    != null) closeButton.onClick.AddListener(CloseShop);
        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(OnConfirmPurchase);
        if (confirmNoButton  != null) confirmNoButton.onClick.AddListener(HideConfirmation);

        if (UIManager.Instance != null)
            UIManager.Instance.RegisterWindow(this);
    }

    private void OnDestroy()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UnregisterWindow(this);
    }

    // =========================================================================
    // Public API
    // =========================================================================

    public void OpenShop()
    {
        _playerStats = Object.FindFirstObjectByType<PlayerStats>();
        _inventory   = Object.FindFirstObjectByType<Inventory.Inventory>();

        BuildRows();

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
    // Building row population
    // =========================================================================

    private void BuildRows()
    {
        foreach (var row in _rows)
            if (row != null) Destroy(row.gameObject);
        _rows.Clear();

        RefreshGoldText();

        FarmBuildingData[] allBuildings = Resources.LoadAll<FarmBuildingData>("Buildings");
        if (buildingListContainer == null || buildingRowPrefab == null) return;

        foreach (FarmBuildingData data in allBuildings)
        {
            GameObject rowGO = Instantiate(buildingRowPrefab, buildingListContainer);
            BuildingRow row = rowGO.GetComponent<BuildingRow>();

            if (row == null)
            {
                // Fallback for prefabs without a BuildingRow component
                row = rowGO.AddComponent<BuildingRow>();
                row.AutoWire();
            }

            _rows.Add(row);

            bool alreadyBuilt = FarmBuildingManager.Instance != null &&
                                FarmBuildingManager.Instance.IsBuilt(data.buildingType);
            bool canAfford    = !alreadyBuilt && CheckAffordability(data);

            int playerMaterialCount = GetPlayerMaterialCount(data.materialItemName);

            row.Populate(data, alreadyBuilt, canAfford, playerMaterialCount);

            var captured = data;
            row.SetBuyAction(() => OnBuyClicked(captured));
        }
    }

    private void RefreshRows()
    {
        FarmBuildingData[] allBuildings = Resources.LoadAll<FarmBuildingData>("Buildings");
        int i = 0;
        foreach (var row in _rows)
        {
            if (row == null || i >= allBuildings.Length) break;
            FarmBuildingData data = allBuildings[i++];

            bool alreadyBuilt = FarmBuildingManager.Instance != null &&
                                FarmBuildingManager.Instance.IsBuilt(data.buildingType);
            bool canAfford    = !alreadyBuilt && CheckAffordability(data);
            int  matCount     = GetPlayerMaterialCount(data.materialItemName);

            row.Populate(data, alreadyBuilt, canAfford, matCount);
        }

        RefreshGoldText();
    }

    // =========================================================================
    // Purchase flow
    // =========================================================================

    private void OnBuyClicked(FarmBuildingData data)
    {
        if (data == null) return;
        _pendingPurchase = data;

        // If no confirmation panel, buy immediately
        if (confirmationPanel == null)
        {
            ExecutePurchase();
            return;
        }

        // Show confirmation
        if (confirmNameText != null)
            confirmNameText.text = data.buildingName;

        if (confirmCostText != null)
        {
            string costLine = $"{data.goldCost} gold";
            if (!string.IsNullOrEmpty(data.materialItemName) && data.materialQuantity > 0)
                costLine += $"\n+ {data.materialQuantity}x {data.materialItemName}";
            confirmCostText.text = costLine;
        }

        confirmationPanel.SetActive(true);
    }

    private void OnConfirmPurchase()
    {
        HideConfirmation();
        ExecutePurchase();
    }

    private void ExecutePurchase()
    {
        if (_pendingPurchase == null || FarmBuildingManager.Instance == null ||
            _playerStats == null || _inventory == null)
        {
            ShowFeedback("Purchase failed — missing references.", true);
            return;
        }

        FarmBuildingData purchase = _pendingPurchase;
        bool built = FarmBuildingManager.Instance.TryBuild(purchase, _playerStats, _inventory);
        _pendingPurchase = null;

        if (built)
        {
            ShowFeedback($"Construction complete!", false);
            RefreshRows();
        }
        else
        {
            // Determine why it failed for a useful message
            string reason = "Cannot build.";
            if (!_playerStats.HasMoney(purchase.goldCost))
                reason = "Not enough gold.";
            else if (!string.IsNullOrEmpty(purchase.materialItemName) && purchase.materialQuantity > 0)
                reason = $"Missing {purchase.materialQuantity}x {purchase.materialItemName}.";
            ShowFeedback(reason, true);
        }
    }

    private void HideConfirmation()
    {
        _pendingPurchase = null;
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    // =========================================================================
    // Affordability helpers
    // =========================================================================

    private bool CheckAffordability(FarmBuildingData data)
    {
        if (_playerStats == null) return false;
        if (!_playerStats.HasMoney(data.goldCost)) return false;
        if (!string.IsNullOrEmpty(data.materialItemName) && data.materialQuantity > 0)
        {
            Item mat = ItemDatabase.GetItem(data.materialItemName);
            if (mat == null || _inventory == null) return false;
            if (!_inventory.HasItem(mat, data.materialQuantity)) return false;
        }
        return true;
    }

    private int GetPlayerMaterialCount(string itemName)
    {
        if (string.IsNullOrEmpty(itemName) || _inventory == null) return 0;
        Item mat = ItemDatabase.GetItem(itemName);
        return mat != null ? _inventory.GetItemCount(mat) : 0;
    }

    // =========================================================================
    // Feedback
    // =========================================================================

    private void ShowFeedback(string message, bool isError)
    {
        if (feedbackText == null) return;

        if (_feedbackCoroutine != null)
            StopCoroutine(_feedbackCoroutine);

        feedbackText.text  = message;
        feedbackText.color = isError ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.2f, 0.75f, 0.3f);
        feedbackText.gameObject.SetActive(true);
        _feedbackCoroutine = StartCoroutine(HideFeedbackAfterDelay());
    }

    private IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDisplaySeconds);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        _feedbackCoroutine = null;
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private void RefreshGoldText()
    {
        if (playerGoldText != null && _playerStats != null)
            playerGoldText.text = $"Gold: {_playerStats.Money}g";
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
}

// =============================================================================
// BuildingRow component
// =============================================================================

/// <summary>
/// Attach to the building row prefab. Wire all fields in the Inspector.
/// BuildingShopUI calls Populate() and SetBuyAction() after instantiation.
///
/// PREFAB STRUCTURE (suggested):
///   BuildingRow (BuildingRow component)
///   ├── IconImage        (Image)
///   ├── NameText         (TextMeshProUGUI)
///   ├── EffectText       (TextMeshProUGUI)
///   ├── CostText         (TextMeshProUGUI)
///   ├── MaterialText     (TextMeshProUGUI)  ← "You have: X / Y"
///   ├── StatusText       (TextMeshProUGUI)  ← "✓ Built" or "Cannot afford"
///   └── BuyButton        (Button)
/// </summary>
public class BuildingRow : MonoBehaviour
{
    [Header("Wire these in the Prefab Inspector")]
    [SerializeField] public Image              iconImage;
    [SerializeField] public TextMeshProUGUI    nameText;
    [SerializeField] public TextMeshProUGUI    effectText;
    [SerializeField] public TextMeshProUGUI    costText;
    [SerializeField] public TextMeshProUGUI    materialText;   // "You have: X / Y"
    [SerializeField] public TextMeshProUGUI    statusText;     // built / can't afford
    [SerializeField] public Button             buyButton;

    // Colour palette
    private static readonly Color COLOR_AFFORDABLE   = new Color(0.2f, 0.75f, 0.3f);
    private static readonly Color COLOR_UNAFFORDABLE = new Color(0.85f, 0.35f, 0.25f);
    private static readonly Color COLOR_BUILT        = new Color(0.5f, 0.5f, 0.5f);

    public void Populate(FarmBuildingData data, bool alreadyBuilt, bool canAfford, int playerMaterialCount)
    {
        if (nameText   != null) nameText.text   = data.buildingName;
        if (effectText != null) effectText.text = data.effectDescription;

        // Cost line
        if (costText != null)
        {
            costText.text = alreadyBuilt
                ? ""
                : $"{data.goldCost}g";
        }

        // Material count
        if (materialText != null)
        {
            if (!alreadyBuilt && !string.IsNullOrEmpty(data.materialItemName) && data.materialQuantity > 0)
            {
                materialText.gameObject.SetActive(true);
                materialText.text  = $"{data.materialItemName}: {playerMaterialCount} / {data.materialQuantity}";
                materialText.color = playerMaterialCount >= data.materialQuantity
                    ? COLOR_AFFORDABLE
                    : COLOR_UNAFFORDABLE;
            }
            else
            {
                materialText.gameObject.SetActive(false);
            }
        }

        // Status
        if (statusText != null)
        {
            if (alreadyBuilt)
            {
                statusText.gameObject.SetActive(true);
                statusText.text  = "✓ Built";
                statusText.color = COLOR_BUILT;
            }
            else if (!canAfford)
            {
                statusText.gameObject.SetActive(true);
                statusText.text  = "Cannot afford";
                statusText.color = COLOR_UNAFFORDABLE;
            }
            else
            {
                statusText.gameObject.SetActive(false);
            }
        }

        // Icon
        if (iconImage != null)
        {
            iconImage.sprite  = data.icon;
            iconImage.enabled = data.icon != null;
        }

        // Buy button
        if (buyButton != null)
        {
            buyButton.interactable = !alreadyBuilt && canAfford;
            ColorBlock cb = buyButton.colors;
            cb.normalColor = alreadyBuilt ? COLOR_BUILT
                           : canAfford    ? COLOR_AFFORDABLE
                           :               COLOR_UNAFFORDABLE;
            buyButton.colors = cb;
        }
    }

    public void SetBuyAction(System.Action onBuy)
    {
        if (buyButton == null) return;
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => onBuy?.Invoke());
    }

    /// <summary>
    /// Called when the BuildingRow component is added at runtime (no designer-wired prefab).
    /// Searches child components by index as a best-effort fallback.
    /// Designers should wire the prefab instead.
    /// </summary>
    public void AutoWire()
    {
        var texts  = GetComponentsInChildren<TextMeshProUGUI>();
        var images = GetComponentsInChildren<Image>();

        if (texts.Length > 0) nameText   = texts[0];
        if (texts.Length > 1) effectText = texts[1];
        if (texts.Length > 2) costText   = texts[2];
        if (texts.Length > 3) statusText = texts[3];
        if (images.Length > 1) iconImage = images[1]; // [0] is usually the row background

        buyButton = GetComponentInChildren<Button>();
    }
}

} // namespace SowurShield.Core
