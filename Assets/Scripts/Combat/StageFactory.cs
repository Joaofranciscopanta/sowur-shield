using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SowurShield.Combat
{

/// <summary>
/// Factory class for mass-producing Stage data structures.
/// Provides both runtime and editor-time stage generation.
///
/// USAGE:
/// - Runtime: StageFactory.CreateStage(...) returns a new StageData instance
/// - Editor: Use Window > Combat > Stage Generator for batch creation
/// </summary>
public static class StageFactory
{
    // ============================================================================
    // CONFIGURATION CONSTANTS
    // ============================================================================

    private static readonly Dictionary<StageTheme, string[]> ThemeNames = new Dictionary<StageTheme, string[]>
    {
        { StageTheme.Forest, new[] { "Whispering Woods", "Dark Grove", "Ancient Forest", "Twilight Glade", "Mossy Hollow" } },
        { StageTheme.Desert, new[] { "Scorching Sands", "Dune Sea", "Oasis Ruins", "Sunbaked Wastes", "Mirage Valley" } },
        { StageTheme.Mountain, new[] { "Rocky Peak", "Frozen Summit", "Eagle's Nest", "Stone Giant Pass", "Cloud Ridge" } },
        { StageTheme.Swamp, new[] { "Murky Marsh", "Fogbound Fen", "Poisoned Wetlands", "Bog of Despair", "Croaking Hollow" } },
        { StageTheme.Cave, new[] { "Crystal Cavern", "Shadow Depths", "Echo Chamber", "Underground Lake", "Stalactite Hall" } },
        { StageTheme.Ruins, new[] { "Forgotten Temple", "Crumbling Castle", "Ancient Arena", "Overgrown Shrine", "Lost City" } },
        { StageTheme.Beach, new[] { "Stormy Shore", "Pirate Cove", "Coral Beach", "Tidal Flats", "Shipwreck Bay" } },
        { StageTheme.Volcano, new[] { "Magma Core", "Ashen Caldera", "Lava Fields", "Obsidian Peak", "Fire Basin" } },
        { StageTheme.Tundra, new[] { "Frozen Wastes", "Blizzard Plains", "Ice Cavern", "Permafrost Field", "Glacier Pass" } },
        { StageTheme.Meadow, new[] { "Sunny Fields", "Flower Garden", "Rolling Hills", "Peaceful Pasture", "Wind Prairie" } }
    };

    private static readonly Dictionary<StageTheme, Color> ThemeColors = new Dictionary<StageTheme, Color>
    {
        { StageTheme.Forest, new Color(0.2f, 0.5f, 0.2f) },
        { StageTheme.Desert, new Color(0.9f, 0.8f, 0.5f) },
        { StageTheme.Mountain, new Color(0.6f, 0.6f, 0.7f) },
        { StageTheme.Swamp, new Color(0.3f, 0.4f, 0.2f) },
        { StageTheme.Cave, new Color(0.2f, 0.2f, 0.3f) },
        { StageTheme.Ruins, new Color(0.5f, 0.4f, 0.3f) },
        { StageTheme.Beach, new Color(0.4f, 0.7f, 0.9f) },
        { StageTheme.Volcano, new Color(0.8f, 0.3f, 0.1f) },
        { StageTheme.Tundra, new Color(0.8f, 0.9f, 1f) },
        { StageTheme.Meadow, new Color(0.6f, 0.8f, 0.4f) }
    };

    private static readonly string[] WeatherEffects = { "None", "Rain", "Fog", "Sandstorm", "Snow", "Wind", "Heat" };
    private static readonly string[] TimesOfDay = { "Dawn", "Day", "Dusk", "Night" };

    // ============================================================================
    // SINGLE STAGE CREATION
    // ============================================================================

    /// <summary>
    /// Create a single stage with specified parameters
    /// </summary>
    public static StageData CreateStage(
        string name,
        int stageNumber,
        StageTheme theme,
        int difficulty,
        List<EnemySpawn> enemies = null,
        List<LootDrop> loot = null)
    {
        StageData stage = ScriptableObject.CreateInstance<StageData>();

        stage.stageName = name;
        stage.displayName = new LocalizedString("Combat", $"stage.{name}.name");
        stage.stageNumber = stageNumber;
        stage.theme = theme;
        stage.difficulty = Mathf.Clamp(difficulty, 1, 10);
        stage.ambientColor = ThemeColors.ContainsKey(theme) ? ThemeColors[theme] : Color.white;

        // Calculate derived values
        stage.recommendedTeamSize = Mathf.Clamp(1 + difficulty / 3, 1, 6);
        stage.enemyStatMultiplier = 1f + (difficulty - 1) * 0.15f;
        stage.baseGoldReward = 50 + (difficulty * 25) + (stageNumber * 10);
        stage.baseExperienceReward = 25 + (difficulty * 15) + (stageNumber * 5);
        stage.minTotalEnemies = Mathf.Clamp(1 + difficulty / 4, 1, 3);
        stage.maxTotalEnemies = Mathf.Clamp(2 + difficulty / 2, 2, 6);
        stage.minimumPlayerLevel = Mathf.Max(1, (difficulty - 1) * 2);

        // Random weather and time
        stage.weatherEffect = GetThemeAppropriateWeather(theme);
        stage.timeOfDay = TimesOfDay[Random.Range(0, TimesOfDay.Length)];

        // Generate description — wires the table reference only; LocalizedString can't carry a
        // literal string directly, so the generated EN text below still needs to be added to the
        // String Table by hand (e.g. via the CSV importer) under this same key.
        stage.description = new LocalizedString("Combat", $"stage.{name}.description");
        Debug.Log($"[StageFactory] Add this to the Combat table: stage.{name}.description = \"{GenerateDescription(name, theme, difficulty)}\"");

        // Assign enemies and loot if provided
        if (enemies != null)
        {
            stage.enemySpawns = new List<EnemySpawn>(enemies);
        }

        if (loot != null)
        {
            stage.lootTable = new List<LootDrop>(loot);
        }

        return stage;
    }

    /// <summary>
    /// Create a stage with auto-generated name based on theme
    /// </summary>
    public static StageData CreateStage(int stageNumber, StageTheme theme, int difficulty)
    {
        string name = GenerateStageName(theme, stageNumber);
        return CreateStage(name, stageNumber, theme, difficulty);
    }

    // ============================================================================
    // BATCH STAGE CREATION
    // ============================================================================

    /// <summary>
    /// Create multiple stages with progressive difficulty
    /// </summary>
    public static List<StageData> CreateStageProgression(
        int count,
        StageTheme theme,
        int startingDifficulty = 1,
        int difficultyIncrement = 1)
    {
        List<StageData> stages = new List<StageData>();

        for (int i = 0; i < count; i++)
        {
            int stageNumber = i + 1;
            int difficulty = Mathf.Clamp(startingDifficulty + (i * difficultyIncrement), 1, 10);

            StageData stage = CreateStage(stageNumber, theme, difficulty);

            // Link prerequisite (except first stage)
            if (i > 0)
            {
                stage.prerequisiteStage = stages[i - 1];
            }
            else
            {
                stage.unlockedByDefault = true;
            }

            stages.Add(stage);
        }

        return stages;
    }

    /// <summary>
    /// Create a full world map with multiple themed regions
    /// </summary>
    public static List<StageData> CreateWorldMap(int stagesPerTheme = 5, StageTheme[] themes = null)
    {
        if (themes == null || themes.Length == 0)
        {
            themes = new[] { StageTheme.Meadow, StageTheme.Forest, StageTheme.Cave, StageTheme.Mountain, StageTheme.Volcano };
        }

        List<StageData> allStages = new List<StageData>();
        int globalStageNumber = 1;

        for (int themeIndex = 0; themeIndex < themes.Length; themeIndex++)
        {
            StageTheme theme = themes[themeIndex];
            int baseDifficulty = 1 + (themeIndex * 2); // Each region is harder

            for (int stageInTheme = 0; stageInTheme < stagesPerTheme; stageInTheme++)
            {
                int difficulty = Mathf.Clamp(baseDifficulty + stageInTheme, 1, 10);
                StageData stage = CreateStage(globalStageNumber, theme, difficulty);

                // Link to previous stage
                if (allStages.Count > 0)
                {
                    stage.prerequisiteStage = allStages[allStages.Count - 1];
                }
                else
                {
                    stage.unlockedByDefault = true;
                }

                // Mark last stage of each theme as having a boss
                if (stageInTheme == stagesPerTheme - 1)
                {
                    Debug.Log($"[StageFactory] Boss stage '{stage.stageName}' — prefix its Combat table description entry with \"[BOSS] \" by hand.");
                }

                allStages.Add(stage);
                globalStageNumber++;
            }
        }

        return allStages;
    }

    /// <summary>
    /// Create stages from a configuration array
    /// </summary>
    public static List<StageData> CreateFromConfig(StageConfig[] configs)
    {
        List<StageData> stages = new List<StageData>();

        foreach (StageConfig config in configs)
        {
            StageData stage = CreateStage(
                config.name,
                config.number,
                config.theme,
                config.difficulty,
                config.enemies,
                config.loot
            );

            stages.Add(stage);
        }

        // Link prerequisites based on stage numbers
        for (int i = 0; i < stages.Count; i++)
        {
            if (i > 0)
            {
                stages[i].prerequisiteStage = stages[i - 1];
            }
            else
            {
                stages[i].unlockedByDefault = true;
            }
        }

        return stages;
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    private static string GenerateStageName(StageTheme theme, int stageNumber)
    {
        if (ThemeNames.ContainsKey(theme))
        {
            string[] names = ThemeNames[theme];
            int index = (stageNumber - 1) % names.Length;
            return $"{names[index]} {((stageNumber - 1) / names.Length) + 1}";
        }

        return $"{theme} Stage {stageNumber}";
    }

    private static string GenerateDescription(string name, StageTheme theme, int difficulty)
    {
        string difficultyDesc = difficulty switch
        {
            <= 2 => "A relatively safe area",
            <= 4 => "Moderate danger awaits",
            <= 6 => "A challenging battleground",
            <= 8 => "Only the brave should enter",
            _ => "Legendary warriors only"
        };

        string themeDesc = theme switch
        {
            StageTheme.Forest => "amidst ancient trees",
            StageTheme.Desert => "under the scorching sun",
            StageTheme.Mountain => "at dizzying heights",
            StageTheme.Swamp => "in the murky depths",
            StageTheme.Cave => "in the darkness below",
            StageTheme.Ruins => "among forgotten stones",
            StageTheme.Beach => "by the crashing waves",
            StageTheme.Volcano => "near molten rock",
            StageTheme.Tundra => "in the frozen wastes",
            StageTheme.Meadow => "in peaceful fields",
            _ => ""
        };

        return $"{difficultyDesc} {themeDesc}.";
    }

    private static string GetThemeAppropriateWeather(StageTheme theme)
    {
        return theme switch
        {
            StageTheme.Forest => Random.value > 0.7f ? "Rain" : "None",
            StageTheme.Desert => Random.value > 0.6f ? "Sandstorm" : "Heat",
            StageTheme.Mountain => Random.value > 0.5f ? "Wind" : "None",
            StageTheme.Swamp => "Fog",
            StageTheme.Cave => "None",
            StageTheme.Tundra => Random.value > 0.5f ? "Snow" : "Wind",
            StageTheme.Volcano => "Heat",
            _ => "None"
        };
    }

    // ============================================================================
    // EDITOR UTILITIES
    // ============================================================================

#if UNITY_EDITOR
    /// <summary>
    /// Save a stage as a ScriptableObject asset
    /// </summary>
    public static void SaveStageAsset(StageData stage, string folderPath)
    {
        if (stage == null) return;

        string safeName = stage.stageName.Replace(" ", "_").Replace("/", "_");
        string assetPath = $"{folderPath}/Stage_{stage.stageNumber:D3}_{safeName}.asset";

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string[] folders = folderPath.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }

        AssetDatabase.CreateAsset(stage, assetPath);
    }

    /// <summary>
    /// Save multiple stages as assets
    /// </summary>
    public static void SaveStagesAsAssets(List<StageData> stages, string folderPath)
    {
        foreach (StageData stage in stages)
        {
            SaveStageAsset(stage, folderPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
#endif
}

/// <summary>
/// Configuration struct for batch stage creation
/// </summary>
[System.Serializable]
public struct StageConfig
{
    public string name;
    public int number;
    public StageTheme theme;
    public int difficulty;
    public List<EnemySpawn> enemies;
    public List<LootDrop> loot;

    public StageConfig(string name, int number, StageTheme theme, int difficulty)
    {
        this.name = name;
        this.number = number;
        this.theme = theme;
        this.difficulty = difficulty;
        this.enemies = null;
        this.loot = null;
    }
}

} // namespace SowurShield.Combat
