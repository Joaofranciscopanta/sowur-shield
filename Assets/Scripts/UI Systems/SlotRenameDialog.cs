using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using SowurShield.Core;

namespace SowurShield.UI
{
    /// <summary>
    /// Modal prompt for typing a save slot's display label.
    ///
    /// Deliberately owns no SaveManager logic — it collects a string and raises
    /// <see cref="OnConfirmed"/>. The caller decides which slot it applies to, so the same
    /// dialog serves both the main menu picker and the in-game pause menu.
    ///
    /// UNITY WIRING: assign panel, inputField, confirmButton and cancelButton. titleText and
    /// the two LocalizedStrings are optional; without them the prefab's literal text shows.
    /// </summary>
    public class SlotRenameDialog : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Localized Strings")]
        [Tooltip("All optional. Left unset, the prefab's literal text shows in every language.")]
        [SerializeField] private LocalizedString titleString;       // "mainmenu.slotrename.title"
        [SerializeField] private LocalizedString placeholderString; // "mainmenu.slotrename.placeholder"
        [SerializeField] private LocalizedString confirmString;     // "mainmenu.slotrename.confirm"
        [SerializeField] private LocalizedString cancelString;      // "mainmenu.slotrename.cancel"

        /// <summary>Raised with the typed label when the player confirms. Blank means "reset to default".</summary>
        public event Action<string> OnConfirmed;

        /// <summary>Raised when the player cancels or closes the dialog.</summary>
        public event Action OnCancelled;

        /// <summary>True while the dialog is visible.</summary>
        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(HandleConfirm);
            if (cancelButton != null) cancelButton.onClick.AddListener(HandleCancel);

            // Submitting with Enter is the reason this dialog exists as a text prompt at all.
            if (inputField != null)
            {
                inputField.characterLimit = SaveManager.MaxSlotNameLength;
                inputField.onSubmit.AddListener(_ => HandleConfirm());
            }

            ApplyLocalizedText();
            SetPanelActive(false);

            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged
                += HandleLocaleChanged;
        }

        private void HandleLocaleChanged(UnityEngine.Localization.Locale _) => ApplyLocalizedText();

        /// <summary>
        /// Pushes the localized strings onto the prefab's labels. Each is skipped when it
        /// resolves empty, so the authored text remains as the fallback before the tables
        /// preload rather than the label going blank.
        /// </summary>
        private void ApplyLocalizedText()
        {
            SetText(titleText, titleString);

            if (inputField != null)
            {
                SetText(inputField.placeholder as TextMeshProUGUI, placeholderString);
                SetText(LabelOf(confirmButton), confirmString);
                SetText(LabelOf(cancelButton), cancelString);
            }
        }

        private static TextMeshProUGUI LabelOf(Button button)
            => button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;

        private static void SetText(TextMeshProUGUI target, LocalizedString source)
        {
            if (target == null) return;
            string value = source.SafeGetLocalizedString();
            if (!string.IsNullOrEmpty(value)) target.text = value;
        }

        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(HandleConfirm);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(HandleCancel);

            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged
                -= HandleLocaleChanged;
        }

        /// <summary>
        /// Shows the prompt seeded with <paramref name="currentName"/>.
        /// <paramref name="title"/> is expected already localized by the caller.
        /// </summary>
        public void Open(string currentName, string title = null)
        {
            // Re-apply every time: Awake ran once, so a language change after startup would
            // otherwise leave the placeholder and buttons in the boot language. Measured --
            // switching en -> pt kept "Slot name...", "OK" and "Cancel" in English.
            ApplyLocalizedText();

            // An explicit title wins over the localized default.
            if (titleText != null && !string.IsNullOrEmpty(title))
                titleText.text = title;

            if (inputField != null)
            {
                inputField.text = currentName ?? string.Empty;
                inputField.caretPosition = inputField.text.Length;
            }

            SetPanelActive(true);

            // Selecting on the same frame the panel activates does not take — the object is not
            // yet considered active by the EventSystem. One frame later it does.
            if (inputField != null && isActiveAndEnabled)
                StartCoroutine(FocusNextFrame());
        }

        /// <summary>Hides the prompt without raising either event.</summary>
        public void Close()
        {
            SetPanelActive(false);
        }

        private System.Collections.IEnumerator FocusNextFrame()
        {
            yield return null;
            inputField.Select();
            inputField.ActivateInputField();
        }

        private void HandleConfirm()
        {
            string typed = inputField != null ? inputField.text : string.Empty;
            SetPanelActive(false);
            OnConfirmed?.Invoke(typed);
        }

        private void HandleCancel()
        {
            SetPanelActive(false);
            OnCancelled?.Invoke();
        }

        private void SetPanelActive(bool active)
        {
            if (panel != null && panel.activeSelf != active)
                panel.SetActive(active);
        }
    }
}
