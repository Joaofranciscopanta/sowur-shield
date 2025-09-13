using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("UI Panels")]
    public GameObject inventoryPanel;
    public GameObject sellBoxPanel;
    public GameObject gameMenuPanel;
    
    private List<GameObject> allUIPanels = new List<GameObject>();
    private GameObject currentlyOpenPanel;
    
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
}