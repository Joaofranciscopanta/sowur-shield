using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using SowurShield.Core;

namespace SowurShield.UI
{
    /// <summary>What the picker is being opened for.</summary>
    public enum SaveSlotPickerMode
    {
        Load,
        NewGame
    }

    /// <summary>
    /// Owns the main menu's save-slot picker panel: the slot list, its title and its back button.
    /// Extracted from MainMenuUI (TASK-012) to stop that class growing past 1000 lines.
    ///
    /// Deliberately knows nothing about what happens once a slot is chosen — it raises
    /// <see cref="OnSlotChosen"/> and lets MainMenuUI decide whether that means "load" or
    /// "start a new game". Localized title strings are resolved by the caller and passed in,
    /// so the LocalizedString fields (and their field_map.json auto-wiring) stay on MainMenuUI.
    ///
    /// UNITY WIRING: add this component to a GameObject in the MainMenu scene and assign
    /// slotPickerPanel, slotListParent, saveSlotButtonPrefab, backButton and titleText —
    /// the same five references that used to sit on MainMenuUI under "Slot Picker Panel".
    /// </summary>
    public class MainMenuSaveSlotController : MonoBehaviour
    {
        [Header("Slot Picker Panel")]
        [SerializeField] private GameObject slotPickerPanel;
        [SerializeField] private Transform slotListParent;
        [SerializeField] private GameObject saveSlotButtonPrefab;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Tooltip("Optional. Without it the rename button stays hidden and slots keep default names.")]
        [SerializeField] private SlotRenameDialog renameDialog;

        /// <summary>The slot names the picker shows, in display order.</summary>
        public static readonly string[] SlotNames = { "AutoSave", "Slot1", "Slot2", "Slot3" };

        /// <summary>Raised when the player picks a slot. Args: slot name, mode the picker was opened in.</summary>
        public event Action<string, SaveSlotPickerMode> OnSlotChosen;

        /// <summary>Raised when the player presses the picker's back button.</summary>
        public event Action OnBackRequested;

        /// <summary>Raised after a slot's files were erased, so the caller can refresh dependent UI.</summary>
        public event Action OnSlotDeleted;

        /// <summary>Raised after a slot was given a new label.</summary>
        public event Action OnSlotRenamed;

        /// <summary>The mode the picker was last opened in.</summary>
        public SaveSlotPickerMode CurrentMode { get; private set; }

        /// <summary>True while the picker panel is visible.</summary>
        public bool IsOpen => slotPickerPanel != null && slotPickerPanel.activeSelf;

        private void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(HandleBackClicked);
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(HandleBackClicked);
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>Show the picker in the given mode. <paramref name="title"/> is already localized.</summary>
        public void Open(SaveSlotPickerMode mode, string title)
        {
            CurrentMode = mode;
            SetTitle(title);
            Populate();
            SetPanelActive(true);
        }

        /// <summary>Hide the picker.</summary>
        public void Close()
        {
            SetPanelActive(false);
        }

        /// <summary>Replace the header text (used when localization tables finish preloading late).</summary>
        public void SetTitle(string title)
        {
            if (titleText != null)
                titleText.text = title;
        }

        /// <summary>Rebuild the slot rows for the current mode.</summary>
        public void Refresh()
        {
            Populate();
        }

        // =====================================================================
        // SLOT DATA (static so MainMenuUI can reuse it without opening the picker)
        // =====================================================================

        /// <summary>
        /// Slot metadata from SaveManager when it exists, falling back to a direct disk read
        /// (SaveManager is not guaranteed to be alive in the MainMenu scene).
        /// </summary>
        public static SaveSlotInfo[] GetSlotInfos()
        {
            return SaveManager.Instance != null
                ? SaveManager.Instance.GetAllSlotInfos()
                : ReadSlotInfosFromDisk();
        }

        /// <summary>Reads slot metadata straight from disk when SaveManager is not loaded yet.</summary>
        public static SaveSlotInfo[] ReadSlotInfosFromDisk()
        {
            var result = new SaveSlotInfo[SlotNames.Length];
            string savesRoot = Path.Combine(Application.persistentDataPath, "Saves");

            for (int i = 0; i < SlotNames.Length; i++)
            {
                string slotName = SlotNames[i];
                string metaPath = Path.Combine(savesRoot, slotName, "SlotMeta.json");

                if (File.Exists(metaPath))
                {
                    try
                    {
                        var info = JsonUtility.FromJson<SaveSlotInfo>(File.ReadAllText(metaPath));
                        if (info != null)
                        {
                            info.slotName = slotName;
                            info.isAutoSave = slotName == "AutoSave";
                            result[i] = info;
                            continue;
                        }
                    }
                    catch { }
                }

                result[i] = new SaveSlotInfo
                {
                    slotName = slotName,
                    isAutoSave = slotName == "AutoSave",
                    isEmpty = true
                };
            }

            return result;
        }

