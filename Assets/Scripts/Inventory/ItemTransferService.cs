using UnityEngine;

namespace SowurShield.Inventory
{
    /// <summary>How a transfer ended.</summary>
    public enum TransferOutcome
    {
        /// <summary>Nothing to move (empty source, or source and destination are the same slot).</summary>
        None,

        /// <summary>The whole requested quantity moved.</summary>
        Moved,

        /// <summary>Some of the requested quantity moved; the rest stayed in the source.</summary>
        Partial,

        /// <summary>Two different stacks changed places.</summary>
        Swapped,

        /// <summary>A policy refused the item.</summary>
        Rejected,

        /// <summary>The rules allowed it but there was no room.</summary>
        Full
    }

    /// <summary>The result of a transfer attempt.</summary>
    public readonly struct TransferResult
    {
        public readonly TransferOutcome Outcome;
        public readonly int QuantityMoved;

        public TransferResult(TransferOutcome outcome, int quantityMoved = 0)
        {
            Outcome = outcome;
            QuantityMoved = quantityMoved;
        }

        /// <summary>True if anything at all left the source container.</summary>
        public bool Moved => QuantityMoved > 0 || Outcome == TransferOutcome.Swapped;

        /// <summary>True if the transfer was refused — by a rule or for lack of room.</summary>
        public bool Refused => Outcome == TransferOutcome.Rejected || Outcome == TransferOutcome.Full;

        public static readonly TransferResult Nothing = new TransferResult(TransferOutcome.None);

        public override string ToString() => $"{Outcome} ({QuantityMoved})";
    }

    /// <summary>
    /// Moves items between containers. Pure C#, no MonoBehaviour, no scene — so it is fully
    /// unit-testable, which the four hand-written copies it replaces never were.
    ///
    /// Replaces: <c>Inventory.HandleSlotDrop</c>, <c>SellBox.HandleSlotDrop</c>,
    /// <c>SellBox.HandleSellBoxInternalMove</c>, <c>SellBox.HandleSellBoxToInventoryDrop</c>,
    /// plus the inline trough handling in <c>InventorySlot.OnDrop</c>.
    ///
    /// TWO RULES THIS CLASS FOLLOWS, both from the Etapa 0 findings (see
    /// review/04_CONTAINER_REFACTOR_PLAN.md §5):
    ///
    /// 1. It never calls <c>IInventoryContainer.AddItem</c>/<c>RemoveItem</c>. Those are not
    ///    atomic — they apply a partial result and still return false — so building transfer
    ///    logic on them means a failed move can leave items duplicated or destroyed. This class
    ///    computes exactly how much fits first, then writes that amount.
    ///
    /// 2. It never mutates a stack returned by <c>GetSlot</c>. That method hands back the live
    ///    internal reference, so mutating it would change the container without firing
    ///    <c>OnSlotChanged</c> and leave every subscribed UI stale. Everything goes through
    ///    <c>Clone()</c> then <c>SetSlot</c>.
    /// </summary>
    public static class ItemTransferService
    {
        /// <summary>Pass as <c>quantity</c> to move the whole source stack.</summary>
        public const int All = -1;

        // =====================================================================
        // SLOT -> SLOT
        // =====================================================================

        /// <summary>
        /// Move items from one specific slot to another specific slot — the drag-and-drop case.
        /// Handles the four scenarios: empty destination, stacking (with leftover), and swapping
        /// two incompatible stacks. Works within a single container too (<paramref name="from"/>
        /// and <paramref name="to"/> may be the same instance).
        /// </summary>
        /// <param name="quantity"><see cref="All"/> for the whole stack, otherwise a positive amount.</param>
        public static TransferResult Transfer(
            IInventoryContainer from, int fromIndex,
            IInventoryContainer to, int toIndex,
            IContainerPolicy fromPolicy = null,
            IContainerPolicy toPolicy = null,
            int quantity = All)
        {
            if (from == null || to == null) return TransferResult.Nothing;

            fromPolicy = fromPolicy ?? DefaultContainerPolicy.Instance;
            toPolicy = toPolicy ?? DefaultContainerPolicy.Instance;

            if (!IsValidIndex(from, fromIndex) || !IsValidIndex(to, toIndex))
                return TransferResult.Nothing;

            // Dropping a slot onto itself is a no-op, not an error.
            if (ReferenceEquals(from, to) && fromIndex == toIndex)
                return TransferResult.Nothing;

            ItemStack source = from.GetSlot(fromIndex).Clone();
            if (source.IsEmpty) return TransferResult.Nothing;

            if (!fromPolicy.CanWithdraw(fromIndex))
                return new TransferResult(TransferOutcome.Rejected);

            int requested = ResolveQuantity(quantity, source.quantity);
            if (requested <= 0) return TransferResult.Nothing;

            if (!toPolicy.CanAccept(source.item, toIndex))
            {
                toPolicy.OnRejected(source.item, toIndex);
                return new TransferResult(TransferOutcome.Rejected);
            }

            ItemStack destination = to.GetSlot(toIndex).Clone();

            if (destination.IsEmpty)
                return MoveIntoEmptySlot(from, fromIndex, to, toIndex, toPolicy, source, requested);

            if (destination.CanStack(source.item))
                return MergeIntoStack(from, fromIndex, to, toIndex, toPolicy, source, destination, requested);

            return SwapStacks(from, fromIndex, to, toIndex, fromPolicy, toPolicy, source, destination, requested);
        }

