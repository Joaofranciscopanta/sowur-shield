using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization;

namespace SowurShield.Dialogue
{

/// <summary>
/// ScriptableObject defining a shop's inventory.
/// Prices are discounted by the shopkeeper's relationship level (max 20% at +100 affinity).
///
/// CREATE: Assets > Create > SowurShield > Shop Data
/// </summary>
[CreateAssetMenu(menuName = "SowurShield/Shop Data", fileName = "NewShop")]
public class ShopData : ScriptableObject
{
    [Header("Identity")]
    public LocalizedString shopTitle; // table "Dialogue", key "shop.<name>.title"
    [Tooltip("NPC id used to look up relationship discount in ConversationMemory.")]
    public string shopkeeperNpcId;

    [Header("Items for Sale")]
    public List<ShopItemEntry> items = new List<ShopItemEntry>();
}

[System.Serializable]
public class ShopItemEntry
{
    [Tooltip("Must match ItemDatabase key exactly.")]
    public string itemName;
    [Tooltip("Base price in gold before any discount.")]
    [Min(1)]
    public int basePrice = 10;
    [Tooltip("Maximum stock. -1 = unlimited.")]
    public int maxStock = -1;

    [System.NonSerialized]
    public int currentStock; // Runtime, set from save data on load

    public bool IsUnlimited => maxStock < 0;
    public bool IsInStock => IsUnlimited || currentStock > 0;
}

} // namespace SowurShield.Dialogue
