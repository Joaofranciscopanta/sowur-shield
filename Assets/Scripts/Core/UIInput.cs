using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace SowurShield.Core
{

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
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapeKey();
        }
    }

    private void HandleEscapeKey()
    {
        // Always ensure cursor is visible when handling UI
        EnsureCursorVisible();

        // Delegate ESC handling to UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HandleEscapeKey();
            // Keep cursor visible after UI changes
            StartCoroutine(EnsureCursorVisibleDelayed());
            return;
        }

        // Fallback behavior if no UIManager
        FallbackEscapeHandling();
    }

    private void FallbackEscapeHandling()
    {
        // Legacy ESC handling for compatibility
        // Close any sellbox that might be open
        var sellBox = FindFirstObjectByType<SellBox>();
        if (sellBox != null && sellBox.IsOpen)
        {
            sellBox.CloseSellBox();
            StartCoroutine(EnsureCursorVisibleDelayed());
            return;
        }

        // Try to open game menu
        var gameMenuManager = FindFirstObjectByType<GameMenuManager>();
        if (gameMenuManager != null && !gameMenuManager.IsMenuOpen)
        {
            gameMenuManager.OpenMenu();
        }
    }
    
    private void EnsureCursorVisible()
    {
        // Force cursor to be visible and unlocked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Debug log to help track cursor state

    }
    
    private System.Collections.IEnumerator EnsureCursorVisibleDelayed()
    {
        // Wait a frame for other systems to process
        yield return null;
        
        // Check if any UI is still open that needs cursor
        bool needsCursor = false;
        
        if (UIManager.Instance != null && UIManager.Instance.IsAnyWindowOpen())
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

} // namespace SowurShield.Core