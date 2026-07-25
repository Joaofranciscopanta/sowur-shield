using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using SowurShield.Inventory;

/// <summary>
/// Edit Mode tests for InventoryContainer — the pure data layer under the player inventory,
/// SellBox and FeedingTrough. No MonoBehaviour or scene needed.
///
/// This is Etapa 0 of review/04_CONTAINER_REFACTOR_PLAN.md: the safety net that has to exist
/// BEFORE any of the container refactor moves logic around. Until now this class had zero tests
/// despite being the shared foundation of every container in the game.
///
/// Several tests below deliberately assert CURRENT behaviour rather than ideal behaviour
/// (partial mutation on a failed add/remove, item loss on shrink, GetSlot returning a live
/// reference). They are marked with "DOCUMENTS CURRENT BEHAVIOUR" so a future change that
/// breaks them is a conscious decision, not an accident.
/// </summary>
public class InventoryContainerTests
{
    private Item carrot;      // stackable, maxStack 10
    private Item tomato;      // stackable, maxStack 10
    private Item hoe;         // non-stackable
    private InventoryContainer container;

    [SetUp]
    public void SetUp()
    {
        carrot = MakeItem("Carrot", stackable: true, maxStack: 10, type: ItemType.Food);
        tomato = MakeItem("Tomato", stackable: true, maxStack: 10, type: ItemType.Food);
        hoe = MakeItem("Hoe", stackable: false, maxStack: 1, type: ItemType.Tool);

        container = new InventoryContainer(6, "test");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(carrot);
        Object.DestroyImmediate(tomato);
        Object.DestroyImmediate(hoe);
        ResetItemDatabase();
    }

    private static Item MakeItem(string name, bool stackable, int maxStack, ItemType type)
    {
        var item = ScriptableObject.CreateInstance<Item>();
        item.itemName = name;
        item.isStackable = stackable;
        item.maxStackSize = maxStack;
        item.itemType = type;
        item.baseValue = 5;
        return item;
    }

    // =========================================================================
    // CONSTRUCTION
    // =========================================================================

    [Test]
    public void Constructor_CreatesRequestedNumberOfEmptySlots()
    {
        Assert.AreEqual(6, container.MaxSlots);
        for (int i = 0; i < 6; i++)
            Assert.IsTrue(container.GetSlot(i).IsEmpty, $"slot {i} should start empty");
    }

    [Test]
    public void Constructor_StoresContainerID()
    {
        Assert.AreEqual("test", container.ContainerID);
    }

    // =========================================================================
    // ADD
    // =========================================================================

    [Test]
    public void AddItem_IntoEmptyContainer_FillsFirstSlot()
    {
        Assert.IsTrue(container.AddItem(carrot, 3));

        Assert.AreEqual(carrot, container.GetSlot(0).item);
        Assert.AreEqual(3, container.GetSlot(0).quantity);
        Assert.IsTrue(container.GetSlot(1).IsEmpty);
    }

    [Test]
    public void AddItem_StacksOntoExistingPartialStack()
    {
        container.AddItem(carrot, 3);
        container.AddItem(carrot, 4);

        Assert.AreEqual(7, container.GetSlot(0).quantity);
        Assert.IsTrue(container.GetSlot(1).IsEmpty, "should not have spilled into a second slot");
    }

    [Test]
    public void AddItem_OverflowsIntoNextSlot_WhenStackIsFull()
    {
        container.AddItem(carrot, 12); // maxStack is 10

        Assert.AreEqual(10, container.GetSlot(0).quantity);
        Assert.AreEqual(2, container.GetSlot(1).quantity);
    }

    [Test]
    public void AddItem_NonStackable_TakesOneSlotEach()
    {
        Assert.IsTrue(container.AddItem(hoe, 3));

        Assert.AreEqual(1, container.GetSlot(0).quantity);
        Assert.AreEqual(1, container.GetSlot(1).quantity);
        Assert.AreEqual(1, container.GetSlot(2).quantity);
        Assert.IsTrue(container.GetSlot(3).IsEmpty);
    }

