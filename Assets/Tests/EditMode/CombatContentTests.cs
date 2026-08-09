using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SowurShield.Animals;

namespace SowurShield.Tests
{

/// <summary>
/// Tests the shipped combat *content* rather than the combat systems.
///
/// Both halves of this file cover the same class of bug: a system that works, sitting next to
/// data that never reaches it, with nothing failing loudly enough to notice.
///
/// Skills: the system had been implemented and tested for a while, but only seven skills
/// existed and they were all offensive status effects, so whole mechanics were unreachable —
/// nothing applied Stun, no skill healed, none buffed an ally, and the HappinessThreshold and
/// Season unlock conditions had never been used by any asset.
///
/// Enemies: Mountain and Volcano had 12 art files on disk that no EnemyData referenced,
/// because the filenames described different creatures than the data did.
/// </summary>
public class CombatContentTests
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

    // ── Enemy art wiring (2026-08-02) ───────────────────────────────────────────────────
    //
    // Mountain and Volcano shipped with 12 PNGs on disk whose filenames described entirely
    // different creatures from the EnemyData assets ("Enemy 19 — Snow Wolf" vs "ArmoredBear").
    // The data had sprite: {fileID: 0} and rendered as placeholder spheres in combat while
    // usable art sat unused beside it. The enemies were renamed to match the art.

    [Test]
    public void MountainAndVolcanoEnemies_HaveSprites()
    {
        // IronGolem and ObsidianGolem are knowingly excluded: there are 6 art files per biome
        // for 7 enemies each, and giving two enemies the same sprite would be worse than none.
        var known = new HashSet<string> { "IronGolem", "ObsidianGolem" };

        var missing = Resources.LoadAll<SowurShield.Combat.EnemyData>("Enemies")
            .Where(e => e != null && !known.Contains(e.name) && e.sprite == null)
            .Select(e => e.name)
            .ToList();

        Assert.IsEmpty(missing,
            "These enemies have no sprite and fall back to a placeholder sphere in combat: " +
            string.Join(", ", missing));
    }

    [Test]
    public void EveryEnemy_HasALocalizedDisplayName()
    {
        var unwired = Resources.LoadAll<SowurShield.Combat.EnemyData>("Enemies")
            .Where(e => e != null && e.displayName.IsEmpty)
            .Select(e => e.name)
            .ToList();

        Assert.IsEmpty(unwired,
            "These enemies have no displayName table entry, so GetDisplayName falls back to the " +
            "internal enemyName and the player sees an untranslated identifier: " +
            string.Join(", ", unwired));
    }

    [Test]
    public void EnemyLocalizationKeys_MatchTheirAssetName()
    {
        // Project convention, followed by every enemy: `enemyName` is the spaced English name
        // ("Cave Bat") while the key uses the PascalCase asset name ("enemy.CaveBat.name").
        // When an enemy is renamed, updating one without the other leaves the key pointing at
        // the old creature — which still resolves, so nothing errors and a wrong name ships.
        var mismatched = new List<string>();

        foreach (var e in Resources.LoadAll<SowurShield.Combat.EnemyData>("Enemies"))
        {
            if (e == null || e.displayName.IsEmpty) continue;

            string key = e.displayName.TableEntryReference.Key;
            if (string.IsNullOrEmpty(key)) continue;   // bound by id, nothing to compare

            // Spacing and casing are the only differences the convention allows between the two.
            // Casing has to be ignored because PascalCase capitalises words the English name
            // leaves lowercase: "Lord of Ashes" is keyed "enemy.LordOfAshes.name", and a
            // case-sensitive compare would flag that correct pair as a mismatch.
            string keyStem = key.Replace("enemy.", "").Replace(".name", "");
            if (!string.Equals(keyStem, e.enemyName.Replace(" ", ""),
                               System.StringComparison.OrdinalIgnoreCase))
                mismatched.Add($"{e.name}: key '{key}' but enemyName '{e.enemyName}'");
        }

        Assert.IsEmpty(mismatched,
            "Localization key and enemyName disagree: " + string.Join("; ", mismatched));
    }
}

}
