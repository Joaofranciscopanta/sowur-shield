using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using SowurShield.Animals;

/// <summary>
/// Edit Mode tests for AnimalData ScriptableObject.
/// Tests pure data properties — no scene or MonoBehaviour required.
/// </summary>
public class AnimalDataTests
{
    private AnimalData data;

    [SetUp]
    public void SetUp()
    {
        data = ScriptableObject.CreateInstance<AnimalData>();
        data.animalName = "TestChicken";
        data.animalType = "Chicken";
        data.canProduce = true;
        data.produceItemName = "Egg";
        data.productionIntervalDays = 1;
        data.minProduceAmount = 1;
        data.maxProduceAmount = 3;
        data.happinessProductionBonus = 0.5f;
        data.produceOnlyIfFed = false;
        data.dailyFoodRequirements = new List<FoodRequirement>
        {
            new FoodRequirement { itemName = "Grain", quantityPerDay = 2 }
        };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(data);
    }

    // =========================================================================
    // BASIC FIELDS
    // =========================================================================

    [Test]
    public void AnimalName_IsSetCorrectly()
    {
        Assert.AreEqual("TestChicken", data.animalName);
    }

    [Test]
    public void AnimalType_IsSetCorrectly()
    {
        Assert.AreEqual("Chicken", data.animalType);
    }

    // =========================================================================
    // PRODUCTION FIELDS (New)
    // =========================================================================

    [Test]
    public void CanProduce_DefaultsToFalse_ForNewInstance()
    {
        var fresh = ScriptableObject.CreateInstance<AnimalData>();
        Assert.IsFalse(fresh.canProduce);
        Object.DestroyImmediate(fresh);
    }

    [Test]
    public void GroundItemPrefab_DefaultsToNull()
    {
        var fresh = ScriptableObject.CreateInstance<AnimalData>();
        Assert.IsNull(fresh.groundItemPrefab);
        Object.DestroyImmediate(fresh);
    }

    [Test]
    public void ProduceOnlyIfFed_DefaultsToFalse()
    {
        var fresh = ScriptableObject.CreateInstance<AnimalData>();
        Assert.IsFalse(fresh.produceOnlyIfFed);
        Object.DestroyImmediate(fresh);
    }

    [Test]
    public void HappinessProductionBonus_DefaultsToZero()
    {
        var fresh = ScriptableObject.CreateInstance<AnimalData>();
        Assert.AreEqual(0f, fresh.happinessProductionBonus, 0.001f);
        Object.DestroyImmediate(fresh);
    }

    [Test]
    public void HappinessProductionBonus_CanBeSetTo50Percent()
    {
        data.happinessProductionBonus = 0.5f;
        Assert.AreEqual(0.5f, data.happinessProductionBonus, 0.001f);
    }

    [Test]
    public void ProduceOnlyIfFed_CanBeEnabledAndDisabled()
    {
        data.produceOnlyIfFed = true;
        Assert.IsTrue(data.produceOnlyIfFed);

        data.produceOnlyIfFed = false;
        Assert.IsFalse(data.produceOnlyIfFed);
    }

    [Test]
    public void ProductionIntervalDays_IsOne_ByDefault()
    {
        var fresh = ScriptableObject.CreateInstance<AnimalData>();
        Assert.AreEqual(1, fresh.productionIntervalDays);
        Object.DestroyImmediate(fresh);
    }

    [Test]
    public void MinProduceAmount_IsOne_ByDefault()
    {
        var fresh = ScriptableObject.CreateInstance<AnimalData>();
        Assert.AreEqual(1, fresh.minProduceAmount);
        Object.DestroyImmediate(fresh);
    }

    [Test]
    public void MaxProduceAmount_IsOne_ByDefault()
    {
        var fresh = ScriptableObject.CreateInstance<AnimalData>();
        Assert.AreEqual(1, fresh.maxProduceAmount);
        Object.DestroyImmediate(fresh);
    }

    // =========================================================================
    // FOOD REQUIREMENTS
    // =========================================================================

    [Test]
    public void DailyFoodRequirements_CanBeAssigned()
    {
        Assert.IsNotNull(data.dailyFoodRequirements);
        Assert.AreEqual(1, data.dailyFoodRequirements.Count);
        Assert.AreEqual("Grain", data.dailyFoodRequirements[0].itemName);
        Assert.AreEqual(2, data.dailyFoodRequirements[0].quantityPerDay);
    }

    [Test]
    public void DailyFoodRequirements_InitializesAsEmptyList()
    {
        var fresh = ScriptableObject.CreateInstance<AnimalData>();
        Assert.IsNotNull(fresh.dailyFoodRequirements);
        Assert.AreEqual(0, fresh.dailyFoodRequirements.Count);
        Object.DestroyImmediate(fresh);
    }

    // =========================================================================
    // MOVEMENT DEFAULTS
    // =========================================================================

    [Test]
    public void MoveSpeed_DefaultsTo1Point5()
    {
        var fresh = ScriptableObject.CreateInstance<AnimalData>();
        Assert.AreEqual(1.5f, fresh.moveSpeed, 0.001f);
        Object.DestroyImmediate(fresh);
    }

    [Test]
    public void WanderRadius_DefaultsTo5()
    {
        var fresh = ScriptableObject.CreateInstance<AnimalData>();
        Assert.AreEqual(5f, fresh.wanderRadius, 0.001f);
        Object.DestroyImmediate(fresh);
    }
}