    [Test]
    public void AddItem_ReturnsFalse_WhenContainerCannotFitEverything()
    {
        // 6 slots x 10 per stack = 60 capacity
        Assert.IsFalse(container.AddItem(carrot, 61));
    }

    [Test]
    public void AddItem_PartialFit_StillAddsWhatFits()
    {
        // DOCUMENTS CURRENT BEHAVIOUR: a failed add is NOT atomic — it fills what it can and
        // reports false. Callers that check the bool without inspecting the container will
        // silently duplicate or lose items. ItemTransferService (Etapa 1) must account for this.
        bool complete = container.AddItem(carrot, 61);

        Assert.IsFalse(complete);
        Assert.AreEqual(60, container.GetItemCount(carrot), "the 60 that fit were still added");
    }

    [Test]
    public void AddItem_NullItem_ReturnsFalseAndChangesNothing()
    {
        Assert.IsFalse(container.AddItem(null, 5));
        Assert.IsTrue(container.GetSlot(0).IsEmpty);
    }

    [Test]
    public void AddItem_ZeroOrNegativeQuantity_ReturnsFalse()
    {
        Assert.IsFalse(container.AddItem(carrot, 0));
        Assert.IsFalse(container.AddItem(carrot, -3));
        Assert.IsTrue(container.GetSlot(0).IsEmpty);
    }

    [Test]
    public void AddItem_DifferentItems_DoNotStackTogether()
    {
        container.AddItem(carrot, 3);
        container.AddItem(tomato, 3);

        Assert.AreEqual(carrot, container.GetSlot(0).item);
        Assert.AreEqual(tomato, container.GetSlot(1).item);
    }

    // =========================================================================
    // REMOVE
    // =========================================================================

    [Test]
    public void RemoveItem_TakesFromASingleSlot()
    {
        container.AddItem(carrot, 8);

        Assert.IsTrue(container.RemoveItem(carrot, 3));
        Assert.AreEqual(5, container.GetItemCount(carrot));
    }

    [Test]
    public void RemoveItem_SpansMultipleSlots()
    {
        container.AddItem(carrot, 15); // slot0 = 10, slot1 = 5

        Assert.IsTrue(container.RemoveItem(carrot, 12));
        Assert.AreEqual(3, container.GetItemCount(carrot));
    }

    [Test]
    public void RemoveItem_EmptiesSlotWhenQuantityHitsZero()
    {
        container.AddItem(carrot, 4);
        container.RemoveItem(carrot, 4);

        Assert.IsTrue(container.GetSlot(0).IsEmpty);
    }

    [Test]
    public void RemoveItem_MoreThanAvailable_ReturnsFalse()
    {
        container.AddItem(carrot, 3);

        Assert.IsFalse(container.RemoveItem(carrot, 10));
    }

    [Test]
    public void RemoveItem_MoreThanAvailable_StillRemovesWhatItCould()
    {
        // DOCUMENTS CURRENT BEHAVIOUR: like AddItem, removal is not atomic. Asking for more
        // than is present empties the container AND returns false.
        container.AddItem(carrot, 3);

        bool complete = container.RemoveItem(carrot, 10);

        Assert.IsFalse(complete);
        Assert.AreEqual(0, container.GetItemCount(carrot), "the 3 present were still removed");
    }

    [Test]
    public void RemoveItem_ItemNotPresent_ReturnsFalse()
    {
        container.AddItem(carrot, 3);
        Assert.IsFalse(container.RemoveItem(tomato, 1));
    }

    [Test]
    public void RemoveItem_NullOrNonPositive_ReturnsFalse()
    {
        container.AddItem(carrot, 3);

        Assert.IsFalse(container.RemoveItem(null, 1));
        Assert.IsFalse(container.RemoveItem(carrot, 0));
        Assert.AreEqual(3, container.GetItemCount(carrot));
    }

