using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SowurShield.Combat;

/// <summary>
/// Play Mode tests for active-pause combat (phase 2). These need Play Mode because the
/// whole feature lives in a coroutine: the battle freezes mid-batch, waits for a command,
/// then resumes. EditMode can only reach the validation rules, not the timing.
/// </summary>
public class CombatActivePausePlayModeTests
{
    private readonly List<Object> _cleanup = new List<Object>();

    private T Track<T>(T obj) where T : Object
    {
        _cleanup.Add(obj);
        return obj;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        // Reset unconditionally: the two long-waiting tests below switch this on, and leaving
        // it set would silently suppress genuine errors in every test that runs after them.
        LogAssert.ignoreFailingMessages = false;

        foreach (var obj in _cleanup)
            if (obj != null) Object.Destroy(obj);
        _cleanup.Clear();
        yield return null;
    }

    private TurnManager CreateTurnManager(CombatMode mode)
    {
        // TurnManager is a singleton whose Awake destroys itself if Instance is already set.
        // Object.Destroy in TearDown is deferred, so without this the next test's manager
        // suicides on creation and the test silently keeps using the PREVIOUS one — inheriting
        // its state. That is how a test asserting the input failsafe saw the flag from the
        // test before it and could never fail.
        foreach (var stale in Object.FindObjectsByType<TurnManager>(FindObjectsSortMode.None))
            Object.DestroyImmediate(stale.gameObject);

        var go = Track(new GameObject("TurnManager"));
        var tm = go.AddComponent<TurnManager>();
        tm.SetCombatMode(mode);
        return tm;
    }

    private CombatUnit CreateUnit(bool isPlayer, float hp = 1000f, float atk = 50f, float spd = 10f)
    {
        var go = Track(new GameObject(isPlayer ? "PlayerUnit" : "EnemyUnit"));
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = isPlayer;
        unit.InitializeAsEnemy(go.name, hp, atk, 0f, spd);
        return unit;
    }

    /// <summary>
    /// Wire units into the manager without a GridManager: InitializeCombat needs one,
    /// but these tests only exercise the action batch.
    /// </summary>
    private static void InjectUnits(TurnManager tm, List<CombatUnit> players, List<CombatUnit> enemies)
    {
        var all = new List<CombatUnit>();
        all.AddRange(players);
        all.AddRange(enemies);

        SetPrivateField(tm, "allUnits", all);
        SetPrivateField(tm, "playerUnits", players);
        SetPrivateField(tm, "enemyUnits", enemies);
        tm.combatActive = true;
    }

    private static void SetPrivateField(object target, string name, object value)
    {
        var f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {target.GetType().Name}");
        f.SetValue(target, value);
    }

    /// <summary>Run the unit's gauge up to full so the next Update picks it up as ready.</summary>
    private static void FillGauge(CombatUnit unit) => unit.turnGauge = 100f;

    // ── Active pause: the battle waits ─────────────────────────────────────────

    [UnityTest]
    public IEnumerator ActivePause_FreezesAndWaitsForPlayerCommand()
    {
        var tm = CreateTurnManager(CombatMode.ActivePause);
        var player = CreateUnit(isPlayer: true);
        var enemy = CreateUnit(isPlayer: false);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { enemy });

        FillGauge(player);
        yield return null; // Update picks up the ready unit and starts the batch
        yield return null; // coroutine reaches the wait

