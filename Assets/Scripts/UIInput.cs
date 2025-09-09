using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class UIInput : MonoBehaviour
{
    private void Start()
    {
        // Ensure cursor is always visible and unlocked for UI interaction
        EnsureCursorVisible();
    }
    private void Update()
    {
        // Handle Escape key to close UI windows
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapeKey();
        }
    }
    
    private void OnGUI()
    {
        // Alternative: Handle Escape key via OnGUI for better reliability
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            HandleEscapeKey();
            Event.current.Use(); // Consume the event
        }
    }
    
    private void HandleEscapeKey()
    {
        // Always ensure cursor is visible when handling UI
        EnsureCursorVisible();
        
        if (UIManager.Instance != null)
        {
            // Close the current panel if any is open
            if (UIManager.Instance.IsAnyPanelOpen())
            {
                UIManager.Instance.CloseCurrentPanel();
                // Keep cursor visible after closing UI panels
                StartCoroutine(EnsureCursorVisibleDelayed());
                return;
            }
        }
        
        // If no UI is open, close any sellbox that might be open
        var sellBox = FindFirstObjectByType<SellBox>();
        if (sellBox != null && sellBox.IsOpen)
        {
            sellBox.CloseSellBox();
            // Keep cursor visible after closing sellbox
            StartCoroutine(EnsureCursorVisibleDelayed());
        }
    }
    
    private void EnsureCursorVisible()
    {
        // Force cursor to be visible and unlocked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Debug log to help track cursor state
        Debug.Log($"Cursor state: Visible={Cursor.visible}, LockState={Cursor.lockState}");
    }
    
    private System.Collections.IEnumerator EnsureCursorVisibleDelayed()
    {
        // Wait a frame for other systems to process
        yield return null;
        
        // Check if any UI is still open that needs cursor
        bool needsCursor = false;
        
        if (UIManager.Instance != null && UIManager.Instance.IsAnyPanelOpen())
        {
            needsCursor = true;
        }
        
        var sellBox = FindFirstObjectByType<SellBox>();
        if (sellBox != null && sellBox.IsOpen)
        {
            needsCursor = true;
        }
        
        // If UI still needs cursor, ensure it's visible
        if (needsCursor)
        {
            EnsureCursorVisible();
            Debug.Log("UIInput: Kept cursor visible for active UI");
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        // Ensure cursor is visible when application gains focus
        if (hasFocus)
        {
            EnsureCursorVisible();
        }
    }
}