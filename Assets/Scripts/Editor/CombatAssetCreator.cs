using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using SowurShield.Combat;

namespace SowurShield.Editor
{

/// <summary>
/// One-click creator for all EnemyData and StageData assets.
/// Menu: Tools > Combat > Create All Combat Assets
/// </summary>
public class CombatAssetCreator : EditorWindow
{
    // ============================================================================
    // PATHS
    // ============================================================================

    private const string EnemyBasePath  = "Assets/Resources/Enemies";
    private const string StageBasePath  = "Assets/Resources/Stages";

    // ============================================================================
    // WINDOW
    // ============================================================================

    [MenuItem("Tools/Combat/Create All Combat Assets")]
    public static void ShowWindow()
    {
        GetWindow<CombatAssetCreator>("Combat Asset Creator").minSize = new Vector2(380, 280);
    }

    private void OnGUI()
    {
        GUILayout.Label("Combat Asset Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Creates all EnemyData and StageData assets.\n" +
            "• EnemyData → Assets/Resources/Enemies/<Region>/\n" +
            "• StageData → Assets/Resources/Stages/<Region>/\n\n" +
            "Safe to run multiple times — existing assets are skipped.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("Create ALL Assets (Enemies + Stages)", GUILayout.Height(40)))
        {
            bool ok = EditorUtility.DisplayDialog(
                "Create All Combat Assets",
                "This will create ~30 EnemyData assets and 25 StageData assets.\nContinue?",
                "Create", "Cancel");
            if (ok) RunAll();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(6);

        if (GUILayout.Button("Create EnemyData only", GUILayout.Height(28)))
            CreateAllEnemies();

        if (GUILayout.Button("Create StageData only (requires enemies)", GUILayout.Height(28)))
            CreateAllStages();
    }

    // ============================================================================
    // TOP-LEVEL RUNNERS
    // ============================================================================

    private void RunAll()
    {
        CreateAllEnemies();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        CreateAllStages();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", "All combat assets created successfully!", "OK");
    }

    // ============================================================================
    // ENEMY CREATION
    // ============================================================================

    private void CreateAllEnemies()
    {
        // ---- Meadow -------------------------------------------------------
        CreateEnemy("Meadow", "Slime",       EnemyType.Normal,  40,  8,  3,  6, 10,  5);
        CreateEnemy("Meadow", "GiantSlime",  EnemyType.Elite,   90, 12,  8,  5, 20, 12);
        CreateEnemy("Meadow", "FieldRat",    EnemyType.Normal,  35, 10,  2, 14, 12,  6);
        CreateEnemy("Meadow", "Crow",        EnemyType.Normal,  30, 12,  4, 16, 14,  7);
        CreateEnemy("Meadow", "HoneyBee",    EnemyType.Normal,  25, 14,  2, 18, 15,  8);
        CreateEnemy("Meadow", "MeadowWolf",  EnemyType.Boss,   200, 18, 15, 12, 80, 40);

        // ---- Forest -------------------------------------------------------
        CreateEnemy("Forest", "ForestBoar",  EnemyType.Normal,  70, 14, 12,  9, 22, 11);
        CreateEnemy("Forest", "PoisonFrog",  EnemyType.Normal,  45, 16,  6, 14, 20, 10);
        CreateEnemy("Forest", "VineSprite",  EnemyType.Normal,  55, 12, 10, 11, 18,  9);
        CreateEnemy("Forest", "HowlingWolf", EnemyType.Normal,  65, 20,  8, 15, 28, 14);
        CreateEnemy("Forest", "GreatOwl",    EnemyType.Elite,   80, 22, 10, 17, 45, 22);
        CreateEnemy("Forest", "Treant",      EnemyType.Miniboss,280,16, 30,  7, 90, 45);
        CreateEnemy("Forest", "AncientWolf", EnemyType.Boss,   400, 25, 20, 14,160, 80);

        // ---- Cave ---------------------------------------------------------
        CreateEnemy("Cave", "CaveBat",       EnemyType.Normal,  40, 20,  5, 22, 30, 15);
        CreateEnemy("Cave", "StoneGolem",    EnemyType.Normal, 120, 12, 40,  6, 35, 17);
        CreateEnemy("Cave", "DarkSpider",    EnemyType.Normal,  55, 24,  8, 18, 32, 16);
        CreateEnemy("Cave", "MushroomSpore", EnemyType.Normal,  75, 15, 18,  9, 28, 14);
        CreateEnemy("Cave", "CrystalBeetle", EnemyType.Elite,  110, 28, 25, 11, 60, 30);
        CreateEnemy("Cave", "ShadowStalker", EnemyType.Elite,   70, 35, 12, 20, 70, 35);
        CreateEnemy("Cave", "CaveTroll",     EnemyType.Boss,   600, 30, 35, 10,240,120);

        // ---- Mountain -----------------------------------------------------
        CreateEnemy("Mountain", "RockHound",    EnemyType.Normal,   85, 28, 18, 16, 45, 22);
        CreateEnemy("Mountain", "Harpy",        EnemyType.Normal,   60, 32, 10, 24, 48, 24);
        CreateEnemy("Mountain", "ArmoredBear",  EnemyType.Normal,  150, 20, 45,  8, 50, 25);
        CreateEnemy("Mountain", "ThunderEagle", EnemyType.Elite,    90, 38, 15, 26, 85, 42);
        CreateEnemy("Mountain", "IronGolem",    EnemyType.Miniboss,450, 25, 55,  7,150, 75);
        CreateEnemy("Mountain", "FrostDrake",   EnemyType.Elite,   130, 35, 28, 18, 90, 45);
        CreateEnemy("Mountain", "MountainKing", EnemyType.Boss,    800, 40, 40, 16,350,175);

        // ---- Volcano ------------------------------------------------------
        CreateEnemy("Volcano", "LavaSalamander", EnemyType.Normal,   90, 35, 20, 18,  60,  30);
        CreateEnemy("Volcano", "MagmaSlime",     EnemyType.Normal,  130, 28, 30, 10,  55,  27);
        CreateEnemy("Volcano", "FireImp",        EnemyType.Normal,   60, 42,  8, 28,  65,  32);
        CreateEnemy("Volcano", "ObsidianGolem",  EnemyType.Normal,  200, 18, 70,  6,  70,  35);
        CreateEnemy("Volcano", "Hellhound",      EnemyType.Elite,   140, 45, 22, 22, 110,  55);
        CreateEnemy("Volcano", "VolcanicDrake",  EnemyType.Miniboss,700, 48, 40, 16, 200, 100);
        CreateEnemy("Volcano", "InfernoDragon",  EnemyType.Boss,   1200, 55, 50, 20, 500, 250);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CombatAssetCreator] All EnemyData assets created.");
    }

    /// <summary>
    /// Creates a single EnemyData asset. Skips if already exists.
    /// </summary>
    private EnemyData CreateEnemy(string region, string fileName,
        EnemyType type, int hp, int atk, int def, int spd, int exp, int gold)
    {
        string folderPath = $"{EnemyBasePath}/{region}";
        EnsureFolder(folderPath);

        string assetPath = $"{folderPath}/{fileName}.asset";
        if (File.Exists(Path.GetFullPath(assetPath)))
        {
            return AssetDatabase.LoadAssetAtPath<EnemyData>(assetPath);
        }

        EnemyData enemy = CreateInstance<EnemyData>();
        enemy.enemyName        = ToDisplayName(fileName);
        enemy.enemyType        = type;
        enemy.baseHealth       = hp;
        enemy.baseAttack       = atk;
        enemy.baseDefense      = def;
        enemy.baseSpeed        = spd;
        enemy.experienceReward = exp;
        enemy.goldDrop         = gold;
        enemy.skillUseChance   = 0.3f;
        enemy.aiBehavior       = GetDefaultBehavior(type);
        enemy.targetPriority   = "Weakest";

        AssetDatabase.CreateAsset(enemy, assetPath);
        return enemy;
    }

    // ============================================================================
    // STAGE CREATION
    // ============================================================================

    private void CreateAllStages()
    {
        // Helper: load enemy by region/fileName
        EnemyData E(string region, string fileName) =>
            AssetDatabase.LoadAssetAtPath<EnemyData>($"{EnemyBasePath}/{region}/{fileName}.asset");

        // ======================================================================
        // REGION 1 — Meadow  (stages 1-5)
        // ======================================================================

        CreateStage(
            region: "Meadow",
            fileName: "Stage_001_SunnyFields",
            stageName: "Sunny Fields",
            number: 1,
            theme: StageTheme.Meadow,
            difficulty: 1,
            unlockedByDefault: true,
            description: "A gentle breeze sweeps through the golden grass. A perfect place to begin your journey.",
            minEnemies: 1, maxEnemies: 2,
            goldReward: 60, expReward: 30,
            spawns: new[] {
                Spawn(E("Meadow","Slime"), 1, 2, 3f),
                Spawn(E("Meadow","FieldRat"), 1, 1, 1f),
            });

        CreateStage(
            region: "Meadow",
            fileName: "Stage_002_FlowerGarden",
            stageName: "Flower Garden",
            number: 2,
            theme: StageTheme.Meadow,
            difficulty: 1,
            description: "Colourful flowers hide small but persistent pests.",
            minEnemies: 1, maxEnemies: 3,
            goldReward: 70, expReward: 35,
            spawns: new[] {
                Spawn(E("Meadow","Slime"), 1, 2, 2f),
                Spawn(E("Meadow","HoneyBee"), 1, 2, 2f),
            });

        CreateStage(
            region: "Meadow",
            fileName: "Stage_003_RollingHills",
            stageName: "Rolling Hills",
            number: 3,
            theme: StageTheme.Meadow,
            difficulty: 2,
            description: "Speed is everything on open ground.",
            minEnemies: 2, maxEnemies: 3,
            goldReward: 80, expReward: 40,
            spawns: new[] {
                Spawn(E("Meadow","FieldRat"), 1, 2, 2f),
                Spawn(E("Meadow","Crow"), 1, 2, 2f),
                Spawn(E("Meadow","Slime"), 1, 1, 1f),
            });

        CreateStage(
            region: "Meadow",
            fileName: "Stage_004_WindPrairie",
            stageName: "Wind Prairie",
            number: 4,
            theme: StageTheme.Meadow,
            difficulty: 2,
            description: "The Giant Slime patrols these plains. It hits harder than it looks.",
            minEnemies: 2, maxEnemies: 3,
            goldReward: 90, expReward: 45,
            spawns: new[] {
                Spawn(E("Meadow","GiantSlime"), 1, 1, 1f),
                Spawn(E("Meadow","HoneyBee"), 1, 2, 2f),
                Spawn(E("Meadow","Crow"), 1, 1, 1f),
            });

        CreateStage(
            region: "Meadow",
            fileName: "Stage_005_MeadowWolf_Boss",
            stageName: "Peaceful Pasture — Boss",
            number: 5,
            theme: StageTheme.Meadow,
            difficulty: 2,
            description: "[BOSS] The Meadow Wolf guards the path to the forest. Defeat it to move on.",
            minEnemies: 1, maxEnemies: 1,
            goldReward: 150, expReward: 80,
            boss: E("Meadow","MeadowWolf"),
            spawns: new[] {
                Spawn(E("Meadow","FieldRat"), 1, 2, 1f),
            });

        // ======================================================================
        // REGION 2 — Forest  (stages 6-10)
        // ======================================================================

        CreateStage(
            region: "Forest",
            fileName: "Stage_006_WhisperingWoods",
            stageName: "Whispering Woods",
            number: 6,
            theme: StageTheme.Forest,
            difficulty: 3,
            description: "The trees close in. Strange sounds echo from the canopy.",
            minEnemies: 2, maxEnemies: 3,
            goldReward: 110, expReward: 55,
            spawns: new[] {
                Spawn(E("Forest","ForestBoar"), 1, 2, 2f),
                Spawn(E("Forest","VineSprite"), 1, 2, 2f),
            });

        CreateStage(
            region: "Forest",
            fileName: "Stage_007_DarkGrove",
            stageName: "Dark Grove",
            number: 7,
            theme: StageTheme.Forest,
            difficulty: 3,
            description: "Poison Frogs lurk beneath the undergrowth.",
            minEnemies: 2, maxEnemies: 4,
            goldReward: 120, expReward: 60,
            spawns: new[] {
                Spawn(E("Forest","PoisonFrog"), 1, 3, 3f),
                Spawn(E("Forest","VineSprite"), 1, 2, 1f),
            });

        CreateStage(
            region: "Forest",
            fileName: "Stage_008_AncientForest",
            stageName: "Ancient Forest",
            number: 8,
            theme: StageTheme.Forest,
            difficulty: 4,
            description: "The howling grows louder. Wolves hunt in packs here.",
            minEnemies: 2, maxEnemies: 4,
            goldReward: 140, expReward: 70,
            spawns: new[] {
                Spawn(E("Forest","HowlingWolf"), 1, 2, 3f),
                Spawn(E("Forest","ForestBoar"), 1, 2, 1f),
                Spawn(E("Forest","PoisonFrog"), 1, 1, 1f),
            });

        CreateStage(
            region: "Forest",
            fileName: "Stage_009_TwilightGlade",
            stageName: "Twilight Glade",
            number: 9,
            theme: StageTheme.Forest,
            difficulty: 4,
            description: "A Great Owl watches from above. The Treant stirs.",
            minEnemies: 2, maxEnemies: 3,
            goldReward: 160, expReward: 80,
            boss: E("Forest","Treant"),
            spawns: new[] {
                Spawn(E("Forest","GreatOwl"), 1, 1, 1f),
                Spawn(E("Forest","HowlingWolf"), 1, 2, 1f),
            });

        CreateStage(
            region: "Forest",
            fileName: "Stage_010_AncientWolf_Boss",
            stageName: "Mossy Hollow — Boss",
            number: 10,
            theme: StageTheme.Forest,
            difficulty: 4,
            description: "[BOSS] The Ancient Wolf rules this forest. Its pack defends it fiercely.",
            minEnemies: 1, maxEnemies: 1,
            goldReward: 250, expReward: 160,
            boss: E("Forest","AncientWolf"),
            spawns: new[] {
                Spawn(E("Forest","HowlingWolf"), 1, 2, 1f),
            });

        // ======================================================================
        // REGION 3 — Cave  (stages 11-15)
        // ======================================================================

        CreateStage(
            region: "Cave",
            fileName: "Stage_011_CrystalCavern",
            stageName: "Crystal Cavern",
            number: 11,
            theme: StageTheme.Cave,
            difficulty: 5,
            description: "The crystals glow faintly. Bats dive from the ceiling.",
            minEnemies: 2, maxEnemies: 4,
            goldReward: 180, expReward: 90,
            spawns: new[] {
                Spawn(E("Cave","CaveBat"), 2, 4, 3f),
                Spawn(E("Cave","MushroomSpore"), 1, 2, 1f),
            });

        CreateStage(
            region: "Cave",
            fileName: "Stage_012_ShadowDepths",
            stageName: "Shadow Depths",
            number: 12,
            theme: StageTheme.Cave,
            difficulty: 5,
            description: "Dark Spiders spin webs across every passage.",
            minEnemies: 2, maxEnemies: 4,
            goldReward: 190, expReward: 95,
            spawns: new[] {
                Spawn(E("Cave","DarkSpider"), 1, 3, 3f),
                Spawn(E("Cave","CaveBat"), 1, 2, 1f),
            });

        CreateStage(
            region: "Cave",
            fileName: "Stage_013_EchoChamber",
            stageName: "Echo Chamber",
            number: 13,
            theme: StageTheme.Cave,
            difficulty: 6,
            description: "The Stone Golem barely moves, but it barely flinches either.",
            minEnemies: 2, maxEnemies: 3,
            goldReward: 210, expReward: 105,
            spawns: new[] {
                Spawn(E("Cave","StoneGolem"), 1, 2, 2f),
                Spawn(E("Cave","CrystalBeetle"), 1, 1, 1f),
                Spawn(E("Cave","DarkSpider"), 1, 2, 1f),
            });

        CreateStage(
            region: "Cave",
            fileName: "Stage_014_UndergroundLake",
            stageName: "Underground Lake",
            number: 14,
            theme: StageTheme.Cave,
            difficulty: 6,
            description: "Something fast moves in the shadows. Don't let it reach your back line.",
            minEnemies: 2, maxEnemies: 3,
            goldReward: 230, expReward: 115,
            spawns: new[] {
                Spawn(E("Cave","ShadowStalker"), 1, 2, 2f),
                Spawn(E("Cave","CrystalBeetle"), 1, 2, 2f),
            });

        CreateStage(
            region: "Cave",
            fileName: "Stage_015_CaveTroll_Boss",
            stageName: "Stalactite Hall — Boss",
            number: 15,
            theme: StageTheme.Cave,
            difficulty: 6,
            description: "[BOSS] The Cave Troll fills the entire chamber. Tanky AND aggressive.",
            minEnemies: 1, maxEnemies: 1,
            goldReward: 380, expReward: 240,
            boss: E("Cave","CaveTroll"),
            spawns: new[] {
                Spawn(E("Cave","StoneGolem"), 1, 1, 1f),
            });

        // ======================================================================
        // REGION 4 — Mountain  (stages 16-20)
        // ======================================================================

        CreateStage(
            region: "Mountain",
            fileName: "Stage_016_RockyPeak",
            stageName: "Rocky Peak",
            number: 16,
            theme: StageTheme.Mountain,
            difficulty: 7,
            description: "Rock Hounds patrol the lower paths. Stronger than forest wolves.",
            minEnemies: 2, maxEnemies: 3,
            goldReward: 260, expReward: 130,
            spawns: new[] {
                Spawn(E("Mountain","RockHound"), 1, 3, 2f),
                Spawn(E("Mountain","Harpy"), 1, 1, 1f),
            });

        CreateStage(
            region: "Mountain",
            fileName: "Stage_017_EaglesNest",
            stageName: "Eagle's Nest",
            number: 17,
            theme: StageTheme.Mountain,
            difficulty: 7,
            description: "Harpies and Thunder Eagles dive from the peaks.",
            minEnemies: 2, maxEnemies: 4,
            goldReward: 280, expReward: 140,
            spawns: new[] {
                Spawn(E("Mountain","Harpy"), 1, 3, 3f),
                Spawn(E("Mountain","ThunderEagle"), 1, 1, 1f),
            });

        CreateStage(
            region: "Mountain",
            fileName: "Stage_018_StoneGiantPass",
            stageName: "Stone Giant Pass",
            number: 18,
            theme: StageTheme.Mountain,
            difficulty: 8,
            description: "Armored Bears and Iron Golems block the mountain pass.",
            minEnemies: 2, maxEnemies: 3,
            goldReward: 300, expReward: 150,
            boss: E("Mountain","IronGolem"),
            spawns: new[] {
                Spawn(E("Mountain","ArmoredBear"), 1, 2, 2f),
                Spawn(E("Mountain","RockHound"), 1, 1, 1f),
            });

        CreateStage(
            region: "Mountain",
            fileName: "Stage_019_CloudRidge",
            stageName: "Cloud Ridge",
            number: 19,
            theme: StageTheme.Mountain,
            difficulty: 8,
            description: "A Frost Drake circles overhead. The elite Thunder Eagles escort it.",
            minEnemies: 2, maxEnemies: 3,
            goldReward: 320, expReward: 160,
            spawns: new[] {
                Spawn(E("Mountain","FrostDrake"), 1, 1, 1f),
                Spawn(E("Mountain","ThunderEagle"), 1, 2, 2f),
                Spawn(E("Mountain","Harpy"), 1, 1, 1f),
            });

        CreateStage(
            region: "Mountain",
            fileName: "Stage_020_MountainKing_Boss",
            stageName: "Frozen Summit — Boss",
            number: 20,
            theme: StageTheme.Mountain,
            difficulty: 8,
            description: "[BOSS] The Mountain King commands peak and storm alike. Nothing survives up here.",
            minEnemies: 1, maxEnemies: 1,
            goldReward: 525, expReward: 350,
            boss: E("Mountain","MountainKing"),
            spawns: new[] {
                Spawn(E("Mountain","FrostDrake"), 1, 1, 1f),
            });

        // ======================================================================
        // REGION 5 — Volcano  (stages 21-25)
        // ======================================================================

        CreateStage(
            region: "Volcano",
            fileName: "Stage_021_LavaFields",
            stageName: "Lava Fields",
            number: 21,
            theme: StageTheme.Volcano,
            difficulty: 9,
            description: "The ground burns. Lava Salamanders are the least of your worries.",
            minEnemies: 2, maxEnemies: 3,
            goldReward: 370, expReward: 185,
            spawns: new[] {
                Spawn(E("Volcano","LavaSalamander"), 1, 3, 3f),
                Spawn(E("Volcano","FireImp"), 1, 1, 1f),
            });

        CreateStage(
            region: "Volcano",
            fileName: "Stage_022_AshenCaldera",
            stageName: "Ashen Caldera",
            number: 22,
            theme: StageTheme.Volcano,
            difficulty: 9,
            description: "Fire Imps move faster than anything you've faced. They hit twice before you blink.",
            minEnemies: 2, maxEnemies: 4,
            goldReward: 390, expReward: 195,
            spawns: new[] {
                Spawn(E("Volcano","FireImp"), 2, 4, 3f),
                Spawn(E("Volcano","MagmaSlime"), 1, 2, 1f),
            });

        CreateStage(
            region: "Volcano",
            fileName: "Stage_023_ObsidianPeak",
            stageName: "Obsidian Peak",
            number: 23,
            theme: StageTheme.Volcano,
            difficulty: 9,
            description: "Obsidian Golems and Hellhounds. One won't die; the other will kill you first.",
            minEnemies: 2, maxEnemies: 3,
            goldReward: 420, expReward: 210,
            spawns: new[] {
                Spawn(E("Volcano","ObsidianGolem"), 1, 2, 2f),
                Spawn(E("Volcano","Hellhound"), 1, 2, 2f),
            });

        CreateStage(
            region: "Volcano",
            fileName: "Stage_024_FireBasin",
            stageName: "Fire Basin",
            number: 24,
            theme: StageTheme.Volcano,
            difficulty: 10,
            description: "The Volcanic Drake and its elite guard. The last test before the Dragon.",
            minEnemies: 2, maxEnemies: 3,
            goldReward: 460, expReward: 230,
            boss: E("Volcano","VolcanicDrake"),
            spawns: new[] {
                Spawn(E("Volcano","Hellhound"), 1, 2, 2f),
                Spawn(E("Volcano","FireImp"), 1, 2, 1f),
            });

        CreateStage(
            region: "Volcano",
            fileName: "Stage_025_InfernoDragon_Boss",
            stageName: "Magma Core — Final Boss",
            number: 25,
            theme: StageTheme.Volcano,
            difficulty: 10,
            description: "[FINAL BOSS] The Inferno Dragon. Absolute peak threat. Bring your best animals — fully fed and petted.",
            minEnemies: 1, maxEnemies: 1,
            goldReward: 750, expReward: 500,
            boss: E("Volcano","InfernoDragon"),
            spawns: new[] {
                Spawn(E("Volcano","VolcanicDrake"), 1, 1, 1f),
                Spawn(E("Volcano","Hellhound"), 1, 1, 1f),
            });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CombatAssetCreator] All StageData assets created.");
    }

