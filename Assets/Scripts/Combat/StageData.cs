using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization;
using SowurShield.Inventory;
using SowurShield.Core;

namespace SowurShield.Combat
{

/// <summary>
/// Defines the theme/visual style of a stage
/// </summary>
public enum StageTheme
{
    Forest,
    Desert,
    Mountain,
    Swamp,
    Cave,
    Ruins,
    Beach,
    Volcano,
    Tundra,
    Meadow
}

/// <summary>
/// Represents a loot drop with quantity range and drop chance
/// </summary>
[System.Serializable]
public class LootDrop
{
    [Tooltip("Item that can drop")]
    public Item item;

    [Tooltip("Minimum quantity when dropped")]
    public int minQuantity = 1;

    [Tooltip("Maximum quantity when dropped")]
    public int maxQuantity = 1;

    [Tooltip("Chance to drop (0-1)")]
    [Range(0f, 1f)]
    public float dropChance = 0.5f;
}

/// <summary>
/// Represents an enemy spawn configuration for a stage
/// </summary>
[System.Serializable]
public class EnemySpawn
{
    [Tooltip("Enemy data reference")]
    public EnemyData enemy;

    [Tooltip("Minimum count of this enemy type")]
    public int minCount = 1;

    [Tooltip("Maximum count of this enemy type")]
    public int maxCount = 3;

    [Tooltip("Spawn weight (higher = more likely to appear)")]
    [Range(0.1f, 10f)]
    public float spawnWeight = 1f;
}

/// <summary>
/// ScriptableObject that defines a combat stage.
/// Create instances via Assets > Create > Combat > Stage Data
/// or use StageFactory for procedural generation.
/// </summary>
[CreateAssetMenu(fileName = "NewStage", menuName = "Combat/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Stable internal ID — used for save data (completed stages), quest/achievement matching, and lookups. Never shown to the player directly.")]
    public string stageName = "New Stage";

    [Tooltip("Player-facing name shown in the World Map / combat HUD")]
    public LocalizedString displayName; // table "Combat", key "stage.<stageName>.name"

    [Tooltip("Stage number in progression (1-based)")]
    public int stageNumber = 1;

    [Tooltip("Brief description shown to player")]
    public LocalizedString description; // table "Combat", key "stage.<stageName>.description"

    [Header("Theme & Visuals")]
    [Tooltip("Visual theme of this stage")]
    public StageTheme theme = StageTheme.Forest;

    [Tooltip("Background sprite for combat scene")]
    public Sprite backgroundSprite;

    [Tooltip("Optional ambient color tint")]
    public Color ambientColor = Color.white;

    [Header("Difficulty")]
    [Tooltip("Difficulty level (1-10)")]
    [Range(1, 10)]
    public int difficulty = 1;

    [Tooltip("Recommended player team size")]
    [Range(1, 6)]
    public int recommendedTeamSize = 3;

    [Tooltip("Enemy stat multiplier based on difficulty")]
    public float enemyStatMultiplier = 1f;

    [Header("Enemies")]
    [Tooltip("Possible enemy spawns for this stage")]
    public List<EnemySpawn> enemySpawns = new List<EnemySpawn>();

    [Tooltip("Minimum total enemies per battle")]
    public int minTotalEnemies = 1;

    [Tooltip("Maximum total enemies per battle")]
    public int maxTotalEnemies = 4;

    [Tooltip("Boss enemy (appears on final wave or as single boss fight)")]
    public EnemyData bossEnemy;

    [Header("Loot & Rewards")]
    [Tooltip("Possible loot drops from this stage")]
    public List<LootDrop> lootTable = new List<LootDrop>();

    [Tooltip("Base gold reward")]
    public int baseGoldReward = 100;

    [Tooltip("Gold reward variance (+/- percentage)")]
    [Range(0f, 0.5f)]
    public float goldVariance = 0.2f;

    [Tooltip("Base experience reward")]
    public int baseExperienceReward = 50;

    [Header("Unlock Requirements")]
    [Tooltip("Previous stage that must be completed to unlock this one")]
    public StageData prerequisiteStage;

    [Tooltip("Minimum player level required")]
    public int minimumPlayerLevel = 1;

    [Tooltip("Is this stage unlocked by default?")]
    public bool unlockedByDefault = false;

    [Header("Special Modifiers")]
    [Tooltip("Weather effect during battle (affects some animal types)")]
    public string weatherEffect = "None";

    [Tooltip("Time of day (affects some skills)")]
    public string timeOfDay = "Day";

    /// <summary>
    /// Calculate total gold reward with variance
    /// </summary>
    public int CalculateGoldReward()
    {
        float variance = Random.Range(-goldVariance, goldVariance);
        return Mathf.RoundToInt(baseGoldReward * (1f + variance));
    }

    /// <summary>
    /// Generate enemy count for this stage
    /// </summary>
    public int GenerateEnemyCount()
    {
        return Random.Range(minTotalEnemies, maxTotalEnemies + 1);
    }

    /// <summary>
    /// Roll for loot drops
    /// </summary>
    public List<(Item item, int quantity)> RollLoot()
    {
        List<(Item, int)> drops = new List<(Item, int)>();

        foreach (LootDrop loot in lootTable)
        {
            if (loot.item != null && Random.value <= loot.dropChance)
            {
                int quantity = Random.Range(loot.minQuantity, loot.maxQuantity + 1);
                drops.Add((loot.item, quantity));
            }
        }

        return drops;
    }

    /// <summary>
    /// Get display string for difficulty
    /// </summary>
    public string GetDifficultyString()
    {
        return difficulty switch
        {
            1 => "Very Easy",
            2 => "Easy",
            3 => "Normal",
            4 => "Moderate",
            5 => "Challenging",
            6 => "Hard",
            7 => "Very Hard",
            8 => "Expert",
            9 => "Master",
            10 => "Legendary",
            _ => "Unknown"
        };
    }

    // Player-facing name. Falls back to the internal stageName if displayName hasn't been wired
    // to a table entry yet, so older StageData assets degrade gracefully instead of a blank label.
    public string GetDisplayName()
    {
        string localized = displayName.SafeGetLocalizedString();
        return string.IsNullOrEmpty(localized) ? stageName : localized;
    }

    public string GetDisplayDescription() => description.SafeGetLocalizedString();
}

} // namespace SowurShield.Combat