    // =========================================================================
    // CAN ADD — must stay consistent with AddItem, since drag/drop checks it first
    // =========================================================================

    [Test]
    public void CanAdd_TrueWhenItFits()
    {
        Assert.IsTrue(container.CanAdd(carrot, 60));
    }

    [Test]
    public void CanAdd_FalseWhenItDoesNotFit()
    {
        Assert.IsFalse(container.CanAdd(carrot, 61));
    }

    [Test]
    public void CanAdd_AccountsForPartialStacks()
    {
        container.AddItem(carrot, 5);   // slot0 has 5 free, 5 empty slots x 10 = 55
        Assert.IsTrue(container.CanAdd(carrot, 55));
        Assert.IsFalse(container.CanAdd(carrot, 56));
    }

    [Test]
    public void CanAdd_MatchesAddItem_ForNonStackableItems()
    {
        Assert.IsTrue(container.CanAdd(hoe, 6));
        Assert.IsFalse(container.CanAdd(hoe, 7), "6 slots can hold at most 6 non-stackable items");
    }

    [Test]
    public void CanAdd_FalseForNullOrNonPositive()
    {
        Assert.IsFalse(container.CanAdd(null, 1));
        Assert.IsFalse(container.CanAdd(carrot, 0));
    }

    // =========================================================================
    // SLOT ACCESS
    // =========================================================================

    [Test]
    public void GetSlot_OutOfRange_ReturnsEmptyStackInsteadOfThrowing()
    {
        Assert.IsTrue(container.GetSlot(-1).IsEmpty);
        Assert.IsTrue(container.GetSlot(999).IsEmpty);
    }

    [Test]
    public void SetSlot_StoresACopy_NotTheCallersInstance()
    {
        var source = new ItemStack(carrot, 5);
        container.SetSlot(0, source);

        source.quantity = 99;

        Assert.AreEqual(5, container.GetSlot(0).quantity,
            "SetSlot must Clone, otherwise the caller can mutate container state behind its back");
    }

    [Test]
    public void SetSlot_NullStack_ClearsTheSlot()
    {
        container.AddItem(carrot, 5);
        container.SetSlot(0, null);

        Assert.IsTrue(container.GetSlot(0).IsEmpty);
    }

    [Test]
    public void SetSlot_OutOfRange_IsANoOp()
    {
        Assert.DoesNotThrow(() => container.SetSlot(-1, new ItemStack(carrot, 1)));
        Assert.DoesNotThrow(() => container.SetSlot(50, new ItemStack(carrot, 1)));
        Assert.AreEqual(0, container.GetItemCount(carrot));
    }

    [Test]
    public void GetSlot_ReturnsLiveReference_NotACopy()
    {
        // DOCUMENTS CURRENT BEHAVIOUR — and it is a real encapsulation leak: unlike GetAllItems,
        // GetSlot hands out the internal ItemStack, so a caller can mutate the container without
        // going through SetSlot (and without firing OnSlotChanged, leaving the UI stale).
        // Etapa 4 has code that relies on this; changing it is its own task.
        container.AddItem(carrot, 5);

        container.GetSlot(0).quantity = 99;

        Assert.AreEqual(99, container.GetSlot(0).quantity);
    }

    [Test]
    public void GetAllItems_ReturnsOnlyNonEmptySlots()
    {
        container.AddItem(carrot, 3);
        container.SetSlot(3, new ItemStack(tomato, 2));

        var items = container.GetAllItems();

        Assert.AreEqual(2, items.Count);
    }

    [Test]
    public void GetAllItems_ReturnsCopies()
    {
        container.AddItem(carrot, 3);

        container.GetAllItems()[0].quantity = 99;

        Assert.AreEqual(3, container.GetItemCount(carrot));
    }

