using UnityEngine;
using System.Collections.Generic;
using SowurShield.Animals;

namespace SowurShield.Combat
{

/// <summary>
/// Spawns the player's team on the combat grid at battle start.
/// Reads team composition from TeamAssemblerData (set in the farm scene).
/// Uses the same proven pattern as CombatTestSpawner.CreateAnimalUnitWithStats().
///
/// SETUP IN UNITY:
/// 1. Add this script to any GameObject in the Combat scene
/// 2. Make sure spawnPlayerTeam = true
/// 3. That's it — no prefab needed
/// </summary>
public class CombatTeamSpawner : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [Tooltip("Spawn player team from TeamAssemblerData on Start?")]
    [SerializeField] private bool spawnPlayerTeam = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // Default player spawn positions (columns 6-8, right side)
    private static readonly Vector2Int[] DefaultPlayerPositions = new Vector2Int[]
    {
        new Vector2Int(6, 2),
        new Vector2Int(7, 1),
        new Vector2Int(8, 2),
        new Vector2Int(6, 0),
        new Vector2Int(7, 3),
        new Vector2Int(8, 0),
    };

    private void Start()
    {
        // A prior bug had Time.timeScale left at 0 when entering CombatScene, which silently
        // prevents Invoke() from ever firing. Self-heals but stays loud since it points at a
        // real bug elsewhere (something left the game paused) if it ever fires again.
        if (Time.timeScale == 0f)
        {
            Debug.LogWarning("[CombatTeamSpawner] Time.timeScale is 0 at Start() — forcing 1f so spawning can proceed.");
            Time.timeScale = 1f;
        }
        // Wait for GridManager.GenerateGrid() (runs in its own Start) and
        // for all other Start() callbacks to complete before spawning.
        // 0.5f is safe; TurnManager waits 1.0f before InitializeCombat.
        Invoke(nameof(SpawnTeams), 0.5f);
    }

