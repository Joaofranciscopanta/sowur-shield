using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    // Static flag to track if any slot is being dragged
    public static bool IsAnySlotDragging { get; private set; }
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI quantityText;
    public Image backgroundImage;
    public Image borderImage;

    [Header("Visual Settings")]
    public Color normalColor = new Color(0.1f, 0.1f, 0.15f, 0.3f);
    public Color selectedColor = new Color(0.1f, 0.1f, 0.15f, 0.8f);
    public Color emptySlotAlpha = new Color(0.1f, 0.1f, 0.15f, 0.8f);

    [Header("Animation Settings")]
    public float hoverScale = 1f;
    public float clickScale = 0.0f;
    public float animationSpeed = 0f;
    public AnimationCurve scaleCurve = new AnimationCurve(new Keyframe(0, 0, 0, 2), new Keyframe(1, 1, 0, 0));

    [Header("Rarity Colors")]
    public Color rarityGlowCommon = Color.white;
    public Color rarityGlowUncommon = Color.green;
    public Color rarityGlowRare = Color.blue;
    public Color rarityGlowEpic = new Color(0.6f, 0f, 0f);
    public Color rarityGlowLegendary = new Color(1f, 0.5f, 0f);

    [Header("SellBox Features")]
    public Image sellableIndicator;      // Shows coin icon for sellable items
    public TextMeshProUGUI valueText;    // Shows individual item value
    public Image rejectHighlight;        // Red highlight for non-sellable items
    public Color sellableColor = Color.green;
    public Color nonSellableColor = Color.red;

    // ============================================================================
    // PRIVATE FIELDS
    // ============================================================================

    // Item data
    private ItemStack itemStack = new ItemStack();

    // State
    private bool isSelected = false;
    private bool isHovered = false;
    private bool isDragging = false;

    // References
    private Inventory inventoryManager;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    // Visual components
    private Image rarityGlow;
    private Image selectionBorder;
    private TextMeshProUGUI slotNumberText;

    // SellBox state
    public bool isSellBoxMode = false;
    private float currentSellMultiplier = 0.8f;

    // Animation
    private Coroutine currentScaleAnimation;
    private Vector3 targetScale = Vector3.one;

    // Drag system
    private GameObject dragPreview;
    private Vector3 originalPosition;
    public bool wasDroppedOnSlot = false;
    private ItemStack draggedItemStack = new ItemStack(); // Store the item being dragged
    private bool dragWasSuccessful = false; // Track if drag was successful

    // Slot properties
    public int slotIndex = -1;

    // ============================================================================
    // PROPERTIES
    // ============================================================================

    public ItemStack ItemStack => itemStack;
    public bool IsEmpty => itemStack.IsEmpty;
    public bool IsSelected => isSelected;

    // ============================================================================
    // UNITY LIFECYCLE
    // ============================================================================

    private void Awake()
    {
        InitializeReferences();
        SetupVisualComponents();
        targetScale = Vector3.one;
    }

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        // Continuously check hover state to prevent stuck states
        CheckHoverState();
    }

    private void OnDestroy()
    {
        CleanupAnimations();
    }

    private void OnDisable()
    {
        ResetToNormalState();
    }

    // ============================================================================
    // INITIALIZATION
    // ============================================================================

    private void InitializeReferences()
    {
        canvas = GetComponentInParent<Canvas>();
        inventoryManager = GetComponentInParent<Inventory>();

        if (inventoryManager == null)
            inventoryManager = FindFirstObjectByType<Inventory>();

        EnsureCanvasGroup();
    }

    private void EnsureCanvasGroup()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void SetupVisualComponents()
    {
        SetupBackgroundImage();
        SetupSelectionBorder();
        SetupRarityGlow();
        SetupItemIcon();
        SetupQuantityText();
        SetupSlotNumber();
        SetupSellBoxComponents();
    }

    private void SetupBackgroundImage()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();

            if (backgroundImage == null)
            {
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(transform, false);
                bgObj.transform.SetSiblingIndex(0);

                backgroundImage = bgObj.AddComponent<Image>();

                RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
            }
        }

        backgroundImage.color = normalColor;
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.raycastTarget = true; // Only the background should receive events
    }

    private void SetupSelectionBorder()
    {
        Transform existingBorder = transform.Find("SelectionBorder");
        if (existingBorder != null)
        {
            selectionBorder = existingBorder.GetComponent<Image>();
        }
        else
        {
            GameObject borderObj = new GameObject("SelectionBorder");
            borderObj.transform.SetParent(transform, false);
            borderObj.transform.SetSiblingIndex(1);

            selectionBorder = borderObj.AddComponent<Image>();

            RectTransform borderRect = borderObj.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-4, -4);
            borderRect.offsetMax = new Vector2(4, 4);
        }

        selectionBorder.color = selectedColor;
        selectionBorder.type = Image.Type.Sliced;
        selectionBorder.raycastTarget = false;
        selectionBorder.gameObject.SetActive(false);
    }

    private void SetupRarityGlow()
    {
        Transform existingGlow = transform.Find("RarityGlow");
        if (existingGlow != null)
        {
            rarityGlow = existingGlow.GetComponent<Image>();
        }
        else
        {
            GameObject glowObj = new GameObject("RarityGlow");
            glowObj.transform.SetParent(transform, false);
            glowObj.transform.SetSiblingIndex(2);

            rarityGlow = glowObj.AddComponent<Image>();

            RectTransform glowRect = glowObj.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-2, -2);
            glowRect.offsetMax = new Vector2(2, 2);
        }

        rarityGlow.color = Color.clear;
        rarityGlow.type = Image.Type.Sliced;
        rarityGlow.raycastTarget = false;
        rarityGlow.gameObject.SetActive(false);
    }

    private void SetupItemIcon()
    {
        if (itemIcon == null)
        {
            Transform iconTransform = transform.Find("ItemIcon");
            if (iconTransform != null)
            {
                itemIcon = iconTransform.GetComponent<Image>();
            }
            else
            {
                GameObject iconObj = new GameObject("ItemIcon");
                iconObj.transform.SetParent(transform, false);
                iconObj.transform.SetSiblingIndex(3);

                itemIcon = iconObj.AddComponent<Image>();

                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(8, 8);
                iconRect.offsetMax = new Vector2(-8, -8);
            }
        }

        itemIcon.preserveAspect = true;
        itemIcon.type = Image.Type.Simple;
        itemIcon.raycastTarget = false;
    }

    private void SetupQuantityText()
    {
        if (quantityText == null)
        {
            Transform textTransform = transform.Find("QuantityText");
            if (textTransform != null)
            {
                quantityText = textTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject textObj = new GameObject("QuantityText");
                textObj.transform.SetParent(transform, false);
                textObj.transform.SetSiblingIndex(4);

                quantityText = textObj.AddComponent<TextMeshProUGUI>();

                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(1, 0);
                textRect.anchorMax = new Vector2(1, 0);
                textRect.anchoredPosition = new Vector2(-5, 5);
                textRect.sizeDelta = new Vector2(30, 20);
            }
        }

        quantityText.fontSize = 12;
        quantityText.fontStyle = FontStyles.Bold;
        quantityText.color = Color.white;
        quantityText.alignment = TextAlignmentOptions.BottomRight;
        quantityText.raycastTarget = false;
    }

    private void SetupSlotNumber()
    {
        if (inventoryManager == null || slotIndex < 0 || slotIndex >= inventoryManager.hotbarSize)
            return;

        Transform numberTransform = transform.Find("SlotNumber");
        if (numberTransform != null)
        {
            slotNumberText = numberTransform.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            GameObject numberObj = new GameObject("SlotNumber");
            numberObj.transform.SetParent(transform, false);
            numberObj.transform.SetSiblingIndex(5);

            slotNumberText = numberObj.AddComponent<TextMeshProUGUI>();

            RectTransform numberRect = numberObj.GetComponent<RectTransform>();
            numberRect.anchorMin = new Vector2(0, 1);
            numberRect.anchorMax = new Vector2(0, 1);
            numberRect.anchoredPosition = new Vector2(5, -5);
            numberRect.sizeDelta = new Vector2(20, 20);
        }

        if (slotNumberText != null)
        {
            slotNumberText.text = (slotIndex + 1).ToString();
            slotNumberText.fontSize = 10;
            slotNumberText.color = new Color(1f, 1f, 1f, 0.6f);
            slotNumberText.fontStyle = FontStyles.Bold;
            slotNumberText.alignment = TextAlignmentOptions.TopLeft;
            slotNumberText.raycastTarget = false;
        }
    }

    private void SetupSellBoxComponents()
    {
        SetupSellableIndicator();
        SetupValueText();
        SetupRejectHighlight();
    }

    private void SetupSellableIndicator()
    {
        if (sellableIndicator == null)
        {
            Transform indicatorTransform = transform.Find("SellableIndicator");
            if (indicatorTransform != null)
            {
                sellableIndicator = indicatorTransform.GetComponent<Image>();
            }
            else
            {
                GameObject indicatorObj = new GameObject("SellableIndicator");
                indicatorObj.transform.SetParent(transform, false);
                indicatorObj.transform.SetSiblingIndex(6);

                sellableIndicator = indicatorObj.AddComponent<Image>();

                RectTransform indicatorRect = indicatorObj.GetComponent<RectTransform>();
                indicatorRect.anchorMin = new Vector2(0, 1);
                indicatorRect.anchorMax = new Vector2(0, 1);
                indicatorRect.anchoredPosition = new Vector2(6, -6);
                indicatorRect.sizeDelta = new Vector2(16, 16);
            }
        }

        if (sellableIndicator != null)
        {
            // You can set a coin sprite here if you have one
            sellableIndicator.color = new Color(1f, 0.84f, 0f, 0.8f); // Gold color
            sellableIndicator.raycastTarget = false;
            sellableIndicator.gameObject.SetActive(false);
        }
    }

    private void SetupValueText()
    {
        if (valueText == null)
        {
            Transform valueTransform = transform.Find("ValueText");
            if (valueTransform != null)
            {
                valueText = valueTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject valueObj = new GameObject("ValueText");
                valueObj.transform.SetParent(transform, false);
                valueObj.transform.SetSiblingIndex(7);

                valueText = valueObj.AddComponent<TextMeshProUGUI>();

                RectTransform valueRect = valueObj.GetComponent<RectTransform>();
                valueRect.anchorMin = new Vector2(0, 0);
                valueRect.anchorMax = new Vector2(0, 0);
                valueRect.anchoredPosition = new Vector2(4, 4);
                valueRect.sizeDelta = new Vector2(40, 14);
            }
        }

        if (valueText != null)
        {
            valueText.fontSize = 8;
            valueText.fontStyle = FontStyles.Bold;
            valueText.color = new Color(1f, 0.84f, 0f); // Gold color
            valueText.alignment = TextAlignmentOptions.BottomLeft;
            valueText.raycastTarget = false;
            valueText.text = "";
        }
    }

    private void SetupRejectHighlight()
    {
        if (rejectHighlight == null)
        {
            Transform rejectTransform = transform.Find("RejectHighlight");
            if (rejectTransform != null)
            {
                rejectHighlight = rejectTransform.GetComponent<Image>();
            }
            else
            {
                GameObject rejectObj = new GameObject("RejectHighlight");
                rejectObj.transform.SetParent(transform, false);
                rejectObj.transform.SetSiblingIndex(8);

                rejectHighlight = rejectObj.AddComponent<Image>();

                RectTransform rejectRect = rejectObj.GetComponent<RectTransform>();
                rejectRect.anchorMin = Vector2.zero;
                rejectRect.anchorMax = Vector2.one;
                rejectRect.offsetMin = Vector2.zero;
                rejectRect.offsetMax = Vector2.zero;
            }
        }

        if (rejectHighlight != null)
        {
            rejectHighlight.color = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red
            rejectHighlight.raycastTarget = false;
            rejectHighlight.gameObject.SetActive(false);
        }
    }

    // ============================================================================
    // PUBLIC METHODS
    // ============================================================================

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
        SetupSlotNumber();
    }

    public void SetItemStack(ItemStack stack)
    {
        bool wasEmpty = IsEmpty;
        bool willBeEmpty = (stack == null || stack.IsEmpty);

        if (stack == null)
        {
            itemStack.Clear();
        }
        else
        {
            itemStack.item = stack.item;
            itemStack.quantity = stack.quantity;
        }

        if (wasEmpty != willBeEmpty)
        {
            // Only start animation if GameObject is active and enabled
            if (gameObject.activeInHierarchy && enabled)
            {
                StartCoroutine(ItemChangeAnimation());
            }
        }

        UpdateVisuals();
    }

    public void ClearSlot()
    {
        itemStack.Clear();
        UpdateVisuals();
    }

    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;

        isSelected = selected;

        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(selected);

            if (selected)
            {
                StartCoroutine(PulseBorder());
            }
        }

        UpdateVisuals();
    }

    public bool TryAddItem(Item item, int quantity = 1)
    {
        if (IsEmpty)
        {
            itemStack.item = item;
            itemStack.quantity = quantity;
            UpdateVisuals();
            return true;
        }
        else if (itemStack.CanStack(item))
        {
            int leftover = itemStack.AddQuantity(quantity);
            UpdateVisuals();
            return leftover == 0;
        }
        return false;
    }

    public ItemStack RemoveItems(int quantity)
    {
        if (IsEmpty) return new ItemStack();

        int toRemove = Mathf.Min(quantity, itemStack.quantity);
        ItemStack removed = new ItemStack(itemStack.item, toRemove);

        itemStack.quantity -= toRemove;
        if (itemStack.quantity <= 0)
        {
            itemStack.Clear();
        }

        UpdateVisuals();
        return removed;
    }

    // Method to get the currently dragged item (for SellBox and other systems)
    public ItemStack GetDraggedItem()
    {
        return draggedItemStack;
    }

    // Method to permanently consume the dragged item (called when drop succeeds)
    public void ConsumeDraggedItem()
    {
        if (!draggedItemStack.IsEmpty)
        {
            Debug.Log($"Permanently consumed dragged item: {draggedItemStack.quantity}x {draggedItemStack.item.itemName}");
            dragWasSuccessful = true; // Mark drag as successful
            // Don't clear draggedItemStack here - let OnEndDrag handle it
        }
    }

    // Method to mark drag as successful (for partial consumption cases)
    public void MarkDragSuccessful()
    {
        dragWasSuccessful = true;
        // Don't clear draggedItemStack here - let OnEndDrag handle it
        Debug.Log($"Marked drag as successful for slot {slotIndex}");
    }

    // ============================================================================
    // SELLBOX SPECIFIC METHODS
    // ============================================================================

    public void EnableSellBoxMode(float sellMultiplier = 0.8f)
    {
        isSellBoxMode = true;
        currentSellMultiplier = sellMultiplier;
        UpdateSellBoxDisplay();
    }

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

    public void UpdateSellBoxDisplay()
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
            StartCoroutine(ShowRejectFeedback());
        }
        else if (rejectHighlight != null)
        {
            rejectHighlight.gameObject.SetActive(false);
        }
    }

    public void ShowAcceptFeedback()
    {
        if (selectionBorder != null)
        {
            StartCoroutine(ShowAcceptFeedbackCoroutine());
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
    }

    private IEnumerator ShowAcceptFeedbackCoroutine()
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
    }

    // ============================================================================
    // HOVER STATE MANAGEMENT
    // ============================================================================

    private void CheckHoverState()
    {
        if (canvas == null || isDragging) return;

        Vector2 mousePos = Mouse.current?.position.ReadValue() ?? Vector2.zero;
        bool mouseOverSlot = RectTransformUtility.RectangleContainsScreenPoint(
            transform as RectTransform,
            mousePos,
            canvas.worldCamera
        );

        if (isHovered && !mouseOverSlot)
        {
            ForceExitHover();
        }
        else if (!isHovered && mouseOverSlot)
        {
            ForceEnterHover();
        }
    }

    private void ForceEnterHover()
    {
        if (isHovered || isDragging) return;

        //isHovered = true;
        //SetTargetScale(Vector3.one * hoverScale);
        //UpdateVisuals();

        ShowTooltip();
    }

    private void ForceExitHover()
    {
        if (!isHovered) return;

        isHovered = false;
        SetTargetScale(Vector3.one);
        UpdateVisuals();

        HideTooltip();
    }

    private void ShowTooltip()
    {
        if (!IsEmpty && inventoryManager != null)
        {
            Vector2 mousePos = Mouse.current?.position.ReadValue() ?? Vector2.zero;
            Vector3 tooltipPosition = new Vector3(mousePos.x + 15, mousePos.y + 15, 0);
            inventoryManager.ShowTooltip(itemStack, tooltipPosition);
        }
    }

    private void HideTooltip()
    {
        if (inventoryManager != null)
        {
            inventoryManager.HideTooltip();
        }
    }

    // ============================================================================
    // VISUAL UPDATES
    // ============================================================================

    private void UpdateVisuals()
    {
        UpdateItemIcon();
        UpdateQuantityText();
        //UpdateBackgroundColor();
        UpdateRarityGlow();
        UpdateSellBoxDisplay();
    }

    private void UpdateItemIcon()
    {
        if (itemIcon == null) return;

        if (!IsEmpty && itemStack.item != null && itemStack.item.icon != null)
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

    private void UpdateQuantityText()
    {
        if (quantityText == null) return;

        if (!IsEmpty && itemStack.quantity > 1)
        {
            quantityText.text = FormatQuantity(itemStack.quantity);
            quantityText.enabled = true;
        }
        else
        {
            quantityText.enabled = false;
        }
    }

    private void UpdateRarityGlow()
    {
        if (rarityGlow == null) return;

        if (!IsEmpty && itemStack.item != null)
        {
            Color glowColor = GetRarityGlowColor(itemStack.item.rarity);

            if (glowColor != Color.clear)
            {
                glowColor.a = 0.4f;
                rarityGlow.color = glowColor;
                rarityGlow.gameObject.SetActive(true);

                if (itemStack.item.rarity >= ItemRarity.Epic)
                {
                    StartCoroutine(PulseRarityGlow(glowColor));
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

    // ============================================================================
    // ANIMATION SYSTEM
    // ============================================================================

    private void SetTargetScale(Vector3 newTargetScale)
    {
        targetScale = newTargetScale;

        if (currentScaleAnimation != null)
        {
            StopCoroutine(currentScaleAnimation);
            currentScaleAnimation = null;
        }

        currentScaleAnimation = StartCoroutine(AnimateToTargetScale());
    }

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

    private IEnumerator ClickAnimation()
    {
        float clickDuration = 0.1f;

        yield return StartCoroutine(AnimateToScale(Vector3.one * clickScale, clickDuration * 0.5f));

        Vector3 returnScale = isHovered ? Vector3.one * hoverScale : Vector3.one;
        yield return StartCoroutine(AnimateToScale(returnScale, clickDuration * 0.5f));
    }

    private IEnumerator PulseBorder()
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

    private IEnumerator AnimateBackgroundColor(Color targetColor)
    {
        if (backgroundImage == null) yield break;

        Color startColor = backgroundImage.color;
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            progress = scaleCurve.Evaluate(progress);

            backgroundImage.color = Color.Lerp(startColor, targetColor, progress);
            yield return null;
        }

        backgroundImage.color = targetColor;
    }

    private IEnumerator PulseRarityGlow(Color baseColor)
    {
        if (rarityGlow == null) yield break;

        Color dimColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.2f);
        Color brightColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.8f);

        while (rarityGlow != null && rarityGlow.gameObject.activeInHierarchy && !IsEmpty)
        {
            yield return StartCoroutine(AnimateGlowColor(brightColor, 1f));
            yield return StartCoroutine(AnimateGlowColor(dimColor, 1f));
        }
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

    private Color GetTargetBackgroundColor()
    {
        if (IsEmpty) return emptySlotAlpha;
        if (isSelected) return selectedColor;
        return normalColor;
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

    private void CleanupAnimations()
    {
        if (currentScaleAnimation != null)
        {
            StopCoroutine(currentScaleAnimation);
            currentScaleAnimation = null;
        }

        transform.localScale = Vector3.one;
    }

    private void ResetToNormalState()
    {
        isHovered = false;
        isDragging = false;

        CleanupAnimations();

        transform.localScale = Vector3.one;
        targetScale = Vector3.one;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        HideTooltip();
    }

    // ============================================================================
    // INPUT EVENT HANDLERS
    // ============================================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging) return;
        ForceEnterHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging) return;
        ForceExitHover();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            StartCoroutine(ClickAnimation());

            if (inventoryManager != null)
            {
                inventoryManager.SelectSlot(this);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isHovered)
        {
            SetTargetScale(Vector3.one * hoverScale);
        }
        else
        {
            SetTargetScale(Vector3.one);
        }
    }

    private void HandleRightClick()
    {
        if (IsEmpty || inventoryManager == null) return;

        bool shiftPressed = Keyboard.current?.leftShiftKey.isPressed ?? false;

        if (shiftPressed && itemStack.quantity > 1)
        {
            inventoryManager.SplitStack(this);
        }
        else if (itemStack.item.isConsumable)
        {
            inventoryManager.UseItem(this);
        }
    }

    // ============================================================================
    // DRAG AND DROP SYSTEM
    // ============================================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsEmpty || eventData.button != PointerEventData.InputButton.Left) return;

        isDragging = true;
        wasDroppedOnSlot = false; // Reset drop flag
        dragWasSuccessful = false; // Reset success flag
        originalPosition = transform.position;

        // Store the item being dragged and clear from actual inventory
        draggedItemStack.item = itemStack.item;
        draggedItemStack.quantity = itemStack.quantity;
        
        // Clear from the actual inventory array (not just visually)
        if (inventoryManager != null && !isSellBoxMode)
        {
            // For regular inventory slots, clear from the inventory array
            ItemStack clearedStack = inventoryManager.ClearSlotForDrag(this);
        }
        else
        {
            // For SellBox slots, just clear visually since SellBox handles its own array
            ItemStack tempClear = new ItemStack();
            SetItemStack(tempClear);
        }
        

        HideTooltip();
        CreateDragPreview();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.7f; // Less transparent so slots remain more visible
            canvasGroup.blocksRaycasts = false; // Don't block raycasts for drop detection
        }

        // Set static flag to indicate dragging is happening
        IsAnySlotDragging = true;
    }


    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragPreview == null) return;

        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            dragPreview.transform.position = eventData.position;
        }
        else if (canvas != null)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out localPoint
            );

            dragPreview.transform.localPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true; // Restore raycast blocking
        }

        // Check if drag was not successful and we still have the dragged item
        if (!dragWasSuccessful && !draggedItemStack.IsEmpty)
        {
            // Check if this was dropped outside UI (create ground item)
            bool droppedOutsideUI = !wasDroppedOnSlot;
            
            if (droppedOutsideUI)
            {
                CreateGroundItemFromDrag(eventData.position);
            }
            else
            {
                // Drag failed but was dropped on a slot - restore the item
                
                if (inventoryManager != null && !isSellBoxMode)
                {
                    // For regular inventory slots, restore to the inventory array
                    inventoryManager.RestoreSlotFromDrag(this, draggedItemStack);
                }
                else
                {
                    // For SellBox slots, just restore visually
                    SetItemStack(draggedItemStack);
                }
            }
        }

        // Clear the dragged item data
        draggedItemStack.Clear();

        if (dragPreview != null)
        {
            Destroy(dragPreview);
            dragPreview = null;
        }

        if (inventoryManager != null)
        {
            inventoryManager.EndDragOperation();
        }

        // Clear static flag - dragging is done
        IsAnySlotDragging = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot draggedSlot = eventData.pointerDrag?.GetComponent<InventorySlot>();
        if (draggedSlot != null && draggedSlot != this)
        {
            Debug.Log($"InventorySlot OnDrop: {draggedSlot.name} -> {this.name} (draggedSlot.isSellBoxMode: {draggedSlot.isSellBoxMode}, this.isSellBoxMode: {isSellBoxMode})");

            // Mark that the dragged slot was dropped on a valid slot
            draggedSlot.wasDroppedOnSlot = true;

            SellBox sellBox = FindFirstObjectByType<SellBox>();

            // Check if dragging FROM SellBox TO inventory
            if (draggedSlot.isSellBoxMode && !isSellBoxMode && sellBox != null && sellBox.IsOpen)
            {
                Debug.Log("Routing to SellBox.HandleSellBoxToInventoryDrop");
                sellBox.HandleSellBoxToInventoryDrop(draggedSlot, this);
                return;
            }

            // Check if this is a SellBox-to-SellBox move (internal rearrangement)
            if (draggedSlot.isSellBoxMode && isSellBoxMode && sellBox != null && sellBox.IsOpen)
            {
                Debug.Log("Routing to SellBox.HandleSellBoxInternalMove");
                sellBox.HandleSellBoxInternalMove(draggedSlot, this);
                return;
            }

            // Check if this is a drop TO SellBox slot (from inventory)
            if (isSellBoxMode && sellBox != null && sellBox.IsOpen)
            {
                Debug.Log("Routing to SellBox.HandleSlotDrop");
                sellBox.HandleSlotDrop(draggedSlot, this);
                return;
            }

            // Default to regular inventory handling
            if (inventoryManager != null)
            {
                Debug.Log("Routing to Inventory.HandleSlotDrop");
                inventoryManager.HandleSlotDrop(draggedSlot, this);
            }
            else
            {
                Debug.LogWarning("No inventory manager found for handling slot drop");
            }
        }
    }

    private bool IsOverUI(Vector2 screenPosition)
    {
        // Check if mouse position is over any UI element
        List<RaycastResult> results = new List<RaycastResult>();
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            // If we find any UI elements, we're over UI
            if (result.gameObject.GetComponent<Canvas>() != null ||
                result.gameObject.GetComponent<InventorySlot>() != null ||
                result.gameObject.name.Contains("Panel") ||
                result.gameObject.name.Contains("UI"))
            {
                return true;
            }
        }

        return false;
    }

    private void CreateGroundItem(Vector2 screenPosition)
    {
        if (IsEmpty) return;

        // Get the main camera to convert screen to world position
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found - cannot create ground item");
            return;
        }

        // Convert screen position to world position
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane));
        worldPosition.z = 0; // Ensure it's on the 2D plane

        // Find player position to drop near player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Drop item near player (within reasonable distance)
            Vector3 playerPos = player.transform.position;
            float maxDropDistance = 2f;

            Vector3 dropDirection = (worldPosition - playerPos).normalized;
            if (dropDirection.magnitude < 0.1f) // If too close, use random direction
            {
                dropDirection = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0).normalized;
            }

            worldPosition = playerPos + dropDirection * Mathf.Min(maxDropDistance, Vector3.Distance(worldPosition, playerPos));
        }

        // Load the GroundItem prefab
        GameObject groundItemPrefab = Resources.Load<GameObject>("Prefabs/GroundItem");
        if (groundItemPrefab == null)
        {
            // Try alternative path
            groundItemPrefab = Resources.Load<GameObject>("GroundItem");
        }

        if (groundItemPrefab == null)
        {
            // Try to find existing GroundItem in scene as template
            GroundItem existingGroundItem = FindFirstObjectByType<GroundItem>();
            if (existingGroundItem != null)
            {
                groundItemPrefab = existingGroundItem.gameObject;
                Debug.Log("Using existing GroundItem as template");
            }
        }

        if (groundItemPrefab == null)
        {
            Debug.LogError("GroundItem prefab not found! Please ensure GroundItem.prefab is in Resources/Prefabs/ folder or create a Resources folder");
            return;
        }

        // Instantiate the ground item
        GameObject groundItemObj = Instantiate(groundItemPrefab, worldPosition, Quaternion.identity);
        GroundItem groundItem = groundItemObj.GetComponent<GroundItem>();

        if (groundItem != null)
        {
            // Store item info for logging
            string itemName = itemStack.item.itemName;
            int quantity = itemStack.quantity;

            // Set the item data (with quantity)
            groundItem.SetItemStack(itemStack);
            Debug.Log($"Created GroundItem: {quantity}x {itemName} at position {worldPosition}");

            // Completely clear the slot
            ClearSlot();

            Debug.Log($"Cleared slot {slotIndex} after dropping {quantity}x {itemName}");
        }
        else
        {
            Debug.LogError("GroundItem component not found on instantiated prefab!");
            Destroy(groundItemObj);
        }
    }

    private void CreateGroundItemFromDrag(Vector2 screenPosition)
    {
        if (draggedItemStack.IsEmpty) return;

        // Get the main camera to convert screen to world position
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found - cannot create ground item");
            return;
        }

        // Convert screen position to world position
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane));
        worldPosition.z = 0; // Ensure it's on the 2D plane

        // Find player position to drop near player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Drop item near player (within reasonable distance)
            Vector3 playerPos = player.transform.position;
            float maxDropDistance = 2f;

            Vector3 dropDirection = (worldPosition - playerPos).normalized;
            if (dropDirection.magnitude < 0.1f) // If too close, use random direction
            {
                dropDirection = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0).normalized;
            }

            worldPosition = playerPos + dropDirection * Mathf.Min(maxDropDistance, Vector3.Distance(worldPosition, playerPos));
        }

        // Load the GroundItem prefab
        GameObject groundItemPrefab = Resources.Load<GameObject>("Prefabs/GroundItem");
        if (groundItemPrefab == null)
        {
            // Try alternative path
            groundItemPrefab = Resources.Load<GameObject>("GroundItem");
        }

        if (groundItemPrefab == null)
        {
            // Try to find existing GroundItem in scene as template
            GroundItem existingGroundItem = FindFirstObjectByType<GroundItem>();
            if (existingGroundItem != null)
            {
                groundItemPrefab = existingGroundItem.gameObject;
                Debug.Log("Using existing GroundItem as template");
            }
        }

        if (groundItemPrefab == null)
        {
            Debug.LogError("GroundItem prefab not found! Please ensure GroundItem.prefab is in Resources/Prefabs/ folder or create a Resources folder");
            return;
        }

        // Instantiate the ground item
        GameObject groundItemObj = Instantiate(groundItemPrefab, worldPosition, Quaternion.identity);
        GroundItem groundItem = groundItemObj.GetComponent<GroundItem>();

        if (groundItem != null)
        {
            // Store item info for logging
            string itemName = draggedItemStack.item.itemName;
            int quantity = draggedItemStack.quantity;

            // Set the item data from the dragged item (with quantity)
            groundItem.SetItemStack(draggedItemStack);
            Debug.Log($"Created GroundItem from drag: {quantity}x {itemName} at position {worldPosition}");

            Debug.Log($"Consumed dragged item {quantity}x {itemName} from slot {slotIndex}");
        }
        else
        {
            Debug.LogError("GroundItem component not found on instantiated prefab!");
            Destroy(groundItemObj);
        }
    }

    private void CreateDragPreview()
    {
        if (draggedItemStack.IsEmpty || canvas == null) return;

        dragPreview = new GameObject("DragPreview");
        dragPreview.transform.SetParent(canvas.transform, false);

        RectTransform previewRect = dragPreview.AddComponent<RectTransform>();
        previewRect.sizeDelta = new Vector2(64, 64);
        previewRect.anchorMin = Vector2.zero;
        previewRect.anchorMax = Vector2.zero;
        previewRect.pivot = new Vector2(0.5f, 0.5f);

        Canvas previewCanvas = dragPreview.AddComponent<Canvas>();
        previewCanvas.overrideSorting = true;
        previewCanvas.sortingOrder = 1000;

        CanvasGroup previewGroup = dragPreview.AddComponent<CanvasGroup>();
        previewGroup.alpha = 0.8f;
        previewGroup.blocksRaycasts = false;

        CreateDragPreviewVisuals();
        PositionDragPreview();
    }

    private void CreateDragPreviewVisuals()
    {
        if (dragPreview == null || draggedItemStack.IsEmpty) return;

        // Background
        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(dragPreview.transform, false);

        Image backgroundImg = backgroundObj.AddComponent<Image>();
        backgroundImg.color = new Color(0.1f, 0.1f, 0.15f, 0.3f);

        RectTransform bgRect = backgroundObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Item icon
        if (draggedItemStack.item?.icon != null)
        {
            GameObject iconObj = new GameObject("ItemIcon");
            iconObj.transform.SetParent(dragPreview.transform, false);

            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = draggedItemStack.item.icon;
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;

            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(6, 6);
            iconRect.offsetMax = new Vector2(-6, -6);
        }

        // Quantity text
        if (draggedItemStack.quantity > 1)
        {
            GameObject quantityObj = new GameObject("Quantity");
            quantityObj.transform.SetParent(dragPreview.transform, false);

            TextMeshProUGUI quantityTMP = quantityObj.AddComponent<TextMeshProUGUI>();
            quantityTMP.text = FormatQuantity(draggedItemStack.quantity);
            quantityTMP.fontSize = 12;
            quantityTMP.fontStyle = FontStyles.Bold;
            quantityTMP.color = Color.white;
            quantityTMP.alignment = TextAlignmentOptions.BottomRight;

            RectTransform quantityRect = quantityObj.GetComponent<RectTransform>();
            quantityRect.anchorMin = new Vector2(1, 0);
            quantityRect.anchorMax = new Vector2(1, 0);
            quantityRect.anchoredPosition = new Vector2(-5, 5);
            quantityRect.sizeDelta = new Vector2(30, 20);

            Outline outline = quantityObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);
        }

        // Rarity glow
        if (draggedItemStack.item != null && draggedItemStack.item.rarity > ItemRarity.Common)
        {
            GameObject glowObj = new GameObject("RarityGlow");
            glowObj.transform.SetParent(dragPreview.transform, false);
            glowObj.transform.SetSiblingIndex(0);

            Image glowImg = glowObj.AddComponent<Image>();
            Color glowColor = GetRarityGlowColor(draggedItemStack.item.rarity);
            glowColor.a = 0.5f;
            glowImg.color = glowColor;

            RectTransform glowRect = glowObj.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-3, -3);
            glowRect.offsetMax = new Vector2(3, 3);
        }
    }

    private void PositionDragPreview()
    {
        if (dragPreview == null) return;

        Vector2 mousePos = Mouse.current?.position.ReadValue() ?? Vector2.zero;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            dragPreview.transform.position = mousePos;
        }
        else
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                mousePos,
                canvas.worldCamera,
                out localPoint
            );
            dragPreview.transform.localPosition = localPoint;
        }
    }
}
