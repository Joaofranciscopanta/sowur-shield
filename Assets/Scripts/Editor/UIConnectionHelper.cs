#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool to help connect UI elements to PlayerStats automatically
/// </summary>
public class UIConnectionHelper : EditorWindow
{
    [MenuItem("Tools/Sowur Shield/Connect Player UI")]
    public static void ShowWindow()
    {
        GetWindow<UIConnectionHelper>("UI Connection Helper");
    }

    private void OnGUI()
    {
        GUILayout.Label("Player UI Connection Helper", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Auto-Connect UI Elements"))
        {
            AutoConnectUI();
        }
        
        if (GUILayout.Button("Validate UI Connections"))
        {
            ValidateUIConnections();
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "This tool will:\n" +
            "• Find PlayerStats in the scene\n" +
            "• Find UIManagerPlayer in the scene\n" +
            "• Auto-connect stamina slider and money text\n" +
            "• Validate all connections are working",
            MessageType.Info
        );
    }
    
    private void AutoConnectUI()
    {
        Debug.Log("=== Auto-Connecting Player UI Elements ===");
        
        // Find PlayerStats
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("No PlayerStats found in scene! Add PlayerStats component to a GameObject first.");
            return;
        }
        
        // Find UIManagerPlayer
        UIManagerPlayer uiManager = FindFirstObjectByType<UIManagerPlayer>();
        if (uiManager == null)
        {
            Debug.LogError("No UIManagerPlayer found in scene! Add UIManagerPlayer component to a GameObject first.");
            return;
        }
        
        bool madeChanges = false;
        
        // Connect stamina slider
        if (uiManager.staminaSlider == null)
        {
            Slider staminaSlider = FindUIElement<Slider>("StaminaSlider", "Stamina", "Energy");
            if (staminaSlider != null)
            {
                uiManager.staminaSlider = staminaSlider;
                playerStats.energySlider = staminaSlider; // Also connect to PlayerStats
                Debug.Log($"✓ Connected stamina slider: {staminaSlider.name}");
                madeChanges = true;
            }
            else
            {
                Debug.LogWarning("Could not find stamina/energy slider in scene. Create a UI Slider named 'StaminaSlider' or similar.");
            }
        }
        
        // Connect money text
        if (uiManager.moneyText == null)
        {
            TextMeshProUGUI moneyText = FindUIElement<TextMeshProUGUI>("MoneyText", "Money", "Cash", "Coins");
            if (moneyText != null)
            {
                uiManager.moneyText = moneyText;
                playerStats.moneyText = moneyText.GetComponent<Text>(); // Try to connect to PlayerStats too
                Debug.Log($"✓ Connected money text: {moneyText.name}");
                madeChanges = true;
            }
            else
            {
                Debug.LogWarning("Could not find money text in scene. Create a TextMeshProUGUI named 'MoneyText' or similar.");
            }
        }
        
        if (madeChanges)
        {
            EditorUtility.SetDirty(uiManager);
            EditorUtility.SetDirty(playerStats);
            Debug.Log("🎉 UI connections updated! Save the scene to persist changes.");
        }
        else
        {
            Debug.Log("No changes needed - UI elements already connected.");
        }
    }
    
    private T FindUIElement<T>(params string[] searchNames) where T : Component
    {
        T[] allComponents = FindObjectsByType<T>(FindObjectsSortMode.None);
        
        foreach (var component in allComponents)
        {
            foreach (string searchName in searchNames)
            {
                if (component.name.ToLower().Contains(searchName.ToLower()))
                {
                    return component;
                }
            }
        }
        
        return null;
    }
    
    private void ValidateUIConnections()
    {
        Debug.Log("=== Validating Player UI Connections ===");
        
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        UIManagerPlayer uiManager = FindFirstObjectByType<UIManagerPlayer>();
        
        if (playerStats == null)
        {
            Debug.LogError("❌ PlayerStats not found!");
            return;
        }
        
        if (uiManager == null)
        {
            Debug.LogError("❌ UIManagerPlayer not found!");
            return;
        }
        
        Debug.Log($"✓ PlayerStats found: {playerStats.name}");
        Debug.Log($"✓ UIManagerPlayer found: {uiManager.name}");
        
        // Check UIManagerPlayer connections
        Debug.Log($"Stamina Slider: {(uiManager.staminaSlider != null ? "✓ Connected" : "❌ Missing")}");
        Debug.Log($"Money Text: {(uiManager.moneyText != null ? "✓ Connected" : "❌ Missing")}");
        Debug.Log($"Time Text: {(uiManager.timeText != null ? "✓ Connected" : "❌ Missing")}");
        Debug.Log($"Day Text: {(uiManager.dayText != null ? "✓ Connected" : "❌ Missing")}");
        
        // Check PlayerStats connections
        Debug.Log($"PlayerStats Energy Slider: {(playerStats.energySlider != null ? "✓ Connected" : "❌ Missing")}");
        Debug.Log($"PlayerStats Health Slider: {(playerStats.healthSlider != null ? "✓ Connected" : "❌ Missing")}");
        Debug.Log($"PlayerStats Money Text: {(playerStats.moneyText != null ? "✓ Connected" : "⚠️ Optional")}");
        
        // Check current values
        Debug.Log($"Current Money: {playerStats.money}");
        Debug.Log($"Current Energy: {playerStats.currentEnergy}/{playerStats.maxEnergy}");
        Debug.Log($"Current Health: {playerStats.currentHealth}/{playerStats.maxHealth}");
        
        Debug.Log("=== Validation Complete ===");
        
        if (uiManager.staminaSlider != null && uiManager.moneyText != null)
        {
            Debug.Log("🎉 Core UI connections look good! Your UI should update with player data changes.");
        }
        else
        {
            Debug.LogWarning("⚠️ Missing core UI connections. Use 'Auto-Connect UI Elements' to fix.");
        }
    }
}
#endif