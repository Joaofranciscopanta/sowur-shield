using System.Collections.Generic;
using UnityEngine;
using SowurShield.Combat;
using SowurShield.Core;

namespace SowurShield.Worldmap
{

/// <summary>
/// Controls the World Map UI panel, integrating with UIManager for consistent
/// window management (ESC close, priority, movement control).
/// Drives biome panel population on open via RefreshBiomePanels().
/// </summary>
public class WorldMapUIController : MonoBehaviour, IUIWindow
{
    // =========================================================================
    // IUIWindow
    // =========================================================================

    public string WindowName => "WorldMap";
    public int WindowPriority => SowurShield.Core.WindowPriority.Inventory; // 10
    public bool IsWindowOpen => gameObject.activeSelf;
    public bool CanCloseWithEsc => true;

    // =========================================================================
    // Biome panels
    // =========================================================================

    [Header("Biome Panels")]
    [Tooltip("One entry per biome. Assign the WorldMapBiomePanel component for each biome.")]
    [SerializeField] private List<WorldMapBiomePanel> biomePanels = new List<WorldMapBiomePanel>();

    // =========================================================================
    // Unity lifecycle
    // =========================================================================

    private void Awake()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.RegisterWindow(this);
    }

    private void OnDestroy()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UnregisterWindow(this);
    }

    // =========================================================================
    // IUIWindow implementation
    // =========================================================================

    public void OpenWindow()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;

        SyncStageProgressFromSave();
        RefreshBiomePanels();
    }

    public void CloseWindow()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
        GameMenuManager.Instance?.SetMapOpen(false);
    }

    public void OnWindowBlocked(string blockedBy) { }

    // =========================================================================
    // Public map helpers
    // =========================================================================

    /// <summary>
    /// Open the world map via UIManager (preferred) or directly as fallback.
    /// </summary>
    public void OpenMap()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TryOpenWindow(this);
        else
            OpenWindow();
    }

    /// <summary>
    /// Close the world map via UIManager (preferred) or directly as fallback.
    /// </summary>
    public void CloseMap()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TryCloseWindow(this);
        else
            CloseWindow();
    }

    // =========================================================================
    // Stage progress sync
    // =========================================================================

    /// <summary>
    /// Reads stage_completed_* world flags from the current save and pushes
    /// them into StageManager so the UI reflects the saved state.
    /// </summary>
    private void SyncStageProgressFromSave()
    {
        var flags = SaveManager.Instance?.CurrentGameData?.worldData?.worldFlags;
        if (flags == null) return;

        var progress = new StageProgressData();
        progress.completedStages = new List<string>();

        const string prefix = "stage_completed_";
        foreach (KeyValuePair<string, bool> kvp in flags)
        {
            if (kvp.Key.StartsWith(prefix) && kvp.Value)
                progress.completedStages.Add(kvp.Key.Substring(prefix.Length));
        }

        StageManager.LoadSaveData(progress);
    }

    // =========================================================================
    // Biome panel refresh
    // =========================================================================

    /// <summary>
    /// Loads all stages, groups them by theme, then drives each
    /// WorldMapBiomePanel to rebuild its stage buttons.
    /// </summary>
    private void RefreshBiomePanels()
    {
        if (biomePanels == null || biomePanels.Count == 0) return;

        // Ensure the stage cache is populated (idempotent call).
        StageManager.LoadAllStages();

        // Load all stage assets directly so we have full objects with theme info.
        StageData[] allStages = Resources.LoadAll<StageData>("Stages");

        // Group stages by theme.
        Dictionary<StageTheme, List<StageData>> byTheme = new Dictionary<StageTheme, List<StageData>>();
        foreach (StageData stage in allStages)
        {
            if (stage == null) continue;
            if (!byTheme.ContainsKey(stage.theme))
                byTheme[stage.theme] = new List<StageData>();
            byTheme[stage.theme].Add(stage);
        }

        // Sort each biome's stage list by stageNumber for consistent display order.
        foreach (List<StageData> list in byTheme.Values)
            list.Sort((a, b) => a.stageNumber.CompareTo(b.stageNumber));

        HashSet<string> completed = StageManager.GetCompletedStages();

        // Populate each panel with the stages that match its theme.
        foreach (WorldMapBiomePanel panel in biomePanels)
        {
            if (panel == null) continue;

            List<StageData> matching;
            if (!byTheme.TryGetValue(panel.BiomeTheme, out matching))
                matching = new List<StageData>();

            panel.Populate(matching, completed);
        }
    }
}

} // namespace SowurShield.Worldmap
