using UnityEngine;
using System;
using SowurShield.Core;
using SowurShield.Inventory;

namespace SowurShield.Animals
{

/// <summary>
/// Main animal behavior component implementing IInteractable and ISaveable.
/// Handles petting, feeding, production, and displays info UI on second pet of the day.
/// Note: Requires TWO colliders - one trigger for interaction, one non-trigger for physics.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Animal : MonoBehaviour, IInteractable, ISaveable
{
    [Header("Animal Configuration")]
    [SerializeField] private AnimalData animalData;
    [SerializeField] private GameBalance balance;

    [Header("Zone Assignment")]
    [SerializeField] private AnimalZone assignedZone;

    [Header("Collider Settings")]
    [SerializeField] private float interactionRadius = 1f;
    [SerializeField] private float physicsRadius = 0.3f;

    [Header("Runtime State")]
    private SpriteRenderer spriteRenderer;
    private Collider2D animalCollider;
    private CircleCollider2D interactionCollider;
    private CircleCollider2D physicsCollider;
    private Animator animator;

    // Cached scene references
    private SowurShield.Inventory.Inventory playerInventory;
    private AnimalInfoUI animalInfoUI;

    // Interaction tracking
    private bool hasBeenPetToday = false;
    private float lastPetTime = -999f;
    private int currentDay = 0;

    // Food tracking
    private int foodEatenToday = 0;
    private bool needsFeeding = true;

    // Production tracking
    private int lastProductionDay = -1;

    // Happiness tracking — initial value set in Start() from GameBalance
    private float happiness = 50f;

    // Particle system
    private GameObject currentHeartParticle;

    /// <summary>Event fired when this animal produces items (day, itemName, amount).</summary>
    public event Action<string, int> OnAnimalProduced;

    public AnimalData AnimalData => animalData;
    public AnimalZone AssignedZone => assignedZone;
    public bool HasBeenPetToday => hasBeenPetToday;
    public int FoodEatenToday => foodEatenToday;
    public bool NeedsFeeding => needsFeeding;
    public int LastProductionDay => lastProductionDay;
    public int CurrentDay => currentDay;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Find or create colliders
        SetupColliders();
    }

    private void SetupColliders()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();

        // We need 2 colliders: one for interaction (trigger), one for physics (non-trigger)
        interactionCollider = null;
        physicsCollider = null;

        foreach (var col in colliders)
        {
            if (col is CircleCollider2D circle)
            {
                if (circle.isTrigger)
                    interactionCollider = circle;
                else
                    physicsCollider = circle;
            }
        }

        // Create interaction collider if missing
        if (interactionCollider == null)
        {
            interactionCollider = gameObject.AddComponent<CircleCollider2D>();
            interactionCollider.isTrigger = true;
        }

        // Create physics collider if missing
        if (physicsCollider == null)
        {
            physicsCollider = gameObject.AddComponent<CircleCollider2D>();
            physicsCollider.isTrigger = false;
        }

        // Apply radius settings
        UpdateColliderRadii();

