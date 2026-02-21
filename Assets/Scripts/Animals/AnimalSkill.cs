using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Unlock condition types for animal skills
/// </summary>
public enum SkillUnlockConditionType
{
    None,                   // Always available
    CombatClass,           // Requires specific combat class
    AnimalFamily,          // Requires specific animal family
    FamilyCount,           // Requires X animals of same family in roster
    HappinessThreshold,    // Requires minimum happiness level
    Season,                // Only active in specific season
    Combined               // Multiple conditions must be met
}

/// <summary>
/// Unlock condition for animal skills
/// </summary>
[System.Serializable]
public class SkillUnlockCondition
{
    public SkillUnlockConditionType conditionType = SkillUnlockConditionType.None;

    [Header("Class Condition")]
    [Tooltip("Required combat class (Tank, DPS, Support, Utility)")]
    public string requiredClass = "";

    [Header("Family Condition")]
    [Tooltip("Required animal family (e.g., Galliformes, Bovidae)")]
    public string requiredFamily = "";

    [Header("Family Count Condition")]
    [Tooltip("Minimum number of same family animals in roster")]
    public int minFamilyCount = 0;

    [Header("Happiness Condition")]
    [Tooltip("Minimum happiness level (0-100)")]
    [Range(0f, 100f)]
    public float minHappiness = 0f;

    [Header("Season Condition")]
    [Tooltip("Required season (Spring, Summer, Fall, Winter)")]
    public string requiredSeason = "";

    [Header("Combined Conditions")]
    [Tooltip("Multiple conditions that must all be met")]
    public List<SkillUnlockCondition> combinedConditions = new List<SkillUnlockCondition>();

    /// <summary>
    /// Check if this condition is met
    /// </summary>
    public bool IsMet(Animal animal, int familyCountInRoster, string currentSeason)
    {
        if (animal == null || animal.AnimalData == null) return false;

        switch (conditionType)
        {
            case SkillUnlockConditionType.None:
                return true;

            case SkillUnlockConditionType.CombatClass:
                return animal.AnimalData.combatClass == requiredClass;

            case SkillUnlockConditionType.AnimalFamily:
                return animal.AnimalData.animalFamily == requiredFamily;

            case SkillUnlockConditionType.FamilyCount:
                return familyCountInRoster >= minFamilyCount;

            case SkillUnlockConditionType.HappinessThreshold:
                return animal.GetHappiness() >= minHappiness;

            case SkillUnlockConditionType.Season:
                return currentSeason == requiredSeason;

            case SkillUnlockConditionType.Combined:
                foreach (var condition in combinedConditions)
                {
                    if (!condition.IsMet(animal, familyCountInRoster, currentSeason))
                    {
                        return false;
                    }
                }
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Get a description of this condition
    /// </summary>
    public string GetDescription()
    {
        switch (conditionType)
        {
            case SkillUnlockConditionType.None:
                return "Always available";

            case SkillUnlockConditionType.CombatClass:
                return $"Requires {requiredClass} class";

            case SkillUnlockConditionType.AnimalFamily:
                return $"Requires {requiredFamily} family";

            case SkillUnlockConditionType.FamilyCount:
                return $"Requires {minFamilyCount}+ {requiredFamily} in roster";

            case SkillUnlockConditionType.HappinessThreshold:
                return $"Requires {minHappiness:F0}+ happiness";

            case SkillUnlockConditionType.Season:
                return $"Active in {requiredSeason}";

            case SkillUnlockConditionType.Combined:
                string desc = "Requires: ";
                for (int i = 0; i < combinedConditions.Count; i++)
                {
                    desc += combinedConditions[i].GetDescription();
                    if (i < combinedConditions.Count - 1)
                        desc += " AND ";
                }
                return desc;

            default:
                return "Unknown condition";
        }
    }
}

/// <summary>
/// Defines an animal skill (active or passive)
/// ScriptableObject for data-driven skill creation
/// </summary>
[CreateAssetMenu(fileName = "NewAnimalSkill", menuName = "Animals/Animal Skill")]
public class AnimalSkill : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Unique skill identifier")]
    public string skillId = "";

    [Tooltip("Display name of the skill")]
    public string skillName = "";

    [Tooltip("Skill type: Active or Passive")]
    public SkillType skillType = SkillType.Passive;

    [Tooltip("Skill icon")]
    public Sprite skillIcon;

    [TextArea(3, 5)]
    [Tooltip("Description of what the skill does")]
    public string description = "";

    [Header("Unlock Condition")]
    [Tooltip("Condition that must be met to unlock/use this skill")]
    public SkillUnlockCondition unlockCondition = new SkillUnlockCondition();

    [Header("Skill Effects (For Future Combat Implementation)")]
    [Tooltip("Damage multiplier (for active skills)")]
    public float damageMultiplier = 1.0f;

    [Tooltip("Defense multiplier (for passive skills)")]
    public float defenseMultiplier = 1.0f;

    [Tooltip("Speed multiplier (for passive skills)")]
    public float speedMultiplier = 1.0f;

    [Tooltip("Attack multiplier (for passive skills)")]
    public float attackMultiplier = 1.0f;

    [Tooltip("Heal amount (for active skills)")]
    public float healAmount = 0f;

    [Tooltip("Applies to allies?")]
    public bool affectsAllies = false;

    [Tooltip("Applies to self?")]
    public bool affectsSelf = true;

    [Tooltip("Cooldown in turns (for active skills)")]
    public int cooldownTurns = 0;

    /// <summary>
    /// Check if this skill can be unlocked for the given animal
    /// </summary>
    public bool CanUnlock(Animal animal, int familyCountInRoster, string currentSeason)
    {
        return unlockCondition.IsMet(animal, familyCountInRoster, currentSeason);
    }

    /// <summary>
    /// Get full skill info as formatted string
    /// </summary>
    public string GetFullInfo()
    {
        string info = $"<b>{skillName}</b> ({skillType})\n";
        info += $"{description}\n\n";
        info += $"Unlock: {unlockCondition.GetDescription()}";
        return info;
    }
}

/// <summary>
/// Skill type enumeration
/// </summary>
public enum SkillType
{
    Active,
    Passive
}
