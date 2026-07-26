using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization;
using SowurShield.Core;
using SowurShield.Inventory;
using SowurShield.Inventory.Policies;

namespace SowurShield.Animals
{

/// <summary>
/// A placeable world object that stores food and auto-feeds animals in its linked AnimalZone.
/// Modeled after SellBox — implements IInteractable (E key), IUIWindow (panel management),
/// and ISaveable (persistence). Uses InventoryContainer for internal food storage.
///
/// Auto-feeding occurs on day change: for each animal in the zone, the trough removes
/// matching food items and calls Animal.AutoFeed().
/// </summary>
public class FeedingTrough : MonoBehaviour, IInteractable, IUIWindow, ISaveable
{
    [Header("Zone Link")]
    [SerializeField] private AnimalZone linkedZone;

    [Header("Storage")]
    [SerializeField] private int slotCount = 12;

    [Header("Visual Sprites")]
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite partialSprite;
    [SerializeField] private Sprite fullSprite;

    [Header("UI References")]
    [SerializeField] private GameObject troughPanel;
    [SerializeField] private Transform slotParent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button closeButton;

    [Header("Interaction")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private string interactionPrompt = "Open Feeding Trough";

    [Header("Localization")]
    [SerializeField] private LocalizedString troughTitleText; // table "Animals", key "animals.trough.title"
    [SerializeField] private LocalizedString troughStatusText; // table "Animals", key "animals.trough.status"

    // Internal storage
    private InventoryContainer container;
    private SpriteRenderer spriteRenderer;
    private bool isOpen = false;

    // Slot UI is owned by ContainerView (Etapa 3 of review/04_CONTAINER_REFACTOR_PLAN.md).
    // The component is added at runtime rather than wired in the Inspector so this migration
    // needs no scene changes — the trough already holds slotParent/slotPrefab and hands them over.
    private ContainerView view;
    private FeedingTroughPolicy policy;

    // =========================================================================
    // IUIWindow Implementation
    // =========================================================================

    public string WindowName => "FeedingTrough";
    public int WindowPriority => SowurShield.Core.WindowPriority.SellBox; // Same tier as SellBox (20)
    public bool IsWindowOpen => isOpen;

    // ESC is reserved for the pause menu; close this window with E (Interact) instead.
    public bool CanCloseWithEsc => false;

    public void OpenWindow()
    {
        if (troughPanel != null)
            troughPanel.SetActive(true);
        isOpen = true;

        DisablePlayerMovement();
        UpdateStatusText();
    }

    public void CloseWindow()
    {
        if (troughPanel != null)
            troughPanel.SetActive(false);
        isOpen = false;

        EnablePlayerMovement();
    }

    public void OnWindowBlocked(string blockedBy) { }

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        container = new InventoryContainer(slotCount, $"FeedingTrough_{gameObject.name}");

        // RejectNonFood stays false: the trough accepts anything today and simply never consumes
        // what no animal eats. Changing that is a gameplay decision, not part of this refactor.
        policy = new FeedingTroughPolicy(GetAcceptedFood);

        // The view handles the slot UI. This handler covers what is the trough's own business,
        // and stays subscribed directly to the container because it must also run before the
        // slot UI exists — LoadData writes slots during Start, ahead of SetupUI.
        container.OnSlotChanged += HandleContainerChanged;

        if (troughPanel != null)
            troughPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseTrough);
    }

    private void Start()
    {
        // Register with InteractionManager
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.RegisterInteractable(this);

        // Register with UIManager
        if (UIManager.Instance != null)
            UIManager.Instance.RegisterWindow(this);

        // Register with SaveManager
        if (SaveManager.Instance != null)
            SaveManager.Instance.RegisterSaveable(this);

        // Subscribe to day changes for auto-feeding
        if (GameTimeController.instance != null)
            GameTimeController.instance.OnDayChanged += OnDayChanged;

        SetupUI();
        UpdateTroughSprite();

        SowurShield.Core.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        if (titleText != null)
            titleText.text = troughTitleText.SafeGetLocalizedString();
        UpdateStatusText();
    }

    /// <summary>Reacts to the container changing. Slot UI is the view's job; this is ours.</summary>
    private void HandleContainerChanged(int index, ItemStack stack)
    {
        UpdateTroughSprite();
        UpdateStatusText();
    }

