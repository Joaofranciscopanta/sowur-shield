using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.Combat;

namespace SowurShield.Worldmap
{

/// <summary>
/// Represents one biome's expandable panel inside the World Map UI.
/// Assign one panel per biome theme in the WorldMapUIController inspector.
/// </summary>
public class WorldMapBiomePanel : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("Biome Identity")]
    [SerializeField] private string biomeName = "Meadow";
    [SerializeField] private StageTheme biomeTheme = StageTheme.Meadow;

    [Header("Stage Button Container")]
    [Tooltip("Parent transform where StageButton GameObjects are spawned.")]
    [SerializeField] private GameObject stageButtonContainer;

    [Tooltip("Prefab that contains a StageButton component.")]
    [SerializeField] private GameObject stageButtonPrefab;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI biomeNameText;
    [SerializeField] private TextMeshProUGUI completionText;

    [Tooltip("Toggles stageButtonContainer visibility when clicked.")]
    [SerializeField] private Button expandButton;

    [Tooltip("Shown when no stage in this biome is currently unlocked.")]
    [SerializeField] private Image lockIcon;

    // -------------------------------------------------------------------------
    // Public read-only accessors (useful for WorldMapUIController)
    // -------------------------------------------------------------------------

    public StageTheme BiomeTheme => biomeTheme;

    // -------------------------------------------------------------------------
    // Internal state
    // -------------------------------------------------------------------------

    private bool isExpanded = false;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (expandButton != null)
            expandButton.onClick.AddListener(ToggleExpand);
    }

    private void OnDestroy()
    {
        if (expandButton != null)
            expandButton.onClick.RemoveListener(ToggleExpand);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Clears and rebuilds all stage buttons for this biome panel.
    /// Called by WorldMapUIController.RefreshBiomePanels() each time the map opens.
    /// </summary>
    /// <param name="stages">All StageData assets that belong to this biome.</param>
    /// <param name="completedStageNames">Set of stage names already completed by the player.</param>
    public void Populate(List<StageData> stages, HashSet<string> completedStageNames)
    {
        // --- Header text ---
        if (biomeNameText != null)
            biomeNameText.text = biomeName;

        // --- Clear existing buttons ---
        if (stageButtonContainer != null)
        {
            for (int i = stageButtonContainer.transform.childCount - 1; i >= 0; i--)
                Destroy(stageButtonContainer.transform.GetChild(i).gameObject);
        }

        // --- Spawn one button per stage ---
        if (stageButtonPrefab != null && stageButtonContainer != null && stages != null)
        {
            foreach (StageData stage in stages)
            {
                if (stage == null) continue;

                GameObject buttonGO = Instantiate(stageButtonPrefab, stageButtonContainer.transform);
                StageButton stageButton = buttonGO.GetComponent<StageButton>();
                if (stageButton != null)
                {
                    stageButton.stageName = stage.stageName;

                    // Wire the WorldMap reference so StageButton can close the map on click.
                    // Walk up to find the WorldMapUIController on a parent or sibling.
                    WorldMapUIController controller = GetComponentInParent<WorldMapUIController>();
                    if (controller != null)
                        stageButton.WorldMap = controller.gameObject;
                }
            }
        }

        // --- Completion counter ---
        int completedCount = BiomeUnlockChecker.GetCompletedCount(stages, completedStageNames);
        int total = stages != null ? stages.Count : 0;
        if (completionText != null)
            completionText.text = $"{completedCount}/{total} completed";

        // --- Lock icon ---
        bool unlocked = BiomeUnlockChecker.IsBiomeUnlocked(stages, completedStageNames);
        if (lockIcon != null)
            lockIcon.gameObject.SetActive(!unlocked);

        // Keep container in whatever expand state it was already in.
        ApplyExpandState();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void ToggleExpand()
    {
        isExpanded = !isExpanded;
        ApplyExpandState();
    }

    private void ApplyExpandState()
    {
        if (stageButtonContainer != null)
            stageButtonContainer.SetActive(isExpanded);
    }
}

} // namespace SowurShield.Worldmap
