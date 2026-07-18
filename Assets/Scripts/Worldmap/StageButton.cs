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

    private Button button;

    private void Awake() => button = GetComponent<Button>();

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
        if (buttonImage != null)
            buttonImage.color = locked ? lockedColor : unlockedColor;
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
