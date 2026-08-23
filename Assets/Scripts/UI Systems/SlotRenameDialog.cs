using System;
using TMPro;
using UnityEngine;
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

            SetPanelActive(false);
        }

        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(HandleConfirm);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(HandleCancel);
        }

        /// <summary>
        /// Shows the prompt seeded with <paramref name="currentName"/>.
        /// <paramref name="title"/> is expected already localized by the caller.
        /// </summary>
        public void Open(string currentName, string title = null)
        {
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
