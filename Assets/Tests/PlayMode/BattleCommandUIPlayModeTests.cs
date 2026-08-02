using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using SowurShield.Combat;

/// <summary>
/// Play Mode tests for BattleCommandUI — the panel that turns the active-pause wait into
/// an actual choice. Verifies it opens on the manager's event, submits the chosen command,
/// and reflects skill cooldown state.
/// </summary>
public class BattleCommandUIPlayModeTests
{
    private readonly List<Object> _cleanup = new List<Object>();
    private BattleCommandUI ui;

    private T Track<T>(T obj) where T : Object
    {
        _cleanup.Add(obj);
        return obj;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Bootstrap() may already have spawned one from a previous scene load.
        ui = Object.FindFirstObjectByType<BattleCommandUI>();
        if (ui == null)
        {
            var go = new GameObject("BattleCommandUI");
            ui = go.AddComponent<BattleCommandUI>();
        }
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var obj in _cleanup)
            if (obj != null) Object.Destroy(obj);
        _cleanup.Clear();
        yield return null;
    }

    private TurnManager CreateTurnManager()
    {
        var go = Track(new GameObject("TurnManager"));
        var tm = go.AddComponent<TurnManager>();
        tm.SetCombatMode(CombatMode.ActivePause);
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

    private static void SetPrivateField(object target, string name, object value)
    {
        var f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {target.GetType().Name}");
        f.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string name)
    {
        var f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {target.GetType().Name}");
        return (T)f.GetValue(target);
    }

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

    private GameObject Panel() => GetPrivateField<GameObject>(ui, "panel");
    private Button AttackButton() => GetPrivateField<Button>(ui, "attackButton");
    private Button SkillButton() => GetPrivateField<Button>(ui, "skillButton");
    private Button DefendButton() => GetPrivateField<Button>(ui, "defendButton");

    // ── Panel visibility ───────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator Panel_IsHiddenBeforeAnyTurnStarts()
    {
        yield return null;
        Assert.IsFalse(Panel().activeSelf, "The command panel must stay hidden until a turn starts.");
    }

    [UnityTest]
    public IEnumerator Panel_OpensWhenPlayerTurnStarts()
    {
        var tm = CreateTurnManager();
        var player = CreateUnit(isPlayer: true);
        var enemy = CreateUnit(isPlayer: false);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { enemy });

        yield return null; // UI binds to the new manager

        player.turnGauge = 100f;
        yield return null;
        yield return null;

        Assert.IsTrue(Panel().activeSelf,
            "The command panel should open when a player unit's turn starts in ActivePause.");
    }

    [UnityTest]
    public IEnumerator Panel_ClosesAfterCommandIsSubmitted()
    {
        var tm = CreateTurnManager();
        var player = CreateUnit(isPlayer: true);
        var enemy = CreateUnit(isPlayer: false);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { enemy });

        yield return null;
        player.turnGauge = 100f;
        yield return null;
        yield return null;
        Assert.IsTrue(Panel().activeSelf, "Precondition: panel should be open.");

        DefendButton().onClick.Invoke();
        yield return null;
        yield return null;

        Assert.IsFalse(Panel().activeSelf, "The panel should close once the command resolves.");
    }

    // ── Commands reach the TurnManager ─────────────────────────────────────────

    [UnityTest]
    public IEnumerator DefendButton_AppliesShieldToTheCommandingUnit()
    {
        var tm = CreateTurnManager();
        var player = CreateUnit(isPlayer: true);
        var enemy = CreateUnit(isPlayer: false);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { enemy });

        yield return null;
        player.turnGauge = 100f;
        yield return null;
        yield return null;

        DefendButton().onClick.Invoke();
        yield return null;
        yield return null;

        Assert.Greater(player.GetShieldReduction(), 0f,
            "Clicking Defend should apply the Shield status effect.");
    }

    [UnityTest]
    public IEnumerator AttackButton_WithSingleEnemyAutoTargetsAndResolves()
    {
        var tm = CreateTurnManager();
        var player = CreateUnit(isPlayer: true, atk: 50f);
        var enemy = CreateUnit(isPlayer: false);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { enemy });

        yield return null;
        player.turnGauge = 100f;
        yield return null;
        yield return null;

        // With exactly one living enemy there is nothing to choose, so the panel should
        // resolve immediately instead of asking for a click.
        AttackButton().onClick.Invoke();
        yield return null;
        yield return null;

        Assert.Less(enemy.currentHealth, 1000f,
            "Attack with a single possible target should resolve without target selection.");
        Assert.IsFalse(tm.IsWaitingForPlayerInput, "The battle should have resumed.");
    }

    [UnityTest]
    public IEnumerator AttackButton_WithTwoEnemiesEntersTargetSelection()
    {
        var tm = CreateTurnManager();
        var player = CreateUnit(isPlayer: true);
        var first = CreateUnit(isPlayer: false);
        var second = CreateUnit(isPlayer: false);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { first, second });

        yield return null;
        player.turnGauge = 100f;
        yield return null;
        yield return null;

        AttackButton().onClick.Invoke();
        yield return null;

        Assert.IsTrue(tm.IsWaitingForPlayerInput,
            "With two possible targets the battle should still be waiting for the player's click.");
        Assert.IsFalse(AttackButton().gameObject.activeSelf,
            "The action buttons should give way to the target prompt.");
        Assert.AreEqual(1000f, first.currentHealth, 0.01f, "Nothing should be hit yet.");
        Assert.AreEqual(1000f, second.currentHealth, 0.01f, "Nothing should be hit yet.");
    }

    // ── Skill button state ─────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator SkillButton_DisabledWhenUnitHasNoSkill()
    {
        var tm = CreateTurnManager();
        var player = CreateUnit(isPlayer: true);   // no skill initialized
        var enemy = CreateUnit(isPlayer: false);
        InjectUnits(tm, new List<CombatUnit> { player }, new List<CombatUnit> { enemy });

        yield return null;
        player.turnGauge = 100f;
        yield return null;
        yield return null;

        Assert.IsFalse(SkillButton().interactable,
            "A unit with no active skill must not offer a Skill command.");
    }
}
