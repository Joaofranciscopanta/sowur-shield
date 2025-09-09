using UnityEngine;
using System.Collections.Generic;

// This component should be attached to each SoilBlock to manage crop growth
public class CropGrowthManager : MonoBehaviour
{
    [Header("Current Crop Status")]
    [SerializeField] private CropData currentCrop;
    [SerializeField] private int currentGrowthStage = 0;
    [SerializeField] private int daysInCurrentStage = 0;
    [SerializeField] private int daysSinceLastWatered = 0;
    [SerializeField] private int regrowthCount = 0;
    [SerializeField] private bool isReadyForHarvest = false;
    [SerializeField] private bool isDead = false;
    [SerializeField] private bool isWatered = false;

    [Header("Visual Components")]
    public SpriteRenderer cropSpriteRenderer;
    public GameObject cropVisualObject;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // Events
    public System.Action<CropGrowthManager> OnCropGrown; // When crop advances a stage
    public System.Action<CropGrowthManager> OnCropReadyForHarvest; // When crop is ready
    public System.Action<CropGrowthManager> OnCropDied; // When crop dies
    public System.Action<CropGrowthManager> OnCropHarvested; // When crop is harvested

    // Properties for external access
    public bool HasCrop => currentCrop != null;
    public bool IsReadyForHarvest => isReadyForHarvest && !isDead;
    public bool IsDead => isDead;
    public bool IsWatered => isWatered;
    public CropData CurrentCrop => currentCrop;
    public int CurrentGrowthStage => currentGrowthStage;
    public float GrowthProgress => HasCrop ? (float)currentGrowthStage / currentCrop.TotalStages : 0f;

    private GameTimeController timeController;

    private void Awake()
    {
        SetupCropVisual();
        ConnectToTimeSystem();
    }

    private void SetupCropVisual()
    {
        // Create crop visual object if it doesn't exist
        if (cropVisualObject == null)
        {
            cropVisualObject = new GameObject("CropVisual");
            cropVisualObject.transform.SetParent(transform);
            cropVisualObject.transform.localPosition = new Vector3(0, 0.25f, -0.1f);
        }

        // Setup sprite renderer
        if (cropSpriteRenderer == null)
        {
            cropSpriteRenderer = cropVisualObject.GetComponent<SpriteRenderer>();
            if (cropSpriteRenderer == null)
                cropSpriteRenderer = cropVisualObject.AddComponent<SpriteRenderer>();
        }

        // Initially hide the crop visual
        cropSpriteRenderer.enabled = false;

        // Set proper sorting order
        SpriteRenderer soilRenderer = GetComponent<SpriteRenderer>();
        if (soilRenderer != null)
            cropSpriteRenderer.sortingOrder = soilRenderer.sortingOrder + 1;
    }

    private void ConnectToTimeSystem()
    {
        timeController = GameTimeController.instance;
        if (timeController == null)
            timeController = FindFirstObjectByType<GameTimeController>();

        if (timeController != null)
        {
            timeController.OnDayChanged += OnDayChanged;
        }
        else
        {
            Debug.LogWarning("CropGrowthManager: GameTimeController not found!");
        }
    }

    private void OnDestroy()
    {
        if (timeController != null)
        {
            timeController.OnDayChanged -= OnDayChanged;
        }
    }

    // Plant a new crop
    public bool PlantCrop(CropData cropData)
    {
        if (HasCrop)
        {
            Debug.LogWarning("Cannot plant crop: soil already has a crop");
            return false;
        }

        if (cropData == null)
        {
            Debug.LogWarning("Cannot plant crop: crop data is null");
            return false;
        }

        // Initialize crop
        currentCrop = cropData;
        currentGrowthStage = 0;
        daysInCurrentStage = 0;
        daysSinceLastWatered = isWatered ? 0 : 1;
        regrowthCount = 0;
        isReadyForHarvest = false;
        isDead = false;

        // Show crop visual
        UpdateCropVisual();

        if (showDebugInfo)
            Debug.Log($"Planted {cropData.cropName}");

        return true;
    }

    // Water the crop
    public void WaterCrop()
    {
        if (!HasCrop || isDead)
            return;

        isWatered = true;
        daysSinceLastWatered = 0;

        if (showDebugInfo)
            Debug.Log($"Watered {currentCrop.cropName}");
    }

    // Called when a new day starts
    private void OnDayChanged()
    {
        if (!HasCrop || isDead)
            return;

        // Reset watered status (water lasts one day)
        isWatered = false;
        daysSinceLastWatered++;

        // Check if crop dies from lack of water
        if (currentCrop.requiresWater && daysSinceLastWatered >= currentCrop.maxDaysWithoutWater)
        {
            KillCrop();
            return;
        }

        // Only grow if watered (or doesn't require water)
        bool canGrow = !currentCrop.requiresWater || daysSinceLastWatered == 1; // Just watered

        if (canGrow)
        {
            daysInCurrentStage++;

            // Check if ready to advance to next stage
            if (daysInCurrentStage >= currentCrop.daysPerStage)
            {
                AdvanceGrowthStage();
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"Day changed - Crop: {currentCrop.cropName}, Stage: {currentGrowthStage}, Days in stage: {daysInCurrentStage}, Days since watered: {daysSinceLastWatered}");
        }
    }

