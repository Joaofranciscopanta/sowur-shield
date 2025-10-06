using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private Canvas canvas;
    private RectTransform canvasRect;
    private bool isVisible = false;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

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
            itemNameText.text = item.itemName;
            itemNameText.color = GetRarityColor(item.rarity);
        }

        // Set item type
        if (itemTypeText != null)
        {
            itemTypeText.text = $"<i>{item.itemType}</i>";
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
                itemValueText.text = $"<color=#FFD700>Value: {item.baseValue} coins</color>";
            }
            else
            {
                itemValueText.text = "<color=#888888>Cannot be sold</color>";
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
            sb.AppendLine($"<color=#AAAAAA>Max Stack: {item.maxStackSize}</color>");
        }

        // Consumable info
        if (item.isConsumable)
        {
            sb.AppendLine("<color=#90EE90>Consumable</color>");

            if (item.energyRestore > 0)
                sb.AppendLine($"  <color=#FFD700>+{item.energyRestore} Energy</color>");

            if (item.healthRestore > 0)
                sb.AppendLine($"  <color=#FF6B6B>+{item.healthRestore} Health</color>");
        }

        // Tool info
        if (item.itemTags.Contains("Tool") || item.itemTags.Contains("Weapon"))
        {
            if (item.toolLevel > 0)
                sb.AppendLine($"<color=#87CEEB>Tool Level: {item.toolLevel}</color>");

            if (item.durability > 0)
                sb.AppendLine($"<color=#FFA500>Durability: {item.durability}</color>");
            else if (item.durability == -1)
                sb.AppendLine("<color=#90EE90>Unbreakable</color>");
        }

        // Tags
        if (item.itemTags.Count > 0)
        {
            string tags = string.Join(", ", item.itemTags);
            sb.AppendLine($"<size=11><color=#888888>{tags}</color></size>");
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
