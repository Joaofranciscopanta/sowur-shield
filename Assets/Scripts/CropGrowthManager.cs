using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SowurShield.Inventory;

namespace SowurShield.Core
{
    // This component should be attached to each SoilBlock to manage crop growth
    public class CropGrowthManager : MonoBehaviour, ISaveable
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

        [Header("Soil Fertility")]
        [SerializeField] [Range(0.5f, 2f)] private float fertility = 1.0f;

        [Header("Debug")]

        // Events
        public System.Action<CropGrowthManager> OnCropGrown; // When crop advances a stage
        public System.Action<CropGrowthManager> OnCropReadyForHarvest; // When crop is ready
        public System.Action<CropGrowthManager> OnCropDied; // When crop dies
        public System.Action<CropGrowthManager> OnCropHarvested; // When crop is harvested
        public System.Action<CropGrowthManager> OnCropDayTick; // Every day the crop is alive, even without a stage change

        /// <summary>
        /// Static, fired alongside the instance OnCropHarvested event whenever ANY crop on the
        /// farm is harvested. Lets global listeners (e.g. AchievementManager) subscribe once
        /// instead of having to find and hook every CropGrowthManager instance in the scene.
        /// </summary>
        public static System.Action<CropGrowthManager> OnAnyCropHarvested;

        // Properties for external access
        public bool HasCrop => currentCrop != null;
        public bool IsReadyForHarvest => isReadyForHarvest && !isDead;
        public bool IsDead => isDead;
        public bool IsWatered => isWatered;
        public float Fertility { get => fertility; set => fertility = Mathf.Clamp(value, 0.5f, 2f); }
        public CropData CurrentCrop => currentCrop;
        public int CurrentGrowthStage => currentGrowthStage;
        public float GrowthProgress => HasCrop ? (float)currentGrowthStage / currentCrop.TotalStages : 0f;

        /// <summary>Days remaining until the crop advances to its next growth stage (0 if ready/dead/no crop).</summary>
        public int DaysUntilNextStage => (HasCrop && !isDead && !isReadyForHarvest)
            ? Mathf.Max(0, currentCrop.daysPerStage - daysInCurrentStage)
            : 0;

        private GameTimeController timeController;
        private Coroutine harvestPulseCoroutine;

        private void Awake()
        {
            SetupCropVisual();
            ConnectToTimeSystem();
            RegisterWithSaveManager();
        }