        private static TransferResult MoveIntoEmptySlot(
            IInventoryContainer from, int fromIndex,
            IInventoryContainer to, int toIndex,
            IContainerPolicy toPolicy,
            ItemStack source, int requested)
        {
            // A single slot can never hold more than one full stack.
            int moved = Mathf.Min(requested, source.item.maxStackSize);

            to.SetSlot(toIndex, new ItemStack(source.item, moved));
            WriteBackSource(from, fromIndex, source, moved);

            toPolicy.OnAccepted(source.item, moved);
            return new TransferResult(
                moved == requested ? TransferOutcome.Moved : TransferOutcome.Partial, moved);
        }

        private static TransferResult MergeIntoStack(
            IInventoryContainer from, int fromIndex,
            IInventoryContainer to, int toIndex,
            IContainerPolicy toPolicy,
            ItemStack source, ItemStack destination, int requested)
        {
            int moved = Mathf.Min(requested, destination.AvailableSpace);

            // Defensive: CanStack() already guarantees free space, so this should be unreachable
            // from Transfer(). A full destination stack of the same item falls through to
            // SwapStacks instead, which matches how Inventory.HandleSlotDrop behaves today.
            if (moved <= 0)
            {
                toPolicy.OnRejected(source.item, toIndex);
                return new TransferResult(TransferOutcome.Full);
            }

            destination.quantity += moved;
            to.SetSlot(toIndex, destination);
            WriteBackSource(from, fromIndex, source, moved);

            toPolicy.OnAccepted(source.item, moved);
            return new TransferResult(
                moved == requested ? TransferOutcome.Moved : TransferOutcome.Partial, moved);
        }

        private static TransferResult SwapStacks(
            IInventoryContainer from, int fromIndex,
            IInventoryContainer to, int toIndex,
            IContainerPolicy fromPolicy, IContainerPolicy toPolicy,
            ItemStack source, ItemStack destination, int requested)
        {
            // A swap moves the destination stack backwards into the source slot, so the SOURCE
            // policy has to accept it too — dragging a carrot onto a hoe sitting in the SellBox
            // would push that hoe into the player inventory.
            // Swapping only makes sense for a whole stack — there is nowhere to put the remainder
            // once the destination stack lands in the source slot. Nothing moves.
            if (requested != source.quantity)
                return TransferResult.Nothing;

            if (!fromPolicy.CanAccept(destination.item, fromIndex))
            {
                fromPolicy.OnRejected(destination.item, fromIndex);
                return new TransferResult(TransferOutcome.Rejected);
            }

            to.SetSlot(toIndex, source);
            from.SetSlot(fromIndex, destination);

            toPolicy.OnAccepted(source.item, source.quantity);
            fromPolicy.OnAccepted(destination.item, destination.quantity);
            return new TransferResult(TransferOutcome.Swapped, source.quantity);
        }

        // =====================================================================
        // SLOT -> CONTAINER (first fit)
        // =====================================================================

