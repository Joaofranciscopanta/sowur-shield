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

        
        // Check for CursorController
        CursorController cursorController = FindFirstObjectByType<CursorController>();
        if (cursorController != null)
        {

        }
        else
        {

        }
        
        // Check for PlayerMove
        PlayerMove playerMove = FindFirstObjectByType<PlayerMove>();
        if (playerMove != null)
        {

        }
        else
        {

        }
        
        // Check for InventorySlot drag detection
        InventorySlot[] slots = FindObjectsByType<InventorySlot>(FindObjectsSortMode.None);

        
        if (slots.Length > 0)
        {

        }
        

        
    }
    
    private void DebugDragState()
    {


        
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

                }
            }
        }
        

    }
}
#endif