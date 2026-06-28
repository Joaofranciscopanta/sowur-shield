using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using SowurShield.Inventory;
using UnityEngine.Localization;

namespace SowurShield.Core
{

/*
 * SELLBOX SETUP INSTRUCTIONS:
 *
 * 1. Create a GameObject with SellBox script
 * 2. Add a Collider2D component with IsTrigger = true
 * 3. Set Layer to an interactable layer (configure in PlayerMove)
 * 4. Assign UI references:
 *    - sellBoxSlotParent: Parent Transform for slot UI
 *    - sellBoxSlotPrefab: InventorySlot prefab
 *    - totalValueText: Text showing total value
 *    - sellBoxTitleText: Title text for the UI
 * 5. Assign Visual references:
 *    - boxSpriteRenderer: SpriteRenderer for box visual
 *    - defaultBoxSprite: Default sprite for empty box
 * 6. Configure Audio/Effects:
 *    - sellSound: Audio when selling (during sleep)
 *    - itemPlaceSound: Audio when placing items
 *    - sellParticleEffect: Particle effect when selling
 *
 * Items placed in the SellBox will be automatically sold when the player sleeps!
 * The system will automatically handle drag & drop between inventory and sellbox!
 */

[System.Serializable]
public class ItemBoxSprite
{
    public Item item;
    public Sprite boxSprite;
    public string itemTag = ""; // Optional: match by item tag instead of specific item
    public ItemType itemType; // Optional: match by item type

    public bool MatchesItem(Item checkItem)
    {
        if (checkItem == null) return false;

        // When a direct item reference is specified it is the ONLY criterion —
        // tag and type are ignored so a different item with a matching tag won't
        // accidentally satisfy this mapping.
        if (item != null)
            return item == checkItem;

        // Tag match has medium priority (only when no direct item is set)
        if (!string.IsNullOrEmpty(itemTag))
            return checkItem.itemTags.Contains(itemTag);

        // Type match has lowest priority
        return checkItem.itemType == itemType;
    }
}

public class SellBox : MonoBehaviour, IInteractable, IUIWindow
{
    [Header("Sell Box Settings")]
    public int boxInventorySize = 12;
    [SerializeField] private GameBalance balance;
    public float maxInteractionDistance = 3f; // Auto-close if player moves farther than this

    private float sellMultiplier => balance != null ? balance.sellMultiplier : 0.8f;

    [Header("UI References")]
    public GameObject sellBoxMainPanel; // Main SellBox UI panel
    public Transform sellBoxSlotParent;
    public GameObject sellBoxSlotPrefab;
    public UnityEngine.UI.Text totalValueText;
    public UnityEngine.UI.Text sellBoxTitleText;

    [Header("Localized Strings")]
    [SerializeField] private LocalizedString titleText; // table "Farming", key "farming.sellbox.title"
    [SerializeField] private LocalizedString totalValueLocalizedText; // table "Farming", key "farming.sellbox.total_value"

    [Header("Visual Feedback")]
    public AudioClip sellSound;
    public AudioClip itemPlaceSound;
    public ParticleSystem sellParticleEffect;

    [Header("Dynamic Box Sprites")]
    public SpriteRenderer boxSpriteRenderer;
    public Sprite defaultBoxSprite;
    public List<ItemBoxSprite> itemBoxSprites = new List<ItemBoxSprite>();

    private InventoryContainer container;
    private List<InventorySlot> sellBoxSlotUIs = new List<InventorySlot>();
    private bool isSellBoxOpen = false;
    private PlayerStats playerStats;
    private SowurShield.Inventory.Inventory playerInventory;
    private Transform playerTransform;
    private PlayerMove playerMove;

    public System.Action<int> OnItemsSold;
    public System.Action OnSellBoxToggled;

    /// <summary>Static, fired alongside OnItemsSold whenever ANY SellBox sells items, so global listeners (e.g. AchievementManager) don't need to find/hook every SellBox instance.</summary>
    public static System.Action<int> OnAnyItemsSold;

