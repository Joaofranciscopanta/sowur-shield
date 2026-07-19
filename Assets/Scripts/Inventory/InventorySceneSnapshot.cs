using System.Collections.Generic;
using UnityEngine;

namespace SowurShield.Inventory
{
    /// <summary>
    /// In-memory snapshot of the player inventory, used to carry items across the
    /// farm → combat → farm scene round-trip. The Inventory lives in SampleScene and
    /// is rebuilt on scene reload; restoring from disk is not an option because demo
    /// builds ship with saving disabled (see SaveManager DEMO_BUILD gate), which was
    /// wiping the inventory after every battle (QA_UI_AUDIT_2026-07-05, ERRO-02).
    ///
    /// Capture right before loading CombatScene; the snapshot is consumed by the
    /// first Inventory that restores it.
    /// </summary>
    public static class InventorySceneSnapshot
    {
        private struct SlotSnapshot
        {
            public int index;
            public Item item;
            public int quantity;
        }

        private static List<SlotSnapshot> slots;

        public static bool HasSnapshot => slots != null;

        public static void Capture(Inventory inventory)
        {
            if (inventory == null) return;

            slots = new List<SlotSnapshot>();
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                ItemStack stack = inventory.GetSlotAt(i);
                if (stack == null || stack.IsEmpty) continue;
                slots.Add(new SlotSnapshot { index = i, item = stack.item, quantity = stack.quantity });
            }
        }

        /// <summary>
        /// Restores the snapshot into the inventory and consumes it. The snapshot is
        /// always newer than any in-memory save data (it is captured at battle start),
        /// so it overwrites whatever SaveManager.ReapplyLoadedDataToRegisteredObjects
        /// put there — call this AFTER that re-apply (SceneTransitionManager does).
        /// </summary>
        public static void TryRestore(Inventory inventory)
        {
            if (slots == null || inventory == null) return;

            for (int i = 0; i < inventory.SlotCount; i++)
                inventory.SetSlotAt(i, new ItemStack());

            foreach (SlotSnapshot slot in slots)
            {
                if (slot.item == null) continue; // asset unloaded — skip defensively
                inventory.SetSlotAt(slot.index, new ItemStack(slot.item, slot.quantity));
            }

            slots = null;
        }

        public static void Clear() => slots = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => slots = null;
    }
}
