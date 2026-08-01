using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Localization;
using SowurShield.Animals;
using SowurShield.Core;
using SowurShield.UI;

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

    [Header("Localization")]
    [SerializeField] private LocalizedString victoryTitle_Localized; // table "Combat", key "combat.results.victory"
    [SerializeField] private LocalizedString defeatTitle_Localized; // table "Combat", key "combat.results.defeat"
    [SerializeField] private LocalizedString drawTitle_Localized; // table "Combat", key "combat.results.draw"
    [SerializeField] private LocalizedString turnLimitText_Localized; // table "Combat", key "combat.results.turn_limit"
    [SerializeField] private LocalizedString partialRewardsText_Localized; // table "Combat", key "combat.results.partial_rewards"
    [SerializeField] private LocalizedString statsText_Localized; // table "Combat", key "combat.results.stats"
    [SerializeField] private LocalizedString noRewardsText_Localized; // table "Combat", key "combat.results.no_rewards"
    [SerializeField] private LocalizedString rewardsHeaderText_Localized; // table "Combat", key "combat.results.rewards_header"
    [SerializeField] private LocalizedString goldText_Localized; // table "Combat", key "combat.results.gold"
    [SerializeField] private LocalizedString xpText_Localized; // table "Combat", key "combat.results.xp"
    [SerializeField] private LocalizedString itemRewardText_Localized; // table "Combat", key "combat.results.item_reward"
    [SerializeField] private LocalizedString animalHappinessText_Localized; // table "Combat", key "combat.results.animal_happiness"

    [Header("Scene Management")]
    [SerializeField] private string farmSceneName = "SampleScene";

    [Header("Battle Statistics")]
    private int totalTurns;
    private int damageDone;
    private int damageTaken;
    private int unitsLost;
    private int enemiesDefeated;

    // Reward tracking
    private CombatRewardData pendingRewards;
    private bool rewardsAwarded;

    // Cached args from the last ShowResults() call, so the screen can be re-rendered on language change
    private TurnManager.BattleResult? lastResult;
    private int lastSurvivingPlayers;
    private int lastSurvivingEnemies;

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

        EnsureDrawsAboveBattleHud();
        ApplyTheme();

        // Hide all panels initially
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);

        // Setup button callbacks
        SetupButtons();

        SowurShield.Core.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    /// <summary>
    /// Put the results screen above the in-battle HUD.
    ///
    /// The scene gives this canvas sortingOrder 10 — the same as BattleStatusCanvas, so which
    /// one wins is arbitrary. In practice the HUD drew on top and its TurnOrderPanel (y 936-996)
    /// covered the "Victory!" title (y 930-1030) completely, making the results screen look
    /// like it had no heading at all.
    ///
    /// Set from code rather than the Inspector so it holds in every scene that hosts this
    /// canvas. 150 sits above BattleHudOverlay and ConsumableBattleUI (both 100) and below the
    /// achievement toast (200), which should still be able to appear over the results.
    /// </summary>
    private void EnsureDrawsAboveBattleHud()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) return;

        canvas.overrideSorting = true;
        canvas.sortingOrder = ResultsSortingOrder;
    }

    private const int ResultsSortingOrder = 150;


    private void OnDestroy()
    {
        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        // Re-render text only — do not call ShowResults() again, it would reset rewardsAwarded
        // and risk double-granting rewards if the player changes language while this screen is up.
        if (!lastResult.HasValue) return;
        switch (lastResult.Value)
        {
            case TurnManager.BattleResult.Victory:
                ShowVictory(lastSurvivingPlayers, lastSurvivingEnemies);
                break;
            case TurnManager.BattleResult.Defeat:
                ShowDefeat(lastSurvivingPlayers, lastSurvivingEnemies);
                break;
            case TurnManager.BattleResult.Draw:
                ShowDraw(lastSurvivingPlayers, lastSurvivingEnemies);
                break;
        }
    }

    /// <summary>
    /// Restyle the results screens with the cozy sprite kit: the dedicated
    /// panel_victory / panel_defeat art (previously unused), primary Return
    /// buttons, and small-action Retry buttons.
    /// </summary>
    private void ApplyTheme()
    {
        UITheme theme = UIThemeStyler.LoadTheme();

        UIThemeStyler.StylePanel(victoryPanel, theme, UIThemeStyler.PanelVictoryPath);
        UIThemeStyler.StylePanel(defeatPanel, theme, UIThemeStyler.PanelDefeatPath);

        UIThemeStyler.StyleButton(victoryReturnButton, theme, UIThemeStyler.ButtonPrimaryPath);
        UIThemeStyler.StyleButton(victoryRetryButton, theme, UIThemeStyler.ButtonSmallPath);
        UIThemeStyler.StyleButton(defeatReturnButton, theme, UIThemeStyler.ButtonPrimaryPath);
        UIThemeStyler.StyleButton(defeatRetryButton, theme, UIThemeStyler.ButtonSmallPath);

        if (theme != null)
        {
            // Dark text, because panel_victory/panel_defeat are LIGHT art — a gold banner over
            // a cream field. The gold title and cream body these panels used to get were picked
            // for a dark panel and measured 1.20 and 1.03 contrast here, i.e. invisible; the
            // title only became noticeable at all once the canvas stopped being covered by the
            // battle HUD. Dark reads at 8.10 on the banner and 12.48 on the cream field.
            UIThemeStyler.TintText(victoryTitleText, theme.textDark);
            UIThemeStyler.TintText(victoryStatsText, theme.textDark);
            UIThemeStyler.TintText(victoryRewardsText, theme.textDark);
            UIThemeStyler.TintText(defeatStatsText, theme.textDark);

            // The defeat heading goes the OTHER way — cream, not dark. The two panels do not
            // share a ribbon: victory's is gold (dark text reads at 8.10) while defeat's is a
            // deep red where nothing dark works — textDark manages only 2.40 and a deep red
            // 1.54. Cream reaches 5.32, and the ribbon itself already carries the "defeat" red.
            UIThemeStyler.TintText(defeatTitleText, theme.backgroundCream);
        }
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
        lastResult = result;
        lastSurvivingPlayers = playerUnitsAlive;
        lastSurvivingEnemies = enemyUnitsAlive;

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
            victoryTitleText.text = Or(victoryTitle_Localized.SafeGetLocalizedString(), "Victory!");
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
            defeatTitleText.text = Or(defeatTitle_Localized.SafeGetLocalizedString(), "Defeated...");
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
            victoryTitleText.text = Or(drawTitle_Localized.SafeGetLocalizedString(), "Draw");
        }

        // Display battle stats
        if (victoryStatsText != null)
        {
            victoryStatsText.text = GetBattleStatsText(survivingPlayers, survivingEnemies) +
                                   Or(turnLimitText_Localized.SafeGetLocalizedString(), "\n(Turn limit reached)");
        }

        // Display reduced rewards
        if (victoryRewardsText != null)
        {
            victoryRewardsText.text = Or(partialRewardsText_Localized.SafeGetLocalizedString(), "Partial rewards awarded.");
        }

    }

    /// <summary>
    /// Get formatted battle statistics text
    /// </summary>
    private string GetBattleStatsText(int survivingPlayers, int survivingEnemies)
    {
        statsText_Localized.Arguments = new object[] { totalTurns, survivingPlayers, survivingEnemies };
        // One stat per line, mirroring the localized entry. The old single-line fallback needed
        // 493px in a 480px rect, so the last value wrapped onto its own line on its own.
        return Or(statsText_Localized.SafeGetLocalizedString(),
            $"<b>Battle Statistics</b>\n\nTurns: {totalTurns}\nYour Survivors: {survivingPlayers}\nEnemy Survivors: {survivingEnemies}");
    }

    /// <summary>
    /// Fallback so the results screen never renders blank when a LocalizedString is
    /// unwired or the string tables haven't finished preloading.
    /// </summary>
    private static string Or(string localized, string fallback) =>
        string.IsNullOrWhiteSpace(localized) ? fallback : localized;

    /// <summary>
    /// Get formatted rewards text from computed reward data.
    /// </summary>
    private string GetRewardsText()
    {
        if (pendingRewards == null || !pendingRewards.isVictory)
            return Or(noRewardsText_Localized.SafeGetLocalizedString(), "No rewards.");

        var sb = new System.Text.StringBuilder(
            Or(rewardsHeaderText_Localized.SafeGetLocalizedString(), "Rewards:\n"));
        goldText_Localized.Arguments = new object[] { pendingRewards.goldReward };
        sb.AppendLine(Or(goldText_Localized.SafeGetLocalizedString(), $"Gold: +{pendingRewards.goldReward}"));
        if (pendingRewards.xpReward > 0)
        {
            xpText_Localized.Arguments = new object[] { pendingRewards.xpReward };
            sb.AppendLine(Or(xpText_Localized.SafeGetLocalizedString(), $"XP: +{pendingRewards.xpReward}"));
        }
        foreach (var (item, qty) in pendingRewards.lootDrops)
        {
            itemRewardText_Localized.Arguments = new object[] { item.GetDisplayName(), qty };
            sb.AppendLine(Or(itemRewardText_Localized.SafeGetLocalizedString(), $"{item.GetDisplayName()} ×{qty}"));
        }
        if (pendingRewards.animalHappinessBonus > 0)
        {
            animalHappinessText_Localized.Arguments = new object[] { pendingRewards.animalHappinessBonus };
            sb.AppendLine(Or(animalHappinessText_Localized.SafeGetLocalizedString(), $"Happiness: +{pendingRewards.animalHappinessBonus}"));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Award pending rewards to the player. Guarded against double-award.
    /// </summary>
    private void AwardRewards()
    {
        if (pendingRewards == null || !pendingRewards.isVictory || rewardsAwarded) return;
        rewardsAwarded = true;

        // Gold & XP — PlayerStats lives in SampleScene, not CombatScene, so stash
        // pending rewards on the persistent TeamAssemblerData and let PlayerStats
        // apply them when SampleScene loads (mirrors pendingReopenAssembler pattern).
        var stats = FindFirstObjectByType<PlayerStats>();
        if (stats != null)
        {
            stats.AddMoney(pendingRewards.goldReward);
            stats.AddExperience(pendingRewards.xpReward);
        }
        else
        {
            TeamAssemblerData.Instance.pendingGoldReward += pendingRewards.goldReward;
            TeamAssemblerData.Instance.pendingXpReward += pendingRewards.xpReward;
        }

        // Loot
        var inventory = FindFirstObjectByType<SowurShield.Inventory.Inventory>();
        if (inventory != null)
            foreach (var (item, qty) in pendingRewards.lootDrops)
                inventory.AddItem(item, qty);

        // Animal happiness + XP
        foreach (var unit in pendingRewards.survivingPlayerUnits)
        {
            var src = unit.GetSourceAnimal();
            src?.ModifyHappiness(pendingRewards.animalHappinessBonus);
            src?.GainCombatExperience(pendingRewards.xpReward);
        }

        // Persist
        SaveManager.Instance?.SaveGame();
    }

    /// <summary>
    /// Return to farm scene, awarding rewards first.
    /// </summary>
    private void ReturnToFarm()
    {
        Debug.LogWarning("[BattleResultsUI] ReturnToFarm() clicked.");
        AwardRewards();
        Time.timeScale = 1f;
        if (SceneTransitionManager.Instance != null)
        {
            Debug.LogWarning($"[BattleResultsUI] ReturnToFarm() — using SceneTransitionManager to load '{farmSceneName}'.");
            SceneTransitionManager.Instance.LoadScene(farmSceneName);
        }
        else
        {
            Debug.LogWarning($"[BattleResultsUI] ReturnToFarm() — SceneTransitionManager.Instance is null, calling SceneManager.LoadScene('{farmSceneName}') directly.");
            SceneManager.LoadScene(farmSceneName);
        }
    }

    /// <summary>
    /// Retry the battle. Returns to the farm scene and reopens the Team Assembler
    /// for the same stage so the player can reassemble their team.
    /// </summary>
    private void RetryBattle()
    {
        Debug.LogWarning("[BattleResultsUI] RetryBattle() clicked.");
        Time.timeScale = 1f; // Ensure time is running

        TeamAssemblerData.Instance.pendingReopenAssembler = true;

        if (SceneTransitionManager.Instance != null)
        {
            Debug.LogWarning($"[BattleResultsUI] RetryBattle() — using SceneTransitionManager to load '{farmSceneName}'.");
            SceneTransitionManager.Instance.LoadScene(farmSceneName);
        }
        else
        {
            Debug.LogWarning($"[BattleResultsUI] RetryBattle() — SceneTransitionManager.Instance is null, calling SceneManager.LoadScene('{farmSceneName}') directly.");
            SceneManager.LoadScene(farmSceneName);
        }
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
