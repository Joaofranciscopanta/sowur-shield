using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using SowurShield.Animals;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for Combat Phase 4 class synergy bonuses
/// (TurnManager.ApplyClassSynergies, called from StartCombat).
/// </summary>
public class CombatPhase4SynergyTests
{
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

        if (TurnManager.Instance != null)
            Object.DestroyImmediate(TurnManager.Instance.gameObject);
    }

    // ── reflection helpers ──────────────────────────────────────────────────

    private static void SetField(object target, string name, object value)
    {
        var f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {target.GetType().Name}");
        f.SetValue(target, value);
    }

    // ── factory helpers ─────────────────────────────────────────────────────

    private CombatUnit CreatePlayerUnit(string combatClass, float atk = 10f, float def = 10f, float spd = 10f)
    {
        var go = new GameObject("PlayerUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = true;
        unit.InitializeAsEnemy(go.name, 100f, atk, def, spd);

        if (combatClass != null)
        {
            var animalGo = new GameObject("Animal");
            Track(animalGo);
            animalGo.AddComponent<SpriteRenderer>();
            var animal = animalGo.AddComponent<Animal>();

            var data = ScriptableObject.CreateInstance<AnimalData>();
            Track(data);
            data.combatClass = combatClass;

            SetField(animal, "animalData", data);
            unit.animal = animal;
        }

        return unit;
    }

    private TurnManager CreateTurnManager(List<CombatUnit> playerUnits, List<CombatUnit> enemyUnits)
    {
        var go = new GameObject("TurnManager");
        Track(go);
        var tm = go.AddComponent<TurnManager>();
        SetField(tm, "playerUnits", playerUnits);
        SetField(tm, "enemyUnits", enemyUnits);

        var allUnits = new List<CombatUnit>();
        allUnits.AddRange(playerUnits);
        allUnits.AddRange(enemyUnits);
        SetField(tm, "allUnits", allUnits);

        return tm;
    }

    // =========================================================================
    // 3+ same-class team synergy
    // =========================================================================

    [Test]
    public void StartCombat_ThreeSameClassUnits_EachGetsAttackAndDefenseBuff()
    {
        var unitA = CreatePlayerUnit("DPS", atk: 10f, def: 10f, spd: 10f);
        var unitB = CreatePlayerUnit("DPS", atk: 20f, def: 20f, spd: 10f);
        var unitC = CreatePlayerUnit("DPS", atk: 30f, def: 30f, spd: 10f);

        var tm = CreateTurnManager(new List<CombatUnit> { unitA, unitB, unitC }, new List<CombatUnit>());
        tm.StartCombat();

        Assert.AreEqual(11f, unitA.GetAttack(), 0.001f, "3+ same-class units should get +10% attack.");
        Assert.AreEqual(11f, unitA.GetDefense(), 0.001f, "3+ same-class units should get +10% defense.");
        Assert.AreEqual(22f, unitB.GetAttack(), 0.001f);
        Assert.AreEqual(33f, unitC.GetDefense(), 0.001f);
    }

    [Test]
    public void StartCombat_ThreeSameClassUnits_SpeedUnaffected()
    {
        var unitA = CreatePlayerUnit("DPS", atk: 10f, def: 10f, spd: 15f);
        var unitB = CreatePlayerUnit("DPS", atk: 10f, def: 10f, spd: 15f);
        var unitC = CreatePlayerUnit("DPS", atk: 10f, def: 10f, spd: 15f);

        var tm = CreateTurnManager(new List<CombatUnit> { unitA, unitB, unitC }, new List<CombatUnit>());
        tm.StartCombat();

        Assert.AreEqual(15f, unitA.GetSpeed(), 0.001f, "Class synergy should not affect speed.");
    }

    [Test]
    public void StartCombat_TwoSameClassUnits_NoBuffApplied()
    {
        var unitA = CreatePlayerUnit("DPS", atk: 10f, def: 10f, spd: 10f);
        var unitB = CreatePlayerUnit("DPS", atk: 10f, def: 10f, spd: 10f);

        var tm = CreateTurnManager(new List<CombatUnit> { unitA, unitB }, new List<CombatUnit>());
        tm.StartCombat();

        Assert.AreEqual(10f, unitA.GetAttack(), 0.001f, "Groups smaller than 3 should not get a synergy buff.");
        Assert.AreEqual(10f, unitB.GetDefense(), 0.001f);
    }

    [Test]
    public void StartCombat_MixedClasses_NoBuffApplied()
    {
        var unitA = CreatePlayerUnit("DPS", atk: 10f, def: 10f, spd: 10f);
        var unitB = CreatePlayerUnit("Tank", atk: 10f, def: 10f, spd: 10f);
        var unitC = CreatePlayerUnit("Support", atk: 10f, def: 10f, spd: 10f);

        var tm = CreateTurnManager(new List<CombatUnit> { unitA, unitB, unitC }, new List<CombatUnit>());
        tm.StartCombat();

        Assert.AreEqual(10f, unitA.GetAttack(), 0.001f);
        Assert.AreEqual(10f, unitB.GetDefense(), 0.001f);
        Assert.AreEqual(10f, unitC.GetAttack(), 0.001f);
    }

    [Test]
    public void StartCombat_ThreeSameClassPlusOneOther_OnlySameClassGroupBuffed()
    {
        var unitA = CreatePlayerUnit("DPS", atk: 10f, def: 10f, spd: 10f);
        var unitB = CreatePlayerUnit("DPS", atk: 10f, def: 10f, spd: 10f);
        var unitC = CreatePlayerUnit("DPS", atk: 10f, def: 10f, spd: 10f);
        var unitD = CreatePlayerUnit("Tank", atk: 10f, def: 10f, spd: 10f);

        var tm = CreateTurnManager(new List<CombatUnit> { unitA, unitB, unitC, unitD }, new List<CombatUnit>());
        tm.StartCombat();

        Assert.AreEqual(11f, unitA.GetAttack(), 0.001f);
        Assert.AreEqual(11f, unitB.GetAttack(), 0.001f);
        Assert.AreEqual(11f, unitC.GetAttack(), 0.001f);
        Assert.AreEqual(10f, unitD.GetAttack(), 0.001f, "Unit not part of the 3+ group should be unaffected.");
    }

    [Test]
    public void StartCombat_UnitsWithoutSourceAnimal_NoBuffApplied()
    {
        // Enemy-style units (no source Animal / AnimalData) should be skipped entirely.
        var unitA = CreatePlayerUnit(null, atk: 10f, def: 10f, spd: 10f);
        var unitB = CreatePlayerUnit(null, atk: 10f, def: 10f, spd: 10f);
        var unitC = CreatePlayerUnit(null, atk: 10f, def: 10f, spd: 10f);

        var tm = CreateTurnManager(new List<CombatUnit> { unitA, unitB, unitC }, new List<CombatUnit>());
        tm.StartCombat();

        Assert.AreEqual(10f, unitA.GetAttack(), 0.001f);
    }
}

} // namespace SowurShield.Tests
