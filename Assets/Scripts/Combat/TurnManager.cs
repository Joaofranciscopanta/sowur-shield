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

    // Combat participants
    private List<CombatUnit> allUnits = new List<CombatUnit>();
    private List<CombatUnit> playerUnits = new List<CombatUnit>();
    private List<CombatUnit> enemyUnits = new List<CombatUnit>();

    // Player combo tracking: consecutive player attacks on the same target build a combo.
    private CombatUnit lastComboTarget = null;
    private int comboCount = 0;
    private const int MaxComboCount = 5;
    private const float ComboDamagePerStack = 0.04f; // +4% damage per stack above 1

    /// <summary>Current player combo stack count (0 = no active combo).</summary>
    public int GetComboCount() => comboCount;

    // Grid layout constants (mirrors GridManager defaults: 9 columns, enemy side = 0-5, player side = 6-8).
    private const int EnemyFrontColumn = 5;  // Enemy column closest to the player side.
    private const int PlayerFrontColumn = 6; // Player column closest to the enemy side.

    // ── Battle modifiers ───────────────────────────────────────────────────────
    private BattleModifier activeModifier = new BattleModifier(BattleModifierType.None, "A normal battle.");

    /// <summary>The randomized modifier active for this battle (BattleModifierType.None if no roll has occurred yet).</summary>
    public BattleModifier GetActiveModifier() => activeModifier;

    /// <summary>Override the active battle modifier (for deterministic testing).</summary>
    public void SetBattleModifierForTesting(BattleModifier mod)
    {
        activeModifier = mod ?? new BattleModifier(BattleModifierType.None, "A normal battle.");
    }

    /// <summary>
    /// Weighted roll for this battle's modifier: 60% None, remaining 40% split evenly
    /// across DoubleSpeed, LowVisibility, HealingRain, and GlassCannon (10% each).
    /// </summary>
    private static BattleModifier RollBattleModifier()
    {
        float roll = UnityEngine.Random.value;
        if (roll < 0.6f) return new BattleModifier(BattleModifierType.None, "A normal battle.");
        if (roll < 0.7f) return new BattleModifier(BattleModifierType.DoubleSpeed, "Turn gauges fill twice as fast!");
        if (roll < 0.8f) return new BattleModifier(BattleModifierType.LowVisibility, "Reduced accuracy for all combatants.");
        if (roll < 0.9f) return new BattleModifier(BattleModifierType.HealingRain, "All units heal a little each round.");
        return new BattleModifier(BattleModifierType.GlassCannon, "Damage dealt and received is doubled!");
    }

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

    // Number of times InitializeCombat has retried after finding zero units
    private int initRetryCount = 0;
    private const int MaxInitRetries = 5;
    private const float InitRetryDelay = 0.5f;

    private void Start()
    {
        Debug.LogWarning($"[TurnManager] Start() — scheduling InitializeCombat in 1s. " +
            $"Time.timeScale={Time.timeScale}, Time.time={Time.time}, Time.unscaledTime={Time.unscaledTime}");
        if (Time.timeScale == 0f)
        {
            Debug.LogError("[TurnManager] Time.timeScale is 0 at Start() — Invoke(1s) will NEVER fire! Forcing Time.timeScale = 1f.");
            Time.timeScale = 1f;
        }
        // Wait for grid and units to spawn, then start combat
        Invoke(nameof(InitializeCombat), 1f);
    }

    /// <summary>
    /// Initialize combat by gathering all units
    /// </summary>
    public void InitializeCombat()
    {
        Debug.LogWarning($"[TurnManager] InitializeCombat() called at Time.time={Time.time}, retry={initRetryCount}");

        if (GridManager.Instance == null)
        {
            Debug.LogError("[TurnManager] GridManager.Instance is null in InitializeCombat — cannot start combat.");
            return;
        }

        // Get all units from grid
        allUnits = GridManager.Instance.GetAllUnits();

        Debug.LogWarning($"[TurnManager] InitializeCombat — found {allUnits.Count} units on grid.");

        if (allUnits.Count == 0)
        {
            if (initRetryCount < MaxInitRetries)
            {
                initRetryCount++;
                Debug.LogWarning($"[TurnManager] No units found yet — retrying in {InitRetryDelay}s " +
                    $"(attempt {initRetryCount}/{MaxInitRetries}). Spawners may still be running (slow WebGL load).");
                Invoke(nameof(InitializeCombat), InitRetryDelay);
                return;
            }

            Debug.LogError("[TurnManager] No units found after retries! CombatTeamSpawner/EnemySpawner may have failed " +
                "(check console for [CombatTeamSpawner]/[EnemySpawner] exceptions above).");
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
        else
        {
            Debug.LogError("[TurnManager] BattleStatusUI.Instance is null — turn counter UI will not update!");
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
        activeModifier = RollBattleModifier();
        ApplyClassSynergies();
    }

    /// <summary>Buff duration (in TickStatusEffects calls) used for effectively-permanent buffs.</summary>
    private const int PermanentBuffDuration = int.MaxValue;

    /// <summary>
    /// Grant a +10% attack/defense synergy bonus to player units that share a combat class
    /// with at least two other teammates (3+ units of the same class).
    /// </summary>
    private void ApplyClassSynergies()
    {
        var groups = playerUnits
            .Where(u => u != null && u.GetSourceAnimal() != null && u.GetSourceAnimal().AnimalData != null
                && !string.IsNullOrEmpty(u.GetSourceAnimal().AnimalData.combatClass))
            .GroupBy(u => u.GetSourceAnimal().AnimalData.combatClass);

        foreach (var group in groups)
        {
            if (group.Count() >= 3)
            {
                foreach (CombatUnit unit in group)
                    unit.ApplyStatBuff(1.1f, 1.1f, 1f, PermanentBuffDuration);
            }
        }
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
        float rate = gaugeFilLRate;
        if (activeModifier.type == BattleModifierType.DoubleSpeed)
            rate *= 2f;

        foreach (CombatUnit unit in allUnits)
        {
            if (unit != null && unit.IsAlive())
            {
                unit.UpdateTurnGauge(deltaTime * rate);
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

        // HealingRain modifier: heal all alive units at the start of each round
        // (approximated as the first action after every unit has acted once).
        if (activeModifier.type == BattleModifierType.HealingRain && allUnits.Count > 0 && currentTurn % allUnits.Count == 1)
        {
            foreach (CombatUnit u in allUnits)
            {
                if (u != null && u.IsAlive())
                    u.Heal(BattleModifier.HealingRainAmount);
            }
        }

        // Capture stun state before ticking — TickStatusEffects() decrements and removes
        // a Stun with turnsRemaining==1 down to 0, which would otherwise make IsStunned
        // report false for the very turn that should be skipped.
        bool wasStunned = unit.IsStunned;

        // Tick skill cooldown and status effects
        unit.TickSkillCooldown();
        float burnDamage = unit.TickStatusEffects();

        // Apply burn damage (after tick so expired effects don't deal damage).
        // Uses TakeDamageWithShield so Shield status can reduce burn ticks too.
        if (burnDamage > 0f && unit.IsAlive())
            unit.TakeDamageWithShield(burnDamage);

        // Stun: unit loses its turn
        if (wasStunned)
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
                OnTelegraph?.Invoke(new TelegraphInfo { actor = unit, target = skillTarget, skill = skill });
                ExecuteSkill(unit, skill, skillTarget);
                unit.SetSkillOnCooldown(skill);
                unit.ResetTurnGauge();
                return;
            }
        }

        // Fall back to basic attack
        CombatUnit target = SelectTarget(unit);
        if (target != null)
        {
            OnTelegraph?.Invoke(new TelegraphInfo { actor = unit, target = target, skill = null });
            ExecuteAttack(unit, target);
        }

        unit.ResetTurnGauge();
    }

    /// <summary>Info about an upcoming action, broadcast just before it resolves (for VFX/telegraph hooks).</summary>
    public struct TelegraphInfo
    {
        public CombatUnit actor;
        public CombatUnit target;
        public AnimalSkill skill;
    }

    /// <summary>Fired immediately before an attack or skill resolves (skill is null for basic attacks).</summary>
    public event System.Action<TelegraphInfo> OnTelegraph;

    /// <summary>Hit-stop duration (seconds) applied to critical hits.</summary>
    private const float BigHitCritDuration = 0.08f;

    /// <summary>Hit-stop duration (seconds) applied to large (non-crit) hits.</summary>
    private const float BigHitLargeDuration = 0.05f;

    /// <summary>Damage threshold (as a fraction of target max health) considered a "big hit".</summary>
    private const float BigHitHealthFraction = 0.25f;

    /// <summary>
    /// Fired when an attack or skill deals a critical hit (0.08s) or a large hit — damage
    /// at least 25% of the target's max health (0.05s). Not fired for smaller hits.
    /// </summary>
    public static event System.Action<float> OnBigHit;

    /// <summary>
    /// Raise OnBigHit if the given hit qualifies (crit, or damage >= 25% of target max health).
    /// </summary>
    private static void RaiseBigHitIfQualifying(float damage, float targetMaxHealth, bool isCrit)
    {
        if (isCrit)
            OnBigHit?.Invoke(BigHitCritDuration);
        else if (targetMaxHealth > 0f && damage >= targetMaxHealth * BigHitHealthFraction)
            OnBigHit?.Invoke(BigHitLargeDuration);
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
            float accuracy = GetEffectiveAccuracy(attacker);
            if (UnityEngine.Random.value <= accuracy)
            {
                float baseDamage = attacker.GetAttack() * skill.damageMultiplier;
                float defense = primaryTarget.GetDefense();
                float damageReduction = 1f - (defense / (defense + 100f));
                float finalDamage = baseDamage * damageReduction;

                bool isCrit = UnityEngine.Random.value < attacker.GetCritChance();
                finalDamage = CombatUnit.ApplyCrit(finalDamage, isCrit);

                if (activeModifier.type == BattleModifierType.GlassCannon)
                    finalDamage *= BattleModifier.GlassCannonMultiplier;

                RaiseBigHitIfQualifying(finalDamage, primaryTarget.GetMaxHealth(), isCrit);
                primaryTarget.TakeDamageWithShield(finalDamage, isCrit);
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
        else if (skill.statusEffect == AnimalSkillEffect.Poison && !skill.affectsAllies
            && !primaryTarget.IsImmuneTo(StatusEffectType.Poison))
        {
            primaryTarget.ApplyStatusEffect(StatusEffectType.Poison, skill.statusEffectValue, skill.statusEffectDuration > 0 ? skill.statusEffectDuration : 2);
        }
        else if (skill.statusEffect == AnimalSkillEffect.Weakness && !skill.affectsAllies
            && !primaryTarget.IsImmuneTo(StatusEffectType.Weakness))
        {
            primaryTarget.ApplyStatusEffect(StatusEffectType.Weakness, skill.statusEffectValue, skill.statusEffectDuration > 0 ? skill.statusEffectDuration : 2);
        }

        // Temporary stat buffs (attack/defense/speed multipliers) on self or allies.
        if (skill.attackMultiplier != 1f || skill.defenseMultiplier != 1f || skill.speedMultiplier != 1f)
        {
            int buffDuration = skill.statusEffectDuration > 0 ? skill.statusEffectDuration : 3;

            if (skill.affectsSelf)
                attacker.ApplyStatBuff(skill.attackMultiplier, skill.defenseMultiplier, skill.speedMultiplier, buffDuration);

            if (skill.affectsAllies)
            {
                List<CombatUnit> allies = attacker.isPlayerUnit ? playerUnits : enemyUnits;
                foreach (var ally in allies)
                    if (ally != null && ally.IsAlive())
                        ally.ApplyStatBuff(skill.attackMultiplier, skill.defenseMultiplier, skill.speedMultiplier, buffDuration);
            }
        }
    }

    /// <summary>Select primary target for a skill.</summary>
    private CombatUnit SelectSkillTarget(CombatUnit attacker, AnimalSkill skill)
    {
        bool isOffensiveStatus = skill.statusEffect == AnimalSkillEffect.Burn
            || skill.statusEffect == AnimalSkillEffect.Poison
            || skill.statusEffect == AnimalSkillEffect.Weakness;

        // Aggressive AI focuses offensive status skills (Burn/Poison/Weakness) on the
        // highest-HP enemy (the tank) instead of the default lethal-first/front-column target.
        // Damage skills still fall through to SelectTarget's lethal-first logic below.
        if (!attacker.isPlayerUnit && isOffensiveStatus && attacker.GetAIBehavior() == "Aggressive")
        {
            List<CombatUnit> opponents = playerUnits;
            var aliveOpponents = opponents.Where(o => o != null && o.IsAlive()).ToList();
            if (aliveOpponents.Count > 0)
                return aliveOpponents.OrderByDescending(o => o.currentHealth).First();
        }

        // Offensive skills always hit an opponent regardless of self-buff flags.
        // Any self-heal from affectsSelf is applied separately inside ExecuteSkill.
        if (skill.damageMultiplier > 0f || isOffensiveStatus)
            return SelectTarget(attacker);

        // Support AI directs heal/shield-type skills to the ally with the lowest HP%.
        if (!attacker.isPlayerUnit && attacker.GetAIBehavior() == "Support"
            && (skill.healAmount > 0f || skill.statusEffect == AnimalSkillEffect.Shield || skill.affectsAllies))
        {
            List<CombatUnit> allies = attacker.isPlayerUnit ? playerUnits : enemyUnits;
            var aliveAllies = allies.Where(a => a != null && a.IsAlive()).ToList();
            if (aliveAllies.Count > 0)
                return aliveAllies.OrderBy(a => a.GetHealthPercent()).First();
        }

        // Pure healing / self-buff skills: primaryTarget is self (ally spread handled in ExecuteSkill).
        return attacker;
    }

    /// <summary>
    /// Select a target for this unit to attack.
    ///
    /// Priority:
    /// 1. Lethal kills: any alive enemy this attack would reduce to 0 HP (accounting for
    ///    Shield damage reduction), preferring the lowest-HP killable target — secures kills
    ///    instead of spreading chip damage.
    /// 2. Otherwise, the enemy in the frontmost column (closest to the attacker), tiebroken
    ///    by lowest HP percentage — pressures whichever unit is closest to dying.
    /// </summary>
    private CombatUnit SelectTarget(CombatUnit attacker)
    {
        // Get enemy team
        List<CombatUnit> enemies = attacker.isPlayerUnit ? enemyUnits : playerUnits;

        // Filter to alive enemies only
        var aliveEnemies = enemies.Where(e => e != null && e.IsAlive()).ToList();

        if (aliveEnemies.Count == 0)
            return null;

        // Melee units can only reach the opposing side's front column, unless it's empty
        // (no alive units there), in which case they can reach any column.
        if (attacker.GetAttackRange() == AttackRange.Melee)
        {
            int frontColumn = attacker.isPlayerUnit ? EnemyFrontColumn : PlayerFrontColumn;
            var frontColumnEnemies = aliveEnemies.Where(e => e.gridPosition.x == frontColumn).ToList();
            if (frontColumnEnemies.Count > 0)
                aliveEnemies = frontColumnEnemies;
        }

        // Behavior-aware targeting for enemy AI units (player targeting always uses
        // lethal-first / front-column logic below).
        if (!attacker.isPlayerUnit)
        {
            string behavior = attacker.GetAIBehavior();
            if (behavior == "Defensive")
            {
                // Target the biggest threat: highest effective attack.
                return aliveEnemies.OrderByDescending(e => e.GetAttack()).First();
            }
            if (behavior == "Support")
            {
                // Target the easiest to chip down: lowest effective defense.
                return aliveEnemies.OrderBy(e => e.GetDefense()).First();
            }
        }

        // Lethal-first: secure a kill if this attack would finish off an enemy.
        CombatUnit lethalTarget = null;
        foreach (CombatUnit enemy in aliveEnemies)
        {
            float estimatedDamage = EstimateAttackDamage(attacker, enemy);
            float effectiveHealth = enemy.currentHealth * (1f - enemy.GetShieldReduction());
            if (estimatedDamage < effectiveHealth)
                continue;

            if (lethalTarget == null || enemy.currentHealth < lethalTarget.currentHealth)
                lethalTarget = enemy;
        }
        if (lethalTarget != null)
            return lethalTarget;

        // Priority AI: Target enemies in front columns first (closest to attacker)
        // Player units (right side, columns 6-8) attack enemies from right to left (columns 5→0)
        // Enemy units (left side, columns 0-5) attack players from left to right (columns 6→8)

        CombatUnit target;
        if (attacker.isPlayerUnit)
        {
            // Player attacks: prioritize rightmost enemy columns (closest = column 5, farthest = column 0)
            target = aliveEnemies
                .OrderByDescending(e => e.gridPosition.x) // Rightmost enemies first
                .ThenBy(e => e.GetHealthPercent()) // Then lowest HP%
                .First();
        }
        else
        {
            // Enemy attacks: prioritize leftmost player columns (closest = column 6, farthest = column 8)
            target = aliveEnemies
                .OrderBy(e => e.gridPosition.x) // Leftmost players first
                .ThenBy(e => e.GetHealthPercent()) // Then lowest HP%
                .First();
        }

        return target;
    }

    /// <summary>
    /// Attacker's accuracy after applying the LowVisibility battle modifier (if active),
    /// clamped to a minimum of 0.
    /// </summary>
    private float GetEffectiveAccuracy(CombatUnit attacker)
    {
        float accuracy = attacker.GetAccuracy();
        if (activeModifier.type == BattleModifierType.LowVisibility)
            accuracy -= BattleModifier.LowVisibilityAccuracyPenalty;
        return Mathf.Max(0f, accuracy);
    }

    /// <summary>
    /// Estimate the damage attacker's basic attack would deal to target, ignoring accuracy
    /// (Shield is applied separately by the caller). Mirrors the formula in ExecuteAttack.
    /// </summary>
    private float EstimateAttackDamage(CombatUnit attacker, CombatUnit target)
    {
        float defense = target.GetDefense();
        float damageReduction = 1f - (defense / (defense + 100f));
        return attacker.GetAttack() * damageReduction;
    }

    /// <summary>
    /// Execute an attack from attacker to target
    /// </summary>
    private void ExecuteAttack(CombatUnit attacker, CombatUnit target)
    {
        // Flash attacker yellow when attacking
        attacker.FlashAttack();

        // Combo tracking: only player attacks build/extend a combo. Any enemy
        // attack breaks the player's combo.
        if (attacker.isPlayerUnit)
        {
            if (target == lastComboTarget)
                comboCount = Mathf.Min(comboCount + 1, MaxComboCount);
            else
            {
                comboCount = 1;
                lastComboTarget = target;
            }
        }
        else
        {
            comboCount = 0;
            lastComboTarget = null;
        }

        // Accuracy check
        float accuracy = GetEffectiveAccuracy(attacker);
        if (Random.value > accuracy)
        {
            return;
        }

        // Calculate damage (from PRD damage formula)
        float finalDamage = EstimateAttackDamage(attacker, target);

        // Combo bonus: +4% damage per stack above 1 (player attacks only).
        if (attacker.isPlayerUnit && comboCount > 1)
            finalDamage *= 1f + (comboCount - 1) * ComboDamagePerStack;

        // Critical hit roll
        bool isCrit = Random.value < attacker.GetCritChance();
        finalDamage = CombatUnit.ApplyCrit(finalDamage, isCrit);

        // GlassCannon modifier: every hit (by either side) deals double damage,
        // so damage dealt and received are both effectively doubled.
        if (activeModifier.type == BattleModifierType.GlassCannon)
            finalDamage *= BattleModifier.GlassCannonMultiplier;

        RaiseBigHitIfQualifying(finalDamage, target.GetMaxHealth(), isCrit);

        // Apply damage — respects any Shield status effect on target
        target.TakeDamageWithShield(finalDamage, isCrit);
    }

    /// <summary>
    /// Use a consumable inventory item on a target unit mid-battle (e.g. a healing
    /// item on an injured ally). This is a free action — it does not consume a turn.
    /// Returns true if the item was used and consumed from the player's inventory.
    /// </summary>
    public bool UseConsumableOnUnit(Item item, CombatUnit target)
    {
        if (item == null || !item.isConsumable || target == null || !target.IsAlive())
            return false;

        if (item.healthRestore > 0)
            target.Heal(item.healthRestore);

        SowurShield.Inventory.Inventory inventory = FindFirstObjectByType<SowurShield.Inventory.Inventory>();
        if (inventory != null)
            inventory.RemoveItem(item, 1);

        return true;
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
