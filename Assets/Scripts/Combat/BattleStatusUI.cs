using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization;
using SowurShield.Core;
using SowurShield.UI;

namespace SowurShield.Combat
{

/// <summary>
/// Displays battle status information including turn counter, team info, and turn order.
/// Updates in real-time during combat.
///
/// SETUP IN UNITY:
/// 1. Create UI Canvas with this script
/// 2. Add TextMeshPro elements for turn counter and team info
/// 3. Add turn order panel with icons
/// 4. Assign references in Inspector
/// 5. TurnManager will update this each turn
/// </summary>
public class BattleStatusUI : MonoBehaviour
{
    [Header("Status Text Elements")]
    [Tooltip("Displays current turn number")]
    [SerializeField] private TextMeshProUGUI turnCounterText;

    [Tooltip("Displays player team count")]
    [SerializeField] private TextMeshProUGUI playerTeamText;

    [Tooltip("Displays enemy team count")]
    [SerializeField] private TextMeshProUGUI enemyTeamText;

    [Header("Turn Order Display")]
    [Tooltip("Panel containing turn order icons")]
    [SerializeField] private GameObject turnOrderPanel;

    [Tooltip("Prefab for turn order icon (with Image component)")]
    [SerializeField] private GameObject turnOrderIconPrefab;

    [Tooltip("Maximum number of units to show in turn order")]
    [SerializeField] private int maxTurnOrderDisplay = 10;

    [Header("Colors")]
    [SerializeField] private Color playerTeamColor = new Color(0.3f, 0.8f, 1f); // Blue
    [SerializeField] private Color enemyTeamColor = new Color(1f, 0.3f, 0.3f); // Red

    [Header("Theme")]
    [SerializeField] private UITheme theme;

    [Header("Localization")]
    [SerializeField] private LocalizedString turnCounterText_Localized; // table "Combat", key "combat.status.turn_counter"
    [SerializeField] private LocalizedString playerTeamText_Localized; // table "Combat", key "combat.status.your_team"
    [SerializeField] private LocalizedString enemyTeamText_Localized; // table "Combat", key "combat.status.enemies"

    // Turn order icon pool
    private List<Image> turnOrderIcons = new List<Image>();

    // Singleton
    public static BattleStatusUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (theme == null)
            theme = Resources.Load<UITheme>("UI/CozyUITheme");
        if (theme != null)
        {
            playerTeamColor = theme.positive;
            enemyTeamColor = theme.negative;
        }

        // Initialize turn order icons
        InitializeTurnOrderIcons();

