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
        TeamAssemblerUI.Instance.OpenAssembler();
        WorldMap.SetActive(false);
    }
}

} // namespace SowurShield.Worldmap
