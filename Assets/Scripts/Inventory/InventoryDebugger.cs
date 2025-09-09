using UnityEngine;

[System.Serializable]
public class InventoryDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebugging = true;
    public KeyCode debugKey = KeyCode.F1;
    
    private Inventory inventory;
    
    private void Start()
    {
        inventory = FindFirstObjectByType<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("InventoryDebugger: No Inventory component found in scene!");
        }
    }
    
    private void Update()
    {
        if (enableDebugging && Input.GetKeyDown(debugKey))
        {
            DebugInventoryState();
        }
    }
    
    public void DebugInventoryState()
    {
        if (inventory == null)
        {
            Debug.LogError("InventoryDebugger: Inventory is null!");
            return;
        }
        
        Debug.Log("=== INVENTORY DEBUG INFO ===");
        Debug.Log($"Inventory Size: {inventory.inventorySize}");
        Debug.Log($"Hotbar Size: {inventory.hotbarSize}");
        Debug.Log($"Selected Slot: {inventory.SelectedSlotIndex}");
        Debug.Log($"Is Open: {inventory.IsInventoryOpen}");
        
        ItemStack selectedItem = inventory.SelectedItem;
        if (!selectedItem.IsEmpty)
        {
            Debug.Log($"Selected Item: {selectedItem.item.itemName} x{selectedItem.quantity}");
        }
        else
        {
            Debug.Log("Selected Item: None");
        }
        
        // Count non-empty slots
        var allItems = inventory.GetAllItems();
        Debug.Log($"Total Items in Inventory: {allItems.Count}");
        
        Debug.Log("=== END DEBUG INFO ===");
    }
}