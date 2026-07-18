using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SowurShield.Core;

/// <summary>
/// Play Mode integration tests for CropGrowthManager.
/// Tests planting, watering, harvesting, and day-advance simulation.
/// Day changes are simulated by invoking OnDayChanged via reflection,
/// since the event is private and normally triggered by GameTimeController.
/// </summary>
public class CropGrowthPlayModeTests
{
    private GameObject soilGo;
    private CropGrowthManager cropManager;
    private CropData testCrop;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Create a minimal crop definition
        testCrop = ScriptableObject.CreateInstance<CropData>();
        testCrop.cropName = "TestCarrot";
        testCrop.daysPerStage = 1;
        testCrop.growthStages = new Sprite[3]; // 3 stages (0, 1, 2)
        testCrop.requiresWater = true;
        testCrop.maxDaysWithoutWater = 3;
        testCrop.minYield = 1;
        testCrop.maxYield = 3;
        testCrop.growsInAllSeasons = true;
        testCrop.regrowsAfterHarvest = false;

        soilGo = new GameObject("SoilBlock");
        cropManager = soilGo.AddComponent<CropGrowthManager>();

        yield return null; // let Awake + Start complete
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(testCrop);
        Object.DestroyImmediate(soilGo);
        yield return null;
    }

    /// <summary>
    /// Simulate a day passing by invoking the private OnDayChanged method.
    /// </summary>
    private void SimulateDayChange()
    {
        var method = typeof(CropGrowthManager).GetMethod(
            "OnDayChanged",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "CropGrowthManager must have a private OnDayChanged() method");
        method.Invoke(cropManager, null);
    }

    // =========================================================================
    // INITIAL STATE
    // =========================================================================

    [UnityTest]
    public IEnumerator NewManager_HasNoCrop()
    {
        yield return null;
        Assert.IsFalse(cropManager.HasCrop);
    }

    [UnityTest]
    public IEnumerator NewManager_IsNotReadyForHarvest()
    {
        yield return null;
        Assert.IsFalse(cropManager.IsReadyForHarvest);
    }

    [UnityTest]
    public IEnumerator NewManager_GrowthProgress_IsZero()
    {
        yield return null;
        Assert.AreEqual(0f, cropManager.GrowthProgress, 0.001f);
    }

    // =========================================================================
    // PLANTING
    // =========================================================================

    [UnityTest]
    public IEnumerator PlantCrop_ReturnsTrueAndSetsCropData()
    {
        bool result = cropManager.PlantCrop(testCrop);
        yield return null;

        Assert.IsTrue(result);
        Assert.IsTrue(cropManager.HasCrop);
        Assert.AreSame(testCrop, cropManager.CurrentCrop);
    }

    [UnityTest]
    public IEnumerator PlantCrop_ReturnsFalse_WhenCropAlreadyPlanted()
    {
        cropManager.PlantCrop(testCrop);
        bool second = cropManager.PlantCrop(testCrop);
        yield return null;

        Assert.IsFalse(second);
    }

    [UnityTest]
    public IEnumerator PlantCrop_ReturnsFalse_WhenCropDataIsNull()
    {
        bool result = cropManager.PlantCrop(null);
        yield return null;

        Assert.IsFalse(result);
        Assert.IsFalse(cropManager.HasCrop);
    }

    [UnityTest]
    public IEnumerator NewCrop_StartsAtStageZero()
    {
        cropManager.PlantCrop(testCrop);
        yield return null;

        Assert.AreEqual(0, cropManager.CurrentGrowthStage);
    }

    [UnityTest]
    public IEnumerator NewCrop_IsNotReadyForHarvest()
    {
        cropManager.PlantCrop(testCrop);
        yield return null;

        Assert.IsFalse(cropManager.IsReadyForHarvest);
    }

    [UnityTest]
    public IEnumerator NewCrop_IsNotDead()
    {
        cropManager.PlantCrop(testCrop);
        yield return null;

        Assert.IsFalse(cropManager.IsDead);
    }

    // =========================================================================
    // WATERING
    // =========================================================================

    [UnityTest]
    public IEnumerator WaterCrop_SetsIsWateredTrue()
    {
        cropManager.PlantCrop(testCrop);
        cropManager.WaterCrop();
        yield return null;

        Assert.IsTrue(cropManager.IsWatered);
    }

    [UnityTest]
    public IEnumerator WaterCrop_DoesNothing_WhenNoCropPlanted()
    {
        cropManager.WaterCrop(); // should not throw
        yield return null;

        Assert.IsFalse(cropManager.IsWatered);
    }

    // =========================================================================
    // DAY ADVANCEMENT & GROWTH
    // =========================================================================

    [UnityTest]
    public IEnumerator WateredCrop_AdvancesGrowthStageAfterOneDayPerStage()
    {
        testCrop.daysPerStage = 1;
        cropManager.PlantCrop(testCrop);
        cropManager.WaterCrop();

        int stageBefore = cropManager.CurrentGrowthStage;
        SimulateDayChange();
        yield return null;

        Assert.Greater(cropManager.CurrentGrowthStage, stageBefore,
            "Crop should advance a stage after one watered day");
    }

    [UnityTest]
    public IEnumerator UnwateredCrop_DoesNotAdvanceGrowthStage()
    {
        cropManager.PlantCrop(testCrop);
        // Do NOT water

        int stageBefore = cropManager.CurrentGrowthStage;
        SimulateDayChange();
        yield return null;

        Assert.AreEqual(stageBefore, cropManager.CurrentGrowthStage,
            "Unwatered crop should not advance");
    }

    [UnityTest]
    public IEnumerator FullyGrownCrop_IsReadyForHarvest()
    {
        // 3 stages, 1 day/stage → water and advance 3 times
        testCrop.daysPerStage = 1;
        testCrop.growthStages = new Sprite[3];
        cropManager.PlantCrop(testCrop);

        for (int i = 0; i < 3; i++)
        {
            cropManager.WaterCrop();
            SimulateDayChange();
            yield return null;
        }

        Assert.IsTrue(cropManager.IsReadyForHarvest,
            "Crop should be ready after all growth stages complete");
    }

    [UnityTest]
    public IEnumerator CropDies_AfterTooManyDaysWithoutWater()
    {
        testCrop.requiresWater = true;
        testCrop.maxDaysWithoutWater = 2;
        cropManager.PlantCrop(testCrop);
        // Never water it

        for (int i = 0; i < 3; i++)
        {
            SimulateDayChange();
            yield return null;
        }

        Assert.IsTrue(cropManager.IsDead, "Crop should die after maxDaysWithoutWater exceeded");
    }

    [UnityTest]
    public IEnumerator WateredCrop_ReturnsNotWatered_AfterDayChange()
    {
        cropManager.PlantCrop(testCrop);
        cropManager.WaterCrop();
        Assert.IsTrue(cropManager.IsWatered);

        SimulateDayChange();
        yield return null;

        Assert.IsFalse(cropManager.IsWatered,
            "Water should reset at the start of each day");
    }

    // =========================================================================
    // HARVESTING
    // =========================================================================

    [UnityTest]
    public IEnumerator HarvestCrop_ReturnsZero_WhenNotReady()
    {
        cropManager.PlantCrop(testCrop);
        yield return null;

        int yield_ = cropManager.HarvestCrop();
        Assert.AreEqual(0, yield_);
    }

    [UnityTest]
    public IEnumerator HarvestCrop_ReturnsPositiveYield_WhenReady()
    {
        testCrop.daysPerStage = 1;
        testCrop.growthStages = new Sprite[3];
        testCrop.minYield = 1;
        testCrop.maxYield = 5;
        cropManager.PlantCrop(testCrop);

        for (int i = 0; i < 3; i++)
        {
            cropManager.WaterCrop();
            SimulateDayChange();
            yield return null;
        }

        Assert.IsTrue(cropManager.IsReadyForHarvest);
        int harvest = cropManager.HarvestCrop();
        Assert.Greater(harvest, 0);
    }

    [UnityTest]
    public IEnumerator HarvestCrop_RemovesCrop_WhenNotRegrowable()
    {
        testCrop.daysPerStage = 1;
        testCrop.growthStages = new Sprite[3];
        testCrop.regrowsAfterHarvest = false;
        cropManager.PlantCrop(testCrop);

        for (int i = 0; i < 3; i++)
        {
            cropManager.WaterCrop();
            SimulateDayChange();
            yield return null;
        }

        cropManager.HarvestCrop();
        yield return null;

        Assert.IsFalse(cropManager.HasCrop, "Non-regrowable crop should be removed after harvest");
    }

    // =========================================================================
    // REMOVE CROP
    // =========================================================================

    [UnityTest]
    public IEnumerator RemoveCrop_ClearsHasCrop()
    {
        cropManager.PlantCrop(testCrop);
        yield return null;

        cropManager.RemoveCrop();
        yield return null;

        Assert.IsFalse(cropManager.HasCrop);
    }

    // =========================================================================
    // GROWTH PROGRESS
    // =========================================================================

    [UnityTest]
    public IEnumerator GrowthProgress_IsZero_BeforePlanting()
    {
        yield return null;
        Assert.AreEqual(0f, cropManager.GrowthProgress, 0.001f);
    }

    [UnityTest]
    public IEnumerator GrowthProgress_IncreasesAfterAdvancing()
    {
        testCrop.daysPerStage = 1;
        testCrop.growthStages = new Sprite[4];
        cropManager.PlantCrop(testCrop);
        yield return null;

        float progressBefore = cropManager.GrowthProgress;
        cropManager.WaterCrop();
        SimulateDayChange();
        yield return null;

        Assert.Greater(cropManager.GrowthProgress, progressBefore);
    }
}
