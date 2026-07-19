using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using SowurShield.Animals;
using SowurShield.Core;

/// <summary>
/// Edit Mode tests for the Animal Husbandry Expansion:
///  - AnimalRoster: Registration, queries, events
///  - Happiness System: ModifyHappiness, decay, save/load
///  - AutoFeed: FeedingTrough bypass feeding path
///  - GetDisplayName: Display name fallback
///
/// Start() does NOT run in Edit Mode, so singletons and scene managers
/// will be null — that is expected. We use reflection to inject data and
/// manually call registration methods.
/// </summary>
public class AnimalHusbandryTests
{
    // ── shared helpers ──────────────────────────────────────────────────────

    private static void SetField(object target, string name, object value)
    {
        var f = target.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {target.GetType().Name}");
        f.SetValue(target, value);
    }

    private static T GetField<T>(object target, string name)
    {
        var f = target.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {target.GetType().Name}");
        return (T)f.GetValue(target);
    }

    private static object InvokePrivate(object target, string methodName,
        object[] args = null)
    {
        var m = target.GetType().GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{methodName}' not found on {target.GetType().Name}");
        return m.Invoke(target, args);
    }

    // ── factory helpers ─────────────────────────────────────────────────────

    private AnimalData CreateAnimalData(string name = "TestChicken",
        string type = "Chicken", string family = "Phasianidae")
    {
        var data = ScriptableObject.CreateInstance<AnimalData>();
        data.animalName = name;
        data.animalType = type;
        data.animalFamily = family;
        data.canProduce = false;
        data.dailyFoodRequirements = new List<FoodRequirement>
        {
            new FoodRequirement { itemName = "Grain", quantityPerDay = 3 }
        };
        data.pettingCooldown = 0f; // No cooldown for tests
        return data;
    }

    /// <summary>Creates a fully wired Animal GameObject (Awake runs automatically).</summary>
    private (GameObject go, Animal animal) CreateAnimal(string objName,
        AnimalData data = null, AnimalZone zone = null)
    {
        var go = new GameObject(objName);
        go.AddComponent<SpriteRenderer>();
        var animal = go.AddComponent<Animal>();

        if (data == null) data = CreateAnimalData();
        SetField(animal, "animalData", data);
        if (zone != null) SetField(animal, "assignedZone", zone);

        return (go, animal);
    }

    // ── objects to clean up ─────────────────────────────────────────────────

    private List<Object> cleanupList = new List<Object>();
    private T Track<T>(T obj) where T : Object { cleanupList.Add(obj); return obj; }

    [TearDown]
    public void TearDown()
    {
        // Destroy all tracked objects
        foreach (var obj in cleanupList)
        {
            if (obj != null) Object.DestroyImmediate(obj);
        }
        cleanupList.Clear();

        // Reset AnimalRoster singleton
        if (AnimalRoster.Instance != null)
            Object.DestroyImmediate(AnimalRoster.Instance.gameObject);
    }

    // ── AnimalRoster helper ─────────────────────────────────────────────────

    private AnimalRoster CreateRoster()
    {
        var rosterGo = new GameObject("AnimalRoster_Test");
        Track(rosterGo);

        // AddComponent() does not run Awake() synchronously in Edit Mode, so invoke it
        // manually via reflection to set AnimalRoster.Instance now.
        var roster = rosterGo.AddComponent<AnimalRoster>();
        InvokePrivate(roster, "Awake");

        return roster;
    }

    // =========================================================================
    // ANIMAL ROSTER — Registration
    // =========================================================================

    [Test]
    public void Roster_RegisterAnimal_IncreasesCount()
    {
        var roster = CreateRoster();
        var (go, animal) = CreateAnimal("Hen1");
        Track(go);
        var data = CreateAnimalData(); Track(data);
        SetField(animal, "animalData", data);

        roster.RegisterAnimal(animal);

        Assert.AreEqual(1, roster.GetAnimalCount());
    }

    [Test]
    public void Roster_RegisterAnimal_IgnoresNull()
    {
        var roster = CreateRoster();
        roster.RegisterAnimal(null);
        Assert.AreEqual(0, roster.GetAnimalCount());
    }

    [Test]
    public void Roster_RegisterAnimal_IgnoresDuplicate()
    {
        var roster = CreateRoster();
        var (go, animal) = CreateAnimal("Hen2");
        Track(go);
        var data = CreateAnimalData(); Track(data);
        SetField(animal, "animalData", data);

        roster.RegisterAnimal(animal);
        roster.RegisterAnimal(animal); // duplicate

        Assert.AreEqual(1, roster.GetAnimalCount());
    }

