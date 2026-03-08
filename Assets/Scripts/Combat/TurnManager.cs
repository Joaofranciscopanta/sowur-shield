using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SowurShield.Core;
using SowurShield.Inventory;

namespace SowurShield.Combat
{

/// <summary>
/// Manages combat turns using a Turn Gauge System.
/// Units with higher speed fill their gauge faster and act more frequently.
///
/// TURN GAUGE SYSTEM (from PRD):
/// - Each unit has a gauge that fills based on their speed stat
/// - When gauge reaches 100, unit takes action and gauge resets
/// - Speed 20 unit acts twice as often as Speed 10 unit
///
/// SETUP IN UNITY:
/// 1. Create empty GameObject named "TurnManager" in CombatScene
/// 2. Add this component
/// 3. It will automatically find GridManager and start combat
/// </summary>
public class TurnManager : MonoBehaviour
{
    [Header("Combat Configuration")]
    [Tooltip("Speed at which turn gauges fill (multiplier). 10 = ~1s per turn at speed 10.")]
    [SerializeField] private float gaugeFilLRate = 10f;

    [Tooltip("Maximum number of actions before battle ends in draw")]
    [SerializeField] private int maxActions = 500;

    [Tooltip("Micro-delay between simultaneous actions (seconds, for visual clarity)")]
    [SerializeField] private float actionMicroDelay = 0.05f;

    // ATB system tracking
    private int totalActionsExecuted = 0;
    private bool isProcessingActions = false;
#pragma warning disable CS0414
    private float timeSinceLastActionBatch = 0f;
#pragma warning restore CS0414

    [Header("Combat State")]
    [Tooltip("Is combat currently active?")]
    public bool combatActive = false;

    [Tooltip("Current action count (for display/limits)")]
    public int currentTurn = 0; // Kept for compatibility with UI

    [Header("Debug")]
    [Tooltip("Enable detailed combat logging")]
    [SerializeField] private bool verboseLogging = true;

    // Combat participants
    private List<CombatUnit> allUnits = new List<CombatUnit>();
    private List<CombatUnit> playerUnits = new List<CombatUnit>();
    private List<CombatUnit> enemyUnits = new List<CombatUnit>();

    // Battle result
    public enum BattleResult { Ongoing, Victory, Defeat, Draw }
    public BattleResult battleResult = BattleResult.Ongoing;

    // Singleton pattern
    public static TurnManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Wait for grid and units to spawn, then start combat
        Invoke(nameof(InitializeCombat), 1f);
    }

    /// <summary>
    /// Initialize combat by gathering all units
    /// </summary>
    public void InitializeCombat()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        // Get all units from grid
        allUnits = GridManager.Instance.GetAllUnits();

        Debug.LogWarning($"[TurnManager] InitializeCombat — found {allUnits.Count} units on grid.");

        if (allUnits.Count == 0)
        {
            Debug.LogError("[TurnManager] No units found! CombatTeamSpawner/EnemySpawner may have failed.");
            return;
        }

        // Separate player and enemy units
        playerUnits = allUnits.Where(u => u.isPlayerUnit).ToList();
        enemyUnits = allUnits.Where(u => !u.isPlayerUnit).ToList();


        // Initialize battle status UI
        if (BattleStatusUI.Instance != null)
        {
            BattleStatusUI.Instance.UpdateAll(currentTurn, maxActions, playerUnits, enemyUnits, allUnits);
        }

