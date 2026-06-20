using UnityEngine;

namespace SowurShield.Core
{

/// <summary>
/// ScriptableObject defining a purchasable farm building.
/// The game effect is keyed by BuildingType; FarmBuildingManager exposes
/// IsBuilt() for other systems to query.
///
/// CREATE: Assets > Create > SowurShield > Farm Building Data
/// </summary>
[CreateAssetMenu(menuName = "SowurShield/Farm Building Data", fileName = "NewBuilding")]
public class FarmBuildingData : ScriptableObject
{
    [Header("Identity")]
    public BuildingType buildingType;
    public string buildingName = "New Building";
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    [Header("Cost")]
    [Min(0)]
    public int goldCost = 500;
    [Tooltip("Optional material cost — item name must match ItemDatabase exactly.")]
    public string materialItemName = "";
    [Min(0)]
    public int materialQuantity = 0;

    [Header("Effects (shown in UI — actual logic lives in game systems)")]
    [TextArea(1, 3)]
    public string effectDescription;
}

public enum BuildingType
{
    Barn,           // Increases AnimalZone capacity (default 5 → 10)
    Greenhouse,     // Allows planting crops out of season
    Silo,           // Harvest-All Upgrade: harvesting one ready crop harvests all ready crops on the farm
    Workshop        // Lucky Seed Upgrade: chance to refund the planted seed on harvest
}

} // namespace SowurShield.Core