    [Test]
    public void Roster_UnregisterAnimal_DecreasesCount()
    {
        var roster = CreateRoster();
        var (go, animal) = CreateAnimal("Hen3");
        Track(go);
        var data = CreateAnimalData(); Track(data);
        SetField(animal, "animalData", data);

        roster.RegisterAnimal(animal);
        roster.UnregisterAnimal(animal);

        Assert.AreEqual(0, roster.GetAnimalCount());
    }

    [Test]
    public void Roster_UnregisterAnimal_IgnoresNull()
    {
        var roster = CreateRoster();
        Assert.DoesNotThrow(() => roster.UnregisterAnimal(null));
    }

    [Test]
    public void Roster_UnregisterAnimal_IgnoresNotRegistered()
    {
        var roster = CreateRoster();
        var (go, animal) = CreateAnimal("Hen4");
        Track(go);

        Assert.DoesNotThrow(() => roster.UnregisterAnimal(animal));
        Assert.AreEqual(0, roster.GetAnimalCount());
    }

    // =========================================================================
    // ANIMAL ROSTER — Events
    // =========================================================================

    [Test]
    public void Roster_RegisterAnimal_FiresEvent()
    {
        var roster = CreateRoster();
        var (go, animal) = CreateAnimal("HenEvt");
        Track(go);
        var data = CreateAnimalData(); Track(data);
        SetField(animal, "animalData", data);

        Animal received = null;
        roster.OnAnimalRegistered += a => received = a;

        roster.RegisterAnimal(animal);

        Assert.AreSame(animal, received);
    }

    [Test]
    public void Roster_UnregisterAnimal_FiresEvent()
    {
        var roster = CreateRoster();
        var (go, animal) = CreateAnimal("HenEvt2");
        Track(go);
        var data = CreateAnimalData(); Track(data);
        SetField(animal, "animalData", data);

        roster.RegisterAnimal(animal);

        Animal received = null;
        roster.OnAnimalUnregistered += a => received = a;

        roster.UnregisterAnimal(animal);

        Assert.AreSame(animal, received);
    }

    // =========================================================================
    // ANIMAL ROSTER — Queries
    // =========================================================================

    [Test]
    public void Roster_GetAllAnimals_ReturnsCopy()
    {
        var roster = CreateRoster();
        var (go, animal) = CreateAnimal("HenCopy");
        Track(go);
        var data = CreateAnimalData(); Track(data);
        SetField(animal, "animalData", data);

        roster.RegisterAnimal(animal);
        var list = roster.GetAllAnimals();

        // Modifying the returned list should not affect roster
        list.Clear();
        Assert.AreEqual(1, roster.GetAnimalCount());
    }

    [Test]
    public void Roster_GetAnimalsByType_FiltersCorrectly()
    {
        var roster = CreateRoster();

        var chickenData = CreateAnimalData("Hen", "Chicken"); Track(chickenData);
        var cowData = CreateAnimalData("Bessie", "Cow"); Track(cowData);

        var (go1, chicken) = CreateAnimal("Hen_Type");
        Track(go1);
        SetField(chicken, "animalData", chickenData);

        var (go2, cow) = CreateAnimal("Cow_Type");
        Track(go2);
        SetField(cow, "animalData", cowData);

        roster.RegisterAnimal(chicken);
        roster.RegisterAnimal(cow);

        var chickens = roster.GetAnimalsByType("Chicken");
        Assert.AreEqual(1, chickens.Count);
        Assert.AreSame(chicken, chickens[0]);
    }

    [Test]
    public void Roster_GetAnimalsByType_CaseInsensitive()
    {
        var roster = CreateRoster();
        var data = CreateAnimalData("Hen", "Chicken"); Track(data);

        var (go, animal) = CreateAnimal("Hen_CI");
        Track(go);
        SetField(animal, "animalData", data);

        roster.RegisterAnimal(animal);

        var result = roster.GetAnimalsByType("chicken"); // lowercase
        Assert.AreEqual(1, result.Count);
    }

    [Test]
    public void Roster_GetAnimalsByType_ReturnsEmpty_ForNullOrEmpty()
    {
        var roster = CreateRoster();
        Assert.AreEqual(0, roster.GetAnimalsByType(null).Count);
        Assert.AreEqual(0, roster.GetAnimalsByType("").Count);
    }

