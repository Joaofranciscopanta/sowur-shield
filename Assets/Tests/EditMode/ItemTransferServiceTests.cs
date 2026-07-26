using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using SowurShield.Inventory;

/// <summary>
/// Edit Mode tests for ItemTransferService — Etapa 1 of review/04_CONTAINER_REFACTOR_PLAN.md.
///
/// This service replaces four hand-written copies of transfer logic (Inventory.HandleSlotDrop,
/// SellBox.HandleSlotDrop, SellBox.HandleSellBoxInternalMove, SellBox.HandleSellBoxToInventoryDrop)
/// plus the inline trough handling in InventorySlot.OnDrop. None of those had a single test.
///
/// Nothing in production calls the service yet — that is Etapa 4.
/// </summary>
public class ItemTransferServiceTests
{
    private Item carrot;   // stackable, maxStack 10
    private Item tomato;   // stackable, maxStack 10
    private Item hoe;      // non-stackable

    private InventoryContainer source;
    private InventoryContainer destination;

    [SetUp]
    public void SetUp()
    {
        carrot = MakeItem("Carrot", stackable: true, maxStack: 10);
        tomato = MakeItem("Tomato", stackable: true, maxStack: 10);
        hoe = MakeItem("Hoe", stackable: false, maxStack: 1);

        source = new InventoryContainer(4, "source");
        destination = new InventoryContainer(4, "destination");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(carrot);
        Object.DestroyImmediate(tomato);
        Object.DestroyImmediate(hoe);
    }

    private static Item MakeItem(string name, bool stackable, int maxStack)
    {
        var item = ScriptableObject.CreateInstance<Item>();
        item.itemName = name;
        item.isStackable = stackable;
        item.maxStackSize = maxStack;
        item.baseValue = 5;
        return item;
    }

    // =========================================================================
    // EMPTY DESTINATION SLOT
    // =========================================================================

    [Test]
    public void Transfer_IntoEmptySlot_MovesTheWholeStack()
    {
        source.SetSlot(0, new ItemStack(carrot, 5));

        var result = ItemTransferService.Transfer(source, 0, destination, 2);

        Assert.AreEqual(TransferOutcome.Moved, result.Outcome);
        Assert.AreEqual(5, result.QuantityMoved);
        Assert.IsTrue(source.GetSlot(0).IsEmpty);
        Assert.AreEqual(5, destination.GetSlot(2).quantity);
        Assert.AreEqual(carrot, destination.GetSlot(2).item);
    }

    [Test]
    public void Transfer_IntoEmptySlot_WithExplicitQuantity_LeavesTheRemainder()
    {
        source.SetSlot(0, new ItemStack(carrot, 8));

        var result = ItemTransferService.Transfer(source, 0, destination, 0, quantity: 3);

        Assert.AreEqual(TransferOutcome.Moved, result.Outcome, "the full REQUESTED amount moved");
        Assert.AreEqual(3, result.QuantityMoved);
        Assert.AreEqual(5, source.GetSlot(0).quantity);
        Assert.AreEqual(3, destination.GetSlot(0).quantity);
    }

    [Test]
    public void Transfer_QuantityLargerThanStack_IsClampedToWhatExists()
    {
        source.SetSlot(0, new ItemStack(carrot, 4));

        var result = ItemTransferService.Transfer(source, 0, destination, 0, quantity: 99);

        Assert.AreEqual(4, result.QuantityMoved);
        Assert.IsTrue(source.GetSlot(0).IsEmpty);
    }

    // =========================================================================
    // STACKING
    // =========================================================================

    [Test]
    public void Transfer_OntoCompatibleStack_Merges()
    {
        source.SetSlot(0, new ItemStack(carrot, 3));
        destination.SetSlot(1, new ItemStack(carrot, 4));

        var result = ItemTransferService.Transfer(source, 0, destination, 1);

        Assert.AreEqual(TransferOutcome.Moved, result.Outcome);
        Assert.AreEqual(7, destination.GetSlot(1).quantity);
        Assert.IsTrue(source.GetSlot(0).IsEmpty);
    }

