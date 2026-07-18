using NUnit.Framework;
using UnityEngine;
using SowurShield.Core;

/// <summary>
/// Edit Mode tests for CropData ScriptableObject.
/// Tests property calculations and pure logic — no scene required.
/// </summary>
public class CropDataTests
{
    private CropData crop;

    [SetUp]
    public void SetUp()
    {
        crop = ScriptableObject.CreateInstance<CropData>();
        crop.cropName = "TestCarrot";
        crop.daysPerStage = 2;
        crop.growthStages = new Sprite[4]; // 4 stages, nulls are fine for logic tests
        crop.minYield = 2;
        crop.maxYield = 5;
        crop.growsInAllSeasons = true;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(crop);
    }

    // =========================================================================
    // TOTAL STAGES
    // =========================================================================

    [Test]
    public void TotalStages_EqualsGrowthSpritesArrayLength()
    {
        Assert.AreEqual(4, crop.TotalStages);
    }

    [Test]
    public void TotalStages_IsZero_WhenGrowthStagesNull()
    {
        crop.growthStages = null;
        Assert.AreEqual(0, crop.TotalStages);
    }

    [Test]
    public void TotalStages_IsZero_WhenGrowthStagesEmpty()
    {
        crop.growthStages = new Sprite[0];
        Assert.AreEqual(0, crop.TotalStages);
    }

    // =========================================================================
    // TOTAL GROWTH DAYS
    // =========================================================================

    [Test]
    public void TotalGrowthDays_IsStagesTimesPerStageDays()
    {
        // 4 stages * 2 days/stage = 8 days
        Assert.AreEqual(8, crop.TotalGrowthDays);
    }

    [Test]
    public void TotalGrowthDays_IsZero_WhenNoStages()
    {
        crop.growthStages = null;
        Assert.AreEqual(0, crop.TotalGrowthDays);
    }

    [Test]
    public void TotalGrowthDays_ScalesCorrectlyWithDaysPerStage()
    {
        crop.growthStages = new Sprite[3];
        crop.daysPerStage = 5;
        Assert.AreEqual(15, crop.TotalGrowthDays);
    }

    // =========================================================================
    // IS READY FOR HARVEST
    // =========================================================================

    [Test]
    public void IsReadyForHarvest_False_WhenBelowLastStage()
    {
        // 4 stages (0-3), last stage is 3
        Assert.IsFalse(crop.IsReadyForHarvest(0));
        Assert.IsFalse(crop.IsReadyForHarvest(1));
        Assert.IsFalse(crop.IsReadyForHarvest(2));
    }

    [Test]
    public void IsReadyForHarvest_True_AtLastStage()
    {
        Assert.IsTrue(crop.IsReadyForHarvest(3));
    }

    [Test]
    public void IsReadyForHarvest_True_BeyondLastStage()
    {
        Assert.IsTrue(crop.IsReadyForHarvest(10));
    }

    // =========================================================================
    // IS VALID SEASON
    // =========================================================================

    [Test]
    public void IsValidSeason_True_ForAllSeasons_WhenGrowsInAllSeasons()
    {
        crop.growsInAllSeasons = true;
        Assert.IsTrue(crop.IsValidSeason(Season.Spring));
        Assert.IsTrue(crop.IsValidSeason(Season.Summer));
        Assert.IsTrue(crop.IsValidSeason(Season.Fall));
        Assert.IsTrue(crop.IsValidSeason(Season.Winter));
    }

    [Test]
    public void IsValidSeason_True_ForListedSeason()
    {
        crop.growsInAllSeasons = false;
        crop.validSeasons = new Season[] { Season.Spring, Season.Summer };
        Assert.IsTrue(crop.IsValidSeason(Season.Spring));
        Assert.IsTrue(crop.IsValidSeason(Season.Summer));
    }

    [Test]
    public void IsValidSeason_False_ForUnlistedSeason()
    {
        crop.growsInAllSeasons = false;
        crop.validSeasons = new Season[] { Season.Spring, Season.Summer };
        Assert.IsFalse(crop.IsValidSeason(Season.Fall));
        Assert.IsFalse(crop.IsValidSeason(Season.Winter));
    }

    [Test]
    public void IsValidSeason_False_WhenValidSeasonsIsEmpty_AndNotAllSeasons()
    {
        crop.growsInAllSeasons = false;
        crop.validSeasons = new Season[0];
        Assert.IsFalse(crop.IsValidSeason(Season.Spring));
    }

