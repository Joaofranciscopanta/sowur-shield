using NUnit.Framework;
using UnityEngine;
using SowurShield.Inventory;

/// <summary>
/// Edit Mode tests for hotbar auto-refill — the feature that pulls a replacement stack out of
/// storage when a hotbar slot runs dry.
///
/// It was silently dead on the consume path: Inventory.UseItem called CheckHotbarAutoRefill
/// BEFORE writing the decremented stack back, so the refill's "is this slot empty now?" guard
/// always saw the old stack and bailed out (see review/04_CONTAINER_REFACTOR_PLAN.md §6.5).
///
/// Inventory is a MonoBehaviour, but Awake only builds the container and the tracking array —
/// no scene, canvas or input needed — so it can be exercised directly here.
/// </summary>
public class InventoryHotbarRefillTests
{
    private const int HotbarSize = 9;   // Inventory's default
    private const int FirstStorageSlot = HotbarSize;

    private GameObject host;
    private Inventory inventory;
    private Item potion;
    private Item carrot;

    [SetUp]
    public void SetUp()
    {
        potion = MakeItem("Potion", consumable: true);
        carrot = MakeItem("Carrot", consumable: false);

        host = new GameObject("InventoryHost");
        inventory = host.AddComponent<Inventory>();   // Awake builds the container
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
        Object.DestroyImmediate(potion);
        Object.DestroyImmediate(carrot);
    }

    private static Item MakeItem(string name, bool consumable)
    {
        var item = ScriptableObject.CreateInstance<Item>();
        item.itemName = name;
        item.isStackable = true;
        item.maxStackSize = 10;
        item.baseValue = 5;
        item.isConsumable = consumable;
        return item;
    }

    /// <summary>
    /// Put a stack in a slot and let the inventory notice, so hotbar tracking records what
    /// lives there — that record is what auto-refill later looks up.
    /// </summary>
    private void PlaceAndTrack(int slotIndex, Item item, int quantity)
    {
        inventory.SetSlotAt(slotIndex, new ItemStack(item, quantity));
        inventory.OnSlotsChangedExternally(slotIndex);
    }

    // =========================================================================
    // THE BUG
    // =========================================================================

    [Test]
    public void ConsumingTheLastItemOfAHotbarSlot_RefillsFromStorage()
    {
        PlaceAndTrack(0, potion, 1);
        PlaceAndTrack(FirstStorageSlot, potion, 5);

        Assert.IsTrue(inventory.UseItemAt(0));

        Assert.AreEqual(potion, inventory.GetSlotAt(0).item,
            "the hotbar slot should have been refilled from storage");
        Assert.AreEqual(5, inventory.GetSlotAt(0).quantity);
        Assert.IsTrue(inventory.GetSlotAt(FirstStorageSlot).IsEmpty,
            "the storage stack moved, it was not duplicated");
    }

    [Test]
    public void TotalCountIsPreservedAcrossTheRefill()
    {
        PlaceAndTrack(0, potion, 1);
        PlaceAndTrack(FirstStorageSlot, potion, 5);

        inventory.UseItemAt(0);

        Assert.AreEqual(5, inventory.GetItemCount(potion), "6 existed, 1 was consumed");
    }

    // =========================================================================
    // WHEN IT SHOULD NOT FIRE
    // =========================================================================

    [Test]
    public void ConsumingWhenTheStackStillHasItems_DoesNotPullFromStorage()
    {
        PlaceAndTrack(0, potion, 3);
        PlaceAndTrack(FirstStorageSlot, potion, 5);

        inventory.UseItemAt(0);

        Assert.AreEqual(2, inventory.GetSlotAt(0).quantity);
        Assert.AreEqual(5, inventory.GetSlotAt(FirstStorageSlot).quantity,
            "storage must not be touched while the hotbar stack is still usable");
    }

    [Test]
    public void NoMatchingItemInStorage_LeavesTheSlotEmpty()
    {
        PlaceAndTrack(0, potion, 1);
        PlaceAndTrack(FirstStorageSlot, carrot, 5);

        inventory.UseItemAt(0);

        Assert.IsTrue(inventory.GetSlotAt(0).IsEmpty);
        Assert.AreEqual(5, inventory.GetSlotAt(FirstStorageSlot).quantity,
            "a different item must not be pulled into the hotbar");
    }

    [Test]
    public void EmptyStorage_LeavesTheSlotEmpty()
    {
        PlaceAndTrack(0, potion, 1);

        inventory.UseItemAt(0);

        Assert.IsTrue(inventory.GetSlotAt(0).IsEmpty);
    }

    [Test]
    public void RefillOnlyAppliesToHotbarSlots()
    {
        int storageA = FirstStorageSlot;
        int storageB = FirstStorageSlot + 1;

        PlaceAndTrack(storageA, potion, 1);
        PlaceAndTrack(storageB, potion, 5);

        inventory.UseItemAt(storageA);

        Assert.IsTrue(inventory.GetSlotAt(storageA).IsEmpty, "storage slots do not auto-refill");
        Assert.AreEqual(5, inventory.GetSlotAt(storageB).quantity);
    }

    // =========================================================================
    // USE ITEM GUARDS
    // =========================================================================

    [Test]
    public void NonConsumableItem_IsNotUsed()
    {
        PlaceAndTrack(0, carrot, 3);

        Assert.IsFalse(inventory.UseItemAt(0));
        Assert.AreEqual(3, inventory.GetSlotAt(0).quantity);
    }

    [Test]
    public void EmptySlot_IsNotUsed()
    {
        Assert.IsFalse(inventory.UseItemAt(0));
    }

    [Test]
    public void OutOfRangeIndex_IsNotUsed()
    {
        Assert.IsFalse(inventory.UseItemAt(-1));
        Assert.IsFalse(inventory.UseItemAt(9999));
    }

    // =========================================================================
    // TRACKING NO LONGER DEPENDS ON SLOT UI EXISTING
    // =========================================================================

    [Test]
    public void RefillWorksWithNoSlotUIPresent()
    {
        // These tests run with zero slot UIs. Hotbar tracking used to live inside
        // UpdateSlot's "does a slot UI exist?" guard, so it never recorded anything here and
        // this whole suite would have been impossible to write.
        PlaceAndTrack(0, potion, 1);
        PlaceAndTrack(FirstStorageSlot, potion, 2);

        inventory.UseItemAt(0);

        Assert.AreEqual(2, inventory.GetSlotAt(0).quantity);
    }
}
