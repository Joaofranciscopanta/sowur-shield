using System;
using System.Collections.Generic;
using System.Linq;

namespace SowurShield.Inventory
{
    /// <summary>
    /// Filtering utilities for inventory items
    /// Provides various filtering strategies and predicate-based filtering
    /// </summary>
    public static class InventoryFiltering
    {
        /// <summary>
        /// Interface for inventory filters
        /// </summary>
        public interface IInventoryFilter
        {
            bool Matches(ItemStack stack);
            string GetDescription();
        }

        // ============================================================================
        // BUILT-IN FILTERS
        // ============================================================================

        /// <summary>
        /// Filter by item type
        /// </summary>
        public class TypeFilter : IInventoryFilter
        {
            private ItemType targetType;

            public TypeFilter(ItemType type)
            {
                targetType = type;
            }

            public bool Matches(ItemStack stack)
            {
                return !stack.IsEmpty && stack.item.itemType == targetType;
            }

            public string GetDescription()
            {
                return $"Type: {targetType}";
            }
        }

        /// <summary>
        /// Filter by item tag
        /// </summary>
        public class TagFilter : IInventoryFilter
        {
            private string targetTag;

            public TagFilter(string tag)
            {
                targetTag = tag;
            }

            public bool Matches(ItemStack stack)
            {
                return !stack.IsEmpty && stack.item.itemTags.Contains(targetTag);
            }

            public string GetDescription()
            {
                return $"Tag: {targetTag}";
            }
        }

        /// <summary>
        /// Filter by item rarity
        /// </summary>
        public class RarityFilter : IInventoryFilter
        {
            private ItemRarity targetRarity;

            public RarityFilter(ItemRarity rarity)
            {
                targetRarity = rarity;
            }

            public bool Matches(ItemStack stack)
            {
                return !stack.IsEmpty && stack.item.rarity == targetRarity;
            }

            public string GetDescription()
            {
                return $"Rarity: {targetRarity}";
            }
        }

        /// <summary>
        /// Filter by value range
        /// </summary>
        public class ValueRangeFilter : IInventoryFilter
        {
            private int minValue;
            private int maxValue;

            public ValueRangeFilter(int min, int max)
            {
                minValue = min;
                maxValue = max;
            }

            public bool Matches(ItemStack stack)
            {
                if (stack.IsEmpty) return false;
                int value = stack.item.baseValue;
                return value >= minValue && value <= maxValue;
            }

            public string GetDescription()
            {
                return $"Value: {minValue}-{maxValue}";
            }
        }

        /// <summary>
        /// Filter by item properties (stackable, consumable, etc.)
        /// </summary>
        public class PropertyFilter : IInventoryFilter
        {
            public enum Property
            {
                Stackable,
                Consumable,
                Sellable,
                Equippable
            }

            private Property targetProperty;

            public PropertyFilter(Property property)
            {
                targetProperty = property;
            }

            public bool Matches(ItemStack stack)
            {
                if (stack.IsEmpty) return false;

                switch (targetProperty)
                {
                    case Property.Stackable:
                        return stack.item.isStackable;

                    case Property.Consumable:
                        return stack.item.isConsumable;

                    case Property.Sellable:
                        return stack.item.canBeSold;

                    case Property.Equippable:
                        return stack.item.itemTags.Contains("Equippable") ||
                               stack.item.itemTags.Contains("Tool") ||
                               stack.item.itemTags.Contains("Weapon");

                    default:
                        return false;
                }
            }

            public string GetDescription()
            {
                return $"Property: {targetProperty}";
            }
        }

        /// <summary>
        /// Filter by custom predicate
        /// </summary>
        public class CustomFilter : IInventoryFilter
        {
            private Func<ItemStack, bool> predicate;
            private string description;

            public CustomFilter(Func<ItemStack, bool> predicate, string description = "Custom")
            {
                this.predicate = predicate;
                this.description = description;
            }

            public bool Matches(ItemStack stack)
            {
                return predicate(stack);
            }

            public string GetDescription()
            {
                return description;
            }
        }

        /// <summary>
        /// Combine multiple filters with AND logic
        /// </summary>
        public class CombinedFilter : IInventoryFilter
        {
            private List<IInventoryFilter> filters;

            public CombinedFilter(params IInventoryFilter[] filters)
            {
                this.filters = new List<IInventoryFilter>(filters);
            }

            public void AddFilter(IInventoryFilter filter)
            {
                filters.Add(filter);
            }

            public bool Matches(ItemStack stack)
            {
                // All filters must match
                return filters.All(f => f.Matches(stack));
            }

            public string GetDescription()
            {
                return string.Join(" AND ", filters.Select(f => f.GetDescription()));
            }
        }

        // ============================================================================
        // FILTERING METHODS
        // ============================================================================

        /// <summary>
        /// Filter a list of ItemStacks using a filter
        /// </summary>
        public static List<ItemStack> Filter(List<ItemStack> items, IInventoryFilter filter)
        {
            if (items == null || filter == null) return items;

            return items.Where(stack => filter.Matches(stack)).ToList();
        }

        /// <summary>
        /// Filter by item type (convenience method)
        /// </summary>
        public static List<ItemStack> FilterByType(List<ItemStack> items, ItemType type)
        {
            return Filter(items, new TypeFilter(type));
        }

        /// <summary>
        /// Filter by tag (convenience method)
        /// </summary>
        public static List<ItemStack> FilterByTag(List<ItemStack> items, string tag)
        {
            return Filter(items, new TagFilter(tag));
        }

        /// <summary>
        /// Filter by rarity (convenience method)
        /// </summary>
        public static List<ItemStack> FilterByRarity(List<ItemStack> items, ItemRarity rarity)
        {
            return Filter(items, new RarityFilter(rarity));
        }

        /// <summary>
        /// Filter by custom predicate (convenience method)
        /// </summary>
        public static List<ItemStack> FilterByPredicate(List<ItemStack> items, Func<ItemStack, bool> predicate)
        {
            return Filter(items, new CustomFilter(predicate));
        }

        /// <summary>
        /// Search items by name (partial match, case-insensitive)
        /// </summary>
        public static List<ItemStack> SearchByName(List<ItemStack> items, string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm)) return items;

            string lowerSearch = searchTerm.ToLower();
            return items.Where(stack =>
                !stack.IsEmpty &&
                stack.item.itemName.ToLower().Contains(lowerSearch)
            ).ToList();
        }

        /// <summary>
        /// Get all unique item types present in the list
        /// </summary>
        public static List<ItemType> GetPresentTypes(List<ItemStack> items)
        {
            return items
                .Where(stack => !stack.IsEmpty)
                .Select(stack => stack.item.itemType)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Get all unique tags present in the list
        /// </summary>
        public static List<string> GetPresentTags(List<ItemStack> items)
        {
            return items
                .Where(stack => !stack.IsEmpty)
                .SelectMany(stack => stack.item.itemTags)
                .Distinct()
                .ToList();
        }
    }
} // namespace SowurShield.Inventory
