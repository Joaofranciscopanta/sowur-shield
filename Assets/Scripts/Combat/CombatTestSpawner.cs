using UnityEngine;
using System.Collections.Generic;
using SowurShield.Animals;

namespace SowurShield.Combat
{

/// <summary>
/// TEST SCRIPT - Spawns test units on the combat grid
/// This is temporary for testing - will be replaced by proper BattleManager later
///
/// SETUP IN UNITY:
/// 1. Attach this script to GridManager object
/// 2. Assign AnimalData assets in Inspector
/// 3. Press Play
/// 4. See animal units spawn on grid with sprites!
///
/// PURPOSE:
/// - Test real animal integration in combat
/// - Verify grid cell placement works
/// - Test CombatUnit initialization with Animal data
/// - Visual verification of unit positioning with sprites
/// </summary>
public class CombatTestSpawner : MonoBehaviour
{
    [Header("Test Configuration")]
    [Tooltip("Spawn test units automatically on Start?")]
    [SerializeField] private bool autoSpawnOnStart = false;

    [Tooltip("Use team from TeamAssemblerData? (false = spawn test units)")]
    [SerializeField] private bool useTeamAssemblerData = false;

    [Tooltip("Number of player units to spawn (1-5) - only if not using TeamAssemblerData")]
    [SerializeField] private int playerUnitsToSpawn = 3;

    [Tooltip("Number of enemy units to spawn (1-5)")]
    [SerializeField] private int enemyUnitsToSpawn = 2;

    [Header("Animal Data (Optional - use sprites!)")]
    [Tooltip("Drag AnimalData assets here to use real animals in combat")]
    [SerializeField] private List<AnimalData> testAnimalData = new List<AnimalData>();

    private void Start()
    {
        if (autoSpawnOnStart)
        {
            // Wait for grid to generate, then spawn
            Invoke(nameof(SpawnTestUnits), 0.5f);
        }
    }

    /// <summary>
    /// Spawn test units for visual verification
    /// </summary>
    public void SpawnTestUnits()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        // Check if we should use TeamAssemblerData
        if (useTeamAssemblerData && TeamAssemblerData.Instance != null && TeamAssemblerData.Instance.GetTeamSize() > 0)
        {
            SpawnTeamFromAssembler();
        }
        else
        {
            // Spawn player units
            SpawnPlayerUnits();
        }

        // Always spawn enemy units
        SpawnEnemyUnits();
    }

    /// <summary>
    /// Spawn player team from TeamAssemblerData
    /// </summary>
    private void SpawnTeamFromAssembler()
    {

        if (TeamAssemblerData.Instance.team.Count == 0)
        {
            return;
        }

        foreach (var positioned in TeamAssemblerData.Instance.team)
        {
            // Check if we have valid AnimalData (ScriptableObject - persists!)
            if (positioned.animalData == null)
            {
                continue;
            }

            string displayName = positioned.GetDisplayName();

            // Create unit using persisted AnimalData and runtime stats
            CreateAnimalUnitWithStats(
                displayName,
                positioned.gridPosition,
                true, // isPlayer
                positioned.animalData,
                positioned.happiness,
                positioned.attackGrowth,
                positioned.defenseGrowth,
                positioned.speedGrowth,
                positioned.healthGrowth,
                positioned.level,
                positioned.experience,
                positioned.seasonalAttackMod,
                positioned.seasonalDefenseMod,
                positioned.seasonalSpeedMod
            );

        }
    }

    /// <summary>
    /// Spawn player units on player side (columns 6-8, RIGHT side)
    /// </summary>
    private void SpawnPlayerUnits()
    {
        // Pre-defined test positions for player units
        // Player side = columns 6-8 (rightmost)
        Vector2Int[] playerPositions = new Vector2Int[]
        {
            new Vector2Int(6, 0), // Front row, left of player side
            new Vector2Int(7, 1), // Middle row, center
            new Vector2Int(8, 0), // Front row, right
            new Vector2Int(6, 2), // Back row
            new Vector2Int(7, 2)  // Back row
        };

        for (int i = 0; i < playerUnitsToSpawn && i < playerPositions.Length; i++)
        {
            Vector2Int pos = playerPositions[i];
            CreateTestUnit($"Player_{i + 1}", pos, true, 100f, 15f, 10f, 12f);
        }
    }

    /// <summary>
    /// Spawn enemy units on enemy side (columns 0-5, LEFT side)
    /// </summary>
    private void SpawnEnemyUnits()
    {
        // Pre-defined test positions for enemy units
        // Enemy side = columns 0-5 (leftmost)
        Vector2Int[] enemyPositions = new Vector2Int[]
        {
            new Vector2Int(2, 2), // Middle row
            new Vector2Int(4, 1), // Middle row
            new Vector2Int(1, 3), // Back row
            new Vector2Int(3, 4), // Back row
            new Vector2Int(5, 2)  // Middle row
        };

        for (int i = 0; i < enemyUnitsToSpawn && i < enemyPositions.Length; i++)
        {
            Vector2Int pos = enemyPositions[i];
            CreateTestUnit($"Enemy_{i + 1}", pos, false, 80f, 12f, 8f, 10f);
        }
    }

