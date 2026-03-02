using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using SowurShield.Combat;

namespace SowurShield.Editor
{

/// <summary>
/// Editor window for mass-producing Stage assets.
/// Accessible via Window > Combat > Stage Generator
/// </summary>
public class StageGeneratorWindow : EditorWindow
{
    // ============================================================================
    // WINDOW STATE
    // ============================================================================

    private enum GenerationMode
    {
        SingleStage,
        Progression,
        WorldMap
    }

    private GenerationMode generationMode = GenerationMode.Progression;
    private Vector2 scrollPosition;

    // Single stage settings
    private string stageName = "New Stage";
    private int stageNumber = 1;
    private StageTheme stageTheme = StageTheme.Forest;
    private int stageDifficulty = 1;

    // Enemy spawn configuration (SingleStage mode only)
    private List<EnemySpawnConfig> enemySpawnConfigs = new List<EnemySpawnConfig>();

    private class EnemySpawnConfig
    {
        public EnemyData enemy;
        public int minCount = 1;
        public int maxCount = 3;
        public float spawnWeight = 1f;
    }

    // Progression settings
    private int progressionCount = 5;
    private StageTheme progressionTheme = StageTheme.Forest;
    private int startDifficulty = 1;
    private int difficultyIncrement = 1;

    // World map settings
    private int stagesPerTheme = 5;
    private List<StageTheme> worldMapThemes = new List<StageTheme>
    {
        StageTheme.Meadow,
        StageTheme.Forest,
        StageTheme.Cave,
        StageTheme.Mountain,
        StageTheme.Volcano
    };

    // Output settings
    private string outputFolder = "Assets/Resources/Stages";

    // Preview
    private List<StageData> previewStages = new List<StageData>();

    // ============================================================================
    // MENU ITEM
    // ============================================================================

    [MenuItem("Window/Combat/Stage Generator")]
    public static void ShowWindow()
    {
        StageGeneratorWindow window = GetWindow<StageGeneratorWindow>("Stage Generator");
        window.minSize = new Vector2(400, 500);
    }

    // ============================================================================
    // GUI
    // ============================================================================

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        EditorGUILayout.Space(10);

        DrawModeSelector();
        EditorGUILayout.Space(10);

        DrawModeSettings();
        EditorGUILayout.Space(10);

        DrawOutputSettings();
        EditorGUILayout.Space(10);

        DrawActions();
        EditorGUILayout.Space(10);

