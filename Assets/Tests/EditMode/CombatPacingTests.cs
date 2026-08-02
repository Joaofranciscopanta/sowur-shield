using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for the combat pacing pass (phase 1 of the active-pause design):
/// the 1x/2x battle speed multiplier, its PlayerPrefs persistence, and its effect on
/// the gauge fill rate.
/// </summary>
public class CombatPacingTests
{
    private const string SpeedPrefKey = "combat_speed";

    private List<Object> _cleanupList = new List<Object>();
    private bool _hadStoredSpeed;
    private float _storedSpeed;

    private T Track<T>(T obj) where T : Object
    {
        _cleanupList.Add(obj);
        return obj;
    }

    [SetUp]
    public void SetUp()
    {
        // SetSpeedMultiplier writes to PlayerPrefs, which is shared with the Editor and
        // every other test. Snapshot it so a run can't leave the user's combat stuck at 2x.
        _hadStoredSpeed = PlayerPrefs.HasKey(SpeedPrefKey);
        _storedSpeed = PlayerPrefs.GetFloat(SpeedPrefKey, 1f);
        PlayerPrefs.DeleteKey(SpeedPrefKey);
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

        if (_hadStoredSpeed) PlayerPrefs.SetFloat(SpeedPrefKey, _storedSpeed);
        else PlayerPrefs.DeleteKey(SpeedPrefKey);
    }

    private TurnManager CreateTurnManager()
    {
        var go = new GameObject("TurnManager");
        Track(go);
        return go.AddComponent<TurnManager>();
    }

    /// <summary>
    /// EditMode does not run Awake on AddComponent, so tests that depend on Awake's work
    /// (singleton registration, restoring the stored speed) must invoke it explicitly.
    /// </summary>
    private static void InvokeAwake(TurnManager tm)
    {
        var m = typeof(TurnManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "Method 'Awake' not found on TurnManager");
        m.Invoke(tm, null);
    }

