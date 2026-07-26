using System;
using System.Collections.Generic;

namespace SowurShield.Inventory.Policies
{
    /// <summary>
    /// Rules for the FeedingTrough.
    ///
    /// ⚠️ BEHAVIOUR FLAG — read before flipping it.
    /// The trough today accepts ANY item and simply ignores whatever no animal eats when
    /// <c>OnDayChanged</c> runs. Rejecting non-food at drop time would be a nicer game, but it is
    /// a gameplay change, not a refactor, so <see cref="RejectNonFood"/> defaults to <c>false</c>
    /// and this policy preserves the current behaviour exactly. Etapa 4 can ship without
    /// changing how the game plays; flipping the flag is then a separate, revertable decision.
    ///
    /// What counts as food is supplied by a callback rather than read from AnimalZone directly:
    /// the accepted set depends on which animals are in the linked zone right now, and keeping
    /// it behind a delegate is what lets this class be tested without a scene, an AnimalZone or
    /// a populated ItemDatabase.
    /// </summary>
    public class FeedingTroughPolicy : IContainerPolicy
    {
        private readonly Func<IEnumerable<Item>> acceptedFoodProvider;
        private readonly Action<Item, int> onAccepted;
        private readonly Action<Item, int> onRejected;

        /// <summary>
        /// When false (the default, and what the game does today) every item is accepted and
        /// non-food is simply never consumed. When true, only food for animals currently in the
        /// linked zone can be dropped in.
        /// </summary>
        public bool RejectNonFood { get; set; }

        /// <param name="acceptedFoodProvider">
        /// Returns the items animals in the linked zone eat — for FeedingTrough that is every
        /// <c>AnimalData.dailyFoodRequirements</c> entry resolved through <c>ItemDatabase</c>.
        /// Only called when <see cref="RejectNonFood"/> is true.
        /// </param>
        public FeedingTroughPolicy(
            Func<IEnumerable<Item>> acceptedFoodProvider = null,
            Action<Item, int> onAccepted = null,
            Action<Item, int> onRejected = null)
        {
            this.acceptedFoodProvider = acceptedFoodProvider;
            this.onAccepted = onAccepted;
            this.onRejected = onRejected;
        }

        public SlotRole GetRole(int slotIndex) => SlotRole.Storage;

        public bool CanAccept(Item item, int slotIndex)
        {
            if (item == null) return false;
            if (!RejectNonFood) return true;

            IEnumerable<Item> food = acceptedFoodProvider?.Invoke();

            // No zone, no animals, or no way to tell — stay permissive rather than locking the
            // player out of a trough they can't fill.
            if (food == null) return true;

            foreach (Item accepted in food)
                if (accepted == item) return true;

            return false;
        }

        public bool CanWithdraw(int slotIndex) => true;

        public void OnAccepted(Item item, int quantity) => onAccepted?.Invoke(item, quantity);

        public void OnRejected(Item item, int slotIndex) => onRejected?.Invoke(item, slotIndex);
    }
}
