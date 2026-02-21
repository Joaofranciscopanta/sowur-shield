using UnityEngine;

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