    private void SetupUI()
    {
        if (view != null && view.IsBuilt) return;

        if (slotParent == null || slotPrefab == null)
        {
            Debug.LogWarning("[FeedingTrough] SlotParent or SlotPrefab not assigned — slots won't be created.");
            return;
        }

        if (view == null)
            view = gameObject.AddComponent<ContainerView>();

        view.Configure(slotParent, slotPrefab, "TroughSlot");
        // No per-slot configuration needed: the slot's OwnerView is what identifies it now.
        view.Bind(container, policy);

        if (titleText != null)
            titleText.text = troughTitleText.SafeGetLocalizedString();

        UpdateStatusText();
    }

    /// <summary>
    /// Every item animals in the linked zone eat. Feeds FeedingTroughPolicy — only consulted
    /// while RejectNonFood is on, which it is not by default.
    /// </summary>
    private IEnumerable<Item> GetAcceptedFood()
    {
        if (linkedZone == null) yield break;

        foreach (Animal animal in linkedZone.GetAnimals())
        {
            AnimalData data = animal != null ? animal.AnimalData : null;
            if (data?.dailyFoodRequirements == null) continue;

            foreach (FoodRequirement req in data.dailyFoodRequirements)
            {
                if (string.IsNullOrEmpty(req.itemName)) continue;

                Item food = ItemDatabase.GetItem(req.itemName);
                if (food != null) yield return food;
            }
        }
    }

