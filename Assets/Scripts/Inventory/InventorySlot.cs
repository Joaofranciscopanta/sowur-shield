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
    public static bool IsAnySlotDragging { get; set; }

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
    public Image sellableIndicator;
    public TextMeshProUGUI valueText;
    public Image rejectHighlight;
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

    // References
    private Inventory inventoryManager;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    // Visual components
    private Image rarityGlow;
    private Image selectionBorder;
    private TextMeshProUGUI slotNumberText;

    // Slot properties
    public int slotIndex = -1;

    // Component references (new architecture)
    private SlotVisualController visualController;
    private SlotDragHandler dragHandler;
    private SlotSellBoxAdapter sellBoxAdapter;

    // ============================================================================
    // PROPERTIES
    // ============================================================================

    public ItemStack ItemStack => itemStack;
    public bool IsEmpty => itemStack.IsEmpty;
    public bool IsSelected => isSelected;
    public bool isSellBoxMode => sellBoxAdapter != null && sellBoxAdapter.IsSellBoxMode;
    public bool wasDroppedOnSlot
    {
        get => dragHandler != null && dragHandler.wasDroppedOnSlot;
        set { if (dragHandler != null) dragHandler.wasDroppedOnSlot = value; }
    }

    // ============================================================================
    // UNITY LIFECYCLE
    // ============================================================================

    private void Awake()
    {
        InitializeReferences();
        SetupVisualComponents();
        InitializeComponentArchitecture();
    }

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        CheckHoverState();
    }

    private void OnDestroy()
    {
        if (visualController != null)
            visualController.CleanupAnimations();
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

    private void InitializeComponentArchitecture()
    {
        // Add visual controller component
        visualController = gameObject.AddComponent<SlotVisualController>();
        visualController.Initialize(
            itemIcon, quantityText, backgroundImage, selectionBorder, rarityGlow, slotNumberText,
            normalColor, selectedColor, emptySlotAlpha,
            hoverScale, clickScale, animationSpeed, scaleCurve,
            rarityGlowUncommon, rarityGlowRare, rarityGlowEpic, rarityGlowLegendary
        );

        // Add drag handler component
        dragHandler = gameObject.AddComponent<SlotDragHandler>();
        dragHandler.Initialize(canvas, canvasGroup, inventoryManager, scaleCurve);

        // Add SellBox adapter component
        sellBoxAdapter = gameObject.AddComponent<SlotSellBoxAdapter>();
        sellBoxAdapter.Initialize(sellableIndicator, valueText, rejectHighlight, selectionBorder);
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
        backgroundImage.raycastTarget = true;
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
            sellableIndicator.color = new Color(1f, 0.84f, 0f, 0.8f);
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
            valueText.color = new Color(1f, 0.84f, 0f);
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
            rejectHighlight.color = new Color(1f, 0f, 0f, 0.3f);
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

        if (wasEmpty != willBeEmpty && gameObject.activeInHierarchy && enabled && visualController != null)
        {
            visualController.StartItemChangeAnimation();
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

        if (visualController != null)
        {
            visualController.SetSelected(selected);
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

    public ItemStack GetDraggedItem()
    {
        return dragHandler != null ? dragHandler.DraggedItemStack : new ItemStack();
    }

    public void ConsumeDraggedItem()
    {
        if (dragHandler != null)
        {
            dragHandler.ConsumeDraggedItem();
        }
    }

    public void MarkDragSuccessful()
    {
        if (dragHandler != null)
        {
            dragHandler.MarkDragSuccessful();
        }
    }

    // ============================================================================
    // SELLBOX SPECIFIC METHODS
    // ============================================================================

    public void EnableSellBoxMode(float sellMultiplier = 0.8f)
    {
        if (sellBoxAdapter != null)
        {
            sellBoxAdapter.EnableSellBoxMode(sellMultiplier);
            UpdateSellBoxDisplay();
        }
    }

    public void DisableSellBoxMode()
    {
        if (sellBoxAdapter != null)
        {
            sellBoxAdapter.DisableSellBoxMode();
        }
    }

    public void UpdateSellBoxDisplay()
    {
        if (sellBoxAdapter != null)
        {
            sellBoxAdapter.UpdateSellBoxDisplay(itemStack, isSelected);
        }
    }

    public void ShowAcceptFeedback()
    {
        if (sellBoxAdapter != null)
        {
            sellBoxAdapter.ShowAcceptFeedback(isSelected);
        }
    }

    // ============================================================================
    // HOVER STATE MANAGEMENT
    // ============================================================================

    private void CheckHoverState()
    {
        if (canvas == null || (dragHandler != null && dragHandler.IsDragging)) return;

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
        if (isHovered || (dragHandler != null && dragHandler.IsDragging)) return;

        ShowTooltip();
    }

    private void ForceExitHover()
    {
        if (!isHovered) return;

        isHovered = false;
        if (visualController != null)
        {
            visualController.SetTargetScale(Vector3.one);
        }
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
        if (visualController != null)
        {
            visualController.UpdateVisuals(itemStack, isSelected);
        }
        UpdateSellBoxDisplay();
    }

    private void ResetToNormalState()
    {
        isHovered = false;

        if (visualController != null)
        {
            visualController.ResetToNormalState();
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        HideTooltip();
    }

    // ============================================================================
    // INPUT EVENT HANDLERS
    // ============================================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (dragHandler != null && dragHandler.IsDragging) return;
        ForceEnterHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (dragHandler != null && dragHandler.IsDragging) return;
        ForceExitHover();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (visualController != null)
            {
                visualController.StartClickAnimation(isHovered);
            }

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
        if (visualController != null)
        {
            if (isHovered)
            {
                visualController.SetTargetScale(Vector3.one * hoverScale);
            }
            else
            {
                visualController.SetTargetScale(Vector3.one);
            }
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

        if (dragHandler != null)
        {
            dragHandler.BeginDrag(itemStack, isSellBoxMode, this);
            HideTooltip();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragHandler != null)
        {
            dragHandler.UpdateDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragHandler != null)
        {
            dragHandler.EndDrag(eventData, isSellBoxMode, this);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot draggedSlot = eventData.pointerDrag?.GetComponent<InventorySlot>();
        if (draggedSlot != null && draggedSlot != this && draggedSlot.dragHandler != null)
        {
            draggedSlot.dragHandler.wasDroppedOnSlot = true;

            SellBox sellBox = FindFirstObjectByType<SellBox>();

            // Check if dragging FROM SellBox TO inventory
            if (draggedSlot.isSellBoxMode && !isSellBoxMode && sellBox != null && sellBox.IsOpen)
            {
                sellBox.HandleSellBoxToInventoryDrop(draggedSlot, this);
                return;
            }

            // Check if this is a SellBox-to-SellBox move
            if (draggedSlot.isSellBoxMode && isSellBoxMode && sellBox != null && sellBox.IsOpen)
            {
                sellBox.HandleSellBoxInternalMove(draggedSlot, this);
                return;
            }

            // Check if this is a drop TO SellBox slot
            if (isSellBoxMode && sellBox != null && sellBox.IsOpen)
            {
                sellBox.HandleSlotDrop(draggedSlot, this);
                return;
            }

            // Default to regular inventory handling
            if (inventoryManager != null)
            {
                inventoryManager.HandleSlotDrop(draggedSlot, this);
            }
        }
    }
}