    [Test]
    public void Transfer_OntoAlmostFullStack_MovesWhatFitsAndKeepsTheRest()
    {
        source.SetSlot(0, new ItemStack(carrot, 6));
        destination.SetSlot(1, new ItemStack(carrot, 8)); // room for 2

        var result = ItemTransferService.Transfer(source, 0, destination, 1);

        Assert.AreEqual(TransferOutcome.Partial, result.Outcome);
        Assert.AreEqual(2, result.QuantityMoved);
        Assert.AreEqual(10, destination.GetSlot(1).quantity);
        Assert.AreEqual(4, source.GetSlot(0).quantity, "the leftover stays in the source slot");
    }

    [Test]
    public void Transfer_MoreThanOneStackIntoAnEmptySlot_IsCappedAtMaxStackSize()
    {
        // A single slot can never hold more than maxStackSize, even if the source somehow does.
        source.SetSlot(0, new ItemStack(carrot, 25));

        var result = ItemTransferService.Transfer(source, 0, destination, 0);

        Assert.AreEqual(TransferOutcome.Partial, result.Outcome);
        Assert.AreEqual(10, result.QuantityMoved);
        Assert.AreEqual(15, source.GetSlot(0).quantity);
    }

    // =========================================================================
    // SWAP
    // =========================================================================

    [Test]
    public void Transfer_OntoIncompatibleItem_SwapsTheStacks()
    {
        source.SetSlot(0, new ItemStack(carrot, 3));
        destination.SetSlot(1, new ItemStack(tomato, 7));

        var result = ItemTransferService.Transfer(source, 0, destination, 1);

        Assert.AreEqual(TransferOutcome.Swapped, result.Outcome);
        Assert.AreEqual(tomato, source.GetSlot(0).item);
        Assert.AreEqual(7, source.GetSlot(0).quantity);
        Assert.AreEqual(carrot, destination.GetSlot(1).item);
        Assert.AreEqual(3, destination.GetSlot(1).quantity);
    }

    [Test]
    public void Transfer_OntoFullStackOfTheSameItem_Swaps()
    {
        // CanStack() is false once the destination is full, so this falls through to a swap —
        // matching how Inventory.HandleSlotDrop behaves today.
        source.SetSlot(0, new ItemStack(carrot, 3));
        destination.SetSlot(1, new ItemStack(carrot, 10));

        var result = ItemTransferService.Transfer(source, 0, destination, 1);

        Assert.AreEqual(TransferOutcome.Swapped, result.Outcome);
        Assert.AreEqual(10, source.GetSlot(0).quantity);
        Assert.AreEqual(3, destination.GetSlot(1).quantity);
    }

    [Test]
    public void Transfer_PartialQuantityOntoIncompatibleItem_DoesNothing()
    {
        source.SetSlot(0, new ItemStack(carrot, 8));
        destination.SetSlot(1, new ItemStack(tomato, 2));

        var result = ItemTransferService.Transfer(source, 0, destination, 1, quantity: 3);

        Assert.AreEqual(TransferOutcome.None, result.Outcome,
            "there is nowhere to put the remainder, so a partial swap must be refused outright");
        Assert.AreEqual(8, source.GetSlot(0).quantity);
        Assert.AreEqual(tomato, destination.GetSlot(1).item);
    }

