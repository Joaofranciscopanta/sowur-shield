using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ItemTooltip : MonoBehaviour
{
    [Header("UI Components")]
    public RectTransform tooltipPanel;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI itemDetailsText;
    public Image itemIcon;
    public Image rarityBorder;
    public Image background;

    [Header("Visual Settings")]
    public Vector2 padding = new Vector2(20f, 15f);
    public float fadeSpeed = 5f;
    public float scaleSpeed = 8f;
    public Vector3 targetScale = Vector3.one;
    public AnimationCurve showCurve = new AnimationCurve(new Keyframe(0, 0, 0, 2), new Keyframe(1, 1, 0, 0));
    public AnimationCurve hideCurve = new AnimationCurve(new Keyframe(0, 1, 0, 0), new Keyframe(1, 0, 2, 0));

    [Header("Rarity Colors")]
    public Color commonColor = Color.white;
    public Color uncommonColor = Color.green;
    public Color rareColor = Color.blue;
    public Color epicColor = new Color(0.6f, 0f, 1f);
    public Color legendaryColor = new Color(1f, 0.5f, 0f);

    [Header("Layout Settings")]
    public float maxWidth = 300f;
    public float iconSize = 48f;
    public Vector2 screenPadding = new Vector2(20f, 20f);

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private bool isVisible = false;
    private Coroutine showHideCoroutine;
    private Camera uiCamera;

    private void Awake()
    {
        SetupComponents();
        SetupLayout();
        Hide();
    }

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        uiCamera = canvas?.worldCamera ?? Camera.main;
    }

    private void SetupComponents()
    {
        // Ensure we have a CanvasGroup for fading
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Setup tooltip panel if not assigned
        if (tooltipPanel == null)
        {
            tooltipPanel = GetComponent<RectTransform>();
        }

        // Create background if needed
        SetupBackground();

        // Create layout components
        SetupLayoutComponents();
    }

    private void SetupBackground()
    {
        if (background == null)
        {
            background = GetComponent<Image>();

            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
            }
        }

        // Style the background
        background.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        background.type = Image.Type.Sliced;

        // Add shadow effect
        Shadow shadow = GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = gameObject.AddComponent<Shadow>();
        }
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(2, -2);
    }

    private void SetupLayoutComponents()
    {
        // Create main content container
        GameObject contentContainer = new GameObject("Content");
        contentContainer.transform.SetParent(transform, false);

        RectTransform contentRect = contentContainer.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(padding.x, padding.y);
        contentRect.offsetMax = new Vector2(-padding.x, -padding.y);

        // Add VerticalLayoutGroup
        VerticalLayoutGroup verticalLayout = contentContainer.AddComponent<VerticalLayoutGroup>();
        verticalLayout.childControlHeight = false;
        verticalLayout.childControlWidth = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.spacing = 8f;
        verticalLayout.padding = new RectOffset(0, 0, 0, 0);
        verticalLayout.childAlignment = TextAnchor.UpperLeft;

        // Create header section with icon and name
        CreateHeaderSection(contentContainer);

        // Create description section
        CreateDescriptionSection(contentContainer);

        // Create details section
        CreateDetailsSection(contentContainer);

        // Add ContentSizeFitter to adjust tooltip size
        ContentSizeFitter sizeFitter = gameObject.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = gameObject.AddComponent<ContentSizeFitter>();
        }
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void CreateHeaderSection(GameObject parent)
    {
        GameObject headerSection = new GameObject("Header");
        headerSection.transform.SetParent(parent.transform, false);

        // Add horizontal layout for icon + name
        HorizontalLayoutGroup headerLayout = headerSection.AddComponent<HorizontalLayoutGroup>();
        headerLayout.childControlHeight = true;
        headerLayout.childControlWidth = false;
        headerLayout.childForceExpandHeight = false;
        headerLayout.childForceExpandWidth = false;
        headerLayout.spacing = 12f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;

        // Create icon container
        CreateIconContainer(headerSection);

        // Create name text container
        CreateNameContainer(headerSection);

        // Set preferred height for header
        LayoutElement headerElement = headerSection.AddComponent<LayoutElement>();
        headerElement.preferredHeight = iconSize + 8f;
    }

    private void CreateIconContainer(GameObject parent)
    {
        GameObject iconContainer = new GameObject("IconContainer");
        iconContainer.transform.SetParent(parent.transform, false);

        // Add item icon
        if (itemIcon == null)
        {
            itemIcon = iconContainer.AddComponent<Image>();
        }
        else
        {
            itemIcon.transform.SetParent(iconContainer.transform, false);
        }

        itemIcon.preserveAspect = true;
        itemIcon.type = Image.Type.Simple;

        // Set icon size
        RectTransform iconRect = itemIcon.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);

        LayoutElement iconElement = iconContainer.AddComponent<LayoutElement>();
        iconElement.preferredWidth = iconSize;
        iconElement.preferredHeight = iconSize;
        iconElement.flexibleWidth = 0;
        iconElement.flexibleHeight = 0;

        // Add rarity border around icon
        CreateRarityBorder(iconContainer);
    }

    private void CreateRarityBorder(GameObject parent)
    {
        GameObject borderObj = new GameObject("RarityBorder");
        borderObj.transform.SetParent(parent.transform, false);
        borderObj.transform.SetSiblingIndex(0); // Behind icon

        if (rarityBorder == null)
        {
            rarityBorder = borderObj.AddComponent<Image>();
        }

        RectTransform borderRect = rarityBorder.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-3, -3);
        borderRect.offsetMax = new Vector2(3, 3);

        rarityBorder.type = Image.Type.Sliced;
    }

    private void CreateNameContainer(GameObject parent)
    {
        GameObject nameContainer = new GameObject("NameContainer");
        nameContainer.transform.SetParent(parent.transform, false);

        if (itemNameText == null)
        {
            itemNameText = nameContainer.AddComponent<TextMeshProUGUI>();
        }

        // Style the name text
        itemNameText.fontSize = 16;
        itemNameText.fontStyle = FontStyles.Bold;
        itemNameText.color = Color.white;
        itemNameText.alignment = TextAlignmentOptions.Left;
        itemNameText.overflowMode = TextOverflowModes.Ellipsis;

        LayoutElement nameElement = nameContainer.AddComponent<LayoutElement>();
        nameElement.flexibleWidth = 1;
        nameElement.preferredHeight = iconSize;
    }

    private void CreateDescriptionSection(GameObject parent)
    {
        GameObject descSection = new GameObject("Description");
        descSection.transform.SetParent(parent.transform, false);

        if (itemDescriptionText == null)
        {
            itemDescriptionText = descSection.AddComponent<TextMeshProUGUI>();
        }

        // Style description text
        itemDescriptionText.fontSize = 12;
        itemDescriptionText.fontStyle = FontStyles.Italic;
        itemDescriptionText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        itemDescriptionText.alignment = TextAlignmentOptions.TopLeft;
        itemDescriptionText.textWrappingMode = TMPro.TextWrappingModes.Normal;

        LayoutElement descElement = descSection.AddComponent<LayoutElement>();
        descElement.preferredWidth = maxWidth - (padding.x * 2) - iconSize - 12f;
        descElement.flexibleHeight = 1;
    }

    private void CreateDetailsSection(GameObject parent)
    {
        GameObject detailsSection = new GameObject("Details");
        detailsSection.transform.SetParent(parent.transform, false);

        if (itemDetailsText == null)
        {
            itemDetailsText = detailsSection.AddComponent<TextMeshProUGUI>();
        }

        // Style details text
        itemDetailsText.fontSize = 10;
        itemDetailsText.fontStyle = FontStyles.Normal;
        itemDetailsText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        itemDetailsText.alignment = TextAlignmentOptions.TopLeft;
        itemDetailsText.textWrappingMode = TMPro.TextWrappingModes.Normal;

        LayoutElement detailsElement = detailsSection.AddComponent<LayoutElement>();
        detailsElement.preferredWidth = maxWidth - (padding.x * 2);
        detailsElement.flexibleHeight = 1;
    }

    private void SetupLayout()
    {
        // Ensure proper anchoring for tooltip positioning
        if (tooltipPanel != null)
        {
            tooltipPanel.anchorMin = Vector2.zero;
            tooltipPanel.anchorMax = Vector2.zero;
            tooltipPanel.pivot = new Vector2(0, 1); // Top-left pivot for positioning
        }
    }

    public void ShowTooltip(ItemStack itemStack, Vector3 screenPosition)
    {
        if (itemStack.IsEmpty) return;

        UpdateTooltipContent(itemStack);
        UpdatePosition(screenPosition);
        Show();
    }

    public void ShowTooltip(Item item, Vector3 screenPosition)
    {
        if (item == null) return;
        ShowTooltip(new ItemStack(item, 1), screenPosition);
    }

    private void UpdateTooltipContent(ItemStack itemStack)
    {
        Item item = itemStack.item;
        if (item == null) return;

        // Update icon
        if (itemIcon != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.color = Color.white;
        }

        // Update name with rarity coloring
        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
            itemNameText.color = GetRarityColor(item.rarity);
        }

        // Update rarity border
        if (rarityBorder != null)
        {
            Color borderColor = GetRarityColor(item.rarity);
            borderColor.a = 0.8f;
            rarityBorder.color = borderColor;
        }

        // Update description
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = item.description;
        }

        // Update details
        if (itemDetailsText != null)
        {
            itemDetailsText.text = BuildDetailsText(itemStack);
        }
    }

    private string BuildDetailsText(ItemStack itemStack)
    {
        Item item = itemStack.item;
        System.Text.StringBuilder details = new System.Text.StringBuilder();

        // Item type and rarity
        details.AppendLine($"<color=#CCCCCC>Type:</color> {item.itemType}");
        details.AppendLine($"<color=#CCCCCC>Rarity:</color> <color={ColorUtility.ToHtmlStringRGB(GetRarityColor(item.rarity))}>{item.rarity}</color>");

        // Quantity if more than 1
        if (itemStack.quantity > 1)
        {
            details.AppendLine($"<color=#CCCCCC>Quantity:</color> {itemStack.quantity}");
        }

        // Stack info
        if (item.isStackable)
        {
            details.AppendLine($"<color=#CCCCCC>Max Stack:</color> {item.maxStackSize}");
        }

        // Tool properties
        if (item.itemType == ItemType.Tool || item.itemType == ItemType.Weapon)
        {
            if (item.toolLevel > 0)
                details.AppendLine($"<color=#CCCCCC>Tool Level:</color> {item.toolLevel}");

            if (item.durability > 0)
                details.AppendLine($"<color=#CCCCCC>Durability:</color> {item.durability}");
            else if (item.durability == -1)
                details.AppendLine($"<color=#CCCCCC>Durability:</color> <color=#00FF00>Infinite</color>");
        }

        // Consumable properties
        if (item.isConsumable)
        {
            if (item.energyRestore > 0)
                details.AppendLine($"<color=#CCCCCC>Energy Restore:</color> <color=#FFFF00>+{item.energyRestore}</color>");

            if (item.healthRestore > 0)
                details.AppendLine($"<color=#CCCCCC>Health Restore:</color> <color=#FF4444>+{item.healthRestore}</color>");
        }

        // Value
        if (item.canBeSold)
        {
            int totalValue = item.baseValue * itemStack.quantity;
            details.AppendLine($"<color=#CCCCCC>Value:</color> <color=#FFD700>{totalValue}g</color>");
        }

        // Tags
        if (item.itemTags != null && item.itemTags.Count > 0)
        {
            details.AppendLine($"<color=#CCCCCC>Tags:</color> {string.Join(", ", item.itemTags)}");
        }

        return details.ToString().TrimEnd();
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => commonColor,
            ItemRarity.Uncommon => uncommonColor,
            ItemRarity.Rare => rareColor,
            ItemRarity.Epic => epicColor,
            ItemRarity.Legendary => legendaryColor,
            _ => commonColor
        };
    }

    private void UpdatePosition(Vector3 screenPosition)
    {
        if (tooltipPanel == null || canvas == null) return;

        // Convert screen position to canvas position
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPosition,
            uiCamera,
            out canvasPosition
        );

        // Get tooltip size (after content is updated)
        Canvas.ForceUpdateCanvases();
        Vector2 tooltipSize = tooltipPanel.sizeDelta;

        // Calculate position with screen bounds checking
        Vector2 finalPosition = CalculateTooltipPosition(canvasPosition, tooltipSize);

        tooltipPanel.anchoredPosition = finalPosition;
    }

    private Vector2 CalculateTooltipPosition(Vector2 mousePosition, Vector2 tooltipSize)
    {
        if (canvas == null) return mousePosition;

        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector2 canvasSize = canvasRect.sizeDelta;

        // Start with offset from mouse
        Vector2 position = mousePosition + new Vector2(15, 15);

        // Check right boundary
        if (position.x + tooltipSize.x > canvasSize.x * 0.5f - screenPadding.x)
        {
            position.x = mousePosition.x - tooltipSize.x - 15;
        }

        // Check left boundary
        if (position.x < -canvasSize.x * 0.5f + screenPadding.x)
        {
            position.x = -canvasSize.x * 0.5f + screenPadding.x;
        }

        // Check top boundary
        if (position.y > canvasSize.y * 0.5f - screenPadding.y)
        {
            position.y = canvasSize.y * 0.5f - screenPadding.y;
        }

        // Check bottom boundary
        if (position.y - tooltipSize.y < -canvasSize.y * 0.5f + screenPadding.y)
        {
            position.y = mousePosition.y + tooltipSize.y + 15;
        }

        return position;
    }

    public void Show()
    {
        if (isVisible) return;

        isVisible = true;
        gameObject.SetActive(true);

        if (showHideCoroutine != null)
        {
            StopCoroutine(showHideCoroutine);
        }

        showHideCoroutine = StartCoroutine(ShowAnimation());
    }

    public void Hide()
    {
        if (!isVisible) return;

        if (showHideCoroutine != null)
        {
            StopCoroutine(showHideCoroutine);
        }

        showHideCoroutine = StartCoroutine(HideAnimation());
    }

    public void HideTooltip()
    {
        Hide();
    }

    private IEnumerator ShowAnimation()
    {
        // Start invisible and small
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.zero;

        float elapsed = 0f;
        float duration = 1f / scaleSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float curveValue = showCurve.Evaluate(progress);

            canvasGroup.alpha = curveValue;
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, curveValue);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        transform.localScale = targetScale;
        showHideCoroutine = null;
    }

    private IEnumerator HideAnimation()
    {
        float startAlpha = canvasGroup.alpha;
        Vector3 startScale = transform.localScale;

        float elapsed = 0f;
        float duration = 1f / fadeSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float curveValue = hideCurve.Evaluate(progress);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, curveValue);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, curveValue);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
        isVisible = false;
        showHideCoroutine = null;
    }

    private void OnDestroy()
    {
        if (showHideCoroutine != null)
        {
            StopCoroutine(showHideCoroutine);
        }
    }
}
