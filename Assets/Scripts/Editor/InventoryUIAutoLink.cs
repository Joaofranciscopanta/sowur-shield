using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to automatically link Inventory to InventoryUIManager
/// </summary>
[InitializeOnLoad]
public class InventoryUIAutoLink
{
    [MenuItem("Tools/Inventory/Auto-Link Inventory to UI Manager")]
    public static void AutoLinkInventory()
    {
        // Find InventoryUIManager in scene
        InventoryUIManager uiManager = Object.FindFirstObjectByType<InventoryUIManager>();

        if (uiManager == null)
        {
            EditorUtility.DisplayDialog("Not Found", "No InventoryUIManager found in scene!", "OK");
            return;
        }

        // Find Inventory in scene
        Inventory inventory = Object.FindFirstObjectByType<Inventory>();

        if (inventory == null)
        {
            EditorUtility.DisplayDialog("Not Found", "No Inventory component found in scene!", "OK");
            return;
        }

        // Link them
        SerializedObject so = new SerializedObject(uiManager);
        SerializedProperty inventoryProp = so.FindProperty("inventory");
        inventoryProp.objectReferenceValue = inventory;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(uiManager);

        Debug.Log($"Successfully linked Inventory '{inventory.gameObject.name}' to InventoryUIManager '{uiManager.gameObject.name}'");
        EditorUtility.DisplayDialog("Success", $"Linked Inventory on '{inventory.gameObject.name}' to InventoryUIManager!", "OK");
    }

    [MenuItem("Tools/Inventory/Auto-Link Inventory to UI Manager", true)]
    public static bool AutoLinkInventoryValidation()
    {
        // Only enable if in play mode or edit mode
        return true;
    }
}
