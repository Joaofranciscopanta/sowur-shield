using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for the Combat Phase 3 player combo counter
/// (TurnManager.GetComboCount + per-attack combo damage bonus).
/// </summary>
public class CombatPhase3ComboTests
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

    private CombatUnit CreateUnit(bool isPlayer, float hp = 1000f, float atk = 10f, float def = 0f, float spd = 10f)
    {
        var go = new GameObject(isPlayer ? "PlayerUnit" : "EnemyUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = isPlayer;
        unit.InitializeAsEnemy(go.name, hp, atk, def, spd);
        return unit;
    }

    private TurnManager CreateTurnManager()
    {
        var go = new GameObject("TurnManager");
        Track(go);
        return go.AddComponent<TurnManager>();
    }

    private static void ExecuteAttack(TurnManager tm, CombatUnit attacker, CombatUnit target)
    {
        var m = typeof(TurnManager).GetMethod("ExecuteAttack", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "Method 'ExecuteAttack' not found on TurnManager");
        m.Invoke(tm, new object[] { attacker, target });
    }

    [Test]
    public void ExecuteAttack_ConsecutivePlayerHitsOnSameTarget_IncrementsCombo()
    {
        var tm = CreateTurnManager();
        var attacker = CreateUnit(isPlayer: true);
        var target = CreateUnit(isPlayer: false);

        ExecuteAttack(tm, attacker, target);
        Assert.AreEqual(1, tm.GetComboCount(), "First hit on a target should start the combo at 1.");

        ExecuteAttack(tm, attacker, target);
        Assert.AreEqual(2, tm.GetComboCount(), "A second consecutive hit on the same target should increment the combo to 2.");

        ExecuteAttack(tm, attacker, target);
        Assert.AreEqual(3, tm.GetComboCount(), "A third consecutive hit on the same target should increment the combo to 3.");
    }

    [Test]
    public void ExecuteAttack_ComboCapsAtMaxComboCount()
    {
        var tm = CreateTurnManager();
        var attacker = CreateUnit(isPlayer: true);
        var target = CreateUnit(isPlayer: false);

        for (int i = 0; i < 10; i++)
            ExecuteAttack(tm, attacker, target);

        Assert.AreEqual(5, tm.GetComboCount(), "Combo count should be capped at 5.");
    }

    [Test]
    public void ExecuteAttack_SwitchingTargets_ResetsComboToOne()
    {
        var tm = CreateTurnManager();
        var attacker = CreateUnit(isPlayer: true);
        var targetA = CreateUnit(isPlayer: false);
        var targetB = CreateUnit(isPlayer: false);

        ExecuteAttack(tm, attacker, targetA);
        ExecuteAttack(tm, attacker, targetA);
        Assert.AreEqual(2, tm.GetComboCount(), "Combo should be 2 after two hits on targetA.");

        ExecuteAttack(tm, attacker, targetB);
        Assert.AreEqual(1, tm.GetComboCount(), "Switching to a new target should reset the combo to 1.");
    }

    [Test]
    public void ExecuteAttack_EnemyAction_ResetsComboToZero()
    {
        var tm = CreateTurnManager();
        var player = CreateUnit(isPlayer: true);
        var enemy = CreateUnit(isPlayer: false);

        ExecuteAttack(tm, player, enemy);
        ExecuteAttack(tm, player, enemy);
        Assert.AreEqual(2, tm.GetComboCount(), "Combo should be 2 after two player hits.");

        // Enemy attacks the player back.
        ExecuteAttack(tm, enemy, player);
        Assert.AreEqual(0, tm.GetComboCount(), "Any enemy action should reset the player's combo to 0.");
    }

    [Test]
    public void ExecuteAttack_ComboBonus_IncreasesAverageDamageDealt()
    {
        // Crit chance (5%) introduces per-hit variance, so average over many trials
        // to verify the +4%/stack combo bonus reliably increases expected damage.
        const int trials = 300;
        const float atk = 100f;

        float totalDamageComboOne = 0f;
        float totalDamageComboFive = 0f;

        for (int i = 0; i < trials; i++)
        {
            var tm = CreateTurnManager();
            var attacker = CreateUnit(isPlayer: true, atk: atk);

            var targetNoCombo = CreateUnit(isPlayer: false, hp: 1_000_000f, def: 0f);
            ExecuteAttack(tm, attacker, targetNoCombo);
            totalDamageComboOne += 1_000_000f - targetNoCombo.currentHealth;

            var targetWithCombo = CreateUnit(isPlayer: false, hp: 1_000_000f, def: 0f);
            for (int hit = 0; hit < 4; hit++)
                ExecuteAttack(tm, attacker, targetWithCombo); // build combo to 5

            float healthBeforeFifthHit = targetWithCombo.currentHealth;
            ExecuteAttack(tm, attacker, targetWithCombo); // 5th consecutive hit, combo == 5
            totalDamageComboFive += healthBeforeFifthHit - targetWithCombo.currentHealth;

            TearDown();
        }

        float avgComboOne = totalDamageComboOne / trials;
        float avgComboFive = totalDamageComboFive / trials;

        // At combo 5 the bonus is +16% (4 stacks above 1 * 4%/stack).
        Assert.Greater(avgComboFive, avgComboOne,
            "Average damage at combo stack 5 should exceed average damage at combo stack 1.");
        Assert.AreEqual(avgComboOne * 1.16f, avgComboFive, avgComboOne * 0.05f,
            "Average combo-5 damage should be approximately 16% higher than combo-1 damage.");
    }
}

} // namespace SowurShield.Tests
