using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns player and enemy teams on the combat grid at the start of battle.
/// Reads team composition from TeamAssemblerData and creates CombatUnits.
///
/// SETUP IN UNITY:
/// 1. Add this script to a GameObject in the Combat scene
/// 2. Assign the combatUnitPrefab (prefab with CombatUnit component)
/// 3. This script will automatically spawn teams when Start() is called
///
/// RESPONSIBILITIES:
/// - Load player team from TeamAssemblerData
/// - Spawn CombatUnits on the grid at correct positions
/// - Initialize units with animal data
/// - Handle team validation
/// </summary>
public class CombatTeamSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Prefab with CombatUnit component to spawn for each animal")]
    [SerializeField] private GameObject combatUnitPrefab;

    [Header("Spawn Configuration")]
    [Tooltip("Should spawn player team from TeamAssemblerData?")]
    [SerializeField] private bool spawnPlayerTeam = true;

    [Tooltip("Should spawn enemy team? (placeholder for now)")]
    [SerializeField] private bool spawnEnemyTeam = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private void Start()
    {
        // Small delay to ensure GridManager is ready
        Invoke(nameof(SpawnTeams), 0.1f);
    }

    /// <summary>
    /// Spawn both player and enemy teams
    /// </summary>
    private void SpawnTeams()
    {
        if (showDebugLogs)
        {
        }

        // Verify GridManager is ready
        if (GridManager.Instance == null)
        {
            return;
        }

        // Spawn player team
        if (spawnPlayerTeam)
        {
            SpawnPlayerTeam();
        }

        // Spawn enemy team (placeholder for now)
        if (spawnEnemyTeam)
        {
            SpawnEnemyTeam();
        }
    }

    /// <summary>
    /// Spawn player team from TeamAssemblerData
    /// </summary>
    private void SpawnPlayerTeam()
    {
        if (showDebugLogs)
        {
        }

        // Check if TeamAssemblerData exists
        if (TeamAssemblerData.Instance == null)
        {
            return;
        }

        // Get team from TeamAssemblerData
        List<TeamAssemblerData.PositionedAnimal> playerTeam = TeamAssemblerData.Instance.team;

        if (playerTeam == null || playerTeam.Count == 0)
        {
            return;
        }

        if (showDebugLogs)
        {
        }

        // Spawn each animal at their assigned position
        int spawnedCount = 0;
        foreach (TeamAssemblerData.PositionedAnimal positioned in playerTeam)
        {
            if (positioned.animalData == null)
            {
                continue;
            }

            // Spawn the unit using AnimalData and runtime stats
            bool success = SpawnAnimalUnit(
                positioned.animalData,
                positioned.customName,
                positioned.gridPosition,
                isPlayerUnit: true,
                isFed: positioned.isFed,
                happiness: positioned.happiness,
                attackGrowth: positioned.attackGrowth,
                defenseGrowth: positioned.defenseGrowth,
                speedGrowth: positioned.speedGrowth,
                healthGrowth: positioned.healthGrowth,
                level: positioned.level,
                experience: positioned.experience,
                seasonalAttackMod: positioned.seasonalAttackMod,
                seasonalDefenseMod: positioned.seasonalDefenseMod,
                seasonalSpeedMod: positioned.seasonalSpeedMod
            );

            if (success)
            {
                spawnedCount++;
            }
        }

        if (showDebugLogs)
        {
        }
    }

    /// <summary>
    /// Spawn enemy team (placeholder - will be expanded later)
    /// </summary>
    private void SpawnEnemyTeam()
    {
        if (showDebugLogs)
        {
        }

        // TODO: Implement enemy spawning based on zone difficulty
        // For now, this is a placeholder
    }

    /// <summary>
    /// Spawn a single animal as a CombatUnit on the grid using AnimalData
    /// </summary>
    private bool SpawnAnimalUnit(AnimalData animalData, string customName, Vector2Int gridPosition, bool isPlayerUnit, bool isFed,
        float happiness, float attackGrowth, float defenseGrowth, float speedGrowth, float healthGrowth, int level, float experience,
        float seasonalAttackMod, float seasonalDefenseMod, float seasonalSpeedMod)
    {
        if (animalData == null)
        {
            return false;
        }

        if (combatUnitPrefab == null)
        {
            return false;
        }

        // Validate grid position
        if (!GridManager.Instance.IsValidPosition(gridPosition.x, gridPosition.y))
        {
            return false;
        }

        // Get the grid cell
        GridCell targetCell = GridManager.Instance.GetCell(gridPosition);
        if (targetCell == null)
        {
            return false;
        }

        // Check if cell is already occupied
        if (!targetCell.IsEmpty())
        {
            return false;
        }

        // Instantiate the combat unit
        GameObject unitObj = Instantiate(combatUnitPrefab, targetCell.transform.position, Quaternion.identity);
        unitObj.name = $"CombatUnit_{customName}";

        // Add SpriteRenderer with the sprite directly assigned
        SpriteRenderer sr = unitObj.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = unitObj.AddComponent<SpriteRenderer>();
        }
        sr.sprite = animalData.idleSprite;
        sr.sortingOrder = 10; // Above grid

        // Add Animal component and initialize it with AnimalData
        Animal animal = unitObj.GetComponent<Animal>();
        if (animal == null)
        {
            animal = unitObj.AddComponent<Animal>();
        }

        // Set the animal data via reflection (since it's a SerializeField)
        var animalDataField = typeof(Animal).GetField("animalData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        animalDataField?.SetValue(animal, animalData);

        // Set custom name for player units
        if (isPlayerUnit && !string.IsNullOrEmpty(customName))
        {
            var customNameField = typeof(Animal).GetField("customName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            customNameField?.SetValue(animal, customName);
        }

        // Manually trigger combat stats initialization
        var combatStatsField = typeof(Animal).GetField("combatStats",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        AnimalCombatStats stats = combatStatsField?.GetValue(animal) as AnimalCombatStats;
        if (stats != null && animalData.baseCombatStats != null)
        {
            stats.Initialize(animalData.baseCombatStats);

            // Apply runtime progression stats from farm scene
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

            if (showDebugLogs)
            {
            }
        }

        // Get CombatUnit component
        CombatUnit combatUnit = unitObj.GetComponent<CombatUnit>();
        if (combatUnit == null)
        {
            Destroy(unitObj);
            return false;
        }

        // Set team BEFORE initializing
        combatUnit.isPlayerUnit = isPlayerUnit;

        // Initialize the unit from the Animal component
        combatUnit.InitializeFromAnimal(animal, isPlayerUnit);

        // Place unit on grid
        bool placed = GridManager.Instance.PlaceUnitAt(combatUnit, gridPosition);

        if (placed)
        {
            if (showDebugLogs)
            {
            }
            return true;
        }
        else
        {
            Destroy(unitObj);
            return false;
        }
    }

    /// <summary>
    /// Clear all units from grid (for battle restart)
    /// </summary>
    public void ClearAllUnits()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.ClearAllUnits();

            // Also destroy GameObjects
            CombatUnit[] allUnits = FindObjectsOfType<CombatUnit>();
            foreach (CombatUnit unit in allUnits)
            {
                Destroy(unit.gameObject);
            }

            if (showDebugLogs)
            {
            }
        }
    }

    /// <summary>
    /// Respawn teams (useful for battle restart)
    /// </summary>
    public void RespawnTeams()
    {
        ClearAllUnits();
        SpawnTeams();
    }
}