    // ============================================================================
    // STAGE HELPER
    // ============================================================================

    private void CreateStage(
        string region,
        string fileName,
        string stageName,
        int number,
        StageTheme theme,
        int difficulty,
        string description,
        int minEnemies,
        int maxEnemies,
        int goldReward,
        int expReward,
        EnemySpawn[] spawns,
        EnemyData boss = null,
        bool unlockedByDefault = false)
    {
        string folderPath = $"{StageBasePath}/{region}";
        EnsureFolder(folderPath);

        string assetPath = $"{folderPath}/{fileName}.asset";
        if (File.Exists(Path.GetFullPath(assetPath)))
        {
            Debug.Log($"[CombatAssetCreator] Skipped (exists): {assetPath}");
            return;
        }

        StageData stage = CreateInstance<StageData>();
        stage.stageName           = stageName;
        stage.stageNumber         = number;
        stage.theme               = theme;
        stage.difficulty          = Mathf.Clamp(difficulty, 1, 10);
        stage.description         = description;
        stage.minTotalEnemies     = minEnemies;
        stage.maxTotalEnemies     = maxEnemies;
        stage.baseGoldReward      = goldReward;
        stage.goldVariance        = 0.2f;
        stage.baseExperienceReward= expReward;
        stage.bossEnemy           = boss;
        stage.unlockedByDefault   = unlockedByDefault || number == 1;
        stage.minimumPlayerLevel  = Mathf.Max(1, (difficulty - 1) * 2);
        stage.recommendedTeamSize = Mathf.Clamp(1 + difficulty / 3, 1, 6);
        stage.enemyStatMultiplier = 1f + (difficulty - 1) * 0.15f;
        stage.ambientColor        = GetThemeColor(theme);

        stage.enemySpawns = new List<EnemySpawn>();
        if (spawns != null)
        {
            foreach (EnemySpawn s in spawns)
            {
                if (s.enemy != null)
                    stage.enemySpawns.Add(s);
            }
        }

        AssetDatabase.CreateAsset(stage, assetPath);
        Debug.Log($"[CombatAssetCreator] Created stage: {assetPath}");
    }