        SowurShield.Core.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDestroy()
    {
        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private int lastCurrentTurn, lastMaxTurns;
    private int lastPlayerAlive, lastPlayerTotal, lastEnemyAlive, lastEnemyTotal;

    private void HandleLanguageChanged(Locale locale)
    {
        UpdateTurnCounter(lastCurrentTurn, lastMaxTurns);
        UpdateTeamCounts(lastPlayerAlive, lastPlayerTotal, lastEnemyAlive, lastEnemyTotal);
    }

    /// <summary>
    /// Initialize turn order icon pool
    /// </summary>
    private void InitializeTurnOrderIcons()
    {
        if (turnOrderPanel == null || turnOrderIconPrefab == null) return;

        // Create icon pool
        for (int i = 0; i < maxTurnOrderDisplay; i++)
        {
            GameObject iconObj = Instantiate(turnOrderIconPrefab, turnOrderPanel.transform);
            Image icon = iconObj.GetComponent<Image>();

            if (icon != null)
            {
                turnOrderIcons.Add(icon);
                icon.gameObject.SetActive(false); // Hide initially
            }
            else
            {
                Destroy(iconObj);
            }
        }

    }

    /// <summary>
    /// Update turn counter display
    /// </summary>
    public void UpdateTurnCounter(int currentTurn, int maxTurns)
    {
        lastCurrentTurn = currentTurn;
        lastMaxTurns = maxTurns;
        if (turnCounterText != null)
        {
            turnCounterText_Localized.Arguments = new object[] { currentTurn, maxTurns };
            turnCounterText.text = turnCounterText_Localized.SafeGetLocalizedString();
        }
    }

    /// <summary>
    /// Update team counts
    /// </summary>
    public void UpdateTeamCounts(int playerAlive, int playerTotal, int enemyAlive, int enemyTotal)
    {
        lastPlayerAlive = playerAlive;
        lastPlayerTotal = playerTotal;
        lastEnemyAlive = enemyAlive;
        lastEnemyTotal = enemyTotal;

        if (playerTeamText != null)
        {
            playerTeamText_Localized.Arguments = new object[] { playerAlive, playerTotal };
            playerTeamText.text = playerTeamText_Localized.SafeGetLocalizedString();
        }

        if (enemyTeamText != null)
        {
            enemyTeamText_Localized.Arguments = new object[] { enemyAlive, enemyTotal };
            enemyTeamText.text = enemyTeamText_Localized.SafeGetLocalizedString();
        }
    }

    /// <summary>
    /// Update turn order display showing which units will act next
    /// </summary>
    public void UpdateTurnOrder(List<CombatUnit> allUnits)
    {
        if (turnOrderIcons.Count == 0) return;

        // Sort units by turn gauge (highest first)
        var sortedUnits = allUnits
            .Where(u => u != null && u.IsAlive())
            .OrderByDescending(u => u.turnGauge)
            .ThenByDescending(u => u.GetSpeed()) // Tiebreaker: faster units first
            .Take(maxTurnOrderDisplay)
            .ToList();

        // Update icons
        for (int i = 0; i < turnOrderIcons.Count; i++)
        {
            if (i < sortedUnits.Count)
            {
                // Show icon
                CombatUnit unit = sortedUnits[i];
                Image icon = turnOrderIcons[i];

                icon.gameObject.SetActive(true);

                // Color based on team
                icon.color = unit.isPlayerUnit ? playerTeamColor : enemyTeamColor;

                // TODO: Set sprite from unit's animal data if available
                // For now, just use colored squares

                // Add gauge fill indicator (opacity based on how close to acting)
                float fillPercent = unit.turnGauge / 100f;
                Color iconColor = unit.isPlayerUnit ? playerTeamColor : enemyTeamColor;
                iconColor.a = 0.5f + (fillPercent * 0.5f); // 50-100% opacity
                icon.color = iconColor;
            }
            else
            {
                // Hide unused icons
                turnOrderIcons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Update all battle status UI elements
    /// </summary>
    public void UpdateAll(int currentTurn, int maxTurns, List<CombatUnit> playerUnits, List<CombatUnit> enemyUnits, List<CombatUnit> allUnits)
    {
        // Update turn counter
        UpdateTurnCounter(currentTurn, maxTurns);

        // Update team counts
        int playerAlive = playerUnits.Count(u => u != null && u.IsAlive());
        int enemyAlive = enemyUnits.Count(u => u != null && u.IsAlive());

        UpdateTeamCounts(playerAlive, playerUnits.Count, enemyAlive, enemyUnits.Count);

        // Update turn order
        UpdateTurnOrder(allUnits);
    }

    /// <summary>
    /// Show battle status UI
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Hide battle status UI
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Highlight the unit that's currently acting
    /// </summary>
    public void HighlightActingUnit(CombatUnit unit)
    {
        if (unit == null || turnOrderIcons.Count == 0) return;

        // Flash the first icon (the acting unit should be at top of turn order)
        StartCoroutine(FlashIconCoroutine(turnOrderIcons[0]));
    }

    private System.Collections.IEnumerator FlashIconCoroutine(Image icon)
    {
        Color originalColor = icon.color;

        // Flash brighter
        icon.color = Color.white;

        yield return new WaitForSeconds(0.2f);

        // Return to original
        icon.color = originalColor;
    }
}

} // namespace SowurShield.Combat
