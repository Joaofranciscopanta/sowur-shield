using UnityEngine;
using System.Collections.Generic;

namespace SowurShield.Combat
{

/// <summary>
/// Manages stage selection, completion status, and progression.
/// Persists between scenes and integrates with save system.
/// </summary>
public static class StageManager
{
    // ============================================================================
    // STATIC DATA
    // ============================================================================

    // Currently selected stage for combat
    private static StageData selectedStage;

    // Set of completed stage names (for persistence)
    private static HashSet<string> completedStages = new HashSet<string>();

    // Cache of all loaded stages
    private static Dictionary<int, StageData> stageCache = new Dictionary<int, StageData>();

    // ============================================================================
    // STAGE SELECTION
    // ============================================================================

    /// <summary>
    /// Set the currently selected stage for combat
    /// </summary>
    public static void SetSelectedStage(StageData stage)
    {
        selectedStage = stage;
        Debug.Log($"[StageManager] Selected stage: {stage?.stageName ?? "None"}");
    }

    /// <summary>
    /// Get the currently selected stage
    /// </summary>
    public static StageData GetSelectedStage()
    {
        return selectedStage;
    }

    /// <summary>
    /// Clear the selected stage
    /// </summary>
    public static void ClearSelectedStage()
    {
        selectedStage = null;
    }

    // ============================================================================
    // STAGE COMPLETION
    // ============================================================================

    /// <summary>
    /// Mark a stage as completed
    /// </summary>
    public static void CompleteStage(StageData stage)
    {
        if (stage == null) return;

        string stageKey = GetStageKey(stage);
        if (!completedStages.Contains(stageKey))
        {
            completedStages.Add(stageKey);
            Debug.Log($"[StageManager] Stage completed: {stage.stageName}");
        }
    }

    /// <summary>
    /// Check if a stage is completed
    /// </summary>
    public static bool IsStageCompleted(StageData stage)
    {
        if (stage == null) return false;
        return completedStages.Contains(GetStageKey(stage));
    }

    /// <summary>
    /// Check if a stage is completed by name
    /// </summary>
    public static bool IsStageCompleted(string stageName)
    {
        return completedStages.Contains(stageName);
    }

    /// <summary>
    /// Get all completed stages
    /// </summary>
    public static HashSet<string> GetCompletedStages()
    {
        return new HashSet<string>(completedStages);
    }

    /// <summary>
    /// Get total completed stage count
    /// </summary>
    public static int GetCompletedCount()
    {
        return completedStages.Count;
    }

    // ============================================================================
    // STAGE CACHE
    // ============================================================================

    /// <summary>
    /// Register a stage in the cache
    /// </summary>
    public static void RegisterStage(StageData stage)
    {
        if (stage == null) return;

        if (!stageCache.ContainsKey(stage.stageNumber))
        {
            stageCache[stage.stageNumber] = stage;
        }
    }

    /// <summary>
    /// Get a stage by number
    /// </summary>
    public static StageData GetStageByNumber(int stageNumber)
    {
        stageCache.TryGetValue(stageNumber, out StageData stage);
        return stage;
    }

    /// <summary>
    /// Get total registered stages
    /// </summary>
    public static int GetTotalStages()
    {
        return stageCache.Count;
    }

    /// <summary>
    /// Load all stages from Resources folder
    /// </summary>
    public static void LoadAllStages()
    {
        stageCache.Clear();

        StageData[] stages = Resources.LoadAll<StageData>("Stages");
        foreach (StageData stage in stages)
        {
            RegisterStage(stage);
        }

        Debug.Log($"[StageManager] Loaded {stages.Length} stages from Resources");
    }

    // ============================================================================
    // SAVE/LOAD
    // ============================================================================

    /// <summary>
    /// Get save data for persistence
    /// </summary>
    public static StageProgressData GetSaveData()
    {
        return new StageProgressData
        {
            completedStages = new List<string>(completedStages),
            selectedStageName = selectedStage?.stageName ?? ""
        };
    }

    /// <summary>
    /// Load save data
    /// </summary>
    public static void LoadSaveData(StageProgressData data)
    {
        if (data == null) return;

        completedStages.Clear();
        if (data.completedStages != null)
        {
            foreach (string stageName in data.completedStages)
            {
                completedStages.Add(stageName);
            }
        }

        Debug.Log($"[StageManager] Loaded progress: {completedStages.Count} stages completed");
    }

    /// <summary>
    /// Reset all progress (for new game)
    /// </summary>
    public static void ResetProgress()
    {
        completedStages.Clear();
        selectedStage = null;
        Debug.Log("[StageManager] Progress reset");
    }

    // ============================================================================
    // HELPERS
    // ============================================================================

    private static string GetStageKey(StageData stage)
    {
        // Use stage name as key (could also use a unique ID if needed)
        return stage.stageName;
    }
}

/// <summary>
/// Serializable data structure for saving stage progress
/// </summary>
[System.Serializable]
public class StageProgressData
{
    public List<string> completedStages = new List<string>();
    public string selectedStageName = "";
}

} // namespace SowurShield.Combat