    // Advance to next growth stage
    private void AdvanceGrowthStage()
    {
        daysInCurrentStage = 0;
        currentGrowthStage++;

        // Check if crop is now ready for harvest
        if (currentGrowthStage >= currentCrop.TotalStages - 1)
        {
            currentGrowthStage = currentCrop.TotalStages - 1;
            isReadyForHarvest = true;
            OnCropReadyForHarvest?.Invoke(this);

            if (showDebugInfo)
                Debug.Log($"{currentCrop.cropName} is ready for harvest!");
        }

        UpdateCropVisual();
        OnCropGrown?.Invoke(this);
    }

    // Kill the crop
    private void KillCrop()
    {
        if (!HasCrop)
            return;

        isDead = true;
        isReadyForHarvest = false;
        UpdateCropVisual();
        OnCropDied?.Invoke(this);

        if (showDebugInfo)
            Debug.Log($"{currentCrop.cropName} died from lack of water!");
    }

    // Harvest the crop
    public int HarvestCrop()
    {
        if (!HasCrop || !isReadyForHarvest || isDead)
            return 0;

        int yield = currentCrop.GetRandomYield();

        // Handle regrowth
        if (currentCrop.regrowsAfterHarvest &&
            (currentCrop.maxRegrowths == 0 || regrowthCount < currentCrop.maxRegrowths))
        {
            // Reset for regrowth
            regrowthCount++;
            currentGrowthStage = 0;
            daysInCurrentStage = 0;
            isReadyForHarvest = false;
            UpdateCropVisual();

            if (showDebugInfo)
                Debug.Log($"Harvested {yield} {currentCrop.harvestItem.itemName} - Regrowing (#{regrowthCount})");
        }
        else
        {
            // Remove crop completely
            RemoveCrop();

            if (showDebugInfo)
                Debug.Log($"Harvested {yield} {currentCrop.harvestItem.itemName} - Crop removed");
        }

        OnCropHarvested?.Invoke(this);
        return yield;
    }

    // Remove crop completely
    public void RemoveCrop()
    {
        if (!HasCrop)
            return;

        currentCrop = null;
        currentGrowthStage = 0;
        daysInCurrentStage = 0;
        daysSinceLastWatered = 0;
        regrowthCount = 0;
        isReadyForHarvest = false;
        isDead = false;
        isWatered = false;

        if (cropSpriteRenderer != null)
            cropSpriteRenderer.enabled = false;

        if (showDebugInfo)
            Debug.Log("Crop removed");
    }

    // Update the visual appearance of the crop
    private void UpdateCropVisual()
    {
        if (!HasCrop || cropSpriteRenderer == null)
        {
            if (cropSpriteRenderer != null)
                cropSpriteRenderer.enabled = false;
            return;
        }

        cropSpriteRenderer.enabled = true;

        // Show dead sprite if crop is dead
        if (isDead && currentCrop.deadCropSprite != null)
        {
            cropSpriteRenderer.sprite = currentCrop.deadCropSprite;
            return;
        }

        // Show growth stage sprite
        Sprite stageSprite = currentCrop.GetSpriteForStage(currentGrowthStage);
        if (stageSprite != null)
        {
            cropSpriteRenderer.sprite = stageSprite;
        }
    }

    // Force update visual (useful for editor testing)
    [ContextMenu("Update Visual")]
    public void ForceUpdateVisual()
    {
        UpdateCropVisual();
    }

    // Get crop info for UI/debugging
    public string GetCropInfo()
    {
        if (!HasCrop)
            return "No crop planted";

        if (isDead)
            return $"{currentCrop.cropName} (DEAD)";

        if (isReadyForHarvest)
            return $"{currentCrop.cropName} (Ready for harvest!)";

        float progressPercent = (GrowthProgress * 100f);
        return $"{currentCrop.cropName} - Stage {currentGrowthStage + 1}/{currentCrop.TotalStages} ({progressPercent:F0}%)";
    }
}

// Static helper class for crop management
public static class CropDatabase
{
    private static CropData[] allCrops;

    // Load all crop data from Resources folder
    public static CropData[] GetAllCrops()
    {
        if (allCrops == null)
        {
            allCrops = Resources.LoadAll<CropData>("Crops");

            if (allCrops.Length == 0)
            {
                Debug.LogWarning("No CropData found in Resources/Crops folder!");
            }
        }

        return allCrops;
    }

    // Find crop data by seed item
    public static CropData GetCropDataForSeed(Item seedItem)
    {
        if (seedItem == null)
            return null;

        CropData[] crops = GetAllCrops();
        foreach (CropData crop in crops)
        {
            if (crop.seedItem == seedItem)
                return crop;
        }

        return null;
    }

    // Find crops that can grow in specific season
    public static CropData[] GetCropsForSeason(Season season)
    {
        CropData[] allCrops = GetAllCrops();
        System.Collections.Generic.List<CropData> seasonalCrops = new System.Collections.Generic.List<CropData>();

        foreach (CropData crop in allCrops)
        {
            if (crop.IsValidSeason(season))
                seasonalCrops.Add(crop);
        }

        return seasonalCrops.ToArray();
    }
}
