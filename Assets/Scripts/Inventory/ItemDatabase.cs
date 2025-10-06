using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Centralized item database for fast item lookups
/// Eliminates slow Resources.LoadAll calls during save/load
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    private static ItemDatabase instance;
    private static Dictionary<string, Item> itemLookup = new Dictionary<string, Item>();
    private static bool isInitialized = false;

    [Header("Auto-Load Settings")]
    [Tooltip("Automatically load all items from Resources/Items folder")]
    public bool autoLoadFromResources = true;

    [Header("Manual Item List")]
    [Tooltip("Manually assigned items (optional, used if autoLoadFromResources is false)")]
    public List<Item> items = new List<Item>();

    // Singleton instance
    public static ItemDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                // Try to load from Resources
                instance = Resources.Load<ItemDatabase>("ItemDatabase");

                if (instance == null)
                {
                    Debug.LogWarning("ItemDatabase not found in Resources folder. Creating temporary instance.");
                    instance = CreateInstance<ItemDatabase>();
                    instance.autoLoadFromResources = true;
                }

                instance.Initialize();
            }
            return instance;
        }
    }

    /// <summary>
    /// Initialize the database - loads all items into lookup dictionary
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;

        itemLookup.Clear();
        List<Item> allItems = new List<Item>();

        if (autoLoadFromResources)
        {
            // Load all items from Resources folders
            Item[] resourceItems = Resources.LoadAll<Item>("");
            allItems.AddRange(resourceItems);
            Debug.Log($"ItemDatabase: Auto-loaded {resourceItems.Length} items from Resources");
        }
        else
        {
            // Use manually assigned items
            allItems.AddRange(items);
            Debug.Log($"ItemDatabase: Loaded {items.Count} manually assigned items");
        }

        // Build lookup dictionary
        int duplicateCount = 0;
        foreach (Item item in allItems)
        {
            if (item == null) continue;

            if (itemLookup.ContainsKey(item.itemName))
            {
                Debug.LogError($"ItemDatabase: Duplicate item name detected: '{item.itemName}'. " +
                              $"Only the first item will be used. Please ensure all items have unique names.");
                duplicateCount++;
                continue;
            }

            itemLookup[item.itemName] = item;
        }

        isInitialized = true;

        if (duplicateCount > 0)
        {
            Debug.LogWarning($"ItemDatabase: Found {duplicateCount} duplicate item names. Check console for details.");
        }

        Debug.Log($"ItemDatabase initialized with {itemLookup.Count} unique items");
    }

    /// <summary>
    /// Force re-initialization (useful for editor testing)
    /// </summary>
    public static void ForceReload()
    {
        isInitialized = false;
        itemLookup.Clear();
        instance = null;
        var _ = Instance; // Trigger reload
    }

    /// <summary>
    /// Get an item by name (fast dictionary lookup)
    /// </summary>
    public static Item GetItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;

        // Ensure database is initialized
        var _ = Instance;

        if (itemLookup.TryGetValue(itemName, out Item item))
        {
            return item;
        }

        Debug.LogWarning($"ItemDatabase: Item '{itemName}' not found in database");
        return null;
    }

    /// <summary>
    /// Check if an item exists in the database
    /// </summary>
    public static bool ItemExists(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return false;

        var _ = Instance;
        return itemLookup.ContainsKey(itemName);
    }

    /// <summary>
    /// Get all items in the database
    /// </summary>
    public static List<Item> GetAllItems()
    {
        var _ = Instance;
        return new List<Item>(itemLookup.Values);
    }

    /// <summary>
    /// Get all items of a specific type
    /// </summary>
    public static List<Item> GetItemsByType(ItemType itemType)
    {
        var _ = Instance;
        return itemLookup.Values.Where(item => item.itemType == itemType).ToList();
    }

    /// <summary>
    /// Get all items with a specific tag
    /// </summary>
    public static List<Item> GetItemsByTag(string tag)
    {
        var _ = Instance;
        return itemLookup.Values.Where(item => item.itemTags.Contains(tag)).ToList();
    }

    /// <summary>
    /// Get count of items in database
    /// </summary>
    public static int ItemCount
    {
        get
        {
            var _ = Instance;
            return itemLookup.Count;
        }
    }

    /// <summary>
    /// Validate database - check for issues
    /// </summary>
    public void ValidateDatabase()
    {
        Initialize();

        Debug.Log("=== ItemDatabase Validation ===");
        Debug.Log($"Total items: {itemLookup.Count}");

        // Check for items with no icon
        var noIconItems = itemLookup.Values.Where(i => i.icon == null).ToList();
        if (noIconItems.Count > 0)
        {
            Debug.LogWarning($"Items with no icon: {noIconItems.Count}");
            foreach (var item in noIconItems)
            {
                Debug.LogWarning($"  - {item.itemName}");
            }
        }

        // Check for items with no description
        var noDescItems = itemLookup.Values.Where(i => string.IsNullOrEmpty(i.description)).ToList();
        if (noDescItems.Count > 0)
        {
            Debug.LogWarning($"Items with no description: {noDescItems.Count}");
        }

        // Show item type breakdown
        Debug.Log("Item breakdown by type:");
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            int count = itemLookup.Values.Count(i => i.itemType == type);
            if (count > 0)
            {
                Debug.Log($"  - {type}: {count} items");
            }
        }

        Debug.Log("=== Validation Complete ===");
    }

    // Editor-only: Called when the ScriptableObject is loaded
    private void OnEnable()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
}