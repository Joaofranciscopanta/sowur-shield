using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SowurShield.Animals;
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

    // Pre-allocated buffer so ProcessReadyUnits() doesn't alloc every frame
    private readonly List<CombatUnit> _readyBuffer = new List<CombatUnit>();

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
    /// Process any units with full turn gauge (ATB System).
    /// Uses a pre-allocated buffer to avoid per-frame LINQ allocations.
    /// </summary>
    private void ProcessReadyUnits()
    {
        // Don't start new batch while processing current batch
        if (isProcessingActions) return;

        // Collect ready units into the reusable buffer (no alloc when nothing is ready)
        _readyBuffer.Clear();
        for (int i = 0; i < allUnits.Count; i++)
        {
            var u = allUnits[i];
            if (u != null && u.IsReadyToAct())
                _readyBuffer.Add(u);
        }

        if (_readyBuffer.Count == 0) return;

        // Sort: highest speed first, then highest gauge as tiebreaker
        _readyBuffer.Sort((a, b) =>
        {
            int cmp = b.GetSpeed().CompareTo(a.GetSpeed());
            return cmp != 0 ? cmp : b.turnGauge.CompareTo(a.turnGauge);
        });

        // Hand a snapshot to the coroutine so the buffer can be reused next frame
        StartCoroutine(ProcessActionBatch(new List<CombatUnit>(_readyBuffer)));
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
    /// Execute a single unit's turn: tick cooldowns/status, apply burn, check stun,
    /// then attempt skill or fall back to basic attack.
    /// </summary>
    private void ExecuteUnitTurn(CombatUnit unit)
    {
        if (unit == null || !unit.IsAlive()) return;

        currentTurn++;

        // Tick skill cooldown and status effects
        unit.TickSkillCooldown();
        float burnDamage = unit.TickStatusEffects();

        // Apply burn damage (after tick so expired effects don't deal damage).
        // Uses TakeDamageWithShield so Shield status can reduce burn ticks too.
        if (burnDamage > 0f && unit.IsAlive())
            unit.TakeDamageWithShield(burnDamage);

        // Stun: unit loses its turn
        if (unit.IsStunned)
        {
            unit.ResetTurnGauge();
            return;
        }

        if (!unit.IsAlive()) { unit.ResetTurnGauge(); return; }

        // Highlight acting unit in UI
        if (BattleStatusUI.Instance != null)
            BattleStatusUI.Instance.HighlightActingUnit(unit);

        // Check if unit has a ready skill to use
        AnimalSkill skill = unit.GetReadySkill();
        if (skill != null)
        {
            CombatUnit skillTarget = SelectSkillTarget(unit, skill);
            if (skillTarget != null)
            {
                ExecuteSkill(unit, skill, skillTarget);
                unit.SetSkillOnCooldown(skill);
                unit.ResetTurnGauge();
                return;
            }
        }

        // Fall back to basic attack
        CombatUnit target = SelectTarget(unit);
        if (target != null)
            ExecuteAttack(unit, target);

        unit.ResetTurnGauge();
    }

    /// <summary>
    /// Execute a skill from attacker against primaryTarget.
    /// Handles damage, healing, and status effects.
    /// </summary>
    private void ExecuteSkill(CombatUnit attacker, AnimalSkill skill, CombatUnit primaryTarget)
    {
        attacker.FlashAttack();

        // Damage component (active skills with damageMultiplier > 0)
        if (skill.damageMultiplier > 0f && !skill.affectsAllies)
        {
            float accuracy = attacker.GetAccuracy();
            if (UnityEngine.Random.value <= accuracy)
            {
                float baseDamage = attacker.GetAttack() * skill.damageMultiplier;
                float defense = primaryTarget.GetDefense();
                float damageReduction = 1f - (defense / (defense + 100f));
                float finalDamage = baseDamage * damageReduction;
                primaryTarget.TakeDamageWithShield(finalDamage);
            }
        }

        // Heal component — targets self or allies
        if (skill.healAmount > 0f)
        {
            if (skill.affectsSelf)
                attacker.Heal(skill.healAmount);

            if (skill.affectsAllies)
            {
                List<CombatUnit> allies = attacker.isPlayerUnit ? playerUnits : enemyUnits;
                foreach (var ally in allies)
                    if (ally != null && ally.IsAlive())
                        ally.Heal(skill.healAmount);
            }
        }

        // Shield on self (self-buff)
        if (skill.statusEffect == AnimalSkillEffect.Shield && skill.affectsSelf)
        {
            attacker.ApplyStatusEffect(StatusEffectType.Shield, skill.statusEffectValue, skill.statusEffectDuration);
        }

        // Status effect on target (Stun / Burn — offensive, blocked by immunity)
        if (skill.statusEffect == AnimalSkillEffect.Stun && !skill.affectsAllies
            && !primaryTarget.IsImmuneTo(StatusEffectType.Stun))
        {
            primaryTarget.ApplyStatusEffect(StatusEffectType.Stun, 0f, skill.statusEffectDuration > 0 ? skill.statusEffectDuration : 1);
        }
        else if (skill.statusEffect == AnimalSkillEffect.Burn && !skill.affectsAllies
            && !primaryTarget.IsImmuneTo(StatusEffectType.Burn))
        {
            primaryTarget.ApplyStatusEffect(StatusEffectType.Burn, skill.statusEffectValue, skill.statusEffectDuration > 0 ? skill.statusEffectDuration : 2);
        }
    }

    /// <summary>Select primary target for a skill.</summary>
    private CombatUnit SelectSkillTarget(CombatUnit attacker, AnimalSkill skill)
    {
        // Offensive skills always hit an opponent regardless of self-buff flags.
        // Any self-heal from affectsSelf is applied separately inside ExecuteSkill.
        if (skill.damageMultiplier > 0f)
            return SelectTarget(attacker);

        // Pure healing / self-buff skills: primaryTarget is self (ally spread handled in ExecuteSkill).
        return attacker;
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

        // Apply damage — respects any Shield status effect on target
        target.TakeDamageWithShield(finalDamage);

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

        // Apply happiness changes to source animals immediately (before results UI awards more)
        const float defeatHappinessPenalty = -5f;
        const float defeatedUnitPenalty    = -3f;

        if (result == BattleResult.Defeat || result == BattleResult.Draw)
        {
            // All player units take a morale hit on defeat/draw
            foreach (var unit in playerUnits)
                unit?.GetSourceAnimal()?.ModifyHappiness(defeatHappinessPenalty);
        }
        else if (result == BattleResult.Victory)
        {
            // Units that died in a victory still take a small hit
            foreach (var unit in playerUnits)
                if (unit != null && !unit.IsAlive())
                    unit.GetSourceAnimal()?.ModifyHappiness(defeatedUnitPenalty);
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
            data.xpReward   = stage != null ? stage.baseExperienceReward : 50;
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
