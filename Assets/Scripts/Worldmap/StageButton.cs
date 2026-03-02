using UnityEngine;
using SowurShield.Combat;

namespace SowurShield.Worldmap
{

public class StageButton : MonoBehaviour
{
    public string stageName;
    public GameObject WorldMap;

    public void OnClick()
    {
        StageManager.LoadAllStages();

        StageData stage = StageManager.GetStageByName(stageName);
        if (stage != null)
            StageManager.SetSelectedStage(stage);
        else
            Debug.LogWarning($"[StageButton] Stage '{stageName}' not found in StageManager.");

        TeamAssemblerUI.Instance.OpenAssembler();
        WorldMap.SetActive(false);
    }
}

} // namespace SowurShield.Worldmap
