using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Main animal behavior component implementing IInteractable and ISaveable.
/// Handles petting, feeding, displays info UI, and persists combat stats.
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

    // Combat stats tracking
    [Header("Combat Integration")]
    [SerializeField] private AnimalCombatStats combatStats = new AnimalCombatStats();
    private string uniqueID;

    // Custom naming
    [Header("Custom Name")]
    [SerializeField] private string customName = ""; // Player-given name

    // Stat growth tracking
    private int timesPetted = 0;
    private int timesFedProperly = 0;
    private int highQualityProductsCreated = 0;

    public AnimalData AnimalData => animalData;
    public AnimalZone AssignedZone => assignedZone;
    public bool HasBeenPetToday => hasBeenPetToday;
    public int FoodEatenToday => foodEatenToday;
    public bool NeedsFeeding => needsFeeding;
    public int CurrentDay => currentDay;
    public int LastProductionDay => lastProductionDay;
    // Fired when the animal produces (itemName, amount)
    public event Action<string, int> OnAnimalProduced;

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
            return;
        }

        // Initialize unique ID for save system
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = System.Guid.NewGuid().ToString();
        }

        // Initialize combat stats from base data
        combatStats.Initialize(animalData.baseCombatStats);

        // Set active skill from AnimalData
        if (animalData.activeSkill != null)
        {
            combatStats.SetActiveSkill(animalData.activeSkill.skillId);
        }

        // Check for unlockable passive skills
        CheckAndUnlockSkills();

        // Register with InteractionManager
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.RegisterInteractable(this);
        }
        else
        {
        }

        // Register with zone
        if (assignedZone != null)
        {
            assignedZone.RegisterAnimal(this);
        }
        else
        {
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

        // Apply seasonal modifiers if available
        UpdateSeasonalModifiers();

        // Register with SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
        }
        else
        {
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
        string displayName = GetDisplayName();

        // Check if player has valid food in hand
        Inventory playerInventory = FindObjectOfType<PlayerMove>()?.GetComponent<Inventory>();
        if (playerInventory != null)
        {
            Item selectedItem = playerInventory.GetSelectedItem();
            if (selectedItem != null && CanEatFood(selectedItem.itemName))
            {
                return $"Feed {displayName}";
            }
        }

        // Default to petting
        if (!hasBeenPetToday)
        {
            return $"Pet {displayName}";
        }
        else
        {
            return $"View {displayName} Info";
        }
    }

    public void Interact()
    {

        // Face the player
        FacePlayer();

        // Check if player has food
        Inventory playerInventory = FindObjectOfType<PlayerMove>()?.GetComponent<Inventory>();
        if (playerInventory != null)
        {
            Item selectedItem = playerInventory.GetSelectedItem();

            if (selectedItem != null && CanEatFood(selectedItem.itemName))
            {
                FeedAnimal(selectedItem, playerInventory);
                return;
            }
        }

        // Otherwise, pet the animal
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


        // Check cooldown
        if (timeSincePet < animalData.pettingCooldown)
        {
            return;
        }

        lastPetTime = Time.time;

        // First pet of the day - show heart particle
        if (!hasBeenPetToday)
        {
            hasBeenPetToday = true;
            SpawnHeartParticle();

            // Apply stat growth and happiness bonus
            timesPetted++;
            combatStats.ModifyHappiness(5f); // +5 happiness per pet
            ApplyStatGrowth("all", animalData.pettingStatBonus);

            // Check for new skill unlocks after happiness change
            CheckAndUnlockSkills();
        }
        // Second pet - open info UI
        else
        {
            OpenAnimalInfoUI();
        }
    }

    private void SpawnHeartParticle()
    {
        if (animalData.heartParticlePrefab == null)
        {
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
            return;
        }

        foodEatenToday++;

        // Check if food is "perfect" (what the animal wants)
        bool isPerfect = IsPerfectFood(food);

        if (isPerfect)
        {
            timesFedProperly++;
            combatStats.ModifyHappiness(10f); // +10 happiness for proper food
            ApplyStatGrowth("attack", animalData.feedingStatBonus); // Boost attack primarily
            ApplyStatGrowth("defense", animalData.feedingStatBonus * 0.5f); // Smaller defense boost
        }
        else
        {
            combatStats.ModifyHappiness(3f); // +3 happiness for any food
        }

        // Check if daily requirements are met
        CheckFoodRequirements();

        // Play eating animation if available
        if (animator != null)
        {
            animator.SetTrigger("Eat");
        }

        // Spawn heart particle as thanks
        SpawnHeartParticle();


        // Check for new skill unlocks after happiness change
        CheckAndUnlockSkills();
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

        // Apply happiness decay based on care received
        combatStats.ApplyDailyHappinessDecay(hasBeenPetToday, !needsFeeding);

        // Reset daily tracking
        hasBeenPetToday = false;
        foodEatenToday = 0;
        needsFeeding = true;

        // Check for production (eggs, milk, etc.)
        if (animalData.canProduce)
        {
            CheckProduction();
        }

        // Update seasonal modifiers (in case season changed)
        UpdateSeasonalModifiers();

        // Check for skill unlocks (season or happiness might have changed)
        CheckAndUnlockSkills();

    }

    private void CheckProduction()
    {
        // Check if this animal can produce and if it's time to produce
        if (!animalData.canProduce) return;
        if (currentDay % animalData.productionIntervalDays != 0) return;
        if (lastProductionDay == currentDay) return; // Already produced today

        lastProductionDay = currentDay;

        // Calculate production quality based on happiness, feeding, season
        int quality = CalculateProductQuality();
        int amount = UnityEngine.Random.Range(animalData.minProduceAmount, animalData.maxProduceAmount + 1);


        // Apply stat bonuses for high-quality production
        if (quality >= 4) // 4-5 star quality
        {
            highQualityProductsCreated++;

            // High quality boosts defense and health
            ApplyStatGrowth("defense", animalData.productionQualityBonus);
            ApplyStatGrowth("health", animalData.productionQualityBonus * 0.5f);


            // Check for new skill unlocks after production milestone
            CheckAndUnlockSkills();
        }

        // TODO: Spawn actual items in world (ProductionPoint system - Phase 5)
        // For now, just log the production
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

    // ============================================================================
    // COMBAT STATS API
    // ============================================================================

    /// <summary>
    /// Get combat stats for this animal
    /// </summary>
    public AnimalCombatStats GetCombatStats()
    {
        return combatStats;
    }

    /// <summary>
    /// Get current happiness value (0-100)
    /// </summary>
    public float GetHappiness()
    {
        return combatStats.happiness;
    }

    /// <summary>
    /// Get unique ID for save system
    /// </summary>
    public string GetUniqueID()
    {
        return uniqueID;
    }

    /// <summary>
    /// Set unique ID (used when loading from save)
    /// </summary>
    public void SetUniqueID(string id)
    {
        uniqueID = id;
    }

    /// <summary>
    /// Get stat growth tracking data
    /// </summary>
    public (int petted, int fed, int quality) GetGrowthTracking()
    {
        return (timesPetted, timesFedProperly, highQualityProductsCreated);
    }

    /// <summary>
    /// Get the display name (custom name if set, otherwise breed name)
    /// </summary>
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(customName))
        {
            return customName;
        }

        return animalData != null ? animalData.animalName : "Unknown Animal";
    }

    /// <summary>
    /// Set the custom name for this animal
    /// </summary>
    public void SetCustomName(string newName)
    {
        customName = newName;
    }

    /// <summary>
    /// Get the breed/type name (from AnimalData)
    /// </summary>
    public string GetBreedName()
    {
        return animalData != null ? animalData.animalName : "Unknown";
    }

    /// <summary>
    /// Check if animal has a custom name
    /// </summary>
    public bool HasCustomName()
    {
        return !string.IsNullOrEmpty(customName);
    }

    #endregion

    #region Combat Stat Growth Methods

    /// <summary>
    /// Apply stat growth to combat stats
    /// </summary>
    private void ApplyStatGrowth(string statName, float growthAmount)
    {
        combatStats.ApplyStatGrowth(statName, growthAmount);
    }

    /// <summary>
    /// Update seasonal combat modifiers based on current season
    /// </summary>
    private void UpdateSeasonalModifiers()
    {
        if (GameTimeController.instance == null || animalData == null) return;

        string currentSeason = GameTimeController.instance.GetCurrentSeason();

        if (currentSeason == animalData.preferredSeason)
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

    /// <summary>
    /// Calculate production quality based on happiness and feeding
    /// </summary>
    private int CalculateProductQuality()
    {
        // Quality factors:
        // - Happiness: 40%
        // - Fed today: 30%
        // - Seasonal match: 20%
        // - Random: 10%

        float qualityScore = 0f;

        // Happiness contribution (0-40)
        qualityScore += (combatStats.happiness / 100f) * 40f;

        // Fed contribution (0-30)
        if (!needsFeeding)
        {
            qualityScore += 30f;
        }

        // Seasonal contribution (0-20)
        if (GameTimeController.instance != null)
        {
            string currentSeason = GameTimeController.instance.GetCurrentSeason();
            if (currentSeason == animalData.preferredSeason)
            {
                qualityScore += 20f;
            }
        }

        // Random contribution (0-10)
        qualityScore += UnityEngine.Random.Range(0f, 10f);

        // Convert to 1-5 star quality
        if (qualityScore >= 90f) return 5;
        if (qualityScore >= 75f) return 4;
        if (qualityScore >= 55f) return 3;
        if (qualityScore >= 35f) return 2;
        return 1;
    }

    /// <summary>
    /// Check if food is "perfect" (exactly what animal wants)
    /// </summary>
    private bool IsPerfectFood(Item food)
    {
        if (animalData.dailyFoodRequirements == null) return false;

        foreach (FoodRequirement req in animalData.dailyFoodRequirements)
        {
            if (req.itemName == food.itemName)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Skill Management

    /// <summary>
    /// Check if any passive skills can be unlocked based on current conditions
    /// </summary>
    private void CheckAndUnlockSkills()
    {
        if (animalData == null || animalData.availablePassiveSkills == null) return;
        if (combatStats.passiveSkillIds.Count >= 4) return; // Max passive skills reached

        // Get current conditions
        int familyCount = GetFamilyCountInRoster();
        string currentSeason = GameTimeController.instance != null
            ? GameTimeController.instance.GetCurrentSeason()
            : "Spring";

        // Check each available passive skill
        foreach (AnimalSkill skill in animalData.availablePassiveSkills)
        {
            if (skill == null) continue;

            // Skip if already unlocked
            if (combatStats.HasPassiveSkill(skill.skillId)) continue;

            // Check if unlock conditions are met
            if (skill.CanUnlock(this, familyCount, currentSeason))
            {
                if (combatStats.UnlockPassiveSkill(skill.skillId))
                {
                }
            }
        }
    }

    /// <summary>
    /// Get count of animals with same family in roster (stub for now - will be implemented with AnimalRoster)
    /// </summary>
    private int GetFamilyCountInRoster()
    {
        // TODO: Implement proper roster checking when AnimalRoster system is created (Phase 2)
        // For now, return 1 (just this animal)
        return 1;
    }

    /// <summary>
    /// Get active skill
    /// </summary>
    public AnimalSkill GetActiveSkill()
    {
        return animalData?.activeSkill;
    }

    /// <summary>
    /// Get all unlocked passive skills
    /// </summary>
    public List<AnimalSkill> GetUnlockedPassiveSkills()
    {
        List<AnimalSkill> unlockedSkills = new List<AnimalSkill>();

        if (animalData == null || animalData.availablePassiveSkills == null) return unlockedSkills;

        foreach (AnimalSkill skill in animalData.availablePassiveSkills)
        {
            if (skill != null && combatStats.HasPassiveSkill(skill.skillId))
            {
                unlockedSkills.Add(skill);
            }
        }

        return unlockedSkills;
    }

    #endregion

    #region ISaveable Implementation

    public void SaveData(GameData gameData)
    {
        // Create or update AnimalData in CombatGameData
        var existingAnimal = gameData.combatData.ownedAnimals
            .Find(a => a.animalId == uniqueID);

        CombatGameData.AnimalData animalSaveData;

        if (existingAnimal != null)
        {
            // Update existing data
            animalSaveData = existingAnimal;
        }
        else
        {
            // Create new data
            animalSaveData = new CombatGameData.AnimalData();
            gameData.combatData.ownedAnimals.Add(animalSaveData);
        }

        // Save basic info
        animalSaveData.animalId = uniqueID;
        animalSaveData.animalType = animalData.animalType;
        animalSaveData.animalName = animalData.animalName;
        animalSaveData.level = combatStats.level;
        animalSaveData.experience = combatStats.experience;
        animalSaveData.health = combatStats.currentHealth;
        animalSaveData.maxHealth = combatStats.MaxHealth;
        animalSaveData.isActive = gameObject.activeSelf;

        // Save combat stats
        animalSaveData.stats["attack"] = combatStats.CurrentAttack;
        animalSaveData.stats["defense"] = combatStats.CurrentDefense;
        animalSaveData.stats["speed"] = combatStats.CurrentSpeed;
        animalSaveData.stats["accuracy"] = combatStats.baseAccuracy;
        animalSaveData.stats["happiness"] = combatStats.happiness;

        // Save growth multipliers
        animalSaveData.stats["attackGrowth"] = combatStats.attackGrowth;
        animalSaveData.stats["defenseGrowth"] = combatStats.defenseGrowth;
        animalSaveData.stats["speedGrowth"] = combatStats.speedGrowth;
        animalSaveData.stats["healthGrowth"] = combatStats.healthGrowth;

        // Save base stats
        animalSaveData.stats["baseAttack"] = combatStats.baseAttack;
        animalSaveData.stats["baseDefense"] = combatStats.baseDefense;
        animalSaveData.stats["baseSpeed"] = combatStats.baseSpeed;
        animalSaveData.stats["baseHealth"] = combatStats.baseHealth;

        // Save farm tracking data
        animalSaveData.animalData["timesPetted"] = timesPetted.ToString();
        animalSaveData.animalData["timesFed"] = timesFedProperly.ToString();
        animalSaveData.animalData["qualityProducts"] = highQualityProductsCreated.ToString();
        animalSaveData.animalData["positionX"] = transform.position.x.ToString();
        animalSaveData.animalData["positionY"] = transform.position.y.ToString();
        animalSaveData.animalData["positionZ"] = transform.position.z.ToString();
        animalSaveData.animalData["zoneId"] = assignedZone != null ? assignedZone.name : "";
        animalSaveData.animalData["currentDay"] = currentDay.ToString();
        animalSaveData.animalData["hasBeenPetToday"] = hasBeenPetToday.ToString();
        animalSaveData.animalData["foodEatenToday"] = foodEatenToday.ToString();
        animalSaveData.animalData["customName"] = customName;

        // Save skills
        animalSaveData.abilities.Clear();
        if (!string.IsNullOrEmpty(combatStats.activeSkillId))
        {
            animalSaveData.abilities.Add(combatStats.activeSkillId); // Active skill is first
        }
        animalSaveData.abilities.AddRange(combatStats.passiveSkillIds); // Then passive skills

    }

    public void LoadData(GameData gameData)
    {
        // Find matching animal data
        var savedAnimal = gameData.combatData.ownedAnimals
            .Find(a => a.animalId == uniqueID);

        if (savedAnimal == null)
        {
            return;
        }

        // Restore combat stats
        combatStats.level = savedAnimal.level;
        combatStats.experience = savedAnimal.experience;
        combatStats.currentHealth = savedAnimal.health;

        // Restore base stats
        if (savedAnimal.stats.ContainsKey("baseAttack"))
            combatStats.baseAttack = savedAnimal.stats["baseAttack"];
        if (savedAnimal.stats.ContainsKey("baseDefense"))
            combatStats.baseDefense = savedAnimal.stats["baseDefense"];
        if (savedAnimal.stats.ContainsKey("baseSpeed"))
            combatStats.baseSpeed = savedAnimal.stats["baseSpeed"];
        if (savedAnimal.stats.ContainsKey("baseHealth"))
            combatStats.baseHealth = savedAnimal.stats["baseHealth"];

        // Restore growth multipliers
        if (savedAnimal.stats.ContainsKey("attackGrowth"))
            combatStats.attackGrowth = savedAnimal.stats["attackGrowth"];
        if (savedAnimal.stats.ContainsKey("defenseGrowth"))
            combatStats.defenseGrowth = savedAnimal.stats["defenseGrowth"];
        if (savedAnimal.stats.ContainsKey("speedGrowth"))
            combatStats.speedGrowth = savedAnimal.stats["speedGrowth"];
        if (savedAnimal.stats.ContainsKey("healthGrowth"))
            combatStats.healthGrowth = savedAnimal.stats["healthGrowth"];

        // Restore happiness
        if (savedAnimal.stats.ContainsKey("happiness"))
            combatStats.happiness = savedAnimal.stats["happiness"];

        // Restore skills (first is active skill, rest are passive)
        if (savedAnimal.abilities.Count > 0)
        {
            combatStats.activeSkillId = savedAnimal.abilities[0];

            combatStats.passiveSkillIds.Clear();
            for (int i = 1; i < savedAnimal.abilities.Count && i < 5; i++) // Max 4 passive skills
            {
                combatStats.passiveSkillIds.Add(savedAnimal.abilities[i]);
            }
        }

        // Restore farm tracking
        if (savedAnimal.animalData.ContainsKey("timesPetted"))
            int.TryParse(savedAnimal.animalData["timesPetted"], out timesPetted);
        if (savedAnimal.animalData.ContainsKey("timesFed"))
            int.TryParse(savedAnimal.animalData["timesFed"], out timesFedProperly);
        if (savedAnimal.animalData.ContainsKey("qualityProducts"))
            int.TryParse(savedAnimal.animalData["qualityProducts"], out highQualityProductsCreated);
        if (savedAnimal.animalData.ContainsKey("currentDay"))
            int.TryParse(savedAnimal.animalData["currentDay"], out currentDay);
        if (savedAnimal.animalData.ContainsKey("hasBeenPetToday"))
            bool.TryParse(savedAnimal.animalData["hasBeenPetToday"], out hasBeenPetToday);
        if (savedAnimal.animalData.ContainsKey("foodEatenToday"))
            int.TryParse(savedAnimal.animalData["foodEatenToday"], out foodEatenToday);
        if (savedAnimal.animalData.ContainsKey("customName"))
            customName = savedAnimal.animalData["customName"];

        // Restore position
        if (savedAnimal.animalData.ContainsKey("positionX") &&
            savedAnimal.animalData.ContainsKey("positionY") &&
            savedAnimal.animalData.ContainsKey("positionZ"))
        {
            float x = float.Parse(savedAnimal.animalData["positionX"]);
            float y = float.Parse(savedAnimal.animalData["positionY"]);
            float z = float.Parse(savedAnimal.animalData["positionZ"]);
            transform.position = new Vector3(x, y, z);
        }

    }

    #endregion
}
