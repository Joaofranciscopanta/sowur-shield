using NUnit.Framework;
using UnityEngine;
using SowurShield.Inventory;

/// <summary>
/// Edit Mode tests for what ContainerView.Rebuild does to slots that already exist.
///
/// Rebuild only runs when a container changes size, and until Etapa 4a-bis nothing reached it:
/// the SellBox and the feeding trough are fixed-size and build exactly once. Migrating the
/// player inventory made it live — UpgradeInventorySize and SetInventorySize both resize the
/// container — and both bugs below showed up the first time an inventory was grown in Play Mode.
///
/// SlotGroupTests covers the index arithmetic without a scene. These need real slot objects,
/// so they build a minimal prefab in memory rather than loading the project's one.
///
/// Of the two bugs, only the visibility one is genuinely caught here — confirmed by reverting
/// each fix and checking these actually go red. The orphaned-slot bug is Play-Mode-only; see
/// the note above those tests.
/// </summary>
public class ContainerViewRebuildTests
{
    private GameObject host;
    private GameObject slotPrefab;
    private Transform groupOneParent;
    private Transform groupTwoParent;
    private ContainerView view;
    private InventoryContainer container;

    private const int HotbarSize = 4;
    private const int InitialSlots = 10;

    [SetUp]
    public void SetUp()
    {
        slotPrefab = new GameObject("SlotPrefab");
        slotPrefab.AddComponent<InventorySlot>();

        host = new GameObject("ViewHost");
        groupOneParent = new GameObject("HotbarParent").transform;
        groupTwoParent = new GameObject("StorageParent").transform;

        view = host.AddComponent<ContainerView>();
        container = new InventoryContainer(InitialSlots, "TestContainer");

        view.Configure(
            slotPrefab,
            new SlotGroup(groupOneParent, 0, HotbarSize, true, "HotbarSlot"),
            new SlotGroup(groupTwoParent, HotbarSize, 0, false, "StorageSlot"));

        view.Bind(container);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
        Object.DestroyImmediate(slotPrefab);
        if (groupOneParent != null) Object.DestroyImmediate(groupOneParent.gameObject);
        if (groupTwoParent != null) Object.DestroyImmediate(groupTwoParent.gameObject);
    }

