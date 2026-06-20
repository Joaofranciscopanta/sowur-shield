using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using SowurShield.Core;

/// <summary>
/// Edit Mode unit tests for CropGrowthManager.
///
/// Complements CropGrowthPlayModeTests (which covers the full coroutine / yield cycle)
/// by exercising edges that do not need a running scene:
///   - Events (OnCropGrown, OnCropDied, OnCropReadyForHarvest, OnCropHarvested)
///   - GetCropInfo string output
///   - Regrowth logic (regrowsAfterHarvest, maxRegrowths)
///   - Crops that grow without water (requiresWater = false)
///   - Save / Load null-safety and written values
///   - KillCrop (private) via reflection
/// </summary>
public class CropGrowthManagerTests
{
    // ── fixtures ─────────────────────────────────────────────────────────────

    private GameObject soilGo;
    private CropGrowthManager cropManager;
    private CropData testCrop;

    [SetUp]
    public void SetUp()
    {
        testCrop = ScriptableObject.CreateInstance<CropData>();
        testCrop.cropName = "TestCarrot";
        testCrop.daysPerStage = 1;
        testCrop.growthStages = new Sprite[3]; // stages 0-2, harvest at stage 2
        testCrop.requiresWater = true;
        testCrop.maxDaysWithoutWater = 5; // generous — won't die accidentally
        testCrop.minYield = 2;
        testCrop.maxYield = 4;
        testCrop.growsInAllSeasons = true;
        testCrop.regrowsAfterHarvest = false;
        testCrop.maxRegrowths = 0;

        soilGo = new GameObject("SoilBlock_CGM_Test");
        cropManager = soilGo.AddComponent<CropGrowthManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(soilGo);
        Object.DestroyImmediate(testCrop);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>Invoke the private OnDayChanged() method.</summary>
    private void SimulateDayChange()
    {
        var m = typeof(CropGrowthManager).GetMethod(
            "OnDayChanged",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "CropGrowthManager must have a private OnDayChanged() method");
        m.Invoke(cropManager, null);
    }

    /// <summary>Invoke the private KillCrop() method.</summary>
    private void KillCropDirectly()
    {
        var m = typeof(CropGrowthManager).GetMethod(
            "KillCrop",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "CropGrowthManager must have a private KillCrop() method");
        m.Invoke(cropManager, null);
    }

    /// <summary>Grow the crop all the way to harvest-ready (waters + advances each day).</summary>
    private void GrowToHarvest()
    {
        cropManager.PlantCrop(testCrop);
        int totalStages = testCrop.TotalStages;
        for (int i = 0; i < totalStages; i++)
        {
            cropManager.WaterCrop();
            SimulateDayChange();
        }
    }

    // =========================================================================
    // KILL CROP (private)
    // =========================================================================

    [Test]
    public void KillCrop_SetsIsDead()
    {
        cropManager.PlantCrop(testCrop);
        KillCropDirectly();
        Assert.IsTrue(cropManager.IsDead);
    }

    [Test]
    public void KillCrop_DoesNothing_WhenNoCropPlanted()
    {
        Assert.DoesNotThrow(() => KillCropDirectly());
        Assert.IsFalse(cropManager.IsDead);
    }

    [Test]
    public void KillCrop_ClearsReadyForHarvest()
    {
        GrowToHarvest();
        Assert.IsTrue(cropManager.IsReadyForHarvest);

        KillCropDirectly();

        Assert.IsFalse(cropManager.IsReadyForHarvest);
    }

    // =========================================================================
    // EVENTS
    // =========================================================================

    [Test]
    public void OnCropGrown_Fires_WhenStageAdvances()
    {
        testCrop.daysPerStage = 1;
        cropManager.PlantCrop(testCrop);
        cropManager.WaterCrop();

        bool fired = false;
        cropManager.OnCropGrown += _ => fired = true;

        SimulateDayChange();

        Assert.IsTrue(fired, "OnCropGrown should fire when a growth stage is completed.");
    }

    [Test]
    public void OnCropReadyForHarvest_Fires_WhenFullyGrown()
    {
        testCrop.daysPerStage = 1;
        testCrop.growthStages = new Sprite[2]; // harvest at stage 1

        cropManager.PlantCrop(testCrop);

        bool harvestFired = false;
        cropManager.OnCropReadyForHarvest += _ => harvestFired = true;

        // Advance through all stages
        for (int i = 0; i < testCrop.TotalStages; i++)
        {
            cropManager.WaterCrop();
            SimulateDayChange();
        }

        Assert.IsTrue(harvestFired, "OnCropReadyForHarvest should fire when all stages complete.");
    }

    [Test]
    public void OnCropDied_Fires_WhenKillCropCalled()
    {
        cropManager.PlantCrop(testCrop);

        bool firedDied = false;
        cropManager.OnCropDied += _ => firedDied = true;

        KillCropDirectly();

        Assert.IsTrue(firedDied, "OnCropDied should fire when KillCrop() is called.");
    }

    [Test]
    public void OnCropDied_Fires_WhenDiesFromDehydration()
    {
        testCrop.requiresWater = true;
        testCrop.maxDaysWithoutWater = 2; // dies after initial(1) + 1 day change = 2

        cropManager.PlantCrop(testCrop);

        bool firedDied = false;
        cropManager.OnCropDied += _ => firedDied = true;

        SimulateDayChange(); // daysSinceLastWatered reaches threshold

        Assert.IsTrue(firedDied, "OnCropDied should fire when the crop dies from dehydration.");
    }

    [Test]
    public void OnCropHarvested_Fires_WhenCropIsHarvested()
    {
        GrowToHarvest();

        bool firedHarvested = false;
        cropManager.OnCropHarvested += _ => firedHarvested = true;

        cropManager.HarvestCrop();

        Assert.IsTrue(firedHarvested, "OnCropHarvested should fire when HarvestCrop() succeeds.");
    }

    [Test]
    public void OnCropGrown_DoesNotFire_WhenNotWatered()
    {
        testCrop.requiresWater = true;
        testCrop.maxDaysWithoutWater = 5;
        cropManager.PlantCrop(testCrop);

        bool fired = false;
        cropManager.OnCropGrown += _ => fired = true;

        // No watering — growth should be blocked
        SimulateDayChange();

        Assert.IsFalse(fired, "OnCropGrown should NOT fire when the crop was not watered.");
    }

    // =========================================================================
    // NON-WATER CROPS (requiresWater = false)
    // =========================================================================

    [Test]
    public void NonWaterCrop_AdvancesGrowth_WithoutWatering()
    {
        testCrop.requiresWater = false;
        testCrop.daysPerStage = 1;
        cropManager.PlantCrop(testCrop);

        int stageBefore = cropManager.CurrentGrowthStage;
        SimulateDayChange(); // no WaterCrop() call

        Assert.Greater(cropManager.CurrentGrowthStage, stageBefore,
            "A crop that does not require water should grow automatically each day.");
    }

    [Test]
    public void NonWaterCrop_DoesNotDie_WhenNotWatered()
    {
        testCrop.requiresWater = false;
        cropManager.PlantCrop(testCrop);

        for (int i = 0; i < 10; i++) SimulateDayChange();

        Assert.IsFalse(cropManager.IsDead, "A crop that does not require water should never die from dehydration.");
    }

    // =========================================================================
    // REGROWTH
    // =========================================================================

    [Test]
    public void RegrowthCrop_ResetsToStageZero_AfterHarvest()
    {
        testCrop.regrowsAfterHarvest = true;
        testCrop.maxRegrowths = 0; // 0 = unlimited

        GrowToHarvest();
        Assert.IsTrue(cropManager.IsReadyForHarvest);

        cropManager.HarvestCrop();

        Assert.IsTrue(cropManager.HasCrop, "Crop should still exist after regrowth harvest.");
        Assert.AreEqual(0, cropManager.CurrentGrowthStage, "Regrowth should reset stage to 0.");
        Assert.IsFalse(cropManager.IsReadyForHarvest, "Regrowth crop should not be immediately ready.");
    }

    [Test]
    public void RegrowthCrop_RemovesCrop_WhenMaxRegrowthsReached()
    {
        testCrop.regrowsAfterHarvest = true;
        testCrop.maxRegrowths = 1; // only one regrowth allowed

        // First harvest (regrowth 0 → 1)
        GrowToHarvest();
        cropManager.HarvestCrop();
        Assert.IsTrue(cropManager.HasCrop, "Crop should exist after first harvest (one regrowth allowed).");

        // Grow again and harvest (regrowth 1 → limit reached)
        for (int i = 0; i < testCrop.TotalStages; i++)
        {
            cropManager.WaterCrop();
            SimulateDayChange();
        }
        cropManager.HarvestCrop();

        Assert.IsFalse(cropManager.HasCrop, "Crop should be removed after maxRegrowths is reached.");
    }

    // =========================================================================
    // GET CROP INFO
    // =========================================================================

    [Test]
    public void GetCropInfo_ReturnsNoCropMessage_WhenNoCropPlanted()
    {
        Assert.AreEqual("No crop planted", cropManager.GetCropInfo());
    }

    [Test]
    public void GetCropInfo_ContainsCropName_WhenGrowing()
    {
        cropManager.PlantCrop(testCrop);
        string info = cropManager.GetCropInfo();
        StringAssert.Contains(testCrop.cropName, info);
    }

    [Test]
    public void GetCropInfo_ContainsDEAD_WhenCropIsDead()
    {
        cropManager.PlantCrop(testCrop);
        KillCropDirectly();
        StringAssert.Contains("DEAD", cropManager.GetCropInfo());
    }

    [Test]
    public void GetCropInfo_ContainsReadyForHarvest_WhenFullyGrown()
    {
        GrowToHarvest();
        StringAssert.Contains("Ready", cropManager.GetCropInfo());
    }

    // =========================================================================
    // ISAVEABLE — SAVE
    // =========================================================================

    [Test]
    public void SaveData_DoesNotThrow_WhenGameDataIsNull()
    {
        cropManager.PlantCrop(testCrop);
        Assert.DoesNotThrow(() => cropManager.SaveData(null));
    }

    [Test]
    public void SaveData_DoesNothing_WhenNoCropPlanted()
    {
        var gd = new GameData();
        cropManager.SaveData(gd);
        Assert.AreEqual(0, gd.farmingData.activeCrops.Count,
            "SaveData should not write anything when no crop is planted.");
    }

    [Test]
    public void SaveData_AddsOneEntry_WhenCropIsPlanted()
    {
        cropManager.PlantCrop(testCrop);
        var gd = new GameData();
        cropManager.SaveData(gd);
        Assert.AreEqual(1, gd.farmingData.activeCrops.Count);
    }

    [Test]
    public void SaveData_WritesCorrectCropType()
    {
        cropManager.PlantCrop(testCrop);
        var gd = new GameData();
        cropManager.SaveData(gd);
        Assert.AreEqual(testCrop.cropName, gd.farmingData.activeCrops[0].cropType);
    }

    [Test]
    public void SaveData_WritesCorrectGrowthStage_AfterAdvancing()
    {
        testCrop.daysPerStage = 1;
        cropManager.PlantCrop(testCrop);
        cropManager.WaterCrop();
        SimulateDayChange(); // stage 0 → 1

        var gd = new GameData();
        cropManager.SaveData(gd);

        Assert.AreEqual(1, gd.farmingData.activeCrops[0].growthStage);
    }

    [Test]
    public void SaveData_WritesIsWatered_Correctly()
    {
        cropManager.PlantCrop(testCrop);
        cropManager.WaterCrop();

        var gd = new GameData();
        cropManager.SaveData(gd);

        Assert.IsTrue(gd.farmingData.activeCrops[0].isWatered);
    }

    [Test]
    public void SaveData_WritesIsReady_WhenCropIsReadyForHarvest()
    {
        GrowToHarvest();
        var gd = new GameData();
        cropManager.SaveData(gd);
        Assert.IsTrue(gd.farmingData.activeCrops[0].isReady);
    }

    // =========================================================================
    // ISAVEABLE — LOAD
    // =========================================================================

    [Test]
    public void LoadData_DoesNotThrow_WhenGameDataIsNull()
    {
        Assert.DoesNotThrow(() => cropManager.LoadData(null));
    }

    [Test]
    public void LoadData_SilentlyIgnores_WhenNoMatchingPosition()
    {
        var gd = new GameData(); // empty — no activeCrops
        Assert.DoesNotThrow(() => cropManager.LoadData(gd));
        Assert.IsFalse(cropManager.HasCrop, "CropGrowthManager should have no crop after loading empty data.");
    }

    // =========================================================================
    // DAYS UNTIL NEXT STAGE (floating status text support)
    // =========================================================================

    [Test]
    public void DaysUntilNextStage_IsZero_WhenNoCropPlanted()
    {
        Assert.AreEqual(0, cropManager.DaysUntilNextStage);
    }

    [Test]
    public void DaysUntilNextStage_EqualsDaysPerStage_RightAfterPlanting()
    {
        testCrop.daysPerStage = 3;
        cropManager.PlantCrop(testCrop);
        Assert.AreEqual(3, cropManager.DaysUntilNextStage);
    }

    [Test]
    public void DaysUntilNextStage_DecreasesEachWateredDay()
    {
        testCrop.daysPerStage = 3;
        cropManager.PlantCrop(testCrop);
        cropManager.WaterCrop();
        SimulateDayChange();

        Assert.AreEqual(2, cropManager.DaysUntilNextStage);
    }

    [Test]
    public void DaysUntilNextStage_IsZero_WhenReadyForHarvest()
    {
        GrowToHarvest();
        Assert.AreEqual(0, cropManager.DaysUntilNextStage);
    }

    [Test]
    public void DaysUntilNextStage_IsZero_WhenCropIsDead()
    {
        cropManager.PlantCrop(testCrop);
        KillCropDirectly();
        Assert.AreEqual(0, cropManager.DaysUntilNextStage);
    }
}