        /// <summary>
        /// Move items from a slot into a container without naming a destination slot — stacking
        /// onto partial stacks first, then filling empty slots, exactly like
        /// <c>InventoryContainer.AddItem</c> but honouring per-slot policy and staying atomic.
        ///
        /// This is the "dropped an item on the SellBox" case, and the one a chest or a shop
        /// window will use.
        /// </summary>
        public static TransferResult TransferToContainer(
            IInventoryContainer from, int fromIndex,
            IInventoryContainer to,
            IContainerPolicy fromPolicy = null,
            IContainerPolicy toPolicy = null,
            int quantity = All)
        {
            if (from == null || to == null) return TransferResult.Nothing;

            fromPolicy = fromPolicy ?? DefaultContainerPolicy.Instance;
            toPolicy = toPolicy ?? DefaultContainerPolicy.Instance;

            if (!IsValidIndex(from, fromIndex)) return TransferResult.Nothing;

            ItemStack source = from.GetSlot(fromIndex).Clone();
            if (source.IsEmpty) return TransferResult.Nothing;

            if (!fromPolicy.CanWithdraw(fromIndex))
                return new TransferResult(TransferOutcome.Rejected);

            int requested = ResolveQuantity(quantity, source.quantity);
            if (requested <= 0) return TransferResult.Nothing;

            int capacity = SpaceFor(to, toPolicy, source.item);
            if (capacity <= 0)
            {
                // No room anywhere. Distinguishing this from a rule rejection matters: the
                // SellBox shows the same red flash for both, but a shop or a quest might not.
                toPolicy.OnRejected(source.item, -1);
                return new TransferResult(
                    AcceptsAnywhere(to, toPolicy, source.item) ? TransferOutcome.Full : TransferOutcome.Rejected);
            }

            int moved = Mathf.Min(requested, capacity);
            Insert(to, toPolicy, source.item, moved);
            WriteBackSource(from, fromIndex, source, moved);

            toPolicy.OnAccepted(source.item, moved);
            return new TransferResult(
                moved == source.quantity ? TransferOutcome.Moved : TransferOutcome.Partial, moved);
        }

        /// <summary>
        /// How many of this item the container could take right now, respecting policy per slot.
        /// Same shape as <c>InventoryContainer.CanAdd</c> but returns the amount instead of a
        /// bool, which is what makes an atomic transfer possible.
        /// </summary>
        public static int SpaceFor(IInventoryContainer container, IContainerPolicy policy, Item item)
        {
            if (container == null || item == null) return 0;
            policy = policy ?? DefaultContainerPolicy.Instance;

            int space = 0;
            for (int i = 0; i < container.MaxSlots; i++)
            {
                if (!policy.CanAccept(item, i)) continue;

                ItemStack slot = container.GetSlot(i);

                if (slot.IsEmpty)
                    space += item.maxStackSize;
                else if (slot.CanStack(item))
                    space += slot.AvailableSpace;
            }
            return space;
        }

        // =====================================================================
        // INTERNALS
        // =====================================================================

        /// <summary>Writes <paramref name="amount"/> of an item into the container, stacking first.</summary>
        private static void Insert(IInventoryContainer container, IContainerPolicy policy, Item item, int amount)
        {
            int remaining = amount;

            // Top up partial stacks before opening a new slot.
            if (item.isStackable)
            {
                for (int i = 0; i < container.MaxSlots && remaining > 0; i++)
                {
                    if (!policy.CanAccept(item, i)) continue;

                    ItemStack slot = container.GetSlot(i).Clone();
                    if (!slot.CanStack(item)) continue;

                    int add = Mathf.Min(remaining, slot.AvailableSpace);
                    if (add <= 0) continue;

                    slot.quantity += add;
                    container.SetSlot(i, slot);
                    remaining -= add;
                }
            }

            for (int i = 0; i < container.MaxSlots && remaining > 0; i++)
            {
                if (!policy.CanAccept(item, i)) continue;
                if (!container.GetSlot(i).IsEmpty) continue;

                int add = Mathf.Min(remaining, item.maxStackSize);
                container.SetSlot(i, new ItemStack(item, add));
                remaining -= add;
            }
        }

        /// <summary>Removes what was moved from the source slot, clearing it when it empties.</summary>
        private static void WriteBackSource(IInventoryContainer from, int fromIndex, ItemStack source, int moved)
        {
            int left = source.quantity - moved;
            from.SetSlot(fromIndex, left > 0 ? new ItemStack(source.item, left) : new ItemStack());
        }

        /// <summary>True if at least one slot would accept this item, regardless of free space.</summary>
        private static bool AcceptsAnywhere(IInventoryContainer container, IContainerPolicy policy, Item item)
        {
            for (int i = 0; i < container.MaxSlots; i++)
                if (policy.CanAccept(item, i)) return true;
            return false;
        }

        private static bool IsValidIndex(IInventoryContainer container, int index)
        {
            return index >= 0 && index < container.MaxSlots;
        }

        private static int ResolveQuantity(int requested, int available)
        {
            return requested == All ? available : Mathf.Clamp(requested, 0, available);
        }
    }
}
