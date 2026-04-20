using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using SowurShield.Animals;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for Phase 1 combat features:
///   - StatusEffect: Burn, Shield, Stun apply/tick/expire correctly
///   - Skill cooldown: player skill tracks cooldown turns accurately
///   - Enemy skill use-chance: 0 chance yields no ready skill
///
/// CombatUnit is a MonoBehaviour so every test creates a real GameObject.
/// All created objects are destroyed in TearDown to prevent scene pollution.
///
/// Tests do NOT use [UnityTest] — all assertions are synchronous.
/// No coroutines are awaited; we drive state via public API calls only.
/// </summary>
public class CombatPhase1Tests
{
    // ── tracked objects ──────────────────────────────────────────────────────

    private List<Object> _cleanupList = new List<Object>();

    private T Track<T>(T obj) where T : Object
    {
        _cleanupList.Add(obj);
        return obj;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var obj in _cleanupList)
        {
            if (obj != null) Object.DestroyImmediate(obj);
        }
        _cleanupList.Clear();

        // Clear TurnManager singleton if any test created one
        if (TurnManager.Instance != null)
            Object.DestroyImmediate(TurnManager.Instance.gameObject);
    }

    // ── factory helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates and initialises a CombatUnit using InitializeAsEnemy so health
    /// and other private stats are set without needing a real Animal asset.
    /// Set isPlayerUnit before calling if you need a player unit — the method
    /// documentation says to do exactly that.
    /// </summary>
    private CombatUnit CreateUnit(bool isPlayer = false,
        float hp = 100f, float atk = 10f, float def = 5f, float spd = 10f)
    {
        var go = new GameObject("TestUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = isPlayer;
        unit.InitializeAsEnemy("TestUnit", hp, atk, def, spd);
        return unit;
    }

    /// <summary>
    /// Creates a minimal AnimalSkill ScriptableObject. Does not need to be
    /// saved to disk; ScriptableObject.CreateInstance works in EditMode.
    /// </summary>
    private AnimalSkill CreateSkill(SkillType type = SkillType.Active, int cooldown = 2,
        float damageMultiplier = 1.5f,
        AnimalSkillEffect statusEffect = AnimalSkillEffect.None,
        float statusValue = 0f, int statusDuration = 0,
        float useChance = 1f)
    {
        var skill = ScriptableObject.CreateInstance<AnimalSkill>();
        Track(skill);
        skill.skillType = type;
        skill.cooldownTurns = cooldown;
        skill.damageMultiplier = damageMultiplier;
        skill.statusEffect = statusEffect;
        skill.statusEffectValue = statusValue;
        skill.statusEffectDuration = statusDuration;
        return skill;
    }

    // =========================================================================
    // STATUS EFFECT — Burn
    // =========================================================================

    [Test]
    public void StatusEffect_BurnDealsDamagePerTick()
    {
        var unit = CreateUnit();
        unit.ApplyStatusEffect(StatusEffectType.Burn, 10f, 2);

        float dmg1 = unit.TickStatusEffects(); // turn 1: tick → 1 remaining
        float dmg2 = unit.TickStatusEffects(); // turn 2: tick → 0 remaining, removed
        float dmg3 = unit.TickStatusEffects(); // turn 3: no effects left

        Assert.AreEqual(10f, dmg1, 0.001f, "Burn tick 1 should deal 10 damage");
        Assert.AreEqual(10f, dmg2, 0.001f, "Burn tick 2 should deal 10 damage");
        Assert.AreEqual(0f,  dmg3, 0.001f, "Burn tick 3 should deal 0 — effect expired");
    }

    [Test]
    public void StatusEffect_BurnEffectRemovedAfterExpiry()
    {
        // After the duration is exhausted the effect list must be empty,
        // verified indirectly: a third tick returns 0.
        var unit = CreateUnit();
        unit.ApplyStatusEffect(StatusEffectType.Burn, 5f, 1);

        unit.TickStatusEffects(); // consumes the 1 remaining turn
        float dmgAfterExpiry = unit.TickStatusEffects();

        Assert.AreEqual(0f, dmgAfterExpiry, 0.001f,
            "No burn damage should be returned after effect expires");
    }

    // =========================================================================
    // STATUS EFFECT — Shield
    // =========================================================================

    [Test]
    public void StatusEffect_ShieldReducesDamage()
    {
        var unit = CreateUnit(hp: 200f);
        float initialHealth = unit.currentHealth;

        unit.ApplyStatusEffect(StatusEffectType.Shield, 0.5f, 1);
        unit.TakeDamageWithShield(100f);

        // 100 * (1 - 0.5) = 50 damage taken
        Assert.AreEqual(initialHealth - 50f, unit.currentHealth, 0.001f,
            "Shield 0.5 should halve incoming damage");
    }

    [Test]
    public void StatusEffect_ShieldCappedAt90Percent()
    {
        var unit = CreateUnit();

        // Two shields totalling 1.2 — capped at 0.9
        unit.ApplyStatusEffect(StatusEffectType.Shield, 0.6f, 3);
        // Applying a second Shield of the same type refreshes duration (max rule),
        // but the cap test only needs one overlapping check — use a fresh unit
        // per effect via separate units or verify cap on GetShieldReduction directly.
        //
        // Because ApplyStatusEffect refreshes rather than stacks the SAME type,
        // we need to verify cap behaviour through GetShieldReduction when the
        // single effect's value already exceeds 0.9.
        var unit2 = CreateUnit();
        unit2.ApplyStatusEffect(StatusEffectType.Shield, 0.95f, 3);

        float reduction = unit2.GetShieldReduction();

        Assert.AreEqual(0.9f, reduction, 0.001f,
            "Shield reduction must be capped at 0.9 regardless of value");
    }

    [Test]
    public void StatusEffect_ShieldGetShieldReduction_ReturnsZeroWithNoShield()
    {
        var unit = CreateUnit();
        Assert.AreEqual(0f, unit.GetShieldReduction(), 0.001f,
            "GetShieldReduction with no effects should be 0");
    }

    [Test]
    public void StatusEffect_ShieldExpires_AfterDuration()
    {
        var unit = CreateUnit(hp: 200f);

        unit.ApplyStatusEffect(StatusEffectType.Shield, 0.5f, 1);
        unit.TickStatusEffects(); // consume the 1 turn → shield expires

        float reductionAfterExpiry = unit.GetShieldReduction();
        Assert.AreEqual(0f, reductionAfterExpiry, 0.001f,
            "Shield should provide no reduction after it expires");
    }

    // =========================================================================
    // STATUS EFFECT — Stun
    // =========================================================================

    [Test]
    public void StatusEffect_StunFlagsUnit()
    {
        var unit = CreateUnit();
        unit.ApplyStatusEffect(StatusEffectType.Stun, 0f, 1);

        Assert.IsTrue(unit.IsStunned, "Unit should be stunned immediately after ApplyStatusEffect");
    }

    [Test]
    public void StatusEffect_StunClearedAfterOneTick()
    {
        var unit = CreateUnit();
        unit.ApplyStatusEffect(StatusEffectType.Stun, 0f, 1);

        unit.TickStatusEffects(); // stun expires (1 turn duration consumed)

        Assert.IsFalse(unit.IsStunned,
            "Unit should no longer be stunned after TickStatusEffects consumes its duration");
    }

    [Test]
    public void StatusEffect_StunDoesNotDealBurnDamage()
    {
        var unit = CreateUnit();
        unit.ApplyStatusEffect(StatusEffectType.Stun, 0f, 2);

        float dmg = unit.TickStatusEffects();

        Assert.AreEqual(0f, dmg, 0.001f,
            "Stun effect should contribute 0 burn damage on tick");
    }

    // =========================================================================
    // STATUS EFFECT — Refresh (no stacking)
    // =========================================================================

    [Test]
    public void StatusEffect_RefreshDurationNotStack_Stun()
    {
        var unit = CreateUnit();

        // Apply stun for 1 turn, then re-apply for 3 turns.
        // turnsRemaining should become max(1, 3) = 3 — no second entry added.
        unit.ApplyStatusEffect(StatusEffectType.Stun, 0f, 1);
        unit.ApplyStatusEffect(StatusEffectType.Stun, 0f, 3);

        // Consume 2 ticks — stun should still be active after tick 1
        unit.TickStatusEffects(); // 3 → 2
        Assert.IsTrue(unit.IsStunned,
            "After 1 tick from refreshed duration=3 stun, unit should still be stunned");

        unit.TickStatusEffects(); // 2 → 1
        Assert.IsTrue(unit.IsStunned,
            "After 2 ticks from refreshed duration=3 stun, unit should still be stunned");

        unit.TickStatusEffects(); // 1 → 0, removed
        Assert.IsFalse(unit.IsStunned,
            "After 3 ticks the stun should expire");
    }

    [Test]
    public void StatusEffect_RefreshUsesMaxDuration_WhenShorterApplied()
    {
        // If the active duration is already longer than the new one, keep it.
        var unit = CreateUnit();

        unit.ApplyStatusEffect(StatusEffectType.Burn, 5f, 5);
        unit.ApplyStatusEffect(StatusEffectType.Burn, 5f, 1); // shorter — should NOT reduce

        // After 4 ticks burn should still be active (not expired at tick 1)
        unit.TickStatusEffects(); // 5→4
        unit.TickStatusEffects(); // 4→3
        unit.TickStatusEffects(); // 3→2
        float dmgAtTick4 = unit.TickStatusEffects(); // 2→1

        Assert.AreEqual(5f, dmgAtTick4, 0.001f,
            "Burn should still be active on tick 4 (max(5,1)=5 duration was preserved)");
    }

    // =========================================================================
    // STATUS EFFECT — Expired effects not processed
    // =========================================================================

    [Test]
    public void StatusEffect_ExpiredEffectsNotProcessed()
    {
        var unit = CreateUnit();
        unit.ApplyStatusEffect(StatusEffectType.Burn, 10f, 1);

        float dmgTick1 = unit.TickStatusEffects(); // duration 1→0, removed, damage=10
        float dmgTick2 = unit.TickStatusEffects(); // no effects → damage=0

        Assert.AreEqual(10f, dmgTick1, 0.001f, "First tick should return burn damage");
        Assert.AreEqual(0f,  dmgTick2, 0.001f, "Second tick should return 0 — effect is gone");
    }

    // =========================================================================
    // SKILL COOLDOWN — Player unit
    // =========================================================================

    [Test]
    public void SkillCooldown_InitiallyZero_GetReadySkillReturnsSkill()
    {
        var unit = CreateUnit(isPlayer: true);
        var skill = CreateSkill(type: SkillType.Active, cooldown: 2);

        unit.InitializePlayerSkill(skill);

        AnimalSkill ready = unit.GetReadySkill();
        Assert.AreSame(skill, ready,
            "Freshly initialised player skill with 0 cooldown should be returned by GetReadySkill");
    }

    [Test]
    public void SkillCooldown_SetCooldownPreventsUse()
    {
        var unit = CreateUnit(isPlayer: true);
        var skill = CreateSkill(type: SkillType.Active, cooldown: 2);

        unit.InitializePlayerSkill(skill);
        unit.SetSkillOnCooldown(skill); // cooldownRemaining = 2

        AnimalSkill readyImmediately = unit.GetReadySkill();
        Assert.IsNull(readyImmediately,
            "Skill should not be ready immediately after being put on cooldown");
    }

    [Test]
    public void SkillCooldown_TickDecrementsAndReenables()
    {
        var unit = CreateUnit(isPlayer: true);
        var skill = CreateSkill(type: SkillType.Active, cooldown: 2);

        unit.InitializePlayerSkill(skill);
        unit.SetSkillOnCooldown(skill); // cooldownRemaining = 2

        unit.TickSkillCooldown(); // cooldownRemaining = 1
        Assert.IsNull(unit.GetReadySkill(),
            "Skill should still be on cooldown after 1 tick (cooldown=2)");

        unit.TickSkillCooldown(); // cooldownRemaining = 0
        Assert.AreSame(skill, unit.GetReadySkill(),
            "Skill should be ready after 2 ticks (matching cooldownTurns=2)");
    }

    [Test]
    public void SkillCooldown_ThirdTickReturnSkill()
    {
        // Verify the boundary: available exactly at tick == cooldownTurns, not before.
        var unit = CreateUnit(isPlayer: true);
        var skill = CreateSkill(type: SkillType.Active, cooldown: 2);

        unit.InitializePlayerSkill(skill);
        unit.SetSkillOnCooldown(skill);

        // Tick twice
        unit.TickSkillCooldown();
        unit.TickSkillCooldown();

        // Asking on tick 3 (after two decrements to 0) should be ready
        Assert.IsNotNull(unit.GetReadySkill(),
            "GetReadySkill should return non-null once cooldown reaches 0");
    }

    [Test]
    public void SkillCooldown_PassiveSkillNeverReturned()
    {
        var unit = CreateUnit(isPlayer: true);
        var passiveSkill = CreateSkill(type: SkillType.Passive, cooldown: 0);

        unit.InitializePlayerSkill(passiveSkill);

        Assert.IsNull(unit.GetReadySkill(),
            "Passive skills should never be returned by GetReadySkill (only Active skills qualify)");
    }

    [Test]
    public void SkillCooldown_NoSkillInitialised_ReturnsNull()
    {
        var unit = CreateUnit(isPlayer: true);
        // InitializePlayerSkill not called

        Assert.IsNull(unit.GetReadySkill(),
            "GetReadySkill should return null when no skill has been initialised");
    }

    [Test]
    public void SkillCooldown_TickAtZeroDoesNotUnderflow()
    {
        // Ticking when cooldown is already 0 should not go negative or cause issues.
        var unit = CreateUnit(isPlayer: true);
        var skill = CreateSkill(type: SkillType.Active, cooldown: 1);

        unit.InitializePlayerSkill(skill);
        unit.SetSkillOnCooldown(skill); // = 1
        unit.TickSkillCooldown();       // = 0

        // Extra tick — should remain 0 (TickSkillCooldown only decrements when > 0)
        unit.TickSkillCooldown();

        Assert.IsNotNull(unit.GetReadySkill(),
            "Skill should still be ready after extra ticks when cooldown is already 0");
    }

    // =========================================================================
    // SKILL COOLDOWN — Enemy unit
    // =========================================================================

    [Test]
    public void EnemySkill_UseChanceZero_NeverReady()
    {
        var unit = CreateUnit(isPlayer: false);
        var skill = CreateSkill(type: SkillType.Active, cooldown: 0);

        // useChance = 0 means Random.value (0-1) will always be > 0, so skill never fires
        unit.InitializeEnemySkills(new List<AnimalSkill> { skill }, useChance: 0f);

        // Run many attempts to be statistically certain
        int notNullCount = 0;
        for (int i = 0; i < 200; i++)
        {
            if (unit.GetReadySkill() != null)
                notNullCount++;
        }

        Assert.AreEqual(0, notNullCount,
            "With useChance=0 the enemy skill should never be returned (Random.value > 0 always)");
    }

    [Test]
    public void EnemySkill_EmptySkillPool_ReturnsNull()
    {
        var unit = CreateUnit(isPlayer: false);
        unit.InitializeEnemySkills(new List<AnimalSkill>(), useChance: 1f);

        Assert.IsNull(unit.GetReadySkill(),
            "Enemy with empty skill list should return null from GetReadySkill");
    }

    [Test]
    public void EnemySkill_NullSkillPool_ReturnsNull()
    {
        var unit = CreateUnit(isPlayer: false);
        unit.InitializeEnemySkills(null, useChance: 1f);

        Assert.IsNull(unit.GetReadySkill(),
            "Enemy with null skill list should return null from GetReadySkill");
    }

    // =========================================================================
    // TakeDamageWithShield — integration check
    // =========================================================================

    [Test]
    public void TakeDamageWithShield_NoShield_TakesFullDamage()
    {
        var unit = CreateUnit(hp: 100f);
        float initialHealth = unit.currentHealth;

        unit.TakeDamageWithShield(30f);

        Assert.AreEqual(initialHealth - 30f, unit.currentHealth, 0.001f,
            "Without a shield TakeDamageWithShield should apply full damage");
    }

    [Test]
    public void TakeDamageWithShield_WithShield_ReducesDamage()
    {
        var unit = CreateUnit(hp: 200f);
        float initialHealth = unit.currentHealth;

        unit.ApplyStatusEffect(StatusEffectType.Shield, 0.25f, 5);
        unit.TakeDamageWithShield(80f);

        // Expected: 80 * (1 - 0.25) = 60
        Assert.AreEqual(initialHealth - 60f, unit.currentHealth, 0.001f,
            "Shield 0.25 should reduce 80 raw damage to 60");
    }

    [Test]
    public void TakeDamageWithShield_MaxShieldCap_StillDealsMinimumDamage()
    {
        // Even with the 0.9 cap, at least 10% damage must get through.
        var unit = CreateUnit(hp: 200f);
        float initialHealth = unit.currentHealth;

        unit.ApplyStatusEffect(StatusEffectType.Shield, 0.95f, 3); // capped to 0.9
        unit.TakeDamageWithShield(100f);

        // Expected: 100 * (1 - 0.9) = 10
        Assert.AreEqual(initialHealth - 10f, unit.currentHealth, 0.001f,
            "Even at max shield cap (0.9), 10% of damage (10) must still be applied");
    }
}

} // namespace SowurShield.Tests

