using UnityEngine;
using System.Collections.Generic;
using SowurShield.Inventory;

namespace SowurShield.Core
{
    [CreateAssetMenu(fileName = "New Tool Type", menuName = "Farming/Tool Type")]
    public class ToolType : ScriptableObject
    {
        public string typeName;       // Ex: "Hoe", "WateringCan"
        public List<string> itemNames; // Ex: "Bronze Hoe", "Iron Hoe", etc.

        // Verifica se um item é deste tipo de ferramenta
        public bool MatchesItem(Item item)
        {
            if (item == null || item.itemType != ItemType.Tool)
                return false;

            foreach (string name in itemNames)
            {
                if (item.itemName.Contains(name))
                    return true;
            }

            return false;
        }
    }
} // namespace SowurShield.Core
