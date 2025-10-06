#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to test SellBox drag and drop behavior fixes
/// </summary>
public class SellBoxDragTester : EditorWindow
{
    [MenuItem("Tools/Sowur Shield/Test SellBox Drag Fix")]
    public static void ShowWindow()
    {
        GetWindow<SellBoxDragTester>("SellBox Drag Tester");
    }

    private void OnGUI()
    {
        GUILayout.Label("SellBox Drag & Drop Fix Tester", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to test SellBox behavior", MessageType.Info);
            return;
        }
        
        SellBox sellBox = FindFirstObjectByType<SellBox>();
        if (sellBox == null)
        {
            EditorGUILayout.HelpBox("No SellBox found in scene!", MessageType.Error);
            return;
        }
        
        EditorGUILayout.HelpBox(
            "TESTING THE SEQUENCE BUG:\n\n" +
            "Sequence that causes value duplication:\n" +
            "1. Inventory → SellBox (works fine)\n" +
            "2. SellBox → Inventory (works fine)\n" +
            "3. Inventory → SellBox again (VALUE DUPLICATES!)\n\n" +
            "Fix Applied:\n" +
            "✓ Enhanced UpdateSlot() to ensure proper SellBox mode\n" +
            "✓ Added ghost value clearing in HandleSellBoxToInventoryDrop\n" +
            "✓ Added debug logging to CalculateTotalValue\n" +
            "✓ Improved state management during drag sequences\n\n" +
            "Test this specific sequence and check Console for debug logs!",
            MessageType.Warning
        );
        
        EditorGUILayout.Space();
        
        // Show SellBox status
        EditorGUILayout.LabelField("SellBox Status", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Is Open: {sellBox.IsOpen}");
        
        if (sellBox.IsOpen)
        {
            // Show total value (if accessible)
            EditorGUILayout.LabelField("SellBox is currently open - test dragging behavior!");
        }
        else
        {
            EditorGUILayout.HelpBox("Open the SellBox to test drag behavior", MessageType.Info);
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Validate SellBox Setup"))
        {
            ValidateSellBoxSetup();
        }
        
        if (GUILayout.Button("Debug SellBox State"))
        {
            DebugSellBoxState();
        }
        
        if (GUILayout.Button("Fix Value Duplication (Emergency)"))
        {
            FixValueDuplication();
        }
        
        // Auto-refresh during play mode
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
    
    private void ValidateSellBoxSetup()
    {

        
        SellBox sellBox = FindFirstObjectByType<SellBox>();
        if (sellBox != null)
        {

            
            // Check if new methods exist using reflection
            var methods = typeof(SellBox).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            bool hasInternalMove = System.Array.Exists(methods, m => m.Name == "HandleSellBoxInternalMove");
            bool hasSilentAdd = System.Array.Exists(methods, m => m.Name == "AddItemSilent");
            


            
            if (hasInternalMove && hasSilentAdd)
            {

            }
            else
            {

            }
        }
        else
        {

        }
        
        // Check InventorySlot routing fix
        var inventorySlotType = typeof(InventorySlot);
        var onDropMethod = inventorySlotType.GetMethod("OnDrop");
        if (onDropMethod != null)
        {

        }
        

    }
    
    private void DebugSellBoxState()
    {

        
        SellBox sellBox = FindFirstObjectByType<SellBox>();
        if (sellBox == null)
        {

            return;
        }
        

        
        // Try to get inventory via reflection for debugging
        try
        {
            var inventoryField = typeof(SellBox).GetField("sellBoxInventory", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (inventoryField != null)
            {
                var inventory = inventoryField.GetValue(sellBox);

            }
        }
        catch (System.Exception e)
        {

        }
        
        // Check for InventorySlot components
        InventorySlot[] slots = FindObjectsByType<InventorySlot>(FindObjectsSortMode.None);
        int sellBoxSlots = 0;
        foreach (var slot in slots)
        {
            var isSellBoxField = typeof(InventorySlot).GetField("isSellBoxMode", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (isSellBoxField != null)
            {
                bool isSellBox = (bool)isSellBoxField.GetValue(slot);
                if (isSellBox) sellBoxSlots++;
            }
        }
        


    }
    
    private void FixValueDuplication()
    {

        
        SellBox sellBox = FindFirstObjectByType<SellBox>();
        if (sellBox == null)
        {

            return;
        }
        
        // Call the validation method
        sellBox.ValidateAndFixState();
        


    }
}
#endif