/* =============================================================================
 * MANUAL COMBAT PHASE 1 CHECKLIST
 * Run these in Unity Play Mode in the CombatScene.
 * Tick each item off once verified.
 * =============================================================================
 *
 * SKILL EXECUTION
 * [ ] Assign an active AnimalSkill asset to a chicken's AnimalData.activeSkill
 *     in the Inspector. Confirm skillType = Active, cooldownTurns >= 1.
 * [ ] Start combat. Observe the chicken uses its skill on its first turn (not
 *     a basic attack) because cooldown starts at 0.
 * [ ] After using the skill, the cooldown counter should prevent re-use for
 *     exactly cooldownTurns subsequent turns of that unit.
 * [ ] Verify in the console logs (TurnManager) that "ExecuteSkill" is logged
 *     on the correct turns and "basic attack" on cooldown turns.
 *
 * BURN STATUS EFFECT
 * [ ] Create an AnimalSkill with statusEffect = Burn, statusEffectValue = 5,
 *     statusEffectDuration = 3.
 * [ ] Assign to a player unit and start combat.
 * [ ] On the enemy target's next 3 turns it should receive 5 damage at the
 *     start of each turn (before its action). Console should show burn damage.
 * [ ] After 3 ticks the enemy should no longer take burn damage.
 * [ ] Verify enemy health bar decreases by the correct amount each tick.
 *
 * STUN STATUS EFFECT
 * [ ] Create an AnimalSkill with statusEffect = Stun, statusEffectDuration = 1.
 * [ ] Assign to a player unit and start combat.
 * [ ] The enemy target should skip its immediately following turn (ATB gauge
 *     refills normally but action is skipped — "stunned" log expected).
 * [ ] After 1 turn the enemy resumes acting normally.
 * [ ] Verify no infinite stun loop if multiple stun skills are chained
 *     (duration refreshes to max, does not stack).
 *
 * SHIELD STATUS EFFECT (self-buff)
 * [ ] Create an AnimalSkill with statusEffect = Shield, affectsSelf = true,
 *     statusEffectValue = 0.5, statusEffectDuration = 2.
 * [ ] Observe the buffed unit takes 50% less damage for 2 of its turns.
 * [ ] Verify after 2 turns the unit returns to taking full damage.
 * [ ] Apply two shield skills back-to-back and confirm reduction does not
 *     exceed 0.9 (90%) — the cap must hold in Play Mode too.
 *
 * DEFEAT HAPPINESS DECAY
 * [ ] Win a battle that includes a dead player unit.
 *     Expected: that unit's Animal loses 3 happiness (check AnimalInfoUI
 *     after returning to the farm scene).
 * [ ] Lose a battle (all player units die).
 *     Expected: every player Animal loses 5 happiness.
 * [ ] Confirm happiness is clamped at 0 — it should not go negative.
 * [ ] Verify surviving player units in a winning battle do NOT lose happiness
 *     due to defeat.
 *
 * REWARD DELIVERY
 * [ ] Win a battle then click "Return to Farm" on BattleResultsUI.
 * [ ] Check PlayerStats.money increased by the stage's goldReward amount.
 * [ ] Confirm any loot items from the stage's lootTable appear in the
 *     player's inventory after returning.
 * [ ] Verify the reward panel shows the correct gold and item names before
 *     the player clicks confirm.
 *
 * ANIMATOR INTEGRATION
 * [ ] Assign an AnimatorController to an AnimalData asset in the Inspector.
 *     The controller must have triggers: Attack, Hurt, Die (matching the
 *     hash constants in CombatUnit).
 * [ ] In combat:
 *     - Attack animation fires when the unit executes its action.
 *     - Hurt animation fires when the unit receives damage.
 *     - Die animation fires and sprite greys out when the unit is defeated.
 * [ ] Confirm no NullReferenceExceptions are logged when animatorController
 *     is null (units without a controller should fall back silently).
 *
 * =============================================================================
 */
