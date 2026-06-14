using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for randomized battle modifiers (TurnManager.activeModifier):
/// DoubleSpeed, LowVisibility, HealingRain, and GlassCannon.
/// </summary>
public class CombatPhase3BattleModifierTests
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

    private static object GetField(object target, string name)
    {
        var f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {target.GetType().Name}");
        return f.GetValue(target);
    }

    private static object InvokePrivate(object target, string methodName, object[] args)
    {
        var m = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{methodName}' not found on {target.GetType().Name}");
        return m.Invoke(target, args);
    }

    private static object InvokePrivateStatic(System.Type type, string methodName, object[] args)
    {
        var m = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(m, $"Static method '{methodName}' not found on {type.Name}");
        return m.Invoke(null, args);
    }

    // ── factory helpers ─────────────────────────────────────────────────────

    private CombatUnit CreateUnit(bool isPlayer, float hp, float atk = 10f, float def = 0f, float spd = 10f)
    {
        var go = new GameObject(isPlayer ? "PlayerUnit" : "EnemyUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = isPlayer;
        unit.InitializeAsEnemy(go.name, hp, atk, def, spd);
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
    // SetBattleModifierForTesting / GetActiveModifier
    // =========================================================================

    [Test]
    public void SetBattleModifierForTesting_OverridesActiveModifier()
    {
        var tm = CreateTurnManager(new List<CombatUnit>(), new List<CombatUnit>());

        var mod = new BattleModifier(BattleModifierType.GlassCannon, "test");
        tm.SetBattleModifierForTesting(mod);

        Assert.AreEqual(BattleModifierType.GlassCannon, tm.GetActiveModifier().type);
    }

    [Test]
    public void SetBattleModifierForTesting_Null_FallsBackToNone()
    {
        var tm = CreateTurnManager(new List<CombatUnit>(), new List<CombatUnit>());

        tm.SetBattleModifierForTesting(new BattleModifier(BattleModifierType.GlassCannon, "test"));
        tm.SetBattleModifierForTesting(null);

        Assert.AreEqual(BattleModifierType.None, tm.GetActiveModifier().type);
    }

    [Test]
    public void DefaultModifier_IsNone()
    {
        var tm = CreateTurnManager(new List<CombatUnit>(), new List<CombatUnit>());

        Assert.AreEqual(BattleModifierType.None, tm.GetActiveModifier().type);
    }

    // =========================================================================
    // RollBattleModifier — distribution sanity
    // =========================================================================

    [Test]
    public void RollBattleModifier_DistributionIsRoughlySixtyPercentNone()
    {
        const int trials = 1000;
        int noneCount = 0;

        for (int i = 0; i < trials; i++)
        {
            var mod = (BattleModifier)InvokePrivateStatic(typeof(TurnManager), "RollBattleModifier", null);
            Assert.IsNotNull(mod);

            if (mod.type == BattleModifierType.None)
                noneCount++;
        }

        float noneFraction = (float)noneCount / trials;
        Assert.That(noneFraction, Is.InRange(0.5f, 0.7f),
            $"Expected ~60% None rolls, got {noneFraction:P1} over {trials} trials.");
    }

    [Test]
    public void RollBattleModifier_AlwaysReturnsValidType()
    {
        for (int i = 0; i < 100; i++)
        {
            var mod = (BattleModifier)InvokePrivateStatic(typeof(TurnManager), "RollBattleModifier", null);
            Assert.IsNotNull(mod);
            Assert.IsTrue(System.Enum.IsDefined(typeof(BattleModifierType), mod.type));
        }
    }

    // =========================================================================
    // GlassCannon — doubles damage dealt and received via ExecuteAttack
    // =========================================================================

    [Test]
    public void ExecuteAttack_GlassCannon_DoublesDamageDealt()
    {
        var attacker = CreateUnit(isPlayer: true, hp: 100f, atk: 50f, def: 0f, spd: 10f);
        var target = CreateUnit(isPlayer: false, hp: 1000f, atk: 0f, def: 0f, spd: 10f);
        var tm = CreateTurnManager(new List<CombatUnit> { attacker }, new List<CombatUnit> { target });

        // Force 100% accuracy and no crit by disabling randomness influence:
        // accuracy defaults to 1.0, crit chance is 5% — average over many trials to avoid flakiness.
        tm.SetBattleModifierForTesting(new BattleModifier(BattleModifierType.GlassCannon, "test"));

        const int trials = 300;
        float totalDamageGlassCannon = 0f;

        for (int i = 0; i < trials; i++)
        {
            target.currentHealth = target.GetMaxHealth();
            InvokePrivate(tm, "ExecuteAttack", new object[] { attacker, target });
            totalDamageGlassCannon += target.GetMaxHealth() - target.currentHealth;
        }

        float avgGlassCannon = totalDamageGlassCannon / trials;

        // Now measure baseline (no modifier) damage with a fresh setup.
        var attacker2 = CreateUnit(isPlayer: true, hp: 100f, atk: 50f, def: 0f, spd: 10f);
        var target2 = CreateUnit(isPlayer: false, hp: 1000f, atk: 0f, def: 0f, spd: 10f);
        var tm2 = CreateTurnManager(new List<CombatUnit> { attacker2 }, new List<CombatUnit> { target2 });

        float totalDamageNormal = 0f;
        for (int i = 0; i < trials; i++)
        {
            target2.currentHealth = target2.GetMaxHealth();
            InvokePrivate(tm2, "ExecuteAttack", new object[] { attacker2, target2 });
            totalDamageNormal += target2.GetMaxHealth() - target2.currentHealth;
        }

        float avgNormal = totalDamageNormal / trials;

        Assert.That(avgGlassCannon, Is.GreaterThan(avgNormal * 1.8f),
            $"GlassCannon should roughly double average damage dealt. Normal={avgNormal}, GlassCannon={avgGlassCannon}");
    }

    // =========================================================================
    // LowVisibility — reduces effective accuracy
    // =========================================================================

    [Test]
    public void GetEffectiveAccuracy_LowVisibility_ReducesAccuracyByPenalty()
    {
        var attacker = CreateUnit(isPlayer: true, hp: 100f, atk: 10f, def: 0f, spd: 10f);
        var tm = CreateTurnManager(new List<CombatUnit> { attacker }, new List<CombatUnit>());

        float baseAccuracy = (float)InvokePrivate(tm, "GetEffectiveAccuracy", new object[] { attacker });

        tm.SetBattleModifierForTesting(new BattleModifier(BattleModifierType.LowVisibility, "test"));
        float reducedAccuracy = (float)InvokePrivate(tm, "GetEffectiveAccuracy", new object[] { attacker });

        Assert.AreEqual(baseAccuracy - BattleModifier.LowVisibilityAccuracyPenalty, reducedAccuracy, 0.001f);
    }

    [Test]
    public void GetEffectiveAccuracy_LowVisibility_ClampsToZero()
    {
        var attacker = CreateUnit(isPlayer: true, hp: 100f, atk: 10f, def: 0f, spd: 10f);
        SetField(attacker, "accuracy", 0.1f);
        var tm = CreateTurnManager(new List<CombatUnit> { attacker }, new List<CombatUnit>());

        tm.SetBattleModifierForTesting(new BattleModifier(BattleModifierType.LowVisibility, "test"));
        float accuracy = (float)InvokePrivate(tm, "GetEffectiveAccuracy", new object[] { attacker });

        Assert.AreEqual(0f, accuracy, 0.001f);
    }

    [Test]
    public void GetEffectiveAccuracy_NoModifier_ReturnsBaseAccuracy()
    {
        var attacker = CreateUnit(isPlayer: true, hp: 100f, atk: 10f, def: 0f, spd: 10f);
        var tm = CreateTurnManager(new List<CombatUnit> { attacker }, new List<CombatUnit>());

        float accuracy = (float)InvokePrivate(tm, "GetEffectiveAccuracy", new object[] { attacker });

        Assert.AreEqual(attacker.GetAccuracy(), accuracy, 0.001f);
    }

    // =========================================================================
    // DoubleSpeed — doubles turn gauge fill rate
    // =========================================================================

    [Test]
    public void FillTurnGauges_DoubleSpeed_FillsGaugeTwiceAsFast()
    {
        var unitA = CreateUnit(isPlayer: true, hp: 100f, atk: 10f, def: 0f, spd: 10f);
        var unitB = CreateUnit(isPlayer: false, hp: 100f, atk: 10f, def: 0f, spd: 10f);
        var tm = CreateTurnManager(new List<CombatUnit> { unitA }, new List<CombatUnit> { unitB });

        InvokePrivate(tm, "FillTurnGauges", new object[] { 1f });
        float normalGauge = (float)GetField(unitA, "turnGauge");

        // Reset gauge and re-run with DoubleSpeed active.
        SetField(unitA, "turnGauge", 0f);
        SetField(unitB, "turnGauge", 0f);
        tm.SetBattleModifierForTesting(new BattleModifier(BattleModifierType.DoubleSpeed, "test"));

        InvokePrivate(tm, "FillTurnGauges", new object[] { 1f });
        float doubledGauge = (float)GetField(unitA, "turnGauge");

        Assert.AreEqual(normalGauge * 2f, doubledGauge, 0.001f);
    }

    // =========================================================================
    // HealingRain — heals all alive units at the start of each round
    // =========================================================================

    [Test]
    public void ExecuteUnitTurn_HealingRain_HealsAllAliveUnitsAtRoundStart()
    {
        var unitA = CreateUnit(isPlayer: true, hp: 100f, atk: 10f, def: 0f, spd: 10f);
        var unitB = CreateUnit(isPlayer: false, hp: 100f, atk: 10f, def: 0f, spd: 10f);
        unitA.currentHealth = 50f;
        unitB.currentHealth = 50f;

        // Acting unit's attack must always miss so the resulting damage doesn't
        // interfere with the HealingRain assertion below.
        SetField(unitA, "accuracy", 0f);

        var tm = CreateTurnManager(new List<CombatUnit> { unitA }, new List<CombatUnit> { unitB });
        tm.SetBattleModifierForTesting(new BattleModifier(BattleModifierType.HealingRain, "test"));

        // allUnits.Count == 2, so currentTurn % 2 == 1 on the first action (currentTurn becomes 1).
        SetField(tm, "currentTurn", 0);

        InvokePrivate(tm, "ExecuteUnitTurn", new object[] { unitA });

        Assert.AreEqual(55f, unitA.currentHealth, 0.001f, "Acting unit should be healed by HealingRain at round start.");
        Assert.AreEqual(55f, unitB.currentHealth, 0.001f, "Other alive units should also be healed by HealingRain at round start.");
    }

    [Test]
    public void ExecuteUnitTurn_HealingRain_DoesNotHealOutsideRoundStart()
    {
        var unitA = CreateUnit(isPlayer: true, hp: 100f, atk: 10f, def: 0f, spd: 10f);
        var unitB = CreateUnit(isPlayer: false, hp: 100f, atk: 10f, def: 0f, spd: 10f);
        unitA.currentHealth = 50f;
        unitB.currentHealth = 50f;

        // Acting unit's attack must always miss so the resulting damage doesn't
        // interfere with the HealingRain assertion below.
        SetField(unitA, "accuracy", 0f);

        var tm = CreateTurnManager(new List<CombatUnit> { unitA }, new List<CombatUnit> { unitB });
        tm.SetBattleModifierForTesting(new BattleModifier(BattleModifierType.HealingRain, "test"));

        // allUnits.Count == 2, so currentTurn % 2 == 0 (no heal) when currentTurn becomes 2.
        SetField(tm, "currentTurn", 1);

        InvokePrivate(tm, "ExecuteUnitTurn", new object[] { unitA });

        Assert.AreEqual(50f, unitA.currentHealth, 0.001f, "No heal should occur outside the round-start boundary.");
        Assert.AreEqual(50f, unitB.currentHealth, 0.001f, "No heal should occur outside the round-start boundary.");
    }

    [Test]
    public void ExecuteUnitTurn_NoModifier_DoesNotHeal()
    {
        var unitA = CreateUnit(isPlayer: true, hp: 100f, atk: 10f, def: 0f, spd: 10f);
        var unitB = CreateUnit(isPlayer: false, hp: 100f, atk: 10f, def: 0f, spd: 10f);
        unitA.currentHealth = 50f;
        unitB.currentHealth = 50f;

        // Acting unit's attack must always miss so the resulting damage doesn't
        // interfere with the HealingRain assertion below.
        SetField(unitA, "accuracy", 0f);

        var tm = CreateTurnManager(new List<CombatUnit> { unitA }, new List<CombatUnit> { unitB });
        SetField(tm, "currentTurn", 0);

        InvokePrivate(tm, "ExecuteUnitTurn", new object[] { unitA });

        Assert.AreEqual(50f, unitA.currentHealth, 0.001f, "No heal should occur without HealingRain.");
        Assert.AreEqual(50f, unitB.currentHealth, 0.001f, "No heal should occur without HealingRain.");
    }
}

} // namespace SowurShield.Tests
