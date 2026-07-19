using UnityEngine;
using System.Collections.Generic;

namespace SowurShield.Animals
{

/// <summary>
/// ScriptableObject defining a market's catalog of animals available to buy, plus the
/// sell-back rate for animals the player already owns. Mirrors ShopData's shape — price
/// lives on the catalog entry, not on AnimalData, since AnimalData is a shared definition
/// asset (one "Chicken" feeds every chicken instance in the game).
///
/// CREATE: Assets > Create > SowurShield > Animal Market Data
/// </summary>
[CreateAssetMenu(menuName = "SowurShield/Animal Market Data", fileName = "NewAnimalMarket")]
public class AnimalMarketData : ScriptableObject
{
    [Header("Identity")]
    public string marketTitle = "Animal Market";
    [Tooltip("NPC id used to look up relationship discount in ConversationMemory (same pattern as ShopData).")]
    public string marketKeeperNpcId;

    [Header("Catalog — animals available to buy")]
    public List<AnimalMarketEntry> catalogEntries = new List<AnimalMarketEntry>();

    [Header("Sell Settings")]
    [Tooltip("Fraction of an animal's catalog buyPrice paid when the player sells it back. 0.5 = 50%.")]
    [Range(0.05f, 1f)]
    public float sellRateMultiplier = 0.5f;

    [Tooltip("Sell price used when the owned animal's type isn't in this market's catalog.")]
    [Min(1)]
    public int defaultSellPriceIfNotInCatalog = 25;
}

[System.Serializable]
public class AnimalMarketEntry
{
    [Tooltip("AnimalData asset this entry sells.")]
    public AnimalData animalData;
    [Min(1)]
    public int buyPrice = 100;
    [Tooltip("Max purchases of this entry, total. -1 = unlimited.")]
    public int maxStock = -1;

    [System.NonSerialized]
    public int purchasedCount; // Runtime, restored from save data on load

    public bool IsUnlimited => maxStock < 0;
    public bool IsInStock => IsUnlimited || purchasedCount < maxStock;
}

} // namespace SowurShield.Animals
