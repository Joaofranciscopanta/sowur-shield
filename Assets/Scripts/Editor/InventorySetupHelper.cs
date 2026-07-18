using UnityEngine;
using UnityEditor;
using SowurShield.Inventory;

namespace SowurShield.Editor
{

[System.Serializable]
public class InventorySetupHelper : EditorWindow
{
    [MenuItem("Tools/Inventory System/Setup Helper")]
    public static void ShowWindow()
    {
        GetWindow<InventorySetupHelper>("Inventory Setup");
    }

    private GameObject playerObject;
    private Transform uiParent;
    private GameObject slotPrefab;

    private void OnGUI()
    {
        GUILayout.Label("Inventory System Setup Helper", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Player Object Field
        GUILayout.Label("1. Player Setup", EditorStyles.boldLabel);
        playerObject = EditorGUILayout.ObjectField("Player Object", playerObject, typeof(GameObject), true) as GameObject;
        
        if (GUILayout.Button("Add Inventory Component to Player"))
        {
            AddInventoryToPlayer();
        }
        
        GUILayout.Space(10);

        // UI Setup
        GUILayout.Label("2. UI Setup", EditorStyles.boldLabel);
        uiParent = EditorGUILayout.ObjectField("UI Parent (for slots)", uiParent, typeof(Transform), true) as Transform;
        slotPrefab = EditorGUILayout.ObjectField("Slot Prefab", slotPrefab, typeof(GameObject), false) as GameObject;

        if (GUILayout.Button("Create 36 Inventory Slots"))
        {
            CreateInventorySlots();
        }

        GUILayout.Space(10);

        // Validation
        GUILayout.Label("3. Validation", EditorStyles.boldLabel);
        if (GUILayout.Button("Validate Scene Setup"))
        {
            ValidateSetup();
        }

        if (GUILayout.Button("Add Test Items to Player"))
        {
            AddTestItems();
        }

        GUILayout.Space(10);
        
        // Quick Actions
        GUILayout.Label("Quick Actions", EditorStyles.boldLabel);
        if (GUILayout.Button("Find Player in Scene"))
        {
            FindPlayer();
        }
        
        if (GUILayout.Button("Find UI Canvas"))
        {
            FindUICanvas();
        }
    }

    private void AddInventoryToPlayer()
    {
        if (playerObject == null)
        {

            return;
        }

        // Check if Inventory component already exists
        SowurShield.Inventory.Inventory existingInventory = playerObject.GetComponent<SowurShield.Inventory.Inventory>();
        if (existingInventory != null)
        {

            return;
        }

        // Add Inventory component
        SowurShield.Inventory.Inventory inventory = playerObject.AddComponent<SowurShield.Inventory.Inventory>();
        
        // Configure default values
        inventory.inventorySize = 36;
        inventory.hotbarSize = 9;
        
        // Try to find UI parent automatically
        if (uiParent != null)
        {
            inventory.slotParent = uiParent;
        }


        EditorUtility.SetDirty(playerObject);
    }

    private void CreateInventorySlots()
    {
        if (uiParent == null)
        {

            return;
        }

        if (slotPrefab == null)
        {

            return;
        }

        // Clear existing slots
        for (int i = uiParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(uiParent.GetChild(i).gameObject);
        }

        // Create 36 new slots
        for (int i = 0; i < 36; i++)
        {
            GameObject slot = PrefabUtility.InstantiatePrefab(slotPrefab) as GameObject;
            slot.transform.SetParent(uiParent, false);
            slot.name = $"InventorySlot ({i + 1})";
            
            // Configure slot position (9 slots per row)
            RectTransform rect = slot.GetComponent<RectTransform>();
            if (rect != null)
            {
                float x = (i % 9) * 70;
                float y = -(i / 9) * 70;
                rect.anchoredPosition = new Vector2(x, y);
            }
        }


    }

    private void ValidateSetup()
    {

        
        // Check player
        if (playerObject == null)
        {

        }
        else
        {
            SowurShield.Inventory.Inventory inv = playerObject.GetComponent<SowurShield.Inventory.Inventory>();
            if (inv == null)
            {

            }
            else
            {

                
                if (inv.slotParent == null)
                {

                }
                else
                {

                }
            }
        }

        // Check UI
        if (uiParent == null)
        {

        }
        else
        {
            int slotCount = 0;
            for (int i = 0; i < uiParent.childCount; i++)
            {
                if (uiParent.GetChild(i).GetComponent<InventorySlot>() != null)
                {
                    slotCount++;
                }
            }

        }

        // Check EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {

        }
        else
        {

        }


    }

    private void AddTestItems()
    {
        if (playerObject == null)
        {

            return;
        }

        SowurShield.Inventory.Inventory inventory = playerObject.GetComponent<SowurShield.Inventory.Inventory>();
        if (inventory == null)
        {

            return;
        }

        // Try to find test items in Resources
        Item[] items = Resources.LoadAll<Item>("Items");
        
        if (items.Length == 0)
        {

            return;
        }

        // Add some test items
        for (int i = 0; i < Mathf.Min(5, items.Length); i++)
        {
            inventory.AddItem(items[i], Random.Range(1, 10));
        }


    }

    private void FindPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length > 0)
        {
            playerObject = players[0];

        }
        else
        {

        }
    }

    private void FindUICanvas()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            // Look for inventory-related UI elements
            Transform inventoryPanel = canvas.transform.Find("Inventory");
            if (inventoryPanel != null)
            {
                uiParent = inventoryPanel;

            }
            else
            {

            }
        }
        else
        {

        }
    }
}

} // namespace SowurShield.Editor