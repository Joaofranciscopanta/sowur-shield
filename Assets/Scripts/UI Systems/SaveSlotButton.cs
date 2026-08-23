using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using SowurShield.Core;
using UnityEngine.Localization;

namespace SowurShield.UI
{

/// <summary>
/// UI component for a single save slot row in the slot picker panel.
/// Assign to the root GameObject of the SaveSlotButton prefab.
/// </summary>
public class SaveSlotButton : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI slotNameText;
    [SerializeField] private TextMeshProUGUI daySeasonText;
    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI playTimeText;

    [Header("Groups")]
    [SerializeField] private GameObject contentGroup;
    [SerializeField] private GameObject emptyGroup;

    [Header("Buttons")]
    [SerializeField] private Button mainButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button renameButton;

    [Header("Indicators")]
    [SerializeField] private GameObject lockedIndicator;

    [Header("Localized Strings")]
    [SerializeField] private LocalizedString autoSaveText; // table "MainMenu", key "mainmenu.saveslotbutton.autosave"
    [SerializeField] private LocalizedString slotPrefixText; // table "MainMenu", key "mainmenu.saveslotbutton.slot_prefix"
    [SerializeField] private LocalizedString dayLabelText; // table "MainMenu", key "mainmenu.saveslotbutton.day_label"
    [SerializeField] private LocalizedString moneyLabelText; // table "MainMenu", key "mainmenu.saveslotbutton.money_label"
    [Tooltip("Label for an unused slot. Left unset, the prefab's literal 'Empty Slot' shows " +
             "in every language.")]
    [SerializeField] private LocalizedString emptySlotText; // table "MainMenu", key "mainmenu.saveslotbutton.empty"
    [SerializeField] private TextMeshProUGUI emptyText;

    [Tooltip("Labels for the two action buttons. Left unset, the prefab's literal text shows " +
             "in every language.")]
    [SerializeField] private LocalizedString deleteButtonText; // key "mainmenu.saveslotbutton.delete"
    [SerializeField] private LocalizedString renameButtonText; // key "mainmenu.saveslotbutton.rename"

    /// <summary>
    /// Initializes the slot button with save info and callbacks.
    /// </summary>
    /// <param name="info">Slot data to display.</param>
    /// <param name="onClickAction">Callback when the main button is pressed.</param>
    /// <param name="onDeleteAction">Callback when the delete button is pressed. Pass null to hide the button.</param>
    /// <param name="locked">If true, the slot is non-interactable (shows locked indicator).</param>
    public void Initialize(SaveSlotInfo info, Action onClickAction, Action onDeleteAction = null,
                           bool locked = false, Action onRenameAction = null)
    {
        // Slot name — format "Slot1" → "Slot 1", "AutoSave" → "Auto Save"
        // A player-set label wins over the formatted directory name. GetDisplayName falls back
        // to "Slot N" when customName is blank, so the localized strings still drive the default.
        if (slotNameText != null)
        {
            slotNameText.text = info.GetDisplayName(
                autoSaveText.SafeGetLocalizedString(),
                slotPrefixText.SafeGetLocalizedString());
        }

        if (info.isEmpty)
        {
            SetGroupActive(contentGroup, false);
            SetGroupActive(emptyGroup, true);

            if (emptyText != null)
            {
                string label = emptySlotText.SafeGetLocalizedString();
                if (!string.IsNullOrEmpty(label)) emptyText.text = label;
            }
        }
        else
        {
            SetGroupActive(contentGroup, true);
            SetGroupActive(emptyGroup, false);

            if (daySeasonText != null)
            {
                // info.season is the raw English enum name ("Spring") straight out of the save
                // file. Passing it through untranslated produced "Dia 1 — Spring, Ano 1" on
                // every slot in the Portuguese build.
                dayLabelText.Arguments = new object[]
                {
                    info.currentDay, LocalizeSeason(info.season), info.year
                };
                daySeasonText.text = dayLabelText.SafeGetLocalizedString();
            }

            if (timestampText != null)
                timestampText.text = info.saveTimestamp;

            if (moneyText != null)
            {
                moneyLabelText.Arguments = new object[] { info.money };
                moneyText.text = moneyLabelText.SafeGetLocalizedString();
            }

            if (playTimeText != null)
            {
                int totalMinutes = Mathf.FloorToInt(info.totalPlayTime / 60f);
                if (totalMinutes >= 1)
                {
                    int hours = totalMinutes / 60;
                    int minutes = totalMinutes % 60;
                    playTimeText.text = hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
                }
                else
                {
                    playTimeText.text = string.Empty;
                }
            }
        }

        // Main button
        if (mainButton != null)
        {
            mainButton.onClick.RemoveAllListeners();
            mainButton.interactable = !locked && (onClickAction != null);
            if (onClickAction != null)
                mainButton.onClick.AddListener(() => onClickAction());
        }

        // Delete button — hide for AutoSave or when no callback given
        if (deleteButton != null)
        {
            bool showDelete = !info.isAutoSave && onDeleteAction != null && !info.isEmpty;
            deleteButton.gameObject.SetActive(showDelete);
            if (showDelete)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(() => onDeleteAction());
                ApplyButtonLabel(deleteButton, deleteButtonText);
            }
        }

        // Rename button — same visibility rule as delete: a real, non-AutoSave save only.
        if (renameButton != null)
        {
            bool showRename = !info.isAutoSave && onRenameAction != null && !info.isEmpty;
            renameButton.gameObject.SetActive(showRename);
            if (showRename)
            {
                renameButton.onClick.RemoveAllListeners();
                renameButton.onClick.AddListener(() => onRenameAction());
                ApplyButtonLabel(renameButton, renameButtonText);
            }
        }

        // Locked indicator
        if (lockedIndicator != null)
            lockedIndicator.SetActive(locked);
    }

    /// <summary>
    /// Replaces a button's child label with its localized string. A blank result leaves the
    /// prefab's literal text alone, which is what shows before the tables finish preloading.
    /// </summary>
    private static void ApplyButtonLabel(Button button, LocalizedString source)
    {
        if (button == null) return;

        string label = source.SafeGetLocalizedString();
        if (string.IsNullOrEmpty(label)) return;

        var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null) text.text = label;
    }

    /// <summary>
    /// Map a save file's English season name onto the shared UI_Common season entry so the
    /// slot row reads in the player's language. Unknown values fall through unchanged rather
    /// than becoming blank — a wrong-but-visible season beats an empty one.
    /// </summary>
    private static string LocalizeSeason(string rawSeason)
    {
        if (string.IsNullOrEmpty(rawSeason)) return rawSeason;

        string key = rawSeason.Trim().ToLowerInvariant() switch
        {
            "spring" => "ui_common.season.spring",
            "summer" => "ui_common.season.summer",
            "fall" or "autumn" => "ui_common.season.fall",
            "winter" => "ui_common.season.winter",
            _ => null
        };
        if (key == null) return rawSeason;

        string localized = new LocalizedString("UI_Common", key).SafeGetLocalizedString();
        return string.IsNullOrEmpty(localized) ? rawSeason : localized;
    }

    private void SetGroupActive(GameObject group, bool active)
    {
        if (group != null)
            group.SetActive(active);
    }
}

} // namespace SowurShield.UI
