using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using SowurShield.Animals;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for TurnManager.SelectTarget's AI targeting heuristics:
///   - Lethal-first: prefer a kill over chip damage, even if it isn't the front-column target
///   - Front-column fallback: when no kill is available, target the nearest column
///   - HP% tiebreak: among equal-column targets, prefer the lowest HP percentage
///
/// TurnManager is a MonoBehaviour singleton; SelectTarget/EstimateAttackDamage are private,
/// so tests use reflection to set up team lists and invoke the method directly,
/// following the pattern established in AnimalHusbandryTests.
/// </summary>
public class CombatTargetingTests
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

    private static object InvokePrivate(object target, string methodName, object[] args)
    {
        var m = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{methodName}' not found on {target.GetType().Name}");
        return m.Invoke(target, args);
    }

    // ── factory helpers ─────────────────────────────────────────────────────

    private CombatUnit CreateUnit(bool isPlayer, int column, float hp, float atk = 10f, float def = 0f, float spd = 10f)
    {
        var go = new GameObject(isPlayer ? "PlayerUnit" : "EnemyUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = isPlayer;
        unit.InitializeAsEnemy(go.name, hp, atk, def, spd);
        unit.gridPosition = new Vector2Int(column, 0);
        return unit;
    }

    private TurnManager CreateTurnManager(List<CombatUnit> playerUnits, List<CombatUnit> enemyUnits)
    {
        var go = new GameObject("TurnManager");
        Track(go);
        var tm = go.AddComponent<TurnManager>();
        SetField(tm, "playerUnits", playerUnits);
        SetField(tm, "enemyUnits", enemyUnits);
        return tm;
    }

    private CombatUnit SelectTarget(TurnManager tm, CombatUnit attacker)
    {
        return (CombatUnit)InvokePrivate(tm, "SelectTarget", new object[] { attacker });
    }

    private CombatUnit SelectSkillTarget(TurnManager tm, CombatUnit attacker, AnimalSkill skill)
    {
        return (CombatUnit)InvokePrivate(tm, "SelectSkillTarget", new object[] { attacker, skill });
    }

    private AnimalSkill CreateSkill(float damageMultiplier = 0f, AnimalSkillEffect statusEffect = AnimalSkillEffect.None,
        float healAmount = 0f, bool affectsAllies = false, bool affectsSelf = false)
    {
        var skill = ScriptableObject.CreateInstance<AnimalSkill>();
        Track(skill);
        skill.skillType = SkillType.Active;
        skill.damageMultiplier = damageMultiplier;
        skill.statusEffect = statusEffect;
        skill.healAmount = healAmount;
        skill.affectsAllies = affectsAllies;
        skill.affectsSelf = affectsSelf;
        return skill;
    }

    // =========================================================================
    // LETHAL-FIRST
    // =========================================================================

    [Test]
    public void SelectTarget_PrefersLethalKillOverFrontColumn()
    {
        // Player attacker (atk=50, 0 defense => 50 dmg) faces two enemies:
        //   - column 5 (front, closest): high HP, attack would NOT kill it
        //   - column 2 (farther): low HP, attack WOULD kill it
        var attacker = CreateUnit(isPlayer: true, column: 8, hp: 100f, atk: 50f);
        var frontTough = CreateUnit(isPlayer: false, column: 5, hp: 200f, def: 0f);
        var farWeak = CreateUnit(isPlayer: false, column: 2, hp: 40f, def: 0f);

        var tm = CreateTurnManager(
            new List<CombatUnit> { attacker },
            new List<CombatUnit> { frontTough, farWeak });

        CombatUnit target = SelectTarget(tm, attacker);

        Assert.AreSame(farWeak, target, "Should prioritize the killable target over the front-column target.");
    }

    [Test]
    public void SelectTarget_AmongMultipleLethalTargets_PicksLowestHealth()
    {
        // Both enemies are killable by a 50-damage hit; pick the one with less current HP.
        var attacker = CreateUnit(isPlayer: true, column: 8, hp: 100f, atk: 50f);
        var killableHigher = CreateUnit(isPlayer: false, column: 1, hp: 45f, def: 0f);
        var killableLower = CreateUnit(isPlayer: false, column: 4, hp: 20f, def: 0f);

        var tm = CreateTurnManager(
            new List<CombatUnit> { attacker },
            new List<CombatUnit> { killableHigher, killableLower });

        CombatUnit target = SelectTarget(tm, attacker);

        Assert.AreSame(killableLower, target, "Among multiple lethal targets, the lowest-HP one should be chosen.");
    }

    [Test]
    public void SelectTarget_LethalCheckAccountsForShieldReduction()
    {
        // Shield reduces effective HP (60 * (1 - 0.5) = 30), so a 50-damage hit is still
        // lethal through it even though it wouldn't one-shot the raw 60 HP.
        var attacker = CreateUnit(isPlayer: true, column: 8, hp: 100f, atk: 50f);

        var shielded = CreateUnit(isPlayer: false, column: 5, hp: 60f, def: 0f);
        shielded.ApplyStatusEffect(StatusEffectType.Shield, 0.5f, 3);

        var frontWeaker = CreateUnit(isPlayer: false, column: 3, hp: 80f, def: 0f);

        var tm = CreateTurnManager(
            new List<CombatUnit> { attacker },
            new List<CombatUnit> { shielded, frontWeaker });

        CombatUnit target = SelectTarget(tm, attacker);

        Assert.AreSame(shielded, target, "Shield-adjusted effective HP (30) is <= the 50 damage estimate, so the shielded unit is lethal and should be prioritized.");
    }

    // =========================================================================
    // FRONT-COLUMN FALLBACK (no lethal target available)
    // =========================================================================

    [Test]
    public void SelectTarget_PlayerAttacker_TargetsFrontmostEnemyColumn()
    {
        // No target is killable (atk too low relative to HP). Player should target
        // the rightmost (frontmost from player's perspective) enemy column.
        var attacker = CreateUnit(isPlayer: true, column: 8, hp: 100f, atk: 5f);
        var front = CreateUnit(isPlayer: false, column: 5, hp: 100f, def: 0f);
        var back = CreateUnit(isPlayer: false, column: 0, hp: 100f, def: 0f);

        var tm = CreateTurnManager(
            new List<CombatUnit> { attacker },
            new List<CombatUnit> { front, back });

        CombatUnit target = SelectTarget(tm, attacker);

        Assert.AreSame(front, target, "Player attacker should target the frontmost (rightmost) enemy column.");
    }

    [Test]
    public void SelectTarget_EnemyAttacker_TargetsFrontmostPlayerColumn()
    {
        var attacker = CreateUnit(isPlayer: false, column: 0, hp: 100f, atk: 5f);
        var front = CreateUnit(isPlayer: true, column: 6, hp: 100f, def: 0f);
        var back = CreateUnit(isPlayer: true, column: 8, hp: 100f, def: 0f);

        var tm = CreateTurnManager(
            new List<CombatUnit> { front, back },
            new List<CombatUnit> { attacker });

        CombatUnit target = SelectTarget(tm, attacker);

        Assert.AreSame(front, target, "Enemy attacker should target the frontmost (leftmost) player column.");
    }

    [Test]
    public void SelectTarget_SameColumn_TiebreaksByLowestHealthPercent()
    {
        // No lethal targets. Two enemies share the frontmost column; the one with
        // lower HP percentage should be chosen.
        var attacker = CreateUnit(isPlayer: true, column: 8, hp: 100f, atk: 1f);

        var go1 = CreateUnit(isPlayer: false, column: 5, hp: 100f, def: 0f);
        // Damage go1 down to 20% HP via reflection-free public API.
        go1.TakeDamageWithShield(80f);

        var go2 = CreateUnit(isPlayer: false, column: 5, hp: 100f, def: 0f);
        go2.TakeDamageWithShield(50f); // 50% HP remaining

        var tm = CreateTurnManager(
            new List<CombatUnit> { attacker },
            new List<CombatUnit> { go1, go2 });

        CombatUnit target = SelectTarget(tm, attacker);

        Assert.AreSame(go1, target, "Among same-column targets, the lowest HP% should be chosen.");
    }

    [Test]
    public void SelectTarget_ReturnsNull_WhenNoAliveEnemies()
    {
        var attacker = CreateUnit(isPlayer: true, column: 8, hp: 100f, atk: 10f);
        var dead = CreateUnit(isPlayer: false, column: 5, hp: 100f, def: 0f);
        dead.TakeDamageWithShield(1000f);

        var tm = CreateTurnManager(
            new List<CombatUnit> { attacker },
            new List<CombatUnit> { dead });

        CombatUnit target = SelectTarget(tm, attacker);

        Assert.IsNull(target, "SelectTarget should return null when there are no alive enemies.");
    }

    // =========================================================================
    // AI BEHAVIOR — Defensive / Support (enemy attackers only)
    // =========================================================================

    [Test]
    public void SelectTarget_DefensiveEnemy_TargetsHighestAttackPlayer()
    {
        // Neither player is killable or lethal-relevant; Defensive AI should pick
        // the player with the highest effective attack (the biggest threat),
        // even though it isn't the front-column target.
        var attacker = CreateUnit(isPlayer: false, column: 0, hp: 100f, atk: 1f);
        attacker.SetAIBehavior("Defensive");

        var frontWeakAttacker = CreateUnit(isPlayer: true, column: 6, hp: 100f, atk: 5f, def: 50f);
        var backStrongAttacker = CreateUnit(isPlayer: true, column: 8, hp: 100f, atk: 40f, def: 50f);

        var tm = CreateTurnManager(
            new List<CombatUnit> { frontWeakAttacker, backStrongAttacker },
            new List<CombatUnit> { attacker });

        CombatUnit target = SelectTarget(tm, attacker);

        Assert.AreSame(backStrongAttacker, target, "Defensive AI should target the player with the highest attack.");
    }

    [Test]
    public void SelectTarget_SupportEnemy_TargetsLowestDefensePlayer()
    {
        var attacker = CreateUnit(isPlayer: false, column: 0, hp: 100f, atk: 1f);
        attacker.SetAIBehavior("Support");

        var frontTanky = CreateUnit(isPlayer: true, column: 6, hp: 100f, atk: 5f, def: 50f);
        var backSquishy = CreateUnit(isPlayer: true, column: 8, hp: 100f, atk: 5f, def: 2f);

        var tm = CreateTurnManager(
            new List<CombatUnit> { frontTanky, backSquishy },
            new List<CombatUnit> { attacker });

        CombatUnit target = SelectTarget(tm, attacker);

        Assert.AreSame(backSquishy, target, "Support AI should target the player with the lowest defense.");
    }

    [Test]
    public void SelectTarget_AggressiveEnemy_StillUsesLethalFirst()
    {
        // Regression: default "Aggressive" behavior must preserve existing lethal-first logic.
        var attacker = CreateUnit(isPlayer: false, column: 0, hp: 100f, atk: 50f);
        attacker.SetAIBehavior("Aggressive");

        var frontTough = CreateUnit(isPlayer: true, column: 6, hp: 200f, def: 0f);
        var backWeak = CreateUnit(isPlayer: true, column: 8, hp: 40f, def: 0f);

        var tm = CreateTurnManager(
            new List<CombatUnit> { frontTough, backWeak },
            new List<CombatUnit> { attacker });

        CombatUnit target = SelectTarget(tm, attacker);

        Assert.AreSame(backWeak, target, "Aggressive AI should still prioritize a lethal kill over the front column.");
    }

    // =========================================================================
    // AI BEHAVIOR — Skill targeting (SelectSkillTarget)
    // =========================================================================

    [Test]
    public void SelectSkillTarget_AggressiveEnemy_OffensiveStatusSkill_TargetsHighestHealthEnemy()
    {
        // Aggressive AI should focus an offensive status skill (e.g. Poison) on the
        // highest-HP enemy (the tank), not the default lethal-first/front-column target.
        var attacker = CreateUnit(isPlayer: false, column: 0, hp: 100f, atk: 10f);
        attacker.SetAIBehavior("Aggressive");

        var lowHpFront = CreateUnit(isPlayer: true, column: 6, hp: 50f, def: 0f);
        var highHpBack = CreateUnit(isPlayer: true, column: 8, hp: 200f, def: 0f);

        var tm = CreateTurnManager(
            new List<CombatUnit> { lowHpFront, highHpBack },
            new List<CombatUnit> { attacker });

        var poisonSkill = CreateSkill(statusEffect: AnimalSkillEffect.Poison);

        CombatUnit target = SelectSkillTarget(tm, attacker, poisonSkill);

        Assert.AreSame(highHpBack, target, "Aggressive AI should focus offensive status skills on the highest-HP enemy.");
    }

    [Test]
    public void SelectSkillTarget_SupportEnemy_HealSkill_TargetsLowestHealthPercentAlly()
    {
        // Support AI should direct a heal skill to the ally with the lowest HP%.
        var attacker = CreateUnit(isPlayer: false, column: 0, hp: 100f, atk: 1f);
        attacker.SetAIBehavior("Support");

        var healthyAlly = CreateUnit(isPlayer: false, column: 1, hp: 100f, def: 0f);

        var hurtAlly = CreateUnit(isPlayer: false, column: 2, hp: 100f, def: 0f);
        hurtAlly.TakeDamageWithShield(80f); // 20% HP remaining

        var enemyPlayer = CreateUnit(isPlayer: true, column: 6, hp: 100f, def: 0f);

        var tm = CreateTurnManager(
            new List<CombatUnit> { enemyPlayer },
            new List<CombatUnit> { attacker, healthyAlly, hurtAlly });

        var healSkill = CreateSkill(healAmount: 20f, affectsSelf: true);

        CombatUnit target = SelectSkillTarget(tm, attacker, healSkill);

        Assert.AreSame(hurtAlly, target, "Support AI should direct heal skills to the ally with the lowest HP%.");
    }

    [Test]
    public void SelectSkillTarget_AggressiveEnemy_DamageSkill_UsesLethalFirstFallback()
    {
        // Regression: a damage skill with no killable enemy in range should fall back
        // to the default SelectTarget (front-column) logic, not the highest-HP heuristic.
        var attacker = CreateUnit(isPlayer: false, column: 0, hp: 100f, atk: 5f);
        attacker.SetAIBehavior("Aggressive");

        var killable = CreateUnit(isPlayer: true, column: 8, hp: 4f, def: 0f);
        var front = CreateUnit(isPlayer: true, column: 6, hp: 100f, def: 0f);

        var tm = CreateTurnManager(
            new List<CombatUnit> { front, killable },
            new List<CombatUnit> { attacker });

        var damageSkill = CreateSkill(damageMultiplier: 1f);

        CombatUnit target = SelectSkillTarget(tm, attacker, damageSkill);

        Assert.AreSame(killable, target, "Aggressive AI damage skills should still use lethal-first targeting.");
    }
}

} // namespace SowurShield.Tests