    // ============================================================================
    // UTILITIES
    // ============================================================================

    private static EnemySpawn Spawn(EnemyData enemy, int min, int max, float weight)
    {
        return new EnemySpawn
        {
            enemy       = enemy,
            minCount    = min,
            maxCount    = max,
            spawnWeight = weight
        };
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string ToDisplayName(string camelCase)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < camelCase.Length; i++)
        {
            if (i > 0 && char.IsUpper(camelCase[i]))
                sb.Append(' ');
            sb.Append(camelCase[i]);
        }
        return sb.ToString();
    }

    private static string GetDefaultBehavior(EnemyType type)
    {
        return type switch
        {
            EnemyType.Normal   => "Aggressive",
            EnemyType.Elite    => "Aggressive",
            EnemyType.Miniboss => "Defensive",
            EnemyType.Boss     => "Aggressive",
            _                  => "Aggressive"
        };
    }

    private static Color GetThemeColor(StageTheme theme)
    {
        return theme switch
        {
            StageTheme.Meadow   => new Color(0.6f, 0.85f, 0.4f),
            StageTheme.Forest   => new Color(0.2f, 0.5f, 0.2f),
            StageTheme.Cave     => new Color(0.2f, 0.2f, 0.3f),
            StageTheme.Mountain => new Color(0.6f, 0.6f, 0.7f),
            StageTheme.Volcano  => new Color(0.8f, 0.3f, 0.1f),
            _                   => Color.white
        };
    }
}

} // namespace SowurShield.Editor
