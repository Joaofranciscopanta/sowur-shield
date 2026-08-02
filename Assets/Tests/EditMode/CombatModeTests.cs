using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for the combat mode plumbing (phase 2 of the active-pause design):
/// the CombatMode default, how the mode travels on TeamAssemblerData, and the
/// validation rules in SubmitPlayerAction.
/// </summary>
public class CombatModeTests
{
    private const string ModePrefKey = "combat_mode";

    private List<Object> _cleanupList = new List<Object>();
    private bool _hadStoredMode;
    private int _storedMode;

    private T Track<T>(T obj) where T : Object
    {
        _cleanupList.Add(obj);
        return obj;
    }

    [SetUp]
    public void SetUp()
    {
        _hadStoredMode = PlayerPrefs.HasKey(ModePrefKey);
        _storedMode = PlayerPrefs.GetInt(ModePrefKey, 0);
        PlayerPrefs.DeleteKey(ModePrefKey);
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

        if (_hadStoredMode) PlayerPrefs.SetInt(ModePrefKey, _storedMode);
        else PlayerPrefs.DeleteKey(ModePrefKey);
    }

    private TurnManager CreateTurnManager()
    {
        var go = new GameObject("TurnManager");
        Track(go);
        return go.AddComponent<TurnManager>();
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

    // ── Mode default ───────────────────────────────────────────────────────────

    [Test]
    public void TurnManager_DefaultsToAutoMode()
    {
        var tm = CreateTurnManager();
        Assert.AreEqual(CombatMode.Auto, tm.Mode,
            "A TurnManager with no assembler data must default to Auto, or every test " +
            "and CombatTestSpawner battle would hang waiting for input that never comes.");
    }

    [Test]
    public void TurnManager_NotWaitingForInputByDefault()
    {
        var tm = CreateTurnManager();
        Assert.IsFalse(tm.IsWaitingForPlayerInput);
        Assert.IsNull(tm.AwaitingInputFor);
    }

    [Test]
    public void SetCombatMode_ChangesMode()
    {
        var tm = CreateTurnManager();
        tm.SetCombatMode(CombatMode.ActivePause);
        Assert.AreEqual(CombatMode.ActivePause, tm.Mode);
    }

    // ── TeamAssemblerData carries the mode ─────────────────────────────────────

    [Test]
    public void TeamAssemblerData_DefaultsToActivePause()
    {
        var go = Track(new GameObject("TeamAssemblerData"));
        var data = go.AddComponent<TeamAssemblerData>();

        Assert.AreEqual(CombatMode.ActivePause, data.combatMode,
            "Active pause is the designed default for players; only the TurnManager " +
            "falls back to Auto when no assembler data exists.");
    }

    [Test]
    public void TeamAssemblerData_ModeSurvivesPrefsRoundTrip()
    {
        var go = Track(new GameObject("TeamAssemblerData"));
        var data = go.AddComponent<TeamAssemblerData>();

        data.combatMode = CombatMode.Auto;
        data.SaveToPrefs();

        data.combatMode = CombatMode.ActivePause; // clobber before restoring
        data.LoadFromPrefs();

        Assert.AreEqual(CombatMode.Auto, data.combatMode,
            "The chosen mode should persist to the next battle via PlayerPrefs.");
    }

    [Test]
    public void TeamAssemblerData_ModeLoadsEvenWithNoSavedTeam()
    {
        var go = Track(new GameObject("TeamAssemblerData"));
        var data = go.AddComponent<TeamAssemblerData>();

        // Store only the mode, no team — LoadFromPrefs early-returns on a missing team,
        // and the mode must be read before that return.
        PlayerPrefs.SetInt(ModePrefKey, (int)CombatMode.Auto);
        PlayerPrefs.DeleteKey("Combat_TeamCount");

        data.combatMode = CombatMode.ActivePause;
        data.LoadFromPrefs();

        Assert.AreEqual(CombatMode.Auto, data.combatMode,
            "Mode preference must survive even when there is no saved team to restore.");
    }

    // ── SubmitPlayerAction validation ──────────────────────────────────────────

    [Test]
    public void SubmitPlayerAction_RejectedWhenNothingIsWaiting()
    {
        var tm = CreateTurnManager();
        var enemy = CreateUnit(isPlayer: false);

        Assert.IsFalse(tm.SubmitAttack(enemy),
            "Submitting a command with no unit awaiting input must be rejected, not queued.");
    }

    [Test]
    public void SubmitPlayerAction_RejectedWhenActionIsNull()
    {
        var tm = CreateTurnManager();
        var player = CreateUnit(isPlayer: true);
        tm.SetAwaitingInputForTesting(player);

        Assert.IsFalse(tm.SubmitPlayerAction(null));
    }

    [Test]
    public void SubmitAttack_AcceptedWhileWaiting()
    {
        var tm = CreateTurnManager();
        var player = CreateUnit(isPlayer: true);
        var enemy = CreateUnit(isPlayer: false);
        tm.SetAwaitingInputForTesting(player);

        Assert.IsTrue(tm.SubmitAttack(enemy));
    }

    [Test]
    public void SubmitPlayerAction_SecondSubmissionForSameTurnIsRejected()
    {
        var tm = CreateTurnManager();
        var player = CreateUnit(isPlayer: true);
        var enemy = CreateUnit(isPlayer: false);
        tm.SetAwaitingInputForTesting(player);

        Assert.IsTrue(tm.SubmitAttack(enemy), "First command should be accepted.");
        Assert.IsFalse(tm.SubmitAttack(enemy),
            "A double-click must not queue a second action for the same turn.");
    }

    [Test]
    public void SubmitSkill_RejectedWhenUnitHasNoReadySkill()
    {
        var tm = CreateTurnManager();
        var player = CreateUnit(isPlayer: true);   // no skill initialized
        var enemy = CreateUnit(isPlayer: false);
        tm.SetAwaitingInputForTesting(player);

        Assert.IsFalse(tm.SubmitSkill(enemy),
            "Submitting a skill the unit cannot use would burn the turn doing nothing.");
    }

    [Test]
    public void SubmitDefend_AcceptedWhileWaiting()
    {
        var tm = CreateTurnManager();
        var player = CreateUnit(isPlayer: true);
        tm.SetAwaitingInputForTesting(player);

        Assert.IsTrue(tm.SubmitDefend(), "Defend needs no target and should always be available.");
    }
}

}
