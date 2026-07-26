using UnityEngine;

namespace SowurShield.Inventory
{
    /// <summary>
    /// Turns "slot A was dropped on slot B" into a single <see cref="ItemTransferService"/> call,
    /// whatever containers those slots belong to.
    ///
    /// Replaces the five-branch if-else in <c>InventorySlot.OnDrop</c> that named SellBox and the
    /// trough explicitly and ran <c>FindFirstObjectByType&lt;SellBox&gt;()</c> on every drop.
    /// A new container type now needs a policy and a ContainerView — nothing here changes.
    ///
    /// THE TRICK THAT MAKES THIS WORK — "restore, then transfer".
    /// Drag is destructive for player-inventory slots: <c>SlotDragHandler.BeginDrag</c> calls
    /// <c>Inventory.ClearSlotForDrag</c>, so by drop time the source slot is empty in the
    /// container and the item exists only in the drag handler. SellBox and trough slots are the
    /// opposite — they keep the item in their container while dragging. That asymmetry is what
    /// forced the old branching.
    ///
    /// Instead of teaching the transfer service about payloads, the router simply puts the
    /// dragged item back into its source slot before transferring. From there every case is
    /// identical: a normal container-to-container move, with the service's existing atomicity
    /// and policy guarantees. The restore and the move happen in the same frame, so nothing
    /// renders in between.
    /// </summary>
    public static class SlotTransferRouter
    {
        /// <summary>Where a slot lives, resolved from the slot alone.</summary>
        private readonly struct SlotBinding
        {
            public readonly IInventoryContainer Container;
            public readonly int Index;
            public readonly IContainerPolicy Policy;

            /// <summary>True when the drag already removed the item from the container.</summary>
            public readonly bool DragWasDestructive;

            /// <summary>Set for player-inventory slots, so hotbar bookkeeping can run after a move.</summary>
            public readonly Inventory PlayerInventory;

            public bool IsValid => Container != null && Index >= 0;

            public SlotBinding(IInventoryContainer container, int index, IContainerPolicy policy,
                bool dragWasDestructive, Inventory playerInventory)
            {
                Container = container;
                Index = index;
                Policy = policy ?? DefaultContainerPolicy.Instance;
                DragWasDestructive = dragWasDestructive;
                PlayerInventory = playerInventory;
            }

            public static readonly SlotBinding None = new SlotBinding(null, -1, null, false, null);
        }

        /// <summary>
        /// Perform the drop. Returns the outcome so the caller can drive feedback; the source
        /// slot's drag is always marked successful afterwards, because the containers already
        /// hold the correct state and letting SlotDragHandler "restore" on top would duplicate.
        /// </summary>
        public static TransferResult Route(InventorySlot from, InventorySlot to)
        {
            if (from == null || to == null || from == to)
                return TransferResult.Nothing;

            SlotBinding source = Resolve(from);
            SlotBinding destination = Resolve(to);

            if (!source.IsValid || !destination.IsValid)
                return TransferResult.Nothing;

            ItemStack payload = from.GetDraggedItem();
            if (payload == null || payload.IsEmpty)
                return TransferResult.Nothing;

            // Put the item back where it came from so both sides look the same to the service.
            if (source.DragWasDestructive)
                source.Container.SetSlot(source.Index, payload.Clone());

            TransferResult result = ItemTransferService.Transfer(
                source.Container, source.Index,
                destination.Container, destination.Index,
                source.Policy, destination.Policy);

            // The containers are authoritative now, whatever the outcome — a refusal simply left
            // the item in the source slot we just restored.
            from.MarkDragSuccessful();

            NotifyInventory(source, destination, result);
            return result;
        }

        /// <summary>
        /// Hotbar auto-refill and the drop sound used to live inside Inventory.HandleSlotDrop.
        /// They are Inventory's business, not the transfer's, so they are signalled here.
        /// </summary>
        private static void NotifyInventory(SlotBinding source, SlotBinding destination, TransferResult result)
        {
            if (!result.Moved) return;

            source.PlayerInventory?.OnSlotsChangedExternally(source.Index);
            destination.PlayerInventory?.OnSlotsChangedExternally(destination.Index);
        }

        /// <summary>
        /// Work out which container a slot belongs to. Slots built by a ContainerView carry a
        /// back-reference; player-inventory slots are still built by Inventory itself and are
        /// resolved through it (Etapa 4a-bis will move those to a view as well).
        /// </summary>
        private static SlotBinding Resolve(InventorySlot slot)
        {
            ContainerView view = slot.OwnerView;
            if (view != null && view.Container != null)
            {
                int index = view.IndexOf(slot);
                return index >= 0
                    ? new SlotBinding(view.Container, index, view.Policy, false, null)
                    : SlotBinding.None;
            }

            Inventory inventory = slot.InventoryManager;
            if (inventory != null && inventory.Container != null)
            {
                int index = inventory.IndexOfSlot(slot);
                return index >= 0
                    ? new SlotBinding(inventory.Container, index, DefaultContainerPolicy.Instance, true, inventory)
                    : SlotBinding.None;
            }

            return SlotBinding.None;
        }
    }
}
