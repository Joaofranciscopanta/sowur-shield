using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using SowurShield.Inventory;
using SowurShield.Inventory.Policies;

/// <summary>
/// Edit Mode tests for the concrete container policies — Etapa 2 of
/// review/04_CONTAINER_REFACTOR_PLAN.md.
///
/// Each policy is tested twice: on its own, and through ItemTransferService, because a policy
/// that answers correctly but is wired into the service wrongly still lets the wrong item into
/// the wrong container.
///
/// Nothing in production uses these yet — SellBox and FeedingTrough get them in Etapa 3/4.
/// </summary>
public class ContainerPolicyTests
{
    private Item carrot;      // sellable, and food
    private Item questItem;   // canBeSold = false
    private Item rock;        // sellable, not food

    private InventoryContainer source;
    private InventoryContainer destination;

    [SetUp]
    public void SetUp()
    {
        carrot = MakeItem("Carrot", sellable: true);
        questItem = MakeItem("AncientRelic", sellable: false);
        rock = MakeItem("Rock", sellable: true);

        source = new InventoryContainer(4, "source");
        destination = new InventoryContainer(4, "destination");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(carrot);
        Object.DestroyImmediate(questItem);
        Object.DestroyImmediate(rock);
    }

    private static Item MakeItem(string name, bool sellable)
    {
        var item = ScriptableObject.CreateInstance<Item>();
        item.itemName = name;
        item.isStackable = true;
        item.maxStackSize = 10;
        item.baseValue = 5;
        item.canBeSold = sellable;
        return item;
    }

    // =========================================================================
    // SELLBOX POLICY
    // =========================================================================

    [Test]
    public void SellBox_AcceptsSellableItems()
    {
        var policy = new SellBoxPolicy();
        Assert.IsTrue(policy.CanAccept(carrot, 0));
    }

    [Test]
    public void SellBox_RejectsUnsellableItems()
    {
        var policy = new SellBoxPolicy();
        Assert.IsFalse(policy.CanAccept(questItem, 0));
    }

    [Test]
    public void SellBox_RejectsNull()
    {
        Assert.IsFalse(new SellBoxPolicy().CanAccept(null, 0));
    }

    [Test]
    public void SellBox_RuleIsTheSameForEverySlot()
    {
        var policy = new SellBoxPolicy();

        for (int i = 0; i < 12; i++)
        {
            Assert.IsTrue(policy.CanAccept(carrot, i));
            Assert.IsFalse(policy.CanAccept(questItem, i));
        }
    }

    [Test]
    public void SellBox_AlwaysAllowsTakingItemsBackOut()
    {
        // Players must be able to pull something out of the box before they sleep on it.
        Assert.IsTrue(new SellBoxPolicy().CanWithdraw(0));
    }

    [Test]
    public void SellBox_CallbacksAreOptional()
    {
        var policy = new SellBoxPolicy();

        Assert.DoesNotThrow(() => policy.OnAccepted(carrot, 1));
        Assert.DoesNotThrow(() => policy.OnRejected(carrot, 0));
    }

    [Test]
    public void SellBox_ForwardsAcceptedItemAndQuantity()
    {
        Item seen = null;
        int quantity = 0;
        var policy = new SellBoxPolicy(onAccepted: (i, q) => { seen = i; quantity = q; });

        policy.OnAccepted(carrot, 4);

        Assert.AreEqual(carrot, seen);
        Assert.AreEqual(4, quantity);
    }

    // --- through the transfer service ---

    [Test]
    public void SellBox_ThroughService_RefusesUnsellableItemAndLeavesItInPlace()
    {
        source.SetSlot(0, new ItemStack(questItem, 2));
        Item rejected = null;
        var policy = new SellBoxPolicy(onRejected: (i, _) => rejected = i);

        var result = ItemTransferService.TransferToContainer(source, 0, destination, toPolicy: policy);

        Assert.AreEqual(TransferOutcome.Rejected, result.Outcome);
        Assert.AreEqual(questItem, rejected, "the SellBox needs the item to flash the reject highlight");
        Assert.AreEqual(2, source.GetSlot(0).quantity);
        Assert.AreEqual(0, destination.GetAllItems().Count);
    }