        Assert.IsTrue(tm.IsWaitingForPlayerInput,
            "A player unit filling its gauge in ActivePause must freeze the battle.");
        Assert.AreSame(player, tm.AwaitingInputFor);
        Assert.AreEqual(1000f, enemy.currentHealth, 0.01f,
            "No damage should be dealt while the battle waits for a command.");
    }

    [UnityTest]
    public IEnumerator ActivePause_EnemyGaugesDoNotFillWhileWaiting()
    {
        var tm = CreateTurnManager(CombatMode.ActivePause);
        var player = CreateUnit(isPlayer: true);
        var enemy = CreateUnit(isPlayer: false, spd: 30f);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { enemy });

        FillGauge(player);
        yield return null;
        yield return null;
        Assert.IsTrue(tm.IsWaitingForPlayerInput, "Precondition: battle should be waiting.");

        float enemyGaugeAtFreeze = enemy.turnGauge;
        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(enemyGaugeAtFreeze, enemy.turnGauge, 0.01f,
            "Enemy gauges must stay frozen while the player is choosing an action.");
    }

    [UnityTest]
    public IEnumerator ActivePause_SubmittedAttackResolvesAndResumesBattle()
    {
        var tm = CreateTurnManager(CombatMode.ActivePause);
        var player = CreateUnit(isPlayer: true, atk: 50f);
        var enemy = CreateUnit(isPlayer: false);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { enemy });

        FillGauge(player);
        yield return null;
        yield return null;
        Assert.IsTrue(tm.IsWaitingForPlayerInput, "Precondition: battle should be waiting.");

        Assert.IsTrue(tm.SubmitAttack(enemy), "The command should be accepted.");
        yield return null; // the wait loop observes the pending action

        Assert.IsFalse(tm.IsWaitingForPlayerInput, "Submitting a command must unfreeze the battle.");
        Assert.Less(enemy.currentHealth, 1000f, "The submitted attack should have dealt damage.");
    }

    [UnityTest]
    public IEnumerator ActivePause_AttackHitsThePlayerChosenTarget()
    {
        var tm = CreateTurnManager(CombatMode.ActivePause);
        var player = CreateUnit(isPlayer: true, atk: 50f);
        // The AI targets the front column first; the player should be able to override that.
        var frontEnemy = CreateUnit(isPlayer: false);
        frontEnemy.gridPosition = new Vector2Int(5, 1);
        var backEnemy = CreateUnit(isPlayer: false);
        backEnemy.gridPosition = new Vector2Int(0, 1);

        InjectUnits(tm, new List<CombatUnit> { player },
            new List<CombatUnit> { frontEnemy, backEnemy });

        FillGauge(player);
        yield return null;
        yield return null;

        Assert.IsTrue(tm.SubmitAttack(backEnemy));
        yield return null;

        Assert.Less(backEnemy.currentHealth, 1000f,
            "The player's chosen target should take the damage.");
        Assert.AreEqual(1000f, frontEnemy.currentHealth, 0.01f,
            "The AI's preferred front-column target must not be hit instead.");
    }

    [UnityTest]
    public IEnumerator ActivePause_DefendAppliesShieldAndRefundsGauge()
    {
        var tm = CreateTurnManager(CombatMode.ActivePause);
        var player = CreateUnit(isPlayer: true);
        var enemy = CreateUnit(isPlayer: false);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { enemy });

        FillGauge(player);
        yield return null;
        yield return null;

        Assert.IsTrue(tm.SubmitDefend());
        yield return null;

        Assert.Greater(player.GetShieldReduction(), 0f,
            "Defend should apply a Shield status effect.");
        Assert.Greater(player.turnGauge, 0f,
            "Defend should refund part of the turn gauge so the unit acts again sooner.");
    }

    [UnityTest]
    public IEnumerator ActivePause_UnitDyingWhileWaitingDoesNotHangTheBattle()
    {
        var tm = CreateTurnManager(CombatMode.ActivePause);
        var player = CreateUnit(isPlayer: true, hp: 100f);
        var enemy = CreateUnit(isPlayer: false);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { enemy });

        FillGauge(player);
        yield return null;
        yield return null;
        Assert.IsTrue(tm.IsWaitingForPlayerInput, "Precondition: battle should be waiting.");

        // Kill the unit mid-wait, as a burn tick or enemy action could.
        player.TakeDamage(9999f);
        yield return null;
        yield return null;

        Assert.IsFalse(tm.IsWaitingForPlayerInput,
            "A unit dying while its command panel is open must release the freeze.");
    }

    // ── Auto mode is unchanged ─────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator AutoMode_ResolvesPlayerTurnImmediatelyWithoutWaiting()
    {
        var tm = CreateTurnManager(CombatMode.Auto);
        var player = CreateUnit(isPlayer: true, atk: 50f);
        var enemy = CreateUnit(isPlayer: false);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { enemy });

        FillGauge(player);
        yield return null;
        yield return null;

        Assert.IsFalse(tm.IsWaitingForPlayerInput,
            "Auto mode must never wait for input — this is what keeps the existing tests green.");
        Assert.Less(enemy.currentHealth, 1000f,
            "Auto mode should have resolved the attack immediately.");
    }

    [UnityTest]
    public IEnumerator ActivePause_EnemyUnitsStillActAutomatically()
    {
        var tm = CreateTurnManager(CombatMode.ActivePause);
        var player = CreateUnit(isPlayer: true);
        var enemy = CreateUnit(isPlayer: false, atk: 50f);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { enemy });

        FillGauge(enemy);
        yield return null;
        yield return null;

        Assert.IsFalse(tm.IsWaitingForPlayerInput,
            "An enemy's turn must not open a command panel.");
        Assert.Less(player.currentHealth, 1000f,
            "Enemies should keep acting automatically in ActivePause.");
    }

    [UnityTest]
    public IEnumerator ActivePause_QueuesCommandsOneAtATimeForMultipleReadyUnits()
    {
        var tm = CreateTurnManager(CombatMode.ActivePause);
        var first = CreateUnit(isPlayer: true, spd: 20f);
        var second = CreateUnit(isPlayer: true, spd: 10f);
        var enemy = CreateUnit(isPlayer: false);
        InjectUnits(tm, new List<CombatUnit> { first, second }, new List<CombatUnit> { enemy });

        FillGauge(first);
        FillGauge(second);
        yield return null;
        yield return null;

        // Highest speed acts first, and only one panel's worth of state is live at a time.
        Assert.AreSame(first, tm.AwaitingInputFor,
            "With two units ready, the faster one should be commanded first.");

        Assert.IsTrue(tm.SubmitAttack(enemy));

        // Wait past the resolution and the inter-action micro-delay for the next unit.
        yield return new WaitForSeconds(0.4f);

        Assert.AreSame(second, tm.AwaitingInputFor,
            "After the first command resolves, the second ready unit should be awaiting input.");
    }

    // =========================================================================
    // INPUT WAIT
    //
    // The wait used to be a flat 15-second countdown against the player, with no timer, bar
    // or warning drawn anywhere — so Active Pause silently took the turn from anyone still
    // reading their skills, and it read as a bug rather than as time pressure. It is now a
    // failsafe against the command panel failing to open, and stops counting once the panel
    // reports itself ready.
    // =========================================================================

    [UnityTest]
    public IEnumerator AwaitingInput_DoesNotTimeOutOnceTheCommandUiIsUp()
    {
        // TurnManager schedules InitializeCombat shortly after Awake and logs an error with no
        // GridManager present. The other tests here finish before that fires; these two wait
        // several seconds by design. Unrelated to what they assert, so it is ignored rather
        // than matched — these tests check state, not log text.
        LogAssert.ignoreFailingMessages = true;

        var tm = CreateTurnManager(CombatMode.ActivePause);
        var player = CreateUnit(isPlayer: true);
        var enemy = CreateUnit(isPlayer: false);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { enemy });

        FillGauge(player);
        yield return null;
        yield return null;

        Assert.AreSame(player, tm.AwaitingInputFor, "The unit should be awaiting a command.");

        // No manual NotifyCommandUiReady: the real BattleCommandUI bootstraps itself into every
        // Play Mode session and acknowledges on its own, so this exercises the actual path.
        var ackField = typeof(TurnManager).GetField("commandUiAcknowledged",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsTrue((bool)ackField.GetValue(tm),
            "The command UI should have acknowledged the turn it was told about.");

        // Well past the 5s failsafe. A player reading their options must not lose the turn.
        yield return new WaitForSecondsRealtime(6f);

        Assert.AreSame(player, tm.AwaitingInputFor,
            "The turn was taken automatically while the command panel was open. Active Pause " +
            "must wait indefinitely once the player can actually see their options.");

        Assert.IsTrue(tm.SubmitAttack(enemy), "The command should still be accepted after the wait.");
    }

    [UnityTest]
    public IEnumerator AwaitingInput_FallsBackWhenTheCommandUiNeverOpens()
    {
        // Same unrelated GridManager error as the test above; see the note there.
        LogAssert.ignoreFailingMessages = true;

        // BattleCommandUI bootstraps itself via RuntimeInitializeOnLoadMethod and survives as
        // DontDestroyOnLoad, so it is present in every Play Mode test whether or not the test
        // asked for it. Disabling it is what actually reproduces "the panel never opened" —
        // leaving it alive means it acknowledges the turn and the failsafe correctly never
        // fires, which is the real behaviour, just not the one under test here.
        var commandUis = Object.FindObjectsByType<BattleCommandUI>(FindObjectsInactive.Include,
                                                                   FindObjectsSortMode.None);
        foreach (var ui in commandUis) ui.enabled = false;

        var tm = CreateTurnManager(CombatMode.ActivePause);
        var player = CreateUnit(isPlayer: true);
        var enemy = CreateUnit(isPlayer: false);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { enemy });

        FillGauge(player);
        yield return null;
        yield return null;

        Assert.AreSame(player, tm.AwaitingInputFor);

        // Asserted on state rather than on the warning text, so the test does not break when
        // someone rewords the log line. TearDown restores ignoreFailingMessages.
        yield return new WaitForSecondsRealtime(6f);

        foreach (var ui in commandUis) if (ui != null) ui.enabled = true;

        Assert.IsNull(tm.AwaitingInputFor,
            "With no command panel the battle must resolve the turn automatically rather than " +
            "freezing forever.");
    }
}
