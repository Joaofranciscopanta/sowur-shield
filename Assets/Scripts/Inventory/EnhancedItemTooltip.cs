using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using SowurShield.Core;
using SowurShield.UI;

namespace SowurShield.Inventory
{
    /// <summary>
    /// Enhanced tooltip with rich formatting and item details
    /// Shows item name, description, stats, rarity, and value
    /// </summary>
    public class EnhancedItemTooltip : MonoBehaviour
    {
        [Header("Tooltip Panel")]
        public GameObject tooltipPanel;
        public RectTransform tooltipRect;
        public Image backgroundImage;

        [Header("Content")]
        public TextMeshProUGUI itemNameText;
        public TextMeshProUGUI itemTypeText;
        public TextMeshProUGUI itemRarityText;
        public TextMeshProUGUI itemDescriptionText;
        public TextMeshProUGUI itemValueText;
        public TextMeshProUGUI itemStatsText;
        public Image itemIconImage;

        [Header("Rarity Colors")]
        public Color commonColor = Color.white;
        public Color uncommonColor = Color.green;
        public Color rareColor = Color.blue;
        public Color epicColor = new Color(0.5f, 0f, 0.5f);
        public Color legendaryColor = new Color(1f, 0.5f, 0f);

        [Header("Settings")]
        public Vector2 offset = new Vector2(15, -15);
        public float maxWidth = 300f;
        public bool followMouse = true;

        [Header("Theme")]
        public UITheme theme;

        [Header("Localization")]
        [SerializeField] private LocalizedString italicTextLocalized; // table "Inventory", key "inventory.tooltip.italic_text"
        [SerializeField] private LocalizedString valueCoinsLocalized; // table "Inventory", key "inventory.tooltip.value_coins"
        [SerializeField] private LocalizedString cannotBeSoldLocalized; // table "Inventory", key "inventory.tooltip.cannot_be_sold"
        [SerializeField] private LocalizedString maxStackLocalized; // table "Inventory", key "inventory.tooltip.max_stack"
        [SerializeField] private LocalizedString consumableLocalized; // table "Inventory", key "inventory.tooltip.consumable"
        [SerializeField] private LocalizedString energyLocalized; // table "Inventory", key "inventory.tooltip.energy"
        [SerializeField] private LocalizedString healthLocalized; // table "Inventory", key "inventory.tooltip.health"
        [SerializeField] private LocalizedString toolLevelLocalized; // table "Inventory", key "inventory.tooltip.tool_level"
        [SerializeField] private LocalizedString durabilityLocalized; // table "Inventory", key "inventory.tooltip.durability"
        [SerializeField] private LocalizedString unbreakableLocalized; // table "Inventory", key "inventory.tooltip.unbreakable"
        [SerializeField] private LocalizedString descriptionLocalized; // table "Inventory", key "inventory.tooltip.description"

        private Canvas canvas;
        private RectTransform canvasRect;
        private bool isVisible = false;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                canvasRect = canvas.GetComponent<RectTransform>();

            if (theme == null)
                theme = Resources.Load<UITheme>("UI/CozyUITheme");
            if (theme != null)
            {
                commonColor = theme.textDark;
                uncommonColor = theme.positive;
                legendaryColor = theme.warning;
            }

            HideTooltip();
        }

        private void Update()
        {
            if (isVisible && followMouse)
            {
                UpdatePosition();
            }
        }

        /// <summary>
        /// Show tooltip for an item
        /// </summary>
        public void ShowTooltip(Item item)
        {
            if (item == null || tooltipPanel == null)
            {
                HideTooltip();
                return;
            }

            // Set item name with rarity color
            if (itemNameText != null)
            {
                itemNameText.text = item.GetDisplayName();
                itemNameText.color = GetRarityColor(item.rarity);
            }

            // Set item type
            if (itemTypeText != null)
            {
                italicTextLocalized.Arguments = new object[] { item.itemType };
                itemTypeText.text = italicTextLocalized.SafeGetLocalizedString();
                itemTypeText.color = new Color(0.7f, 0.7f, 0.7f);
            }

            // Set rarity
            if (itemRarityText != null)
            {
                itemRarityText.text = item.rarity.ToString();
                itemRarityText.color = GetRarityColor(item.rarity);
            }

            // Set description
            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = item.description;
            }

            // Set value
            if (itemValueText != null)
            {
                if (item.canBeSold)
                {
                    valueCoinsLocalized.Arguments = new object[] { item.baseValue };
                    itemValueText.text = valueCoinsLocalized.SafeGetLocalizedString();
                }
                else
                {
                    itemValueText.text = cannotBeSoldLocalized.SafeGetLocalizedString();
                }
            }

            // Set stats/properties
            if (itemStatsText != null)
            {
                string statsText = BuildStatsText(item);
                itemStatsText.text = statsText;

                if (string.IsNullOrEmpty(statsText))
                    itemStatsText.gameObject.SetActive(false);
                else
                    itemStatsText.gameObject.SetActive(true);
            }

            // Set icon
            if (itemIconImage != null && item.icon != null)
            {
                itemIconImage.sprite = item.icon;
                itemIconImage.gameObject.SetActive(true);
            }
            else if (itemIconImage != null)
            {
                itemIconImage.gameObject.SetActive(false);
            }

