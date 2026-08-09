using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using SowurShield.Core;

namespace SowurShield.Tests
{

/// <summary>
/// Pins the shipped crop economy. Unlike most of the suite these tests read the
/// real ScriptableObjects under Resources/FarmingData rather than building items
/// in memory, because the defect being guarded against was in the asset data,
/// not in the code that reads it.
///
/// The Aug 2026 audit found crop profit ranging from 1.05 gold/day (Cabbage) to
/// 17.64 (Pumpkin) — a 16.8x spread that made four of the five crops pointless.
/// The cause was that CropData.baseValue, which the crop authoring tool writes,
/// is never read by any selling path; the price a player actually receives comes
/// from harvestItem.baseValue, and the two had drifted far apart (Tomato: 80 vs 9).
/// </summary>
public class CropEconomyBalanceTests
{
    // The band the crops were retuned into on 2026-08-09. Deliberately a little
    // wider than the actual 4.00-5.94 so that ordinary tuning does not trip the
    // test — it is here to catch a crop being left an order of magnitude out,
    // not to freeze the numbers.
    // The CropData assets live in Resources/Crops. (The harvest *items* they
    // point at sit under Resources/FarmingData/Crops — a different folder, which
    // is an easy thing to get wrong when writing a test like this.)
    private const string CropResourceFolder = "Crops";

    private const float MinProfitPerDay = 3.0f;
    private const float MaxProfitPerDay = 8.0f;
    private const float MaxSpreadRatio  = 3.0f;

    private class CropEconomy
    {
        public string Name;
        public float ProfitPerDay;
    }

    /// <summary>
    /// Mirrors what the player actually experiences: buy a seed at SeedShopUI's
    /// price (baseValue * 3), wait growthStages * daysPerStage, harvest the
    /// average yield and sell it at harvestItem.baseValue.
    /// </summary>
    private List<CropEconomy> LoadCropEconomies()
    {
        var result = new List<CropEconomy>();

        foreach (var crop in Resources.LoadAll<CropData>(CropResourceFolder))
        {
            if (crop == null || crop.harvestItem == null) continue;

            int days = crop.TotalGrowthDays;
            if (days <= 0) continue; // MysterySeed has no growth stages of its own

            float avgYield = (crop.minYield + crop.maxYield) / 2f;
            int seedPrice = crop.seedItem != null
                ? Mathf.Max(1, crop.seedItem.baseValue * 3)
                : 0;

            result.Add(new CropEconomy
            {
                Name = crop.name,
                ProfitPerDay = (crop.harvestItem.baseValue * avgYield - seedPrice) / days
            });
        }

        return result;
    }

    [Test]
    public void EveryCrop_EarnsWithinTheProfitBand()
    {
        var crops = LoadCropEconomies();
        Assert.IsNotEmpty(crops, $"No CropData assets loaded from Resources/{CropResourceFolder}.");

        foreach (var crop in crops)
        {
            Assert.GreaterOrEqual(crop.ProfitPerDay, MinProfitPerDay,
                $"{crop.Name} earns {crop.ProfitPerDay:F2} gold/day, below the {MinProfitPerDay} floor. " +
                "Raise its harvestItem.baseValue — CropData.baseValue is not read at runtime.");
            Assert.LessOrEqual(crop.ProfitPerDay, MaxProfitPerDay,
                $"{crop.Name} earns {crop.ProfitPerDay:F2} gold/day, above the {MaxProfitPerDay} ceiling, " +
                "which makes the other crops pointless to plant.");
        }
    }

    [Test]
    public void BestCrop_DoesNotDwarfWorstCrop()
    {
        var crops = LoadCropEconomies();
        Assert.IsNotEmpty(crops);

        float best = float.MinValue, worst = float.MaxValue;
        string bestName = "", worstName = "";
        foreach (var crop in crops)
        {
            if (crop.ProfitPerDay > best) { best = crop.ProfitPerDay; bestName = crop.Name; }
            if (crop.ProfitPerDay < worst) { worst = crop.ProfitPerDay; worstName = crop.Name; }
        }

        Assert.Greater(worst, 0f, $"{worstName} is not profitable at all.");
        Assert.LessOrEqual(best / worst, MaxSpreadRatio,
            $"{bestName} ({best:F2}/day) earns {best / worst:F1}x more than {worstName} ({worst:F2}/day). " +
            "The Aug 2026 audit found this at 16.8x, which reduced farming to planting one crop.");
    }

    /// <summary>
    /// The trap that caused the imbalance: the authoring tool writes
    /// CropData.baseValue, but selling reads harvestItem.baseValue. If the two
    /// silently diverge again, whoever tunes the "obvious" field will be tuning
    /// nothing. This asserts they stay within sight of each other.
    /// </summary>
    [Test]
    public void CropDataBaseValue_HasNotDriftedFarFromThePriceThatIsActuallyPaid()
    {
        foreach (var crop in Resources.LoadAll<CropData>(CropResourceFolder))
        {
            if (crop == null || crop.harvestItem == null) continue;
            if (crop.TotalGrowthDays <= 0) continue;

            int authored = crop.baseValue;
            int actual = crop.harvestItem.baseValue;
            if (authored <= 0 || actual <= 0) continue;

            float ratio = Mathf.Max(authored, actual) / (float)Mathf.Min(authored, actual);
            Assert.LessOrEqual(ratio, 4f,
                $"{crop.name}: CropData.baseValue is {authored} but the harvest item sells for {actual} " +
                $"({ratio:F1}x apart). CropData.baseValue is never read at runtime, so the authored " +
                "number is decorative — reconcile them or the next person will tune the wrong one.");
        }
    }
}

}
