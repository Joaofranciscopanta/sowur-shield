using System;
using System.Collections.Generic;

/// <summary>
/// Sorting utilities for inventory items
/// Provides various sorting strategies and comparison methods
/// </summary>
public static class InventorySorting
{
    /// <summary>
    /// Sort mode options for inventory
    /// </summary>
    public enum SortMode
    {
        Type,           // Sort by ItemType
        Name,           // Sort alphabetically by name
        Value,          // Sort by base value (high to low)
        Quantity,       // Sort by stack quantity (high to low)
        Rarity,         // Sort by item rarity
        RecentlyAdded,  // Sort by addition time (newest first)
        Custom          // Use custom comparer
    }

    /// <summary>
    /// Sort direction options
    /// </summary>
    public enum SortDirection
    {
        Ascending,
        Descending
    }

    /// <summary>
    /// Sort a list of ItemStacks by the specified mode
    /// </summary>
    public static void Sort(List<ItemStack> items, SortMode mode, SortDirection direction = SortDirection.Ascending)
    {
        if (items == null || items.Count <= 1) return;

        Comparison<ItemStack> comparison = GetComparison(mode);

        if (direction == SortDirection.Descending)
        {
            // Reverse the comparison for descending order
            Comparison<ItemStack> originalComparison = comparison;
            comparison = (a, b) => -originalComparison(a, b);
        }

        items.Sort(comparison);
    }

    /// <summary>
    /// Sort with multiple criteria (e.g., by type, then by name)
    /// </summary>
    public static void SortMultiple(List<ItemStack> items, params (SortMode mode, SortDirection direction)[] criteria)
    {
        if (items == null || items.Count <= 1 || criteria == null || criteria.Length == 0) return;

        items.Sort((a, b) =>
        {
            // Try each criterion in order until we find a difference
            foreach (var (mode, direction) in criteria)
            {
                Comparison<ItemStack> comparison = GetComparison(mode);
                int result = comparison(a, b);

                if (result != 0)
                {
                    return direction == SortDirection.Descending ? -result : result;
                }
            }
            return 0; // All criteria equal
        });
    }

    /// <summary>
    /// Get a comparison function for the specified sort mode
    /// </summary>
    private static Comparison<ItemStack> GetComparison(SortMode mode)
    {
        switch (mode)
        {
            case SortMode.Type:
                return CompareByType;

            case SortMode.Name:
                return CompareByName;

            case SortMode.Value:
                return CompareByValue;

            case SortMode.Quantity:
                return CompareByQuantity;

            case SortMode.Rarity:
                return CompareByRarity;

            default:
                return CompareByType; // Default fallback
        }
    }

    // ============================================================================
    // COMPARISON METHODS
    // ============================================================================

    private static int CompareByType(ItemStack a, ItemStack b)
    {
        if (a.IsEmpty && b.IsEmpty) return 0;
        if (a.IsEmpty) return 1;  // Empty stacks go to end
        if (b.IsEmpty) return -1;

        int typeCompare = a.item.itemType.CompareTo(b.item.itemType);
        if (typeCompare != 0) return typeCompare;

        // Secondary sort by name if types are equal
        return string.Compare(a.item.itemName, b.item.itemName, StringComparison.Ordinal);
    }

    private static int CompareByName(ItemStack a, ItemStack b)
    {
        if (a.IsEmpty && b.IsEmpty) return 0;
        if (a.IsEmpty) return 1;
        if (b.IsEmpty) return -1;

        return string.Compare(a.item.itemName, b.item.itemName, StringComparison.Ordinal);
    }

    private static int CompareByValue(ItemStack a, ItemStack b)
    {
        if (a.IsEmpty && b.IsEmpty) return 0;
        if (a.IsEmpty) return 1;
        if (b.IsEmpty) return -1;

        // Higher value first
        int valueCompare = b.item.baseValue.CompareTo(a.item.baseValue);
        if (valueCompare != 0) return valueCompare;

        // Secondary sort by name
        return string.Compare(a.item.itemName, b.item.itemName, StringComparison.Ordinal);
    }

    private static int CompareByQuantity(ItemStack a, ItemStack b)
    {
        if (a.IsEmpty && b.IsEmpty) return 0;
        if (a.IsEmpty) return 1;
        if (b.IsEmpty) return -1;

        // Higher quantity first
        int quantityCompare = b.quantity.CompareTo(a.quantity);
        if (quantityCompare != 0) return quantityCompare;

        // Secondary sort by name
        return string.Compare(a.item.itemName, b.item.itemName, StringComparison.Ordinal);
    }

    private static int CompareByRarity(ItemStack a, ItemStack b)
    {
        if (a.IsEmpty && b.IsEmpty) return 0;
        if (a.IsEmpty) return 1;
        if (b.IsEmpty) return -1;

        // Higher rarity first
        int rarityCompare = b.item.rarity.CompareTo(a.item.rarity);
        if (rarityCompare != 0) return rarityCompare;

        // Secondary sort by type, then name
        int typeCompare = a.item.itemType.CompareTo(b.item.itemType);
        if (typeCompare != 0) return typeCompare;

        return string.Compare(a.item.itemName, b.item.itemName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Create a custom comparison function
    /// </summary>
    public static Comparison<ItemStack> CreateCustomComparison(Func<ItemStack, ItemStack, int> compareFunc)
    {
        return (a, b) => compareFunc(a, b);
    }
}
