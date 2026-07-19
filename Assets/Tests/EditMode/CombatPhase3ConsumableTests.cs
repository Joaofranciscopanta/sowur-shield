using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using SowurShield.Combat;
using SowurShield.Inventory;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for Combat Phase 3 in-battle consumable item usage
/// (TurnManager.UseConsumableOnUnit).
/// </summary>
public class CombatPhase3ConsumableTests
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
    }

    private CombatUnit CreateUnit(float hp, float maxHp = 100f)
    {
        var go = new GameObject("TestUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = true;
        unit.InitializeAsEnemy(go.name, maxHp, 10f, 5f, 10f);
        unit.currentHealth = hp;
        return unit;
    }

    private TurnManager CreateTurnManager()
    {
        var go = new GameObject("TurnManager");
        Track(go);
        return go.AddComponent<TurnManager>();
    }

    private Item CreateItem(bool isConsumable, int healthRestore)
    {
        var item = ScriptableObject.CreateInstance<Item>();
        Track(item);
        item.isConsumable = isConsumable;
        item.healthRestore = healthRestore;
        return item;
    }

    [Test]
    public void UseConsumableOnUnit_ValidConsumable_HealsTarget()
    {
        var tm = CreateTurnManager();
        var target = CreateUnit(hp: 50f, maxHp: 100f);
        var item = CreateItem(isConsumable: true, healthRestore: 30);

        bool result = tm.UseConsumableOnUnit(item, target);

        Assert.IsTrue(result, "Using a valid consumable on an alive target should succeed.");
        Assert.AreEqual(80f, target.currentHealth, 0.001f, "Target health should increase by the item's healthRestore amount.");
    }

    [Test]
    public void UseConsumableOnUnit_NonConsumableItem_Rejected()
    {
        var tm = CreateTurnManager();
        var target = CreateUnit(hp: 50f, maxHp: 100f);
        var item = CreateItem(isConsumable: false, healthRestore: 30);

        bool result = tm.UseConsumableOnUnit(item, target);

        Assert.IsFalse(result, "A non-consumable item should be rejected.");
        Assert.AreEqual(50f, target.currentHealth, 0.001f, "Target health should be unchanged when the item is rejected.");
    }

    [Test]
    public void UseConsumableOnUnit_NullItem_Rejected()
    {
        var tm = CreateTurnManager();
        var target = CreateUnit(hp: 50f, maxHp: 100f);

        bool result = tm.UseConsumableOnUnit(null, target);

        Assert.IsFalse(result, "A null item should be rejected.");
    }

    [Test]
    public void UseConsumableOnUnit_NullTarget_Rejected()
    {
        var tm = CreateTurnManager();
        var item = CreateItem(isConsumable: true, healthRestore: 30);

        bool result = tm.UseConsumableOnUnit(item, null);

        Assert.IsFalse(result, "A null target should be rejected.");
    }

    [Test]
    public void UseConsumableOnUnit_DeadTarget_Rejected()
    {
        var tm = CreateTurnManager();
        var target = CreateUnit(hp: 0f, maxHp: 100f);
        var item = CreateItem(isConsumable: true, healthRestore: 30);

        bool result = tm.UseConsumableOnUnit(item, target);

        Assert.IsFalse(result, "A dead target should be rejected.");
        Assert.AreEqual(0f, target.currentHealth, 0.001f, "Dead target's health should remain unchanged.");
    }

    [Test]
    public void UseConsumableOnUnit_HealDoesNotExceedMaxHealth()
    {
        var tm = CreateTurnManager();
        var target = CreateUnit(hp: 90f, maxHp: 100f);
        var item = CreateItem(isConsumable: true, healthRestore: 30);

        tm.UseConsumableOnUnit(item, target);

        Assert.AreEqual(100f, target.currentHealth, 0.001f, "Healing should not exceed the target's max health.");
    }
}

} // namespace SowurShield.Tests
