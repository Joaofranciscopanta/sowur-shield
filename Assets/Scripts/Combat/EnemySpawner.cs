using UnityEngine;
using System.Collections.Generic;

namespace SowurShield.Combat
{

/// <summary>
/// Spawns enemy units on the combat grid at battle start.
/// Reads enemy configuration from StageManager.GetSelectedStage().
/// Falls back to 2 hardcoded test enemies if no stage is selected.
///
/// SETUP IN UNITY:
/// 1. Add this script to any GameObject in the Combat scene
/// 2. No additional configuration required
/// 3. Enemies spawn at 0.6s (after player team at 0.5s, before TurnManager at 1.0s)
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // Pre-defined enemy spawn positions (columns 0-5, left side)
    private static readonly Vector2Int[] EnemyPositions = new Vector2Int[]
    {
        new Vector2Int(2, 2),
        new Vector2Int(4, 2),
        new Vector2Int(1, 1),
        new Vector2Int(3, 3),
        new Vector2Int(5, 1),
        new Vector2Int(0, 2),
        new Vector2Int(2, 0),
        new Vector2Int(4, 4),
    };

    private void Start()
    {
        // A prior bug had Time.timeScale left at 0 when entering CombatScene, which silently
        // prevents Invoke() from ever firing. Self-heals but stays loud since it points at a
        // real bug elsewhere (something left the game paused) if it ever fires again.
        if (Time.timeScale == 0f)
        {
            Debug.LogWarning("[EnemySpawner] Time.timeScale is 0 at Start() — forcing 1f so spawning can proceed.");
            Time.timeScale = 1f;
        }
        // 0.6f: after player team (0.5f), before TurnManager (1.0f)
        Invoke(nameof(SpawnEnemies), 0.6f);
    }

