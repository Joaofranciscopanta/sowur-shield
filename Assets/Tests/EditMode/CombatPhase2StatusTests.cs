using NUnit.Framework;
using UnityEngine;
using System.Reflection;
using SowurShield.Animals;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for Combat Phase 2 status effects: Poison (stacking damage-over-time)
/// and Weakness (attack/defense reduction).
/// </summary>
public class CombatPhase2StatusTests
{
    private System.Collections.Generic.List<Object> _cleanupList = new System.Collections.Generic.List<Object>();

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

        if (TurnManager.Instance != null)
            Object.DestroyImmediate(TurnManager.Instance.gameObject);
    }

    private CombatUnit CreateUnit(float hp = 100f, float atk = 10f, float def = 5f, float spd = 10f)
    {
        var go = new GameObject("TestUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = false;
        unit.InitializeAsEnemy(go.name, hp, atk, def, spd);
        return unit;
    }

    /// <summary>Minimal AnimalSkill ScriptableObject for ExecuteSkill tests.</summary>
    private AnimalSkill CreateSkill(AnimalSkillEffect statusEffect, float statusValue, int statusDuration,
        bool affectsAllies = false)
    {
        var skill = ScriptableObject.CreateInstance<AnimalSkill>();
        Track(skill);
        skill.skillType = SkillType.Active;
        skill.damageMultiplier = 0f;
        skill.affectsAllies = affectsAllies;
        skill.statusEffect = statusEffect;
        skill.statusEffectValue = statusValue;
        skill.statusEffectDuration = statusDuration;
        return skill;
    }

    private TurnManager CreateTurnManager()
    {
        var go = new GameObject("TurnManager");
        Track(go);
        return go.AddComponent<TurnManager>();
    }

    private static void InvokeExecuteSkill(TurnManager tm, CombatUnit attacker, AnimalSkill skill, CombatUnit target)
    {
        var m = typeof(TurnManager).GetMethod("ExecuteSkill", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "Method 'ExecuteSkill' not found on TurnManager");
        m.Invoke(tm, new object[] { attacker, skill, target });
    }

    // =========================================================================
    // POISON STACKING
    // =========================================================================

    [Test]
    public void ApplyStatusEffect_Poison_StacksIndependently()
    {
        var unit = CreateUnit();

        unit.ApplyStatusEffect(StatusEffectType.Poison, 5f, 3);
        unit.ApplyStatusEffect(StatusEffectType.Poison, 5f, 3);

        Assert.AreEqual(2, unit.GetActiveStatusCount(StatusEffectType.Poison),
            "Applying Poison twice should result in two independent stacks.");
    }

    [Test]
    public void TickStatusEffects_SumsBurnAndPoisonDamage()
    {
        var unit = CreateUnit();

        unit.ApplyStatusEffect(StatusEffectType.Burn, 4f, 2);
        unit.ApplyStatusEffect(StatusEffectType.Poison, 3f, 2);
        unit.ApplyStatusEffect(StatusEffectType.Poison, 2f, 2);

        float tickDamage = unit.TickStatusEffects();

        Assert.AreEqual(9f, tickDamage, 0.001f, "Tick damage should sum Burn (4) + Poison (3) + Poison (2) = 9.");
    }

    [Test]
    public void TickStatusEffects_PoisonStacksExpireIndependently()
    {
        var unit = CreateUnit();

        unit.ApplyStatusEffect(StatusEffectType.Poison, 3f, 1); // expires after this tick
        unit.ApplyStatusEffect(StatusEffectType.Poison, 2f, 2); // survives one more tick

        float firstTick = unit.TickStatusEffects();
        Assert.AreEqual(5f, firstTick, 0.001f, "Both poison stacks should deal damage on the first tick.");
        Assert.AreEqual(1, unit.GetActiveStatusCount(StatusEffectType.Poison), "One poison stack should remain after the first tick.");

        float secondTick = unit.TickStatusEffects();
        Assert.AreEqual(2f, secondTick, 0.001f, "Only the remaining poison stack should deal damage on the second tick.");
        Assert.AreEqual(0, unit.GetActiveStatusCount(StatusEffectType.Poison), "No poison stacks should remain after the second tick.");
    }

    // =========================================================================
    // WEAKNESS — REFRESH (NOT STACK)
    // =========================================================================

    [Test]
    public void ApplyStatusEffect_Weakness_RefreshesInsteadOfStacking()
    {
        var unit = CreateUnit();

        unit.ApplyStatusEffect(StatusEffectType.Weakness, 0.2f, 2);
        unit.ApplyStatusEffect(StatusEffectType.Weakness, 0.2f, 5);

        Assert.AreEqual(1, unit.GetActiveStatusCount(StatusEffectType.Weakness),
            "Applying Weakness twice should refresh duration, not create a second stack.");
    }

    // =========================================================================
    // WEAKNESS — GetAttack/GetDefense REDUCTION
    // =========================================================================

    [Test]
    public void GetAttack_NoWeakness_ReturnsBaseAttack()
    {
        var unit = CreateUnit(atk: 20f);

        Assert.AreEqual(20f, unit.GetAttack(), 0.001f, "With no Weakness, GetAttack should return the base attack value.");
    }

    [Test]
    public void GetAttack_AndGetDefense_ReducedByWeaknessFraction()
    {
        var unit = CreateUnit(atk: 20f, def: 10f);

        unit.ApplyStatusEffect(StatusEffectType.Weakness, 0.3f, 3);

        Assert.AreEqual(14f, unit.GetAttack(), 0.001f, "Weakness 0.3 should reduce attack by 30% (20 -> 14).");
        Assert.AreEqual(7f, unit.GetDefense(), 0.001f, "Weakness 0.3 should reduce defense by 30% (10 -> 7).");
    }

    [Test]
    public void GetWeaknessReduction_CappedAtSeventyFivePercent()
    {
        var unit = CreateUnit(atk: 100f);

        // A single Weakness value above the cap should be clamped to 0.75.
        unit.ApplyStatusEffect(StatusEffectType.Weakness, 0.9f, 3);

        Assert.AreEqual(0.75f, unit.GetWeaknessReduction(), 0.001f, "Weakness reduction should be capped at 0.75.");
        Assert.AreEqual(25f, unit.GetAttack(), 0.001f, "Attack should be reduced by at most 75% (100 -> 25).");
    }

    // =========================================================================
    // ExecuteSkill — Poison / Weakness application
    // =========================================================================

    [Test]
    public void ExecuteSkill_PoisonEffect_AppliesPoisonToTarget()
    {
        var tm = CreateTurnManager();
        var attacker = CreateUnit();
        var target = CreateUnit();

        var skill = CreateSkill(AnimalSkillEffect.Poison, 5f, 3);

        InvokeExecuteSkill(tm, attacker, skill, target);

        Assert.AreEqual(1, target.GetActiveStatusCount(StatusEffectType.Poison), "ExecuteSkill with Poison effect should apply Poison to the target.");
    }

    [Test]
    public void ExecuteSkill_WeaknessEffect_AppliesWeaknessToTarget()
    {
        var tm = CreateTurnManager();
        var attacker = CreateUnit();
        var target = CreateUnit(atk: 20f);

        var skill = CreateSkill(AnimalSkillEffect.Weakness, 0.3f, 2);

        InvokeExecuteSkill(tm, attacker, skill, target);

        Assert.AreEqual(1, target.GetActiveStatusCount(StatusEffectType.Weakness), "ExecuteSkill with Weakness effect should apply Weakness to the target.");
        Assert.AreEqual(14f, target.GetAttack(), 0.001f, "Target's attack should reflect the new Weakness reduction.");
    }

    [Test]
    public void ExecuteSkill_PoisonEffect_ImmuneTargetUnaffected()
    {
        var tm = CreateTurnManager();
        var attacker = CreateUnit();
        var target = CreateUnit();
        target.InitializeImmunities(new System.Collections.Generic.List<string> { "Poison" });

        var skill = CreateSkill(AnimalSkillEffect.Poison, 5f, 3);

        InvokeExecuteSkill(tm, attacker, skill, target);

        Assert.AreEqual(0, target.GetActiveStatusCount(StatusEffectType.Poison), "Immune target should not receive Poison from ExecuteSkill.");
    }

    [Test]
    public void ExecuteSkill_WeaknessEffect_ImmuneTargetUnaffected()
    {
        var tm = CreateTurnManager();
        var attacker = CreateUnit();
        var target = CreateUnit();
        target.InitializeImmunities(new System.Collections.Generic.List<string> { "Weakness" });

        var skill = CreateSkill(AnimalSkillEffect.Weakness, 0.3f, 2);

        InvokeExecuteSkill(tm, attacker, skill, target);

        Assert.AreEqual(0, target.GetActiveStatusCount(StatusEffectType.Weakness), "Immune target should not receive Weakness from ExecuteSkill.");
    }
}

} // namespace SowurShield.Tests
