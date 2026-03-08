using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using SowurShield.Animals;
using SowurShield.Core;

namespace SowurShield.Combat
{

/// <summary>
/// Manages the victory/defeat UI display at the end of combat.
/// Shows battle statistics, rewards, and allows player to return to farm.
///
/// SETUP IN UNITY:
/// 1. Create Canvas with this script attached
/// 2. Create two panels: victoryPanel and defeatPanel
/// 3. Assign UI components in Inspector
/// 4. TurnManager will call ShowResults() when battle ends
/// </summary>
public class BattleResultsUI : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Panel shown on victory")]
    [SerializeField] private GameObject victoryPanel;

    [Tooltip("Panel shown on defeat")]
    [SerializeField] private GameObject defeatPanel;

    [Header("Victory UI Elements")]
    [SerializeField] private TextMeshProUGUI victoryTitleText;
    [SerializeField] private TextMeshProUGUI victoryStatsText;
    [SerializeField] private TextMeshProUGUI victoryRewardsText;
    [SerializeField] private Button victoryReturnButton;
    [SerializeField] private Button victoryRetryButton;

    [Header("Defeat UI Elements")]
    [SerializeField] private TextMeshProUGUI defeatTitleText;
    [SerializeField] private TextMeshProUGUI defeatStatsText;
    [SerializeField] private Button defeatReturnButton;
    [SerializeField] private Button defeatRetryButton;

    [Header("Scene Management")]
    [SerializeField] private string farmSceneName = "SampleScene";
    [SerializeField] private string combatSceneName = "CombatScene";

    [Header("Battle Statistics")]
    private int totalTurns;
    private int damageDone;
    private int damageTaken;
    private int unitsLost;
    private int enemiesDefeated;

    // Reward tracking
    private CombatRewardData pendingRewards;
    private bool rewardsAwarded;

