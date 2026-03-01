using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using SowurShield.Core;

namespace SowurShield.Inventory
{
    /// <summary>
    /// Handles all drag and drop operations for InventorySlots.
    /// This includes drag preview creation, ground item spawning, and drag state management.
    /// </summary>
    public class SlotDragHandler : MonoBehaviour
    {
        // References (set by InventorySlot)
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private SowurShield.Inventory.Inventory inventoryManager;

        // Drag state
        private bool isDragging = false;
        private GameObject dragPreview;
        private Vector3 originalPosition;
        private ItemStack draggedItemStack = new ItemStack();
        private bool dragWasSuccessful = false;

        // Public flags
        public bool wasDroppedOnSlot = false;

        // Visual settings for drag preview
        private AnimationCurve scaleCurve;

        /// <summary>
        /// Get the currently dragged item stack
        /// </summary>
        public ItemStack DraggedItemStack => draggedItemStack;

        /// <summary>
        /// Check if currently dragging
        /// </summary>
        public bool IsDragging => isDragging;

        /// <summary>
        /// Initialize the drag handler with references from InventorySlot
        /// </summary>
        public void Initialize(Canvas canvas, CanvasGroup canvasGroup, SowurShield.Inventory.Inventory inventoryManager, AnimationCurve scaleCurve)
        {
            this.canvas = canvas;
            this.canvasGroup = canvasGroup;
            this.inventoryManager = inventoryManager;
            this.scaleCurve = scaleCurve;
        }

        /// <summary>
        /// Begin drag operation
        /// </summary>
        public void BeginDrag(ItemStack currentItemStack, bool isSellBoxMode, InventorySlot slot)
        {
            isDragging = true;
            wasDroppedOnSlot = false;
            dragWasSuccessful = false;
            originalPosition = transform.position;

            // Store the item being dragged
            draggedItemStack.item = currentItemStack.item;
            draggedItemStack.quantity = currentItemStack.quantity;

            // Clear from the actual inventory array (not just visually)
            if (inventoryManager != null && !isSellBoxMode)
            {
                inventoryManager.ClearSlotForDrag(slot);
            }

            CreateDragPreview();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.7f;
                canvasGroup.blocksRaycasts = false;
            }

            // Set static flag to indicate dragging is happening
            InventorySlot.IsAnySlotDragging = true;
        }

        /// <summary>
        /// Update drag preview position
        /// </summary>
        public void UpdateDrag(PointerEventData eventData)
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

        /// <summary>
        /// End drag operation
        /// </summary>
        public void EndDrag(PointerEventData eventData, bool isSellBoxMode, InventorySlot slot)
        {
            isDragging = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
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
                        inventoryManager.RestoreSlotFromDrag(slot, draggedItemStack);
                    }
                    else
                    {
                        // For SellBox slots, restore via slot
                        slot.SetItemStack(draggedItemStack);
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
            InventorySlot.IsAnySlotDragging = false;
        }

        /// <summary>
        /// Mark drag as successful (called when drop succeeds)
        /// </summary>
        public void MarkDragSuccessful()
        {
            dragWasSuccessful = true;
        }

        /// <summary>
        /// Consume the dragged item permanently
        /// </summary>
        public void ConsumeDraggedItem()
        {
            if (!draggedItemStack.IsEmpty)
            {
                dragWasSuccessful = true;
            }
        }

        // ============================================================================
        // DRAG PREVIEW CREATION
        // ============================================================================

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

        // ============================================================================
        // GROUND ITEM CREATION
        // ============================================================================

        private void CreateGroundItemFromDrag(Vector2 screenPosition)
        {
            if (draggedItemStack.IsEmpty) return;

            // Get the main camera to convert screen to world position
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

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
                groundItemPrefab = Resources.Load<GameObject>("GroundItem");
            }

            if (groundItemPrefab == null)
            {
                GroundItem existingGroundItem = FindFirstObjectByType<GroundItem>();
                if (existingGroundItem != null)
                {
                    groundItemPrefab = existingGroundItem.gameObject;
                }
            }

            if (groundItemPrefab == null) return;

            // Instantiate the ground item
            GameObject groundItemObj = Instantiate(groundItemPrefab, worldPosition, Quaternion.identity);
            GroundItem groundItem = groundItemObj.GetComponent<GroundItem>();

            if (groundItem != null)
            {
                groundItem.SetItemStack(draggedItemStack);
            }
            else
            {
                Destroy(groundItemObj);
            }
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
            Color rarityGlowUncommon = Color.green;
            Color rarityGlowRare = Color.blue;
            Color rarityGlowEpic = new Color(0.6f, 0f, 0f);
            Color rarityGlowLegendary = new Color(1f, 0.5f, 0f);

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
            if (dragPreview != null)
            {
                Destroy(dragPreview);
                dragPreview = null;
            }
        }
    }
} // namespace SowurShield.Inventory
