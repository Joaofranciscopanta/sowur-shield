using UnityEngine;
using SowurShield.Core;

namespace SowurShield.Worldmap
{

public class WorldMapUIController : MonoBehaviour
{
    void Update()
    {
        if (!gameObject.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseMap();
        }
    }

    public void CloseMap()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f; // volta o jogo
        GameMenuManager.Instance.SetMapOpen(false);
    }
}

} // namespace SowurShield.Worldmap