    [Test]
    public void Transfer_Swap_IsRefusedWhenTheSourceWouldNotAcceptTheReturnedItem()
    {
        // Dragging a carrot onto a hoe parked in a sell-only container would push that hoe back
        // into the source, so the SOURCE policy gets a say too.
        source.SetSlot(0, new ItemStack(carrot, 3));
        destination.SetSlot(1, new ItemStack(hoe, 1));

        var rejectsHoe = new FakePolicy { Accept = (item, _) => item != hoe };

        var result = ItemTransferService.Transfer(source, 0, destination, 1, fromPolicy: rejectsHoe);

        Assert.AreEqual(TransferOutcome.Rejected, result.Outcome);
        Assert.AreEqual(carrot, source.GetSlot(0).item, "nothing moved");
        Assert.AreEqual(hoe, destination.GetSlot(1).item);
        Assert.AreEqual(hoe, rejectsHoe.RejectedItem);
    }

    // =========================================================================
    // POLICY
    // =========================================================================

    [Test]
    public void Transfer_DestinationPolicyRejectsItem_NothingMoves()
    {
        source.SetSlot(0, new ItemStack(carrot, 5));
        var rejectAll = new FakePolicy { Accept = (_, __) => false };

        var result = ItemTransferService.Transfer(source, 0, destination, 0, toPolicy: rejectAll);

        Assert.AreEqual(TransferOutcome.Rejected, result.Outcome);
        Assert.AreEqual(0, result.QuantityMoved);
        Assert.AreEqual(5, source.GetSlot(0).quantity);
        Assert.IsTrue(destination.GetSlot(0).IsEmpty);
    }

    [Test]
    public void Transfer_RejectionFiresOnRejectedExactlyOnce()
    {
        source.SetSlot(0, new ItemStack(carrot, 5));
        var rejectAll = new FakePolicy { Accept = (_, __) => false };

        ItemTransferService.Transfer(source, 0, destination, 0, toPolicy: rejectAll);

        Assert.AreEqual(1, rejectAll.RejectedCalls);
        Assert.AreEqual(0, rejectAll.AcceptedCalls);
    }

    [Test]
    public void Transfer_SuccessFiresOnAcceptedWithTheQuantityMoved()
    {
        source.SetSlot(0, new ItemStack(carrot, 6));
        destination.SetSlot(0, new ItemStack(carrot, 8)); // room for 2
        var policy = new FakePolicy();

        ItemTransferService.Transfer(source, 0, destination, 0, toPolicy: policy);

        Assert.AreEqual(1, policy.AcceptedCalls);
        Assert.AreEqual(2, policy.AcceptedQuantity);
        Assert.AreEqual(0, policy.RejectedCalls);
    }

    [Test]
    public void Transfer_SourcePolicyForbidsWithdrawal_NothingMoves()
    {
        // The crafting-input case: items go in, they do not come back out.
        source.SetSlot(0, new ItemStack(carrot, 5));
        var locked = new FakePolicy { Withdraw = _ => false };

        var result = ItemTransferService.Transfer(source, 0, destination, 0, fromPolicy: locked);

        Assert.AreEqual(TransferOutcome.Rejected, result.Outcome);
        Assert.AreEqual(5, source.GetSlot(0).quantity);
    }

    [Test]
    public void Transfer_PolicyCanRejectPerSlot()
    {
        // The crafting-bench case: slot 3 is an output, so nothing may be placed there.
        source.SetSlot(0, new ItemStack(carrot, 5));
        var outputSlot3 = new FakePolicy { Accept = (_, index) => index != 3 };

        Assert.AreEqual(TransferOutcome.Rejected,
            ItemTransferService.Transfer(source, 0, destination, 3, toPolicy: outputSlot3).Outcome);

        Assert.AreEqual(TransferOutcome.Moved,
            ItemTransferService.Transfer(source, 0, destination, 2, toPolicy: outputSlot3).Outcome);
    }

    // =========================================================================
    // GUARDS
    // =========================================================================

    [Test]
    public void Transfer_FromEmptySlot_DoesNothing()
    {
        var result = ItemTransferService.Transfer(source, 0, destination, 0);

        Assert.AreEqual(TransferOutcome.None, result.Outcome);
        Assert.IsFalse(result.Moved);
    }

