using UnityEngine;

public class InventorySpacingFix : MonoBehaviour
{
    [Header("Spacing Settings")]
    public float horizontalSpacing = 5f;
    public float verticalSpacing = 5f;
    public int slotsPerRow = 9;
    
    [Header("References")]
    public Transform inventoryParent;
    
    private void Start()
    {
        if (inventoryParent != null)
        {
            ApplySpacingToSlots();
        }
        else
        {
            // Try to find the hotbar automatically
            var inventory = FindFirstObjectByType<Inventory>();
            if (inventory != null && inventory.slotParent != null)
            {
                inventoryParent = inventory.slotParent;
                ApplySpacingToSlots();
            }
        }
    }
    
    private void ApplySpacingToSlots()
    {
        if (inventoryParent == null) return;
        
        Debug.Log($"Applying spacing to {inventoryParent.childCount} inventory slots");
        
        // Get all child slots
        for (int i = 0; i < inventoryParent.childCount; i++)
        {
            Transform slot = inventoryParent.GetChild(i);
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            
            if (slotRect == null) continue;
            
            // Calculate row and column
            int row = i / slotsPerRow;
            int col = i % slotsPerRow;
            
            // Apply spacing offset
            Vector2 currentPos = slotRect.anchoredPosition;
            Vector2 spacingOffset = new Vector2(col * horizontalSpacing, -row * verticalSpacing);
            
            slotRect.anchoredPosition = currentPos + spacingOffset;
            
            Debug.Log($"Slot {i}: Row {row}, Col {col}, Position: {slotRect.anchoredPosition}");
        }
        
        Debug.Log("Inventory spacing applied successfully!");
    }
    
    [ContextMenu("Apply Spacing")]
    public void ApplySpacingManually()
    {
        ApplySpacingToSlots();
    }
    
    [ContextMenu("Reset Positions")]
    public void ResetPositions()
    {
        if (inventoryParent == null) return;
        
        for (int i = 0; i < inventoryParent.childCount; i++)
        {
            Transform slot = inventoryParent.GetChild(i);
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            
            if (slotRect == null) continue;
            
            // Reset to original grid position without spacing
            int row = i / slotsPerRow;
            int col = i % slotsPerRow;
            
            Vector2 basePosition = new Vector2(col * 64, -row * 64); // Assuming 64px slot size
            slotRect.anchoredPosition = basePosition;
        }
        
        Debug.Log("Inventory positions reset!");
    }
}