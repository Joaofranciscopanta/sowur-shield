using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for the OnTelegraph event hook (TurnManager.ExecuteUnitTurn),
/// fired immediately before an attack or skill resolves.
/// </summary>
public class CombatPhase5TelegraphTests
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

    private static void InvokePrivate(object target, string methodName, object[] args)
    {
        var m = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{methodName}' not found on {target.GetType().Name}");
        m.Invoke(target, args);
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
    // Basic attack telegraph
    // =========================================================================

    [Test]
    public void ExecuteUnitTurn_BasicAttack_FiresOnTelegraphBeforeResolution()
    {
        var attacker = CreateUnit(isPlayer: true, hp: 100f, atk: 10f, def: 0f, spd: 10f);
        var target = CreateUnit(isPlayer: false, hp: 100f, atk: 0f, def: 0f, spd: 10f);
        var tm = CreateTurnManager(new List<CombatUnit> { attacker }, new List<CombatUnit> { target });

        int eventCount = 0;
        TurnManager.TelegraphInfo received = default;
        float targetHealthAtEventTime = -1f;

        tm.OnTelegraph += info =>
        {
            eventCount++;
            received = info;
            targetHealthAtEventTime = target.currentHealth;
        };

        InvokePrivate(tm, "ExecuteUnitTurn", new object[] { attacker });

        Assert.AreEqual(1, eventCount, "OnTelegraph should fire exactly once for a basic attack.");
        Assert.AreEqual(attacker, received.actor);
        Assert.AreEqual(target, received.target);
        Assert.IsNull(received.skill, "Basic attacks should report a null skill.");
        Assert.AreEqual(100f, targetHealthAtEventTime, 0.001f, "OnTelegraph should fire before damage is applied.");
    }

    [Test]
    public void ExecuteUnitTurn_NoListeners_DoesNotThrow()
    {
        var attacker = CreateUnit(isPlayer: true, hp: 100f, atk: 10f, def: 0f, spd: 10f);
        var target = CreateUnit(isPlayer: false, hp: 100f, atk: 0f, def: 0f, spd: 10f);
        var tm = CreateTurnManager(new List<CombatUnit> { attacker }, new List<CombatUnit> { target });

        Assert.DoesNotThrow(() => InvokePrivate(tm, "ExecuteUnitTurn", new object[] { attacker }));
    }

    [Test]
    public void ExecuteUnitTurn_DeadUnit_DoesNotFireOnTelegraph()
    {
        var attacker = CreateUnit(isPlayer: true, hp: 0f, atk: 10f, def: 0f, spd: 10f);
        var target = CreateUnit(isPlayer: false, hp: 100f, atk: 0f, def: 0f, spd: 10f);
        var tm = CreateTurnManager(new List<CombatUnit> { attacker }, new List<CombatUnit> { target });

        int eventCount = 0;
        tm.OnTelegraph += info => eventCount++;

        InvokePrivate(tm, "ExecuteUnitTurn", new object[] { attacker });

        Assert.AreEqual(0, eventCount, "A dead unit's turn should not fire OnTelegraph.");
    }

    [Test]
    public void ExecuteUnitTurn_StunnedUnit_DoesNotFireOnTelegraph()
    {
        var attacker = CreateUnit(isPlayer: true, hp: 100f, atk: 10f, def: 0f, spd: 10f);
        var target = CreateUnit(isPlayer: false, hp: 100f, atk: 0f, def: 0f, spd: 10f);
        attacker.ApplyStatusEffect(StatusEffectType.Stun, 0f, 1);

        var tm = CreateTurnManager(new List<CombatUnit> { attacker }, new List<CombatUnit> { target });

        int eventCount = 0;
        tm.OnTelegraph += info => eventCount++;

        InvokePrivate(tm, "ExecuteUnitTurn", new object[] { attacker });

        Assert.AreEqual(0, eventCount, "A stunned unit's turn should not fire OnTelegraph.");
    }
}

} // namespace SowurShield.Tests