    private void SpawnEnemies()
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("[EnemySpawner] GridManager.Instance is null!");
            return;
        }

        StageData stage = StageManager.GetSelectedStage();

        // In builds, static fields may be cleared on scene load — restore from TeamAssemblerData
        if (stage == null)
        {
            StageManager.LoadAllStages();
            string savedName = TeamAssemblerData.Instance?.selectedStageName;
            if (!string.IsNullOrEmpty(savedName))
            {
                StageData restored = StageManager.GetStageByName(savedName);
                if (restored != null) StageManager.SetSelectedStage(restored);
                else Debug.LogWarning($"[EnemySpawner] StageManager.GetStageByName('{savedName}') returned null.");
            }
            stage = StageManager.GetSelectedStage();
        }

        if (stage == null || stage.enemySpawns == null || stage.enemySpawns.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] No stage selected or stage has no enemy spawns — using fallback test enemies.");
            try
            {
                SpawnFallbackEnemies();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EnemySpawner] Exception in SpawnFallbackEnemies(): {ex}");
            }
            return;
        }

        try
        {
            SpawnBackground(stage);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[EnemySpawner] Exception in SpawnBackground(): {ex}");
        }

        try
        {
            SpawnFromStage(stage);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[EnemySpawner] Exception in SpawnFromStage(): {ex}");
        }
    }

    private void SpawnBackground(StageData stage)
    {
        if (stage.backgroundSprite == null) return;

        GameObject bg = new GameObject("CombatBackground");
        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = stage.backgroundSprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = -20; // Behind grid cells (-10) and units (10)

        // Scale to fill camera view (orthographic size * 2 = full height)
        Camera cam = Camera.main;
        if (cam != null)
        {
            float camHeight = cam.orthographicSize * 2f;
            float camWidth  = camHeight * cam.aspect;
            float spriteHeight = sr.sprite.bounds.size.y;
            float spriteWidth  = sr.sprite.bounds.size.x;
            float scaleY = camHeight / spriteHeight;
            float scaleX = camWidth  / spriteWidth;
            float scale  = Mathf.Max(scaleX, scaleY); // Cover, not contain
            bg.transform.localScale = Vector3.one * scale;
        }

        bg.transform.position = Vector3.zero;
    }

    private void SpawnFromStage(StageData stage)
    {
        int totalEnemies = Random.Range(stage.minTotalEnemies, stage.maxTotalEnemies + 1);
        totalEnemies = Mathf.Clamp(totalEnemies, 1, EnemyPositions.Length);

        // Build weighted spawn pool
        List<EnemyData> pool = BuildSpawnPool(stage.enemySpawns, totalEnemies);
        if (showDebugLogs)
            Debug.Log($"[EnemySpawner] SpawnFromStage('{stage.stageName}') — totalEnemies={totalEnemies}, pool.Count={pool.Count}");

        int posIndex = 0;
        int spawned = 0;
        foreach (EnemyData enemyData in pool)
        {
            if (posIndex >= EnemyPositions.Length) break;

            Vector2Int pos = GetNextEmptyEnemyPosition(ref posIndex);
            if (pos.x < 0) break; // No free positions

            // Wrap per-enemy spawn in try/catch: an uncaught exception here would
            // silently abort the whole foreach loop, leaving GridManager.GetAllUnits()
            // with fewer (or zero) enemy units in builds with no visible in-game error.
            bool ok = false;
            try
            {
                ok = SpawnEnemy(enemyData, pos, stage.difficulty);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EnemySpawner] Exception spawning '{enemyData?.enemyName}' at {pos}: {ex}");
            }

            if (ok) spawned++;
        }
        if (showDebugLogs)
            Debug.Log($"[EnemySpawner] SpawnFromStage done — spawned {spawned} enemies.");
    }

    /// <summary>
    /// Build a flat list of EnemyData to spawn, respecting weights and min/max counts.
    /// </summary>
    private List<EnemyData> BuildSpawnPool(List<EnemySpawn> spawns, int totalCount)
    {
        List<EnemyData> result = new List<EnemyData>();

        // First, respect minimum counts
        foreach (var spawn in spawns)
        {
            if (spawn.enemy == null) continue;
            int min = Mathf.Clamp(spawn.minCount, 0, totalCount - result.Count);
            for (int i = 0; i < min && result.Count < totalCount; i++)
                result.Add(spawn.enemy);
        }

        // Then fill remaining slots by weight
        float totalWeight = 0f;
        foreach (var spawn in spawns)
        {
            if (spawn.enemy != null) totalWeight += spawn.spawnWeight;
        }

        while (result.Count < totalCount && totalWeight > 0f)
        {
            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var spawn in spawns)
            {
                if (spawn.enemy == null) continue;
                cumulative += spawn.spawnWeight;
                if (roll <= cumulative)
                {
                    // Respect max count for this enemy type
                    int currentCount = result.FindAll(e => e == spawn.enemy).Count;
                    if (currentCount < spawn.maxCount)
                        result.Add(spawn.enemy);
                    break;
                }
            }

            // Safety: if we can't fill any more (all at max), break
            bool anyAvailable = false;
            foreach (var spawn in spawns)
            {
                if (spawn.enemy == null) continue;
                int c = result.FindAll(e => e == spawn.enemy).Count;
                if (c < spawn.maxCount) { anyAvailable = true; break; }
            }
            if (!anyAvailable) break;
        }

        return result;
    }

    /// <summary>
    /// Spawn 2 generic test enemies when no stage data is available.
    /// </summary>
    private void SpawnFallbackEnemies()
    {
        SpawnGenericEnemy("Enemy_1", new Vector2Int(2, 2), 100f, 10f, 5f, 10f);
        SpawnGenericEnemy("Enemy_2", new Vector2Int(4, 2), 80f, 12f, 4f, 12f);
    }

    private bool SpawnEnemy(EnemyData enemyData, Vector2Int pos, int difficulty)
    {
        GameObject unitObj = new GameObject(enemyData.enemyName);
        unitObj.transform.localScale = Vector3.one;

        // ── SpriteRenderer ────────────────────────────────────────────────────
        SpriteRenderer sr = unitObj.AddComponent<SpriteRenderer>();
        sr.sprite       = enemyData.sprite;
        sr.sortingOrder = 10;

        // ── CombatUnit ────────────────────────────────────────────────────────
        CombatUnit combatUnit = unitObj.AddComponent<CombatUnit>();
        combatUnit.isPlayerUnit = false;

        var (hp, atk, def, spd) = enemyData.GetScaledStats(difficulty);
        combatUnit.InitializeAsEnemy(enemyData.GetDisplayName(), hp, atk, def, spd, enemyData.GetScaledAccuracy(difficulty));

        // Sprite size is normalized by CombatUnit itself (NormalizeSpriteSize, called from
        // SetupVisuals during Awake) once it picks up the SpriteRenderer set above.
        combatUnit.visualObject = unitObj;

        // ── Skills ────────────────────────────────────────────────────────────
        combatUnit.InitializeEnemySkills(enemyData.skills, enemyData.GetScaledSkillUseChance(difficulty));
        combatUnit.InitializeImmunities(enemyData.immunities);
        combatUnit.SetAIBehavior(enemyData.aiBehavior);

        // ── Place on grid ─────────────────────────────────────────────────────
        bool placed = GridManager.Instance.PlaceUnitAt(combatUnit, pos);
        if (!placed)
        {
            Destroy(unitObj);
            Debug.LogError($"[EnemySpawner] PlaceUnitAt({pos}) failed for '{enemyData.enemyName}'.");
            return false;
        }

        return true;
    }

    private bool SpawnGenericEnemy(string name, Vector2Int pos, float hp, float atk, float def, float spd)
    {
        if (!GridManager.Instance.IsValidPosition(pos.x, pos.y))
        {
            Debug.LogError($"[EnemySpawner] Invalid fallback position {pos}.");
            return false;
        }

        GridCell cell = GridManager.Instance.GetCell(pos);
        if (cell == null || !cell.IsEmpty())
        {
            Debug.LogWarning($"[EnemySpawner] Fallback position {pos} is occupied, skipping '{name}'.");
            return false;
        }

        GameObject unitObj = new GameObject(name);
        unitObj.transform.localScale = Vector3.one * 0.5f;

        CombatUnit combatUnit = unitObj.AddComponent<CombatUnit>();
        combatUnit.isPlayerUnit = false;
        combatUnit.InitializeAsEnemy(name, hp, atk, def, spd);

        bool placed = GridManager.Instance.PlaceUnitAt(combatUnit, pos);
        if (!placed)
        {
            Destroy(unitObj);
            Debug.LogError($"[EnemySpawner] Fallback PlaceUnitAt({pos}) failed for '{name}'.");
            return false;
        }

        return true;
    }

    private Vector2Int GetNextEmptyEnemyPosition(ref int posIndex)
    {
        while (posIndex < EnemyPositions.Length)
        {
            Vector2Int pos = EnemyPositions[posIndex++];
            GridCell cell = GridManager.Instance.GetCell(pos);
            if (cell != null && cell.IsEmpty())
                return pos;
        }
        return new Vector2Int(-1, -1); // No position found
    }
}

} // namespace SowurShield.Combat
