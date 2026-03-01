using UnityEngine;
using System.Collections.Generic;

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
        public string itemName = "New Item";
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