    /// <summary>
    /// Create a test unit at specified position
    /// </summary>
    private void CreateTestUnit(string name, Vector2Int gridPos, bool isPlayer,
                                float hp, float atk, float def, float spd)
    {
        // Try to use real animal data if available AND has sprites
        if (testAnimalData != null && testAnimalData.Count > 0)
        {
            // Pick random animal data from the list
            AnimalData data = testAnimalData[Random.Range(0, testAnimalData.Count)];

            // Check if this AnimalData has a sprite assigned
            if (data != null && data.idleSprite != null)
            {
                CreateAnimalUnit(name, gridPos, isPlayer, data);
            }
            else
            {
                CreateGenericTestUnit(name, gridPos, isPlayer, hp, atk, def, spd);
            }
        }
        else
        {
            // Fall back to sphere-based test units
            CreateGenericTestUnit(name, gridPos, isPlayer, hp, atk, def, spd);
        }
    }

    /// <summary>
    /// Create a unit from AnimalData (with sprites and real stats!)
    /// </summary>
    private void CreateAnimalUnit(string customName, Vector2Int gridPos, bool isPlayer, AnimalData animalData)
    {
        CreateAnimalUnitWithStats(customName, gridPos, isPlayer, animalData, 50f, 1f, 1f, 1f, 1f, 1, 0f, 1f, 1f, 1f); // Default stats
    }

    /// <summary>
    /// Create a unit from AnimalData with runtime stats (happiness, growth, level, seasonal bonuses, etc.)
    /// </summary>
    private void CreateAnimalUnitWithStats(string customName, Vector2Int gridPos, bool isPlayer, AnimalData animalData,
        float happiness, float attackGrowth, float defenseGrowth, float speedGrowth, float healthGrowth, int level, float experience,
        float seasonalAttackMod, float seasonalDefenseMod, float seasonalSpeedMod)
    {
        // Create GameObject for the combat unit
        GameObject unitObj = new GameObject(customName);
        unitObj.transform.localScale = Vector3.one;

        // Add SpriteRenderer FIRST with the sprite directly assigned
        SpriteRenderer sr = unitObj.AddComponent<SpriteRenderer>();
        sr.sprite = animalData.idleSprite;
        sr.sortingOrder = 10; // Above grid

        // Add Animal component
        Animal animal = unitObj.AddComponent<Animal>();

        // Set the animal data via reflection (since it's a SerializeField)
        var animalDataField = typeof(Animal).GetField("animalData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        animalDataField?.SetValue(animal, animalData);

        // Only set custom name for player units (enemies use breed name)
        if (isPlayer)
        {
            var customNameField = typeof(Animal).GetField("customName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            customNameField?.SetValue(animal, customName);
        }
        // For enemies, leave customName empty so GetDisplayName() returns breed name

        // Manually trigger combat stats initialization
        var combatStatsField = typeof(Animal).GetField("combatStats",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        AnimalCombatStats stats = combatStatsField?.GetValue(animal) as AnimalCombatStats;
        if (stats != null && animalData.baseCombatStats != null)
        {
            stats.Initialize(animalData.baseCombatStats);

            // Apply runtime progression stats
            stats.happiness = happiness;
            stats.attackGrowth = attackGrowth;
            stats.defenseGrowth = defenseGrowth;
            stats.speedGrowth = speedGrowth;
            stats.healthGrowth = healthGrowth;
            stats.level = level;
            stats.experience = experience;

            // Apply seasonal bonuses
            stats.seasonalAttackMod = seasonalAttackMod;
            stats.seasonalDefenseMod = seasonalDefenseMod;
            stats.seasonalSpeedMod = seasonalSpeedMod;

        }

        // Add CombatUnit component
        CombatUnit combatUnit = unitObj.AddComponent<CombatUnit>();

        // Set team BEFORE initializing
        combatUnit.isPlayerUnit = isPlayer;

        // Initialize from the animal (this will use real stats and sprite!)
        combatUnit.InitializeFromAnimal(animal, isPlayer);


        // Place on grid using GridManager (this assigns health bar prefab)
        bool placed = GridManager.Instance.PlaceUnitAt(combatUnit, gridPos);
        if (!placed)
        {
            Destroy(unitObj);
        }
    }

    /// <summary>
    /// Create generic test unit (sphere-based fallback)
    /// </summary>
    private void CreateGenericTestUnit(string name, Vector2Int gridPos, bool isPlayer,
                                       float hp, float atk, float def, float spd)
    {
        // Create GameObject
        GameObject unitObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        unitObj.name = name;
        unitObj.transform.localScale = Vector3.one * 0.5f;

        // Add CombatUnit component
        CombatUnit unit = unitObj.AddComponent<CombatUnit>();

        // Set team BEFORE initializing (so color is correct)
        unit.isPlayerUnit = isPlayer;

        // Initialize as test unit
        unit.InitializeAsEnemy(name, hp, atk, def, spd);

        // Place on grid using GridManager (this assigns health bar prefab)
        bool placed = GridManager.Instance.PlaceUnitAt(unit, gridPos);
        if (!placed)
        {
            Destroy(unitObj);
        }
    }

    /// <summary>
    /// Clear all units from grid (for testing)
    /// </summary>
    [ContextMenu("Clear All Units")]
    public void ClearAllUnits()
    {
        if (GridManager.Instance != null)
        {
            List<CombatUnit> units = GridManager.Instance.GetAllUnits();
            foreach (CombatUnit unit in units)
            {
                if (unit != null)
                {
                    Destroy(unit.gameObject);
                }
            }

            GridManager.Instance.ClearAllUnits();
        }
    }

    /// <summary>
    /// Respawn units (for testing)
    /// </summary>
    [ContextMenu("Respawn Units")]
    public void RespawnUnits()
    {
        ClearAllUnits();
        Invoke(nameof(SpawnTestUnits), 0.1f);
    }
}

} // namespace SowurShield.Combat
