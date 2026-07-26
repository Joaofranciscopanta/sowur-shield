using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SowurShield.Core
{

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Window Management")]
    [Tooltip("Logs every window open/close/block to the console. Off by default — turn on when debugging UI stacking.")]
    [SerializeField] private bool enableDebugLogs = false;

    // Window management system. The legacy parallel panel system
    // (allUIPanels/currentlyOpenPanel/OpenPanel/ClosePanel/IsAnyPanelOpen) was removed —
    // SellBox was its only writer and it already implements IUIWindow, so the stack below
    // is now the single source of truth for "what UI is open".
    private List<IUIWindow> registeredWindows = new List<IUIWindow>();
    private Stack<IUIWindow> openWindowStack = new Stack<IUIWindow>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Register a window for management. Windows should register themselves in Awake/Start.
    /// </summary>
    public void RegisterWindow(IUIWindow window)
    {
        if (window != null && !registeredWindows.Contains(window))
        {
            registeredWindows.Add(window);
            LogDebug($"Registered window: {window.WindowName} (Priority: {window.WindowPriority})");
        }
    }

    /// <summary>
    /// Unregister a window from management. Windows should unregister in OnDestroy.
    /// </summary>
    public void UnregisterWindow(IUIWindow window)
    {
        if (window != null)
        {
            registeredWindows.Remove(window);

            // Remove from stack if it's currently open
            if (openWindowStack.Contains(window))
            {
                var tempStack = new Stack<IUIWindow>();
                while (openWindowStack.Count > 0)
                {
                    var w = openWindowStack.Pop();
                    if (w != window)
                        tempStack.Push(w);
                }

                // Rebuild stack without the removed window
                while (tempStack.Count > 0)
                {
                    openWindowStack.Push(tempStack.Pop());
                }
            }

            LogDebug($"Unregistered window: {window.WindowName}");
        }
    }

    private void EnsureCursorVisible()
    {
        // Force cursor to be visible and unlocked for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Attempt to open a window. Returns true if successful, false if blocked.
    /// </summary>
    public bool TryOpenWindow(IUIWindow window)
    {
        if (window == null)
        {
            LogDebug("TryOpenWindow: window is null");
            return false;
        }

        // Check if any window is currently open
        if (openWindowStack.Count > 0)
        {
            var topWindow = openWindowStack.Peek();
            LogDebug($"Window '{window.WindowName}' blocked by '{topWindow.WindowName}'");
            window.OnWindowBlocked(topWindow.WindowName);
            return false;
        }

        // No windows open, allow this one to open
        openWindowStack.Push(window);
        window.OpenWindow();

        LogDebug($"Opened window: {window.WindowName} (Priority: {window.WindowPriority})");

        // Ensure cursor is visible for UI interaction
        EnsureCursorVisible();

        return true;
    }

    /// <summary>
    /// Close a specific window if it's currently open
    /// </summary>
    public bool TryCloseWindow(IUIWindow window)
    {
        if (window == null || openWindowStack.Count == 0)
            return false;

        // Check if this window is the currently open one
        if (openWindowStack.Peek() == window)
        {
            openWindowStack.Pop();
            window.CloseWindow();
            LogDebug($"Closed window: {window.WindowName}");
            return true;
        }

        LogDebug($"Cannot close window '{window.WindowName}' - not the active window");
        return false;
    }

    /// <summary>
    /// Handle ESC key press. Closes the highest priority window or opens game menu if none are open.
    /// </summary>
    public void HandleEscapeKey()
    {
        LogDebug($"ESC pressed. Open windows: {openWindowStack.Count}");

        // If windows are open, try to close the top one
        if (openWindowStack.Count > 0)
        {
            var topWindow = openWindowStack.Peek();

            if (topWindow.CanCloseWithEsc)
            {
                TryCloseWindow(topWindow);
                LogDebug($"ESC closed window: {topWindow.WindowName}");
            }
            else
            {
                LogDebug($"ESC ignored - window '{topWindow.WindowName}' cannot be closed with ESC");
            }
            return;
        }

        // No windows open, try to open game menu
        var gameMenu = registeredWindows.FirstOrDefault(w => w.WindowName == "GameMenu");
        if (gameMenu != null && !gameMenu.IsWindowOpen &&
            (gameMenu is not GameMenuManager menuManager || menuManager.CanOpenMenu()))
        {
            TryOpenWindow(gameMenu);
            LogDebug("ESC opened game menu");
        }
        else
        {
            LogDebug("ESC pressed but no game menu found, blocked, or it's already open");
        }
    }

    /// <summary>
    /// Check if any window is currently open
    /// </summary>
    public bool IsAnyWindowOpen()
    {
        return openWindowStack.Count > 0;
    }

    /// <summary>
    /// Get the currently active window, or null if none
    /// </summary>
    public IUIWindow GetActiveWindow()
    {
        return openWindowStack.Count > 0 ? openWindowStack.Peek() : null;
    }

    /// <summary>
    /// Force close all windows (emergency use only)
    /// </summary>
    public void ForceCloseAllWindows()
    {
        LogDebug($"Force closing {openWindowStack.Count} windows");

        while (openWindowStack.Count > 0)
        {
            var window = openWindowStack.Pop();
            window.CloseWindow();
        }
    }

    private void LogDebug(string message)
    {
        // The body used to be an empty if-block left behind when the logs were stripped, so the
        // ~15 LogDebug call sites in this class did nothing at all. Restored as a real (opt-in)
        // log so window-stack issues are debuggable again — off by default to keep the console clean.
        if (enableDebugLogs)
            Debug.Log($"[UIManager] {message}");
    }
}

} // namespace SowurShield.Core