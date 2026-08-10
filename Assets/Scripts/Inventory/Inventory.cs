using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using SowurShield.Core;

namespace SowurShield.Inventory
{

public class Inventory : MonoBehaviour, ISaveable
{
    [Header("Inventory Settings")]
    public int inventorySize = 36; // 4 rows of 9 slots
    public int hotbarSize = 9; // First 9 slots are hotbar

    [Header("UI References")]
    public Transform slotParent; // Parent object containing all slot UI elements (DEPRECATED - use hotbarParent and storageParent)
    public GameObject slotPrefab; // Prefab for creating slots
    public ItemTooltip tooltip;
    public Transform hotbarParent; // Parent for hotbar slots (first 9 slots)
    public Transform storageParent; // Parent for storage slots (remaining 27 slots)
    public GameObject storagePanelBackground; // Wood window panel shown behind the storage grid while open

    [Header("Input Actions")]
    public InputActionReference inventoryToggleAction;
    public InputActionReference[] hotbarActions;

    [Header("Audio")]
    public AudioClip pickupSound;
    public AudioClip dropSound;
    public AudioClip useSound;

    // Inventory data - now using container system
    private InventoryContainer container;

    /// <summary>
    /// Builds and refreshes the slot UI — the last container to adopt it (Etapa 4a-bis).
    /// The hotbar and the storage grid are two SlotGroups of ONE container, which is why the
    /// view had to learn about groups first: slot 0-8 live under hotbarParent and the rest
    /// under storageParent, which starts hidden.
    /// </summary>
    private ContainerView view;

    /// <summary>Storage is group 1; the hotbar is group 0. Used by ToggleInventory.</summary>
    private const int StorageGroupIndex = 1;

    // Selection and interaction
    private int selectedSlotIndex = 0;
    private InventorySlot selectedSlot;
    private bool isInventoryOpen = false;

    // Hotbar auto-refill tracking
    private Item[] lastHotbarItems; // Track last item in each hotbar slot for refill

    // Events
    public System.Action<int> OnHotbarSelectionChanged;
    public System.Action<ItemStack> OnItemUsed;
    public System.Action<ItemStack> OnItemAdded;
    public System.Action<ItemStack> OnItemRemoved;
    public System.Action<int> OnInventorySizeChanged;

    // Properties
    public ItemStack SelectedItem => selectedSlotIndex >= 0 && selectedSlotIndex < inventorySize
        ? container.GetSlot(selectedSlotIndex) : new ItemStack();
    public bool IsInventoryOpen => isInventoryOpen;
    public int SelectedSlotIndex => selectedSlotIndex;

    // Legacy compatibility method
    public Item GetSelectedItem()
    {
        ItemStack selected = SelectedItem;
        return selected.IsEmpty ? null : selected.item;
    }

    // Legacy compatibility methods for GroundItem and SoilBlockInteractable
    public bool CanAdd(Item item, int quantity = 1)
    {
        return container.CanAdd(item, quantity);
    }

    public bool Add(Item item, int quantity = 1)
    {
        return AddItem(item, quantity);
    }

    public bool Remove(Item item, int quantity = 1)
    {
        return RemoveItem(item, quantity);
    }

    // ============================================================================
    // INITIALIZATION
    // ============================================================================

    private void Awake()
    {
        InitializeInventory();

        // Only register if this is on the Player GameObject
        if (gameObject.CompareTag("Player"))
        {
            // Register with SaveManager first (before SaveManager.Start() runs)
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.RegisterSaveable(this);

            }
        }
    }

    private void Start()
    {
        SetupUI();
        SelectSlot(0); // Select first hotbar slot
        EnableInputActions();

        // Carry items across the combat round-trip (scene reload rebuilds this
        // inventory; disk saves may be unavailable — see InventorySceneSnapshot).
        // In the normal flow SceneTransitionManager restores AFTER it re-applies
        // save data (which would overwrite us) — only restore here when playing
        // the scene directly without a transition manager.
        if (SowurShield.Core.SceneTransitionManager.Instance == null)
            InventorySceneSnapshot.TryRestore(this);

        // Cache the background panel while it's still active in the scene (GameObject.Find
        // can't locate inactive objects), then hide it — the inventory starts closed.
        if (storagePanelBackground == null)
            storagePanelBackground = GameObject.Find("InventoryPanelBG");
        if (storagePanelBackground != null)
            storagePanelBackground.SetActive(false);
    }

