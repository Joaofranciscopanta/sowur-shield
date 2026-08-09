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
    [Tooltip("Speed at which turn gauges fill (multiplier). 6 = ~1.7s per turn at speed 10.")]
    [SerializeField] private float gaugeFilLRate = 6f;

    [Tooltip("Maximum number of actions before battle ends in draw")]
    [SerializeField] private int maxActions = 500;

    [Tooltip("Micro-delay between simultaneous actions (seconds, for visual clarity)")]
    [SerializeField] private float actionMicroDelay = 0.25f;

    // ── Battle speed (1x / 2x) ─────────────────────────────────────────────────
    // Scales the gauge fill rate up and the micro-delay down, so 2x is roughly the
    // pre-2026-08 pacing for players who prefer it. Deliberately NOT Time.timeScale:
    // HitStopController already owns that and briefly drops it to a fraction on big
    // hits, so a speed button writing to it would fight the hit-stop coroutine.
    private const string SpeedPrefKey = "combat_speed";

    /// <summary>Battle speed multiplier (1 = normal, 2 = fast). Persisted in PlayerPrefs.</summary>
    public float SpeedMultiplier { get; private set; } = 1f;

    /// <summary>Fired when the battle speed multiplier changes, with the new value.</summary>
    public event System.Action<float> OnSpeedMultiplierChanged;

    /// <summary>
    /// Set the battle speed multiplier, clamped to the 1x-2x range the HUD exposes.
    /// Persists the choice so the next battle starts at the same speed.
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        float clamped = Mathf.Clamp(multiplier, 1f, 2f);
        if (Mathf.Approximately(clamped, SpeedMultiplier)) return;

        SpeedMultiplier = clamped;
        PlayerPrefs.SetFloat(SpeedPrefKey, clamped);
        OnSpeedMultiplierChanged?.Invoke(clamped);
    }

    /// <summary>Toggle between 1x and 2x battle speed. Returns the new multiplier.</summary>
    public float ToggleSpeedMultiplier()
    {
        SetSpeedMultiplier(SpeedMultiplier >= 2f ? 1f : 2f);
        return SpeedMultiplier;
    }

    // ── Combat mode / active pause ─────────────────────────────────────────────
    // Defaults to Auto rather than ActivePause on purpose: a TurnManager constructed
    // without a TeamAssemblerData (every EditMode/PlayMode test, CombatTestSpawner)
    // must keep resolving turns immediately. InitializeCombat opts into ActivePause
    // only when the assembler explicitly asked for it.

    /// <summary>How the player's units are controlled this battle.</summary>
    public CombatMode Mode { get; private set; } = CombatMode.Auto;

    /// <summary>The player unit currently waiting for a command (null when not waiting).</summary>
    public CombatUnit AwaitingInputFor { get; private set; }

    /// <summary>True while the battle is frozen waiting for the player to choose an action.</summary>
    public bool IsWaitingForPlayerInput => AwaitingInputFor != null;

    /// <summary>
    /// Fired when a player unit's gauge fills in ActivePause mode and the battle is
    /// waiting for a command. The command UI listens to this to open its panel.
    /// </summary>
    public event System.Action<CombatUnit> OnPlayerTurnStarted;

    /// <summary>
    /// Fired once the pending player action has been resolved (or timed out), so the
    /// command UI can close its panel.
    /// </summary>
    public event System.Action OnPlayerTurnEnded;

    /// <summary>
    /// How long (seconds) to wait for a command before falling back to the automatic
    /// action. Without this, a UI that fails to open would freeze the battle forever.
    /// </summary>
    private const float PlayerInputTimeout = 15f;

    /// <summary>Set the combat mode. Exposed for the assembler and for tests.</summary>
    public void SetCombatMode(CombatMode mode) => Mode = mode;

    /// <summary>
    /// Put the manager into the "awaiting a command" state without running the batch
    /// coroutine, so EditMode tests can exercise SubmitPlayerAction's validation rules
    /// directly. Prefer the real flow (PlayMode) for anything involving timing.
    /// </summary>
    public void SetAwaitingInputForTesting(CombatUnit unit)
    {
        AwaitingInputFor = unit;
        pendingAction = null;
    }

    /// <summary>The action submitted for the current turn, if any (test inspection hook).</summary>
    public PlayerAction GetPendingActionForTesting() => pendingAction;

    /// <summary>True if this unit should be commanded by the player rather than the AI.</summary>
    private bool IsPlayerControlled(CombatUnit unit)
        => Mode == CombatMode.ActivePause && unit != null && unit.isPlayerUnit;

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
    private BattleModifier activeModifier = new BattleModifier(BattleModifierType.None);

    /// <summary>The randomized modifier active for this battle (BattleModifierType.None if no roll has occurred yet).</summary>
    public BattleModifier GetActiveModifier() => activeModifier;

    /// <summary>Override the active battle modifier (for deterministic testing).</summary>
    public void SetBattleModifierForTesting(BattleModifier mod)
    {
        activeModifier = mod ?? new BattleModifier(BattleModifierType.None);
    }

    /// <summary>
    /// Weighted roll for this battle's modifier: 60% None, remaining 40% split evenly
    /// across DoubleSpeed, LowVisibility, HealingRain, and GlassCannon (10% each).
    /// </summary>
    private static BattleModifier RollBattleModifier()
    {
        float roll = UnityEngine.Random.value;
        if (roll < 0.6f) return new BattleModifier(BattleModifierType.None);
        if (roll < 0.7f) return new BattleModifier(BattleModifierType.DoubleSpeed);
        if (roll < 0.8f) return new BattleModifier(BattleModifierType.LowVisibility);
        if (roll < 0.9f) return new BattleModifier(BattleModifierType.HealingRain);
        return new BattleModifier(BattleModifierType.GlassCannon);
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

        SpeedMultiplier = Mathf.Clamp(PlayerPrefs.GetFloat(SpeedPrefKey, 1f), 1f, 2f);
    }

    private void OnDestroy()
    {
        // Without this, Instance keeps pointing at the destroyed manager after leaving
        // CombatScene, and the next battle's TurnManager sees a non-null Instance in
        // Awake, destroys itself, and never initializes.
        if (Instance == this)
            Instance = null;
    }

    // Number of times InitializeCombat has retried after finding zero units
    private int initRetryCount = 0;
    private const int MaxInitRetries = 5;
    private const float InitRetryDelay = 0.5f;

    private void Start()
    {
        // A prior bug had Time.timeScale left at 0 when entering CombatScene, which silently
        // prevents Invoke() from ever firing. Self-heals but stays loud since it points at a
        // real bug elsewhere (something left the game paused) if it ever fires again.
        if (Time.timeScale == 0f)
        {
            Debug.LogWarning("[TurnManager] Time.timeScale is 0 at Start() — forcing 1f so combat can initialize.");
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
        if (GridManager.Instance == null)
        {
            Debug.LogError("[TurnManager] GridManager.Instance is null in InitializeCombat — cannot start combat.");
            return;
        }

        // Get all units from grid
        allUnits = GridManager.Instance.GetAllUnits();

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

        // Adopt the mode chosen in the team assembler. Uses the existing instance only —
        // TeamAssemblerData.Instance would create one on demand, which would silently flip
        // every test and CombatTestSpawner battle into ActivePause and hang them.
        var assemblerData = FindFirstObjectByType<TeamAssemblerData>(FindObjectsInactive.Include);
        if (assemblerData != null)
            Mode = assemblerData.combatMode;


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

        // Freeze the battle while a command panel is open. Uses a flag rather than
        // Time.timeScale = 0 so animations, tweens and HitStopController keep working
        // (and because the WorldMap's timeScale=0 has caused silent hangs before).
        if (IsWaitingForPlayerInput)
        {
            UpdateBattleStatusUI();
            return;
        }

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
        float rate = gaugeFilLRate * SpeedMultiplier;
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
                // In ActivePause the batch is walked one unit at a time, so three ready
                // animals queue three commands in sequence rather than opening three panels.
                if (IsPlayerControlled(unit))
                    yield return AwaitPlayerAction(unit);
                else
                    ExecuteUnitTurn(unit);

                totalActionsExecuted++;

                // Micro-delay for visual clarity (units act in quick succession).
                // Shortened by the speed multiplier so 2x speeds up the whole batch,
                // not just the gauge fill.
                float delay = actionMicroDelay / SpeedMultiplier;
                if (delay > 0)
                {
                    yield return new WaitForSeconds(delay);
                }
            }
        }

        isProcessingActions = false;
    }

    /// <summary>
    /// Freeze the battle and wait for the player to submit an action for this unit.
    /// Falls back to the automatic turn if the unit dies while waiting or no command
    /// arrives within <see cref="PlayerInputTimeout"/> seconds.
    /// </summary>
    private System.Collections.IEnumerator AwaitPlayerAction(CombatUnit unit)
    {
        // Tick cooldowns/status up front so the command panel shows the same skill
        // availability the action will actually resolve with. Returns false if the
        // turn was consumed by the tick itself (death or stun), in which case there
        // is nothing to command.
        if (!TickTurnStart(unit))
            return null;

        return AwaitPlayerActionLoop(unit);
    }

    private System.Collections.IEnumerator AwaitPlayerActionLoop(CombatUnit unit)
    {
        pendingAction = null;
        AwaitingInputFor = unit;

        if (BattleStatusUI.Instance != null)
            BattleStatusUI.Instance.HighlightActingUnit(unit);

        OnPlayerTurnStarted?.Invoke(unit);

        float waited = 0f;
        while (pendingAction == null && waited < PlayerInputTimeout)
        {
            // The unit can die while the panel is open (burn tick, enemy action mid-wait).
            if (!unit.IsAlive())
                break;

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        PlayerAction action = pendingAction;
        pendingAction = null;
        AwaitingInputFor = null;

        OnPlayerTurnEnded?.Invoke();

        if (!unit.IsAlive())
        {
            unit.ResetTurnGauge();
            yield break;
        }

        if (action == null)
        {
            // Timed out or the panel never opened — resolve automatically so a UI bug
            // can never hard-freeze the battle.
            Debug.LogWarning($"[TurnManager] No command received for '{unit.name}' within " +
                $"{PlayerInputTimeout}s — falling back to the automatic action.");
            ResolveUnitAction(unit);
            yield break;
        }

        ExecutePlayerAction(unit, action);
    }

    // ── Player-submitted actions (ActivePause) ─────────────────────────────────

    /// <summary>What the player chose to do with the unit that is awaiting a command.</summary>
    public enum PlayerActionType { Attack, Skill, Defend }

    /// <summary>A command submitted by the player for the unit currently awaiting input.</summary>
    public class PlayerAction
    {
        public PlayerActionType type;
        /// <summary>Target for Attack/Skill. Ignored by Defend; falls back to the AI pick if null.</summary>
        public CombatUnit target;
    }

    private PlayerAction pendingAction;

    /// <summary>Defense multiplier granted for one turn by the Defend action.</summary>
    private const float DefendShieldReduction = 0.5f;

    /// <summary>
    /// Fraction of the turn gauge Defend refunds, so giving up an attack means acting
    /// again sooner rather than simply losing the turn.
    /// </summary>
    private const float DefendGaugeRefund = 50f;

    /// <summary>
    /// Submit the player's chosen action for the unit currently awaiting input.
    /// Returns false if nothing is waiting or the action is invalid, so the UI can
    /// keep the panel open instead of silently dropping the command.
    /// </summary>
    public bool SubmitPlayerAction(PlayerAction action)
    {
        if (action == null || AwaitingInputFor == null) return false;
        if (pendingAction != null) return false; // already submitted this turn

        // A skill command is only valid when the unit actually has one off cooldown;
        // otherwise the UI would consume the turn doing nothing.
        if (action.type == PlayerActionType.Skill && AwaitingInputFor.GetReadySkill() == null)
            return false;

        pendingAction = action;
        return true;
    }

    /// <summary>Convenience overload: submit a basic attack on the given target.</summary>
    public bool SubmitAttack(CombatUnit target)
        => SubmitPlayerAction(new PlayerAction { type = PlayerActionType.Attack, target = target });

    /// <summary>Convenience overload: submit the unit's active skill on the given target.</summary>
    public bool SubmitSkill(CombatUnit target)
        => SubmitPlayerAction(new PlayerAction { type = PlayerActionType.Skill, target = target });

    /// <summary>Convenience overload: submit the Defend action.</summary>
    public bool SubmitDefend()
        => SubmitPlayerAction(new PlayerAction { type = PlayerActionType.Defend });

    /// <summary>
    /// Carry out a command submitted by the player. Mirrors ResolveUnitAction, but with
    /// the player's choice of action and target instead of the AI's.
    /// </summary>
    private void ExecutePlayerAction(CombatUnit unit, PlayerAction action)
    {
        switch (action.type)
        {
            case PlayerActionType.Defend:
                // Shield already exists as a status effect, so Defend reuses it rather
                // than introducing a parallel damage-reduction path.
                unit.ApplyStatusEffect(StatusEffectType.Shield, DefendShieldReduction, 1);
                unit.ResetTurnGauge();
                unit.turnGauge += DefendGaugeRefund;
                return;

            case PlayerActionType.Skill:
            {
                AnimalSkill skill = unit.GetReadySkill();
                if (skill != null)
                {
                    CombatUnit target = action.target != null && action.target.IsAlive()
                        ? action.target
                        : SelectSkillTarget(unit, skill);

                    if (target != null)
                    {
                        OnTelegraph?.Invoke(new TelegraphInfo { actor = unit, target = target, skill = skill });
                        ExecuteSkill(unit, skill, target);
                        unit.SetSkillOnCooldown(skill);
                        unit.ResetTurnGauge();
                        return;
                    }
                }
                // Skill became unusable between the panel opening and the command
                // resolving — fall through to a basic attack rather than wasting the turn.
                break;
            }
        }

        CombatUnit attackTarget = action.target != null && action.target.IsAlive()
            ? action.target
            : SelectTarget(unit);

        if (attackTarget != null)
        {
            OnTelegraph?.Invoke(new TelegraphInfo { actor = unit, target = attackTarget, skill = null });
            ExecuteAttack(unit, attackTarget);
        }

        unit.ResetTurnGauge();
    }

    /// <summary>
    /// Execute a single unit's turn: tick cooldowns/status, apply burn, check stun,
    /// then attempt skill or fall back to basic attack.
    /// </summary>
    private void ExecuteUnitTurn(CombatUnit unit)
    {
        if (!TickTurnStart(unit)) return;
        ResolveUnitAction(unit);
    }

    /// <summary>
    /// Start-of-turn upkeep: turn counter, HealingRain, cooldown/status ticks, burn damage
    /// and the stun check. Split out of ExecuteUnitTurn so ActivePause can run it before
    /// opening the command panel and resolve the action later without ticking twice.
    /// Returns false when the turn is already over (unit null/dead/stunned).
    /// </summary>
    private bool TickTurnStart(CombatUnit unit)
    {
        if (unit == null || !unit.IsAlive()) return false;

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
            return false;
        }

        if (!unit.IsAlive()) { unit.ResetTurnGauge(); return false; }

        // Highlight acting unit in UI
        if (BattleStatusUI.Instance != null)
            BattleStatusUI.Instance.HighlightActingUnit(unit);

        return true;
    }

    /// <summary>
    /// Resolve the unit's action for this turn using the AI: use a ready skill if one is
    /// available, otherwise a basic attack. Assumes <see cref="TickTurnStart"/> already ran.
    /// </summary>
    private void ResolveUnitAction(CombatUnit unit)
    {
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
                float finalDamage = ApplyDefense(baseDamage, primaryTarget.GetDefense());

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
    /// Smallest damage any connecting hit may deal. Without a floor, defense/(defense+100)
    /// drives damage asymptotically toward zero, so a high-defense enemy makes an encounter
    /// unwinnable rather than merely hard — the player keeps hitting and nothing happens.
    /// </summary>
    private const float MinimumDamage = 1f;

    /// <summary>
    /// Apply the defense curve to a raw damage figure. Single definition so the basic-attack
    /// and skill paths cannot drift apart.
    /// </summary>
    public static float ApplyDefense(float rawDamage, float defense)
    {
        float damageReduction = 1f - (defense / (defense + 100f));
        return Mathf.Max(MinimumDamage, rawDamage * damageReduction);
    }

    /// <summary>
    /// Estimate the damage attacker's basic attack would deal to target, ignoring accuracy
    /// (Shield is applied separately by the caller). Mirrors the formula in ExecuteAttack.
    /// </summary>
    private float EstimateAttackDamage(CombatUnit attacker, CombatUnit target)
    {
        return ApplyDefense(attacker.GetAttack(), target.GetDefense());
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

        // Drop any pending command so the results screen isn't left behind an open
        // command panel waiting for a unit whose battle is already over.
        if (AwaitingInputFor != null)
        {
            AwaitingInputFor = null;
            pendingAction = null;
            OnPlayerTurnEnded?.Invoke();
        }

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
    /// Get a copy of the enemy units list.
    /// </summary>
    public List<CombatUnit> GetEnemyUnits() => new List<CombatUnit>(enemyUnits);

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