    /// <summary>Children that are still parented and not already destroyed.</summary>
    private static int LiveChildCount(Transform parent)
    {
        int count = 0;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i) != null)
                count++;
        return count;
    }

    // =========================================================================
    // BUG 1 — a resize left the previous generation of slots behind
    //
    // HONEST LIMITATION: these three cannot fail on the bug they describe. It only exists in
    // Play Mode, where Destroy is deferred to end of frame and the outgoing slots are still
    // children while Rebuild instantiates the new ones (measured: growing a 45-slot inventory
    // once left 81 children under a parent that should hold 45, and 36 orphans). Edit Mode
    // takes the DestroyImmediate branch, so the children are gone before the rebuild starts and
    // the counts come out right either way — verified by reverting the fix and watching them
    // still pass.
    //
    // They are kept as guards on the invariant itself: if Rebuild ever stops clearing, or
    // starts double-instantiating, these catch it. The deferred-Destroy case is covered by the
    // Play Mode check recorded in the plan.
    // =========================================================================

    [Test]
    public void Resizing_DoesNotLeaveTheOldSlotsUnderTheParents()
    {
        Assert.AreEqual(HotbarSize, LiveChildCount(groupOneParent), "precondition");
        Assert.AreEqual(InitialSlots - HotbarSize, LiveChildCount(groupTwoParent), "precondition");

        container.SetMaxSlots(InitialSlots + 5);   // fires OnSizeChanged -> Rebuild

        Assert.AreEqual(HotbarSize, LiveChildCount(groupOneParent),
            "the hotbar parent must hold one generation of slots, not two");
        Assert.AreEqual(InitialSlots + 5 - HotbarSize, LiveChildCount(groupTwoParent),
            "the storage parent must hold one generation of slots, not two");
    }

    [Test]
    public void Resizing_LeavesNoChildTheViewDoesNotKnowAbout()
    {
        container.SetMaxSlots(InitialSlots + 5);

        foreach (Transform parent in new[] { groupOneParent, groupTwoParent })
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null) continue;

                var slot = child.GetComponent<InventorySlot>();
                Assert.IsNotNull(slot, $"unexpected child '{child.name}' under {parent.name}");
                Assert.GreaterOrEqual(view.IndexOf(slot), 0,
                    $"'{child.name}' is orphaned — it survived a Rebuild but the view has no record of it");
            }
        }
    }

    [Test]
    public void RepeatedResizes_DoNotAccumulateSlots()
    {
        container.SetMaxSlots(15);
        container.SetMaxSlots(20);
        container.SetMaxSlots(25);

        Assert.AreEqual(HotbarSize, LiveChildCount(groupOneParent));
        Assert.AreEqual(25 - HotbarSize, LiveChildCount(groupTwoParent));
        Assert.AreEqual(25, view.SlotCount);
    }

    // =========================================================================
    // BUG 2 — a rebuilt group reverted to startActive, closing an open grid
    // =========================================================================

    [Test]
    public void Rebuilding_KeepsAGroupThatWasShown()
    {
        view.SetGroupActive(1, true);          // player opens the storage grid
        Assert.IsTrue(view.GetSlotUI(HotbarSize).gameObject.activeSelf, "precondition");

        container.SetMaxSlots(InitialSlots + 5);

        // Fresh slots come back at the group's startActive, which is false for storage. The
        // view has to remember what the group is actually showing, or growing the inventory
        // while it is open blanks the grid until the player toggles it twice.
        Assert.IsTrue(view.GetSlotUI(HotbarSize).gameObject.activeSelf,
            "a group that was visible must stay visible across a rebuild");
        Assert.IsTrue(view.GetSlotUI(view.SlotCount - 1).gameObject.activeSelf,
            "slots added by the resize must match the group's current visibility too");
    }

    [Test]
    public void Rebuilding_KeepsAGroupThatWasHidden()
    {
        Assert.IsFalse(view.GetSlotUI(HotbarSize).gameObject.activeSelf, "storage starts hidden");

        container.SetMaxSlots(InitialSlots + 5);

        Assert.IsFalse(view.GetSlotUI(HotbarSize).gameObject.activeSelf,
            "a hidden group must not be revealed by a rebuild");
    }

    [Test]
    public void Rebuilding_HonoursAGroupThatWasHiddenAfterStartingVisible()
    {
        view.SetGroupActive(0, false);         // hide the hotbar, which starts visible

        container.SetMaxSlots(InitialSlots + 5);

        Assert.IsFalse(view.GetSlotUI(0).gameObject.activeSelf,
            "current visibility wins over startActive in both directions");
    }

    // =========================================================================
    // The rebuild still has to do its actual job
    // =========================================================================

    [Test]
    public void Rebuilding_PreservesContainerContents()
    {
        Item item = ScriptableObject.CreateInstance<Item>();
        item.itemName = "Apple";
        item.isStackable = true;
        item.maxStackSize = 99;

        container.SetSlot(0, new ItemStack(item, 7));
        container.SetMaxSlots(InitialSlots + 5);

        Assert.AreEqual(7, container.GetSlot(0).quantity, "growing must not disturb stored items");
        Assert.AreEqual(item, container.GetSlot(0).item);

        Object.DestroyImmediate(item);
    }

    [Test]
    public void Rebuilding_ReindexesEverySlot()
    {
        container.SetMaxSlots(InitialSlots + 5);

        for (int i = 0; i < view.SlotCount; i++)
        {
            InventorySlot slot = view.GetSlotUI(i);
            Assert.IsNotNull(slot, $"slot {i} was not rebuilt");
            Assert.AreEqual(i, view.IndexOf(slot), $"slot {i} answers to the wrong index");
        }
    }

    [Test]
    public void Shrinking_DropsTheSlotsThatNoLongerExist()
    {
        container.SetMaxSlots(6);

        Assert.AreEqual(6, view.SlotCount);
        Assert.AreEqual(HotbarSize, LiveChildCount(groupOneParent));
        Assert.AreEqual(6 - HotbarSize, LiveChildCount(groupTwoParent));
    }
}
