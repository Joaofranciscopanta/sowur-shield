using NUnit.Framework;
using UnityEngine;
using System.Reflection;
using SowurShield.Animals;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for Combat Phase 2 temporary stat buffs (ApplyStatBuff) —
/// activates the previously dormant AnimalSkill attack/defense/speed multipliers.
/// </summary>
public class CombatPhase2BuffTests
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

    private CombatUnit CreateUnit(float hp = 100f, float atk = 10f, float def = 5f, float spd = 10f, bool isPlayer = false)
    {
        var go = new GameObject("TestUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = isPlayer;
        unit.InitializeAsEnemy(go.name, hp, atk, def, spd);
        return unit;
    }

    private AnimalSkill CreateBuffSkill(float atkMult, float defMult, float spdMult, int duration,
        bool affectsSelf = true, bool affectsAllies = false)
    {
        var skill = ScriptableObject.CreateInstance<AnimalSkill>();
        Track(skill);
        skill.skillType = SkillType.Active;
        skill.damageMultiplier = 0f;
        skill.affectsSelf = affectsSelf;
        skill.affectsAllies = affectsAllies;
        skill.statusEffect = AnimalSkillEffect.None;
        skill.statusEffectDuration = duration;
        skill.attackMultiplier = atkMult;
        skill.defenseMultiplier = defMult;
        skill.speedMultiplier = spdMult;
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
    // ApplyStatBuff — direct API
    // =========================================================================

    [Test]
    public void ApplyStatBuff_ScalesAttackDefenseSpeed()
    {
        var unit = CreateUnit(atk: 10f, def: 10f, spd: 10f);

        unit.ApplyStatBuff(1.2f, 1.5f, 0.5f, 3);

        Assert.AreEqual(12f, unit.GetAttack(), 0.001f, "Attack should be scaled by 1.2.");
        Assert.AreEqual(15f, unit.GetDefense(), 0.001f, "Defense should be scaled by 1.5.");
        Assert.AreEqual(5f, unit.GetSpeed(), 0.001f, "Speed should be scaled by 0.5.");
    }

    [Test]
    public void ApplyStatBuff_MultipleBuffsStackMultiplicatively()
    {
        var unit = CreateUnit(atk: 10f);

        unit.ApplyStatBuff(1.1f, 1f, 1f, 5);
        unit.ApplyStatBuff(1.1f, 1f, 1f, 5);

        Assert.AreEqual(10f * 1.1f * 1.1f, unit.GetAttack(), 0.001f, "Two +10% buffs should multiply (1.1 * 1.1).");
        Assert.AreEqual(2, unit.GetActiveBuffCount());
    }

    [Test]
    public void TickStatusEffects_ExpiresBuffsAfterDuration()
    {
        var unit = CreateUnit(atk: 10f);

        unit.ApplyStatBuff(2f, 1f, 1f, 2);

        Assert.AreEqual(20f, unit.GetAttack(), 0.001f, "Buff should be active immediately after applying.");

        unit.TickStatusEffects(); // 1 turn remaining
        Assert.AreEqual(20f, unit.GetAttack(), 0.001f, "Buff should still be active after 1 tick.");

        unit.TickStatusEffects(); // expires
        Assert.AreEqual(10f, unit.GetAttack(), 0.001f, "Buff should expire after its duration elapses.");
        Assert.AreEqual(0, unit.GetActiveBuffCount());
    }

    [Test]
    public void GetSpeed_AffectsTurnGaugeFillRate()
    {
        var unit = CreateUnit(spd: 10f);

        unit.ApplyStatBuff(1f, 1f, 2f, 5); // double speed

        unit.UpdateTurnGauge(1f); // 1 second

        Assert.AreEqual(20f, unit.turnGauge, 0.001f, "Turn gauge should fill at the buffed speed (20/sec), not the base speed.");
    }

    // =========================================================================
    // ExecuteSkill — activates AnimalSkill multiplier fields
    // =========================================================================

    [Test]
    public void ExecuteSkill_SelfBuffSkill_AppliesStatBuffToCaster()
    {
        var tm = CreateTurnManager();
        var attacker = CreateUnit(atk: 10f, def: 10f, spd: 10f);
        var target = CreateUnit();

        var skill = CreateBuffSkill(atkMult: 1.5f, defMult: 1.2f, spdMult: 1f, duration: 3, affectsSelf: true);

        InvokeExecuteSkill(tm, attacker, skill, target);

        Assert.AreEqual(15f, attacker.GetAttack(), 0.001f, "Self-buff skill should raise the caster's attack by 1.5x.");
        Assert.AreEqual(12f, attacker.GetDefense(), 0.001f, "Self-buff skill should raise the caster's defense by 1.2x.");
    }

    [Test]
    public void ExecuteSkill_AllyBuffSkill_AppliesStatBuffToAllAllies()
    {
        var tm = CreateTurnManager();
        var attacker = CreateUnit(atk: 10f, isPlayer: true);
        var ally = CreateUnit(atk: 20f, isPlayer: true);
        var target = CreateUnit();

        SetField(tm, "playerUnits", new System.Collections.Generic.List<CombatUnit> { attacker, ally });

        var skill = CreateBuffSkill(atkMult: 1.5f, defMult: 1f, spdMult: 1f, duration: 3, affectsSelf: false, affectsAllies: true);

        InvokeExecuteSkill(tm, attacker, skill, target);

        Assert.AreEqual(15f, attacker.GetAttack(), 0.001f, "Attacker should receive the ally-wide buff.");
        Assert.AreEqual(30f, ally.GetAttack(), 0.001f, "Ally should receive the ally-wide buff.");
    }

    [Test]
    public void ExecuteSkill_NoMultiplierFields_DoesNotApplyBuff()
    {
        var tm = CreateTurnManager();
        var attacker = CreateUnit(atk: 10f);
        var target = CreateUnit();

        var skill = CreateBuffSkill(atkMult: 1f, defMult: 1f, spdMult: 1f, duration: 3);

        InvokeExecuteSkill(tm, attacker, skill, target);

        Assert.AreEqual(0, attacker.GetActiveBuffCount(), "A skill with all multipliers == 1 should not apply a buff.");
    }

    // ── reflection helper ────────────────────────────────────────────────────
    private static void SetField(object target, string name, object value)
    {
        var f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {target.GetType().Name}");
        f.SetValue(target, value);
    }
}

} // namespace SowurShield.Tests
