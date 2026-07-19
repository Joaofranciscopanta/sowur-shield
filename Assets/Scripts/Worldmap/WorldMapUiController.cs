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
    /// WorldMapBiomePanel to rebuild its stage buttons. If no biome panels
    /// are configured, falls back to RefreshFlatStageButtons() so every
    /// stage is still reachable from the map.
    /// </summary>
    private void RefreshBiomePanels()
    {
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

        if (biomePanels == null || biomePanels.Count == 0)
        {
            // No per-biome panels configured in the scene - fall back to a flat
            // grid of stage buttons (grouped by biome row) so every stage
            // remains reachable.
            RefreshFlatStageButtons(allStages);
            return;
        }

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

    // =========================================================================
    // Flat stage button fallback (used when biomePanels is empty)
    // =========================================================================

    [Header("Flat Layout Fallback")]
    [Tooltip("Template StageButton (e.g. StageButton_SunnyFields) cloned for every stage when no biome panels are configured. If left empty, the controller will search its own children for a StageButton on Awake.")]
    [SerializeField] private StageButton flatLayoutTemplate;

    [Tooltip("Pixel size (width, height) of each generated stage button in the flat layout.")]
    [SerializeField] private Vector2 flatButtonCellSize = new Vector2(140f, 60f);

    [Tooltip("Horizontal/vertical spacing between generated stage buttons.")]
    [SerializeField] private Vector2 flatButtonSpacing = new Vector2(20f, 20f);

    [Tooltip("Top-left starting offset (relative to the template's parent) for the generated grid.")]
    [SerializeField] private Vector2 flatLayoutOrigin = new Vector2(40f, -40f);

    private const string GeneratedButtonPrefix = "StageButton_Generated_";

    /// <summary>
    /// Builds (or rebuilds) a flat grid of stage buttons, one row per biome
    /// theme, so all stages remain accessible even without dedicated
    /// WorldMapBiomePanel containers.
    /// </summary>
    private void RefreshFlatStageButtons(StageData[] allStages)
    {
        StageButton template = ResolveFlatLayoutTemplate();
        if (template == null || allStages == null) return;

        Transform parent = template.transform.parent;
        if (parent == null) return;

        // Remove any buttons generated by a previous refresh.
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name.StartsWith(GeneratedButtonPrefix))
                Destroy(child.gameObject);
        }

        // Group + sort stages by theme/stageNumber for a stable row order.
        Dictionary<StageTheme, List<StageData>> byTheme = new Dictionary<StageTheme, List<StageData>>();
        foreach (StageData stage in allStages)
        {
            if (stage == null) continue;
            if (!byTheme.ContainsKey(stage.theme))
                byTheme[stage.theme] = new List<StageData>();
            byTheme[stage.theme].Add(stage);
        }
        foreach (List<StageData> list in byTheme.Values)
            list.Sort((a, b) => a.stageNumber.CompareTo(b.stageNumber));

        List<StageTheme> themesInOrder = new List<StageTheme>(byTheme.Keys);
        themesInOrder.Sort((a, b) => a.ToString().CompareTo(b.ToString()));

        int row = 0;
        foreach (StageTheme theme in themesInOrder)
        {
            List<StageData> stages = byTheme[theme];
            for (int col = 0; col < stages.Count; col++)
            {
                StageData stage = stages[col];
                if (stage == null) continue;

                bool isFirst = row == 0 && col == 0;
                GameObject buttonGO = isFirst ? template.gameObject : Instantiate(template.gameObject, parent);
                if (!isFirst)
                    buttonGO.name = GeneratedButtonPrefix + stage.stageName;

                ConfigureFlatStageButton(buttonGO, stage, row, col);
            }

            row++;
        }
    }

    /// <summary>
    /// Positions a (possibly cloned) stage button in the flat grid and wires
    /// its stageName, label text, and WorldMap reference.
    /// </summary>
    private void ConfigureFlatStageButton(GameObject buttonGO, StageData stage, int row, int col)
    {
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            float x = flatLayoutOrigin.x + col * (flatButtonCellSize.x + flatButtonSpacing.x);
            float y = flatLayoutOrigin.y - row * (flatButtonCellSize.y + flatButtonSpacing.y);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = flatButtonCellSize;
        }

        StageButton stageButton = buttonGO.GetComponent<StageButton>();
        if (stageButton != null)
        {
            stageButton.stageName = stage.stageName;
            stageButton.WorldMap = gameObject;

            // StageButton.OnEnable() reads stageName to set up its locked/unlocked
            // visuals. Clones are instantiated active with the template's old
            // stageName, so re-trigger OnEnable now that stageName is correct.
            if (buttonGO.activeInHierarchy)
            {
                buttonGO.SetActive(false);
                buttonGO.SetActive(true);
            }
        }

        // Update the button's label (TextMeshProUGUI child) to the stage name.
        TMPro.TextMeshProUGUI label = buttonGO.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (label != null)
            label.text = stage.GetDisplayName();
    }

    /// <summary>
    /// Returns the configured flatLayoutTemplate, or searches this controller's
    /// children for the first StageButton found if none was assigned.
    /// </summary>
    private StageButton ResolveFlatLayoutTemplate()
    {
        if (flatLayoutTemplate != null) return flatLayoutTemplate;

        flatLayoutTemplate = GetComponentInChildren<StageButton>(true);
        return flatLayoutTemplate;
    }
}

} // namespace SowurShield.Worldmap