    public bool IsOpen => isSellBoxOpen;
    public int TotalValue => CalculateTotalValue();

    // IUIWindow implementation
    public string WindowName => "SellBox";
    public int WindowPriority => 20;
    public bool IsWindowOpen => isSellBoxOpen;

    // ESC is reserved for the pause menu; close this window with E (Interact) instead.
    public bool CanCloseWithEsc => false;

    // Static reference for single UI window management
    private static SellBox currentlyOpenSellBox;

    // Flag to track if UI needs updating after inactive period
    private bool needsUIUpdate = false;

    // Auto-close system
    private bool isTrackingInputForClose = false;
    private float inputTrackingStartTime = 0f;
    private const float autoCloseDelay = 0.5f;

    private void Awake()
    {
        InitializeSellBox();
    }

    private void Start()
    {
        if (balance == null)
            balance = Resources.Load<GameBalance>("GameBalance");
        if (balance != null)
            maxInteractionDistance = balance.sellBoxInteractionRange;

        SetupUI();
        playerStats = FindFirstObjectByType<PlayerStats>();
        playerInventory = FindFirstObjectByType<SowurShield.Inventory.Inventory>();

        // Find player transform and PlayerMove component for distance checking and movement control
        var player = GameObject.FindWithTag("Player");
        if (player == null) player = FindFirstObjectByType<PlayerMove>()?.gameObject;
        if (player != null)
        {
            playerTransform = player.transform;
            playerMove = player.GetComponent<PlayerMove>();
        }

        UpdateTotalValueDisplay();
        UpdateBoxSprite();
        CloseSellBox();

        SowurShield.Core.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;

        // Ensure main panel starts inactive
        if (sellBoxMainPanel != null)
            sellBoxMainPanel.SetActive(false);

        // Register with InteractionManager
        RegisterWithInteractionManager();

        // Register with UIManager
        RegisterWithUIManager();

        // Debug information
        ValidateSetup();
    }

    private void OnDestroy()
    {
        UnregisterFromInteractionManager();
        UnregisterFromUIManager();
        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(Locale locale)
    {
        if (sellBoxTitleText != null)
            sellBoxTitleText.text = titleText.SafeGetLocalizedString();
        UpdateTotalValueDisplay();
    }

    private void RegisterWithUIManager()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterWindow(this);
        }
    }

