using UnityEngine;
using SowurShield.Core;

namespace SowurShield.Worldmap
{

/// <summary>
/// Controls the World Map UI panel, integrating with UIManager for consistent
/// window management (ESC close, priority, movement control).
/// </summary>
public class WorldMapUIController : MonoBehaviour, IUIWindow
{
    // IUIWindow
    public string WindowName => "WorldMap";
    public int WindowPriority => SowurShield.Core.WindowPriority.Inventory; // 10
    public bool IsWindowOpen => gameObject.activeSelf;
    public bool CanCloseWithEsc => true;

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

    public void OpenWindow()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    public void CloseWindow()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
        GameMenuManager.Instance?.SetMapOpen(false);
    }

    public void OnWindowBlocked(string blockedBy) { }

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
}

} // namespace SowurShield.Worldmap