    [Test]
    public void SellBox_ThroughService_AcceptsSellableItem()
    {
        source.SetSlot(0, new ItemStack(carrot, 3));
        int acceptedQuantity = 0;
        var policy = new SellBoxPolicy(onAccepted: (_, q) => acceptedQuantity = q);

        var result = ItemTransferService.TransferToContainer(source, 0, destination, toPolicy: policy);

        Assert.AreEqual(TransferOutcome.Moved, result.Outcome);
        Assert.AreEqual(3, acceptedQuantity);
        Assert.AreEqual(3, destination.GetItemCount(carrot));
    }

    [Test]
    public void SellBox_ThroughService_BlocksUnsellableItemOnASlotDropToo()
    {
        // The inline check in SellBox.HandleSlotDrop only guards the container drop path.
        // As a policy it covers slot-targeted drops as well.
        source.SetSlot(0, new ItemStack(questItem, 1));

        var result = ItemTransferService.Transfer(source, 0, destination, 2, toPolicy: new SellBoxPolicy());

        Assert.AreEqual(TransferOutcome.Rejected, result.Outcome);
        Assert.IsTrue(destination.GetSlot(2).IsEmpty);
    }

    [Test]
    public void SellBox_ThroughService_WontLetASwapPushAnUnsellableItemIn()
    {
        // Dragging a carrot onto an unsellable item that somehow sits in the box would swap the
        // two — the policy has to catch that direction as well.
        source.SetSlot(0, new ItemStack(carrot, 1));
        destination.SetSlot(0, new ItemStack(rock, 1));

        var noRocks = new SellBoxPolicy();
        rock.canBeSold = false; // rock became unsellable after it was already in the box

        var result = ItemTransferService.Transfer(
            source, 0, destination, 0, fromPolicy: noRocks, toPolicy: noRocks);

        Assert.AreEqual(TransferOutcome.Rejected, result.Outcome);
        Assert.AreEqual(rock, destination.GetSlot(0).item, "nothing moved");
    }

    // =========================================================================
    // FEEDING TROUGH POLICY
    // =========================================================================

    [Test]
    public void Trough_ByDefault_AcceptsAnything()
    {
        // DOCUMENTS CURRENT GAME BEHAVIOUR: the trough takes any item and simply never consumes
        // what no animal eats. Etapa 4 must not change this, so the default has to stay false.
        var policy = new FeedingTroughPolicy(() => new[] { carrot });

        Assert.IsFalse(policy.RejectNonFood, "flipping this default is a gameplay change, not a refactor");
        Assert.IsTrue(policy.CanAccept(rock, 0));
    }

    [Test]
    public void Trough_RejectsNullRegardless()
    {
        Assert.IsFalse(new FeedingTroughPolicy().CanAccept(null, 0));
    }

    [Test]
    public void Trough_WithRejectNonFood_AcceptsFood()
    {
        var policy = new FeedingTroughPolicy(() => new[] { carrot }) { RejectNonFood = true };

        Assert.IsTrue(policy.CanAccept(carrot, 0));
    }

    [Test]
    public void Trough_WithRejectNonFood_RejectsNonFood()
    {
        var policy = new FeedingTroughPolicy(() => new[] { carrot }) { RejectNonFood = true };

        Assert.IsFalse(policy.CanAccept(rock, 0));
    }

    [Test]
    public void Trough_WithRejectNonFood_StaysPermissiveWhenTheFoodListIsUnavailable()
    {
        // No linked zone, no animals, or ItemDatabase not resolving names yet. Locking the player
        // out of a trough they cannot fill would be worse than accepting too much.
        var noProvider = new FeedingTroughPolicy(null) { RejectNonFood = true };
        var nullList = new FeedingTroughPolicy(() => null) { RejectNonFood = true };

        Assert.IsTrue(noProvider.CanAccept(rock, 0));
        Assert.IsTrue(nullList.CanAccept(rock, 0));
    }

