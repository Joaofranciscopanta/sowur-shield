using UnityEngine;

namespace SowurShield.Core
{

/// <summary>
/// Central ScriptableObject for all gameplay balance constants.
/// Create one instance at Assets/Resources/GameBalance.asset and assign it
/// wherever a GameBalance reference is needed, or load via Resources.Load.
/// </summary>
[CreateAssetMenu(menuName = "SowurShield/Game Balance", fileName = "GameBalance")]
public class GameBalance : ScriptableObject
{
    [Header("Economy")]
    [Tooltip("Fraction of an item's base value paid when sold via SellBox. Default: 0.8 (80%).")]
    public float sellMultiplier = 0.8f;

    [Header("Animal — Happiness")]
    [Tooltip("Happiness gained when a player pets an animal for the first time today.")]
    public float petHappinessBonus = 5f;

    [Tooltip("Happiness gained when a player manually feeds an animal one food item.")]
    public float feedHappinessBonus = 3f;

    [Tooltip("Happiness gained per unit of food when the FeedingTrough auto-feeds an animal.")]
    public float autoFeedHappinessBonusPerUnit = 3f;

    [Tooltip("Happiness lost per day when the animal was NOT petted yesterday.")]
    public float dailyDecayNoPet = 0.5f;

    [Tooltip("Happiness lost per day when the animal was NOT fed yesterday.")]
    public float dailyDecayNoFeed = 1.0f;

    [Tooltip("Minimum happiness value. Animals cannot drop below this floor.")]
    public float happinessFloor = 20f;

    [Tooltip("Maximum happiness value.")]
    public float happinessCeiling = 100f;

    [Tooltip("Starting happiness for a new animal.")]
    public float initialHappiness = 50f;

    [Header("Animal — Combat Multiplier")]
    [Tooltip("Stat multiplier applied to combat when happiness is at the floor (happinessFloor).")]
    public float happinessMultiplierMin = 0.5f;

    [Tooltip("Stat multiplier applied to combat when happiness is at the ceiling (happinessCeiling).")]
    public float happinessMultiplierMax = 1.5f;

    [Header("Farming — Garden Upgrades")]
    [Tooltip("Chance (0-1) that harvesting a crop refunds its planted seed, when the Workshop Lucky Seed upgrade is built.")]
    public float luckySeedChance = 0.25f;

    [Header("Interaction Distances")]
    [Tooltip("Default proximity range for the E-key interaction system (most interactables).")]
    public float defaultInteractionRange = 2f;

    [Tooltip("Proximity range for the SellBox E-key interaction and auto-close distance.")]
    public float sellBoxInteractionRange = 3f;

    [Tooltip("Maximum distance the cursor tool can reach from the player.")]
    public float maxToolDistance = 2f;
}

} // namespace SowurShield.Core
