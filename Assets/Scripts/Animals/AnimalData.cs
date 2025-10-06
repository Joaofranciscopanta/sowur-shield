using UnityEngine;
using System.Collections.Generic;

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

    [Header("Future: Work System")]
    public bool canDoChores = false;
    public string choreType; // "Watering", "Harvesting", etc. - for future implementation
}