    [Test]
    public void Transfer_OntoItself_DoesNothing()
    {
        source.SetSlot(0, new ItemStack(carrot, 5));

        var result = ItemTransferService.Transfer(source, 0, source, 0);

        Assert.AreEqual(TransferOutcome.None, result.Outcome);
        Assert.AreEqual(5, source.GetSlot(0).quantity, "the stack must not be duplicated or lost");
    }

    [Test]
    public void Transfer_WithinTheSameContainer_Works()
    {
        // The SellBox internal-move case.
        source.SetSlot(0, new ItemStack(carrot, 5));

        var result = ItemTransferService.Transfer(source, 0, source, 3);

        Assert.AreEqual(TransferOutcome.Moved, result.Outcome);
        Assert.IsTrue(source.GetSlot(0).IsEmpty);
        Assert.AreEqual(5, source.GetSlot(3).quantity);
    }

    [Test]
    public void Transfer_NullContainer_DoesNothing()
    {
        Assert.AreEqual(TransferOutcome.None, ItemTransferService.Transfer(null, 0, destination, 0).Outcome);
        Assert.AreEqual(TransferOutcome.None, ItemTransferService.Transfer(source, 0, null, 0).Outcome);
    }

    [Test]
    public void Transfer_OutOfRangeIndex_DoesNothing()
    {
        source.SetSlot(0, new ItemStack(carrot, 5));

        Assert.AreEqual(TransferOutcome.None, ItemTransferService.Transfer(source, -1, destination, 0).Outcome);
        Assert.AreEqual(TransferOutcome.None, ItemTransferService.Transfer(source, 0, destination, 99).Outcome);
        Assert.AreEqual(5, source.GetSlot(0).quantity);
    }

    [Test]
    public void Transfer_ZeroQuantity_DoesNothing()
    {
        source.SetSlot(0, new ItemStack(carrot, 5));

        var result = ItemTransferService.Transfer(source, 0, destination, 0, quantity: 0);

        Assert.AreEqual(TransferOutcome.None, result.Outcome);
        Assert.AreEqual(5, source.GetSlot(0).quantity);
    }

    [Test]
    public void Transfer_NullPolicies_FallBackToPermissiveDefaults()
    {
        source.SetSlot(0, new ItemStack(carrot, 5));

        var result = ItemTransferService.Transfer(source, 0, destination, 0, null, null);

        Assert.AreEqual(TransferOutcome.Moved, result.Outcome);
    }

    // =========================================================================
    // SLOT -> CONTAINER (first fit)
    // =========================================================================

    [Test]
    public void TransferToContainer_FillsTheFirstEmptySlot()
    {
        source.SetSlot(0, new ItemStack(carrot, 5));

        var result = ItemTransferService.TransferToContainer(source, 0, destination);

        Assert.AreEqual(TransferOutcome.Moved, result.Outcome);
        Assert.AreEqual(5, destination.GetSlot(0).quantity);
        Assert.IsTrue(source.GetSlot(0).IsEmpty);
    }

    [Test]
    public void TransferToContainer_TopsUpPartialStacksBeforeOpeningNewSlots()
    {
        source.SetSlot(0, new ItemStack(carrot, 5));
        destination.SetSlot(0, new ItemStack(carrot, 8)); // room for 2

        ItemTransferService.TransferToContainer(source, 0, destination);

        Assert.AreEqual(10, destination.GetSlot(0).quantity);
        Assert.AreEqual(3, destination.GetSlot(1).quantity, "the rest opened a new slot");
        Assert.IsTrue(source.GetSlot(0).IsEmpty);
    }

    [Test]
    public void TransferToContainer_SpillsAcrossMultipleSlots()
    {
        source.SetSlot(0, new ItemStack(carrot, 25));

        var result = ItemTransferService.TransferToContainer(source, 0, destination);

        Assert.AreEqual(TransferOutcome.Moved, result.Outcome);
        Assert.AreEqual(25, result.QuantityMoved);
        Assert.AreEqual(10, destination.GetSlot(0).quantity);
        Assert.AreEqual(10, destination.GetSlot(1).quantity);
        Assert.AreEqual(5, destination.GetSlot(2).quantity);
    }

