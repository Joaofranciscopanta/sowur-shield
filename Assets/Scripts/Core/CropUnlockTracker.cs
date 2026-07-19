using SowurShield.Inventory;

namespace SowurShield.Core
{
    /// <summary>
    /// Tracks which crops have been harvested at least once, persisted via GameData.worldFlags.
    /// Used by CropData.RollMysteryOutcome() to restrict Mystery Seed results to crops the
    /// player has already discovered — mirrors Hades II's GardenData SeedMystery requirements.
    /// </summary>
    public static class CropUnlockTracker
    {
        private const string FlagPrefix = "crop_unlocked_";

        public static bool IsUnlocked(CropData crop)
        {
            if (crop == null) return false;

            GameData data = SaveManager.Instance != null ? SaveManager.Instance.CurrentGameData : null;
            if (data == null) return false;

            return data.GetWorldFlag(FlagPrefix + crop.cropName);
        }

        public static void MarkUnlocked(CropData crop)
        {
            if (crop == null) return;

            GameData data = SaveManager.Instance != null ? SaveManager.Instance.CurrentGameData : null;
            if (data == null) return;

            data.SetWorldFlag(FlagPrefix + crop.cropName, true);
        }
    }
} // namespace SowurShield.Core
