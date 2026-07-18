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

    [Header("Indicators")]
    [SerializeField] private GameObject lockedIndicator;

    [Header("Localized Strings")]
    [SerializeField] private LocalizedString autoSaveText; // table "MainMenu", key "mainmenu.saveslotbutton.autosave"
    [SerializeField] private LocalizedString slotPrefixText; // table "MainMenu", key "mainmenu.saveslotbutton.slot_prefix"
    [SerializeField] private LocalizedString dayLabelText; // table "MainMenu", key "mainmenu.saveslotbutton.day_label"
    [SerializeField] private LocalizedString moneyLabelText; // table "MainMenu", key "mainmenu.saveslotbutton.money_label"

    /// <summary>
    /// Initializes the slot button with save info and callbacks.
    /// </summary>
    /// <param name="info">Slot data to display.</param>
    /// <param name="onClickAction">Callback when the main button is pressed.</param>
    /// <param name="onDeleteAction">Callback when the delete button is pressed. Pass null to hide the button.</param>
    /// <param name="locked">If true, the slot is non-interactable (shows locked indicator).</param>
    public void Initialize(SaveSlotInfo info, Action onClickAction, Action onDeleteAction = null, bool locked = false)
    {
        // Slot name — format "Slot1" → "Slot 1", "AutoSave" → "Auto Save"
        if (slotNameText != null)
        {
            if (info.isAutoSave)
                slotNameText.text = autoSaveText.SafeGetLocalizedString();
            else if (info.slotName.StartsWith("Slot") && info.slotName.Length > 4)
                slotNameText.text = slotPrefixText.SafeGetLocalizedString() + info.slotName.Substring(4);
            else
                slotNameText.text = info.slotName;
        }

        if (info.isEmpty)
        {
            SetGroupActive(contentGroup, false);
            SetGroupActive(emptyGroup, true);
        }
        else
        {
            SetGroupActive(contentGroup, true);
            SetGroupActive(emptyGroup, false);

            if (daySeasonText != null)
            {
                dayLabelText.Arguments = new object[] { info.currentDay, info.season, info.year };
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
            }
        }

        // Locked indicator
        if (lockedIndicator != null)
            lockedIndicator.SetActive(locked);
    }

    private void SetGroupActive(GameObject group, bool active)
    {
        if (group != null)
            group.SetActive(active);
    }
}

} // namespace SowurShield.UI