    // Singleton instance
    public static BattleResultsUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Hide all panels initially
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);

        // Setup button callbacks
        SetupButtons();
    }

    /// <summary>
    /// Setup button click handlers
    /// </summary>
    private void SetupButtons()
    {
        if (victoryReturnButton != null)
            victoryReturnButton.onClick.AddListener(ReturnToFarm);

        if (victoryRetryButton != null)
            victoryRetryButton.onClick.AddListener(RetryBattle);

        if (defeatReturnButton != null)
            defeatReturnButton.onClick.AddListener(ReturnToFarm);

        if (defeatRetryButton != null)
            defeatRetryButton.onClick.AddListener(RetryBattle);
    }

    /// <summary>
    /// Show battle results screen
    /// </summary>
    public void ShowResults(TurnManager.BattleResult result, int turns, int playerUnitsAlive, int enemyUnitsAlive, CombatRewardData rewards = null)
    {
        pendingRewards = rewards;
        rewardsAwarded = false;
        totalTurns = turns;

        switch (result)
        {
            case TurnManager.BattleResult.Victory:
                ShowVictory(playerUnitsAlive, enemyUnitsAlive);
                break;

            case TurnManager.BattleResult.Defeat:
                ShowDefeat(playerUnitsAlive, enemyUnitsAlive);
                break;

            case TurnManager.BattleResult.Draw:
                ShowDraw(playerUnitsAlive, enemyUnitsAlive);
                break;
        }
    }

    /// <summary>
    /// Display victory screen
    /// </summary>
    private void ShowVictory(int survivingPlayers, int survivingEnemies)
    {
        if (victoryPanel == null)
        {
            return;
        }

        victoryPanel.SetActive(true);

        // Set title
        if (victoryTitleText != null)
        {
            victoryTitleText.text = "*** VICTORY! ***";
        }

        // Display battle stats
        if (victoryStatsText != null)
        {
            victoryStatsText.text = GetBattleStatsText(survivingPlayers, survivingEnemies);
        }

        // Display rewards
        if (victoryRewardsText != null)
        {
            victoryRewardsText.text = GetRewardsText();
        }

    }

    /// <summary>
    /// Display defeat screen
    /// </summary>
    private void ShowDefeat(int survivingPlayers, int survivingEnemies)
    {
        if (defeatPanel == null)
        {
            return;
        }

        defeatPanel.SetActive(true);

        // Set title
        if (defeatTitleText != null)
        {
            defeatTitleText.text = "-- DEFEAT --";
        }

        // Display battle stats
        if (defeatStatsText != null)
        {
            defeatStatsText.text = GetBattleStatsText(survivingPlayers, survivingEnemies);
        }

    }

    /// <summary>
    /// Display draw screen (use victory panel with different text)
    /// </summary>
    private void ShowDraw(int survivingPlayers, int survivingEnemies)
    {
        if (victoryPanel == null)
        {
            return;
        }

        victoryPanel.SetActive(true);

        // Set title
        if (victoryTitleText != null)
        {
            victoryTitleText.text = "⏱️ DRAW ⏱️";
        }

        // Display battle stats
        if (victoryStatsText != null)
        {
            victoryStatsText.text = GetBattleStatsText(survivingPlayers, survivingEnemies) +
                                   "\n\nTurn limit reached!";
        }

        // Display reduced rewards
        if (victoryRewardsText != null)
        {
            victoryRewardsText.text = "Partial Rewards:\n" +
                                     "Gold: 50\n" +
                                     "XP: 25 (all units)";
        }

    }

    /// <summary>
    /// Get formatted battle statistics text
    /// </summary>
    private string GetBattleStatsText(int survivingPlayers, int survivingEnemies)
    {
        return $"<b>Battle Statistics</b>\n\n" +
               $"Turns: {totalTurns}\n" +
               $"Your Survivors: {survivingPlayers}\n" +
               $"Enemy Survivors: {survivingEnemies}\n";
    }

    /// <summary>
    /// Get formatted rewards text from computed reward data.
    /// </summary>
    private string GetRewardsText()
    {
        if (pendingRewards == null || !pendingRewards.isVictory)
            return "No rewards.";

        var sb = new System.Text.StringBuilder("<b>Rewards</b>\n\n");
        sb.AppendLine($"Gold: {pendingRewards.goldReward}");
        foreach (var (item, qty) in pendingRewards.lootDrops)
            sb.AppendLine($"{item.itemName} x{qty}");
        if (pendingRewards.animalHappinessBonus > 0)
            sb.AppendLine($"Animal Happiness: +{pendingRewards.animalHappinessBonus}");
        return sb.ToString();
    }

    /// <summary>
    /// Award pending rewards to the player. Guarded against double-award.
    /// </summary>
    private void AwardRewards()
    {
        if (pendingRewards == null || !pendingRewards.isVictory || rewardsAwarded) return;
        rewardsAwarded = true;

        // Gold
        var stats = FindFirstObjectByType<PlayerStats>();
        stats?.AddMoney(pendingRewards.goldReward);

        // Loot
        var inventory = FindFirstObjectByType<SowurShield.Inventory.Inventory>();
        if (inventory != null)
            foreach (var (item, qty) in pendingRewards.lootDrops)
                inventory.AddItem(item, qty);

        // Animal happiness
        foreach (var unit in pendingRewards.survivingPlayerUnits)
            unit.GetSourceAnimal()?.ModifyHappiness(pendingRewards.animalHappinessBonus);

        // Persist
        SaveManager.Instance?.SaveGame();
    }

    /// <summary>
    /// Return to farm scene, awarding rewards first.
    /// </summary>
    private void ReturnToFarm()
    {
        AwardRewards();
        Time.timeScale = 1f;
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene(farmSceneName);
        else
            SceneManager.LoadScene(farmSceneName);
    }

    /// <summary>
    /// Retry the battle (reload combat scene)
    /// </summary>
    private void RetryBattle()
    {

        Time.timeScale = 1f; // Ensure time is running
        SceneManager.LoadScene(combatSceneName);
    }

    /// <summary>
    /// Hide all panels
    /// </summary>
    public void HideAll()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
    }

    // ============================================================================
    // PUBLIC API FOR TRACKING STATS (called by TurnManager during combat)
    // ============================================================================

    /// <summary>
    /// Track damage done by player units
    /// </summary>
    public void RecordDamageDone(float damage)
    {
        damageDone += Mathf.RoundToInt(damage);
    }

    /// <summary>
    /// Track damage taken by player units
    /// </summary>
    public void RecordDamageTaken(float damage)
    {
        damageTaken += Mathf.RoundToInt(damage);
    }

    /// <summary>
    /// Track player unit deaths
    /// </summary>
    public void RecordUnitLost()
    {
        unitsLost++;
    }

    /// <summary>
    /// Track enemy defeats
    /// </summary>
    public void RecordEnemyDefeated()
    {
        enemiesDefeated++;
    }

    /// <summary>
    /// Reset battle statistics and pending rewards.
    /// </summary>
    public void ResetStats()
    {
        totalTurns = 0;
        damageDone = 0;
        damageTaken = 0;
        unitsLost = 0;
        enemiesDefeated = 0;
        pendingRewards = null;
        rewardsAwarded = false;
    }
}

} // namespace SowurShield.Combat