        // =====================================================================
        // INTERNALS
        // =====================================================================

        private void Populate()
        {
            if (slotListParent == null || saveSlotButtonPrefab == null)
                return;

            // Clear old rows
            // Destroy() is deferred to end of frame; a rename or delete repopulates in the same
            // frame and would then see the old rows and double the list.
            for (int i = slotListParent.childCount - 1; i >= 0; i--)
                DestroyImmediate(slotListParent.GetChild(i).gameObject);

            foreach (var info in GetSlotInfos())
            {
                GameObject go = Instantiate(saveSlotButtonPrefab, slotListParent);
                SaveSlotButton btn = go.GetComponent<SaveSlotButton>();
                if (btn == null) continue;

                string slotName = info.slotName;

                // Load mode locks empty slots; New Game mode allows overwriting any slot.
                bool locked = CurrentMode == SaveSlotPickerMode.Load && info.isEmpty;

                // AutoSave and empty slots have nothing to delete.
                Action onDelete = info.isEmpty || info.isAutoSave
                    ? null
                    : () => DeleteSlotAndRefresh(slotName);

                // Renaming a slot is meaningful in either mode, and unlike picking it, it is
                // still allowed while the row is locked for Load — a full slot is never locked.
                string currentLabel = info.customName;
                Action onRename = info.isEmpty || info.isAutoSave || renameDialog == null
                    ? null
                    : () => BeginRename(slotName, currentLabel);

                btn.Initialize(
                    info,
                    locked ? null : (Action)(() => OnSlotChosen?.Invoke(slotName, CurrentMode)),
                    onDelete,
                    locked,
                    onRename
                );
            }
        }

        /// <summary>Erases a slot's files and repopulates the list in place — does NOT start the game.</summary>
        private void DeleteSlotAndRefresh(string slotName)
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.DeleteSlot(slotName);
            }
            else
            {
                string dir = Path.Combine(Application.persistentDataPath, "Saves", slotName);
                if (Directory.Exists(dir))
                    foreach (string f in Directory.GetFiles(dir))
                        File.Delete(f);
            }

            Populate();
            OnSlotDeleted?.Invoke();
        }

        /// <summary>
        /// Opens the rename prompt for one slot. The handler is re-subscribed per open and
        /// removed on close, so a cancelled rename cannot leak into the next slot's dialog.
        /// </summary>
        private void BeginRename(string slotName, string currentLabel)
        {
            if (renameDialog == null) return;

            Action<string> onConfirmed = null;
            Action onCancelled = null;

            onConfirmed = typed =>
            {
                renameDialog.OnConfirmed -= onConfirmed;
                renameDialog.OnCancelled -= onCancelled;

                if (SaveManager.Instance != null)
                    SaveManager.Instance.RenameSlot(slotName, typed);
                else
                    RenameSlotOnDisk(slotName, typed);

                Populate();
                OnSlotRenamed?.Invoke();
            };

            onCancelled = () =>
            {
                renameDialog.OnConfirmed -= onConfirmed;
                renameDialog.OnCancelled -= onCancelled;
            };

            renameDialog.OnConfirmed += onConfirmed;
            renameDialog.OnCancelled += onCancelled;
            renameDialog.Open(currentLabel);
        }

        /// <summary>
        /// Writes the label straight to SlotMeta.json. SaveManager is not guaranteed to exist in
        /// the MainMenu scene, which is exactly where the picker lives.
        /// </summary>
        private static void RenameSlotOnDisk(string slotName, string newName)
        {
            try
            {
                string metaPath = Path.Combine(
                    Application.persistentDataPath, "Saves", slotName, "SlotMeta.json");
                if (!File.Exists(metaPath)) return;

                var info = JsonUtility.FromJson<SaveSlotInfo>(File.ReadAllText(metaPath));
                if (info == null) return;

                string clean = string.IsNullOrWhiteSpace(newName) ? string.Empty : newName.Trim();
                if (clean.Length > SaveManager.MaxSlotNameLength)
                    clean = clean.Substring(0, SaveManager.MaxSlotNameLength);

                info.customName = clean;
                File.WriteAllText(metaPath, JsonUtility.ToJson(info, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[MainMenuSaveSlotController] Rename failed for '{slotName}': {e.Message}");
            }
        }

        private void HandleBackClicked()
        {
            OnBackRequested?.Invoke();
        }

        private void SetPanelActive(bool active)
        {
            if (slotPickerPanel != null && slotPickerPanel.activeSelf != active)
                slotPickerPanel.SetActive(active);
        }
    }
}
