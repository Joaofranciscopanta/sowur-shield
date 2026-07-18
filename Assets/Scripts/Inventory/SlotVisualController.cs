using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace SowurShield.Inventory
{
    /// <summary>
    /// Handles all visual updates and animations for InventorySlots.
    /// This includes item icon display, quantity text, rarity glows, selection borders, and all animations.
    /// </summary>
    public class SlotVisualController : MonoBehaviour
    {
        // UI References (set by InventorySlot)
        private Image itemIcon;
        private TextMeshProUGUI quantityText;
        private Image backgroundImage;
        private Image selectionBorder;
        private Image rarityGlow;
        private TextMeshProUGUI slotNumberText;

        // Visual Configuration
        private Color normalColor = new Color(0.1f, 0.1f, 0.15f, 0.3f);
        private Color selectedColor = new Color(0.1f, 0.1f, 0.15f, 0.8f);
        private Color emptySlotAlpha = new Color(0.1f, 0.1f, 0.15f, 0.8f);

        // Rarity Colors
        private Color rarityGlowUncommon = Color.green;
        private Color rarityGlowRare = Color.blue;
        private Color rarityGlowEpic = new Color(0.6f, 0f, 0f);
        private Color rarityGlowLegendary = new Color(1f, 0.5f, 0f);

        // Animation Settings
        private float hoverScale = 1f;
        private float clickScale = 0.0f;
        private float animationSpeed = 0f;
        private AnimationCurve scaleCurve = new AnimationCurve(new Keyframe(0, 0, 0, 2), new Keyframe(1, 1, 0, 0));

        // Animation State
        private Coroutine currentScaleAnimation;
        private Coroutine currentBorderPulse;
        private Coroutine currentGlowPulse;
        private Vector3 targetScale = Vector3.one;

        /// <summary>
        /// Initialize the visual controller with UI references from InventorySlot
        /// </summary>
        public void Initialize(
            Image itemIcon, TextMeshProUGUI quantityText, Image backgroundImage,
            Image selectionBorder, Image rarityGlow, TextMeshProUGUI slotNumberText,
            Color normalColor, Color selectedColor, Color emptySlotAlpha,
            float hoverScale, float clickScale, float animationSpeed, AnimationCurve scaleCurve,
            Color rarityGlowUncommon, Color rarityGlowRare, Color rarityGlowEpic, Color rarityGlowLegendary)
        {
            this.itemIcon = itemIcon;
            this.quantityText = quantityText;
            this.backgroundImage = backgroundImage;
            this.selectionBorder = selectionBorder;
            this.rarityGlow = rarityGlow;
            this.slotNumberText = slotNumberText;
            this.normalColor = normalColor;
            this.selectedColor = selectedColor;
            this.emptySlotAlpha = emptySlotAlpha;
            this.hoverScale = hoverScale;
            this.clickScale = clickScale;
            this.animationSpeed = animationSpeed;
            this.scaleCurve = scaleCurve;
            this.rarityGlowUncommon = rarityGlowUncommon;
            this.rarityGlowRare = rarityGlowRare;
            this.rarityGlowEpic = rarityGlowEpic;
            this.rarityGlowLegendary = rarityGlowLegendary;

            targetScale = Vector3.one;
        }

        /// <summary>
        /// Update all visual elements based on item stack
        /// </summary>
        public void UpdateVisuals(ItemStack itemStack, bool isSelected)
        {
            UpdateItemIcon(itemStack);
            UpdateQuantityText(itemStack);
            UpdateRarityGlow(itemStack);
        }

        /// <summary>
        /// Update item icon display
        /// </summary>
        private void UpdateItemIcon(ItemStack itemStack)
        {
            if (itemIcon == null) return;

            if (!itemStack.IsEmpty && itemStack.item != null && itemStack.item.icon != null)
            {
                itemIcon.sprite = itemStack.item.icon;
                itemIcon.color = Color.white;
                itemIcon.enabled = true;
            }
            else
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
            }
        }

        /// <summary>
        /// Update quantity text display
        /// </summary>
        private void UpdateQuantityText(ItemStack itemStack)
        {
            if (quantityText == null) return;

            if (!itemStack.IsEmpty && itemStack.quantity > 1)
            {
                quantityText.text = FormatQuantity(itemStack.quantity);
                quantityText.enabled = true;
            }
            else
            {
                quantityText.enabled = false;
            }
        }

        /// <summary>
        /// Update rarity glow effect
        /// </summary>
        private void UpdateRarityGlow(ItemStack itemStack)
        {
            if (rarityGlow == null) return;

            if (!itemStack.IsEmpty && itemStack.item != null)
            {
                Color glowColor = GetRarityGlowColor(itemStack.item.rarity);

                if (glowColor != Color.clear)
                {
                    glowColor.a = 0.4f;
                    rarityGlow.color = glowColor;
                    rarityGlow.gameObject.SetActive(true);

                    if (itemStack.item.rarity >= ItemRarity.Epic)
                    {
                        if (currentGlowPulse != null)
                            StopCoroutine(currentGlowPulse);
                        currentGlowPulse = StartCoroutine(PulseRarityGlow(glowColor, itemStack));
                    }
                }
                else
                {
                    rarityGlow.gameObject.SetActive(false);
                }
            }
            else
            {
                rarityGlow.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Set selection state visuals
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (selectionBorder != null)
            {
                selectionBorder.gameObject.SetActive(selected);

                if (selected)
                {
                    if (currentBorderPulse != null)
                        StopCoroutine(currentBorderPulse);
                    currentBorderPulse = StartCoroutine(PulseBorder(selected));
                }
            }
        }

        /// <summary>
        /// Start item change animation (when item added/removed)
        /// </summary>
        public Coroutine StartItemChangeAnimation()
        {
            return StartCoroutine(ItemChangeAnimation());
        }

        /// <summary>
        /// Start click animation
        /// </summary>
        public Coroutine StartClickAnimation(bool isHovered)
        {
            return StartCoroutine(ClickAnimation(isHovered));
        }

        /// <summary>
        /// Set target scale for hover/unhover
        /// </summary>
        public void SetTargetScale(Vector3 newTargetScale)
        {
            targetScale = newTargetScale;

            if (currentScaleAnimation != null)
            {
                StopCoroutine(currentScaleAnimation);
                currentScaleAnimation = null;
            }

            currentScaleAnimation = StartCoroutine(AnimateToTargetScale());
        }

        /// <summary>
        /// Reset to normal visual state
        /// </summary>
        public void ResetToNormalState()
        {
            CleanupAnimations();
            transform.localScale = Vector3.one;
            targetScale = Vector3.one;
        }

        /// <summary>
        /// Cleanup all animations
        /// </summary>
        public void CleanupAnimations()
        {
            if (currentScaleAnimation != null)
            {
                StopCoroutine(currentScaleAnimation);
                currentScaleAnimation = null;
            }

            if (currentBorderPulse != null)
            {
                StopCoroutine(currentBorderPulse);
                currentBorderPulse = null;
            }

            if (currentGlowPulse != null)
            {
                StopCoroutine(currentGlowPulse);
                currentGlowPulse = null;
            }

            transform.localScale = Vector3.one;
        }

        // ============================================================================
        // ANIMATION COROUTINES
        // ============================================================================

        private IEnumerator AnimateToTargetScale()
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            float duration = 1f / animationSpeed;

            if (targetScale == Vector3.zero)
                targetScale = Vector3.one;

            while (elapsed < duration && Vector3.Distance(transform.localScale, targetScale) > 0.01f)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                progress = scaleCurve.Evaluate(progress);

                transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
                yield return null;
            }

            transform.localScale = targetScale;
            currentScaleAnimation = null;
        }

        private IEnumerator ItemChangeAnimation()
        {
            float pulseScale = 1.15f;
            float duration = 0.2f;

            yield return StartCoroutine(AnimateToScale(Vector3.one * pulseScale, duration * 0.3f));
            yield return StartCoroutine(AnimateToScale(Vector3.one, duration * 0.7f));
        }

        private IEnumerator AnimateToScale(Vector3 newScale, float duration)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                progress = scaleCurve.Evaluate(progress);

                transform.localScale = Vector3.Lerp(startScale, newScale, progress);
                yield return null;
            }

            transform.localScale = newScale;
        }

        private IEnumerator ClickAnimation(bool isHovered)
        {
            float clickDuration = 0.1f;

            yield return StartCoroutine(AnimateToScale(Vector3.one * clickScale, clickDuration * 0.5f));

            Vector3 returnScale = isHovered ? Vector3.one * hoverScale : Vector3.one;
            yield return StartCoroutine(AnimateToScale(returnScale, clickDuration * 0.5f));
        }

        private IEnumerator PulseBorder(bool isSelected)
        {
            if (selectionBorder == null) yield break;

            Color baseColor = selectedColor;
            Color brightColor = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
            Color dimColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.6f);

            while (isSelected && selectionBorder.gameObject.activeInHierarchy)
            {
                yield return StartCoroutine(AnimateBorderColor(brightColor, 0.8f));
                yield return StartCoroutine(AnimateBorderColor(dimColor, 0.8f));
            }

            currentBorderPulse = null;
        }

        private IEnumerator AnimateBorderColor(Color targetColor, float duration)
        {
            if (selectionBorder == null) yield break;

            Color startColor = selectionBorder.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                progress = scaleCurve.Evaluate(progress);

                selectionBorder.color = Color.Lerp(startColor, targetColor, progress);
                yield return null;
            }

            selectionBorder.color = targetColor;
        }

        private IEnumerator PulseRarityGlow(Color baseColor, ItemStack itemStack)
        {
            if (rarityGlow == null) yield break;

            Color dimColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.2f);
            Color brightColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.8f);

            while (rarityGlow != null && rarityGlow.gameObject.activeInHierarchy && !itemStack.IsEmpty)
            {
                yield return StartCoroutine(AnimateGlowColor(brightColor, 1f));
                yield return StartCoroutine(AnimateGlowColor(dimColor, 1f));
            }

            currentGlowPulse = null;
        }

        private IEnumerator AnimateGlowColor(Color targetColor, float duration)
        {
            if (rarityGlow == null) yield break;

            Color startColor = rarityGlow.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                progress = scaleCurve.Evaluate(progress);

                rarityGlow.color = Color.Lerp(startColor, targetColor, progress);
                yield return null;
            }

            rarityGlow.color = targetColor;
        }

        // ============================================================================
        // UTILITY METHODS
        // ============================================================================

        private string FormatQuantity(int quantity)
        {
            if (quantity >= 1000000)
                return $"{quantity / 1000000f:0.#}M";
            if (quantity >= 1000)
                return $"{quantity / 1000f:0.#}K";
            return quantity.ToString();
        }

        private Color GetRarityGlowColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => Color.clear,
                ItemRarity.Uncommon => rarityGlowUncommon,
                ItemRarity.Rare => rarityGlowRare,
                ItemRarity.Epic => rarityGlowEpic,
                ItemRarity.Legendary => rarityGlowLegendary,
                _ => Color.clear
            };
        }

        private void OnDestroy()
        {
            CleanupAnimations();
        }
    }
} // namespace SowurShield.Inventory
