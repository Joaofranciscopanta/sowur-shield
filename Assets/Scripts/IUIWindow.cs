using UnityEngine;

/// <summary>
/// Interface for all UI windows that need to be managed by the UIManager
/// Provides standardized window management, priority handling, and ESC key behavior
/// </summary>
public interface IUIWindow
{
    /// <summary>
    /// Unique name identifier for this window (for debugging and logging)
    /// </summary>
    string WindowName { get; }

    /// <summary>
    /// Priority level for this window. Higher numbers = higher priority.
    /// Priority determines which window closes first when ESC is pressed.
    ///
    /// Recommended priorities:
    /// - GameMenu: 1 (lowest priority, closes first)
    /// - Inventory: 10
    /// - SellBox: 20
    /// - Dialogue: 30 (highest priority for important conversations)
    /// </summary>
    int WindowPriority { get; }

    /// <summary>
    /// Whether this window is currently open and visible
    /// </summary>
    bool IsWindowOpen { get; }

    /// <summary>
    /// Whether this window can be closed with the ESC key
    /// Most windows should return true, but some critical dialogs might return false
    /// </summary>
    bool CanCloseWithEsc { get; }

    /// <summary>
    /// Open this window. Should handle all opening logic including UI activation,
    /// player input disabling, cursor management, etc.
    /// </summary>
    void OpenWindow();

    /// <summary>
    /// Close this window. Should handle all closing logic including UI deactivation,
    /// player input restoration, cursor management, etc.
    /// </summary>
    void CloseWindow();

    /// <summary>
    /// Called when this window attempted to open but was blocked by another window.
    /// Can be used to show feedback to the player or queue the window for later.
    /// </summary>
    /// <param name="blockedBy">Name of the window that blocked this one</param>
    void OnWindowBlocked(string blockedBy);
}

/// <summary>
/// Window priority constants for consistency across the project
/// </summary>
public static class WindowPriority
{
    public const int GameMenu = 1;          // Lowest priority - background menu
    public const int Inventory = 10;        // Standard UI windows
    public const int SellBox = 20;          // Interactive containers
    public const int Dialogue = 30;         // Important conversations
    public const int CriticalDialog = 100;  // System dialogs that can't be interrupted
}