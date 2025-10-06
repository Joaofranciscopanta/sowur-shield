using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("UI Panels")]
    public GameObject inventoryPanel;
    public GameObject sellBoxPanel;
    public GameObject gameMenuPanel;
    
    private List<GameObject> allUIPanels = new List<GameObject>();
    private GameObject currentlyOpenPanel;

    [Header("Window Management")]
    [SerializeField] private bool enableDebugLogs = true;

    // New window management system
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
        
        InitializeUIPanels();
    }
    
    private void InitializeUIPanels()
    {
        // Add all UI panels to the list
        if (inventoryPanel != null) allUIPanels.Add(inventoryPanel);
        if (sellBoxPanel != null) allUIPanels.Add(sellBoxPanel);
        if (gameMenuPanel != null) allUIPanels.Add(gameMenuPanel);
        
        // Auto-find panels if not assigned
        if (inventoryPanel == null)
        {
            var inventory = FindFirstObjectByType<Inventory>();
            if (inventory != null)
            {
                // Try to find inventory UI panel
                inventoryPanel = inventory.transform.Find("InventoryPanel")?.gameObject;
                if (inventoryPanel != null) allUIPanels.Add(inventoryPanel);
            }
        }
    }
    
    public void OpenPanel(GameObject panel)
    {
        if (panel == null) return;
        
        // Close all other panels first
        CloseAllPanels();
        
        // Open the requested panel
        panel.SetActive(true);
        currentlyOpenPanel = panel;
        
        // Ensure cursor is visible for UI interaction
        EnsureCursorVisible();
        

    }
    
    public void ClosePanel(GameObject panel)
    {
        if (panel == null) return;
        
        panel.SetActive(false);
        
        if (currentlyOpenPanel == panel)
            currentlyOpenPanel = null;
            

    }
    
    public void CloseAllPanels()
    {
        foreach (GameObject panel in allUIPanels)
        {
            if (panel != null && panel.activeInHierarchy)
            {
                panel.SetActive(false);
            }
        }
        currentlyOpenPanel = null;

    }
    
    public void CloseCurrentPanel()
    {
        if (currentlyOpenPanel != null)
        {
            ClosePanel(currentlyOpenPanel);
        }
    }
    
    public bool IsAnyPanelOpen()
    {
        return currentlyOpenPanel != null;
    }
    
    public GameObject GetCurrentPanel()
    {
        return currentlyOpenPanel;
    }
    
    // Add a panel to be managed
    public void RegisterPanel(GameObject panel)
    {
        if (panel != null && !allUIPanels.Contains(panel))
        {
            allUIPanels.Add(panel);
        }
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
    
    // Remove a panel from management
    public void UnregisterPanel(GameObject panel)
    {
        if (panel != null && allUIPanels.Contains(panel))
        {
            allUIPanels.Remove(panel);
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
        if (gameMenu != null && !gameMenu.IsWindowOpen)
        {
            TryOpenWindow(gameMenu);
            LogDebug("ESC opened game menu");
        }
        else
        {
            LogDebug("ESC pressed but no game menu found or it's already open");
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

        // Also close legacy panels for compatibility
        CloseAllPanels();
    }

    private void LogDebug(string message)
    {
        #if UNITY_EDITOR
        if (enableDebugLogs)
        {
            Debug.Log($"[UIManager] {message}");
        }
        #endif
    }
}