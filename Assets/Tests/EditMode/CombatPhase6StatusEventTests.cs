using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for OnStatusApplied/OnStatusExpired events on CombatUnit.
/// </summary>
public class CombatPhase6StatusEventTests
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
    }

    private CombatUnit CreateUnit(float hp = 100f)
    {
        var go = new GameObject("CombatUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = true;
        unit.InitializeAsEnemy(go.name, hp, 10f, 0f, 10f);
        return unit;
    }

    // =========================================================================
    // OnStatusApplied
    // =========================================================================

    [Test]
    public void ApplyStatusEffect_FiresOnStatusAppliedWithCorrectType()
    {
        var unit = CreateUnit();

        StatusEffectType? received = null;
        unit.OnStatusApplied += t => received = t;

        unit.ApplyStatusEffect(StatusEffectType.Burn, 5f, 3);

        Assert.IsTrue(received.HasValue);
        Assert.AreEqual(StatusEffectType.Burn, received.Value);
    }

    [Test]
    public void ApplyStatusEffect_Refresh_FiresOnStatusAppliedAgain()
    {
        var unit = CreateUnit();

        int count = 0;
        unit.OnStatusApplied += t => count++;

        unit.ApplyStatusEffect(StatusEffectType.Weakness, 0.2f, 2);
        unit.ApplyStatusEffect(StatusEffectType.Weakness, 0.2f, 3); // refresh, same type

        Assert.AreEqual(2, count, "Both the initial apply and the refresh should fire OnStatusApplied.");
    }

    [Test]
    public void ApplyStatusEffect_Poison_FiresOnStatusAppliedForEachStack()
    {
        var unit = CreateUnit();

        int count = 0;
        unit.OnStatusApplied += t => count++;

        unit.ApplyStatusEffect(StatusEffectType.Poison, 3f, 2);
        unit.ApplyStatusEffect(StatusEffectType.Poison, 3f, 2);

        Assert.AreEqual(2, count, "Each independently-stacked Poison application should fire OnStatusApplied.");
    }

    // =========================================================================
    // OnStatusExpired
    // =========================================================================

    [Test]
    public void TickStatusEffects_EffectExpires_FiresOnStatusExpiredWithCorrectType()
    {
        var unit = CreateUnit();

        StatusEffectType? received = null;
        unit.OnStatusExpired += t => received = t;

        unit.ApplyStatusEffect(StatusEffectType.Burn, 5f, 1);
        unit.TickStatusEffects(); // duration 1 -> 0, should expire

        Assert.IsTrue(received.HasValue);
        Assert.AreEqual(StatusEffectType.Burn, received.Value);
    }

    [Test]
    public void TickStatusEffects_EffectNotYetExpired_DoesNotFireOnStatusExpired()
    {
        var unit = CreateUnit();

        bool fired = false;
        unit.OnStatusExpired += t => fired = true;

        unit.ApplyStatusEffect(StatusEffectType.Burn, 5f, 2);
        unit.TickStatusEffects(); // duration 2 -> 1, still active

        Assert.IsFalse(fired, "OnStatusExpired should not fire while the effect still has turns remaining.");
    }

    [Test]
    public void TickStatusEffects_NoActiveEffects_DoesNotFireOnStatusExpired()
    {
        var unit = CreateUnit();

        bool fired = false;
        unit.OnStatusExpired += t => fired = true;

        unit.TickStatusEffects();

        Assert.IsFalse(fired);
    }
}

} // namespace SowurShield.Tests
