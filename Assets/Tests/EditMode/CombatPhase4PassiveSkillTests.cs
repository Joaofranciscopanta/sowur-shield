using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using SowurShield.Animals;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for Combat Phase 4 passive skill unlocks
/// (CombatTeamSpawner.ApplyUnlockedPassiveSkills, called during team spawn).
/// </summary>
public class CombatPhase4PassiveSkillTests
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

        if (AnimalRoster.Instance != null)
            Object.DestroyImmediate(AnimalRoster.Instance.gameObject);

        // The team is a persistent singleton; leaving entries behind would leak into the
        // next test's family counts.
        TeamAssemblerData.Instance.team.Clear();
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

    private Animal CreateAnimal(string combatClass, string animalFamily, List<AnimalSkill> passiveSkills)
    {
        var go = new GameObject("Animal");
        Track(go);
        go.AddComponent<SpriteRenderer>();
        var animal = go.AddComponent<Animal>();

        var data = ScriptableObject.CreateInstance<AnimalData>();
        Track(data);
        data.combatClass = combatClass;
        data.animalFamily = animalFamily;
        data.availablePassiveSkills = passiveSkills ?? new List<AnimalSkill>();

        SetField(animal, "animalData", data);
        return animal;
    }

    /// <summary>An AnimalData on its own, for building team entries without a GameObject.</summary>
    private AnimalData CreateAnimalData(string combatClass, string animalFamily)
    {
        var data = ScriptableObject.CreateInstance<AnimalData>();
        Track(data);
        data.combatClass = combatClass;
        data.animalFamily = animalFamily;
        return data;
    }

    private CombatUnit CreateCombatUnit(float atk = 10f, float def = 10f, float spd = 10f)
    {
        var go = new GameObject("CombatUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = true;
        unit.InitializeAsEnemy(go.name, 100f, atk, def, spd);
        return unit;
    }

    private AnimalSkill CreatePassiveSkill(SkillUnlockConditionType conditionType, string requiredClass = "",
        string requiredFamily = "", int minFamilyCount = 0,
        float atkMult = 1f, float defMult = 1f, float spdMult = 1f)
    {
        var skill = ScriptableObject.CreateInstance<AnimalSkill>();
        Track(skill);
        skill.skillType = SkillType.Passive;
        skill.attackMultiplier = atkMult;
        skill.defenseMultiplier = defMult;
        skill.speedMultiplier = spdMult;
        skill.unlockCondition = new SkillUnlockCondition
        {
            conditionType = conditionType,
            requiredClass = requiredClass,
            requiredFamily = requiredFamily,
            minFamilyCount = minFamilyCount,
        };
        return skill;
    }

    private CombatTeamSpawner CreateSpawner()
    {
        var go = new GameObject("CombatTeamSpawner");
        Track(go);
        return go.AddComponent<CombatTeamSpawner>();
    }

    // =========================================================================
    // CombatClass unlock condition
    // =========================================================================

    [Test]
    public void ApplyUnlockedPassiveSkills_CombatClassConditionMet_AppliesBuff()
    {
        var skill = CreatePassiveSkill(SkillUnlockConditionType.CombatClass, requiredClass: "Tank",
            atkMult: 1f, defMult: 1.2f, spdMult: 1f);
        var animal = CreateAnimal(combatClass: "Tank", animalFamily: "Bovidae", passiveSkills: new List<AnimalSkill> { skill });
        var unit = CreateCombatUnit(atk: 10f, def: 10f, spd: 10f);
        var spawner = CreateSpawner();

        InvokePrivate(spawner, "ApplyUnlockedPassiveSkills", new object[] { animal, unit });

        Assert.AreEqual(12f, unit.GetDefense(), 0.001f, "Defense buff from unlocked passive skill should be applied.");
    }

    [Test]
    public void ApplyUnlockedPassiveSkills_CombatClassConditionNotMet_NoBuff()
    {
        var skill = CreatePassiveSkill(SkillUnlockConditionType.CombatClass, requiredClass: "Tank",
            atkMult: 1f, defMult: 1.2f, spdMult: 1f);
        var animal = CreateAnimal(combatClass: "DPS", animalFamily: "Bovidae", passiveSkills: new List<AnimalSkill> { skill });
        var unit = CreateCombatUnit(atk: 10f, def: 10f, spd: 10f);
        var spawner = CreateSpawner();

        InvokePrivate(spawner, "ApplyUnlockedPassiveSkills", new object[] { animal, unit });

        Assert.AreEqual(10f, unit.GetDefense(), 0.001f, "Unmet unlock condition should not apply a buff.");
    }

    // =========================================================================
    // FamilyCount unlock condition
    // =========================================================================

    /// <summary>
    /// FamilyCount counts within the ASSEMBLED TEAM as of 2026-09-06. It used to ask
    /// AnimalRoster for the count across the whole farm, so a farm with plenty of one
    /// family had these passives permanently unlocked regardless of who was taken to
    /// battle — the team the player built made no difference.
    /// </summary>
    [Test]
    public void ApplyUnlockedPassiveSkills_FamilyCountConditionMet_AppliesBuff()
    {
        // Put 3 Bovidae on the TEAM so FamilyCount (min 2) is satisfied.
        var team = TeamAssemblerData.Instance.team;
        team.Clear();
        for (int i = 0; i < 3; i++)
        {
            team.Add(new TeamAssemblerData.PositionedAnimal
            {
                animalData = CreateAnimalData(combatClass: "Tank", animalFamily: "Bovidae"),
                animalId = "TeamCow_" + i,
                gridPosition = new Vector2Int(6, i)
            });
        }

        var skill = CreatePassiveSkill(SkillUnlockConditionType.FamilyCount, minFamilyCount: 2,
            atkMult: 1.15f, defMult: 1f, spdMult: 1f);
        var animal = CreateAnimal(combatClass: "Tank", animalFamily: "Bovidae", passiveSkills: new List<AnimalSkill> { skill });
        var unit = CreateCombatUnit(atk: 10f, def: 10f, spd: 10f);
        var spawner = CreateSpawner();

        InvokePrivate(spawner, "ApplyUnlockedPassiveSkills", new object[] { animal, unit });

        Assert.AreEqual(11.5f, unit.GetAttack(), 0.001f, "Attack buff from FamilyCount-unlocked passive should be applied.");
        team.Clear();
    }

    [Test]
    public void ApplyUnlockedPassiveSkills_FamilyCountConditionNotMet_NoBuff()
    {
        // An empty team means no family reaches the threshold, however many animals the
        // farm itself holds.
        TeamAssemblerData.Instance.team.Clear();

        var skill = CreatePassiveSkill(SkillUnlockConditionType.FamilyCount, minFamilyCount: 2,
            atkMult: 1.15f, defMult: 1f, spdMult: 1f);
        var animal = CreateAnimal(combatClass: "Tank", animalFamily: "Bovidae", passiveSkills: new List<AnimalSkill> { skill });
        var unit = CreateCombatUnit(atk: 10f, def: 10f, spd: 10f);
        var spawner = CreateSpawner();

        InvokePrivate(spawner, "ApplyUnlockedPassiveSkills", new object[] { animal, unit });

        Assert.AreEqual(10f, unit.GetAttack(), 0.001f, "FamilyCount condition not met should not apply a buff.");
    }

    // =========================================================================
    // Default multipliers — no buff applied even if unlocked
    // =========================================================================

    [Test]
    public void ApplyUnlockedPassiveSkills_UnlockedWithDefaultMultipliers_NoBuffApplied()
    {
        var skill = CreatePassiveSkill(SkillUnlockConditionType.None, atkMult: 1f, defMult: 1f, spdMult: 1f);
        var animal = CreateAnimal(combatClass: "DPS", animalFamily: "Bovidae", passiveSkills: new List<AnimalSkill> { skill });
        var unit = CreateCombatUnit(atk: 10f, def: 10f, spd: 10f);
        var spawner = CreateSpawner();

        InvokePrivate(spawner, "ApplyUnlockedPassiveSkills", new object[] { animal, unit });

        Assert.AreEqual(0, unit.GetActiveBuffCount(), "A passive skill with default (1f) multipliers should not register a buff.");
    }

    [Test]
    public void ApplyUnlockedPassiveSkills_ActiveSkillIgnored()
    {
        var skill = CreatePassiveSkill(SkillUnlockConditionType.None, atkMult: 1.5f, defMult: 1f, spdMult: 1f);
        skill.skillType = SkillType.Active;
        var animal = CreateAnimal(combatClass: "DPS", animalFamily: "Bovidae", passiveSkills: new List<AnimalSkill> { skill });
        var unit = CreateCombatUnit(atk: 10f, def: 10f, spd: 10f);
        var spawner = CreateSpawner();

        InvokePrivate(spawner, "ApplyUnlockedPassiveSkills", new object[] { animal, unit });

        Assert.AreEqual(10f, unit.GetAttack(), 0.001f, "Active skills should be ignored by passive-skill application.");
    }

    [Test]
    public void ApplyUnlockedPassiveSkills_NoPassiveSkills_NoBuff()
    {
        var animal = CreateAnimal(combatClass: "DPS", animalFamily: "Bovidae", passiveSkills: new List<AnimalSkill>());
        var unit = CreateCombatUnit(atk: 10f, def: 10f, spd: 10f);
        var spawner = CreateSpawner();

        InvokePrivate(spawner, "ApplyUnlockedPassiveSkills", new object[] { animal, unit });

        Assert.AreEqual(0, unit.GetActiveBuffCount());
    }
}

} // namespace SowurShield.Tests