    private static void InvokeOnDestroy(TurnManager tm)
    {
        var m = typeof(TurnManager).GetMethod("OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "Method 'OnDestroy' not found on TurnManager");
        m.Invoke(tm, null);
    }

    private CombatUnit CreateUnit(bool isPlayer, float spd = 10f)
    {
        var go = new GameObject(isPlayer ? "PlayerUnit" : "EnemyUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = isPlayer;
        unit.InitializeAsEnemy(go.name, 1000f, 10f, 0f, spd);
        return unit;
    }

    private static T GetPrivateField<T>(TurnManager tm, string name)
    {
        var f = typeof(TurnManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on TurnManager");
        return (T)f.GetValue(tm);
    }

    private static void SetPrivateField(TurnManager tm, string name, object value)
    {
        var f = typeof(TurnManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on TurnManager");
        f.SetValue(tm, value);
    }

    private static void FillTurnGauges(TurnManager tm, float deltaTime)
    {
        var m = typeof(TurnManager).GetMethod("FillTurnGauges", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "Method 'FillTurnGauges' not found on TurnManager");
        m.Invoke(tm, new object[] { deltaTime });
    }

    // ── Pacing defaults ────────────────────────────────────────────────────────

    [Test]
    public void GaugeFillRate_DefaultsToSlowerPacing()
    {
        var tm = CreateTurnManager();
        Assert.AreEqual(6f, GetPrivateField<float>(tm, "gaugeFilLRate"), 0.001f,
            "Gauge fill rate should default to 6 (was 10 before the pacing pass).");
    }

    [Test]
    public void ActionMicroDelay_DefaultsToReadableGap()
    {
        var tm = CreateTurnManager();
        Assert.AreEqual(0.25f, GetPrivateField<float>(tm, "actionMicroDelay"), 0.001f,
            "Micro-delay should default to 0.25s (was 0.05s before the pacing pass).");
    }

    // ── Speed multiplier ───────────────────────────────────────────────────────

    [Test]
    public void SpeedMultiplier_DefaultsToOne()
    {
        var tm = CreateTurnManager();
        Assert.AreEqual(1f, tm.SpeedMultiplier, 0.001f);
    }

    [Test]
    public void ToggleSpeedMultiplier_AlternatesBetweenOneAndTwo()
    {
        var tm = CreateTurnManager();

        Assert.AreEqual(2f, tm.ToggleSpeedMultiplier(), 0.001f, "First toggle should go to 2x.");
        Assert.AreEqual(1f, tm.ToggleSpeedMultiplier(), 0.001f, "Second toggle should return to 1x.");
    }

    [Test]
    public void SetSpeedMultiplier_ClampsToOneTwoRange()
    {
        var tm = CreateTurnManager();

        tm.SetSpeedMultiplier(99f);
        Assert.AreEqual(2f, tm.SpeedMultiplier, 0.001f, "Above-range speed should clamp to 2x.");

        tm.SetSpeedMultiplier(-5f);
        Assert.AreEqual(1f, tm.SpeedMultiplier, 0.001f, "Below-range speed should clamp to 1x.");
    }

    [Test]
    public void SetSpeedMultiplier_FiresChangeEventOnlyOnActualChange()
    {
        var tm = CreateTurnManager();
        int calls = 0;
        float lastValue = 0f;
        tm.OnSpeedMultiplierChanged += v => { calls++; lastValue = v; };

        tm.SetSpeedMultiplier(2f);
        Assert.AreEqual(1, calls, "Changing the speed should fire the event once.");
        Assert.AreEqual(2f, lastValue, 0.001f);

        tm.SetSpeedMultiplier(2f);
        Assert.AreEqual(1, calls, "Re-setting the same speed should not fire the event again.");
    }

    [Test]
    public void SpeedMultiplier_PersistsAcrossManagers()
    {
        var first = CreateTurnManager();
        first.SetSpeedMultiplier(2f);

        // A new battle constructs a fresh TurnManager; Awake should restore the stored choice.
        Object.DestroyImmediate(first.gameObject);
        var second = CreateTurnManager();
        InvokeAwake(second);

        Assert.AreEqual(2f, second.SpeedMultiplier, 0.001f,
            "Battle speed should persist to the next battle via PlayerPrefs.");
    }

    [Test]
    public void TurnManager_ClearsSingletonInstanceOnDestroy()
    {
        var first = CreateTurnManager();
        InvokeAwake(first);
        Assert.AreSame(first, TurnManager.Instance);

        InvokeOnDestroy(first);
        Assert.IsNull(TurnManager.Instance,
            "Instance must be cleared on destroy, or the next battle's TurnManager " +
            "sees a stale Instance in Awake and destroys itself instead of initializing.");
    }

    // ── Effect on the actual gauge fill ────────────────────────────────────────

    [Test]
    public void FillTurnGauges_DoubleSpeedFillsTwiceAsFast()
    {
        var tm = CreateTurnManager();
        var unit = CreateUnit(isPlayer: true, spd: 10f);
        SetPrivateField(tm, "allUnits", new List<CombatUnit> { unit });

        FillTurnGauges(tm, 0.1f);
        float atNormalSpeed = unit.turnGauge;

        unit.turnGauge = 0f;
        tm.SetSpeedMultiplier(2f);
        FillTurnGauges(tm, 0.1f);

        Assert.AreEqual(atNormalSpeed * 2f, unit.turnGauge, 0.001f,
            "2x speed should fill the turn gauge twice as fast.");
    }

    [Test]
    public void FillTurnGauges_SpeedMultiplierStacksWithDoubleSpeedModifier()
    {
        var tm = CreateTurnManager();
        var unit = CreateUnit(isPlayer: true, spd: 10f);
        SetPrivateField(tm, "allUnits", new List<CombatUnit> { unit });

        FillTurnGauges(tm, 0.1f);
        float baseline = unit.turnGauge;

        unit.turnGauge = 0f;
        tm.SetSpeedMultiplier(2f);
        tm.SetBattleModifierForTesting(new BattleModifier(BattleModifierType.DoubleSpeed));
        FillTurnGauges(tm, 0.1f);

        Assert.AreEqual(baseline * 4f, unit.turnGauge, 0.001f,
            "The 2x button and the DoubleSpeed battle modifier should multiply together (4x).");
    }
}

}
