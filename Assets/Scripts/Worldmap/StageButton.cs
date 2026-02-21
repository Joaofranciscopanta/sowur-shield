using UnityEngine;

public class StageButton : MonoBehaviour
{
    public string stageName;
    public GameObject WorldMap;

    public void OnClick()
    {
        Debug.Log("Teste");
        TeamAssemblerUI.Instance.OpenAssembler();
        WorldMap.SetActive(false);
    }
}
