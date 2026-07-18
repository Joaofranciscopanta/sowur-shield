using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace SowurShield.Inventory
{
    /// <summary>
    /// Handles all SellBox-specific functionality for InventorySlots.
    /// This component manages sellable indicators, value display, and visual feedback for SellBox mode.
    /// </summary>
    public class SlotSellBoxAdapter : MonoBehaviour
    {
        // References (set by InventorySlot)
        private Image sellableIndicator;
        private TextMeshProUGUI valueText;
        private Image rejectHighlight;
        private Image selectionBorder;

        // Configuration
        private Color sellableColor = Color.green;
        private Color nonSellableColor = Color.red;

        // State
        private bool isSellBoxMode = false;
        private float currentSellMultiplier = 0.8f;

        // Coroutine tracking
        private Coroutine rejectFeedbackCoroutine;
        private Coroutine acceptFeedbackCoroutine;

        /// <summary>
        /// Initialize the adapter with UI references from InventorySlot
        /// </summary>
        public void Initialize(Image sellableIndicator, TextMeshProUGUI valueText, Image rejectHighlight, Image selectionBorder)
        {
            this.sellableIndicator = sellableIndicator;
            this.valueText = valueText;
            this.rejectHighlight = rejectHighlight;
            this.selectionBorder = selectionBorder;
        }

        /// <summary>
        /// Check if currently in SellBox mode
        /// </summary>
        public bool IsSellBoxMode => isSellBoxMode;

        /// <summary>
        /// Get current sell multiplier
        /// </summary>
        public float CurrentSellMultiplier => currentSellMultiplier;

        /// <summary>
        /// Enable SellBox mode with specified sell multiplier
        /// </summary>
        public void EnableSellBoxMode(float sellMultiplier = 0.8f)
        {
            isSellBoxMode = true;
            currentSellMultiplier = sellMultiplier;
        }

        /// <summary>
        /// Disable SellBox mode and hide all SellBox UI elements
        /// </summary>
        public void DisableSellBoxMode()
        {
            isSellBoxMode = false;

            // Hide SellBox-specific UI elements
            if (sellableIndicator != null)
                sellableIndicator.gameObject.SetActive(false);
            if (valueText != null)
                valueText.text = "";
            if (rejectHighlight != null)
                rejectHighlight.gameObject.SetActive(false);
        }

        /// <summary>
        /// Update SellBox display based on current item stack
        /// </summary>
        public void UpdateSellBoxDisplay(ItemStack itemStack, bool isSelected)
        {
            if (!isSellBoxMode) return;

            if (itemStack.IsEmpty)
            {
                if (sellableIndicator != null) sellableIndicator.gameObject.SetActive(false);
                if (valueText != null) valueText.text = "";
                if (rejectHighlight != null) rejectHighlight.gameObject.SetActive(false);
                return;
            }

            bool canSell = itemStack.item.canBeSold;

            // Show/hide sellable indicator
            if (sellableIndicator != null)
                sellableIndicator.gameObject.SetActive(canSell);

            // Show item value
            if (valueText != null)
            {
                if (canSell)
                {
                    int value = Mathf.RoundToInt(itemStack.item.baseValue * currentSellMultiplier * itemStack.quantity);
                    valueText.text = $"{value}c";
                }
                else
                {
                    valueText.text = "";
                }
            }

            // Show rejection highlight for non-sellable items (briefly)
            if (!canSell && rejectHighlight != null)
            {
                if (rejectFeedbackCoroutine != null)
                    StopCoroutine(rejectFeedbackCoroutine);
                rejectFeedbackCoroutine = StartCoroutine(ShowRejectFeedback());
            }
            else if (rejectHighlight != null)
            {
                rejectHighlight.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Show visual feedback when item is accepted into SellBox
        /// </summary>
        public void ShowAcceptFeedback(bool isSelected)
        {
            if (selectionBorder != null)
            {
                if (acceptFeedbackCoroutine != null)
                    StopCoroutine(acceptFeedbackCoroutine);
                acceptFeedbackCoroutine = StartCoroutine(ShowAcceptFeedbackCoroutine(isSelected));
            }
        }

        private IEnumerator ShowRejectFeedback()
        {
            if (rejectHighlight != null)
            {
                rejectHighlight.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.8f);
                rejectHighlight.gameObject.SetActive(false);
            }
            rejectFeedbackCoroutine = null;
        }

        private IEnumerator ShowAcceptFeedbackCoroutine(bool isSelected)
        {
            if (selectionBorder != null)
            {
                Color originalColor = selectionBorder.color;
                selectionBorder.color = sellableColor;
                selectionBorder.gameObject.SetActive(true);

                yield return new WaitForSeconds(0.3f);

                selectionBorder.color = originalColor;
                if (!isSelected)
                    selectionBorder.gameObject.SetActive(false);
            }
            acceptFeedbackCoroutine = null;
        }

        private void OnDestroy()
        {
            // Cleanup coroutines
            if (rejectFeedbackCoroutine != null)
                StopCoroutine(rejectFeedbackCoroutine);
            if (acceptFeedbackCoroutine != null)
                StopCoroutine(acceptFeedbackCoroutine);
        }
    }
} // namespace SowurShield.Inventory