    private void OnDestroy()
    {
        DisableInputActions();

        // Unregister from SaveManager if this was registered
        if (gameObject.CompareTag("Player") && SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
        }
    }

    private void InitializeInventory()
    {
        // Initialize ItemDatabase
        var _ = ItemDatabase.Instance;

        // Create container
        container = new InventoryContainer(inventorySize, "PlayerInventory");

        // Subscribe to container events
        container.OnSlotChanged += (index, stack) => UpdateSlot(index);
        container.OnItemAdded += (item, qty) => OnItemAdded?.Invoke(new ItemStack(item, qty));
        container.OnItemRemoved += (item, qty) => OnItemRemoved?.Invoke(new ItemStack(item, qty));

        // Initialize hotbar tracking
        lastHotbarItems = new Item[hotbarSize];
    }

    private void SetupUI()
    {
        if (hotbarParent == null || storageParent == null || slotPrefab == null)
        {
            Debug.LogError(
                $"[Inventory] {name}: hotbarParent, storageParent and slotPrefab must all be assigned — no slots created.",
                this);
            return;
        }

        // RUNTIME FIX: Ensure GridLayoutGroups are properly configured
        EnsureGridLayoutGroup(hotbarParent, 9, TextAnchor.MiddleCenter);
        EnsureGridLayoutGroup(storageParent, 9, TextAnchor.UpperCenter);

        if (view == null) view = gameObject.AddComponent<ContainerView>();

        // Two groups, one container. The storage group starts inactive, which is what used to
        // be a SetActive(false) inside the build loop and a lazy "create the rest on first
        // open" branch in ToggleInventory.
        view.Configure(
            slotPrefab,
            new SlotGroup(hotbarParent, 0, hotbarSize, true, "HotbarSlot"),
            new SlotGroup(storageParent, hotbarSize, 0, false, "StorageSlot"));

        // Hotbar tracking is data and must survive a UI rebuild, so it is refreshed from the
        // container here rather than riding on slot construction the way it used to.
        view.Bind(container, DefaultContainerPolicy.Instance, (slotUI, index) => TrackHotbarItem(index));
    }

    // CreateSlotUI and the legacy slotParent adoption path were deleted in Etapa 4a-bis.
    // The legacy branch scanned slotParent for slots already present in the scene; in
    // SampleScene — the only scene with an Inventory — hotbarParent and storageParent are both
    // wired and their transforms have no pre-existing children, so it was unreachable.

    /// <summary>
    /// Ensures the parent Transform has a properly configured GridLayoutGroup
    /// This fixes the issue where StorageContainer was missing GridLayoutGroup
    /// </summary>
    private void EnsureGridLayoutGroup(Transform parent, int columns, TextAnchor alignment)
    {
        if (parent == null) return;

        GridLayoutGroup grid = parent.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = parent.gameObject.AddComponent<GridLayoutGroup>();
        }