        DrawPreview();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Stage Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Mass-produce Stage assets for your combat system. " +
            "Choose a generation mode and configure the settings below.",
            MessageType.Info);
    }

    private void DrawModeSelector()
    {
        EditorGUILayout.LabelField("Generation Mode", EditorStyles.boldLabel);
        generationMode = (GenerationMode)EditorGUILayout.EnumPopup("Mode", generationMode);
    }

    private void DrawModeSettings()
    {
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

        switch (generationMode)
        {
            case GenerationMode.SingleStage:
                DrawSingleStageSettings();
                break;
            case GenerationMode.Progression:
                DrawProgressionSettings();
                break;
            case GenerationMode.WorldMap:
                DrawWorldMapSettings();
                break;
        }
    }

    private void DrawSingleStageSettings()
    {
        stageName = EditorGUILayout.TextField("Stage Name", stageName);
        stageNumber = EditorGUILayout.IntField("Stage Number", stageNumber);
        stageTheme = (StageTheme)EditorGUILayout.EnumPopup("Theme", stageTheme);
        stageDifficulty = EditorGUILayout.IntSlider("Difficulty", stageDifficulty, 1, 10);

        EditorGUILayout.Space(6);
        DrawEnemySpawns();
    }

    private void DrawEnemySpawns()
    {
        EditorGUILayout.LabelField("Enemy Spawns", EditorStyles.boldLabel);

        for (int i = 0; i < enemySpawnConfigs.Count; i++)
        {
            EnemySpawnConfig config = enemySpawnConfigs[i];

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Slot {i}", GUILayout.Width(45));
            config.enemy = (EnemyData)EditorGUILayout.ObjectField(config.enemy, typeof(EnemyData), false);
            if (GUILayout.Button("✕", GUILayout.Width(22)))
            {
                enemySpawnConfigs.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                i--;
                continue;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            config.minCount = EditorGUILayout.IntSlider("Min Count", config.minCount, 1, 10);
            config.maxCount = EditorGUILayout.IntSlider("Max Count", Mathf.Max(config.minCount, config.maxCount), config.minCount, 10);
            config.spawnWeight = EditorGUILayout.Slider("Weight", config.spawnWeight, 0.1f, 10f);
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Enemy"))
        {
            enemySpawnConfigs.Add(new EnemySpawnConfig());
        }
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (enemySpawnConfigs.Count > 0 && GUILayout.Button("Clear All", GUILayout.Width(80)))
        {
            enemySpawnConfigs.Clear();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (enemySpawnConfigs.Count > 0)
        {
            int totalMin = enemySpawnConfigs.Sum(c => c.minCount);
            int totalMax = enemySpawnConfigs.Sum(c => c.maxCount);
            EditorGUILayout.HelpBox($"Total Enemies: Min {totalMin} / Max {totalMax}", MessageType.None);
        }
    }

    private void DrawProgressionSettings()
    {
        progressionCount = EditorGUILayout.IntSlider("Number of Stages", progressionCount, 1, 20);
        progressionTheme = (StageTheme)EditorGUILayout.EnumPopup("Theme", progressionTheme);
        startDifficulty = EditorGUILayout.IntSlider("Starting Difficulty", startDifficulty, 1, 10);
        difficultyIncrement = EditorGUILayout.IntSlider("Difficulty Increment", difficultyIncrement, 0, 2);
    }

    private void DrawWorldMapSettings()
    {
        stagesPerTheme = EditorGUILayout.IntSlider("Stages Per Theme", stagesPerTheme, 1, 10);

        EditorGUILayout.LabelField("Themes (in order):");
        EditorGUI.indentLevel++;

        for (int i = 0; i < worldMapThemes.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            worldMapThemes[i] = (StageTheme)EditorGUILayout.EnumPopup($"Region {i + 1}", worldMapThemes[i]);

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                worldMapThemes.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.indentLevel--;

        if (GUILayout.Button("Add Theme"))
        {
            worldMapThemes.Add(StageTheme.Forest);
        }

        int totalStages = stagesPerTheme * worldMapThemes.Count;
        EditorGUILayout.HelpBox($"This will generate {totalStages} stages total.", MessageType.None);
    }

    private void DrawOutputSettings()
    {
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selected))
            {
                // Convert to relative path
                if (selected.StartsWith(Application.dataPath))
                {
                    outputFolder = "Assets" + selected.Substring(Application.dataPath.Length);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawActions()
    {
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Preview", GUILayout.Height(30)))
        {
            GeneratePreview();
        }

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Generate & Save", GUILayout.Height(30)))
        {
            GenerateAndSave();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPreview()
    {
        if (previewStages.Count == 0) return;

        EditorGUILayout.LabelField($"Preview ({previewStages.Count} stages)", EditorStyles.boldLabel);

        EditorGUI.indentLevel++;
        foreach (StageData stage in previewStages)
        {
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField($"#{stage.stageNumber}", GUILayout.Width(40));
            EditorGUILayout.LabelField(stage.stageName, GUILayout.Width(150));
            EditorGUILayout.LabelField(stage.theme.ToString(), GUILayout.Width(80));
            EditorGUILayout.LabelField($"Diff: {stage.difficulty}", GUILayout.Width(60));
            string spawnSummary = stage.enemySpawns != null && stage.enemySpawns.Count > 0
                ? $"{stage.enemySpawns.Count} enemy type(s)"
                : "No enemies";
            EditorGUILayout.LabelField(spawnSummary, GUILayout.Width(110));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel--;
    }

    // ============================================================================
    // GENERATION
    // ============================================================================

    private void GeneratePreview()
    {
        // Clean up previous preview
        foreach (StageData stage in previewStages)
        {
            if (stage != null)
            {
                DestroyImmediate(stage);
            }
        }
        previewStages.Clear();

        // Generate based on mode
        switch (generationMode)
        {
            case GenerationMode.SingleStage:
                var spawns = enemySpawnConfigs
                    .Where(c => c.enemy != null)
                    .Select(c => new EnemySpawn
                    {
                        enemy = c.enemy,
                        minCount = c.minCount,
                        maxCount = c.maxCount,
                        spawnWeight = c.spawnWeight
                    }).ToList();
                previewStages.Add(StageFactory.CreateStage(stageName, stageNumber, stageTheme, stageDifficulty, spawns));
                break;

            case GenerationMode.Progression:
                previewStages = StageFactory.CreateStageProgression(
                    progressionCount, progressionTheme, startDifficulty, difficultyIncrement);
                break;

            case GenerationMode.WorldMap:
                previewStages = StageFactory.CreateWorldMap(stagesPerTheme, worldMapThemes.ToArray());
                break;
        }

        Debug.Log($"[StageGenerator] Generated {previewStages.Count} stage previews");
    }

    private void GenerateAndSave()
    {
        // Generate if not previewed
        if (previewStages.Count == 0)
        {
            GeneratePreview();
        }

        if (previewStages.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No stages to save!", "OK");
            return;
        }

        // Confirm
        bool confirm = EditorUtility.DisplayDialog(
            "Confirm Generation",
            $"This will create {previewStages.Count} stage assets in:\n{outputFolder}\n\nContinue?",
            "Generate", "Cancel");

        if (!confirm) return;

        // Save all stages
        StageFactory.SaveStagesAsAssets(previewStages, outputFolder);

        EditorUtility.DisplayDialog(
            "Success",
            $"Generated {previewStages.Count} stage assets!",
            "OK");

        // Clear preview (assets are now saved)
        previewStages.Clear();
    }
}

} // namespace SowurShield.Editor
