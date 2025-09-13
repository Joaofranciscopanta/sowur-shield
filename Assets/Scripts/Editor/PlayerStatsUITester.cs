#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to test PlayerStats and UI updates during play mode
/// </summary>
[System.Serializable]
public class PlayerStatsUITester : EditorWindow
{
    [MenuItem("Tools/Sowur Shield/Player Stats UI Tester")]
    public static void ShowWindow()
    {
        GetWindow<PlayerStatsUITester>("Player Stats Tester");
    }

    private int testMoneyAmount = 50;
    private int testEnergyAmount = 20;

    private void OnGUI()
    {
        GUILayout.Label("Player Stats UI Tester", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to test UI updates", MessageType.Info);
            return;
        }
        
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null)
        {
            EditorGUILayout.HelpBox("No PlayerStats found in scene!", MessageType.Error);
            return;
        }
        
        // Display current values
        EditorGUILayout.LabelField("Current Values", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Money: {playerStats.money}");
        EditorGUILayout.LabelField($"Energy: {playerStats.currentEnergy}/{playerStats.maxEnergy}");
        EditorGUILayout.LabelField($"Health: {playerStats.currentHealth}/{playerStats.maxHealth}");
        
        EditorGUILayout.Space();
        
        // Money tests
        EditorGUILayout.LabelField("Money Tests", EditorStyles.boldLabel);
        testMoneyAmount = EditorGUILayout.IntField("Test Amount:", testMoneyAmount);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button($"Add ${testMoneyAmount}"))
        {
            playerStats.AddMoney(testMoneyAmount);

        }
        if (GUILayout.Button($"Spend ${testMoneyAmount}"))
        {
            bool success = playerStats.SpendMoney(testMoneyAmount);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Energy tests
        EditorGUILayout.LabelField("Energy Tests", EditorStyles.boldLabel);
        testEnergyAmount = EditorGUILayout.IntField("Test Amount:", testEnergyAmount);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button($"Add {testEnergyAmount} Energy"))
        {
            playerStats.RestoreEnergy(testEnergyAmount);

        }
        if (GUILayout.Button($"Use {testEnergyAmount} Energy"))
        {
            playerStats.UseEnergy(testEnergyAmount);

        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Quick test buttons
        EditorGUILayout.LabelField("Quick Tests", EditorStyles.boldLabel);
        if (GUILayout.Button("Simulate Earning Money (Farm Sale)"))
        {
            int earnings = Random.Range(50, 200);
            playerStats.AddMoney(earnings);

        }
        
        if (GUILayout.Button("Simulate Using Energy (Farm Work)"))
        {
            int energyUsed = Random.Range(10, 30);
            playerStats.UseEnergy(energyUsed);

        }
        
        if (GUILayout.Button("Full Restore (Sleep)"))
        {
            playerStats.RestoreEnergy(playerStats.maxEnergy);

        }
        
        EditorGUILayout.Space();
        
        // Validation
        EditorGUILayout.LabelField("UI Validation", EditorStyles.boldLabel);
        if (GUILayout.Button("Check UI Connections"))
        {
            UIManagerPlayer uiManager = FindFirstObjectByType<UIManagerPlayer>();
            if (uiManager != null)
            {




                
                if (uiManager.staminaSlider != null)
                {

                }
                if (uiManager.moneyText != null)
                {

                }
                

            }
            else
            {

            }
        }
        
        // Auto-refresh during play mode
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}
#endif