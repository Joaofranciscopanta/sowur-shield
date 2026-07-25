using NUnit.Framework;
using SowurShield.Animals;

/// <summary>
/// Edit Mode tests for AnimalIllness — the neglect/illness state machine extracted from
/// Animal.cs. Pure C# class, so no scene, GameObject or ScriptableObject is required.
/// </summary>
public class AnimalIllnessTests
{
    private const int Threshold = 3;

    private AnimalIllness illness;

    [SetUp]
    public void SetUp()
    {
        illness = new AnimalIllness();
    }

    // =========================================================================
    // INITIAL STATE
    // =========================================================================

    [Test]
    public void NewAnimal_IsHealthyWithNoNeglect()
    {
        Assert.IsFalse(illness.IsIll);
        Assert.AreEqual(0, illness.NeglectDays);
    }

    // =========================================================================
    // NEGLECT ACCUMULATION
    // =========================================================================

    [Test]
    public void UpdateNeglect_NotCaredFor_IncrementsNeglectDays()
    {
        illness.UpdateNeglect(false, Threshold);

        Assert.AreEqual(1, illness.NeglectDays);
        Assert.IsFalse(illness.IsIll, "One neglected day is below the threshold.");
    }

    [Test]
    public void UpdateNeglect_BelowThreshold_StaysHealthy()
    {
        for (int i = 0; i < Threshold - 1; i++)
            illness.UpdateNeglect(false, Threshold);

        Assert.AreEqual(Threshold - 1, illness.NeglectDays);
        Assert.IsFalse(illness.IsIll);
    }

    [Test]
    public void UpdateNeglect_ReachesThreshold_BecomesIll()
    {
        for (int i = 0; i < Threshold; i++)
            illness.UpdateNeglect(false, Threshold);

        Assert.AreEqual(Threshold, illness.NeglectDays);
        Assert.IsTrue(illness.IsIll);
    }

    [Test]
    public void UpdateNeglect_PastThreshold_StaysIllAndKeepsCounting()
    {
        for (int i = 0; i < Threshold + 2; i++)
            illness.UpdateNeglect(false, Threshold);

        Assert.AreEqual(Threshold + 2, illness.NeglectDays);
        Assert.IsTrue(illness.IsIll);
    }

    // =========================================================================
    // CARE RESETS THE STREAK
    // =========================================================================

    [Test]
    public void UpdateNeglect_CaredFor_ResetsNeglectStreak()
    {
        illness.UpdateNeglect(false, Threshold);
        illness.UpdateNeglect(false, Threshold);
        illness.UpdateNeglect(true, Threshold);

        Assert.AreEqual(0, illness.NeglectDays);
        Assert.IsFalse(illness.IsIll);
    }

    [Test]
    public void UpdateNeglect_CareAfterIllness_ResetsStreakButStaysIll()
    {
        for (int i = 0; i < Threshold; i++)
            illness.UpdateNeglect(false, Threshold);

        illness.UpdateNeglect(true, Threshold);

        Assert.AreEqual(0, illness.NeglectDays);
        Assert.IsTrue(illness.IsIll, "Illness must only be cleared by medicine, not by care.");
    }

    [Test]
    public void UpdateNeglect_AlternatingCare_NeverBecomesIll()
    {
        for (int i = 0; i < Threshold * 3; i++)
            illness.UpdateNeglect(i % 2 == 0, Threshold);

        Assert.IsFalse(illness.IsIll);
    }

    // =========================================================================
    // CURE
    // =========================================================================

    [Test]
    public void Cure_ClearsIllnessAndNeglectDays()
    {
        for (int i = 0; i < Threshold; i++)
            illness.UpdateNeglect(false, Threshold);

        illness.Cure();

        Assert.IsFalse(illness.IsIll);
        Assert.AreEqual(0, illness.NeglectDays);
    }

    [Test]
    public void Cure_OnHealthyAnimal_IsHarmless()
    {
        illness.Cure();

        Assert.IsFalse(illness.IsIll);
        Assert.AreEqual(0, illness.NeglectDays);
    }

    [Test]
    public void Cure_ThenNeglectAgain_RequiresFullThresholdToRelapse()
    {
        for (int i = 0; i < Threshold; i++)
            illness.UpdateNeglect(false, Threshold);
        illness.Cure();

        for (int i = 0; i < Threshold - 1; i++)
            illness.UpdateNeglect(false, Threshold);
        Assert.IsFalse(illness.IsIll, "Relapse must take a fresh full streak.");

        illness.UpdateNeglect(false, Threshold);
        Assert.IsTrue(illness.IsIll);
    }

    // =========================================================================
    // SAVE / LOAD ROUND-TRIP
    // =========================================================================

    [Test]
    public void RestoreState_RestoresBothFields()
    {
        illness.RestoreState(2, true);

        Assert.AreEqual(2, illness.NeglectDays);
        Assert.IsTrue(illness.IsIll);
    }

    [Test]
    public void RestoreState_ClampsNegativeNeglectDaysToZero()
    {
        illness.RestoreState(-5, false);

        Assert.AreEqual(0, illness.NeglectDays);
    }

    [Test]
    public void RestoreState_ThenOneNeglectedDay_ContinuesFromRestoredStreak()
    {
        illness.RestoreState(Threshold - 1, false);

        illness.UpdateNeglect(false, Threshold);

        Assert.IsTrue(illness.IsIll, "A restored streak must keep counting toward the threshold.");
    }

    // =========================================================================
    // EDGE CASES
    // =========================================================================

    [Test]
    public void UpdateNeglect_ThresholdOfOne_BecomesIllAfterASingleDay()
    {
        illness.UpdateNeglect(false, 1);

        Assert.IsTrue(illness.IsIll);
    }

    [Test]
    public void UpdateNeglect_ZeroOrNegativeThreshold_BecomesIllImmediately()
    {
        illness.UpdateNeglect(false, 0);

        Assert.IsTrue(illness.IsIll,
            "A misconfigured AnimalData (threshold <= 0) should fail loudly in-game, not silently never trigger.");
    }
}