            // Set background color based on rarity
            if (backgroundImage != null)
            {
                Color bgColor = GetRarityColor(item.rarity);
                bgColor.a = 0.15f;
                backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
                // Add a subtle tint
                backgroundImage.color = Color.Lerp(backgroundImage.color, bgColor, 0.2f);
            }

            tooltipPanel.SetActive(true);
            isVisible = true;

            // Force rebuild layout
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

            UpdatePosition();
        }

        /// <summary>
        /// Hide the tooltip
        /// </summary>
        public void HideTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);

            isVisible = false;
        }

        /// <summary>
        /// Build stats text from item properties
        /// </summary>
        private string BuildStatsText(Item item)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // Stackable info
            if (item.isStackable)
            {
                maxStackLocalized.Arguments = new object[] { item.maxStackSize };
                sb.AppendLine(maxStackLocalized.SafeGetLocalizedString());
            }

            // Consumable info
            if (item.isConsumable)
            {
                sb.AppendLine(consumableLocalized.SafeGetLocalizedString());

                if (item.energyRestore > 0)
                {
                    energyLocalized.Arguments = new object[] { item.energyRestore };
                    sb.AppendLine(energyLocalized.SafeGetLocalizedString());
                }

                if (item.healthRestore > 0)
                {
                    healthLocalized.Arguments = new object[] { item.healthRestore };
                    sb.AppendLine(healthLocalized.SafeGetLocalizedString());
                }
            }

            // Tool info
            if (item.itemTags.Contains("Tool") || item.itemTags.Contains("Weapon"))
            {
                if (item.toolLevel > 0)
                {
                    toolLevelLocalized.Arguments = new object[] { item.toolLevel };
                    sb.AppendLine(toolLevelLocalized.SafeGetLocalizedString());
                }

                if (item.durability > 0)
                {
                    durabilityLocalized.Arguments = new object[] { item.durability };
                    sb.AppendLine(durabilityLocalized.SafeGetLocalizedString());
                }
                else if (item.durability == -1)
                    sb.AppendLine(unbreakableLocalized.SafeGetLocalizedString());
            }

            // Tags
            if (item.itemTags.Count > 0)
            {
                string tags = string.Join(", ", item.itemTags);
                descriptionLocalized.Arguments = new object[] { tags };
                sb.AppendLine(descriptionLocalized.SafeGetLocalizedString());
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Update tooltip position to follow mouse
        /// </summary>
        private void UpdatePosition()
        {
            if (tooltipRect == null || canvasRect == null) return;

            // Get mouse position in canvas space
            Vector2 mousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Input.mousePosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out mousePos
            );

            // Apply offset
            mousePos += offset;

            // Clamp to stay within canvas bounds
            Vector2 tooltipSize = tooltipRect.sizeDelta;
            Vector2 canvasSize = canvasRect.sizeDelta;

            // Prevent going off right edge
            if (mousePos.x + tooltipSize.x > canvasSize.x / 2)
            {
                mousePos.x -= (tooltipSize.x + offset.x * 2);
            }

            // Prevent going off top edge
            if (mousePos.y + tooltipSize.y > canvasSize.y / 2)
            {
                mousePos.y -= (tooltipSize.y + offset.y * 2);
            }

            // Prevent going off left edge
            if (mousePos.x - tooltipSize.x < -canvasSize.x / 2)
            {
                mousePos.x = -canvasSize.x / 2 + 10;
            }

            // Prevent going off bottom edge
            if (mousePos.y - tooltipSize.y < -canvasSize.y / 2)
            {
                mousePos.y = -canvasSize.y / 2 + tooltipSize.y + 10;
            }

            tooltipRect.anchoredPosition = mousePos;
        }

        /// <summary>
        /// Get color for rarity level
        /// </summary>
        private Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common:
                    return commonColor;
                case ItemRarity.Uncommon:
                    return uncommonColor;
                case ItemRarity.Rare:
                    return rareColor;
                case ItemRarity.Epic:
                    return epicColor;
                case ItemRarity.Legendary:
                    return legendaryColor;
                default:
                    return commonColor;
            }
        }

        /// <summary>
        /// Show tooltip with custom text
        /// </summary>
        public void ShowTooltipWithText(string title, string description)
        {
            if (tooltipPanel == null) return;

            if (itemNameText != null)
                itemNameText.text = title;

            if (itemDescriptionText != null)
                itemDescriptionText.text = description;

            // Hide other elements
            if (itemTypeText != null)
                itemTypeText.gameObject.SetActive(false);
            if (itemRarityText != null)
                itemRarityText.gameObject.SetActive(false);
            if (itemValueText != null)
                itemValueText.gameObject.SetActive(false);
            if (itemStatsText != null)
                itemStatsText.gameObject.SetActive(false);
            if (itemIconImage != null)
                itemIconImage.gameObject.SetActive(false);

            tooltipPanel.SetActive(true);
            isVisible = true;

            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
            UpdatePosition();
        }
    }
} // namespace SowurShield.Inventory
