using UnityEngine;
using UnityEngine.UI;
using SowurShield.Combat;
using SowurShield.Core;

namespace SowurShield.Worldmap
{

[RequireComponent(typeof(Button))]
public class StageButton : MonoBehaviour
{
    public string stageName;
    public GameObject WorldMap;

    [SerializeField] private Image buttonImage;
    [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private Color unlockedColor = Color.white;

    // Locked stage names were unreadable, but not for the reason the field names suggest.
    // lockedColor above never reaches the screen: Unity's Button tints the same Image on top
    // of it, and its disabledColor is (0.784, 0.784, 0.784, 0.502) — half transparent. A
    // locked button is therefore a PALE, see-through panel with the illustrated map showing
    // through it, not a dark one. The fix is to make that panel opaque again so the dark ink
    // has a flat field to sit on; going light on the text (tried first) made it worse.
    // Dark ink on both states, because both panels are light. Locked is lifted slightly off
    // the unlocked ink so the two states still read as different without sacrificing
    // legibility. Measured, not guessed: on the 0.72 panel, 0.35 gives 3.5:1 and 0.30 gives
    // 4.3:1 — both below the 4.5:1 floor. 0.24 measures 5.5:1, leaving room to spare.
    [SerializeField] private Color lockedTextColor = new Color(0.24f, 0.23f, 0.21f);
    [SerializeField] private Color unlockedTextColor = new Color(0.196f, 0.196f, 0.196f);

    // Replaces the Button's translucent default so locked stages stay legible over the map.
    [SerializeField] private Color lockedDisabledTint = new Color(0.72f, 0.71f, 0.68f, 1f);

    private Button button;
    private TMPro.TextMeshProUGUI label;

    private void Awake()
    {
        button = GetComponent<Button>();
        // Cached here rather than in RefreshLockState: OnEnable fires on every map open and
        // GetComponentInChildren walks the hierarchy each time.
        label = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
    }

    private void OnEnable()
    {
        StageManager.LoadAllStages();
        RefreshLockState();
    }

    private void RefreshLockState()
    {
        StageData stage = StageManager.GetStageByName(stageName);
        bool locked = IsLocked(stage);
        button.interactable = !locked;

        // Opaque disabled tint. Left at Unity's default the locked panel is 50% transparent
        // and the map's foliage reads straight through the label.
        ColorBlock colors = button.colors;
        if (colors.disabledColor != lockedDisabledTint)
        {
            colors.disabledColor = lockedDisabledTint;
            button.colors = colors;
        }

        // Image colour stays white for both states: the Button's tint multiplies this, so
        // applying lockedColor here as well would double-darken the panel back to mud.
        // lockedColor/unlockedColor are kept only for callers that drive the visual directly.
        if (buttonImage != null)
            buttonImage.color = unlockedColor;

        // Clones are instantiated before Awake runs on them in some paths, so re-resolve
        // rather than trusting the cache to be populated.
        if (label == null) label = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (label != null)
            label.color = locked ? lockedTextColor : unlockedTextColor;
    }

    private bool IsLocked(StageData stage)
    {
        if (stage == null) return false;
        if (stage.unlockedByDefault) return false;
        if (stage.prerequisiteStage == null) return false;
        var flags = SaveManager.Instance?.CurrentGameData?.worldData?.worldFlags;
        return flags == null || !flags.ContainsKey($"stage_completed_{stage.prerequisiteStage.stageName}");
    }

    public void OnClick()
    {
        StageData stage = StageManager.GetStageByName(stageName);
        if (stage == null || IsLocked(stage)) return;

        StageManager.SetSelectedStage(stage);
        TeamAssemblerUI.Instance?.OpenAssembler();
        if (WorldMap != null)
            WorldMap.SetActive(false);
    }
}

} // namespace SowurShield.Worldmap