        animalCollider = interactionCollider;
    }

    private void UpdateColliderRadii()
    {
        if (interactionCollider != null)
        {
            interactionCollider.radius = interactionRadius;
        }

        if (physicsCollider != null)
        {
            physicsCollider.radius = physicsRadius;
        }
    }

    private void OnValidate()
    {
        // Update collider sizes when values change in editor
        if (Application.isPlaying && interactionCollider != null && physicsCollider != null)
        {
            UpdateColliderRadii();
        }
    }

    private void Start()
    {
        if (animalData == null)
        {
            Debug.LogError($"Animal {gameObject.name} has no AnimalData assigned!");
            return;
        }

        // Register with InteractionManager
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.RegisterInteractable(this);
        }
        else
        {
            Debug.LogWarning($"InteractionManager not found! Animal {animalData.animalName} won't be interactable.");
        }

        // Register with zone
        if (assignedZone != null)
        {
            assignedZone.RegisterAnimal(this);
        }
        else
        {
            Debug.LogWarning($"Animal {animalData.animalName} has no zone assigned!");
        }

        // Load GameBalance from Resources if not assigned in Inspector
        if (balance == null)
            balance = Resources.Load<GameBalance>("GameBalance");

        // Apply initial happiness from balance config
        happiness = balance != null ? balance.initialHappiness : 50f;

        // Cache scene references
        PlayerMove playerMove = UnityEngine.Object.FindFirstObjectByType<PlayerMove>();
        if (playerMove != null)
            playerInventory = playerMove.GetComponent<SowurShield.Inventory.Inventory>();
        animalInfoUI = UnityEngine.Object.FindFirstObjectByType<AnimalInfoUI>();

        // Set initial sprite
        if (animalData.idleSprite != null)
        {
            spriteRenderer.sprite = animalData.idleSprite;
        }

        // Set animator controller
        if (animator != null && animalData.animatorController != null)
        {
            animator.runtimeAnimatorController = animalData.animatorController;
        }

        // Subscribe to day change events
        if (GameTimeController.instance != null)
        {
            GameTimeController.instance.OnDayChanged += OnDayChanged;
            currentDay = GameTimeController.instance.currentDay;
        }

        // Register with SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
        }

        // Register with AnimalRoster
        if (AnimalRoster.Instance != null)
        {
            AnimalRoster.Instance.RegisterAnimal(this);
        }
    }

    private void OnDestroy()
    {
        // Unregister from InteractionManager
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.UnregisterInteractable(this);
        }

        // Unregister from zone
        if (assignedZone != null)
        {
            assignedZone.UnregisterAnimal(this);
        }

        // Unsubscribe from events
        if (GameTimeController.instance != null)
        {
            GameTimeController.instance.OnDayChanged -= OnDayChanged;
        }

        // Unregister from SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
        }

        // Unregister from AnimalRoster
        if (AnimalRoster.Instance != null)
        {
            AnimalRoster.Instance.UnregisterAnimal(this);
        }
    }

    #region IInteractable Implementation

    public string GetInteractionPrompt()
    {
        if (playerInventory != null)
        {
            Item selectedItem = playerInventory.GetSelectedItem();
            if (selectedItem != null && CanEatFood(selectedItem.itemName))
            {
                return $"Feed {animalData.animalName}";
            }
        }

        // Default to petting
        if (!hasBeenPetToday)
        {
            return $"Pet {animalData.animalName}";
        }
        else
        {
            return $"View {animalData.animalName} Info";
        }
    }

    public void Interact()
    {
        FacePlayer();

        if (playerInventory != null)
        {
            Item selectedItem = playerInventory.GetSelectedItem();
            if (selectedItem != null && CanEatFood(selectedItem.itemName))
            {
                FeedAnimal(selectedItem, playerInventory);
                return;
            }
        }

        PetAnimal();
    }

    public float GetInteractionRange()
    {
        return balance != null ? balance.defaultInteractionRange : 2f;
    }

    public bool CanInteract()
    {
        return true; // Animals can always be interacted with
    }

    #endregion

    #region Player Interaction Helpers

    private void FacePlayer()
    {
        if (spriteRenderer == null) return;

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Calculate direction to player
        float directionX = player.transform.position.x - transform.position.x;

        // Flip sprite to face player
        if (directionX < 0)
        {
            spriteRenderer.flipX = true; // Face left
        }
        else if (directionX > 0)
        {
            spriteRenderer.flipX = false; // Face right
        }
    }

    #endregion

    #region Petting System

    private void PetAnimal()
    {
        float timeSincePet = Time.time - lastPetTime;

        if (timeSincePet < animalData.pettingCooldown)
            return;

        lastPetTime = Time.time;

        if (!hasBeenPetToday)
        {
            hasBeenPetToday = true;
            ModifyHappiness(balance != null ? balance.petHappinessBonus : 5f);
            SpawnHeartParticle();
        }
        else
        {
            OpenAnimalInfoUI();
        }
    }

    private void SpawnHeartParticle()
    {
        if (animalData.heartParticlePrefab == null)
        {
            Debug.LogWarning($"[HeartParticle] No heart particle prefab assigned for {animalData.animalName}");
            return;
        }

        // Spawn above animal's head
        Vector3 spawnPosition = transform.position + Vector3.up * 1f;
        spawnPosition.z = 0f;
        currentHeartParticle = Instantiate(animalData.heartParticlePrefab, spawnPosition, Quaternion.identity);

        // Force sorting so particle renders above sprites
        ParticleSystemRenderer psr = currentHeartParticle.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.sortingLayerName = "Default";
            psr.sortingOrder = 9999;
        }

        // Auto-destroy after particle lifetime
        ParticleSystem ps = currentHeartParticle.GetComponent<ParticleSystem>();
        if (ps != null)
            Destroy(currentHeartParticle, ps.main.duration + ps.main.startLifetime.constantMax);
        else
            Destroy(currentHeartParticle, 2f);
    }

    private void OpenAnimalInfoUI()
    {
        if (animalInfoUI != null)
        {
            animalInfoUI.ShowAnimalInfo(this);
        }
        else
        {
            Debug.LogWarning("AnimalInfoUI not found in scene!");
        }
    }

    #endregion

    #region Feeding System

    private bool CanEatFood(string itemName)
    {
        if (animalData.dailyFoodRequirements == null || animalData.dailyFoodRequirements.Count == 0)
        {
            return false;
        }

        foreach (FoodRequirement req in animalData.dailyFoodRequirements)
        {
            if (req.itemName == itemName)
            {
                return true;
            }
        }

        return false;
    }

    private void FeedAnimal(Item food, SowurShield.Inventory.Inventory playerInventory)
    {
        // Remove food from inventory
        if (!playerInventory.RemoveItem(food, 1))
        {
            Debug.LogWarning($"Failed to remove {food.itemName} from inventory");
            return;
        }

        foodEatenToday++;
        ModifyHappiness(balance != null ? balance.feedHappinessBonus : 3f);

        // Check if daily requirements are met
        CheckFoodRequirements();

        // Trigger eating animation via AnimalAI state machine
        AnimalAI animalAI = GetComponent<AnimalAI>();
        if (animalAI != null)
        {
            animalAI.TriggerEating();
        }

        // Spawn heart particle as thanks
        SpawnHeartParticle();
    }

    private void CheckFoodRequirements()
    {
        int totalRequired = 0;
        foreach (FoodRequirement req in animalData.dailyFoodRequirements)
        {
            totalRequired += req.quantityPerDay;
        }

        needsFeeding = foodEatenToday < totalRequired;
    }

    #endregion

    #region Day Change System

    private void OnDayChanged()
    {
        // Get current day from TimeController
        if (GameTimeController.instance != null)
        {
            currentDay = GameTimeController.instance.currentDay;
        }

        // Apply happiness decay before resetting daily flags
        ApplyDailyHappinessDecay();

        // Reset daily tracking
        hasBeenPetToday = false;
        foodEatenToday = 0;
        needsFeeding = true;


        // Check for production (eggs, milk, etc.)
        if (animalData.canProduce)
        {
            CheckProduction();
        }
    }

    private void CheckProduction()
    {
        if (!animalData.canProduce) return;

        // Only produce on the correct interval day
        if (currentDay % animalData.productionIntervalDays != 0) return;

        // Skip if already produced today
        if (lastProductionDay == currentDay) return;

        // Gate production behind feeding requirement if configured
        if (animalData.produceOnlyIfFed && needsFeeding)
        {
            return;
        }

        // Calculate base amount
        int amount = UnityEngine.Random.Range(animalData.minProduceAmount, animalData.maxProduceAmount + 1);

        // Apply happiness bonus when both petted AND fed
        if (animalData.happinessProductionBonus > 0f && hasBeenPetToday && !needsFeeding)
        {
            int bonus = Mathf.RoundToInt(amount * animalData.happinessProductionBonus);
            amount += bonus;
        }

        lastProductionDay = currentDay;
        SpawnProduce(amount);
    }

    private void SpawnProduce(int amount)
    {
        if (amount <= 0) return;

        // Look up the item from the database
        Item produceItem = ItemDatabase.GetItem(animalData.produceItemName);
        if (produceItem == null)
        {
            Debug.LogWarning($"{animalData.animalName}: Item '{animalData.produceItemName}' not found in ItemDatabase!");
            return;
        }

        // Prefer designer-assigned prefab; fall back to a scene search for a GroundItem
        GameObject prefabToUse = animalData.groundItemPrefab;

        if (prefabToUse == null)
        {
            Debug.LogWarning($"{animalData.animalName}: No groundItemPrefab assigned in AnimalData. Cannot spawn produce.");
            return;
        }

        // Spawn slightly above the animal so it pops out visually
        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        GameObject spawnedObj = Instantiate(prefabToUse, spawnPos, Quaternion.identity);

        GroundItem groundItem = spawnedObj.GetComponent<GroundItem>();
        if (groundItem != null)
        {
            groundItem.SetItem(produceItem, amount);
        }
        else
        {
            Debug.LogWarning($"{animalData.animalName}: Spawned prefab has no GroundItem component!");
        }

        OnAnimalProduced?.Invoke(animalData.produceItemName, amount);
    }

    #endregion

    #region ISaveable Implementation

    public void SaveData(GameData gameData)
    {
        if (gameData?.worldData == null) return;

        string prefix = $"animal_{gameObject.name}";
        gameData.worldData.worldFlags[$"{prefix}_petted"] = hasBeenPetToday;
        gameData.worldData.worldCounters[$"{prefix}_foodEaten"] = foodEatenToday;
        gameData.worldData.worldCounters[$"{prefix}_lastProductionDay"] = lastProductionDay;
        gameData.worldData.worldCounters[$"{prefix}_happiness"] = Mathf.RoundToInt(happiness);
    }

    public void LoadData(GameData gameData)
    {
        if (gameData?.worldData == null) return;

        string prefix = $"animal_{gameObject.name}";

        if (gameData.worldData.worldFlags.TryGetValue($"{prefix}_petted", out bool petted))
            hasBeenPetToday = petted;

        if (gameData.worldData.worldCounters.TryGetValue($"{prefix}_foodEaten", out int eaten))
        {
            foodEatenToday = eaten;
            CheckFoodRequirements();
        }

        if (gameData.worldData.worldCounters.TryGetValue($"{prefix}_lastProductionDay", out int prodDay))
            lastProductionDay = prodDay;

        if (gameData.worldData.worldCounters.TryGetValue($"{prefix}_happiness", out int savedHappiness))
        {
            float ceil = balance != null ? balance.happinessCeiling : 100f;
            happiness = Mathf.Clamp(savedHappiness, 0f, ceil);
        }
    }

    #endregion

    #region Happiness System

    /// <summary>Current happiness value (0-100, starts at 50).</summary>
    public float GetHappiness() => happiness;

    /// <summary>
    /// Happiness multiplier applied to stat calculations (0.5x at 0 happiness, 1.5x at 100).
    /// </summary>
    public float GetHappinessMultiplier()
    {
        float min = balance != null ? balance.happinessMultiplierMin : 0.5f;
        float max = balance != null ? balance.happinessMultiplierMax : 1.5f;
        float ceil = balance != null ? balance.happinessCeiling : 100f;
        return min + (happiness / ceil) * (max - min);
    }

    /// <summary>Adjust happiness by amount, clamped to 0-100.</summary>
    public void ModifyHappiness(float amount)
    {
        float ceil = balance != null ? balance.happinessCeiling : 100f;
        happiness = Mathf.Clamp(happiness + amount, 0f, ceil);
    }

    /// <summary>
    /// Apply daily happiness decay. Called at the start of each new day BEFORE resetting daily flags.
    /// Not petted yesterday: -0.5 happiness. Not fed yesterday: -1.0 happiness.
    /// Minimum happiness is 20 (prevents total sadness spiral).
    /// </summary>
    private void ApplyDailyHappinessDecay()
    {
        float decayNoPet  = balance != null ? balance.dailyDecayNoPet  : 0.5f;
        float decayNoFeed = balance != null ? balance.dailyDecayNoFeed : 1.0f;
        float floor       = balance != null ? balance.happinessFloor   : 20f;

        float decay = 0f;

        if (!hasBeenPetToday)
            decay -= decayNoPet;

        if (needsFeeding)
            decay -= decayNoFeed;

        if (decay < 0f)
            happiness = Mathf.Max(floor, happiness + decay);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get the percentage of daily food requirements met
    /// </summary>
    public float GetFoodPercentage()
    {
        if (animalData.dailyFoodRequirements == null || animalData.dailyFoodRequirements.Count == 0)
        {
            return 1f;
        }

        int totalRequired = 0;
        foreach (FoodRequirement req in animalData.dailyFoodRequirements)
        {
            totalRequired += req.quantityPerDay;
        }

        if (totalRequired == 0) return 1f;

        return Mathf.Clamp01((float)foodEatenToday / totalRequired);
    }

    /// <summary>
    /// Auto-feed the animal from a FeedingTrough (bypasses inventory removal).
    /// Sets foodEatenToday directly and triggers eating animation.
    /// </summary>
    public void AutoFeed(int amount)
    {
        if (amount <= 0) return;

        foodEatenToday += amount;
        float bonusPerUnit = balance != null ? balance.autoFeedHappinessBonusPerUnit : 3f;
        ModifyHappiness(bonusPerUnit * amount);
        CheckFoodRequirements();

        // Trigger eating animation
        AnimalAI animalAI = GetComponent<AnimalAI>();
        if (animalAI != null)
        {
            animalAI.TriggerEating();
        }

    }

    /// <summary>
    /// Set the animal's zone (useful for runtime assignment)
    /// </summary>
    public void SetZone(AnimalZone zone)
    {
        if (assignedZone != null)
        {
            assignedZone.UnregisterAnimal(this);
        }

        assignedZone = zone;

        if (assignedZone != null)
        {
            assignedZone.RegisterAnimal(this);
        }
    }

    /// <summary>Get the display name (custom name or data name).</summary>
    public string GetDisplayName()
    {
        return animalData != null ? animalData.animalName : gameObject.name;
    }

    /// <summary>
    /// Get combat stats for this animal.
    /// Creates a new AnimalCombatStats instance with current happiness and animal data.
    /// </summary>
    public AnimalCombatStats GetCombatStats()
    {
        if (animalData == null || animalData.baseCombatStats == null)
        {
            Debug.LogWarning($"[Animal] {gameObject.name} has no combat stats configured!");
            return null;
        }

        // Create a copy of the base combat stats
        AnimalCombatStats stats = new AnimalCombatStats();
        stats.Initialize(animalData.baseCombatStats);

        // Apply current happiness from farming system
        stats.happiness = happiness;
        stats.currentHealth = stats.MaxHealth;

        return stats;
    }

    #endregion
}

} // namespace SowurShield.Animals
