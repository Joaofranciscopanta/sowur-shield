namespace SowurShield.Inventory
{
    /// <summary>
    /// What a slot is for. Storage is the normal case; Input/Output exist so a crafting station
    /// can put both kinds of slot in ONE container instead of needing two containers and a
    /// special case inside the transfer code.
    ///
    /// IMPORTANT: this is descriptive metadata for the UI (render an output slot differently,
    /// label an input slot). It is NOT the permission check — <see cref="ItemTransferService"/>
    /// never reads it. Permission lives entirely in <see cref="IContainerPolicy.CanAccept"/> and
    /// <see cref="IContainerPolicy.CanWithdraw"/> so there is exactly one source of truth. A
    /// crafting policy returns <c>Output</c> for its result slot AND <c>CanAccept == false</c>
    /// for it.
    /// </summary>
    public enum SlotRole
    {
        /// <summary>Items go in and out freely.</summary>
        Storage,

        /// <summary>Feeds a process — e.g. a crafting ingredient.</summary>
        Input,

        /// <summary>Produced by a process — e.g. a crafting result.</summary>
        Output
    }

    /// <summary>
    /// The rules a container applies to item movement, and its reaction to the outcome.
    ///
    /// This is THE extension point of the container architecture: a new container type
    /// (chest, shop, crafting bench) should be a new policy plus scene wiring, without touching
    /// <see cref="ItemTransferService"/>, <see cref="InventorySlot"/> or any existing container.
    ///
    /// Everything is per-slot rather than per-container, because a crafting bench needs
    /// different rules for its input and output slots. Containers with uniform rules just
    /// ignore the index — see <see cref="DefaultContainerPolicy"/>.
    ///
    /// Implementations must be side-effect free in the Can* methods: the transfer service
    /// queries them while deciding, possibly several times, before anything actually moves.
    /// Sounds, particles and sprite changes belong in <see cref="OnAccepted"/> /
    /// <see cref="OnRejected"/>, which fire exactly once per transfer.
    /// </summary>
    public interface IContainerPolicy
    {
        /// <summary>
        /// What the slot at this index is for. Metadata for the UI only — the transfer service
        /// does not read this. See <see cref="SlotRole"/>.
        /// </summary>
        SlotRole GetRole(int slotIndex);

        /// <summary>
        /// Can this item be placed in this slot? Called before anything moves.
        /// The SellBox rejects items with <c>canBeSold == false</c>; a feeding trough rejects
        /// anything no animal eats.
        /// </summary>
        bool CanAccept(Item item, int slotIndex);

        /// <summary>Can items be taken out of this slot? False for a crafting input.</summary>
        bool CanWithdraw(int slotIndex);

        /// <summary>Fired once after items actually landed in this container.</summary>
        void OnAccepted(Item item, int quantity);

        /// <summary>Fired once when a transfer into this container was refused.</summary>
        void OnRejected(Item item, int slotIndex);
    }

    /// <summary>
    /// Everything is storage, everything is allowed, nothing reacts.
    ///
    /// This is the whole implementation a plain chest needs, and it is what the player
    /// inventory uses. Also handy as a base class: override only the one method you care about.
    /// </summary>
    public class DefaultContainerPolicy : IContainerPolicy
    {
        /// <summary>Shared stateless instance — this policy holds no state, so one is enough.</summary>
        public static readonly DefaultContainerPolicy Instance = new DefaultContainerPolicy();

        public virtual SlotRole GetRole(int slotIndex) => SlotRole.Storage;

        public virtual bool CanAccept(Item item, int slotIndex) => item != null;

        public virtual bool CanWithdraw(int slotIndex) => true;

        public virtual void OnAccepted(Item item, int quantity) { }

        public virtual void OnRejected(Item item, int slotIndex) { }
    }
}
