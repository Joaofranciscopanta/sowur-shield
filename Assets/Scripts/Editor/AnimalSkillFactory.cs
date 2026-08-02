using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SowurShield.Animals;

namespace SowurShield.Editor
{
/// <summary>
/// Creates the twelve gameplay skills that fill the holes in the existing roster, and wires
/// passive skills onto the animals that qualify for them.
///
/// Why these twelve: the seven skills that shipped are all offensive status effects
/// (Poison/Weakness/Shield), which left the roster flat. Nothing applied Stun, there was no
/// heal anywhere in the game, no ally buff, ten Tank-class animals with no tank skill, and a
/// single Support unit. Two of the new skills key off happiness so that caring for animals
/// translates into combat power — SkillUnlockCondition has supported HappinessThreshold and
/// Season since it was written, and nothing had ever used either.
///
/// Everything here is data-only. Verified against TurnManager: heal is applied at
/// `ally.Heal(skill.healAmount)` and ally stat buffs at `ally.ApplyStatBuff(...)`, so no
/// combat code needs to change for any of these to work.
///
/// Idempotent: re-running updates the existing assets in place rather than duplicating them,
/// which also preserves their GUIDs and therefore every reference already pointing at them.
/// </summary>
public static class AnimalSkillFactory
{
    private const string SkillFolder = "Assets/Resources/AnimalSkills";

    /// <summary>
    /// One skill definition. Mirrors the AnimalSkill fields that matter, so the whole set
    /// reads as a single balance table instead of twelve scattered object initialisers.
    /// </summary>
    private struct SkillDef
    {
        public string assetName, skillId, skillName, description;
        public SkillType type;

        public float damageMultiplier, attackMultiplier, defenseMultiplier, speedMultiplier;
        public float healAmount;
        public bool affectsAllies;
        public int cooldownTurns;

        public AnimalSkillEffect statusEffect;
        public float statusEffectValue;
        public int statusEffectDuration;

        public SkillUnlockConditionType conditionType;
        public string requiredClass, requiredFamily, requiredSeason;
        public int minFamilyCount;
        public float minHappiness;
    }

    /// <summary>
    /// Default multipliers are 1 (no change). Written as a helper so each definition below
    /// only states what it actually alters — a skill listing speedMultiplier: 1 reads as if
    /// speed were part of its design when it is not.
    /// </summary>
    private static SkillDef NewDef(string assetName, string skillId, string skillName,
                                   string description, SkillType type)
    {
        return new SkillDef
        {
            assetName = assetName,
            skillId = skillId,
            skillName = skillName,
            description = description,
            type = type,
            damageMultiplier = 1f,
            attackMultiplier = 1f,
            defenseMultiplier = 1f,
            speedMultiplier = 1f,
            healAmount = 0f,
            affectsAllies = false,
            cooldownTurns = 0,
            statusEffect = AnimalSkillEffect.None,
            statusEffectValue = 0f,
            statusEffectDuration = 0,
            conditionType = SkillUnlockConditionType.None,
            requiredClass = "",
            requiredFamily = "",
            requiredSeason = "",
            minFamilyCount = 0,
            minHappiness = 0f,
        };
    }

