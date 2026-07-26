using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using SowurShield.Inventory;
using SowurShield.UI;
using UnityEngine.Localization;

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

    [Header("Localized Strings")]
    [SerializeField] private LocalizedString goldCostText; // table "Farming", key "farming.buildingshop.gold_cost"
    [SerializeField] private LocalizedString extraCostText; // table "Farming", key "farming.buildingshop.extra_cost"
    [SerializeField] private LocalizedString purchaseFailedText; // table "Farming", key "farming.buildingshop.purchase_failed"
    [SerializeField] private LocalizedString constructionCompleteText; // table "Farming", key "farming.buildingshop.construction_complete"
    [SerializeField] private LocalizedString cannotBuildText; // table "Farming", key "farming.buildingshop.cannot_build"
    [SerializeField] private LocalizedString notEnoughGoldText; // table "Farming", key "farming.buildingshop.not_enough_gold"
    [SerializeField] private LocalizedString missingMaterialsText; // table "Farming", key "farming.buildingshop.missing_materials"
    [SerializeField] private LocalizedString goldLabelText; // table "Farming", key "farming.buildingshop.gold_label"

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

        ApplyTheme();

        if (buildingPanel  != null) buildingPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (feedbackText   != null) feedbackText.gameObject.SetActive(false);

        if (closeButton    != null) closeButton.onClick.AddListener(CloseShop);
        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(OnConfirmPurchase);
        if (confirmNoButton  != null) confirmNoButton.onClick.AddListener(HideConfirmation);

        if (UIManager.Instance != null)
            UIManager.Instance.RegisterWindow(this);

        SowurShield.Core.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDestroy()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UnregisterWindow(this);

        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        if (IsWindowOpen)
            BuildRows();
    }

    /// <summary>
    /// Restyle the scene-wired building shop with the cozy sprite kit + palette.
    /// Confirmation stays on the generic wood panel; Yes = primary, No = small action.
    /// </summary>
    private void ApplyTheme()
    {
        UITheme theme = UIThemeStyler.LoadTheme();

        UIThemeStyler.StylePanel(buildingPanel, theme);
        UIThemeStyler.StylePanel(confirmationPanel, theme);
        UIThemeStyler.StyleButton(closeButton, theme, UIThemeStyler.ButtonSmallPath);
        UIThemeStyler.StyleButton(confirmYesButton, theme, UIThemeStyler.ButtonPrimaryPath);
        UIThemeStyler.StyleButton(confirmNoButton, theme, UIThemeStyler.ButtonSmallPath);

        if (theme != null)
        {
            UIThemeStyler.TintText(playerGoldText, theme.highlightGold);
            UIThemeStyler.TintText(confirmNameText, theme.backgroundCream);
            UIThemeStyler.TintText(confirmCostText, theme.highlightGold);
        }
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

        // Loaded once rather than per row — LoadTheme goes to Resources.
        UITheme rowTheme = UIThemeStyler.LoadTheme();

        foreach (FarmBuildingData data in allBuildings)
        {
            GameObject rowGO = Instantiate(buildingRowPrefab, buildingListContainer);

            // The prefab's own colours predate the theme (0.15 grey row, flat green button),
            // which read as a different app on top of the cream panel. Styled here because the
            // prefab is Editor-owned and this is the only place rows are created.
            UIThemeStyler.StyleListRow(rowGO, rowTheme);

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
            confirmNameText.text = data.buildingName.SafeGetLocalizedString();

        if (confirmCostText != null)
        {
            goldCostText.Arguments = new object[] { data.goldCost };
            string costLine = goldCostText.SafeGetLocalizedString();
            if (!string.IsNullOrEmpty(data.materialItemName) && data.materialQuantity > 0)
            {
                extraCostText.Arguments = new object[] { data.materialQuantity, data.materialItemName };
                costLine += extraCostText.SafeGetLocalizedString();
            }
            confirmCostText.text = costLine;
        }

        confirmationPanel.SetActive(true);
    }

    private void OnConfirmPurchase()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        ExecutePurchase();
    }

    private void ExecutePurchase()
    {
        if (_pendingPurchase == null || FarmBuildingManager.Instance == null ||
            _playerStats == null || _inventory == null)
        {
            ShowFeedback(purchaseFailedText.SafeGetLocalizedString(), true);
            return;
        }

        FarmBuildingData purchase = _pendingPurchase;
        bool built = FarmBuildingManager.Instance.TryBuild(purchase, _playerStats, _inventory);
        _pendingPurchase = null;

        if (built)
        {
            ShowFeedback(constructionCompleteText.SafeGetLocalizedString(), false);
            RefreshRows();
        }
        else
        {
            // Determine why it failed for a useful message
            string reason = cannotBuildText.SafeGetLocalizedString();
            if (!_playerStats.HasMoney(purchase.goldCost))
                reason = notEnoughGoldText.SafeGetLocalizedString();
            else if (!string.IsNullOrEmpty(purchase.materialItemName) && purchase.materialQuantity > 0)
            {
                missingMaterialsText.Arguments = new object[] { purchase.materialQuantity, purchase.materialItemName };
                reason = missingMaterialsText.SafeGetLocalizedString();
            }
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
        {
            goldLabelText.Arguments = new object[] { _playerStats.Money };
            playerGoldText.text = goldLabelText.SafeGetLocalizedString();
        }
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

} // namespace SowurShield.Core
