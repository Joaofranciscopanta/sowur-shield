using NUnit.Framework;
using UnityEngine;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for Combat Phase 3 critical hits (CombatUnit.GetCritChance/ApplyCrit).
/// </summary>
public class CombatPhase3CritTests
{
    private System.Collections.Generic.List<Object> _cleanupList = new System.Collections.Generic.List<Object>();

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

    private CombatUnit CreateUnit(float hp = 100f, float atk = 10f, float def = 5f, float spd = 10f)
    {
        var go = new GameObject("TestUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = false;
        unit.InitializeAsEnemy(go.name, hp, atk, def, spd);
        return unit;
    }

    [Test]
    public void ApplyCrit_WhenCrit_MultipliesDamageByCritMultiplier()
    {
        Assert.AreEqual(150f, CombatUnit.ApplyCrit(100f, true), 0.001f, "A critical hit should multiply damage by CritMultiplier (1.5x).");
    }

    [Test]
    public void ApplyCrit_WhenNotCrit_ReturnsDamageUnchanged()
    {
        Assert.AreEqual(100f, CombatUnit.ApplyCrit(100f, false), 0.001f, "A non-critical hit should leave damage unchanged.");
    }

    [Test]
    public void GetCritChance_ReturnsExpectedBaseValue()
    {
        var unit = CreateUnit();

        Assert.AreEqual(0.05f, unit.GetCritChance(), 0.001f, "Base crit chance should be 5%.");
    }
}

} // namespace SowurShield.Tests