    private void OnDestroy()
    {
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.UnregisterInteractable(this);

        if (UIManager.Instance != null)
            UIManager.Instance.UnregisterWindow(this);

        if (SaveManager.Instance != null)
            SaveManager.Instance.UnregisterSaveable(this);

        if (GameTimeController.instance != null)
            GameTimeController.instance.OnDayChanged -= OnDayChanged;

        if (container != null)
            container.OnSlotChanged -= HandleContainerChanged;

        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    // =========================================================================
    // IInteractable Implementation
    // =========================================================================

    public void Interact()
    {
        if (isOpen)
        {
            CloseTrough();
            return;
        }

        OpenTrough();
    }

    public string GetInteractionPrompt() => interactionPrompt;
    public float GetInteractionRange() => interactionRange;
    public bool CanInteract() => !isOpen;

    // =========================================================================
    // Open / Close
    // =========================================================================

    private void OpenTrough()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TryOpenWindow(this);
        }
        else
        {
            OpenWindow();
        }
    }

    public void CloseTrough()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TryCloseWindow(this);
        else
            CloseWindow();
    }

    // =========================================================================
    // Auto-Feeding System
    // =========================================================================

    /// <summary>
    /// Called on each new day. Attempts to auto-feed all animals in the linked zone
    /// using food stored in this trough.
    /// </summary>
    private void OnDayChanged()
    {
        if (linkedZone == null) return;

        List<Animal> animals = linkedZone.GetAnimals();
        if (animals.Count == 0) return;

        int totalFed = 0;

        foreach (Animal animal in animals)
        {
            if (animal == null || animal.AnimalData == null) continue;

            int fedAmount = TryFeedAnimal(animal);
            if (fedAmount > 0)
            {
                animal.AutoFeed(fedAmount);
                totalFed++;
            }
        }

        UpdateTroughSprite();
        UpdateStatusText();
    }

    /// <summary>
    /// Attempt to feed a single animal from the trough's container.
    /// Returns the number of food items consumed (to pass to AutoFeed).
    /// </summary>
    private int TryFeedAnimal(Animal animal)
    {
        AnimalData data = animal.AnimalData;
        if (data.dailyFoodRequirements == null || data.dailyFoodRequirements.Count == 0)
            return 0;

        int totalFed = 0;

        foreach (FoodRequirement req in data.dailyFoodRequirements)
        {
            if (string.IsNullOrEmpty(req.itemName)) continue;

            // Look up the food item
            Item foodItem = ItemDatabase.GetItem(req.itemName);
            if (foodItem == null) continue;

            // Try to remove the required quantity from the trough
            int amountNeeded = req.quantityPerDay;
            int amountAvailable = container.GetItemCount(foodItem);
            int amountToFeed = Mathf.Min(amountNeeded, amountAvailable);

            if (amountToFeed > 0 && container.RemoveItem(foodItem, amountToFeed))
            {
                totalFed += amountToFeed;
            }
        }

        return totalFed;
    }

    /// <summary>
    /// Preview how many animals would be fed with current trough contents.
    /// Used by SleepConfirmationPanel.
    /// </summary>
    public int GetFeedableAnimalCount()
    {
        if (linkedZone == null) return 0;

        List<Animal> animals = linkedZone.GetAnimals();
        int count = 0;

        // Build temp counts using the actual item references from the container
        Dictionary<Item, int> tempCounts = new Dictionary<Item, int>();
        foreach (var stack in container.GetAllItems())
        {
            if (stack != null && !stack.IsEmpty)
            {
                if (tempCounts.ContainsKey(stack.item))
                    tempCounts[stack.item] += stack.quantity;
                else
                    tempCounts[stack.item] = stack.quantity;
            }
        }

        foreach (Animal animal in animals)
        {
            if (animal == null || animal.AnimalData == null) continue;

            AnimalData data = animal.AnimalData;
            if (data.dailyFoodRequirements == null) continue;

            bool canFeed = true;
            foreach (FoodRequirement req in data.dailyFoodRequirements)
            {
                if (string.IsNullOrEmpty(req.itemName)) continue;

                // Resolve via ItemDatabase, same as TryFeedAnimal
                Item foodItem = ItemDatabase.GetItem(req.itemName);
                if (foodItem == null) { canFeed = false; break; }

                int available = tempCounts.ContainsKey(foodItem) ? tempCounts[foodItem] : 0;
                if (available < req.quantityPerDay)
                {
                    canFeed = false;
                    break;
                }
            }

            if (canFeed)
            {
                count++;
                // Deduct from temp counts
                foreach (FoodRequirement req in data.dailyFoodRequirements)
                {
                    if (string.IsNullOrEmpty(req.itemName)) continue;
                    Item foodItem = ItemDatabase.GetItem(req.itemName);
                    if (foodItem != null && tempCounts.ContainsKey(foodItem))
                        tempCounts[foodItem] -= req.quantityPerDay;
                }
            }
        }

        return count;
    }

    /// <summary>Total number of animals in the linked zone.</summary>
    public int GetTotalAnimalCount()
    {
        return linkedZone != null ? linkedZone.GetAnimals().Count : 0;
    }

    // =========================================================================
    // Visual Feedback
    // =========================================================================

    private void UpdateTroughSprite()
    {
        if (spriteRenderer == null) return;

        int totalItems = 0;
        foreach (var stack in container.GetAllItems())
        {
            if (stack != null && !stack.IsEmpty)
                totalItems += stack.quantity;
        }

        int occupiedSlots = 0;
        foreach (var stack in container.GetAllItems())
            if (stack != null && !stack.IsEmpty) occupiedSlots++;

        if (totalItems == 0 && emptySprite != null)
            spriteRenderer.sprite = emptySprite;
        else if (occupiedSlots >= slotCount / 2 && fullSprite != null)
            spriteRenderer.sprite = fullSprite;
        else if (partialSprite != null)
            spriteRenderer.sprite = partialSprite;
    }

    private void UpdateStatusText()
    {
        if (statusText == null) return;

        int totalItems = 0;
        foreach (var stack in container.GetAllItems())
        {
            if (stack != null && !stack.IsEmpty)
                totalItems += stack.quantity;
        }

        int feedable = GetFeedableAnimalCount();
        int total = GetTotalAnimalCount();

        troughStatusText.Arguments = new object[] { totalItems, feedable, total };
        statusText.text = troughStatusText.SafeGetLocalizedString();
    }

    // =========================================================================
    // Player Movement Helpers
    // =========================================================================

    private void DisablePlayerMovement()
    {
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        if (player != null)
            player.DisableMovement();
    }

    private void EnablePlayerMovement()
    {
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        if (player != null)
            player.EnableMovement();
    }

    // =========================================================================
    // ISaveable Implementation
    // =========================================================================

    // Save version 2: the per-slot worldStrings/worldCounters loop this used to carry is gone,
    // replaced by the shared container format. See ContainerPersistence.
    //
    // Behaviour note: the old loader used AddItem, so items landed wherever they fit rather than
    // back in the slot they were saved from. The shared format restores exact indices.

    public void SaveData(GameData gameData)
    {
        ContainerPersistence.Save(gameData, container);
    }

    public void LoadData(GameData gameData)
    {
        ContainerPersistence.Load(gameData, container);

        UpdateTroughSprite();
        UpdateStatusText();
    }

    // =========================================================================
    // Public Accessors
    // =========================================================================

    /// <summary>The internal food container for direct access (used by UI slots).</summary>
    public InventoryContainer Container => container;

    /// <summary>The linked AnimalZone.</summary>
    public AnimalZone LinkedZone => linkedZone;
}

} // namespace SowurShield.Animals
