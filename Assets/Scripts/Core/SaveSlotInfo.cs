namespace SowurShield.Core
{
    [System.Serializable]
    public class SaveSlotInfo
    {
        public string slotName;
        public bool isAutoSave;
        public bool isEmpty;
        public int currentDay;
        public string season;
        public int year;
        public int money;
        public float totalPlayTime;
        public string saveTimestamp;
        public long fileSizeBytes;

        /// <summary>
        /// Player-chosen label for the slot. Empty means "no custom name" and callers fall back
        /// to the formatted directory name ("Slot1" -> "Slot 1").
        ///
        /// This is a DISPLAY label only. The slot directory keeps its fixed name, because
        /// GetAllSlotInfos() enumerates Slot1..SlotN by path — renaming the folder would make
        /// the slot vanish from the picker.
        /// </summary>
        public string customName;

        /// <summary>Label to show for this slot: the custom name when set, else "Slot 1"-style.</summary>
        public string GetDisplayName(string autoSaveLabel, string slotPrefix)
        {
            if (!string.IsNullOrWhiteSpace(customName))
                return customName;
            if (isAutoSave)
                return autoSaveLabel;
            if (!string.IsNullOrEmpty(slotName) && slotName.StartsWith("Slot") && slotName.Length > 4)
                return slotPrefix + slotName.Substring(4);
            return slotName;
        }
    }
} // namespace SowurShield.Core
