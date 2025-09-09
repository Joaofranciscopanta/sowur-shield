using UnityEngine;
using UnityEditor;

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
            Debug.LogError("Please assign a Player Object first!");
            return;
        }

        // Check if Inventory component already exists
        Inventory existingInventory = playerObject.GetComponent<Inventory>();
        if (existingInventory != null)
        {
            Debug.Log("Inventory component already exists on player!");
            return;
        }

        // Add Inventory component
        Inventory inventory = playerObject.AddComponent<Inventory>();
        
        // Configure default values
        inventory.inventorySize = 36;
        inventory.hotbarSize = 9;
        
        // Try to find UI parent automatically
        if (uiParent != null)
        {
            inventory.slotParent = uiParent;
        }

        Debug.Log("Added Inventory component to player with default settings!");
        EditorUtility.SetDirty(playerObject);
    }

    private void CreateInventorySlots()
    {
        if (uiParent == null)
        {
            Debug.LogError("Please assign a UI Parent first!");
            return;
        }

        if (slotPrefab == null)
        {
            Debug.LogError("Please assign a Slot Prefab first!");
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

        Debug.Log("Created 36 inventory slots!");
    }

    private void ValidateSetup()
    {
        Debug.Log("=== INVENTORY SETUP VALIDATION ===");
        
        // Check player
        if (playerObject == null)
        {
            Debug.LogError("❌ No player object assigned!");
        }
        else
        {
            Inventory inv = playerObject.GetComponent<Inventory>();
            if (inv == null)
            {
                Debug.LogError("❌ Player missing Inventory component!");
            }
            else
            {
                Debug.Log("✅ Player has Inventory component");
                
                if (inv.slotParent == null)
                {
                    Debug.LogWarning("⚠️ Inventory missing slotParent reference");
                }
                else
                {
                    Debug.Log($"✅ Slot parent assigned: {inv.slotParent.name}");
                }
            }
        }

        // Check UI
        if (uiParent == null)
        {
            Debug.LogWarning("⚠️ No UI parent assigned");
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
            Debug.Log($"✅ Found {slotCount} inventory slots in UI");
        }

        // Check EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            Debug.LogError("❌ No EventSystem in scene! UI interactions won't work!");
        }
        else
        {
            Debug.Log("✅ EventSystem found");
        }

        Debug.Log("=== VALIDATION COMPLETE ===");
    }

    private void AddTestItems()
    {
        if (playerObject == null)
        {
            Debug.LogError("Please assign a Player Object first!");
            return;
        }

        Inventory inventory = playerObject.GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("Player doesn't have Inventory component!");
            return;
        }

        // Try to find test items in Resources
        Item[] items = Resources.LoadAll<Item>("Items");
        
        if (items.Length == 0)
        {
            Debug.LogWarning("No Item assets found in Resources/Items/. Create some items first!");
            return;
        }

        // Add some test items
        for (int i = 0; i < Mathf.Min(5, items.Length); i++)
        {
            inventory.AddItem(items[i], Random.Range(1, 10));
        }

        Debug.Log($"Added {Mathf.Min(5, items.Length)} test items to inventory!");
    }

    private void FindPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length > 0)
        {
            playerObject = players[0];
            Debug.Log($"Found player: {playerObject.name}");
        }
        else
        {
            Debug.LogWarning("No GameObject with 'Player' tag found!");
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
                Debug.Log($"Found UI parent: {uiParent.name}");
            }
            else
            {
                Debug.Log($"Found Canvas: {canvas.name}, but no 'Inventory' child found");
            }
        }
        else
        {
            Debug.LogWarning("No Canvas found in scene!");
        }
    }
}