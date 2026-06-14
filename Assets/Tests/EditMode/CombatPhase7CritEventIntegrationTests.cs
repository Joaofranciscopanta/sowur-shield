using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode integration tests verifying the crit flag is threaded correctly from
/// TurnManager.ExecuteAttack through to CombatUnit.OnDamageTaken and TurnManager.OnBigHit.
/// </summary>
public class CombatPhase7CritEventIntegrationTests
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

        // Clear all subscribers from the static event so tests don't leak into each other.
        var f = typeof(TurnManager).GetField("OnBigHit", BindingFlags.NonPublic | BindingFlags.Static);
        f.SetValue(null, null);
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
    // Crit flag threading: OnDamageTaken(amount, true) + OnBigHit(0.08f)
    // =========================================================================

    [Test]
    public void ExecuteAttack_ForcedCrit_FiresOnDamageTakenTrueAndOnBigHitWithCritDuration()
    {
        var attacker = CreateUnit(isPlayer: true, hp: 100f, atk: 50f, def: 0f, spd: 10f);
        var target = CreateUnit(isPlayer: false, hp: 10000f, atk: 0f, def: 0f, spd: 10f);
        var tm = CreateTurnManager(new List<CombatUnit> { attacker }, new List<CombatUnit> { target });

        int critDamageEvents = 0;
        int nonCritDamageEvents = 0;
        int bigHitCritEvents = 0;

        target.OnDamageTaken += (amount, isCrit) =>
        {
            if (isCrit) critDamageEvents++;
            else nonCritDamageEvents++;
        };

        var f = typeof(TurnManager).GetField("OnBigHit", BindingFlags.NonPublic | BindingFlags.Static);
        System.Action<float> bigHitHandler = duration =>
        {
            if (Mathf.Approximately(duration, 0.08f)) bigHitCritEvents++;
        };
        f.SetValue(null, bigHitHandler);

        // GetCritChance() == 0.05f — run enough trials that at least one crit is virtually guaranteed.
        const int trials = 500;
        for (int i = 0; i < trials; i++)
        {
            target.currentHealth = target.GetMaxHealth();
            InvokePrivate(tm, "ExecuteAttack", new object[] { attacker, target });
        }

        Assert.Greater(critDamageEvents, 0, "Expected at least one crit out of 500 trials at 5% crit chance.");
        Assert.AreEqual(critDamageEvents, bigHitCritEvents,
            "Every crit-flagged OnDamageTaken should correspond to an OnBigHit(0.08f) event.");
        Assert.Greater(nonCritDamageEvents, 0, "Expected at least one non-crit out of 500 trials.");
    }
}

} // namespace SowurShield.Tests