        // Configure grid settings to match prefab size (60x60) with tight spacing
        grid.cellSize = new Vector2(60, 60);
        grid.spacing = new Vector2(5, 5);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.childAlignment = alignment;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.padding = new RectOffset(0, 0, 0, 0);

    }

    // ============================================================================
    // INPUT MANAGEMENT
    // ============================================================================

    private void EnableInputActions()
    {
        if (inventoryToggleAction != null)
        {
            inventoryToggleAction.action.Enable();
            inventoryToggleAction.action.performed += OnInventoryToggle;
        }

        // Null-guarded, not just element-guarded: hotbarActions is a public array left unset
        // unless someone fills it in the Inspector, so an Inventory created in code — a test,
        // or any runtime-built one — threw here in Start() and again in OnDestroy().
        if (hotbarActions == null) return;

        for (int i = 0; i < hotbarActions.Length; i++)
        {
            if (hotbarActions[i] != null)
            {
                hotbarActions[i].action.Enable();
                int slotIndex = i; // Capture index for closure
                hotbarActions[i].action.performed += (ctx) => OnHotbarSlot(slotIndex);
            }
        }
    }

    private void DisableInputActions()
    {
        if (inventoryToggleAction != null)
        {
            inventoryToggleAction.action.performed -= OnInventoryToggle;
            inventoryToggleAction.action.Disable();
        }

        if (hotbarActions == null) return;

        for (int i = 0; i < hotbarActions.Length; i++)
        {
            if (hotbarActions[i] != null)
            {
                hotbarActions[i].action.Disable();
            }
        }
    }

    private void OnInventoryToggle(InputAction.CallbackContext context)
    {
        ToggleInventory();
    }

    private void OnHotbarSlot(int slotIndex)
    {
        if (slotIndex < hotbarSize)
        {
            SelectSlot(slotIndex);
        }
    }

    // ============================================================================
    // INVENTORY OPERATIONS
    // ============================================================================

    public bool AddItem(Item item, int quantity = 1)
    {
        bool success = container.AddItem(item, quantity);

        // Play sound if any items were added
        if (success || container.GetItemCount(item) > 0)
        {
            PlaySound(pickupSound);
        }

        // Notify quest system so CollectItem objectives advance
        if (success && item != null)
            SowurShield.Dialogue.QuestManager.Instance?.OnInventoryItemCountChanged(
                item.itemName, container.GetItemCount(item));

        return success;
    }

    public bool RemoveItem(Item item, int quantity = 1)
    {
        return container.RemoveItem(item, quantity);
    }

    public int GetItemCount(Item item)
    {
        return container.GetItemCount(item);
    }

    public bool HasItem(Item item, int quantity = 1)
    {
        return container.HasItem(item, quantity);
    }

    public void ClearInventory()
    {
        container.ClearAll();
    }

    // ============================================================================
    // SLOT MANAGEMENT
    // ============================================================================

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= hotbarSize) return;

        // Deselect previous slot
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        selectedSlotIndex = index;

        // Select new slot
        selectedSlot = view?.GetSlotUI(index);
        if (selectedSlot != null)
            selectedSlot.SetSelected(true);

        OnHotbarSelectionChanged?.Invoke(selectedSlotIndex);
    }

    public void SelectSlot(InventorySlot slotUI)
    {
        int index = IndexOfSlot(slotUI);
        if (index >= 0 && index < hotbarSize)
        {
            SelectSlot(index);
        }
    }

    private void UpdateSlot(int index)
    {
        // The view refreshes itself from container.OnSlotChanged, so pushing the stack into the
        // slot UI here would be redundant — except when the view has not been built yet (LoadData
        // runs before Start) or when a caller changed a slot without going through SetSlot.
        // RefreshSlot is cheap and tolerates both, so it stays as a belt-and-braces call.
        view?.RefreshSlot(index);

        TrackHotbarItem(index);
    }

    /// <summary>
    /// Record what a hotbar slot currently holds, so auto-refill knows what to pull from storage.
    /// Data, not UI: this used to live inside UpdateSlot's "does a slot UI exist?" guard, which
    /// made the feature depend on UI construction order and impossible to test without a scene.
    /// </summary>
    private void TrackHotbarItem(int index)
    {
        if (index < 0 || index >= hotbarSize || lastHotbarItems == null) return;

        ItemStack stack = container.GetSlot(index);
        lastHotbarItems[index] = stack.IsEmpty ? null : stack.item;
    }

    private void UpdateAllSlots()
    {
        view?.Refresh();

        // Group visibility is not re-asserted here: the view remembers what each group is
        // showing and restores it across a Rebuild.

        for (int i = 0; i < hotbarSize && i < container.MaxSlots; i++)
            TrackHotbarItem(i);
    }

    // ============================================================================
    // DRAG AND DROP OPERATIONS
    // ============================================================================

    // HandleSlotDrop was deleted in Etapa 4b: every drop now goes through
    // SlotTransferRouter -> ItemTransferService. Post-move bookkeeping that used to live
    // at the end of it (hotbar auto-refill, drop sound) is in OnSlotsChangedExternally.


    public void SplitStack(InventorySlot slotUI)
    {
        int slotIndex = IndexOfSlot(slotUI);
        if (slotIndex < 0 || slotIndex >= inventorySize) return;

        ItemStack stack = container.GetSlot(slotIndex);
        if (stack.IsEmpty || stack.quantity <= 1) return;

        // Find empty slot for split
        int emptySlotIndex = container.GetFirstEmptySlotIndex();
        if (emptySlotIndex == -1) return; // No empty slot available

        // Split the stack
        int splitAmount = stack.quantity / 2;
        ItemStack originalStack = stack.Clone();
        originalStack.quantity -= splitAmount;

        container.SetSlot(slotIndex, originalStack);
        container.SetSlot(emptySlotIndex, new ItemStack(stack.item, splitAmount));

        UpdateSlot(slotIndex);
        UpdateSlot(emptySlotIndex);
    }

    public void EndDragOperation()
    {
        // Called when drag operation ends without a valid drop
    }

    // ============================================================================
    // ITEM USAGE AND CONSUMPTION
    // ============================================================================

    public void UseItem(InventorySlot slotUI)
    {
        UseItemAt(IndexOfSlot(slotUI));
    }

    /// <summary>
    /// Consume one item from a slot by index. Split out of <see cref="UseItem(InventorySlot)"/>
    /// so the consume-and-refill behaviour can be exercised without a slot UI.
    /// </summary>
    /// <returns>True if an item was actually consumed.</returns>
    public bool UseItemAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySize) return false;

        ItemStack stack = container.GetSlot(slotIndex);
        if (stack.IsEmpty || !stack.item.isConsumable) return false;

        Item usedItem = stack.item;

        // Apply the item's effect
        UseItem(usedItem);

        // Remove one from stack
        ItemStack updatedStack = stack.Clone();
        updatedStack.quantity--;
        if (updatedStack.quantity <= 0)
            updatedStack.Clear();

        container.SetSlot(slotIndex, updatedStack);

        // Refill AFTER the slot is actually empty in the container. This call used to run
        // BEFORE the write above, so CheckHotbarAutoRefill's "is this slot empty now?" guard
        // always saw the pre-consumption stack and returned immediately — hotbar auto-refill
        // never fired on the consume path (it worked on the drag path, where the call already
        // came after the writes).
        //
        // usedItem has to be handed over explicitly: the SetSlot above already fired
        // OnSlotChanged -> UpdateSlot, which wiped lastHotbarItems[slotIndex] on the way past.
        // Reading the tracking here would always find null and skip the refill.
        if (updatedStack.IsEmpty && slotIndex < hotbarSize)
            CheckHotbarAutoRefill(slotIndex, usedItem);

        UpdateSlot(slotIndex);
        PlaySound(useSound);
        OnItemUsed?.Invoke(new ItemStack(usedItem, 1));
        return true;
    }

    private void UseItem(Item item)
    {
        if (!item.isConsumable) return;

        // Handle item effects based on type
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            if (item.energyRestore > 0)
            {
                playerStats.RestoreEnergy(item.energyRestore);
            }
            if (item.healthRestore > 0)
            {
                playerStats.RestoreHealth(item.healthRestore);
            }
        }

    }

    // Method to clear a slot from the inventory array (called during drag start)
    public ItemStack ClearSlotForDrag(InventorySlot slotUI)
    {
        int slotIndex = IndexOfSlot(slotUI);
        if (slotIndex < 0 || slotIndex >= inventorySize)
        {
            return new ItemStack();
        }

        ItemStack removedStack = container.GetSlot(slotIndex).Clone();
        container.SetSlot(slotIndex, new ItemStack());
        UpdateSlot(slotIndex); // Update the visual slot to show it's empty
        return removedStack;
    }

    // Method to restore a slot in the inventory array (called when drag fails)
    public void RestoreSlotFromDrag(InventorySlot slotUI, ItemStack itemStack)
    {
        int slotIndex = IndexOfSlot(slotUI);
        if (slotIndex < 0 || slotIndex >= inventorySize)
        {
            return;
        }

        container.SetSlot(slotIndex, itemStack.Clone());
        UpdateSlot(slotIndex);
    }

    // ============================================================================
    // HOTBAR AUTO-REFILL SYSTEM
    // ============================================================================

    /// <summary>
    /// Check if a hotbar slot needs auto-refill and perform it if needed
    /// Called when a hotbar slot empties (from consumption or drag/drop)
    /// </summary>
    /// <param name="knownLastItem">
    /// What the slot held immediately before it was emptied, when the caller already knows it.
    /// Writing the emptied stack through SetSlot fires OnSlotChanged -> UpdateSlot, which clears
    /// lastHotbarItems for that slot — so a caller that empties the slot first (UseItemAt) must
    /// pass the item in, otherwise the tracking it would have read is already gone. Callers that
    /// did not empty the slot themselves (the drag path) leave this null and use the tracking.
    /// </param>
    private void CheckHotbarAutoRefill(int slotIndex, Item knownLastItem = null)
    {
        // Only refill hotbar slots
        if (slotIndex < 0 || slotIndex >= hotbarSize) return;

        ItemStack currentStack = container.GetSlot(slotIndex);

        // Only refill if slot is now empty
        if (!currentStack.IsEmpty) return;

        // Get the last item that was in this slot
        Item refillItem = knownLastItem ?? lastHotbarItems[slotIndex];
        if (refillItem == null) return;

        // Search main inventory (slots hotbarSize to end) for matching item
        for (int i = hotbarSize; i < container.MaxSlots; i++)
        {
            ItemStack stack = container.GetSlot(i);
            if (!stack.IsEmpty && stack.item == refillItem)
            {
                // Move entire stack to hotbar
                container.SetSlot(slotIndex, stack.Clone());
                container.SetSlot(i, new ItemStack());

                UpdateSlot(slotIndex);
                UpdateSlot(i);

                return;
            }
        }

        // No matching items found - clear the tracking
        lastHotbarItems[slotIndex] = null;
    }

    // ============================================================================
    // UI MANAGEMENT
    // ============================================================================

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        // Background window panel follows the storage grid's visibility (cached in Start).
        if (storagePanelBackground != null)
            storagePanelBackground.SetActive(isInventoryOpen);

        // Show/hide the storage grid (hotbar always visible). The lazy "create any missing
        // slots on first open" branch that used to follow is gone: the view builds every slot
        // in Bind, so by the time this runs they all exist.
        view?.SetGroupActive(StorageGroupIndex, isInventoryOpen);

        // Don't pause the game - just disable player movement if inventory is open
        // You might want to disable player movement through a different mechanism
    }

    public void ShowTooltip(ItemStack itemStack, Vector3 position)
    {
        if (tooltip != null && !itemStack.IsEmpty)
        {
            tooltip.ShowTooltip(itemStack, position);
        }
    }

    public void HideTooltip()
    {
        if (tooltip != null)
        {
            tooltip.HideTooltip();
        }
    }

    // ============================================================================
    // UTILITY METHODS
    // ============================================================================

    public void SortInventory()
    {
        // Get all non-empty stacks
        List<ItemStack> allStacks = container.GetAllItems();

        // Sort by item type, then by name
        allStacks.Sort((a, b) =>
        {
            int typeCompare = a.item.itemType.CompareTo(b.item.itemType);
            if (typeCompare != 0) return typeCompare;
            return string.Compare(a.item.itemName, b.item.itemName);
        });

        // Clear main inventory (keep hotbar untouched)
        for (int i = hotbarSize; i < inventorySize; i++)
        {
            container.SetSlot(i, new ItemStack());
        }

        // Place sorted items back, starting after hotbar
        int currentIndex = hotbarSize;
        foreach (ItemStack stack in allStacks)
        {
            // Skip items that are already in hotbar
            bool inHotbar = false;
            for (int h = 0; h < hotbarSize; h++)
            {
                ItemStack hotbarStack = container.GetSlot(h);
                if (!hotbarStack.IsEmpty && hotbarStack.item == stack.item && hotbarStack.quantity == stack.quantity)
                {
                    inHotbar = true;
                    break;
                }
            }

            if (inHotbar) continue;

            if (currentIndex >= inventorySize) break;
            container.SetSlot(currentIndex, stack);
            currentIndex++;
        }

        UpdateAllSlots();
    }

    public List<ItemStack> GetAllItems()
    {
        return container.GetAllItems();
    }

    /// <summary>Total slot count (hotbar + storage) — for per-slot snapshots.</summary>
    public int SlotCount => inventorySize;

    /// <summary>The backing container, so SlotTransferRouter can move items in and out of it.</summary>
    public IInventoryContainer Container => container;

    /// <summary>Index of one of this inventory's slot UIs, or -1 if it is not ours.</summary>
    public int IndexOfSlot(InventorySlot slotUI) => view == null ? -1 : view.IndexOf(slotUI);

    /// <summary>
    /// Called by SlotTransferRouter after it moved items in or out of one of our slots.
    /// Covers the bookkeeping that used to live at the end of HandleSlotDrop — the container
    /// itself already refreshed the slot UI through OnSlotChanged.
    /// </summary>
    public void OnSlotsChangedExternally(int slotIndex)
    {
        CheckHotbarAutoRefill(slotIndex);
        UpdateSlot(slotIndex);
        PlaySound(dropSound);
    }

    /// <summary>Per-index slot access, used by InventorySceneSnapshot to preserve layout.</summary>
    public ItemStack GetSlotAt(int index) => container.GetSlot(index);

    public void SetSlotAt(int index, ItemStack stack) => container.SetSlot(index, stack);

    public List<ItemStack> GetItemsByType(ItemType itemType)
    {
        return container.GetItemsByType(itemType);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }

    // ============================================================================
    // SAVE/LOAD SUPPORT
    // ============================================================================

    [System.Serializable]
    public class InventoryData
    {
        public ItemStackData[] items;
        public int selectedSlotIndex;

        [System.Serializable]
        public class ItemStackData
        {
            public string itemName;
            public int quantity;

            public ItemStackData() { }
            public ItemStackData(ItemStack stack)
            {
                itemName = stack.IsEmpty ? "" : stack.item.itemName;
                quantity = stack.quantity;
            }
        }
    }

    public InventoryData GetInventoryData()
    {
        InventoryData data = new InventoryData();
        data.selectedSlotIndex = selectedSlotIndex;
        data.items = new InventoryData.ItemStackData[inventorySize];

        for (int i = 0; i < inventorySize; i++)
        {
            data.items[i] = new InventoryData.ItemStackData(container.GetSlot(i));
        }

        return data;
    }

    public void LoadInventoryData(InventoryData data)
    {
        if (data == null || data.items == null) return;

        // Load items using ItemDatabase
        for (int i = 0; i < Mathf.Min(data.items.Length, inventorySize); i++)
        {
            var itemData = data.items[i];
            if (string.IsNullOrEmpty(itemData.itemName))
            {
                container.SetSlot(i, new ItemStack());
            }
            else
            {
                // Use ItemDatabase for fast lookup
                Item item = ItemDatabase.GetItem(itemData.itemName);

                if (item != null)
                {
                    container.SetSlot(i, new ItemStack(item, itemData.quantity));
                }
                else
                {
                    container.SetSlot(i, new ItemStack());
                }
            }
        }

        // Load selected slot
        selectedSlotIndex = Mathf.Clamp(data.selectedSlotIndex, 0, hotbarSize - 1);

        UpdateAllSlots();
        SelectSlot(selectedSlotIndex);
    }

    // ============================================================================
    // ISAVEABLE IMPLEMENTATION
    // ============================================================================

    public void SaveData(GameData gameData)
    {
        gameData.inventoryData.selectedSlotIndex = selectedSlotIndex;
        gameData.inventoryData.inventorySize = inventorySize;
        gameData.inventoryData.inventoryItems.Clear();

        // Save all inventory items using container
        int itemCount = 0;
        for (int i = 0; i < inventorySize; i++)
        {
            ItemStack stack = container.GetSlot(i);
            gameData.inventoryData.inventoryItems.Add(new InventoryGameData.ItemStackData(stack));
            if (!stack.IsEmpty)
            {
                itemCount++;
            }
        }

    }

    public void LoadData(GameData gameData)
    {
        selectedSlotIndex = gameData.inventoryData.selectedSlotIndex;

        // Clear current inventory
        container.ClearAll();

        // Load items from save data using ItemDatabase
        int foundItems = 0;
        for (int i = 0; i < Mathf.Min(gameData.inventoryData.inventoryItems.Count, inventorySize); i++)
        {
            var itemData = gameData.inventoryData.inventoryItems[i];

            if (!itemData.IsEmpty)
            {
                foundItems++;

                // Use ItemDatabase for fast lookup
                Item item = ItemDatabase.GetItem(itemData.itemName);

                if (item != null)
                {
                    container.SetSlot(i, new ItemStack(item, itemData.quantity));
                }
                else
                {
                }
            }
        }


        // IMPORTANT: Ensure inventory is closed after loading. LoadData can run before Start,
        // in which case there is no view yet — the storage group is then created hidden by
        // Bind anyway, so the null case needs no fallback.
        isInventoryOpen = false;
        view?.SetGroupActive(StorageGroupIndex, false);

        // Update UI and selection
        UpdateAllSlots();
        SelectSlot(Mathf.Clamp(selectedSlotIndex, 0, hotbarSize - 1));
    }

    // ============================================================================
    // SORTING & FILTERING (Phase 2)
    // ============================================================================

    /// <summary>
    /// Sort inventory using specified sort mode
    /// Sorts only the main inventory (preserves hotbar)
    /// </summary>
    public void SortInventoryBy(InventorySorting.SortMode mode, InventorySorting.SortDirection direction = InventorySorting.SortDirection.Ascending)
    {
        // Get all items from main inventory (skip hotbar)
        List<ItemStack> mainInventoryItems = new List<ItemStack>();
        for (int i = hotbarSize; i < inventorySize; i++)
        {
            ItemStack stack = container.GetSlot(i);
            if (!stack.IsEmpty)
            {
                mainInventoryItems.Add(stack.Clone());
            }
        }

        // Sort the items
        InventorySorting.Sort(mainInventoryItems, mode, direction);

        // Clear main inventory
        for (int i = hotbarSize; i < inventorySize; i++)
        {
            container.SetSlot(i, new ItemStack());
        }

        // Place sorted items back
        int currentIndex = hotbarSize;
        foreach (ItemStack stack in mainInventoryItems)
        {
            if (currentIndex >= inventorySize) break;
            container.SetSlot(currentIndex, stack);
            currentIndex++;
        }

        UpdateAllSlots();
    }

    /// <summary>
    /// Sort with multiple criteria
    /// </summary>
    public void SortInventoryMultiple(params (InventorySorting.SortMode mode, InventorySorting.SortDirection direction)[] criteria)
    {
        List<ItemStack> mainInventoryItems = new List<ItemStack>();
        for (int i = hotbarSize; i < inventorySize; i++)
        {
            ItemStack stack = container.GetSlot(i);
            if (!stack.IsEmpty)
            {
                mainInventoryItems.Add(stack.Clone());
            }
        }

        InventorySorting.SortMultiple(mainInventoryItems, criteria);

        for (int i = hotbarSize; i < inventorySize; i++)
        {
            container.SetSlot(i, new ItemStack());
        }

        int currentIndex = hotbarSize;
        foreach (ItemStack stack in mainInventoryItems)
        {
            if (currentIndex >= inventorySize) break;
            container.SetSlot(currentIndex, stack);
            currentIndex++;
        }

        UpdateAllSlots();
    }

    /// <summary>
    /// Get filtered view of inventory items
    /// </summary>
    public List<ItemStack> GetFilteredItems(InventoryFiltering.IInventoryFilter filter)
    {
        List<ItemStack> allItems = container.GetAllItems();
        return InventoryFiltering.Filter(allItems, filter);
    }

    /// <summary>
    /// Get items by tag (convenience method)
    /// </summary>
    public List<ItemStack> GetItemsByTagFiltered(string tag)
    {
        List<ItemStack> allItems = container.GetAllItems();
        return InventoryFiltering.FilterByTag(allItems, tag);
    }

    /// <summary>
    /// Search items by name
    /// </summary>
    public List<ItemStack> SearchItems(string searchTerm)
    {
        List<ItemStack> allItems = container.GetAllItems();
        return InventoryFiltering.SearchByName(allItems, searchTerm);
    }

    // ============================================================================
    // INVENTORY UPGRADES (Phase 2)
    // ============================================================================

    /// <summary>
    /// Upgrade inventory size (adds more slots)
    /// </summary>
    public bool UpgradeInventorySize(int additionalSlots)
    {
        if (additionalSlots <= 0)
        {
            return false;
        }

        int oldSize = inventorySize;
        int newSize = oldSize + additionalSlots;

        // Update the container size. OnSizeChanged makes the view rebuild its slots, so the
        // extra UI prefabs no longer have to be created here.
        container.SetMaxSlots(newSize);
        inventorySize = newSize;

        OnInventorySizeChanged?.Invoke(newSize);

        return true;
    }

    /// <summary>
    /// Set inventory size to a specific value
    /// Warning: Shrinking inventory may lose items!
    /// </summary>
    public bool SetInventorySize(int newSize)
    {
        if (newSize < hotbarSize)
        {
            return false;
        }

        int oldSize = inventorySize;
        if (newSize == oldSize) return true;

        // Warn if shrinking. SetMaxSlots destroys whatever sits in the removed slots without
        // saying so (see plan §6.3) — nothing shrinks the player inventory today, but if that
        // ever changes the loss should not be silent.
        if (newSize < oldSize)
        {
            int itemsInDangerZone = 0;
            for (int i = newSize; i < oldSize; i++)
            {
                if (!container.GetSlot(i).IsEmpty)
                    itemsInDangerZone++;
            }

            if (itemsInDangerZone > 0)
            {
                Debug.LogWarning(
                    $"[Inventory] Shrinking {oldSize} -> {newSize} destroys {itemsInDangerZone} stack(s) in the removed slots.",
                    this);
            }
        }

        // Update container. SetMaxSlots fires OnSizeChanged, which the view answers with a
        // Rebuild — so creating and destroying slot prefabs by hand here is no longer needed.
        container.SetMaxSlots(newSize);
        inventorySize = newSize;

        OnInventorySizeChanged?.Invoke(newSize);
        UpdateAllSlots();

        return true;
    }

    /// <summary>
    /// Get current inventory capacity
    /// </summary>
    public int GetInventoryCapacity()
    {
        return inventorySize;
    }

    /// <summary>
    /// Get number of used slots
    /// </summary>
    public int GetUsedSlotCount()
    {
        int count = 0;
        for (int i = 0; i < inventorySize; i++)
        {
            if (!container.GetSlot(i).IsEmpty)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Get number of empty slots
    /// </summary>
    public int GetEmptySlotCount()
    {
        return inventorySize - GetUsedSlotCount();
    }

    /// <summary>
    /// Check if inventory is full
    /// </summary>
    public bool IsFull()
    {
        return !container.HasEmptySlot();
    }
}

} // namespace SowurShield.Inventory
