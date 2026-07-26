using System;

namespace SowurShield.Inventory.Policies
{
    /// <summary>
    /// Rules for the SellBox: only items flagged <c>canBeSold</c> get in.
    ///
    /// That check currently lives inline in <c>SellBox.HandleSlotDrop</c>, which means it only
    /// applies on that one code path — anything reaching the container another way bypasses it.
    /// As a policy it is enforced by <see cref="ItemTransferService"/> for every route in.
    ///
    /// Deliberately free of MonoBehaviour and of any reference to SellBox itself: feedback is
    /// delivered through the callbacks, so this class stays unit-testable and SellBox keeps
    /// owning its own sounds, particles and sprite swapping.
    /// </summary>
    public class SellBoxPolicy : IContainerPolicy
    {
        private readonly Action<Item, int> onAccepted;
        private readonly Action<Item, int> onRejected;

        /// <param name="onAccepted">(item, quantity) — SellBox plays its place sound and refreshes the total.</param>
        /// <param name="onRejected">(item, slotIndex) — SellBox flashes the red reject highlight. slotIndex is -1 when the drop targeted the container rather than a slot.</param>
        public SellBoxPolicy(Action<Item, int> onAccepted = null, Action<Item, int> onRejected = null)
        {
            this.onAccepted = onAccepted;
            this.onRejected = onRejected;
        }

        public SlotRole GetRole(int slotIndex) => SlotRole.Storage;

        public bool CanAccept(Item item, int slotIndex) => item != null && item.canBeSold;

        public bool CanWithdraw(int slotIndex) => true;

        public void OnAccepted(Item item, int quantity) => onAccepted?.Invoke(item, quantity);

        public void OnRejected(Item item, int slotIndex) => onRejected?.Invoke(item, slotIndex);
    }
}
