using UnityEngine;
using System.Collections.Generic;

namespace SowurShield.Animals
{

[System.Serializable]
public class FoodRequirement
{
    public string itemName;
    public int quantityPerDay;
}

[CreateAssetMenu(fileName = "NewAnimal", menuName = "Animals/Animal Data")]
public class AnimalData : ScriptableObject
{
    [Header("Basic Info")]
    public string animalName;
    public string animalType; // "Chicken", "Cow", "Monkey", etc.

    [Header("Classification (Future Use)")]
    [Tooltip("Biological family (e.g., Canidae, Felidae, Bovidae)")]
    public string animalFamily;
    [Tooltip("Biological class (e.g., Mammal, Bird, Reptile)")]
    public string animalClass;

    [Header("Visual")]
    public Sprite idleSprite;
    public RuntimeAnimatorController animatorController;

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float wanderRadius = 5f;
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;
    public float minWalkTime = 1f;
    public float maxWalkTime = 3f;

    [Header("Feeding")]
    public List<FoodRequirement> dailyFoodRequirements = new List<FoodRequirement>();

    [Header("Interaction")]
    public GameObject heartParticlePrefab;
    public float pettingCooldown = 5f;

    [Header("Production (Optional)")]
    public bool canProduce = false;
    public string produceItemName; // "Egg", "Milk", etc.
    public int productionIntervalDays = 1;
    public int minProduceAmount = 1;
    public int maxProduceAmount = 1;
    [Tooltip("Prefab with a GroundItem component that will be spawned when this animal produces.")]
    public GameObject groundItemPrefab;
    [Tooltip("If true, production is skipped on days the animal was not fed.")]
    public bool produceOnlyIfFed = false;
    [Tooltip("Fractional bonus applied to produce amount when the animal was both petted AND fed (e.g. 0.5 = +50%).")]
    [Range(0f, 1f)]
    public float happinessProductionBonus = 0f;

    [Header("Combat Stats")]
    [Tooltip("Base combat statistics for this animal type")]
    public AnimalCombatStats baseCombatStats = new AnimalCombatStats();

    [Tooltip("Combat class: Tank, DPS, Support, Utility")]
    public string combatClass = "DPS";

    [Header("Stat Growth Rates")]
    [Tooltip("Stat bonus per pet (% growth)")]
    [Range(0f, 0.1f)]
    public float pettingStatBonus = 0.01f; // +1% per pet

    [Tooltip("Stat bonus per proper feeding (% growth)")]
    [Range(0f, 0.1f)]
    public float feedingStatBonus = 0.02f; // +2% per proper feeding

    [Tooltip("Stat bonus for high-quality production (% growth)")]
    [Range(0f, 0.1f)]
    public float productionQualityBonus = 0.05f; // +5% for quality products

    [Header("Seasonal Combat Modifiers")]
    [Tooltip("Season when this animal has combat bonuses")]
    public string preferredSeason = "Spring";

    [Tooltip("Attack modifier in preferred season")]
    public float seasonalAttackBonus = 1.2f; // +20% in preferred season

    [Tooltip("Defense modifier in preferred season")]
    public float seasonalDefenseBonus = 1.1f; // +10% in preferred season

    [Tooltip("Speed modifier in preferred season")]
    public float seasonalSpeedBonus = 1.15f; // +15% in preferred season

    [Header("Synergy System")]
    [Tooltip("Can this animal stack with same family in combat?")]
    public bool canStack = true;

    [Tooltip("Maximum stack size for combat")]
    [Range(1, 5)]
    public int maxStackSize = 3;

    [Header("Skills")]
    [Tooltip("Active skill for this animal (1 per animal)")]
    public AnimalSkill activeSkill;

    [Tooltip("Available passive skills (can unlock up to 4 based on conditions)")]
    public List<AnimalSkill> availablePassiveSkills = new List<AnimalSkill>();

    [Header("Illness System")]
    [Tooltip("Consecutive days of being both unfed AND unpetted before the animal becomes ill.")]
    public int illnessThresholdDays = 3;
    [Tooltip("Item name (must match ItemDatabase key exactly) used to cure illness.")]
    public string illnessCureItemName = "Medicine";
    [Tooltip("Combat stat multiplier applied while the animal is ill (e.g. 0.5 = −50% all stats).")]
    [Range(0.1f, 1f)]
    public float illnessStatPenalty = 0.5f;

    [Header("Future: Work System")]
    public bool canDoChores = false;
    public string choreType; // "Watering", "Harvesting", etc. - for future implementation
}

} // namespace SowurShield.Animals
