using UnityEngine;

[CreateAssetMenu(fileName = "New Crop", menuName = "Farming/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("Basic Information")]
    public string cropName = "New Crop";
    [TextArea(2, 4)]
    public string description = "A crop description";

    [Header("Items")]
    public Item seedItem; // The seed item needed to plant
    public Item harvestItem; // What you get when harvesting

    [Header("Growth Configuration")]
    [Tooltip("Days each growth stage takes")]
    public int daysPerStage = 1;

    [Tooltip("Total number of growth stages (calculated from sprites)")]
    [SerializeField] private int totalStages;

    [Tooltip("Total days from planting to harvest (auto-calculated)")]
    [SerializeField] private int totalGrowthDays;

    [Header("Visual")]
    public Sprite[] growthStages; // All growth stage sprites
    public Sprite deadCropSprite; // When crop dies
    public Sprite seedlingSprite; // Optional: special sprite for just planted

    [Header("Growth Requirements")]
    public bool requiresWater = true;
    [Tooltip("Days without water before crop dies")]
    public int maxDaysWithoutWater = 3;

    [Tooltip("Can this crop grow in any season?")]
    public bool growsInAllSeasons = true;

    [Tooltip("Seasons this crop can grow in (if not all seasons)")]
    public Season[] validSeasons;

    [Header("Harvest")]
    [Range(1, 10)]
    public int minYield = 1;
    [Range(1, 20)]
    public int maxYield = 3;

    [Tooltip("Does this crop regrow after harvest (like berries)?")]
    public bool regrowsAfterHarvest = false;

    [Tooltip("Days to regrow if regrowsAfterHarvest is true")]
    public int regrowthDays = 7;

    [Tooltip("How many times can this crop regrow? (0 = infinite)")]
    public int maxRegrowths = 0;

    [Header("Special Properties")]
    public bool canGrowInWinter = false;
    public bool needsTrellis = false; // For climbing plants
    public bool spreadsToCells = false; // For plants that spread

    [Header("Value")]
    public int baseValue = 50; // Base selling price
    public float qualityMultiplier = 1.0f; // For quality system

    // Properties for easy access
    public int TotalStages => growthStages?.Length ?? 0;
    public int TotalGrowthDays => TotalStages * daysPerStage;
    public bool IsValidSeason(Season season) => growsInAllSeasons || System.Array.Exists(validSeasons, s => s == season);

    // Validate data when inspector values change
    private void OnValidate()
    {
        if (growthStages != null)
        {
            totalStages = growthStages.Length;
            totalGrowthDays = totalStages * daysPerStage;
        }

        // Ensure min yield doesn't exceed max yield
        if (minYield > maxYield)
            minYield = maxYield;

        // Validate regrowth settings
        if (!regrowsAfterHarvest)
        {
            regrowthDays = 0;
            maxRegrowths = 0;
        }
    }

    // Get sprite for specific growth stage
    public Sprite GetSpriteForStage(int stage)
    {
        if (growthStages == null || stage < 0 || stage >= growthStages.Length)
            return null;

        return growthStages[stage];
    }

    // Check if crop is ready for harvest (last stage)
    public bool IsReadyForHarvest(int currentStage)
    {
        return currentStage >= TotalStages - 1;
    }

    // Get random harvest yield
    public int GetRandomYield()
    {
        return Random.Range(minYield, maxYield + 1);
    }
}

[System.Serializable]
public enum Season
{
    Spring,
    Summer,
    Fall,
    Winter
}
