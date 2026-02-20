using UnityEngine;
using System;

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

    // Interaction tracking
    private bool hasBeenPetToday = false;
    private float lastPetTime = -999f;
    private int currentDay = 0;

    // Food tracking
    private int foodEatenToday = 0;
    private bool needsFeeding = true;

    // Production tracking
    private int lastProductionDay = -1;

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
            Debug.Log($"Registered {animalData.animalName} with InteractionManager");
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
    }

    #region IInteractable Implementation

    public string GetInteractionPrompt()
    {
        // Check if player has valid food in hand
        Inventory playerInventory = FindObjectOfType<PlayerMove>()?.GetComponent<Inventory>();
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
        Debug.Log($"[Animal] Interact() called on {animalData?.animalName ?? gameObject.name}");

        // Face the player
        FacePlayer();

        // Check if player has food
        Inventory playerInventory = FindObjectOfType<PlayerMove>()?.GetComponent<Inventory>();
        if (playerInventory != null)
        {
            Item selectedItem = playerInventory.GetSelectedItem();
            Debug.Log($"[Animal] Player has item: {selectedItem?.itemName ?? "null"}");

            if (selectedItem != null && CanEatFood(selectedItem.itemName))
            {
                Debug.Log($"[Animal] Feeding {animalData.animalName} with {selectedItem.itemName}");
                FeedAnimal(selectedItem, playerInventory);
                return;
            }
        }

        // Otherwise, pet the animal
        Debug.Log($"[Animal] Petting {animalData.animalName}. HasBeenPetToday: {hasBeenPetToday}");
        PetAnimal();
    }

    public float GetInteractionRange()
    {
        return 2f; // Standard interaction range
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

        Debug.Log($"[Animal] PetAnimal - Time since last pet: {timeSincePet}s, Cooldown: {animalData.pettingCooldown}s");

        // Check cooldown
        if (timeSincePet < animalData.pettingCooldown)
        {
            Debug.Log($"[Animal] {animalData.animalName} was recently petted. Wait {animalData.pettingCooldown - timeSincePet:F1}s");
            return;
        }

        lastPetTime = Time.time;

        // First pet of the day - show heart particle
        if (!hasBeenPetToday)
        {
            hasBeenPetToday = true;
            Debug.Log($"[Animal] First pet of the day! Spawning heart particle...");
            SpawnHeartParticle();
            Debug.Log($"[Animal] Petted {animalData.animalName} for the first time today!");
        }
        // Second pet - open info UI
        else
        {
            Debug.Log($"[Animal] Second pet - opening info UI...");
            OpenAnimalInfoUI();
        }
    }

    private void SpawnHeartParticle()
    {
        if (animalData.heartParticlePrefab == null)
        {
            Debug.LogWarning($"No heart particle prefab assigned for {animalData.animalName}");
            return;
        }

        // Spawn above animal's head
        Vector3 spawnPosition = transform.position + Vector3.up * 1f;
        currentHeartParticle = Instantiate(animalData.heartParticlePrefab, spawnPosition, Quaternion.identity);

        // Auto-destroy after particle lifetime
        ParticleSystem ps = currentHeartParticle.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            Destroy(currentHeartParticle, ps.main.duration);
        }
        else
        {
            Destroy(currentHeartParticle, 2f);
        }
    }

    private void OpenAnimalInfoUI()
    {
        AnimalInfoUI infoUI = FindObjectOfType<AnimalInfoUI>();
        if (infoUI != null)
        {
            infoUI.ShowAnimalInfo(this);
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

    private void FeedAnimal(Item food, Inventory playerInventory)
    {
        // Remove food from inventory
        if (!playerInventory.RemoveItem(food, 1))
        {
            Debug.LogWarning($"Failed to remove {food.itemName} from inventory");
            return;
        }

        foodEatenToday++;
        Debug.Log($"{animalData.animalName} ate {food.itemName}! Total food today: {foodEatenToday}");

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

        if (!needsFeeding)
        {
            Debug.Log($"{animalData.animalName} has eaten enough food for today!");
        }
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

        // Reset daily tracking
        hasBeenPetToday = false;
        foodEatenToday = 0;
        needsFeeding = true;

        Debug.Log($"{animalData.animalName} - New day {currentDay}. Daily stats reset.");

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
            Debug.Log($"{animalData.animalName} skipped production — not fed today.");
            return;
        }

        // Calculate base amount
        int amount = UnityEngine.Random.Range(animalData.minProduceAmount, animalData.maxProduceAmount + 1);

        // Apply happiness bonus when both petted AND fed
        if (animalData.happinessProductionBonus > 0f && hasBeenPetToday && !needsFeeding)
        {
            int bonus = Mathf.RoundToInt(amount * animalData.happinessProductionBonus);
            amount += bonus;
            Debug.Log($"{animalData.animalName} happiness bonus! +{bonus} extra produce.");
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

        Debug.Log($"{animalData.animalName} produced {amount}x {animalData.produceItemName}!");
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

    #endregion
}
