#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to test that dragging inventory items doesn't trigger tool usage
/// </summary>
public class DragConflictTester : EditorWindow
{
    [MenuItem("Tools/Sowur Shield/Test Drag Conflict Fix")]
    public static void ShowWindow()
    {
        GetWindow<DragConflictTester>("Drag Conflict Tester");
    }

    private void OnGUI()
    {
        GUILayout.Label("Drag Conflict Fix Tester", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to test drag behavior", MessageType.Info);
            return;
        }
        
        EditorGUILayout.HelpBox(
            "Testing Instructions:\n\n" +
            "1. Equip a hoe in your hotbar\n" +
            "2. Try to drag the hoe around in your inventory\n" +
            "3. The hoe should NOT trigger soil tilling while dragging\n" +
            "4. After dropping the hoe, it should work normally again\n\n" +
            "If dragging still triggers tool usage, the fix needs adjustment.",
            MessageType.Info
        );
        
        EditorGUILayout.Space();
        
        // Show current drag state
        EditorGUILayout.LabelField("Drag Status", EditorStyles.boldLabel);
        bool isDragging = InventorySlot.IsAnySlotDragging;
        EditorGUILayout.LabelField($"Is Any Slot Dragging: {isDragging}");
        
        if (isDragging)
        {
            EditorGUILayout.HelpBox("✓ Dragging detected! Tool usage should be blocked.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("No dragging detected. Tool usage should work normally.", MessageType.None);
        }
        
        EditorGUILayout.Space();
        
        // Validation buttons
        if (GUILayout.Button("Validate Systems Present"))
        {
            ValidateSystems();
        }
        
        if (GUILayout.Button("Force Debug Drag State"))
        {
            DebugDragState();
        }
        
        // Auto-refresh during play mode
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
    
    private void ValidateSystems()
    {
        Debug.Log("=== Validating Drag Conflict Fix Systems ===");
        
        // Check for CursorController
        CursorController cursorController = FindFirstObjectByType<CursorController>();
        if (cursorController != null)
        {
            Debug.Log("✓ CursorController found - this handles mouse click tool usage");
        }
        else
        {
            Debug.LogWarning("⚠️ CursorController not found - tool usage might work differently");
        }
        
        // Check for PlayerMove
        PlayerMove playerMove = FindFirstObjectByType<PlayerMove>();
        if (playerMove != null)
        {
            Debug.Log("✓ PlayerMove found - this handles E-key interactions");
        }
        else
        {
            Debug.LogWarning("⚠️ PlayerMove not found");
        }
        
        // Check for InventorySlot drag detection
        InventorySlot[] slots = FindObjectsByType<InventorySlot>(FindObjectsSortMode.None);
        Debug.Log($"✓ Found {slots.Length} InventorySlot components");
        
        if (slots.Length > 0)
        {
            Debug.Log("✓ InventorySlot.IsAnySlotDragging system available for drag detection");
        }
        
        Debug.Log("=== Systems Validation Complete ===");
        
        Debug.Log("\n" +
            "🧪 TEST PROCEDURE:\n" +
            "1. Have a hoe in inventory\n" +
            "2. Start dragging the hoe\n" +
            "3. While dragging, click on soil - should NOT till\n" +
            "4. Drop the hoe, then try to use it - should work normally\n");
    }
    
    private void DebugDragState()
    {
        Debug.Log("=== Debug Drag State ===");
        Debug.Log($"InventorySlot.IsAnySlotDragging: {InventorySlot.IsAnySlotDragging}");
        
        InventorySlot[] slots = FindObjectsByType<InventorySlot>(FindObjectsSortMode.None);
        foreach (var slot in slots)
        {
            // Using reflection to access private isDragging field for debugging
            var field = typeof(InventorySlot).GetField("isDragging", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                bool isDragging = (bool)field.GetValue(slot);
                if (isDragging)
                {
                    Debug.Log($"Slot {slot.name} is currently dragging");
                }
            }
        }
        
        Debug.Log("=== End Debug ===");
    }
}
#endif