    [Test]
    public void Trough_WithRejectNonFood_EmptyFoodListRejectsEverything()
    {
        // An empty list is different from a null one: it means "the zone is empty / these animals
        // eat nothing", which is a real answer.
        var policy = new FeedingTroughPolicy(() => new List<Item>()) { RejectNonFood = true };

        Assert.IsFalse(policy.CanAccept(carrot, 0));
    }

    [Test]
    public void Trough_FoodListIsReadEveryTime_NotCached()
    {
        // Animals move between zones, so the accepted set changes at runtime.
        var food = new List<Item>();
        var policy = new FeedingTroughPolicy(() => food) { RejectNonFood = true };

        Assert.IsFalse(policy.CanAccept(carrot, 0));

        food.Add(carrot);

        Assert.IsTrue(policy.CanAccept(carrot, 0), "a chicken moved into the zone; grain is food now");
    }

    [Test]
    public void Trough_AlwaysAllowsTakingItemsBackOut()
    {
        Assert.IsTrue(new FeedingTroughPolicy().CanWithdraw(0));
        Assert.IsTrue(new FeedingTroughPolicy() { RejectNonFood = true }.CanWithdraw(0));
    }

    // --- through the transfer service ---

    [Test]
    public void Trough_ThroughService_DefaultLetsNonFoodIn()
    {
        source.SetSlot(0, new ItemStack(rock, 2));
        var policy = new FeedingTroughPolicy(() => new[] { carrot });

        var result = ItemTransferService.TransferToContainer(source, 0, destination, toPolicy: policy);

        Assert.AreEqual(TransferOutcome.Moved, result.Outcome);
        Assert.AreEqual(2, destination.GetItemCount(rock));
    }

    [Test]
    public void Trough_ThroughService_WithRejectNonFood_BlocksNonFood()
    {
        source.SetSlot(0, new ItemStack(rock, 2));
        var policy = new FeedingTroughPolicy(() => new[] { carrot }) { RejectNonFood = true };

        var result = ItemTransferService.TransferToContainer(source, 0, destination, toPolicy: policy);

        Assert.AreEqual(TransferOutcome.Rejected, result.Outcome);
        Assert.AreEqual(2, source.GetSlot(0).quantity);
    }

    [Test]
    public void Trough_ThroughService_PlayerCanAlwaysPullFoodBackOut()
    {
        // Trough -> inventory, the direction InventorySlot.OnDrop handles inline today.
        var trough = new InventoryContainer(4, "trough");
        trough.SetSlot(0, new ItemStack(carrot, 5));
        var policy = new FeedingTroughPolicy(() => new[] { carrot }) { RejectNonFood = true };

        var result = ItemTransferService.TransferToContainer(
            trough, 0, destination, fromPolicy: policy);

        Assert.AreEqual(TransferOutcome.Moved, result.Outcome);
        Assert.AreEqual(5, destination.GetItemCount(carrot));
        Assert.IsTrue(trough.GetSlot(0).IsEmpty);
    }

    // =========================================================================
    // DEFAULT POLICY — what a plain chest needs, and nothing more
    // =========================================================================

    [Test]
    public void Default_AcceptsAnyItemInAnySlot()
    {
        var policy = DefaultContainerPolicy.Instance;

        Assert.IsTrue(policy.CanAccept(carrot, 0));
        Assert.IsTrue(policy.CanAccept(questItem, 7));
        Assert.IsTrue(policy.CanWithdraw(3));
        Assert.AreEqual(SlotRole.Storage, policy.GetRole(0));
    }

    [Test]
    public void Default_RejectsNull()
    {
        Assert.IsFalse(DefaultContainerPolicy.Instance.CanAccept(null, 0));
    }

    [Test]
    public void Default_IsSubclassableForOneOffRules()
    {
        // The extension story for a chest with a twist: override one method, inherit the rest.
        var policy = new OnlySellableChest();

        Assert.IsTrue(policy.CanAccept(carrot, 0));
        Assert.IsFalse(policy.CanAccept(questItem, 0));
        Assert.IsTrue(policy.CanWithdraw(0), "inherited untouched");
    }

    private class OnlySellableChest : DefaultContainerPolicy
    {
        public override bool CanAccept(Item item, int slotIndex) => item != null && item.canBeSold;
    }
}