    [Test]
    public void GetItemsByType_FiltersByItemType()
    {
        container.AddItem(carrot, 1);
        container.AddItem(hoe, 1);

        Assert.AreEqual(1, container.GetItemsByType(ItemType.Food).Count);
        Assert.AreEqual(1, container.GetItemsByType(ItemType.Tool).Count);
    }

    // =========================================================================
    // QUERIES
    // =========================================================================

    [Test]
    public void GetItemCount_SumsAcrossSlots()
    {
        container.AddItem(carrot, 15);
        Assert.AreEqual(15, container.GetItemCount(carrot));
    }

    [Test]
    public void GetItemCount_NullItem_IsZero()
    {
        Assert.AreEqual(0, container.GetItemCount(null));
    }

    [Test]
    public void HasItem_RespectsQuantityThreshold()
    {
        container.AddItem(carrot, 5);

        Assert.IsTrue(container.HasItem(carrot, 5));
        Assert.IsFalse(container.HasItem(carrot, 6));
    }

    [Test]
    public void FindSlotWithItem_ReturnsFirstMatch()
    {
        container.SetSlot(2, new ItemStack(carrot, 1));
        container.SetSlot(4, new ItemStack(carrot, 1));

        Assert.AreEqual(2, container.FindSlotWithItem(carrot));
    }

    [Test]
    public void FindSlotWithItem_ReturnsMinusOneWhenAbsent()
    {
        Assert.AreEqual(-1, container.FindSlotWithItem(tomato));
        Assert.AreEqual(-1, container.FindSlotWithItem(null));
    }

    [Test]
    public void EmptySlotQueries_ReflectContents()
    {
        Assert.IsTrue(container.HasEmptySlot());
        Assert.AreEqual(0, container.GetFirstEmptySlotIndex());

        container.AddItem(carrot, 60); // fills all 6 slots

        Assert.IsFalse(container.HasEmptySlot());
        Assert.AreEqual(-1, container.GetFirstEmptySlotIndex());
    }

    [Test]
    public void ClearAll_EmptiesEverySlot()
    {
        container.AddItem(carrot, 15);
        container.ClearAll();

        Assert.AreEqual(0, container.GetItemCount(carrot));
        Assert.AreEqual(0, container.GetAllItems().Count);
    }

    // =========================================================================
    // RESIZE — Inventory.SetInventorySize / UpgradeInventorySize depend on this
    // =========================================================================

    [Test]
    public void SetMaxSlots_Growing_KeepsItemsAndAddsEmptySlots()
    {
        container.AddItem(carrot, 5);
        container.SetMaxSlots(10);

        Assert.AreEqual(10, container.MaxSlots);
        Assert.AreEqual(5, container.GetItemCount(carrot));
        Assert.IsTrue(container.GetSlot(9).IsEmpty);
    }

    [Test]
    public void SetMaxSlots_Shrinking_KeepsItemsInSurvivingSlots()
    {
        container.SetSlot(0, new ItemStack(carrot, 5));
        container.SetMaxSlots(3);

        Assert.AreEqual(3, container.MaxSlots);
        Assert.AreEqual(5, container.GetItemCount(carrot));
    }

    [Test]
    public void SetMaxSlots_Shrinking_SilentlyDestroysItemsInRemovedSlots()
    {
        // DOCUMENTS CURRENT BEHAVIOUR: SetMaxSlots counts the items it is about to lose into a
        // local `lostItems` variable and then never uses it — no warning, no return value, no
        // relocation. Inventory.SetInventorySize is the caller to check before trusting this.
        container.SetSlot(5, new ItemStack(carrot, 7));

        container.SetMaxSlots(3);

        Assert.AreEqual(0, container.GetItemCount(carrot), "items in slot 5 are gone without a trace");
    }

    [Test]
    public void SetMaxSlots_BelowOne_IsIgnored()
    {
        container.SetMaxSlots(0);
        Assert.AreEqual(6, container.MaxSlots);

        container.SetMaxSlots(-5);
        Assert.AreEqual(6, container.MaxSlots);
    }

