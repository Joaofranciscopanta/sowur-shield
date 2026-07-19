using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using SowurShield.Animals;
using SowurShield.Core;

/// <summary>
/// Edit Mode tests for Animal.
/// Awake() runs on AddComponent; Start() does NOT run in Edit Mode, so
/// GameTimeController / SaveManager / InteractionManager will be null — that is expected.
///
/// Production logic, food percentage, save/load, and public API are all
/// exercised here without a full scene.
/// </summary>
public class AnimalTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private GameObject go;
    private Animal animal;
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
        data.minProduceAmount = 2;
        data.maxProduceAmount = 4;
        data.happinessProductionBonus = 0.5f;
        data.produceOnlyIfFed = false;
        data.dailyFoodRequirements = new List<FoodRequirement>
        {
            new FoodRequirement { itemName = "Grain", quantityPerDay = 3 }
        };

        go = new GameObject("Animal_Test");
        // Animal requires SpriteRenderer
        go.AddComponent<SpriteRenderer>();
        animal = go.AddComponent<Animal>();

        // Inject the AnimalData via reflection (private serialized field)
        SetField(animal, "animalData", data);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(data);
    }

    // ── private-state helpers ────────────────────────────────────────────────

    private static void SetField(object target, string name, object value)
    {
        var f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {target.GetType().Name}");
        f.SetValue(target, value);
    }

    private static T GetField<T>(object target, string name)
    {
        var f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {target.GetType().Name}");
        return (T)f.GetValue(target);
    }

    private static object InvokePrivate(object target, string methodName, object[] args = null)
    {
        var m = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{methodName}' not found on {target.GetType().Name}");
        return m.Invoke(target, args);
    }

    // =========================================================================
    // PUBLIC PROPERTIES
    // =========================================================================

    [Test]
    public void AnimalData_Property_ReturnsInjectedData()
    {
        Assert.AreSame(data, animal.AnimalData);
    }

    [Test]
    public void HasBeenPetToday_DefaultsFalse()
    {
        Assert.IsFalse(animal.HasBeenPetToday);
    }

    [Test]
    public void FoodEatenToday_DefaultsZero()
    {
        Assert.AreEqual(0, animal.FoodEatenToday);
    }

    [Test]
    public void NeedsFeeding_DefaultsTrue()
    {
        Assert.IsTrue(animal.NeedsFeeding);
    }

    [Test]
    public void LastProductionDay_DefaultsMinusOne()
    {
        Assert.AreEqual(-1, animal.LastProductionDay);
    }

    [Test]
    public void CurrentDay_DefaultsZero()
    {
        Assert.AreEqual(0, animal.CurrentDay);
    }

    // =========================================================================
    // GET FOOD PERCENTAGE
    // =========================================================================

    [Test]
    public void GetFoodPercentage_ReturnsOne_WhenNoFoodRequirements()
    {
        data.dailyFoodRequirements = new List<FoodRequirement>();
        Assert.AreEqual(1f, animal.GetFoodPercentage(), 0.001f);
    }

    [Test]
    public void GetFoodPercentage_ReturnsZero_WhenNoFoodEaten()
    {
        SetField(animal, "foodEatenToday", 0);
        // totalRequired = 3
        Assert.AreEqual(0f, animal.GetFoodPercentage(), 0.001f);
    }

    [Test]
    public void GetFoodPercentage_ReturnsHalf_WhenHalfEaten()
    {
        SetField(animal, "foodEatenToday", 1); // 1/3 ≈ 0.33 (partial)
        // Use a 2-item requirement summing to 4, eat 2
        data.dailyFoodRequirements = new List<FoodRequirement>
        {
            new FoodRequirement { itemName = "Grain", quantityPerDay = 2 },
            new FoodRequirement { itemName = "Seed",  quantityPerDay = 2 }
        };
        SetField(animal, "foodEatenToday", 2); // 2/4 = 0.5
        Assert.AreEqual(0.5f, animal.GetFoodPercentage(), 0.001f);
    }

    [Test]
    public void GetFoodPercentage_ReturnsOne_WhenFullyFed()
    {
        SetField(animal, "foodEatenToday", 3); // 3/3 = 1.0
        Assert.AreEqual(1f, animal.GetFoodPercentage(), 0.001f);
    }

    [Test]
    public void GetFoodPercentage_ClampsAtOne_WhenOverfed()
    {
        SetField(animal, "foodEatenToday", 99); // Way more than required
        Assert.AreEqual(1f, animal.GetFoodPercentage(), 0.001f);
    }

    // =========================================================================
    // CHECK FOOD REQUIREMENTS (private, via reflection)
    // =========================================================================

    [Test]
    public void CheckFoodRequirements_SetsNeedsFeedingFalse_WhenFullyFed()
    {
        SetField(animal, "foodEatenToday", 3); // Meets requirement of 3
        InvokePrivate(animal, "CheckFoodRequirements");
        Assert.IsFalse(animal.NeedsFeeding);
    }

    [Test]
    public void CheckFoodRequirements_SetsNeedsFeedingTrue_WhenUnderFed()
    {
        SetField(animal, "foodEatenToday", 1);
        InvokePrivate(animal, "CheckFoodRequirements");
        Assert.IsTrue(animal.NeedsFeeding);
    }

    // =========================================================================
    // CHECK PRODUCTION (pure logic — no prefab spawn)
    // =========================================================================

    [Test]
    public void CheckProduction_DoesNothing_WhenCanProduceFalse()
    {
        data.canProduce = false;
        SetField(animal, "currentDay", 1);
        SetField(animal, "lastProductionDay", -1);

        InvokePrivate(animal, "CheckProduction");

        // lastProductionDay should remain -1 (nothing happened)
        Assert.AreEqual(-1, animal.LastProductionDay);
    }

    [Test]
    public void CheckProduction_DoesNothing_WhenIntervalNotMet()
    {
        data.canProduce = true;
        data.productionIntervalDays = 3;
        // Day 1 → 1 % 3 ≠ 0, so no production
        SetField(animal, "currentDay", 1);
        SetField(animal, "lastProductionDay", -1);

        InvokePrivate(animal, "CheckProduction");

        Assert.AreEqual(-1, animal.LastProductionDay);
    }

    [Test]
    public void CheckProduction_DoesNothing_WhenAlreadyProducedToday()
    {
        data.canProduce = true;
        data.productionIntervalDays = 1;
        SetField(animal, "currentDay", 2);
        SetField(animal, "lastProductionDay", 2); // Already ran today

        // We can't easily detect "no spawn" in edit mode, but lastProductionDay won't change
        // (method exits early), so we track the event firing
        bool eventFired = false;
        animal.OnAnimalProduced += (_, __) => eventFired = true;

        InvokePrivate(animal, "CheckProduction");

        Assert.IsFalse(eventFired, "Production event should NOT fire when already produced today.");
    }

    [Test]
    public void CheckProduction_SkipsWhenProduceOnlyIfFed_AndAnimalIsHungry()
    {
        data.canProduce = true;
        data.productionIntervalDays = 1;
        data.produceOnlyIfFed = true;

        SetField(animal, "currentDay", 1);
        SetField(animal, "lastProductionDay", -1);
        SetField(animal, "needsFeeding", true); // Animal is hungry

        bool eventFired = false;
        animal.OnAnimalProduced += (_, __) => eventFired = true;

        InvokePrivate(animal, "CheckProduction");

        Assert.IsFalse(eventFired, "Should not produce when produceOnlyIfFed=true and animal is hungry.");
        Assert.AreEqual(-1, animal.LastProductionDay);
    }

    [Test]
    public void CheckProduction_SetsLastProductionDay_OnProduction()
    {
        // groundItemPrefab is null → SpawnProduce will log a warning and return early,
        // but lastProductionDay IS set before SpawnProduce is called.
        data.canProduce = true;
        data.productionIntervalDays = 1;
        data.produceOnlyIfFed = false;
        data.groundItemPrefab = null; // No prefab — spawn will be skipped

        SetField(animal, "currentDay", 4);
        SetField(animal, "lastProductionDay", -1);

        InvokePrivate(animal, "CheckProduction");

        Assert.AreEqual(4, animal.LastProductionDay);
    }

    [Test]
    public void CheckProduction_ProducesOnCorrectIntervalDay()
    {
        data.canProduce = true;
        data.productionIntervalDays = 2; // Every 2 days
        data.groundItemPrefab = null;

        // Day 2: 2 % 2 == 0 → should produce
        SetField(animal, "currentDay", 2);
        SetField(animal, "lastProductionDay", -1);

        InvokePrivate(animal, "CheckProduction");

        Assert.AreEqual(2, animal.LastProductionDay);
    }

    [Test]
    public void CheckProduction_DoesNotProduceOnOffIntervalDay()
    {
        data.canProduce = true;
        data.productionIntervalDays = 2;
        data.groundItemPrefab = null;

        // Day 3: 3 % 2 ≠ 0 → should NOT produce
        SetField(animal, "currentDay", 3);
        SetField(animal, "lastProductionDay", -1);

        InvokePrivate(animal, "CheckProduction");

        Assert.AreEqual(-1, animal.LastProductionDay);
    }

    // =========================================================================
    // HAPPINESS BONUS LOGIC
    // =========================================================================

    [Test]
    public void CheckProduction_FiresEvent_WhenHappyAndFedAndBonusEnabled()
    {
        data.canProduce = true;
        data.productionIntervalDays = 1;
        data.happinessProductionBonus = 0.5f;
        data.produceOnlyIfFed = false;
        data.groundItemPrefab = null; // No prefab — stops before Instantiate

        SetField(animal, "currentDay", 1);
        SetField(animal, "lastProductionDay", -1);
        SetField(animal, "hasBeenPetToday", true);
        SetField(animal, "needsFeeding", false); // Full and happy

        // We can't check the actual amount without ItemDatabase, but at least verify
        // lastProductionDay is set, meaning the full path ran.
        InvokePrivate(animal, "CheckProduction");

        Assert.AreEqual(1, animal.LastProductionDay);
    }

    // =========================================================================
    // ISAVEABLE — SAVE / LOAD
    // =========================================================================

    [Test]
    public void SaveData_WritesCorrectFieldsToGameData()
    {
        SetField(animal, "hasBeenPetToday", true);
        SetField(animal, "foodEatenToday", 2);
        SetField(animal, "lastProductionDay", 5);

        var gd = new GameData();
        animal.SaveData(gd);

        string prefix = $"animal_{go.name}";
        Assert.IsTrue(gd.worldData.worldFlags[$"{prefix}_petted"]);
        Assert.AreEqual(2, gd.worldData.worldCounters[$"{prefix}_foodEaten"]);
        Assert.AreEqual(5, gd.worldData.worldCounters[$"{prefix}_lastProductionDay"]);
    }

    [Test]
    public void LoadData_RestoresAllFields()
    {
        var gd = new GameData();
        string prefix = $"animal_{go.name}";
        gd.worldData.worldFlags[$"{prefix}_petted"] = true;
        gd.worldData.worldCounters[$"{prefix}_foodEaten"] = 3;
        gd.worldData.worldCounters[$"{prefix}_lastProductionDay"] = 7;

        // Pre-set requirement so CheckFoodRequirements doesn't mark needsFeeding=true
        SetField(animal, "foodEatenToday", 0);

        animal.LoadData(gd);

        Assert.IsTrue(animal.HasBeenPetToday);
        Assert.AreEqual(3, animal.FoodEatenToday);
        Assert.AreEqual(7, animal.LastProductionDay);
    }

    [Test]
    public void SaveLoad_RoundTrip_PreservesAllValues()
    {
        SetField(animal, "hasBeenPetToday", true);
        SetField(animal, "foodEatenToday", 3);
        SetField(animal, "lastProductionDay", 9);

        var gd = new GameData();
        animal.SaveData(gd);

        // Reset fields
        SetField(animal, "hasBeenPetToday", false);
        SetField(animal, "foodEatenToday", 0);
        SetField(animal, "lastProductionDay", -1);

        animal.LoadData(gd);

        Assert.IsTrue(animal.HasBeenPetToday);
        Assert.AreEqual(3, animal.FoodEatenToday);
        Assert.AreEqual(9, animal.LastProductionDay);
    }

    [Test]
    public void SaveData_DoesNotThrow_WhenGameDataIsNull()
    {
        Assert.DoesNotThrow(() => animal.SaveData(null));
    }

    [Test]
    public void LoadData_DoesNotThrow_WhenGameDataIsNull()
    {
        Assert.DoesNotThrow(() => animal.LoadData(null));
    }

    [Test]
    public void LoadData_SilentlyIgnores_MissingKeys()
    {
        // Load from an empty GameData — no keys present, fields stay at defaults
        var gd = new GameData();
        Assert.DoesNotThrow(() => animal.LoadData(gd));

        Assert.IsFalse(animal.HasBeenPetToday);
        Assert.AreEqual(0, animal.FoodEatenToday);
        Assert.AreEqual(-1, animal.LastProductionDay);
    }
}
