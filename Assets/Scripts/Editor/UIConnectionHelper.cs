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

        
        // Find PlayerStats
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null)
        {

            return;
        }
        
        // Find UIManagerPlayer
        UIManagerPlayer uiManager = FindFirstObjectByType<UIManagerPlayer>();
        if (uiManager == null)
        {

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

                madeChanges = true;
            }
            else
            {

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

                madeChanges = true;
            }
            else
            {

            }
        }
        
        if (madeChanges)
        {
            EditorUtility.SetDirty(uiManager);
            EditorUtility.SetDirty(playerStats);

        }
        else
        {

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

        
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        UIManagerPlayer uiManager = FindFirstObjectByType<UIManagerPlayer>();
        
        if (playerStats == null)
        {

            return;
        }
        
        if (uiManager == null)
        {

            return;
        }
        


        
        // Check UIManagerPlayer connections




        
        // Check PlayerStats connections



        
        // Check current values



        

        
        if (uiManager.staminaSlider != null && uiManager.moneyText != null)
        {

        }
        else
        {

        }
    }
}
#endif