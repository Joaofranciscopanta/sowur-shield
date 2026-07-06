using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;

namespace SowurShield.Core
{

/// <summary>
/// Ensures UI elements are properly connected to PlayerStats data
/// Add this component to ensure UI updates work correctly
/// </summary>
public class UIDataConnector : MonoBehaviour
{
    [Header("Auto-Connect Settings")]
    [Tooltip("Automatically find and connect UI elements on Start")]
    public bool autoConnectOnStart = true;
    
    [Header("Manual Assignments (Optional)")]
    public PlayerStats playerStats;
    public UIManagerPlayer uiManager;
    
    [Header("UI Element Overrides (Optional)")]
    public Slider staminaSlider;
    public TextMeshProUGUI moneyText;
    public Text legacyMoneyText; // For regular UI Text component

    [Header("Localization")]
    [SerializeField] private LocalizedString moneyLabelLocalized; // table "UI_Common", key "ui_common.money_label"

    private void Start()
    {
        if (autoConnectOnStart)
        {
            ConnectUIElements();
        }

        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        if (legacyMoneyText != null && playerStats != null)
        {
            moneyLabelLocalized.Arguments = new object[] { playerStats.money };
            legacyMoneyText.text = moneyLabelLocalized.SafeGetLocalizedString();
        }
    }
    
    [ContextMenu("Connect UI Elements")]
    public void ConnectUIElements()
    {

        
        // Find components if not assigned
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
            
        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManagerPlayer>();
        
        if (playerStats == null)
        {

            return;
        }
        
        // Connect stamina/energy slider
        if (staminaSlider == null && uiManager != null)
            staminaSlider = uiManager.staminaSlider;
            
        if (staminaSlider != null)
        {
            // Connect to PlayerStats for direct updates
            playerStats.energySlider = staminaSlider;

            
            // Initial update
            staminaSlider.maxValue = playerStats.maxEnergy;
            staminaSlider.value = playerStats.currentEnergy;
        }
        else
        {

        }
        
        // Connect money text
        if (moneyText == null && uiManager != null)
            moneyText = uiManager.moneyText;
            
        if (moneyText != null)
        {
            // Create a wrapper for TextMeshProUGUI to work with PlayerStats
            EnsureMoneyTextUpdater();

            
            // Initial update
            UpdateMoneyDisplay();
        }
        else if (legacyMoneyText != null)
        {
            playerStats.moneyText = legacyMoneyText;

            
            // Initial update
            moneyLabelLocalized.Arguments = new object[] { playerStats.money };
            legacyMoneyText.text = moneyLabelLocalized.SafeGetLocalizedString();
        }
        else
        {

        }
        
        // Subscribe to PlayerStats events if UIManager exists
        if (uiManager != null)
        {

            // The UIManagerPlayer should handle its own event subscriptions now
        }
        

    }
    
    private void EnsureMoneyTextUpdater()
    {
        // Check if we already have a MoneyTextUpdater component
        MoneyTextUpdater updater = GetComponent<MoneyTextUpdater>();
        if (updater == null)
        {
            updater = gameObject.AddComponent<MoneyTextUpdater>();
        }
        
        updater.Initialize(playerStats, moneyText);
    }
    
    private void UpdateMoneyDisplay()
    {
        if (moneyText != null && playerStats != null)
        {
            moneyLabelLocalized.Arguments = new object[] { playerStats.money };
            moneyText.text = moneyLabelLocalized.SafeGetLocalizedString();
        }
    }
    
    [ContextMenu("Test Money Change")]
    public void TestMoneyChange()
    {
        if (playerStats != null)
        {
            playerStats.AddMoney(25);

        }
    }
    
    [ContextMenu("Test Energy Change")]
    public void TestEnergyChange()
    {
        if (playerStats != null)
        {
            playerStats.UseEnergy(10);

        }
    }
}

/// <summary>
/// Helper component to bridge TextMeshPro with PlayerStats money updates
/// </summary>
public class MoneyTextUpdater : MonoBehaviour
{
    private PlayerStats playerStats;
    private TextMeshProUGUI moneyText;

    [SerializeField] private LocalizedString moneyLabelLocalized; // table "UI_Common", key "ui_common.money_label"

    public void Initialize(PlayerStats stats, TextMeshProUGUI textComponent)
    {
        // Unsubscribe from previous if any
        if (playerStats != null)
            playerStats.OnMoneyChanged -= UpdateMoneyText;
        
        playerStats = stats;
        moneyText = textComponent;
        
        if (playerStats != null)
        {
            playerStats.OnMoneyChanged += UpdateMoneyText;
            UpdateMoneyText(playerStats.money); // Initial update
        }

        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void UpdateMoneyText(int newMoney)
    {
        if (moneyText != null)
        {
            moneyLabelLocalized.Arguments = new object[] { newMoney };
            moneyText.text = moneyLabelLocalized.SafeGetLocalizedString();
        }
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        if (playerStats != null)
            UpdateMoneyText(playerStats.money);
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnMoneyChanged -= UpdateMoneyText;
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }
}

} // namespace SowurShield.Core