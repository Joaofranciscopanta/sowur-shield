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

    /// <summary>
    /// Safety net for the timeScale this window sets to 0 on open. If the map is deactivated
    /// or destroyed by any path that does not run CloseWindow — a scene change, a UIManager
    /// force-close, an exception mid-open — the game would otherwise stay frozen with no
    /// visible cause. TeamAssemblerUI already had to work around exactly this (it sets
    /// timeScale back to 1 before LoadScene, or its Invoke never fires).
    /// </summary>
    private void OnDisable()
    {
        if (Time.timeScale == 0f)
            Time.timeScale = 1f;
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

    // The defaults below were measured against the real map on 2026-08-01, not guessed. The
    // map area is 1890x1080 and the grid is 5 biomes x 5 stages, so the old 140x60 cells with
    // 20px gaps packed everything into the top-left ~800x400 and overlapped every label.

    [Tooltip("Pixel size (width, height) of each generated stage button in the flat layout.")]
    [SerializeField] private Vector2 flatButtonCellSize = new Vector2(300f, 110f);

    [Tooltip("Horizontal/vertical spacing between generated stage buttons.")]
    [SerializeField] private Vector2 flatButtonSpacing = new Vector2(50f, 40f);

    [Tooltip("Top-left starting offset (relative to the template's parent) for the generated grid.")]
    [SerializeField] private Vector2 flatLayoutOrigin = new Vector2(70f, -70f);

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
        //
        // DestroyImmediate, not Destroy: Destroy is deferred to the end of the frame, so the
        // old buttons were still children while the new ones were being created below.
        // RefreshBiomePanels() runs on every OpenWindow(), so opening the map a second time
        // left 50 buttons stacked on 25 positions — each pair fighting over the same cell.
        // Same deferred-destroy trap that blanked the relationship codex earlier today.
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name.StartsWith(GeneratedButtonPrefix))
                DestroyImmediate(child.gameObject);
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

        // Centre the grid in whatever space the map actually has, instead of hanging it from a
        // fixed top-left offset. With 5 rows the block measures 710px inside a 1080px map, and
        // the fixed origin pinned it high: 70px of air above, 300px below. That imbalance is
        // what read as "the map is half empty" — the grid was not too small, it was off-centre.
        //
        // The leftover margin is deliberately *not* absorbed by growing the buttons. The empty
        // band at the bottom is the illustration's village and central path; covering it to
        // fill space would trade one problem for a worse one.
        var parentRect = parent as RectTransform;
        float gridHeight = themesInOrder.Count > 0
            ? themesInOrder.Count * flatButtonCellSize.y + (themesInOrder.Count - 1) * flatButtonSpacing.y
            : 0f;
        float originY = parentRect != null && gridHeight > 0f
            ? -Mathf.Max(flatLayoutOrigin.y * -1f, (parentRect.rect.height - gridHeight) * 0.5f)
            : flatLayoutOrigin.y;

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

                ConfigureFlatStageButton(buttonGO, stage, row, col, originY);
            }

            row++;
        }
    }

    /// <summary>
    /// Positions a (possibly cloned) stage button in the flat grid and wires
    /// its stageName, label text, and WorldMap reference.
    /// </summary>
    private void ConfigureFlatStageButton(GameObject buttonGO, StageData stage, int row, int col,
                                          float originY)
    {
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            float x = flatLayoutOrigin.x + col * (flatButtonCellSize.x + flatButtonSpacing.x);
            float y = originY - row * (flatButtonCellSize.y + flatButtonSpacing.y);
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
        {
            label.text = stage.GetDisplayName();

            // Stage names vary wildly in length once localized — "Lava Fields" next to
            // "Passagem do Gigante de Pedra" — and the template ships with a fixed 24pt font
            // set to Overflow, so the long ones spilled out of their button and over their
            // neighbours. Auto-sizing plus wrapping keeps every label inside its own cell.
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 22f;
            label.textWrappingMode = TMPro.TextWrappingModes.Normal;
            label.overflowMode = TMPro.TextOverflowModes.Truncate;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.margin = new Vector4(8f, 4f, 8f, 4f);

            // The label must fill its button, or auto-sizing measures against whatever size
            // the template happened to have.
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }
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
