using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

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
        var roster = rosterGo.AddComponent<AnimalRoster>();
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

        Object.DestroyImmediate(roster.gameObject);
        // Remove from cleanup list since already destroyed
        cleanupList.Remove(roster.gameObject);

        Assert.IsNull(AnimalRoster.Instance);
    }

    // =========================================================================
    // ANIMAL ROSTER — GetAnimalsByZone
    // =========================================================================

    [Test]
    public void Roster_GetAnimalsByZone_FiltersCorrectly()
    {
        var roster = CreateRoster();

        // Create two zones
        var zoneGo1 = new GameObject("Zone1"); Track(zoneGo1);
        var zone1 = zoneGo1.AddComponent<AnimalZone>();

        var zoneGo2 = new GameObject("Zone2"); Track(zoneGo2);
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
}