    private void SpawnTeams()
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("[CombatTeamSpawner] GridManager.Instance is null!");
            return;
        }

        if (spawnPlayerTeam)
            SpawnPlayerTeam();
        else
            Debug.LogWarning("[CombatTeamSpawner] spawnPlayerTeam is FALSE — tick it in the Inspector!");
    }

    private void SpawnPlayerTeam()
    {
        // Restore from PlayerPrefs if team is empty (domain reload in builds clears static fields)
        if (TeamAssemblerData.Instance.team == null || TeamAssemblerData.Instance.team.Count == 0)
            TeamAssemblerData.Instance.LoadFromPrefs();

        List<TeamAssemblerData.PositionedAnimal> team = null;

        if (TeamAssemblerData.Instance != null && TeamAssemblerData.Instance.team != null &&
            TeamAssemblerData.Instance.team.Count > 0)
        {
            team = TeamAssemblerData.Instance.team;
        }
        else
        {
            Debug.LogWarning("[CombatTeamSpawner] TeamAssemblerData is null or empty — using fallback test unit.");
            try
            {
                SpawnFallbackUnit();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CombatTeamSpawner] Exception in SpawnFallbackUnit(): {ex}");
            }
            return;
        }


        int spawned = 0;
        int posIndex = 0;
        foreach (var positioned in team)
        {
            if (positioned.animalData == null)
            {
                Debug.LogWarning("[CombatTeamSpawner] Skipping entry with null AnimalData.");
                continue;
            }

            // Wrap per-unit spawn (including position resolution) in try/catch: an
            // uncaught exception here would silently abort the whole foreach loop
            // (and the Invoke callback), leaving GridManager.GetAllUnits() empty in
            // builds with no visible error other than a console stack trace.
            bool ok = false;
            try
            {
                // Use the stored grid position from TeamAssemblerData, but map it to valid player columns
                Vector2Int pos = GetPlayerSpawnPosition(positioned.gridPosition, posIndex);
                ok = SpawnUnit(positioned, pos);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CombatTeamSpawner] Exception spawning '{positioned.GetDisplayName()}': {ex}");
            }

            if (ok) spawned++;
            posIndex++;
        }

        if (showDebugLogs)
            Debug.Log($"[CombatTeamSpawner] Spawned {spawned}/{team.Count} units.");
    }

    /// <summary>
    /// Map a TeamAssembler grid position to a valid combat grid position on the player side.
    /// TeamAssembler uses the same 9x5 grid as combat (columns 6-8 = player side).
    /// </summary>
    private Vector2Int GetPlayerSpawnPosition(Vector2Int teamAssemblerPos, int fallbackIndex)
    {
        // TeamAssembler uses the SAME grid coordinate system as the combat grid.
        // Player side = columns 6-8, rows 0-4. Just clamp to valid range.
        int combatCol = Mathf.Clamp(teamAssemblerPos.x, 6, 8);
        int combatRow = Mathf.Clamp(teamAssemblerPos.y, 0, 4);

        // If that cell is already occupied, fall back to default positions
        GridCell cell = GridManager.Instance.GetCell(combatCol, combatRow);
        if (cell != null && cell.IsEmpty())
            return new Vector2Int(combatCol, combatRow);

        // Try default positions
        for (int i = fallbackIndex; i < DefaultPlayerPositions.Length; i++)
        {
            Vector2Int fallback = DefaultPlayerPositions[i];
            GridCell fb = GridManager.Instance.GetCell(fallback);
            if (fb != null && fb.IsEmpty())
                return fallback;
        }

        return new Vector2Int(combatCol, combatRow); // Last resort — PlaceUnitAt will log error
    }

    /// <summary>
    /// Create one CombatUnit from a PositionedAnimal.
    /// Uses the exact same pattern as CombatTestSpawner.CreateAnimalUnitWithStats().
    /// </summary>
    private bool SpawnUnit(TeamAssemblerData.PositionedAnimal positioned, Vector2Int pos)
    {
        AnimalData data = positioned.animalData;
        string displayName = positioned.GetDisplayName();

        return CreateAnimalUnit(displayName, pos, true, data,
            positioned.happiness,
            positioned.attackGrowth,
            positioned.defenseGrowth,
            positioned.speedGrowth,
            positioned.healthGrowth,
            positioned.level,
            positioned.experience,
            positioned.seasonalAttackMod,
            positioned.seasonalDefenseMod,
            positioned.seasonalSpeedMod);
    }

    /// <summary>
    /// Spawn a single hardcoded test chicken when no TeamAssemblerData is present.
    /// Lets you run CombatScene directly without going through the farm flow.
    /// </summary>
    private void SpawnFallbackUnit()
    {
        AnimalData chicken = Resources.Load<AnimalData>("Animals/chicken");
        if (chicken == null)
        {
            Debug.LogWarning("[CombatTeamSpawner] Fallback: no 'Animals/chicken' asset found in Resources.");
            return;
        }

        Vector2Int pos = new Vector2Int(6, 2);
        GridCell cell = GridManager.Instance.GetCell(pos);
        if (cell == null || !cell.IsEmpty())
            pos = new Vector2Int(7, 2);

        CreateAnimalUnit("Chicken (Test)", pos, true, chicken,
            50f, 1f, 1f, 1f, 1f, 1, 0f, 1f, 1f, 1f);
    }

    /// <summary>
    /// Core spawn logic — mirrors CombatTestSpawner.CreateAnimalUnitWithStats() exactly.
    /// </summary>
    private bool CreateAnimalUnit(string customName, Vector2Int gridPos, bool isPlayer,
        AnimalData animalData,
        float happiness, float attackGrowth, float defenseGrowth, float speedGrowth,
        float healthGrowth, int level, float experience,
        float seasonalAttackMod, float seasonalDefenseMod, float seasonalSpeedMod)
    {
        // ── Validate position ─────────────────────────────────────────────────
        if (!GridManager.Instance.IsValidPosition(gridPos.x, gridPos.y))
        {
            Debug.LogError($"[CombatTeamSpawner] Invalid grid position {gridPos} for '{customName}'.");
            return false;
        }

        // ── Create GameObject ─────────────────────────────────────────────────
        // Scale starts at the Transform default (1,1,1); CombatUnit.NormalizeSpriteSize
        // (via SetupVisuals during Awake) overwrites it once it picks up animalData.idleSprite.
        GameObject unitObj = new GameObject(customName);

        // ── SpriteRenderer — assign idle sprite directly (must exist before Animal.Awake) ──
        SpriteRenderer sr = unitObj.AddComponent<SpriteRenderer>();
        sr.sprite       = animalData.idleSprite;
        sr.sortingOrder = 10; // Above grid

        // ── Animal component — set data via reflection (SerializeField) ───────
        Animal animal = unitObj.AddComponent<Animal>();

        var animalDataField = typeof(Animal).GetField("animalData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        animalDataField?.SetValue(animal, animalData);

        // Set custom name for player units only
        if (isPlayer)
        {
            var customNameField = typeof(Animal).GetField("customName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            customNameField?.SetValue(animal, customName);
        }

        // Initialize combat stats (same as CombatTestSpawner)
        var combatStatsField = typeof(Animal).GetField("combatStats",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        AnimalCombatStats stats = combatStatsField?.GetValue(animal) as AnimalCombatStats;
        if (stats != null && animalData.baseCombatStats != null)
        {
            stats.Initialize(animalData.baseCombatStats);
            stats.happiness       = happiness;
            stats.attackGrowth    = attackGrowth;
            stats.defenseGrowth   = defenseGrowth;
            stats.speedGrowth     = speedGrowth;
            stats.healthGrowth    = healthGrowth;
            stats.level           = level;
            stats.experience      = experience;
            stats.seasonalAttackMod  = seasonalAttackMod;
            stats.seasonalDefenseMod = seasonalDefenseMod;
            stats.seasonalSpeedMod   = seasonalSpeedMod;
        }

        // ── CombatUnit — initialize from Animal ───────────────────────────────
        CombatUnit combatUnit = unitObj.AddComponent<CombatUnit>();
        combatUnit.isPlayerUnit = isPlayer;
        combatUnit.InitializeFromAnimal(animal, isPlayer);
        combatUnit.InitializePlayerSkill(animal.AnimalData?.activeSkill);
        ApplyUnlockedPassiveSkills(animal, combatUnit);

        // Apply illness stat penalty (attack/defense/health -%, speed unchanged)
        if (animal.IsIll && animalData.illnessStatPenalty < 1f)
            combatUnit.ApplyStatMultiplier(animalData.illnessStatPenalty);

        // ── Place on grid (also assigns healthBarPrefab and creates health bar) ─
        bool placed = GridManager.Instance.PlaceUnitAt(combatUnit, gridPos);
        if (!placed)
        {
            Destroy(unitObj);
            Debug.LogError($"[CombatTeamSpawner] PlaceUnitAt({gridPos}) failed for '{customName}'.");
            return false;
        }

        if (showDebugLogs)
            Debug.Log($"[CombatTeamSpawner] Spawned '{customName}' at {gridPos}, world pos={unitObj.transform.position}.");
        return true;
    }

    /// <summary>Buff duration (in TickStatusEffects calls) used for effectively-permanent buffs.</summary>
    private const int PermanentBuffDuration = int.MaxValue;

    /// <summary>
    /// Apply permanent stat buffs for any of this animal's passive skills whose unlock
    /// condition is currently met (e.g. combat class or family-count requirements).
    /// </summary>
    private void ApplyUnlockedPassiveSkills(Animal animal, CombatUnit combatUnit)
    {
        if (animal?.AnimalData?.availablePassiveSkills == null) return;

        int familyCountInRoster = (AnimalRoster.Instance != null && animal.AnimalData != null)
            ? AnimalRoster.Instance.GetFamilyCount(animal.AnimalData.animalFamily)
            : 0;

        // No season-singleton exists yet — Season-type unlock conditions simply won't trigger.
        const string currentSeason = "";

        foreach (AnimalSkill skill in animal.AnimalData.availablePassiveSkills)
        {
            if (skill == null || skill.skillType != SkillType.Passive) continue;
            if (!skill.CanUnlock(animal, familyCountInRoster, currentSeason)) continue;

            if (skill.attackMultiplier != 1f || skill.defenseMultiplier != 1f || skill.speedMultiplier != 1f)
                combatUnit.ApplyStatBuff(skill.attackMultiplier, skill.defenseMultiplier, skill.speedMultiplier, PermanentBuffDuration);
        }
    }

    /// <summary>Clears and respawns all units (for battle restart).</summary>
    public void RespawnTeams()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.ClearAllUnits();
            foreach (var u in FindObjectsByType<CombatUnit>(FindObjectsSortMode.None))
                Destroy(u.gameObject);
        }
        SpawnTeams();
    }
}

} // namespace SowurShield.Combat
