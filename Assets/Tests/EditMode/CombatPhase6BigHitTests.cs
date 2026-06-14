using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for the OnBigHit static event (TurnManager.RaiseBigHitIfQualifying),
/// fired for crits and large (>=25% max health) hits.
/// </summary>
public class CombatPhase6BigHitTests
{
    private List<Object> _cleanupList = new List<Object>();

    private T Track<T>(T obj) where T : Object
    {
        _cleanupList.Add(obj);
        return obj;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var obj in _cleanupList)
        {
            if (obj != null) Object.DestroyImmediate(obj);
        }
        _cleanupList.Clear();

        if (TurnManager.Instance != null)
            Object.DestroyImmediate(TurnManager.Instance.gameObject);

        // Clear all subscribers from the static event so tests don't leak into each other.
        var f = typeof(TurnManager).GetField("OnBigHit", BindingFlags.NonPublic | BindingFlags.Static);
        f.SetValue(null, null);
    }

    // ── reflection helpers ──────────────────────────────────────────────────

    private static void InvokeStatic(string methodName, object[] args)
    {
        var m = typeof(TurnManager).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(m, $"Static method '{methodName}' not found on TurnManager");
        m.Invoke(null, args);
    }

    private static void Subscribe(System.Action<float> handler)
    {
        typeof(TurnManager).GetField("OnBigHit", BindingFlags.NonPublic | BindingFlags.Static)
            .SetValue(null, handler);
    }

    // =========================================================================
    // Crit hits
    // =========================================================================

    [Test]
    public void RaiseBigHitIfQualifying_Crit_FiresWithCritDuration()
    {
        float? received = null;
        Subscribe(d => received = d);

        InvokeStatic("RaiseBigHitIfQualifying", new object[] { 10f, 100f, true });

        Assert.IsTrue(received.HasValue, "OnBigHit should fire for a critical hit.");
        Assert.AreEqual(0.08f, received.Value, 0.0001f);
    }

    // =========================================================================
    // Large non-crit hits (>= 25% max health)
    // =========================================================================

    [Test]
    public void RaiseBigHitIfQualifying_LargeNonCritHit_FiresWithLargeDuration()
    {
        float? received = null;
        Subscribe(d => received = d);

        // Damage == 25% of max health, non-crit.
        InvokeStatic("RaiseBigHitIfQualifying", new object[] { 25f, 100f, false });

        Assert.IsTrue(received.HasValue, "OnBigHit should fire for a hit >= 25% of max health.");
        Assert.AreEqual(0.05f, received.Value, 0.0001f);
    }

    [Test]
    public void RaiseBigHitIfQualifying_AboveThresholdNonCritHit_FiresWithLargeDuration()
    {
        float? received = null;
        Subscribe(d => received = d);

        InvokeStatic("RaiseBigHitIfQualifying", new object[] { 50f, 100f, false });

        Assert.IsTrue(received.HasValue);
        Assert.AreEqual(0.05f, received.Value, 0.0001f);
    }

    // =========================================================================
    // Small hits — no event
    // =========================================================================

    [Test]
    public void RaiseBigHitIfQualifying_SmallNonCritHit_DoesNotFire()
    {
        bool fired = false;
        Subscribe(d => fired = true);

        // Damage well below 25% of max health, non-crit.
        InvokeStatic("RaiseBigHitIfQualifying", new object[] { 5f, 100f, false });

        Assert.IsFalse(fired, "OnBigHit should not fire for a small non-crit hit.");
    }

    [Test]
    public void RaiseBigHitIfQualifying_ZeroMaxHealth_DoesNotFireForNonCrit()
    {
        bool fired = false;
        Subscribe(d => fired = true);

        InvokeStatic("RaiseBigHitIfQualifying", new object[] { 50f, 0f, false });

        Assert.IsFalse(fired, "OnBigHit should not fire for non-crit hits when target max health is 0.");
    }

    [Test]
    public void RaiseBigHitIfQualifying_CritTakesPriorityOverHealthFraction()
    {
        float? received = null;
        Subscribe(d => received = d);

        // Small damage, but crit — should still fire with the crit duration (not the large-hit duration).
        InvokeStatic("RaiseBigHitIfQualifying", new object[] { 1f, 100f, true });

        Assert.IsTrue(received.HasValue);
        Assert.AreEqual(0.08f, received.Value, 0.0001f);
    }

    [Test]
    public void RaiseBigHitIfQualifying_NoSubscribers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => InvokeStatic("RaiseBigHitIfQualifying", new object[] { 50f, 100f, true }));
    }
}

} // namespace SowurShield.Tests