        private void RegisterWithSaveManager()
        {
            // Register with SaveManager for save/load functionality
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.RegisterSaveable(this);
            }
            else
            {
                // Try again in Start() - SaveManager might not be initialized yet
                StartCoroutine(DelayedRegistration());
            }
        }

        private System.Collections.IEnumerator DelayedRegistration()
        {
            // Wait a frame for SaveManager to initialize
            yield return null;

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.RegisterSaveable(this);
            }
        }

        private void SetupCropVisual()
        {
            // Create crop visual object if it doesn't exist
            if (cropVisualObject == null)
            {
                cropVisualObject = new GameObject("CropVisual");
                cropVisualObject.transform.SetParent(transform);
                cropVisualObject.transform.localPosition = new Vector3(0, 0.25f, -0.1f);
                cropVisualObject.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
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
        }

        private void OnDestroy()
        {
            if (timeController != null)
            {
                timeController.OnDayChanged -= OnDayChanged;
            }

            // Unregister from SaveManager
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.UnregisterSaveable(this);
            }
        }

        // Plant a new crop
        public bool PlantCrop(CropData cropData)
        {
            if (HasCrop)
                return false;

            if (cropData == null)
                return false;

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

            return true;
        }

        // Water the crop
        public void WaterCrop()
        {
            if (!HasCrop || isDead)
                return;

            isWatered = true;
            daysSinceLastWatered = 0;
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

            OnCropDayTick?.Invoke(this);
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
        }

        // Harvest the crop
        public int HarvestCrop()
        {
            if (!HasCrop || !isReadyForHarvest || isDead)
                return 0;

            StopHarvestPulse();

            int baseYield = currentCrop.GetRandomYield();
            int yield = Mathf.Max(1, Mathf.RoundToInt(baseYield * fertility));
            string harvestedCropName = currentCrop.cropName; // capture before RemoveCrop() can null it

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
            }
            else
            {
                // Remove crop completely
                RemoveCrop();
            }

            OnCropHarvested?.Invoke(this);
            OnAnyCropHarvested?.Invoke(this);
            TutorialManager.NotifyStepComplete("harvest");
            SowurShield.Dialogue.QuestManager.Instance?.NotifyObjective(
                SowurShield.Dialogue.QuestObjectiveType.HarvestCrop, harvestedCropName);
            return yield;
        }

        /// <summary>
        /// Called by the weather system on drought days to accelerate water depletion.
        /// Increments daysSinceLastWatered by an additional 1 and immediately checks
        /// whether the crop should die from thirst.
        /// </summary>
        public void SimulateDroughtDay()
        {
            if (currentCrop == null) return;

            daysSinceLastWatered++;

            if (currentCrop.requiresWater && daysSinceLastWatered >= currentCrop.maxDaysWithoutWater)
            {
                KillCrop();
            }
        }

        // Remove crop completely
        public void RemoveCrop()
        {
            if (!HasCrop)
                return;

            StopHarvestPulse();

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

            // Always render above soil — sync sorting layer and bump order
            SpriteRenderer soilRenderer = GetComponent<SpriteRenderer>();
            if (soilRenderer != null)
            {
                cropSpriteRenderer.sortingLayerID = soilRenderer.sortingLayerID;
                cropSpriteRenderer.sortingOrder = soilRenderer.sortingOrder + 1;
            }

            // Show dead sprite if crop is dead
            if (isDead)
            {
                StopHarvestPulse();
                Sprite deadSprite = currentCrop.deadCropSprite
                    ?? Resources.Load<Sprite>("Sprites/Crop_Dead");
                if (deadSprite != null)
                    cropSpriteRenderer.sprite = deadSprite;
                return;
            }

            // Show growth stage sprite
            Sprite stageSprite = currentCrop.GetSpriteForStage(currentGrowthStage);
            if (stageSprite != null)
                cropSpriteRenderer.sprite = stageSprite;

            // Pulse when ready for harvest so player knows to interact
            if (isReadyForHarvest && harvestPulseCoroutine == null)
                harvestPulseCoroutine = StartCoroutine(HarvestPulse());
            else if (!isReadyForHarvest)
                StopHarvestPulse();
        }

        private void StopHarvestPulse()
        {
            if (harvestPulseCoroutine != null)
            {
                StopCoroutine(harvestPulseCoroutine);
                harvestPulseCoroutine = null;
            }
            if (cropVisualObject != null)
                cropVisualObject.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
        }

        private System.Collections.IEnumerator HarvestPulse()
        {
            Vector3 baseScale = new Vector3(1.3f, 1.3f, 1f);
            Vector3 bigScale = new Vector3(1.5f, 1.5f, 1f);
            float speed = 2f;
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime * speed;
                float s = (Mathf.Sin(t) + 1f) * 0.5f; // 0..1
                cropVisualObject.transform.localScale = Vector3.Lerp(baseScale, bigScale, s);
                yield return null;
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

        // ============================================================================
        // ISAVEABLE IMPLEMENTATION
        // ============================================================================

        public void SaveData(GameData gameData)
        {
            if (gameData == null) return;
            if (!HasCrop)
                return;

            // Create crop save data
            var cropSaveData = new FarmingGameData.CropSaveData
            {
                position = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y)),
                cropType = currentCrop.cropName,
                growthStage = currentGrowthStage,
                growthProgress = daysInCurrentStage,
                isWatered = isWatered,
                isReady = isReadyForHarvest,
                quality = 1, // Default quality, could be extended later
                plantedDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") // Could track actual plant date
            };

            gameData.farmingData.activeCrops.Add(cropSaveData);
        }

        public void LoadData(GameData gameData)
        {
            if (gameData == null) return;
            Vector2Int myPosition = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));

            // Find crop data for this position
            var cropData = gameData.farmingData.activeCrops.FirstOrDefault(crop => crop.position == myPosition);

            if (cropData != null)
            {
                // Find the CropData asset
                CropData cropAsset = Resources.LoadAll<CropData>("Crops")
                    .FirstOrDefault(c => c.cropName == cropData.cropType);

                if (cropAsset != null)
                {
                    // Restore crop state
                    currentCrop = cropAsset;
                    currentGrowthStage = cropData.growthStage;
                    daysInCurrentStage = Mathf.RoundToInt(cropData.growthProgress);
                    isWatered = cropData.isWatered;
                    isReadyForHarvest = cropData.isReady;
                    isDead = false; // Could save/load death state if needed
                    regrowthCount = 0; // Could save/load regrowth count if needed

                    // Update visual
                    UpdateCropVisual();
                }
            }
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
                {
                    seasonalCrops.Add(crop);
                }
            }

            return seasonalCrops.ToArray();
        }
    }
} // namespace SowurShield.Core