    private void UnregisterFromUIManager()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterWindow(this);
        }
    }

    public void OnWindowBlocked(string blockedBy)
    {
        // Could show a message to player or play a sound
    }

    private void RegisterWithInteractionManager()
    {
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.RegisterInteractable(this);
        }
    }

    private void UnregisterFromInteractionManager()
    {
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.UnregisterInteractable(this);
        }
    }

    private void Update()
    {
        if (isSellBoxOpen)
        {
            // Auto-close if player moves too far away
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance > maxInteractionDistance)
                {
                    CloseSellBox();
                    return;
                }
            }

            // Check for movement or interaction attempts to auto-close
            CheckForInputAutoClose();
        }

        // Check if UI needs updating after being inactive (e.g., after sleep selling)
        if (needsUIUpdate && AreUIElementsActive())
        {
            ForceUpdateAllUI();
            needsUIUpdate = false;
        }
    }

    /// <summary>
    /// Check for movement or interaction input attempts and auto-close after delay
    /// Uses Unity's new Input System
    /// </summary>
    private void CheckForInputAutoClose()
    {
        bool hasMovementInput = false;
        bool hasInteractionInput = false;

        // Get current keyboard state
        var keyboard = Keyboard.current;
        if (keyboard == null) return; // No keyboard available

        // Check for movement input (WASD or Arrow keys)
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ||
            keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ||
            keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ||
            keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            hasMovementInput = true;
        }

        // Check for interaction input (E key)
        if (keyboard.eKey.isPressed)
        {
            hasInteractionInput = true;
        }

        // Start tracking if we detect input
        if ((hasMovementInput || hasInteractionInput) && !isTrackingInputForClose)
        {
            isTrackingInputForClose = true;
            inputTrackingStartTime = Time.time;
        }

        // Continue tracking and check for auto-close
        if (isTrackingInputForClose)
        {
            // If input stops, reset tracking
            if (!hasMovementInput && !hasInteractionInput)
            {
                isTrackingInputForClose = false;
                return;
            }

            // If we've been tracking for the delay period, auto-close
            if (Time.time - inputTrackingStartTime >= autoCloseDelay)
            {
                CloseSellBox();
            }
        }
    }

    /// <summary>
    /// Reset input tracking state
    /// </summary>
    private void ResetInputTracking()
    {
        isTrackingInputForClose = false;
        inputTrackingStartTime = 0f;
    }

    private void ValidateSetup()
    {
    }

    private void CloseOtherUIWindows()
    {
        // Close any other currently open SellBox
        if (currentlyOpenSellBox != null && currentlyOpenSellBox != this)
        {
            currentlyOpenSellBox.CloseSellBox();
        }

        // Use UIManager if available, otherwise fallback to manual closing
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseAllPanels();
        }
        else
        {
            // Fallback: Close inventory if it's open
            if (playerInventory != null)
            {
                var inventory = playerInventory.GetComponent<SowurShield.Inventory.Inventory>();
                if (inventory != null)
                {
                    var closeMethod = inventory.GetType().GetMethod("CloseInventory") ??
                                    inventory.GetType().GetMethod("Close") ??
                                    inventory.GetType().GetMethod("SetActive");
                    closeMethod?.Invoke(inventory, new object[] { false });
                }
            }
        }

        // Set this as the currently open SellBox
        currentlyOpenSellBox = this;
    }

    private void InitializeSellBox()
    {
        // Initialize ItemDatabase
        var _ = ItemDatabase.Instance;

        // Create container
        container = new InventoryContainer(boxInventorySize, "SellBox");

        // Subscribe to container events for automatic UI updates
        container.OnSlotChanged += (index, stack) =>
        {
            UpdateSlot(index);
            UpdateTotalValueDisplay();
            UpdateBoxSprite();
        };
    }

    private void SetupUI()
    {
        sellBoxSlotUIs.Clear();

        if (sellBoxSlotParent != null && sellBoxSlotPrefab != null)
        {
            for (int i = 0; i < boxInventorySize; i++)
            {
                CreateSellBoxSlotUI(i);
            }
        }

        if (sellBoxTitleText != null)
            sellBoxTitleText.text = titleText.SafeGetLocalizedString();

        UpdateAllSlots();
    }

    private void CreateSellBoxSlotUI(int index)
    {
        GameObject slotObj = Instantiate(sellBoxSlotPrefab, sellBoxSlotParent);
        slotObj.name = $"SellBoxSlot_{index}";

        InventorySlot slotUI = slotObj.GetComponent<InventorySlot>();
        if (slotUI != null)
        {
            sellBoxSlotUIs.Add(slotUI);
            slotUI.SetSlotIndex(index);
            slotUI.SetItemStack(container.GetSlot(index));
            slotUI.EnableSellBoxMode(sellMultiplier);
        }
    }

    public void Interact()
    {
        // Only interact if not currently open or if this is the closest interactable
        ToggleSellBox();
    }

    public void ToggleSellBox()
    {
        if (isSellBoxOpen)
        {
            CloseSellBox();
        }
        else
        {
            OpenSellBox();
        }
    }

    public void OpenSellBox()
    {
        // Use UIManager to try opening this window
        if (UIManager.Instance != null && !UIManager.Instance.TryOpenWindow(this))
        {
            return; // Window was blocked by another window
        }

        // If no UIManager, fall back to old behavior
        if (UIManager.Instance == null)
        {
            CloseOtherUIWindows();
        }

        isSellBoxOpen = true;

        // Reset input tracking for auto-close
        ResetInputTracking();

        // Disable player movement when SellBox is open
        if (playerMove != null)
        {
            playerMove.DisableMovement();
        }

        // Ensure cursor is visible for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Use UIManager if available
        if (UIManager.Instance != null && sellBoxMainPanel != null)
        {
            UIManager.Instance.OpenPanel(sellBoxMainPanel);
        }
        else if (sellBoxMainPanel != null)
        {
            sellBoxMainPanel.SetActive(true);
        }

        OnSellBoxToggled?.Invoke();
        UpdateTotalValueDisplay();
    }

    public void CloseSellBox()
    {
        // Notify UIManager if available
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TryCloseWindow(this);
        }
        else
        {
            // Direct close if no UIManager
            CloseWindow();
        }
    }

    // IUIWindow implementation methods
    public void OpenWindow()
    {
        // Direct opening logic (called by UIManager)
        isSellBoxOpen = true;

        // Reset input tracking for auto-close
        ResetInputTracking();

        // Disable player movement when SellBox is open
        if (playerMove != null)
        {
            playerMove.DisableMovement();
        }

        // Show UI panel
        if (sellBoxMainPanel != null)
        {
            sellBoxMainPanel.SetActive(true);
        }

        // Set as currently open SellBox
        currentlyOpenSellBox = this;

        // Play opening sound
        if (itemPlaceSound != null && playerTransform != null)
        {
            AudioSource.PlayClipAtPoint(itemPlaceSound, playerTransform.position, 0.5f);
        }

        // Update display
        UpdateTotalValueDisplay();
        ForceUpdateAllUI();

        // Notify systems
        OnSellBoxToggled?.Invoke();
    }

    public void CloseWindow()
    {
        // Direct closing logic (called by UIManager)
        isSellBoxOpen = false;

        // Reset input tracking
        ResetInputTracking();

        // Re-enable player movement when SellBox is closed
        if (playerMove != null)
        {
            playerMove.EnableMovement();
        }

        // Use UIManager if available
        if (UIManager.Instance != null && sellBoxMainPanel != null)
        {
            UIManager.Instance.ClosePanel(sellBoxMainPanel);
        }
        else if (sellBoxMainPanel != null)
        {
            sellBoxMainPanel.SetActive(false);
        }

        // Clear static reference if this was the open one
        if (currentlyOpenSellBox == this)
            currentlyOpenSellBox = null;
    }

    public bool AddItem(Item item, int quantity = 1)
    {
        bool success = AddItemSilent(item, quantity);

        if (!success && (item == null || !item.canBeSold))
        {
            // Show rejection feedback on all slots
            foreach (var slotUI in sellBoxSlotUIs)
            {
                if (slotUI != null)
                    StartCoroutine(ShowRejectFeedbackOnSlot(slotUI));
            }
            return false;
        }

        if (success)
        {
            PlaySound(itemPlaceSound);
            UpdateTotalValueDisplay();
            UpdateBoxSprite();

            // Show accept feedback on slots that received items
            for (int i = 0; i < boxInventorySize; i++)
            {
                ItemStack stack = container.GetSlot(i);
                if (!stack.IsEmpty && stack.item == item)
                {
                    if (i < sellBoxSlotUIs.Count && sellBoxSlotUIs[i] != null)
                        StartCoroutine(ShowAcceptFeedbackOnSlot(sellBoxSlotUIs[i]));
                }
            }
        }

        return success;
    }

    /// <summary>
    /// Adds items without triggering sound, feedback, or UI updates (for internal operations)
    /// </summary>
    private bool AddItemSilent(Item item, int quantity = 1)
    {
        if (item == null || quantity <= 0 || !item.canBeSold) return false;

        // Use container's AddItem method which handles stacking automatically
        return container.AddItem(item, quantity);
    }

    public bool RemoveItem(Item item, int quantity = 1)
    {
        bool success = container.RemoveItem(item, quantity);

        UpdateTotalValueDisplay();
        UpdateBoxSprite();

        return success;
    }

    public bool RemoveFromSlot(int slotIndex, int quantity = 1)
    {
        if (slotIndex < 0 || slotIndex >= boxInventorySize) return false;

        ItemStack stack = container.GetSlot(slotIndex);
        if (stack.IsEmpty || quantity <= 0) return false;

        int toRemove = Mathf.Min(quantity, stack.quantity);
        ItemStack updatedStack = stack.Clone();
        updatedStack.quantity -= toRemove;

        if (updatedStack.quantity <= 0)
        {
            updatedStack.Clear();
        }

        container.SetSlot(slotIndex, updatedStack);
        UpdateTotalValueDisplay();
        UpdateBoxSprite();

        return toRemove > 0;
    }

    public ItemStack GetSlotItemStack(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= boxInventorySize) return null;
        return container.GetSlot(slotIndex);
    }

    public void HandleSlotDrop(InventorySlot fromSlot, InventorySlot toSlot)
    {
        int toIndex = sellBoxSlotUIs.IndexOf(toSlot);
        if (toIndex < 0 || toIndex >= boxInventorySize)
        {
            return;
        }

        ItemStack fromItemStack = fromSlot.GetDraggedItem();
        if (fromItemStack == null || fromItemStack.IsEmpty)
        {
            return;
        }

        // Check if item can be sold
        if (!fromItemStack.item.canBeSold)
        {
            StartCoroutine(ShowRejectFeedbackOnSlot(toSlot));
            return;
        }

        // Try to add the item to the sell box
        int quantityToMove = fromItemStack.quantity;
        bool canAddAll = CanAdd(fromItemStack.item, quantityToMove);

        if (canAddAll)
        {
            // Add all items to sell box (without showing feedback in AddItem)
            bool success = AddItemSilent(fromItemStack.item, quantityToMove);
            if (success)
            {
                // Consume the dragged item (it's already stored in draggedItemStack)
                fromSlot.ConsumeDraggedItem();
                PlaySound(itemPlaceSound);

                // Show accept feedback only once
                StartCoroutine(ShowAcceptFeedbackOnSlot(toSlot));
            }
        }
        else
        {
            // Try to add as much as possible
            int spaceAvailable = GetAvailableSpace(fromItemStack.item);
            if (spaceAvailable > 0)
            {
                bool success = AddItemSilent(fromItemStack.item, spaceAvailable);
                if (success)
                {
                    fromSlot.ConsumeDraggedItem();
                    PlaySound(itemPlaceSound);
                    StartCoroutine(ShowAcceptFeedbackOnSlot(toSlot));
                }
            }
            else
            {
                StartCoroutine(ShowRejectFeedbackOnSlot(toSlot));
            }
        }

        // Single update at the end
        UpdateTotalValueDisplay();
    }

    /// <summary>
    /// Handles moving items within the SellBox (slot to slot rearrangement)
    /// </summary>
    public void HandleSellBoxInternalMove(InventorySlot fromSlot, InventorySlot toSlot)
    {
        int fromIndex = sellBoxSlotUIs.IndexOf(fromSlot);
        int toIndex = sellBoxSlotUIs.IndexOf(toSlot);

        if (fromIndex < 0 || fromIndex >= boxInventorySize || toIndex < 0 || toIndex >= boxInventorySize)
        {
            return;
        }

        // Get stacks
        ItemStack fromStack = container.GetSlot(fromIndex);
        ItemStack toStack = container.GetSlot(toIndex);

        // Swap the items
        container.SetSlot(fromIndex, toStack);
        container.SetSlot(toIndex, fromStack);

        // Consume the dragged item since the move succeeded
        fromSlot.ConsumeDraggedItem();

        // Update both slot UIs
        UpdateSlot(fromIndex);
        UpdateSlot(toIndex);

        // Single total value update (no duplication)
        UpdateTotalValueDisplay();

        // No green feedback for internal moves - just a subtle sound
        PlaySound(itemPlaceSound);
    }

    public void HandleSellBoxToInventoryDrop(InventorySlot fromSlot, InventorySlot toSlot)
    {
        // Find the from slot index in sellBox
        int fromIndex = sellBoxSlotUIs.IndexOf(fromSlot);
        if (fromIndex < 0 || fromIndex >= boxInventorySize)
        {
            return;
        }

        // Get the dragged item from the fromSlot (SellBox slot)
        ItemStack sellBoxItemStack = fromSlot.GetDraggedItem();
        if (sellBoxItemStack == null || sellBoxItemStack.IsEmpty)
        {
            return;
        }

        // Store item info before any modifications
        Item itemToMove = sellBoxItemStack.item;
        int quantityToMove = sellBoxItemStack.quantity;

        if (itemToMove == null)
        {
            return;
        }

        if (playerInventory == null)
        {
            return;
        }

        // Check if inventory can accept the item
        bool canAdd = playerInventory.CanAdd(itemToMove, quantityToMove);

        if (canAdd)
        {
            // Add to inventory
            bool success = playerInventory.Add(itemToMove, quantityToMove);
            if (success)
            {
                // Remove from sellbox inventory
                RemoveFromSlot(fromIndex, quantityToMove);

                // Consume the dragged item
                fromSlot.ConsumeDraggedItem();

                // Mark the slot as processed to prevent double-processing
                if (fromSlot != null)
                {
                    fromSlot.wasDroppedOnSlot = true; // Ensure this is set
                }
            }
        }
    }

    /// <summary>
    /// Automatically sells all items in the box (called during sleep)
    /// </summary>
    public int SellAllItemsAutomatically()
    {
        if (playerStats == null)
        {
            return 0;
        }

        int totalEarnings = 0;
        List<ItemStack> itemsSold = new List<ItemStack>();

        for (int i = 0; i < boxInventorySize; i++)
        {
            ItemStack stack = container.GetSlot(i);
            if (!stack.IsEmpty)
            {
                Item item = stack.item;
                int quantity = stack.quantity;

                if (item.canBeSold)
                {
                    int itemValue = Mathf.RoundToInt(item.baseValue * sellMultiplier * quantity);
                    totalEarnings += itemValue;

                    itemsSold.Add(stack.Clone());
                    container.SetSlot(i, new ItemStack());
                    UpdateSlot(i);
                }
            }
        }

        if (totalEarnings > 0)
        {
            playerStats.AddMoney(totalEarnings);
            OnItemsSold?.Invoke(totalEarnings);
            OnAnyItemsSold?.Invoke(totalEarnings);
        }

        UpdateTotalValueDisplay();
        UpdateBoxSprite();

        // Mark that UI needs to be updated when it becomes active again
        needsUIUpdate = true;

        return totalEarnings;
    }

    /// <summary>
    /// Check if there are any items to sell
    /// </summary>
    public bool HasItemsToSell()
    {
        for (int i = 0; i < boxInventorySize; i++)
        {
            ItemStack stack = container.GetSlot(i);
            if (!stack.IsEmpty && stack.item.canBeSold)
            {
                return true;
            }
        }
        return false;
    }

    private int CalculateTotalValue()
    {
        int total = 0;
        for (int i = 0; i < boxInventorySize; i++)
        {
            ItemStack stack = container.GetSlot(i);
            if (!stack.IsEmpty && stack.item.canBeSold)
            {
                total += Mathf.RoundToInt(stack.item.baseValue * sellMultiplier * stack.quantity);
            }
        }
        return total;
    }

    private void UpdateTotalValueDisplay()
    {
        if (totalValueText != null && totalValueText.gameObject.activeInHierarchy)
        {
            int totalValue = CalculateTotalValue();
            totalValueLocalizedText.Arguments = new object[] { totalValue };
            totalValueText.text = totalValueLocalizedText.SafeGetLocalizedString();
        }
    }

    private void UpdateSlot(int index)
    {
        if (index >= 0 && index < sellBoxSlotUIs.Count && sellBoxSlotUIs[index] != null)
        {
            // Only update UI if the slot GameObject is active
            if (sellBoxSlotUIs[index].gameObject.activeInHierarchy)
            {
                sellBoxSlotUIs[index].SetItemStack(container.GetSlot(index));
            }
        }
    }

    private void UpdateAllSlots()
    {
        for (int i = 0; i < Mathf.Min(container.MaxSlots, sellBoxSlotUIs.Count); i++)
        {
            UpdateSlot(i);
        }
    }

    /// <summary>
    /// Force updates all UI elements regardless of active state checks
    /// Used after automatic selling when UI was inactive
    /// </summary>
    private void ForceUpdateAllUI()
    {
        // Force update all slots without active checks
        for (int i = 0; i < Mathf.Min(container.MaxSlots, sellBoxSlotUIs.Count); i++)
        {
            if (i >= 0 && i < sellBoxSlotUIs.Count && sellBoxSlotUIs[i] != null)
            {
                sellBoxSlotUIs[i].SetItemStack(container.GetSlot(i));
            }
        }

        // Force update total value display
        if (totalValueText != null)
        {
            int totalValue = CalculateTotalValue();
            totalValueLocalizedText.Arguments = new object[] { totalValue };
            totalValueText.text = totalValueLocalizedText.SafeGetLocalizedString();
        }

        // Update box sprite
        UpdateBoxSprite();
    }

    /// <summary>
    /// Checks if the main UI elements are currently active
    /// </summary>
    private bool AreUIElementsActive()
    {
        // Check if the main panel or at least some slot UIs are active
        if (sellBoxMainPanel != null && sellBoxMainPanel.activeInHierarchy)
            return true;

        // Check if any slot UI is active
        foreach (var slot in sellBoxSlotUIs)
        {
            if (slot != null && slot.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }

    public bool CanAdd(Item item, int quantity = 1)
    {
        if (item == null || quantity <= 0 || !item.canBeSold) return false;
        return container.CanAdd(item, quantity);
    }

    public int GetAvailableSpace(Item item)
    {
        if (item == null || !item.canBeSold) return 0;

        int availableSpace = 0;

        // Check existing stacks if stackable
        if (item.isStackable)
        {
            for (int i = 0; i < boxInventorySize; i++)
            {
                ItemStack stack = container.GetSlot(i);
                if (stack.CanStack(item))
                {
                    availableSpace += stack.AvailableSpace;
                }
            }
        }

        // Check empty slots
        for (int i = 0; i < boxInventorySize; i++)
        {
            ItemStack stack = container.GetSlot(i);
            if (stack.IsEmpty)
            {
                availableSpace += item.maxStackSize;
            }
        }

        return availableSpace;
    }

    public List<ItemStack> GetAllItems()
    {
        return container.GetAllItems();
    }

    public void ClearSellBox()
    {
        container.ClearAll();
        UpdateTotalValueDisplay();
        UpdateBoxSprite();
    }

    /// <summary>
    /// Validates and fixes any inconsistencies in SellBox state
    /// Call this if you suspect value duplication issues
    /// </summary>
    public void ValidateAndFixState()
    {
        // Ensure all slots are properly configured
        for (int i = 0; i < sellBoxSlotUIs.Count && i < boxInventorySize; i++)
        {
            if (sellBoxSlotUIs[i] != null)
            {
                // Force correct SellBox mode
                sellBoxSlotUIs[i].EnableSellBoxMode(sellMultiplier);

                // Ensure slot data matches internal inventory
                sellBoxSlotUIs[i].SetItemStack(container.GetSlot(i));

                // Force UI update
                sellBoxSlotUIs[i].UpdateSellBoxDisplay();
            }
        }

        // Force total value recalculation
        UpdateTotalValueDisplay();
    }

    private void UpdateBoxSprite()
    {
        if (boxSpriteRenderer == null) return;

        // Find the first non-empty item to determine the sprite
        Item primaryItem = GetPrimaryItem();

        if (primaryItem == null)
        {
            // Box is empty, use default sprite
            boxSpriteRenderer.sprite = defaultBoxSprite;
            return;
        }

        // Look for matching sprite configuration
        ItemBoxSprite matchingBoxSprite = itemBoxSprites.FirstOrDefault(ibs => ibs.MatchesItem(primaryItem));

        if (matchingBoxSprite != null && matchingBoxSprite.boxSprite != null)
        {
            boxSpriteRenderer.sprite = matchingBoxSprite.boxSprite;
        }
        else
        {
            // No specific sprite found, use default
            boxSpriteRenderer.sprite = defaultBoxSprite;
        }
    }

    private Item GetPrimaryItem()
    {
        // Return the first non-empty item in the inventory
        for (int i = 0; i < boxInventorySize; i++)
        {
            ItemStack stack = container.GetSlot(i);
            if (!stack.IsEmpty)
            {
                return stack.item;
            }
        }
        return null;
    }

    public void SetBoxSprite(Sprite newSprite)
    {
        if (boxSpriteRenderer != null)
            boxSpriteRenderer.sprite = newSprite;
    }

    public Sprite GetCurrentBoxSprite()
    {
        return boxSpriteRenderer != null ? boxSpriteRenderer.sprite : null;
    }

    public void AddItemBoxSprite(Item item, Sprite boxSprite)
    {
        // Remove existing entry for this item if it exists
        itemBoxSprites.RemoveAll(ibs => ibs.item == item);

        // Add new entry
        itemBoxSprites.Add(new ItemBoxSprite { item = item, boxSprite = boxSprite });
    }

    public void AddItemTypeBoxSprite(ItemType itemType, Sprite boxSprite)
    {
        itemBoxSprites.Add(new ItemBoxSprite { itemType = itemType, boxSprite = boxSprite });
    }

    public void AddItemTagBoxSprite(string itemTag, Sprite boxSprite)
    {
        itemBoxSprites.Add(new ItemBoxSprite { itemTag = itemTag, boxSprite = boxSprite });
    }

    private System.Collections.IEnumerator ShowRejectFeedbackOnSlot(InventorySlot slot)
    {
        if (slot != null && slot.rejectHighlight != null)
        {
            slot.rejectHighlight.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            slot.rejectHighlight.gameObject.SetActive(false);
        }
    }

    private System.Collections.IEnumerator ShowAcceptFeedbackOnSlot(InventorySlot slot)
    {
        if (slot != null)
        {
            slot.ShowAcceptFeedback();
        }
        yield return null;
    }

    // ============================================================================
    // PUBLIC INFO METHODS (for UI display)
    // ============================================================================

    public int GetTotalValue()
    {
        return CalculateTotalValue();
    }

    public int GetTotalItemCount()
    {
        int totalItems = 0;
        for (int i = 0; i < boxInventorySize; i++)
        {
            ItemStack stack = container.GetSlot(i);
            if (!stack.IsEmpty)
            {
                totalItems += stack.quantity;
            }
        }
        return totalItems;
    }

    // Methods for InteractionManager compatibility
    public string GetInteractionPrompt() => "Open Sell Box";
    public bool CanInteract() => !isSellBoxOpen;

    public float GetInteractionRange()
    {
        return balance != null ? balance.sellBoxInteractionRange : maxInteractionDistance;
    }

    public bool IsActive()
    {
        return gameObject.activeInHierarchy && enabled;
    }
}

} // namespace SowurShield.Core