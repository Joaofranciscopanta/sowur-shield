using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SowurShield.Inventory
{

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
        }
        else
        {
            // Use manually assigned items
            allItems.AddRange(items);
        }

        // Build lookup dictionary
        int duplicateCount = 0;
        foreach (Item item in allItems)
        {
            if (item == null) continue;

            if (itemLookup.ContainsKey(item.itemName))
            {
                duplicateCount++;
                continue;
            }

            itemLookup[item.itemName] = item;
        }

        isInitialized = true;

        if (duplicateCount > 0)
        {
        }

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


        // Check for items with no icon
        var noIconItems = itemLookup.Values.Where(i => i.icon == null).ToList();
        if (noIconItems.Count > 0)
        {
            foreach (var item in noIconItems)
            {
            }
        }

        // Check for items with no description
        var noDescItems = itemLookup.Values.Where(i => string.IsNullOrEmpty(i.description)).ToList();
        if (noDescItems.Count > 0)
        {
        }

        // Show item type breakdown
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            int count = itemLookup.Values.Count(i => i.itemType == type);
            if (count > 0)
            {
            }
        }

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

} // namespace SowurShield.Inventory