    private static List<SkillDef> BuildSkillSet()
    {
        var list = new List<SkillDef>();

        // ── Stun: nothing in the game applied it before ────────────────────────────────
        var hoofKick = NewDef("HoofKick", "hoof_kick", "Hoof Kick",
            "A heavy kick that knocks the wind out of the target, making it skip its next turn.",
            SkillType.Active);
        hoofKick.damageMultiplier = 1.4f;
        hoofKick.cooldownTurns = 4;              // Stun is the strongest effect: longest cooldown
        hoofKick.statusEffect = AnimalSkillEffect.Stun;
        hoofKick.statusEffectDuration = 1;
        list.Add(hoofKick);

        // ── Tank identity: ten Tank animals had no tank skill ──────────────────────────
        var hideWall = NewDef("HideWall", "hide_wall", "Hide Wall",
            "Braces behind thick hide, sharply reducing incoming damage for several turns.",
            SkillType.Active);
        // 0, not the default 1: TurnManager deals damage whenever damageMultiplier > 0 and the
        // skill is not ally-targeted, so a purely defensive skill left at 1 would also punch.
        hideWall.damageMultiplier = 0f;
        hideWall.cooldownTurns = 4;
        hideWall.statusEffect = AnimalSkillEffect.Shield;
        hideWall.statusEffectValue = 0.4f;       // stronger than FeatherShield's 0.3, longer cooldown
        hideWall.statusEffectDuration = 3;
        hideWall.conditionType = SkillUnlockConditionType.CombatClass;
        hideWall.requiredClass = "Tank";
        list.Add(hideWall);

        // ── Happiness hooks: the link between farming and combat ───────────────────────
        var herdBond = NewDef("HerdBond", "herd_bond", "Herd Bond",
            "A well-cared-for animal fights with confidence, gaining attack and defense.",
            SkillType.Passive);
        herdBond.attackMultiplier = 1.15f;
        herdBond.defenseMultiplier = 1.15f;
        herdBond.conditionType = SkillUnlockConditionType.HappinessThreshold;
        herdBond.minHappiness = 75f;
        list.Add(herdBond);

        var loyalCompanion = NewDef("LoyalCompanion", "loyal_companion", "Loyal Companion",
            "An animal that trusts you completely endures far more punishment at your side.",
            SkillType.Passive);
        loyalCompanion.defenseMultiplier = 1.2f; // HP has no multiplier field; defense is the closest lever
        loyalCompanion.conditionType = SkillUnlockConditionType.HappinessThreshold;
        loyalCompanion.minHappiness = 90f;
        list.Add(loyalCompanion);

        // ── Family-count synergies: reward building a themed team ──────────────────────
        var roosterFury = NewDef("RoosterFury", "rooster_fury", "Rooster Fury",
            "Emboldened by the flock, it throws itself at the enemy with reckless force.",
            SkillType.Active);
        roosterFury.damageMultiplier = 1.6f;
        roosterFury.cooldownTurns = 3;
        roosterFury.conditionType = SkillUnlockConditionType.FamilyCount;
        roosterFury.requiredFamily = "Galliformes";
        roosterFury.minFamilyCount = 3;
        list.Add(roosterFury);

        var largeBrood = NewDef("LargeBrood", "large_brood", "Large Brood",
            "Surrounded by its own kind, it darts about with restless energy.",
            SkillType.Passive);
        largeBrood.speedMultiplier = 1.1f;
        largeBrood.conditionType = SkillUnlockConditionType.FamilyCount;
        largeBrood.requiredFamily = "Galliformes";
        largeBrood.minFamilyCount = 5;
        list.Add(largeBrood);

        // ── Season hooks: supported since launch, never used ───────────────────────────
        var winterCoat = NewDef("WinterCoat", "winter_coat", "Winter Coat",
            "A thick seasonal coat blunts blows that would otherwise land hard.",
            SkillType.Passive);
        winterCoat.defenseMultiplier = 1.25f;
        winterCoat.conditionType = SkillUnlockConditionType.Season;
        winterCoat.requiredSeason = "Winter";
        list.Add(winterCoat);

        var springVigor = NewDef("SpringVigor", "spring_vigor", "Spring Vigor",
            "The turning season puts a spring in its step.",
            SkillType.Passive);
        springVigor.speedMultiplier = 1.2f;
        springVigor.conditionType = SkillUnlockConditionType.Season;
        springVigor.requiredSeason = "Spring";
        list.Add(springVigor);

        // ── A cheap attack: every existing active costs 2-3 turns of cooldown ──────────
        var precisePeck = NewDef("PrecisePeck", "precise_peck", "Precise Peck",
            "A quick, accurate jab. Not devastating, but always ready.",
            SkillType.Active);
        precisePeck.damageMultiplier = 1.3f;
        precisePeck.cooldownTurns = 1;
        list.Add(precisePeck);

        // ── Healing: none existed anywhere in the game ─────────────────────────────────
        var restoringSong = NewDef("RestoringSong", "restoring_song", "Restoring Song",
            "A clear song that mends the wounds of every ally who hears it.",
            SkillType.Active);
        restoringSong.damageMultiplier = 0f;     // support skill, deals no damage
        restoringSong.healAmount = 25f;
        restoringSong.affectsAllies = true;
        restoringSong.cooldownTurns = 4;
        restoringSong.conditionType = SkillUnlockConditionType.CombatClass;
        restoringSong.requiredClass = "Support";
        list.Add(restoringSong);

        // ── Ally buff: also absent before ──────────────────────────────────────────────
        var flockCall = NewDef("FlockCall", "flock_call", "Flock Call",
            "A rallying cry that sharpens the whole team's attacks for a short while.",
            SkillType.Active);
        flockCall.damageMultiplier = 0f;         // buff only, deals no damage
        flockCall.attackMultiplier = 1.2f;
        flockCall.affectsAllies = true;
        flockCall.cooldownTurns = 4;
        flockCall.statusEffectDuration = 2;      // TurnManager reads this as the buff duration
        flockCall.conditionType = SkillUnlockConditionType.AnimalFamily;
        flockCall.requiredFamily = "Passeridae";
        list.Add(flockCall);

        // ── Rabbit identity: it was a generic DPS with nothing of its own ──────────────
        var burrow = NewDef("Burrow", "burrow", "Burrow",
            "Vanishes underground for a moment, avoiding the worst of the next blow.",
            SkillType.Active);
        burrow.damageMultiplier = 0f;            // purely defensive — see HideWall
        burrow.cooldownTurns = 3;
        burrow.statusEffect = AnimalSkillEffect.Shield;
        burrow.statusEffectValue = 0.5f;         // strongest shield, but only one turn
        burrow.statusEffectDuration = 1;
        burrow.conditionType = SkillUnlockConditionType.AnimalFamily;
        burrow.requiredFamily = "Leporidae";
        list.Add(burrow);

        return list;
    }