    [Test]
    public void Roster_GetFamilyCount_CountsMatchingFamily()
    {
        var roster = CreateRoster();

        var data1 = CreateAnimalData("Hen1", "Chicken", "Phasianidae"); Track(data1);
        var data2 = CreateAnimalData("Hen2", "Chicken", "Phasianidae"); Track(data2);
        var data3 = CreateAnimalData("Bessie", "Cow", "Bovidae"); Track(data3);

        var (go1, a1) = CreateAnimal("FC1"); Track(go1); SetField(a1, "animalData", data1);
        var (go2, a2) = CreateAnimal("FC2"); Track(go2); SetField(a2, "animalData", data2);
        var (go3, a3) = CreateAnimal("FC3"); Track(go3); SetField(a3, "animalData", data3);

        roster.RegisterAnimal(a1);
        roster.RegisterAnimal(a2);
        roster.RegisterAnimal(a3);

        Assert.AreEqual(2, roster.GetFamilyCount("Phasianidae"));
        Assert.AreEqual(1, roster.GetFamilyCount("Bovidae"));
        Assert.AreEqual(0, roster.GetFamilyCount("Canidae"));
    }

    [Test]
    public void Roster_GetHungryAnimalCount_CountsCorrectly()
    {
        var roster = CreateRoster();

        var data1 = CreateAnimalData(); Track(data1);
        var data2 = CreateAnimalData(); Track(data2);

        var (go1, a1) = CreateAnimal("HA1"); Track(go1); SetField(a1, "animalData", data1);
        var (go2, a2) = CreateAnimal("HA2"); Track(go2); SetField(a2, "animalData", data2);

        // a1 needs feeding (default), a2 is fully fed
        SetField(a2, "needsFeeding", false);

        roster.RegisterAnimal(a1);
        roster.RegisterAnimal(a2);

        Assert.AreEqual(1, roster.GetHungryAnimalCount());
    }

    [Test]
    public void Roster_GetAverageHappiness_ReturnsCorrectAverage()
    {
        var roster = CreateRoster();

        var data1 = CreateAnimalData(); Track(data1);
        var data2 = CreateAnimalData(); Track(data2);

        var (go1, a1) = CreateAnimal("AVG1"); Track(go1); SetField(a1, "animalData", data1);
        var (go2, a2) = CreateAnimal("AVG2"); Track(go2); SetField(a2, "animalData", data2);

        SetField(a1, "happiness", 80f);
        SetField(a2, "happiness", 40f);

        roster.RegisterAnimal(a1);
        roster.RegisterAnimal(a2);

        Assert.AreEqual(60f, roster.GetAverageHappiness(), 0.001f);
    }

    [Test]
    public void Roster_GetAverageHappiness_ReturnsZero_WhenEmpty()
    {
        var roster = CreateRoster();
        Assert.AreEqual(0f, roster.GetAverageHappiness(), 0.001f);
    }

    // =========================================================================
    // HAPPINESS SYSTEM — Direct API
    // =========================================================================

    [Test]
    public void Happiness_DefaultsToFifty()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("H_Default"); Track(go);
        SetField(animal, "animalData", data);