    [Test]
    public void TransferToContainer_PartialFit_MovesWhatFitsAndKeepsTheRest()
    {
        // THE ETAPA 0 FINDING, GUARDED: InventoryContainer.AddItem would add what fits and still
        // report false, so a caller trusting the bool would leave the source untouched and
        // duplicate items. The service moves exactly what fit and says so.
        var small = new InventoryContainer(1, "small");
        source.SetSlot(0, new ItemStack(carrot, 25));

        var result = ItemTransferService.TransferToContainer(source, 0, small);

        Assert.AreEqual(TransferOutcome.Partial, result.Outcome);
        Assert.AreEqual(10, result.QuantityMoved);
        Assert.AreEqual(10, small.GetSlot(0).quantity);
        Assert.AreEqual(15, source.GetSlot(0).quantity, "nothing was duplicated or destroyed");
    }

    [Test]
    public void TransferToContainer_FullDestination_ReportsFull()
    {
        var full = new InventoryContainer(1, "full");
        full.SetSlot(0, new ItemStack(tomato, 10));
        source.SetSlot(0, new ItemStack(carrot, 5));

        var result = ItemTransferService.TransferToContainer(source, 0, full);

        Assert.AreEqual(TransferOutcome.Full, result.Outcome);
        Assert.AreEqual(5, source.GetSlot(0).quantity);
    }

    [Test]
    public void TransferToContainer_PolicyRejectsItem_ReportsRejectedNotFull()
    {
        // "No room" and "not allowed" have to be distinguishable — the SellBox flashes red for
        // both, but a shop or a quest container may want to say something different.
        source.SetSlot(0, new ItemStack(carrot, 5));
        var rejectAll = new FakePolicy { Accept = (_, __) => false };

        var result = ItemTransferService.TransferToContainer(source, 0, destination, toPolicy: rejectAll);

        Assert.AreEqual(TransferOutcome.Rejected, result.Outcome);
        Assert.AreEqual(5, source.GetSlot(0).quantity);
    }

    [Test]
    public void TransferToContainer_RespectsPerSlotPolicy()
    {
        source.SetSlot(0, new ItemStack(carrot, 25));
        var onlySlotZero = new FakePolicy { Accept = (_, index) => index == 0 };

        var result = ItemTransferService.TransferToContainer(source, 0, destination, toPolicy: onlySlotZero);

        Assert.AreEqual(TransferOutcome.Partial, result.Outcome);
        Assert.AreEqual(10, result.QuantityMoved);
        Assert.AreEqual(10, destination.GetSlot(0).quantity);
        Assert.IsTrue(destination.GetSlot(1).IsEmpty, "slot 1 was off-limits");
    }

    [Test]
    public void TransferToContainer_NonStackableItems_TakeOneSlotEach()
    {
        source.SetSlot(0, new ItemStack(hoe, 3));

        ItemTransferService.TransferToContainer(source, 0, destination);

        Assert.AreEqual(1, destination.GetSlot(0).quantity);
        Assert.AreEqual(1, destination.GetSlot(1).quantity);
        Assert.AreEqual(1, destination.GetSlot(2).quantity);
    }

    [Test]
    public void TransferToContainer_ExplicitQuantity_LeavesTheRemainder()
    {
        source.SetSlot(0, new ItemStack(carrot, 9));

        var result = ItemTransferService.TransferToContainer(source, 0, destination, quantity: 4);

        Assert.AreEqual(TransferOutcome.Moved, result.Outcome);
        Assert.AreEqual(4, destination.GetSlot(0).quantity);
        Assert.AreEqual(5, source.GetSlot(0).quantity);
    }

