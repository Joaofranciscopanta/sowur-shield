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

            return;
        }
        





        
        ItemStack selectedItem = inventory.SelectedItem;
        if (!selectedItem.IsEmpty)
        {

        }
        else
        {

        }
        
        // Count non-empty slots
        var allItems = inventory.GetAllItems();

        

    }
}