    // =========================================================================
    // GET SPRITE FOR STAGE
    // =========================================================================

    [Test]
    public void GetSpriteForStage_ReturnsNull_WhenGrowthStagesIsNull()
    {
        crop.growthStages = null;
        Assert.IsNull(crop.GetSpriteForStage(0));
    }

    [Test]
    public void GetSpriteForStage_ReturnsNull_WhenStageNegative()
    {
        Assert.IsNull(crop.GetSpriteForStage(-1));
    }

    [Test]
    public void GetSpriteForStage_ReturnsNull_WhenStageOutOfBounds()
    {
        Assert.IsNull(crop.GetSpriteForStage(100));
    }

    [Test]
    public void GetSpriteForStage_ReturnsCorrectSprite()
    {
        var sprites = new Sprite[]
        {
            Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f),
            Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f),
        };
        crop.growthStages = sprites;

        Assert.AreSame(sprites[0], crop.GetSpriteForStage(0));
        Assert.AreSame(sprites[1], crop.GetSpriteForStage(1));

        foreach (var s in sprites) Object.DestroyImmediate(s);
    }

    // =========================================================================
    // GET RANDOM YIELD
    // =========================================================================

    [Test]
    public void GetRandomYield_AlwaysWithinMinMaxRange()
    {
        crop.minYield = 2;
        crop.maxYield = 6;

        for (int i = 0; i < 200; i++)
        {
            int yield = crop.GetRandomYield();
            Assert.GreaterOrEqual(yield, 2, $"Yield {yield} was below minYield on iteration {i}");
            Assert.LessOrEqual(yield, 6, $"Yield {yield} exceeded maxYield on iteration {i}");
        }
    }

    [Test]
    public void GetRandomYield_ExactlyMinYield_WhenMinEqualsMax()
    {
        crop.minYield = 3;
        crop.maxYield = 3;
        Assert.AreEqual(3, crop.GetRandomYield());
    }

    // =========================================================================
    // MYSTERY SEED ROLLING
    // =========================================================================

    [Test]
    public void RollMysteryOutcome_ReturnsNull_WhenNoOutcomesConfigured()
    {
        crop.possibleOutcomes = null;
        Assert.IsNull(crop.RollMysteryOutcome());

        crop.possibleOutcomes = new CropData[0];
        Assert.IsNull(crop.RollMysteryOutcome());
    }

    [Test]
    public void RollMysteryOutcome_FallsBackToFullPool_WhenNothingUnlocked()
    {
        // No SaveManager in EditMode tests, so CropUnlockTracker.IsUnlocked always
        // returns false — RollMysteryOutcome must still return a valid outcome.
        var outcomeA = ScriptableObject.CreateInstance<CropData>();
        var outcomeB = ScriptableObject.CreateInstance<CropData>();
        crop.possibleOutcomes = new CropData[] { outcomeA, outcomeB };
        crop.outcomeWeights = new int[] { 1, 1 };

        for (int i = 0; i < 50; i++)
        {
            CropData rolled = crop.RollMysteryOutcome();
            Assert.IsTrue(rolled == outcomeA || rolled == outcomeB);
        }

        Object.DestroyImmediate(outcomeA);
        Object.DestroyImmediate(outcomeB);
    }

    [Test]
    public void RollMysteryOutcome_AlwaysReturnsStartsUnlockedCrop_WhenItIsOnlyEligibleOutcome()
    {
        var locked = ScriptableObject.CreateInstance<CropData>();
        locked.startsUnlocked = false;
        var unlocked = ScriptableObject.CreateInstance<CropData>();
        unlocked.startsUnlocked = true;

        crop.possibleOutcomes = new CropData[] { locked, unlocked };
        crop.outcomeWeights = new int[] { 1, 1 };

        for (int i = 0; i < 50; i++)
        {
            Assert.AreSame(unlocked, crop.RollMysteryOutcome());
        }

        Object.DestroyImmediate(locked);
        Object.DestroyImmediate(unlocked);
    }

    [Test]
    public void RollMysteryOutcome_IgnoresNullEntriesInPool()
    {
        var outcomeA = ScriptableObject.CreateInstance<CropData>();
        crop.possibleOutcomes = new CropData[] { null, outcomeA };
        crop.outcomeWeights = new int[] { 1, 1 };

        Assert.AreSame(outcomeA, crop.RollMysteryOutcome());

        Object.DestroyImmediate(outcomeA);
    }
}
