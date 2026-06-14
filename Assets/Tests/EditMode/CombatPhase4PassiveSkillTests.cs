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

    [Test]
    public void ApplyUnlockedPassiveSkills_FamilyCountConditionMet_AppliesBuff()
    {
        // Register 3 Bovidae animals in the roster so FamilyCount (min 2) is satisfied.
        var rosterGo = new GameObject("AnimalRoster");
        Track(rosterGo);
        var roster = rosterGo.AddComponent<AnimalRoster>();

        var registeredAnimals = new List<Animal>();
        for (int i = 0; i < 3; i++)
            registeredAnimals.Add(CreateAnimal(combatClass: "Tank", animalFamily: "Bovidae", passiveSkills: null));
        SetField(roster, "registeredAnimals", registeredAnimals);

        var skill = CreatePassiveSkill(SkillUnlockConditionType.FamilyCount, minFamilyCount: 2,
            atkMult: 1.15f, defMult: 1f, spdMult: 1f);
        var animal = CreateAnimal(combatClass: "Tank", animalFamily: "Bovidae", passiveSkills: new List<AnimalSkill> { skill });
        var unit = CreateCombatUnit(atk: 10f, def: 10f, spd: 10f);
        var spawner = CreateSpawner();

        InvokePrivate(spawner, "ApplyUnlockedPassiveSkills", new object[] { animal, unit });

        Assert.AreEqual(11.5f, unit.GetAttack(), 0.001f, "Attack buff from FamilyCount-unlocked passive should be applied.");
    }

    [Test]
    public void ApplyUnlockedPassiveSkills_FamilyCountConditionNotMet_NoBuff()
    {
        var rosterGo = new GameObject("AnimalRoster");
        Track(rosterGo);
        var roster = rosterGo.AddComponent<AnimalRoster>();
        SetField(roster, "registeredAnimals", new List<Animal>());

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