    /// <summary>
    /// No modal dialog: this menu item is also driven through automation, and
    /// EditorUtility.DisplayDialog blocks the editor until a human clicks it.
    /// </summary>
    [MenuItem("Tools/Animals/Create Animal Skills")]
    public static void CreateSkills()
    {
        if (!AssetDatabase.IsValidFolder(SkillFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "AnimalSkills");

        int created = 0, updated = 0;

        foreach (var def in BuildSkillSet())
        {
            string path = $"{SkillFolder}/{def.assetName}.asset";

            // Update in place when it already exists: creating a replacement would mint a new
            // GUID and silently break every AnimalData already referencing the old asset.
            var skill = AssetDatabase.LoadAssetAtPath<AnimalSkill>(path);
            bool isNew = skill == null;
            if (isNew) skill = ScriptableObject.CreateInstance<AnimalSkill>();

            skill.skillId = def.skillId;
            skill.skillName = def.skillName;
            skill.skillType = def.type;
            skill.description = def.description;

            skill.damageMultiplier = def.damageMultiplier;
            skill.attackMultiplier = def.attackMultiplier;
            skill.defenseMultiplier = def.defenseMultiplier;
            skill.speedMultiplier = def.speedMultiplier;
            skill.healAmount = def.healAmount;
            skill.affectsAllies = def.affectsAllies;

            // affectsSelf is false whenever affectsAllies is true: TurnManager's ally list
            // (`playerUnits`/`enemyUnits`) already contains the caster, so setting both would
            // heal or buff the caster twice — double value for the same skill.
            skill.affectsSelf = !def.affectsAllies;
            skill.cooldownTurns = def.cooldownTurns;

            skill.statusEffect = def.statusEffect;
            skill.statusEffectValue = def.statusEffectValue;
            skill.statusEffectDuration = def.statusEffectDuration;

            skill.unlockCondition = new SkillUnlockCondition
            {
                conditionType = def.conditionType,
                requiredClass = def.requiredClass,
                requiredFamily = def.requiredFamily,
                requiredSeason = def.requiredSeason,
                minFamilyCount = def.minFamilyCount,
                minHappiness = def.minHappiness,
            };

            if (isNew) { AssetDatabase.CreateAsset(skill, path); created++; }
            else { EditorUtility.SetDirty(skill); updated++; }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int wired = AssignPassiveSkills();
        int retargeted = AssignActiveSkills();

        Debug.Log($"[AnimalSkillFactory] {created} skills created, {updated} updated in {SkillFolder}. " +
                  $"Passives wired onto {wired} animals, {retargeted} active skills reassigned.");
    }

    /// <summary>
    /// Gives each family an active skill that matches what the animal actually is.
    ///
    /// The pre-existing assignments were thematically arbitrary — the rabbit used "Feather
    /// Shield", and every cow used "Draining Howl". Since the new skills are gated by family
    /// and class anyway, an animal holding a skill it can never satisfy the condition for would
    /// simply never use it.
    ///
    /// Only reassigns animals whose family is handled here; anything else keeps what it has.
    /// </summary>
    private static int AssignActiveSkills()
    {
        var byFamily = new Dictionary<string, string>
        {
            ["Bovidae"]     = "HoofKick",       // heavy hooves: damage + stun
            ["Leporidae"]   = "Burrow",         // rabbit: dives underground
            ["Passeridae"]  = "FlockCall",      // sparrow: rallies the team
            ["Anatidae"]    = "ToxicQuack",     // duck keeps its existing, thematically right skill
            ["Galliformes"] = "PrecisePeck",    // chickens: cheap, always-ready jab
        };

        var animals = Resources.LoadAll<AnimalData>("Animals");
        int changed = 0;

        foreach (var animal in animals)
        {
            if (animal == null) continue;

            // The five egg_* entries are unhatched eggs, not fighters. They are the only
            // animals that shipped with no active skill, which reads as deliberate — leave
            // them without one rather than handing an egg a combat move.
            if (animal.name.StartsWith("egg_")) continue;

            if (!byFamily.TryGetValue(animal.animalFamily, out string skillName)) continue;

            // Support units keep a healing role regardless of family — there is exactly one
            // Support animal in the roster and it is the only source of healing in the game.
            if (animal.combatClass == "Support") skillName = "RestoringSong";

            var skill = AssetDatabase.LoadAssetAtPath<AnimalSkill>($"{SkillFolder}/{skillName}.asset");
            if (skill == null) continue;

            if (animal.activeSkill == skill) continue;

            animal.activeSkill = skill;
            EditorUtility.SetDirty(animal);
            changed++;
        }

        AssetDatabase.SaveAssets();
        return changed;
    }

    /// <summary>
    /// Fills in availablePassiveSkills on every AnimalData that qualifies.
    ///
    /// This was empty on 26 of the 28 animals, so the passive skills only ever reached Sparrow
    /// and chicken — every other animal evaluated an empty list and got nothing, no matter how
    /// happy it was or what season it was. The unlock conditions still gate them at runtime via
    /// AnimalSkill.CanUnlock; this only decides which passives an animal is allowed to check.
    ///
    /// Happiness and season passives go to everyone: they are the hooks that tie farm care and
    /// the calendar to combat, and gating them by species as well would make them near-invisible.
    /// Family passives go only to the matching family.
    /// </summary>
    private static int AssignPassiveSkills()
    {
        // Available to every animal — the condition itself is the gate.
        var universal = new[] { "HerdBond", "LoyalCompanion", "WinterCoat", "SpringVigor" };

        var animals = Resources.LoadAll<AnimalData>("Animals");
        int wired = 0;

        foreach (var animal in animals)
        {
            if (animal == null) continue;

            // Eggs are not fighters — see AssignActiveSkills.
            if (animal.name.StartsWith("egg_")) continue;

            var passives = new List<AnimalSkill>();

            foreach (string name in universal)
            {
                var s = AssetDatabase.LoadAssetAtPath<AnimalSkill>($"{SkillFolder}/{name}.asset");
                if (s != null) passives.Add(s);
            }

            // Family-specific passives.
            if (animal.animalFamily == "Galliformes")
            {
                foreach (string name in new[] { "FlockInstinct", "LargeBrood" })
                {
                    var s = AssetDatabase.LoadAssetAtPath<AnimalSkill>($"{SkillFolder}/{name}.asset");
                    if (s != null) passives.Add(s);
                }
            }

            // Class-specific passive that already existed but reached almost nobody.
            if (animal.combatClass == "Support")
            {
                var s = AssetDatabase.LoadAssetAtPath<AnimalSkill>($"{SkillFolder}/SupportersBlessing.asset");
                if (s != null) passives.Add(s);
            }

            animal.availablePassiveSkills = passives;
            EditorUtility.SetDirty(animal);
            wired++;
        }

        AssetDatabase.SaveAssets();
        return wired;
    }
}
} // namespace SowurShield.Editor
