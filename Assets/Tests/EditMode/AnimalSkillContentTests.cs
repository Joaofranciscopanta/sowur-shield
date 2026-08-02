using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SowurShield.Animals;

namespace SowurShield.Tests
{

/// <summary>
/// Tests the shipped skill *content* rather than the skill system.
///
/// The system had been implemented and tested for a while, but only seven skills existed and
/// they were all offensive status effects, so whole mechanics were unreachable: nothing applied
/// Stun, no skill healed, none buffed an ally, and the HappinessThreshold and Season unlock
/// conditions had never been used by any asset. These tests fail if that coverage regresses —
/// a data-only gap that no system test would catch.
/// </summary>
public class AnimalSkillContentTests
{
    private AnimalSkill[] _skills;
    private AnimalData[] _animals;

    [SetUp]
    public void SetUp()
    {
        _skills = Resources.LoadAll<AnimalSkill>("AnimalSkills");
        _animals = Resources.LoadAll<AnimalData>("Animals");
    }

    // ── Mechanics that must have at least one skill using them ──────────────────────────

    [Test]
    public void SomeSkill_AppliesStun()
    {
        Assert.IsTrue(_skills.Any(s => s.statusEffect == AnimalSkillEffect.Stun),
            "No skill applies Stun. CombatUnit and TurnManager implement it, so with no asset " +
            "using it the mechanic is dead content the player can never see.");
    }

    [Test]
    public void SomeSkill_Heals()
    {
        Assert.IsTrue(_skills.Any(s => s.healAmount > 0f),
            "No skill heals. There is no other source of healing in combat, so without one " +
            "the player has no way to recover HP mid-battle.");
    }

    [Test]
    public void SomeSkill_BuffsAllies()
    {
        Assert.IsTrue(_skills.Any(s => s.affectsAllies && s.attackMultiplier != 1f),
            "No skill buffs allies, leaving TurnManager's ally-buff branch unreachable.");
    }

    [Test]
    public void SomeSkill_UnlocksOnHappiness()
    {
        Assert.IsTrue(_skills.Any(s => s.unlockCondition.conditionType == SkillUnlockConditionType.HappinessThreshold),
            "No skill unlocks on happiness. This is the hook that makes caring for animals " +
            "matter in combat; without it, farm care and battle are unconnected systems.");
    }

    [Test]
    public void SomeSkill_UnlocksOnSeason()
    {
        Assert.IsTrue(_skills.Any(s => s.unlockCondition.conditionType == SkillUnlockConditionType.Season),
            "No skill unlocks by season, so the seasonal unlock path is never exercised.");
    }

    // ── Data integrity ──────────────────────────────────────────────────────────────────

    [Test]
    public void EverySkill_HasIdAndName()
    {
        var broken = _skills
            .Where(s => string.IsNullOrEmpty(s.skillId) || string.IsNullOrEmpty(s.skillName))
            .Select(s => s.name)
            .ToList();

        Assert.IsEmpty(broken, "Skills missing skillId or skillName: " + string.Join(", ", broken));
    }

    [Test]
    public void SkillIds_AreUnique()
    {
        var dupes = _skills.GroupBy(s => s.skillId)
                           .Where(g => g.Count() > 1)
                           .Select(g => $"{g.Key} x{g.Count()}")
                           .ToList();

        Assert.IsEmpty(dupes, "Duplicate skillId values make a skill ambiguous to look up: " +
                              string.Join(", ", dupes));
    }

    [Test]
    public void DefensiveSkills_DoNoDamage()
    {
        // TurnManager deals damage whenever damageMultiplier > 0 and the skill is not
        // ally-targeted. A shield or heal skill left at the default multiplier of 1 would
        // therefore also punch the enemy, which is not what a defensive skill should do.
        var offenders = _skills
            .Where(s => !s.affectsAllies
                        && s.damageMultiplier > 0f
                        && (s.statusEffect == AnimalSkillEffect.Shield || s.healAmount > 0f))
            .Select(s => $"{s.name} (dmg {s.damageMultiplier})")
            .ToList();

        Assert.IsEmpty(offenders,
            "These defensive skills also deal damage because damageMultiplier is above 0: " +
            string.Join(", ", offenders));
    }

    [Test]
    public void AllyTargetedSkills_DoNotAlsoTargetSelf()
    {
        // TurnManager's ally list already contains the caster, so a skill with both flags set
        // heals or buffs the caster twice.
        var offenders = _skills
            .Where(s => s.affectsAllies && s.affectsSelf)
            .Select(s => s.name)
            .ToList();

        Assert.IsEmpty(offenders,
            "affectsAllies already includes the caster, so these skills apply to it twice: " +
            string.Join(", ", offenders));
    }

    // ── Wiring: skills must actually reach the animals ──────────────────────────────────

    [Test]
    public void NonEggAnimals_HaveAnActiveSkill()
    {
        var missing = _animals
            .Where(a => a != null && !a.name.StartsWith("egg_") && a.activeSkill == null)
            .Select(a => a.name)
            .ToList();

        Assert.IsEmpty(missing,
            "These animals have no active skill and would fight with basic attacks only: " +
            string.Join(", ", missing));
    }

    [Test]
    public void NonEggAnimals_HavePassiveSkillsAvailable()
    {
        // This was the actual bug: availablePassiveSkills was empty on 26 of 28 animals, so
        // the unlock conditions were evaluated against an empty list and no animal could ever
        // gain a passive, however happy it was.
        var missing = _animals
            .Where(a => a != null && !a.name.StartsWith("egg_")
                        && (a.availablePassiveSkills == null || a.availablePassiveSkills.Count == 0))
            .Select(a => a.name)
            .ToList();

        Assert.IsEmpty(missing,
            "These animals have an empty availablePassiveSkills list, so they can never unlock " +
            "any passive regardless of happiness or season: " + string.Join(", ", missing));
    }

    [Test]
    public void HappinessPassive_UnlocksOnlyWhenHappyEnough()
    {
        var happinessSkill = _skills.FirstOrDefault(
            s => s.unlockCondition.conditionType == SkillUnlockConditionType.HappinessThreshold);
        Assert.IsNotNull(happinessSkill, "Expected at least one happiness-gated skill.");

        var go = new GameObject("SkillContentTestAnimal");
        try
        {
            var animal = go.AddComponent<Animal>();
            var cow = _animals.FirstOrDefault(a => a != null && a.name == "cow");
            Assert.IsNotNull(cow, "Expected a 'cow' AnimalData in Resources/Animals.");

            SetPrivate(animal, "animalData", cow);
            float threshold = happinessSkill.unlockCondition.minHappiness;

            SetPrivate(animal, "happiness", threshold - 10f);
            Assert.IsFalse(happinessSkill.CanUnlock(animal, 1, "Summer"),
                $"{happinessSkill.skillName} unlocked below its {threshold} happiness threshold, " +
                "which would make caring for the animal pointless.");

            SetPrivate(animal, "happiness", threshold + 5f);
            Assert.IsTrue(happinessSkill.CanUnlock(animal, 1, "Summer"),
                $"{happinessSkill.skillName} stayed locked above its {threshold} threshold, so " +
                "the reward for keeping animals happy never arrives.");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private static void SetPrivate(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}

}
