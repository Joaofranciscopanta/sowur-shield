using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization;
using SowurShield.Animals;
using SowurShield.Core;

namespace SowurShield.Combat
{

/// <summary>
/// Enemy type classification for combat behavior
/// </summary>
public enum EnemyType
{
    Normal,     // Standard enemy
    Elite,      // Stronger variant with bonus stats
    Miniboss,   // Mini-boss with special abilities
    Boss        // Full boss with phases
}

/// <summary>
/// ScriptableObject that defines an enemy type.
/// Create instances via Assets > Create > Combat > Enemy Data
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Combat/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Internal name — used for GameObject naming/logs only. Wire displayName for the player-facing name.")]
    public string enemyName = "New Enemy";
    [Tooltip("Player-facing name shown in the combat HUD")]
    public LocalizedString displayName; // table "Combat", key "enemy.<enemyName>.name"

    [Tooltip("Enemy type classification")]
    public EnemyType enemyType = EnemyType.Normal;

    [Tooltip("Brief description")]
    [TextArea(2, 3)]
    public string description = "";

    [Header("Visuals")]
    [Tooltip("Enemy sprite")]
    public Sprite sprite;

    [Tooltip("Animator controller for enemy")]
    public RuntimeAnimatorController animatorController;

    [Tooltip("Scale multiplier for sprite")]
    public float spriteScale = 1f;

    [Header("Base Combat Stats")]
    [Tooltip("Base health points")]
    public int baseHealth = 100;

    [Tooltip("Base attack power")]
    public int baseAttack = 10;

    [Tooltip("Base defense")]
    public int baseDefense = 5;

    [Tooltip("Base speed (determines turn order)")]
    public int baseSpeed = 10;

    [Header("Stat Scaling")]
    [Tooltip("Health multiplier per difficulty level")]
    public float healthScaling = 1.15f;

    [Tooltip("Attack multiplier per difficulty level")]
    public float attackScaling = 1.1f;

    [Tooltip("Defense multiplier per difficulty level")]
    public float defenseScaling = 1.08f;

    [Header("Behavior")]
    [Tooltip("AI behavior pattern")]
    public string aiBehavior = "Aggressive"; // Aggressive, Defensive, Support, Random

    [Tooltip("Target priority")]
    public string targetPriority = "Weakest"; // Weakest, Strongest, Random, LowestHP

    [Header("Skills")]
    [Tooltip("Available skills this enemy can use")]
    public List<AnimalSkill> skills = new List<AnimalSkill>();

    [Tooltip("Chance to use skill instead of basic attack")]
    [Range(0f, 1f)]
    public float skillUseChance = 0.3f;

    [Tooltip("Base accuracy (chance for basic attacks/skills to hit) before difficulty scaling")]
    [Range(0f, 1f)]
    public float baseAccuracy = 1.0f;

    [Tooltip("Melee units can only target the opposing front column (unless empty); Ranged units can target any column.")]
    public AttackRange attackRange = AttackRange.Ranged;

    [Header("Drops")]
    [Tooltip("Experience points granted on defeat")]
    public int experienceReward = 20;

    [Tooltip("Gold dropped on defeat")]
    public int goldDrop = 10;

    [Tooltip("Specific item drops from this enemy")]
    public List<LootDrop> enemyLoot = new List<LootDrop>();

    [Header("Special Properties")]
    [Tooltip("Elemental affinity (for damage calculations)")]
    public string element = "None";

    [Tooltip("Status immunities")]
    public List<string> immunities = new List<string>();

    [Tooltip("Weaknesses (takes extra damage)")]
    public List<string> weaknesses = new List<string>();

    /// <summary>
    /// Calculate scaled stats for a given difficulty level
    /// </summary>
    public (int health, int attack, int defense, int speed) GetScaledStats(int difficultyLevel)
    {
        float difficultyMod = Mathf.Pow(1.1f, difficultyLevel - 1);

        int scaledHealth = Mathf.RoundToInt(baseHealth * Mathf.Pow(healthScaling, difficultyLevel - 1) * difficultyMod);
        int scaledAttack = Mathf.RoundToInt(baseAttack * Mathf.Pow(attackScaling, difficultyLevel - 1) * difficultyMod);
        int scaledDefense = Mathf.RoundToInt(baseDefense * Mathf.Pow(defenseScaling, difficultyLevel - 1) * difficultyMod);
        int scaledSpeed = baseSpeed; // Speed typically doesn't scale

        // Apply enemy type bonuses
        switch (enemyType)
        {
            case EnemyType.Elite:
                scaledHealth = Mathf.RoundToInt(scaledHealth * 1.5f);
                scaledAttack = Mathf.RoundToInt(scaledAttack * 1.3f);
                break;
            case EnemyType.Miniboss:
                scaledHealth = Mathf.RoundToInt(scaledHealth * 2.5f);
                scaledAttack = Mathf.RoundToInt(scaledAttack * 1.5f);
                scaledDefense = Mathf.RoundToInt(scaledDefense * 1.3f);
                break;
            case EnemyType.Boss:
                scaledHealth = Mathf.RoundToInt(scaledHealth * 5f);
                scaledAttack = Mathf.RoundToInt(scaledAttack * 2f);
                scaledDefense = Mathf.RoundToInt(scaledDefense * 1.5f);
                break;
        }

        return (scaledHealth, scaledAttack, scaledDefense, scaledSpeed);
    }

    /// <summary>
    /// Accuracy scales slightly with difficulty, capped at 95% so enemies always miss sometimes.
    /// </summary>
    public float GetScaledAccuracy(int difficultyLevel)
    {
        float scaled = baseAccuracy + (difficultyLevel - 1) * 0.02f;
        return Mathf.Min(scaled, 0.95f);
    }

    /// <summary>
    /// Chance to use a skill instead of a basic attack increases with difficulty, capped at 60%.
    /// </summary>
    public float GetScaledSkillUseChance(int difficultyLevel)
    {
        float scaled = skillUseChance + (difficultyLevel - 1) * 0.05f;
        return Mathf.Min(scaled, 0.6f);
    }

    /// <summary>
    /// Calculate scaled rewards for difficulty
    /// </summary>
    public (int exp, int gold) GetScaledRewards(int difficultyLevel)
    {
        float difficultyMod = 1f + (difficultyLevel - 1) * 0.25f;

        int scaledExp = Mathf.RoundToInt(experienceReward * difficultyMod);
        int scaledGold = Mathf.RoundToInt(goldDrop * difficultyMod);

        // Bonus for special enemy types
        switch (enemyType)
        {
            case EnemyType.Elite:
                scaledExp = Mathf.RoundToInt(scaledExp * 1.5f);
                scaledGold = Mathf.RoundToInt(scaledGold * 1.5f);
                break;
            case EnemyType.Miniboss:
                scaledExp = Mathf.RoundToInt(scaledExp * 3f);
                scaledGold = Mathf.RoundToInt(scaledGold * 2.5f);
                break;
            case EnemyType.Boss:
                scaledExp = Mathf.RoundToInt(scaledExp * 5f);
                scaledGold = Mathf.RoundToInt(scaledGold * 4f);
                break;
        }

        return (scaledExp, scaledGold);
    }

    // Player-facing name. Falls back to the internal enemyName if displayName hasn't been wired
    // to a table entry yet, so older EnemyData assets degrade gracefully instead of a blank label.
    public string GetDisplayName()
    {
        string localized = displayName.SafeGetLocalizedString();
        return string.IsNullOrEmpty(localized) ? enemyName : localized;
    }
}

} // namespace SowurShield.Combat
