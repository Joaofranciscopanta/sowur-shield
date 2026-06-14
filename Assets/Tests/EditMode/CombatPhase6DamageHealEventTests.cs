using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for OnDamageTaken/OnHealed events on CombatUnit, including the
/// optional isCrit flag threaded through TakeDamage/TakeDamageWithShield.
/// </summary>
public class CombatPhase6DamageHealEventTests
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
    // OnDamageTaken
    // =========================================================================

    [Test]
    public void TakeDamage_FiresOnDamageTakenWithAmountAndCritFlag()
    {
        var unit = CreateUnit();

        float receivedAmount = -1f;
        bool receivedCrit = false;
        unit.OnDamageTaken += (amount, isCrit) => { receivedAmount = amount; receivedCrit = isCrit; };

        unit.TakeDamage(15f, isCrit: true);

        Assert.AreEqual(15f, receivedAmount, 0.001f);
        Assert.IsTrue(receivedCrit);
    }

    [Test]
    public void TakeDamage_DefaultIsCritFalse()
    {
        var unit = CreateUnit();

        bool receivedCrit = true;
        unit.OnDamageTaken += (amount, isCrit) => receivedCrit = isCrit;

        unit.TakeDamage(10f);

        Assert.IsFalse(receivedCrit);
    }

    [Test]
    public void TakeDamageWithShield_FiresOnDamageTakenWithPostShieldAmount()
    {
        var unit = CreateUnit();
        unit.ApplyStatusEffect(StatusEffectType.Shield, 0.5f, 3);

        float receivedAmount = -1f;
        unit.OnDamageTaken += (amount, isCrit) => receivedAmount = amount;

        unit.TakeDamageWithShield(20f, isCrit: false);

        Assert.AreEqual(10f, receivedAmount, 0.001f, "OnDamageTaken should report the post-shield damage amount.");
    }

    [Test]
    public void TakeDamageWithShield_PassesCritFlagThrough()
    {
        var unit = CreateUnit();

        bool receivedCrit = false;
        unit.OnDamageTaken += (amount, isCrit) => receivedCrit = isCrit;

        unit.TakeDamageWithShield(20f, isCrit: true);

        Assert.IsTrue(receivedCrit);
    }

    // =========================================================================
    // OnHealed
    // =========================================================================

    [Test]
    public void Heal_FiresOnHealedWithAmount()
    {
        var unit = CreateUnit();
        unit.currentHealth = 50f;

        float receivedAmount = -1f;
        unit.OnHealed += amount => receivedAmount = amount;

        unit.Heal(20f);

        Assert.AreEqual(20f, receivedAmount, 0.001f);
        Assert.AreEqual(70f, unit.currentHealth, 0.001f);
    }

    [Test]
    public void Heal_AtMaxHealth_StillFiresOnHealed()
    {
        var unit = CreateUnit();
        unit.currentHealth = unit.GetMaxHealth();

        float receivedAmount = -1f;
        unit.OnHealed += amount => receivedAmount = amount;

        unit.Heal(20f);

        Assert.AreEqual(20f, receivedAmount, 0.001f, "OnHealed reports the raw heal amount even if clamped by max health.");
        Assert.AreEqual(unit.GetMaxHealth(), unit.currentHealth, 0.001f);
    }
}

} // namespace SowurShield.Tests
