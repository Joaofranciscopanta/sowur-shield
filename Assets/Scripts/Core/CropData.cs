using UnityEngine;
using UnityEngine.Localization;
using SowurShield.Inventory;

namespace SowurShield.Core
{
    [CreateAssetMenu(fileName = "New Crop", menuName = "Farming/Crop Data")]
    public class CropData : ScriptableObject
    {
        [Header("Basic Information")]
        public string cropName = "New Crop"; // stable internal ID — used for CropUnlockTracker save flags and CropGrowthManager lookups. Never shown to the player.
        public LocalizedString displayName; // table "Farming", key "crop.<cropName>.name" — player-facing name shown in UI
        [TextArea(2, 4)]
        public string description = "A crop description";

        [Header("Items")]
        public Item seedItem; // The seed item needed to plant
        public Item harvestItem; // What you get when harvesting

        [Header("Mystery Seed")]
        [Tooltip("If true, this CropData represents a Mystery Seed: planting it rolls a random unlocked crop instead of growing this crop directly.")]
        public bool isMysterySeed = false;
        [Tooltip("Crops this mystery seed can roll. Each crop only becomes eligible once the player has harvested it at least once (tracked via CropUnlockTracker).")]
        public CropData[] possibleOutcomes;
        [Tooltip("Relative weight per outcome, same length/order as possibleOutcomes. Defaults to 1 if left empty.")]
        public int[] outcomeWeights;
        [Tooltip("If true, this crop is always eligible as a Mystery Seed outcome, even before it's ever been harvested.")]
        public bool startsUnlocked = false;

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

        /// <summary>
        /// Rolls a random crop from possibleOutcomes, weighted by outcomeWeights.
        /// Only crops already unlocked (harvested at least once) are eligible, mirroring
        /// the Hades II "Mystery Seed" pattern. Falls back to any outcome if none are unlocked yet.
        /// </summary>
        public CropData RollMysteryOutcome()
        {
            if (possibleOutcomes == null || possibleOutcomes.Length == 0)
                return null;

            var eligible = new System.Collections.Generic.List<CropData>();
            var eligibleWeights = new System.Collections.Generic.List<int>();

            for (int i = 0; i < possibleOutcomes.Length; i++)
            {
                CropData candidate = possibleOutcomes[i];
                if (candidate == null) continue;
                if (!CropUnlockTracker.IsUnlocked(candidate) && !candidate.startsUnlocked) continue;

                eligible.Add(candidate);
                int weight = (outcomeWeights != null && i < outcomeWeights.Length) ? outcomeWeights[i] : 1;
                eligibleWeights.Add(Mathf.Max(1, weight));
            }

            // Nothing unlocked yet — fall back to the full pool so mystery seeds are never dead items
            if (eligible.Count == 0)
            {
                for (int i = 0; i < possibleOutcomes.Length; i++)
                {
                    if (possibleOutcomes[i] == null) continue;
                    eligible.Add(possibleOutcomes[i]);
                    int weight = (outcomeWeights != null && i < outcomeWeights.Length) ? outcomeWeights[i] : 1;
                    eligibleWeights.Add(Mathf.Max(1, weight));
                }
            }

            if (eligible.Count == 0)
                return null;

            int totalWeight = 0;
            foreach (int w in eligibleWeights) totalWeight += w;

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;
            for (int i = 0; i < eligible.Count; i++)
            {
                cumulative += eligibleWeights[i];
                if (roll < cumulative)
                    return eligible[i];
            }

            return eligible[eligible.Count - 1];
        }

        // Player-facing name. Falls back to the internal cropName if displayName hasn't been
        // wired to a table entry yet, so older CropData assets degrade gracefully instead of a
        // blank label.
        public string GetDisplayName()
        {
            string localized = displayName.SafeGetLocalizedString();
            return string.IsNullOrEmpty(localized) ? cropName : localized;
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
} // namespace SowurShield.Core
