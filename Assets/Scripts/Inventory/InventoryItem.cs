using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization;
using SowurShield.Core;

namespace SowurShield.Inventory
{
    public enum ItemType
    {
        Resource,
        Tool,
        Weapon,
        Food,
        Seed,
        Consumable
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
    public class Item : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemName = "New Item"; // stable internal ID — used for ItemDatabase lookups, save data, and recipe/effect keys. Never shown to the player.
        public LocalizedString displayName; // table "Inventory", key "item.<itemName>.name" — player-facing name shown in tooltips/UI
        [TextArea(2, 4)]
        public string description = "A useful item";
        public Sprite icon;
        public ItemType itemType = ItemType.Resource;
        public ItemRarity rarity = ItemRarity.Common;

        [Header("Stacking")]
        public bool isStackable = true;
        public int maxStackSize = 99;

        [Header("Tool Properties")]
        public List<string> itemTags = new List<string>();
        public int toolLevel = 0;
        public int durability = -1; // -1 means infinite durability

        [Header("Consumable Properties")]
        public int energyRestore = 0;
        public int healthRestore = 0;
        public bool isConsumable = false;

        [Header("Value")]
        public int baseValue = 1;
        public bool canBeSold = true;

        [Header("Gifting")]
        [Tooltip("Relationship points gained when this item is given as a gift to an NPC. 0 = not giftable.")]
        public float giftAffinityValue = 0f;

        // Player-facing name. Falls back to the internal itemName if displayName hasn't been
        // wired to a table entry yet, so older Item assets degrade gracefully instead of showing
        // a blank label.
        public string GetDisplayName()
        {
            string localized = displayName.SafeGetLocalizedString();
            return string.IsNullOrEmpty(localized) ? itemName : localized;
        }

        // Get the color associated with rarity
        public Color GetRarityColor()
        {
            return rarity switch
            {
                ItemRarity.Common => Color.white,
                ItemRarity.Uncommon => Color.green,
                ItemRarity.Rare => Color.blue,
                ItemRarity.Epic => new Color(0.6f, 0f, 1f), // Purple
                ItemRarity.Legendary => new Color(1f, 0.5f, 0f), // Orange
                _ => Color.white
            };
        }
    }
} // namespace SowurShield.Inventory