    [Test]
    public void TransferToContainer_FromEmptySlot_DoesNothing()
    {
        Assert.AreEqual(TransferOutcome.None,
            ItemTransferService.TransferToContainer(source, 0, destination).Outcome);
    }

    // =========================================================================
    // SPACE FOR
    // =========================================================================

    [Test]
    public void SpaceFor_EmptyContainer_IsSlotsTimesStackSize()
    {
        Assert.AreEqual(40, ItemTransferService.SpaceFor(destination, null, carrot)); // 4 x 10
    }

    [Test]
    public void SpaceFor_CountsPartialStacksAndEmptySlots()
    {
        destination.SetSlot(0, new ItemStack(carrot, 7)); // 3 free
        destination.SetSlot(1, new ItemStack(tomato, 4)); // unusable for carrot

        Assert.AreEqual(23, ItemTransferService.SpaceFor(destination, null, carrot)); // 3 + 10 + 10
    }

    [Test]
    public void SpaceFor_IgnoresSlotsThePolicyRefuses()
    {
        var onlySlotZero = new FakePolicy { Accept = (_, index) => index == 0 };

        Assert.AreEqual(10, ItemTransferService.SpaceFor(destination, onlySlotZero, carrot));
    }

    [Test]
    public void SpaceFor_NullArguments_AreZero()
    {
        Assert.AreEqual(0, ItemTransferService.SpaceFor(null, null, carrot));
        Assert.AreEqual(0, ItemTransferService.SpaceFor(destination, null, null));
    }

    // =========================================================================
    // ENCAPSULATION — the Etapa 0 GetSlot finding
    // =========================================================================

    [Test]
    public void Transfer_GoesThroughSetSlot_SoSubscribersSeeEveryChange()
    {
        // GetSlot hands out the live internal ItemStack, so a transfer written the lazy way
        // would mutate it directly and never fire OnSlotChanged, leaving the UI stale.
        // ContainerView (Etapa 3) drives all its refreshing off this event.
        source.SetSlot(0, new ItemStack(carrot, 5));

        var touchedSource = new List<int>();
        var touchedDestination = new List<int>();
        source.OnSlotChanged += (i, _) => touchedSource.Add(i);
        destination.OnSlotChanged += (i, _) => touchedDestination.Add(i);

        ItemTransferService.Transfer(source, 0, destination, 2);

        CollectionAssert.Contains(touchedSource, 0);
        CollectionAssert.Contains(touchedDestination, 2);
    }

    [Test]
    public void Transfer_DoesNotMutateTheSourceContainersLiveStack()
    {
        source.SetSlot(0, new ItemStack(carrot, 5));
        var rejectAll = new FakePolicy { Accept = (_, __) => false };

        ItemTransferService.Transfer(source, 0, destination, 0, toPolicy: rejectAll);

        Assert.AreEqual(5, source.GetSlot(0).quantity,
            "a refused transfer must leave the source byte-for-byte untouched");
    }

    // =========================================================================
    // Test double
    // =========================================================================

    private class FakePolicy : IContainerPolicy
    {
        public System.Func<Item, int, bool> Accept = (_, __) => true;
        public System.Func<int, bool> Withdraw = _ => true;

        public int AcceptedCalls;
        public int AcceptedQuantity;
        public int RejectedCalls;
        public Item RejectedItem;

        public SlotRole GetRole(int slotIndex) => SlotRole.Storage;
        public bool CanAccept(Item item, int slotIndex) => Accept(item, slotIndex);
        public bool CanWithdraw(int slotIndex) => Withdraw(slotIndex);

        public void OnAccepted(Item item, int quantity)
        {
            AcceptedCalls++;
            AcceptedQuantity = quantity;
        }

        public void OnRejected(Item item, int slotIndex)
        {
            RejectedCalls++;
            RejectedItem = item;
        }
    }
}