    [Test]
    public void SetMaxSlots_SameSize_DoesNotFireSizeChanged()
    {
        int calls = 0;
        container.OnSizeChanged += _ => calls++;

        container.SetMaxSlots(6);

        Assert.AreEqual(0, calls);
    }

    // =========================================================================
    // EVENTS — the container refactor replaces hand-rolled refresh with these
    // =========================================================================

    [Test]
    public void OnSlotChanged_FiresForEachSlotTouchedByAdd()
    {
        var touched = new List<int>();
        container.OnSlotChanged += (i, _) => touched.Add(i);

        container.AddItem(carrot, 12); // slot0 full, slot1 gets 2

        CollectionAssert.AreEquivalent(new[] { 0, 1 }, touched);
    }

    [Test]
    public void OnItemAdded_ReportsTheQuantityActuallyAdded()
    {
        int reported = 0;
        container.OnItemAdded += (_, qty) => reported = qty;

        container.AddItem(carrot, 61); // only 60 fit

        Assert.AreEqual(60, reported);
    }

    [Test]
    public void OnItemAdded_DoesNotFireWhenNothingFits()
    {
        container.AddItem(carrot, 60);

        bool fired = false;
        container.OnItemAdded += (_, __) => fired = true;

        container.AddItem(tomato, 1); // container is completely full

        Assert.IsFalse(fired);
    }

    [Test]
    public void OnItemRemoved_ReportsTheQuantityActuallyRemoved()
    {
        container.AddItem(carrot, 3);

        int reported = 0;
        container.OnItemRemoved += (_, qty) => reported = qty;

        container.RemoveItem(carrot, 10);

        Assert.AreEqual(3, reported);
    }

    [Test]
    public void ClearAll_OnlyFiresForSlotsThatHadSomething()
    {
        container.AddItem(carrot, 5); // slot 0 only

        int calls = 0;
        container.OnSlotChanged += (_, __) => calls++;

        container.ClearAll();

        Assert.AreEqual(1, calls);
    }

    // =========================================================================
    // SAVE / LOAD — the format Etapa 5 standardises on
    // =========================================================================

    [Test]
    public void GetSaveData_RecordsIdSizeAndOnlyOccupiedSlots()
    {
        container.SetSlot(0, new ItemStack(carrot, 3));
        container.SetSlot(4, new ItemStack(tomato, 7));

        var data = container.GetSaveData();

        Assert.AreEqual("test", data.containerID);
        Assert.AreEqual(6, data.maxSlots);
        Assert.AreEqual(2, data.slots.Count);

        Assert.AreEqual(0, data.slots[0].index);
        Assert.AreEqual("Carrot", data.slots[0].itemName);
        Assert.AreEqual(3, data.slots[0].quantity);

        Assert.AreEqual(4, data.slots[1].index);
        Assert.AreEqual("Tomato", data.slots[1].itemName);
    }

    [Test]
    public void GetSaveData_EmptyContainer_HasNoSlotEntries()
    {
        Assert.AreEqual(0, container.GetSaveData().slots.Count);
    }

    [Test]
    public void SaveLoad_RoundTrip_RestoresItemsAtTheirOriginalIndices()
    {
        InstallTestItemDatabase(carrot, tomato);

        container.SetSlot(0, new ItemStack(carrot, 3));
        container.SetSlot(4, new ItemStack(tomato, 7));
        var data = container.GetSaveData();

        var restored = new InventoryContainer(6, "test");
        restored.LoadFromSaveData(data);

        Assert.AreEqual(carrot, restored.GetSlot(0).item);
        Assert.AreEqual(3, restored.GetSlot(0).quantity);
        Assert.AreEqual(tomato, restored.GetSlot(4).item);
        Assert.AreEqual(7, restored.GetSlot(4).quantity);
        Assert.IsTrue(restored.GetSlot(1).IsEmpty);
    }