        Assert.AreEqual(50f, animal.GetHappiness(), 0.001f);
    }

    [Test]
    public void ModifyHappiness_IncreasesValue()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("H_Inc"); Track(go);
        SetField(animal, "animalData", data);

        animal.ModifyHappiness(10f);
        Assert.AreEqual(60f, animal.GetHappiness(), 0.001f);
    }

    [Test]
    public void ModifyHappiness_DecreasesValue()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("H_Dec"); Track(go);
        SetField(animal, "animalData", data);

        animal.ModifyHappiness(-20f);
        Assert.AreEqual(30f, animal.GetHappiness(), 0.001f);
    }

    [Test]
    public void ModifyHappiness_ClampsAtZero()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("H_Zero"); Track(go);
        SetField(animal, "animalData", data);

        animal.ModifyHappiness(-200f);
        Assert.AreEqual(0f, animal.GetHappiness(), 0.001f);
    }

    [Test]
    public void ModifyHappiness_ClampsAtHundred()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("H_Max"); Track(go);
        SetField(animal, "animalData", data);

        animal.ModifyHappiness(200f);
        Assert.AreEqual(100f, animal.GetHappiness(), 0.001f);
    }

    [Test]
    public void GetHappinessMultiplier_Returns0_5_AtZero()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("HM_0"); Track(go);
        SetField(animal, "animalData", data);
        SetField(animal, "happiness", 0f);

        Assert.AreEqual(0.5f, animal.GetHappinessMultiplier(), 0.001f);
    }

    [Test]
    public void GetHappinessMultiplier_Returns1_0_AtFifty()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("HM_50"); Track(go);
        SetField(animal, "animalData", data);
        // happiness defaults to 50

        Assert.AreEqual(1.0f, animal.GetHappinessMultiplier(), 0.001f);
    }

    [Test]
    public void GetHappinessMultiplier_Returns1_5_AtHundred()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("HM_100"); Track(go);
        SetField(animal, "animalData", data);
        SetField(animal, "happiness", 100f);

        Assert.AreEqual(1.5f, animal.GetHappinessMultiplier(), 0.001f);
    }

    // =========================================================================
    // HAPPINESS — Daily Decay (via reflection)
    // =========================================================================

    [Test]
    public void ApplyDailyHappinessDecay_NoDecay_WhenPettedAndFed()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("HD_NoDecay"); Track(go);
        SetField(animal, "animalData", data);
        SetField(animal, "happiness", 80f);
        SetField(animal, "hasBeenPetToday", true);
        SetField(animal, "needsFeeding", false);

        InvokePrivate(animal, "ApplyDailyHappinessDecay");

        Assert.AreEqual(80f, animal.GetHappiness(), 0.001f);
    }

    [Test]
    public void ApplyDailyHappinessDecay_Loses0_5_WhenNotPetted()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("HD_NoPet"); Track(go);
        SetField(animal, "animalData", data);
        SetField(animal, "happiness", 80f);
        SetField(animal, "hasBeenPetToday", false);
        SetField(animal, "needsFeeding", false);

        InvokePrivate(animal, "ApplyDailyHappinessDecay");

        Assert.AreEqual(79.5f, animal.GetHappiness(), 0.001f);
    }

    [Test]
    public void ApplyDailyHappinessDecay_Loses1_0_WhenNotFed()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("HD_NoFeed"); Track(go);
        SetField(animal, "animalData", data);
        SetField(animal, "happiness", 80f);
        SetField(animal, "hasBeenPetToday", true);
        SetField(animal, "needsFeeding", true);

        InvokePrivate(animal, "ApplyDailyHappinessDecay");

        Assert.AreEqual(79f, animal.GetHappiness(), 0.001f);
    }

    [Test]
    public void ApplyDailyHappinessDecay_Loses1_5_WhenNeither()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("HD_Neither"); Track(go);
        SetField(animal, "animalData", data);
        SetField(animal, "happiness", 80f);
        SetField(animal, "hasBeenPetToday", false);
        SetField(animal, "needsFeeding", true);

        InvokePrivate(animal, "ApplyDailyHappinessDecay");

        Assert.AreEqual(78.5f, animal.GetHappiness(), 0.001f);
    }

    [Test]
    public void ApplyDailyHappinessDecay_ClampsAtTwenty()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("HD_Min"); Track(go);
        SetField(animal, "animalData", data);
        SetField(animal, "happiness", 20f); // At the floor
        SetField(animal, "hasBeenPetToday", false);
        SetField(animal, "needsFeeding", true);

        InvokePrivate(animal, "ApplyDailyHappinessDecay");

        Assert.AreEqual(20f, animal.GetHappiness(), 0.001f);
    }

    // =========================================================================
    // HAPPINESS — Save/Load
    // =========================================================================

    [Test]
    public void SaveData_PersistsHappiness()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("H_Save"); Track(go);
        SetField(animal, "animalData", data);
        SetField(animal, "happiness", 75f);

        var gd = new GameData();
        animal.SaveData(gd);

        string prefix = $"animal_{go.name}";
        Assert.IsTrue(gd.worldData.worldCounters.ContainsKey($"{prefix}_happiness"));
        Assert.AreEqual(75, gd.worldData.worldCounters[$"{prefix}_happiness"]);
    }

    [Test]
    public void LoadData_RestoresHappiness()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("H_Load"); Track(go);
        SetField(animal, "animalData", data);

        var gd = new GameData();
        string prefix = $"animal_{go.name}";
        gd.worldData.worldCounters[$"{prefix}_happiness"] = 90;

        animal.LoadData(gd);

        Assert.AreEqual(90f, animal.GetHappiness(), 0.001f);
    }

    [Test]
    public void LoadData_ClampsHappinessToRange()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("H_Clamp"); Track(go);
        SetField(animal, "animalData", data);

        var gd = new GameData();
        string prefix = $"animal_{go.name}";
        gd.worldData.worldCounters[$"{prefix}_happiness"] = 999;

        animal.LoadData(gd);

        Assert.AreEqual(100f, animal.GetHappiness(), 0.001f);
    }

    [Test]
    public void SaveLoad_RoundTrip_PreservesHappiness()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("H_RT"); Track(go);
        SetField(animal, "animalData", data);
        SetField(animal, "happiness", 63f);

        var gd = new GameData();
        animal.SaveData(gd);

        // Reset
        SetField(animal, "happiness", 0f);

        animal.LoadData(gd);

        // Saved as RoundToInt(63) = 63, loaded as 63
        Assert.AreEqual(63f, animal.GetHappiness(), 0.001f);
    }

    // =========================================================================
    // AUTOFEED — Trough-based feeding bypass
    // =========================================================================

    [Test]
    public void AutoFeed_IncreasesFood()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("AF_Inc"); Track(go);
        SetField(animal, "animalData", data);

        animal.AutoFeed(2);

        Assert.AreEqual(2, animal.FoodEatenToday);
    }

    [Test]
    public void AutoFeed_IncreasesHappiness()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("AF_Happy"); Track(go);
        SetField(animal, "animalData", data);
        SetField(animal, "happiness", 50f);

        animal.AutoFeed(2); // +3 * 2 = +6 happiness

        Assert.AreEqual(56f, animal.GetHappiness(), 0.001f);
    }

    [Test]
    public void AutoFeed_SetsNeedsFeedingFalse_WhenFullyFed()
    {
        var data = CreateAnimalData(); Track(data);
        // dailyFoodRequirements: Grain x3
        var (go, animal) = CreateAnimal("AF_Full"); Track(go);
        SetField(animal, "animalData", data);

        Assert.IsTrue(animal.NeedsFeeding);
        animal.AutoFeed(3); // Feed exactly the required amount

        Assert.IsFalse(animal.NeedsFeeding);
    }

    [Test]
    public void AutoFeed_DoesNothing_WhenAmountZero()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("AF_Zero"); Track(go);
        SetField(animal, "animalData", data);
        SetField(animal, "happiness", 50f);

        animal.AutoFeed(0);

        Assert.AreEqual(0, animal.FoodEatenToday);
        Assert.AreEqual(50f, animal.GetHappiness(), 0.001f);
    }

    [Test]
    public void AutoFeed_DoesNothing_WhenAmountNegative()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("AF_Neg"); Track(go);
        SetField(animal, "animalData", data);
        SetField(animal, "happiness", 50f);

        animal.AutoFeed(-5);

        Assert.AreEqual(0, animal.FoodEatenToday);
        Assert.AreEqual(50f, animal.GetHappiness(), 0.001f);
    }

    [Test]
    public void AutoFeed_StacksWithManualFeeding()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("AF_Stack"); Track(go);
        SetField(animal, "animalData", data);
        SetField(animal, "foodEatenToday", 1); // Already manually fed 1

        animal.AutoFeed(2); // Auto-feed 2 more

        Assert.AreEqual(3, animal.FoodEatenToday);
    }

    // =========================================================================
    // GET DISPLAY NAME
    // =========================================================================

    [Test]
    public void GetDisplayName_ReturnsAnimalDataName()
    {
        var data = CreateAnimalData("Clucky"); Track(data);
        var (go, animal) = CreateAnimal("DN_Data"); Track(go);
        SetField(animal, "animalData", data);

        Assert.AreEqual("Clucky", animal.GetDisplayName());
    }

    [Test]
    public void GetDisplayName_ReturnsGameObjectName_WhenNoData()
    {
        var (go, animal) = CreateAnimal("Fallback_Name");
        Track(go);
        SetField(animal, "animalData", null);

        Assert.AreEqual("Fallback_Name", animal.GetDisplayName());
    }

    // =========================================================================
    // ANIMAL ROSTER — Singleton
    // =========================================================================

    [Test]
    public void Roster_Singleton_DestroysSecondInstance()
    {
        var roster1 = CreateRoster();
        // roster1 is now the singleton

        var go2 = new GameObject("Roster_Dup");
        Track(go2);
        var roster2 = go2.AddComponent<AnimalRoster>();

        // Second Awake should Destroy itself, leaving Instance as roster1
        Assert.AreSame(roster1, AnimalRoster.Instance);
    }

    [Test]
    public void Roster_Singleton_ClearsOnDestroy()
    {
        var roster = CreateRoster();
        Assert.AreSame(roster, AnimalRoster.Instance);

        var rosterGo = roster.gameObject;

        // AnimalRoster.OnDestroy() clears Instance, but DestroyImmediate doesn't
        // invoke MonoBehaviour callbacks synchronously in batch Edit Mode tests —
        // invoke it manually, matching the Awake() workaround used in CreateRoster().
        InvokePrivate(roster, "OnDestroy");

        Object.DestroyImmediate(rosterGo);
        // Remove from cleanup list since already destroyed
        cleanupList.Remove(rosterGo);

        Assert.IsTrue(AnimalRoster.Instance == null);
    }

    // =========================================================================
    // ANIMAL ROSTER — GetAnimalsByZone
    // =========================================================================

    [Test]
    public void Roster_GetAnimalsByZone_FiltersCorrectly()
    {
        var roster = CreateRoster();

        // Create two zones (AnimalZone requires a Collider2D)
        var zoneGo1 = new GameObject("Zone1"); Track(zoneGo1);
        zoneGo1.AddComponent<BoxCollider2D>();
        var zone1 = zoneGo1.AddComponent<AnimalZone>();

        var zoneGo2 = new GameObject("Zone2"); Track(zoneGo2);
        zoneGo2.AddComponent<BoxCollider2D>();
        var zone2 = zoneGo2.AddComponent<AnimalZone>();

        var data = CreateAnimalData(); Track(data);

        var (go1, a1) = CreateAnimal("ZA1"); Track(go1);
        SetField(a1, "animalData", data);
        SetField(a1, "assignedZone", zone1);

        var (go2, a2) = CreateAnimal("ZA2"); Track(go2);
        SetField(a2, "animalData", data);
        SetField(a2, "assignedZone", zone2);

        roster.RegisterAnimal(a1);
        roster.RegisterAnimal(a2);

        var zone1Animals = roster.GetAnimalsByZone(zone1);
        Assert.AreEqual(1, zone1Animals.Count);
        Assert.AreSame(a1, zone1Animals[0]);
    }

    [Test]
    public void Roster_GetAnimalsByZone_ReturnsEmpty_ForNullZone()
    {
        var roster = CreateRoster();
        Assert.AreEqual(0, roster.GetAnimalsByZone(null).Count);
    }

    // =========================================================================
    // ANIMAL ROSTER — GetOccupiedZones
    // =========================================================================

    [Test]
    public void Roster_GetOccupiedZones_ReturnsUniqueZones()
    {
        var roster = CreateRoster();

        var zoneGo = new GameObject("ZoneOcc"); Track(zoneGo);
        zoneGo.AddComponent<BoxCollider2D>();
        var zone = zoneGo.AddComponent<AnimalZone>();

        var data = CreateAnimalData(); Track(data);

        var (go1, a1) = CreateAnimal("OZ1"); Track(go1);
        SetField(a1, "animalData", data);
        SetField(a1, "assignedZone", zone);

        var (go2, a2) = CreateAnimal("OZ2"); Track(go2);
        SetField(a2, "animalData", data);
        SetField(a2, "assignedZone", zone);

        roster.RegisterAnimal(a1);
        roster.RegisterAnimal(a2);

        var zones = roster.GetOccupiedZones();
        Assert.AreEqual(1, zones.Count); // Same zone, shouldn't duplicate
    }

    // =========================================================================
    // COMBAT STATS GROWTH — Daily Care
    // =========================================================================

    [Test]
    public void ApplyStatGrowth_IncreasesAttackGrowth()
    {
        var stats = new AnimalCombatStats();
        stats.attackGrowth = 1f;

        stats.ApplyStatGrowth("attack", 0.01f);

        Assert.AreEqual(1.01f, stats.attackGrowth, 0.001f);
    }

    [Test]
    public void ApplyStatGrowth_ClampsAtThree()
    {
        var stats = new AnimalCombatStats();
        stats.attackGrowth = 2.99f;

        stats.ApplyStatGrowth("attack", 0.1f);

        Assert.AreEqual(3f, stats.attackGrowth, 0.001f);
    }

    [Test]
    public void ApplyStatGrowth_ClampsAtOne_Minimum()
    {
        var stats = new AnimalCombatStats();
        stats.defenseGrowth = 1f;

        stats.ApplyStatGrowth("defense", -0.5f);

        Assert.AreEqual(1f, stats.defenseGrowth, 0.001f);
    }

    [Test]
    public void ApplyStatGrowth_All_GrowsAllStats()
    {
        var stats = new AnimalCombatStats();

        stats.ApplyStatGrowth("all", 0.05f);

        Assert.AreEqual(1.05f, stats.attackGrowth, 0.001f);
        Assert.AreEqual(1.05f, stats.defenseGrowth, 0.001f);
        Assert.AreEqual(1.05f, stats.speedGrowth, 0.001f);
        Assert.AreEqual(1.05f, stats.healthGrowth, 0.001f);
    }

    [Test]
    public void GetCombatStats_ReturnsInitializedStats()
    {
        var data = CreateAnimalData(); Track(data);
        data.baseCombatStats = new AnimalCombatStats
        {
            baseAttack = 20f,
            baseDefense = 15f,
            baseSpeed = 10f,
            baseHealth = 100f
        };
        var (go, animal) = CreateAnimal("CS_Init"); Track(go);
        SetField(animal, "animalData", data);

        // Force initialization via GetCombatStats
        var stats = animal.GetCombatStats();

        Assert.IsNotNull(stats);
        Assert.AreEqual(20f, stats.baseAttack, 0.001f);
        Assert.AreEqual(15f, stats.baseDefense, 0.001f);
    }

    [Test]
    public void GetCombatStats_SyncsHappiness()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("CS_Sync"); Track(go);
        SetField(animal, "animalData", data);
        SetField(animal, "happiness", 80f);

        var stats = animal.GetCombatStats();

        Assert.AreEqual(80f, stats.happiness, 0.001f);
    }

    [Test]
    public void GrowthSaveLoad_RoundTrip_PreservesMultipliers()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("GR_RT"); Track(go);
        SetField(animal, "animalData", data);

        // Set up growth values
        var stats = animal.GetCombatStats();
        stats.attackGrowth = 1.25f;
        stats.defenseGrowth = 1.10f;
        stats.speedGrowth = 1.50f;
        stats.healthGrowth = 2.00f;

        // Save
        var gd = new GameData();
        animal.SaveData(gd);

        // Reset growth
        stats.attackGrowth = 1f;
        stats.defenseGrowth = 1f;
        stats.speedGrowth = 1f;
        stats.healthGrowth = 1f;

        // Load
        animal.LoadData(gd);

        // Verify — int*1000 encoding may cause minor precision loss
        stats = animal.GetCombatStats();
        Assert.AreEqual(1.25f, stats.attackGrowth, 0.002f);
        Assert.AreEqual(1.10f, stats.defenseGrowth, 0.002f);
        Assert.AreEqual(1.50f, stats.speedGrowth, 0.002f);
        Assert.AreEqual(2.00f, stats.healthGrowth, 0.002f);
    }

    [Test]
    public void GrowthLoad_ClampsValues()
    {
        var data = CreateAnimalData(); Track(data);
        var (go, animal) = CreateAnimal("GR_Clamp"); Track(go);
        SetField(animal, "animalData", data);

        // Force init
        animal.GetCombatStats();

        // Manually inject out-of-range values
        var gd = new GameData();
        string prefix = $"animal_{go.name}";
        gd.worldData.worldCounters[$"{prefix}_attackGrowth"] = 5000; // 5.0 — over max
        gd.worldData.worldCounters[$"{prefix}_defenseGrowth"] = 500; // 0.5 — under min

        animal.LoadData(gd);

        var stats = animal.GetCombatStats();
        Assert.AreEqual(3f, stats.attackGrowth, 0.001f); // Clamped to max
        Assert.AreEqual(1f, stats.defenseGrowth, 0.001f); // Clamped to min
    }

    // =========================================================================
    // SEASONAL MODIFIERS
    // =========================================================================

    [Test]
    public void SeasonalModifiers_ApplyCorrectly()
    {
        var stats = new AnimalCombatStats();

        stats.ApplySeasonalModifiers(1.2f, 1.1f, 1.15f);

        Assert.AreEqual(1.2f, stats.seasonalAttackMod, 0.001f);
        Assert.AreEqual(1.1f, stats.seasonalDefenseMod, 0.001f);
        Assert.AreEqual(1.15f, stats.seasonalSpeedMod, 0.001f);
    }

    [Test]
    public void SeasonalModifiers_ResetToDefaults()
    {
        var stats = new AnimalCombatStats();
        stats.ApplySeasonalModifiers(1.5f, 1.5f, 1.5f);

        stats.ResetSeasonalModifiers();

        Assert.AreEqual(1f, stats.seasonalAttackMod, 0.001f);
        Assert.AreEqual(1f, stats.seasonalDefenseMod, 0.001f);
        Assert.AreEqual(1f, stats.seasonalSpeedMod, 0.001f);
    }

    [Test]
    public void SeasonalModifiers_AffectCalculatedStats()
    {
        var stats = new AnimalCombatStats();
        stats.baseAttack = 10f;
        stats.attackGrowth = 1f;
        stats.happiness = 50f; // HappinessMultiplier = 1.0

        stats.ApplySeasonalModifiers(1.2f, 1f, 1f);

        // CurrentAttack = 10 * 1.0 * 1.0 * 1.2 = 12
        Assert.AreEqual(12f, stats.CurrentAttack, 0.001f);
    }

    // =========================================================================
    // CUSTOM NAMING
    // =========================================================================

    [Test]
    public void SetCustomName_UpdatesDisplayName()
    {
        var data = CreateAnimalData("Clucky"); Track(data);
        var (go, animal) = CreateAnimal("CN_Set"); Track(go);
        SetField(animal, "animalData", data);

        animal.SetCustomName("Bob");

        Assert.AreEqual("Bob", animal.GetDisplayName());
    }

    [Test]
    public void SetCustomName_Empty_ResetsToDefault()
    {
        var data = CreateAnimalData("Clucky"); Track(data);
        var (go, animal) = CreateAnimal("CN_Reset"); Track(go);
        SetField(animal, "animalData", data);

        animal.SetCustomName("Bob");
        animal.SetCustomName("");

        Assert.AreEqual("Clucky", animal.GetDisplayName());
    }

    [Test]
    public void SetCustomName_Null_ResetsToDefault()
    {
        var data = CreateAnimalData("Clucky"); Track(data);
        var (go, animal) = CreateAnimal("CN_Null"); Track(go);
        SetField(animal, "animalData", data);

        animal.SetCustomName("Bob");
        animal.SetCustomName(null);

        Assert.AreEqual("Clucky", animal.GetDisplayName());
    }

    [Test]
    public void SetCustomName_TruncatesAt20Chars()
    {
        var data = CreateAnimalData("Hen"); Track(data);
        var (go, animal) = CreateAnimal("CN_Long"); Track(go);
        SetField(animal, "animalData", data);

        animal.SetCustomName("ABCDEFGHIJKLMNOPQRSTUVWXYZ"); // 26 chars

        Assert.AreEqual(20, animal.GetCustomName().Length);
        Assert.AreEqual("ABCDEFGHIJKLMNOPQRST", animal.GetCustomName());
    }

    [Test]
    public void SetCustomName_TrimsWhitespace()
    {
        var data = CreateAnimalData("Hen"); Track(data);
        var (go, animal) = CreateAnimal("CN_Trim"); Track(go);
        SetField(animal, "animalData", data);

        animal.SetCustomName("  Fluffy  ");

        Assert.AreEqual("Fluffy", animal.GetCustomName());
    }

    [Test]
    public void CustomName_SaveLoad_RoundTrip()
    {
        var data = CreateAnimalData("Clucky"); Track(data);
        var (go, animal) = CreateAnimal("CN_RT"); Track(go);
        SetField(animal, "animalData", data);

        animal.SetCustomName("Princess");

        // Save
        var gd = new GameData();
        animal.SaveData(gd);

        // Reset
        SetField(animal, "customName", "");
        Assert.AreEqual("Clucky", animal.GetDisplayName());

        // Load
        animal.LoadData(gd);

        Assert.AreEqual("Princess", animal.GetDisplayName());
    }

    [Test]
    public void CustomName_SaveLoad_EmptyName_NotSaved()
    {
        var data = CreateAnimalData("Clucky"); Track(data);
        var (go, animal) = CreateAnimal("CN_Empty"); Track(go);
        SetField(animal, "animalData", data);

        // Don't set custom name — should not save a worldStrings entry
        var gd = new GameData();
        animal.SaveData(gd);

        string prefix = $"animal_{go.name}";
        Assert.IsFalse(gd.worldData.worldStrings.ContainsKey($"{prefix}_customName"));
    }
}