        // Start combat
        StartCombat();
    }

    /// <summary>
    /// Start the combat loop
    /// </summary>
    public void StartCombat()
    {
        combatActive = true;
        currentTurn = 0;
        battleResult = BattleResult.Ongoing;

    }

    private void Update()
    {
        if (!combatActive) return;

        // Fill all unit turn gauges
        FillTurnGauges(Time.deltaTime);

        // Check for units ready to act
        ProcessReadyUnits();

        // Update battle status UI
        UpdateBattleStatusUI();

        // Check for battle end conditions
        CheckBattleEnd();
    }

    /// <summary>
    /// Update battle status UI each frame
    /// </summary>
    private void UpdateBattleStatusUI()
    {
        if (BattleStatusUI.Instance != null)
        {
            BattleStatusUI.Instance.UpdateAll(currentTurn, maxActions, playerUnits, enemyUnits, allUnits);
        }
    }

    /// <summary>
    /// Fill turn gauges for all alive units
    /// </summary>
    private void FillTurnGauges(float deltaTime)
    {
        foreach (CombatUnit unit in allUnits)
        {
            if (unit != null && unit.IsAlive())
            {
                unit.UpdateTurnGauge(deltaTime * gaugeFilLRate);
            }
        }
    }

    /// <summary>
    /// Process any units with full turn gauge (ATB System)
    /// </summary>
    private void ProcessReadyUnits()
    {
        // Don't start new batch while processing current batch
        if (isProcessingActions) return;

        // Get all units ready to act, sorted by speed (highest first)
        var readyUnits = allUnits
            .Where(u => u != null && u.IsReadyToAct())
            .OrderByDescending(u => u.GetSpeed())
            .ThenByDescending(u => u.turnGauge) // Tiebreaker: higher gauge first
            .ToList();

        // If multiple units are ready, process them in sequence with micro-delays
        if (readyUnits.Count > 0)
        {
            StartCoroutine(ProcessActionBatch(readyUnits));
        }
    }

    /// <summary>
    /// Process a batch of ready units with micro-delays between each
    /// </summary>
    private System.Collections.IEnumerator ProcessActionBatch(List<CombatUnit> readyUnits)
    {
        isProcessingActions = true;

        foreach (CombatUnit unit in readyUnits)
        {
            // Check if unit is still alive and ready (might have died during batch)
            if (unit != null && unit.IsAlive() && unit.IsReadyToAct())
            {
                ExecuteUnitTurn(unit);
                totalActionsExecuted++;

                // Micro-delay for visual clarity (units act in quick succession)
                if (actionMicroDelay > 0)
                {
                    yield return new WaitForSeconds(actionMicroDelay);
                }
            }
        }

        isProcessingActions = false;
    }

    /// <summary>
    /// Execute a single unit's turn
    /// </summary>
    private void ExecuteUnitTurn(CombatUnit unit)
    {
        if (unit == null || !unit.IsAlive()) return;

        currentTurn++;

        if (verboseLogging)
        {
        }

        // Highlight acting unit in UI
        if (BattleStatusUI.Instance != null)
        {
            BattleStatusUI.Instance.HighlightActingUnit(unit);
        }

        // Select target
        CombatUnit target = SelectTarget(unit);

        if (target == null)
        {
            unit.ResetTurnGauge();
            return;
        }

        // Execute attack
        ExecuteAttack(unit, target);

        // Reset turn gauge
        unit.ResetTurnGauge();
    }

    /// <summary>
    /// Select a target for this unit to attack
    /// </summary>
    private CombatUnit SelectTarget(CombatUnit attacker)
    {
        // Get enemy team
        List<CombatUnit> enemies = attacker.isPlayerUnit ? enemyUnits : playerUnits;

        // Filter to alive enemies only
        var aliveEnemies = enemies.Where(e => e != null && e.IsAlive()).ToList();

        if (aliveEnemies.Count == 0)
            return null;

        // Priority AI: Target enemies in front columns first (closest to attacker)
        // Player units (right side, columns 6-8) attack enemies from right to left (columns 5→0)
        // Enemy units (left side, columns 0-5) attack players from left to right (columns 6→8)

        CombatUnit target;
        if (attacker.isPlayerUnit)
        {
            // Player attacks: prioritize rightmost enemy columns (closest = column 5, farthest = column 0)
            target = aliveEnemies
                .OrderByDescending(e => e.gridPosition.x) // Rightmost enemies first
                .ThenBy(e => e.currentHealth) // Then lowest HP
                .First();
        }
        else
        {
            // Enemy attacks: prioritize leftmost player columns (closest = column 6, farthest = column 8)
            target = aliveEnemies
                .OrderBy(e => e.gridPosition.x) // Leftmost players first
                .ThenBy(e => e.currentHealth) // Then lowest HP
                .First();
        }

        return target;
    }

    /// <summary>
    /// Execute an attack from attacker to target
    /// </summary>
    private void ExecuteAttack(CombatUnit attacker, CombatUnit target)
    {
        // Flash attacker yellow when attacking
        attacker.FlashAttack();

        // Accuracy check
        float accuracy = attacker.GetAccuracy();
        if (Random.value > accuracy)
        {
            return;
        }

        // Calculate damage (from PRD damage formula)
        float baseDamage = attacker.GetAttack();
        float defense = target.GetDefense();
        float damageReduction = 1 - (defense / (defense + 100));
        float finalDamage = baseDamage * damageReduction;

        // Apply damage (target will flash red)
        target.TakeDamage(finalDamage);

        if (verboseLogging)
        {
        }

        // Check if target died
        if (!target.IsAlive())
        {
        }
    }

    /// <summary>
    /// Check if battle has ended
    /// </summary>
    private void CheckBattleEnd()
    {
        // Check for victory (all enemies dead)
        bool allEnemiesDead = enemyUnits.All(u => u == null || !u.IsAlive());
        if (allEnemiesDead)
        {
            EndBattle(BattleResult.Victory);
            return;
        }

        // Check for defeat (all player units dead)
        bool allPlayersDead = playerUnits.All(u => u == null || !u.IsAlive());
        if (allPlayersDead)
        {
            EndBattle(BattleResult.Defeat);
            return;
        }

        // Check for action limit draw
        if (totalActionsExecuted >= maxActions)
        {
            EndBattle(BattleResult.Draw);
            return;
        }
    }

    /// <summary>
    /// End the battle with a result
    /// </summary>
    private void EndBattle(BattleResult result)
    {
        combatActive = false;
        battleResult = result;

        switch (result)
        {
            case BattleResult.Victory:
                break;
            case BattleResult.Defeat:
                break;
            case BattleResult.Draw:
                break;
        }


        // Compute rewards and update stats
        CombatRewardData rewards = ComputeRewards(result);

        // Display surviving units
        int survivingPlayers = playerUnits.Count(u => u != null && u.IsAlive());
        int survivingEnemies = enemyUnits.Count(u => u != null && u.IsAlive());

        // Show battle results UI
        if (BattleResultsUI.Instance != null)
        {
            BattleResultsUI.Instance.ShowResults(result, currentTurn, survivingPlayers, survivingEnemies, rewards);
        }
    }

    /// <summary>
    /// Get a copy of the player units list.
    /// </summary>
    public List<CombatUnit> GetPlayerUnits() => new List<CombatUnit>(playerUnits);

    /// <summary>
    /// Compute rewards for a battle result.
    /// Marks the stage complete in StageManager and worldFlags on victory.
    /// Updates combatData stats regardless of outcome.
    /// </summary>
    private CombatRewardData ComputeRewards(BattleResult result)
    {
        var data = new CombatRewardData { isVictory = result == BattleResult.Victory };

        StageData stage = StageManager.GetSelectedStage();

        if (data.isVictory)
        {
            data.goldReward = stage != null ? stage.CalculateGoldReward() : 100 + currentTurn * 5;
            data.lootDrops = stage != null ? stage.RollLoot() : new List<(Item, int)>();
            data.animalHappinessBonus = 5f;
            data.survivingPlayerUnits = playerUnits.Where(u => u != null && u.IsAlive()).ToList();

            // Mark stage complete in runtime cache and world flags
            if (stage != null)
            {
                StageManager.CompleteStage(stage);
                var gameData = SaveManager.Instance?.CurrentGameData;
                if (gameData != null)
                    gameData.worldData.worldFlags[$"stage_completed_{stage.stageName}"] = true;
            }

            var combat = SaveManager.Instance?.CurrentGameData?.combatData;
            if (combat != null)
            {
                combat.battlesWon++;
                combat.enemiesDefeated += enemyUnits.Count(u => u != null && !u.IsAlive());
            }
        }
        else
        {
            var combat = SaveManager.Instance?.CurrentGameData?.combatData;
            if (combat != null) combat.battlesLost++;
        }

        return data;
    }

    /// <summary>
    /// Manually start combat (for testing)
    /// </summary>
    [ContextMenu("Start Combat Now")]
    public void StartCombatManual()
    {
        InitializeCombat();
    }

    /// <summary>
    /// Stop combat (for testing)
    /// </summary>
    [ContextMenu("Stop Combat")]
    public void StopCombat()
    {
        combatActive = false;
    }
}

} // namespace SowurShield.Combat