    [Test]
    public void LoadFromSaveData_ResizesTheContainerToMatch()
    {
        InstallTestItemDatabase(carrot);

        var big = new InventoryContainer(12, "test");
        big.SetSlot(11, new ItemStack(carrot, 1));
        var data = big.GetSaveData();

        container.LoadFromSaveData(data); // container is 6 slots

        Assert.AreEqual(12, container.MaxSlots);
        Assert.AreEqual(1, container.GetItemCount(carrot));
    }

    [Test]
    public void LoadFromSaveData_ClearsAnythingAlreadyInTheContainer()
    {
        InstallTestItemDatabase(carrot, tomato);

        container.SetSlot(2, new ItemStack(tomato, 9));

        var source = new InventoryContainer(6, "test");
        source.SetSlot(0, new ItemStack(carrot, 1));
        container.LoadFromSaveData(source.GetSaveData());

        Assert.AreEqual(0, container.GetItemCount(tomato), "pre-existing contents must not survive a load");
        Assert.AreEqual(1, container.GetItemCount(carrot));
    }

    [Test]
    public void LoadFromSaveData_UnknownItemName_IsSkippedWithoutThrowing()
    {
        InstallTestItemDatabase(carrot); // tomato deliberately NOT registered

        var data = new InventoryContainer.ContainerSaveData { containerID = "test", maxSlots = 6 };
        data.slots.Add(new InventoryContainer.ContainerSaveData.SlotSaveData { index = 0, itemName = "Carrot", quantity = 2 });
        data.slots.Add(new InventoryContainer.ContainerSaveData.SlotSaveData { index = 1, itemName = "DeletedItem", quantity = 5 });

        Assert.DoesNotThrow(() => container.LoadFromSaveData(data));

        Assert.AreEqual(2, container.GetItemCount(carrot));
        Assert.IsTrue(container.GetSlot(1).IsEmpty);
    }

    [Test]
    public void LoadFromSaveData_OutOfRangeIndex_IsSkipped()
    {
        InstallTestItemDatabase(carrot);

        var data = new InventoryContainer.ContainerSaveData { containerID = "test", maxSlots = 6 };
        data.slots.Add(new InventoryContainer.ContainerSaveData.SlotSaveData { index = 99, itemName = "Carrot", quantity = 2 });

        Assert.DoesNotThrow(() => container.LoadFromSaveData(data));
        Assert.AreEqual(0, container.GetItemCount(carrot));
    }

    [Test]
    public void LoadFromSaveData_Null_IsANoOp()
    {
        container.AddItem(carrot, 3);

        Assert.DoesNotThrow(() => container.LoadFromSaveData(null));
        Assert.AreEqual(3, container.GetItemCount(carrot), "a null payload must not wipe the container");
    }

    // =========================================================================
    // ItemDatabase test seam
    // =========================================================================
    // ItemDatabase resolves names through Resources and exposes no injection point, so these
    // tests reach into its private statics. Kept local to the test file rather than adding a
    // testing-only API to production code.

    private static void InstallTestItemDatabase(params Item[] items)
    {
        var db = ScriptableObject.CreateInstance<ItemDatabase>();
        db.autoLoadFromResources = false;
        db.items = new List<Item>(items);

        SetStatic("instance", db);
        SetStatic("isInitialized", false);
        GetLookup().Clear();

        db.Initialize();
    }

    private static void ResetItemDatabase()
    {
        SetStatic("instance", null);
        SetStatic("isInitialized", false);
        GetLookup().Clear();
    }

    private static void SetStatic(string fieldName, object value)
    {
        typeof(ItemDatabase)
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
            ?.SetValue(null, value);
    }

    private static Dictionary<string, Item> GetLookup()
    {
        return typeof(ItemDatabase)
            .GetField("itemLookup", BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null) as Dictionary<string, Item> ?? new Dictionary<string, Item>();
    }
}
