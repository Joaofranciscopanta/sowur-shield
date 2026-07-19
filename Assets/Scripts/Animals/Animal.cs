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

    // Persistent combat stats (growth persists across sessions)
    private AnimalCombatStats combatStats;

    // Custom name set by player
    private string customName = "";

    // True for animals spawned at runtime by AnimalMarketUI (vs. hand-placed in the scene).
    // Hand-placed animals already exist in every scene load; purchased ones must be recorded
    // in GameData.purchasedAnimals so AnimalPurchaseLoader can re-instantiate them.
    private bool isPurchased = false;

    // Illness tracking
    private bool isIll = false;
    private int neglectDays = 0;

    /// <summary>True when the animal is ill (production blocked, stats penalised).</summary>
    public bool IsIll => isIll;

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

    private System.Collections.IEnumerator RegisterWithInteractionManagerWhenReady()
    {
        const float timeout = 5f;
        float elapsed = 0f;
        while (InteractionManager.Instance == null && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }

        if (InteractionManager.Instance != null)
            InteractionManager.Instance.RegisterInteractable(this);
        else
            Debug.LogWarning($"InteractionManager not found after {timeout}s! Animal {animalData.animalName} won't be interactable.");
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

        // Initialize persistent combat stats from base data
        InitializeCombatStats();

        // Register with InteractionManager (retry for a few seconds — scene-load order
        // can run this Start before the InteractionManager singleton exists)
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.RegisterInteractable(this);
        }
        else
        {
            StartCoroutine(RegisterWithInteractionManagerWhenReady());
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

        // Set initial sprite — prefer prefab value; fall back to AnimalData only if blank
        if (spriteRenderer.sprite == null && animalData.idleSprite != null)
        {
            spriteRenderer.sprite = animalData.idleSprite;
        }

        // Set animator controller — prefer prefab value; fall back to AnimalData only if blank
        if (animator != null && animator.runtimeAnimatorController == null && animalData.animatorController != null)
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

        // A sold/despawned purchased animal must not be re-instantiated on the next
        // load or scene reload — drop its entry from the current in-memory save data.
        // Guard on gameObject.scene.isLoaded: OnDestroy() also fires for every object
        // when the scene is torn down (e.g. loading CombatScene), and that must NOT be
        // treated as a sale — it wiped every purchased animal's record on the very same
        // frame TeamAssemblerUI had just captured it, before this fix.
        if (isPurchased && gameObject.scene.isLoaded && SaveManager.Instance?.CurrentGameData?.worldData != null)
        {
            SaveManager.Instance.CurrentGameData.worldData.purchasedAnimals
                .RemoveAll(p => p.gameObjectName == gameObject.name);
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
        if (isIll)
        {
            if (playerInventory != null)
            {
                Item selectedItem = playerInventory.GetSelectedItem();
                string cureItem = animalData?.illnessCureItemName ?? "Medicine";
                if (selectedItem != null && selectedItem.itemName == cureItem)
                    return $"Cure {GetDisplayName()} (use {cureItem})";
            }
            return $"{GetDisplayName()} is ill! (use {animalData?.illnessCureItemName ?? "Medicine"} to cure)";
        }

        if (playerInventory != null)
        {
            Item selectedItem = playerInventory.GetSelectedItem();
            if (selectedItem != null && CanEatFood(selectedItem.itemName))
                return $"Feed {GetDisplayName()}";
        }

        return !hasBeenPetToday ? $"Pet {GetDisplayName()}" : $"View {GetDisplayName()} Info";
    }

    public void Interact()
    {
        FacePlayer();

        if (playerInventory != null)
        {
            Item selectedItem = playerInventory.GetSelectedItem();
            if (selectedItem != null)
            {
                // Medicine cures illness (checked first so medicine isn't accidentally fed)
                string cureItem = animalData?.illnessCureItemName ?? "Medicine";
                if (isIll && selectedItem.itemName == cureItem)
                {
                    CureIllness(playerInventory);
                    return;
                }

                if (CanEatFood(selectedItem.itemName))
                {
                    FeedAnimal(selectedItem, playerInventory);
                    return;
                }
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
            SowurShield.Core.SFXManager.Play("PetAnimal");
            SowurShield.Core.TutorialManager.NotifyStepComplete("pet_animal");
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

        // Apply combat stat growth from daily care (before resetting flags)
        ApplyDailyCareGrowth();

        // Apply seasonal combat modifiers
        ApplySeasonalModifiers();

        // Track neglect and progress illness — evaluated BEFORE resetting daily flags
        UpdateNeglectAndIllness();

        // Reset daily tracking
        hasBeenPetToday = false;
        foodEatenToday = 0;
        needsFeeding = true;

        // Check for production — blocked when ill
        if (animalData.canProduce && !isIll)
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

        // Purchased animals don't exist in any scene file — record them so they can be
        // re-instantiated on load/scene-reload, before the per-attribute data below is read.
        if (isPurchased && animalData != null)
        {
            var existing = gameData.worldData.purchasedAnimals.Find(p => p.gameObjectName == gameObject.name);
            if (existing == null)
            {
                gameData.worldData.purchasedAnimals.Add(new WorldGameData.PurchasedAnimalData
                {
                    gameObjectName = gameObject.name,
                    animalDataName = animalData.animalName,
                    zoneName = assignedZone != null ? assignedZone.gameObject.name : ""
                });
            }
        }

        string prefix = $"animal_{gameObject.name}";
        gameData.worldData.worldFlags[$"{prefix}_petted"] = hasBeenPetToday;
        gameData.worldData.worldCounters[$"{prefix}_foodEaten"] = foodEatenToday;
        gameData.worldData.worldCounters[$"{prefix}_lastProductionDay"] = lastProductionDay;
        gameData.worldData.worldCounters[$"{prefix}_happiness"] = Mathf.RoundToInt(happiness);

        // Save growth multipliers (float * 1000 → int for precision)
        if (combatStats != null)
        {
            gameData.worldData.worldCounters[$"{prefix}_attackGrowth"] = Mathf.RoundToInt(combatStats.attackGrowth * 1000f);
            gameData.worldData.worldCounters[$"{prefix}_defenseGrowth"] = Mathf.RoundToInt(combatStats.defenseGrowth * 1000f);
            gameData.worldData.worldCounters[$"{prefix}_speedGrowth"] = Mathf.RoundToInt(combatStats.speedGrowth * 1000f);
            gameData.worldData.worldCounters[$"{prefix}_healthGrowth"] = Mathf.RoundToInt(combatStats.healthGrowth * 1000f);
            gameData.worldData.worldCounters[$"{prefix}_level"] = combatStats.level;
            gameData.worldData.worldCounters[$"{prefix}_experience"] = Mathf.RoundToInt(combatStats.experience);
        }

        // Save illness state
        gameData.worldData.worldFlags[$"{prefix}_isIll"] = isIll;
        gameData.worldData.worldCounters[$"{prefix}_neglectDays"] = neglectDays;

        // Save custom name (only if set)
        if (!string.IsNullOrEmpty(customName))
            gameData.worldData.worldStrings[$"{prefix}_customName"] = customName;
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

        // Load growth multipliers (int / 1000 → float)
        if (combatStats == null) InitializeCombatStats();
        if (gameData.worldData.worldCounters.TryGetValue($"{prefix}_attackGrowth", out int atkG))
            combatStats.attackGrowth = Mathf.Clamp(atkG / 1000f, 1f, 3f);
        if (gameData.worldData.worldCounters.TryGetValue($"{prefix}_defenseGrowth", out int defG))
            combatStats.defenseGrowth = Mathf.Clamp(defG / 1000f, 1f, 3f);
        if (gameData.worldData.worldCounters.TryGetValue($"{prefix}_speedGrowth", out int spdG))
            combatStats.speedGrowth = Mathf.Clamp(spdG / 1000f, 1f, 3f);
        if (gameData.worldData.worldCounters.TryGetValue($"{prefix}_healthGrowth", out int hpG))
            combatStats.healthGrowth = Mathf.Clamp(hpG / 1000f, 1f, 3f);
        if (gameData.worldData.worldCounters.TryGetValue($"{prefix}_level", out int savedLevel))
            combatStats.level = Mathf.Clamp(savedLevel, 1, 10);
        if (gameData.worldData.worldCounters.TryGetValue($"{prefix}_experience", out int savedXP))
            combatStats.experience = Mathf.Max(0f, savedXP);

        // Load illness state
        if (gameData.worldData.worldFlags.TryGetValue($"{prefix}_isIll", out bool savedIll))
            isIll = savedIll;
        if (gameData.worldData.worldCounters.TryGetValue($"{prefix}_neglectDays", out int savedNeglect))
            neglectDays = Mathf.Max(0, savedNeglect);

        // Load custom name
        if (gameData.worldData.worldStrings.TryGetValue($"{prefix}_customName", out string savedName))
            customName = savedName ?? "";
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
    /// Assigns the AnimalData and zone for an animal spawned at runtime (e.g. by AnimalMarketUI
    /// after a purchase). Must be called immediately after AddComponent&lt;Animal&gt;(), before
    /// Start() runs — Start() is the first place animalData/assignedZone are read.
    /// </summary>
    public void InitializeFromMarket(AnimalData data, AnimalZone zone)
    {
        animalData = data;
        assignedZone = zone;
        isPurchased = true;
    }

    /// <summary>
    /// Builds a GameObject name guaranteed unique among currently-loaded GameObjects,
    /// starting from baseName. Used for purchased animals so buying two of the same
    /// species doesn't collide on the "animal_{gameObject.name}" save-data prefix (which
    /// also doubles as the ISaveable identity key).
    /// </summary>
    public static string GenerateUniquePurchasedName(string baseName)
    {
        string candidate = baseName;
        int suffix = 1;
        while (GameObject.Find(candidate) != null)
        {
            candidate = $"{baseName}_{suffix}";
            suffix++;
        }
        return candidate;
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

    /// <summary>Get the display name (custom name first, then data name, then GO name).</summary>
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(customName))
            return customName;
        return animalData != null ? animalData.GetDisplayName() : gameObject.name;
    }

    /// <summary>Set a custom name for this animal (max 20 chars).</summary>
    public void SetCustomName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            customName = "";
            return;
        }
        customName = name.Trim();
        if (customName.Length > 20)
            customName = customName.Substring(0, 20);
    }

    /// <summary>Get the custom name (empty string if not set).</summary>
    public string GetCustomName() => customName;

    /// <summary>
    /// Get the persistent combat stats for this animal.
    /// Syncs happiness before returning.
    /// </summary>
    public AnimalCombatStats GetCombatStats()
    {
        if (combatStats == null)
            InitializeCombatStats();

        // Sync happiness from the farming side to combat stats
        combatStats.happiness = happiness;
        return combatStats;
    }

    #endregion

    #region Combat Stats & Growth

    /// <summary>Initialize combat stats from AnimalData base stats.</summary>
    private void InitializeCombatStats()
    {
        combatStats = new AnimalCombatStats();
        if (animalData != null && animalData.baseCombatStats != null)
        {
            combatStats.Initialize(animalData.baseCombatStats);
        }
        combatStats.happiness = happiness;
    }

    /// <summary>
    /// Apply stat growth from daily care activities.
    /// Called at the start of each new day BEFORE resetting daily flags.
    /// Petting: +pettingStatBonus to a random stat.
    /// Full feeding: +feedingStatBonus to a random stat.
    /// </summary>
    private void ApplyDailyCareGrowth()
    {
        if (combatStats == null || animalData == null) return;

        string[] stats = { "attack", "defense", "speed", "health" };

        // Petting bonus
        if (hasBeenPetToday)
        {
            string stat = stats[UnityEngine.Random.Range(0, stats.Length)];
            combatStats.ApplyStatGrowth(stat, animalData.pettingStatBonus);
        }

        // Feeding bonus (fully fed)
        if (!needsFeeding)
        {
            string stat = stats[UnityEngine.Random.Range(0, stats.Length)];
            combatStats.ApplyStatGrowth(stat, animalData.feedingStatBonus);
        }
    }

    /// <summary>
    /// Grant combat XP to this animal and handle level-ups.
    /// Called by BattleResultsUI.AwardRewards() for each surviving player unit.
    /// Level-up formula: XP needed = current level * 100 (e.g. level 1→2 = 100 XP).
    /// Max level: 10. On level-up: +5% boost distributed to all four growth stats.
    /// </summary>
    public void GainCombatExperience(float xp)
    {
        if (combatStats == null) InitializeCombatStats();
        if (combatStats.level >= 10) return;

        combatStats.experience += xp;

        // Check for level-ups (may gain multiple levels from one large XP grant)
        while (combatStats.level < 10)
        {
            float xpNeeded = combatStats.level * 100f;
            if (combatStats.experience < xpNeeded) break;

            combatStats.experience -= xpNeeded;
            combatStats.level++;

            // Apply a flat +5% boost to all growth stats, capped at 3×
            combatStats.ApplyStatGrowth("all", 0.05f);
        }
    }

    /// <summary>Returns the animal's current combat level (1-10).</summary>
    public int GetCombatLevel() => combatStats != null ? combatStats.level : 1;

    /// <summary>Returns current XP progress toward the next level.</summary>
    public float GetCombatExperience() => combatStats != null ? combatStats.experience : 0f;

    #endregion

    #region Illness System

    /// <summary>
    /// Called once per day (before daily flags reset) to update neglect counter and
    /// transition the animal into the ill state if fully neglected too many days in a row.
    /// Neglect = not petted AND not fed on the same day.
    /// Any care action (petting OR feeding) resets the neglect counter.
    /// </summary>
    private void UpdateNeglectAndIllness()
    {
        if (animalData == null) return;

        bool neglectedToday = !hasBeenPetToday && needsFeeding;

        if (neglectedToday)
        {
            neglectDays++;
            if (!isIll && neglectDays >= animalData.illnessThresholdDays)
                isIll = true;
        }
        else
        {
            // Animal was cared for — reset neglect streak (illness still requires medicine)
            neglectDays = 0;
        }
    }

    /// <summary>
    /// Cure the animal's illness by consuming one medicine item from the player's inventory.
    /// </summary>
    private void CureIllness(SowurShield.Inventory.Inventory inventory)
    {
        if (!isIll || inventory == null || animalData == null) return;

        Item medicine = ItemDatabase.GetItem(animalData.illnessCureItemName);
        if (medicine == null)
        {
            Debug.LogWarning($"[Animal] Cure item '{animalData.illnessCureItemName}' not found in ItemDatabase.");
            return;
        }

        if (!inventory.RemoveItem(medicine, 1)) return;

        isIll = false;
        neglectDays = 0;
        ModifyHappiness(10f); // Recovery happiness boost
        SpawnHeartParticle();
    }

    #endregion

    #region Combat Stats (illness penalty integration)

    /// <summary>
    /// Apply seasonal combat modifiers based on current season.
    /// Animals in their preferred season get bonus stats.
    /// </summary>
    private void ApplySeasonalModifiers()
    {
        if (combatStats == null || animalData == null) return;

        string currentSeason = "";
        if (GameTimeController.instance != null)
            currentSeason = GameTimeController.instance.GetCurrentSeason();

        if (!string.IsNullOrEmpty(animalData.preferredSeason) &&
            currentSeason.Equals(animalData.preferredSeason, StringComparison.OrdinalIgnoreCase))
        {
            combatStats.ApplySeasonalModifiers(
                animalData.seasonalAttackBonus,
                animalData.seasonalDefenseBonus,
                animalData.seasonalSpeedBonus
            );
        }
        else
        {
            combatStats.ResetSeasonalModifiers();
        }
    }

    #endregion
}

} // namespace SowurShield.Animals
