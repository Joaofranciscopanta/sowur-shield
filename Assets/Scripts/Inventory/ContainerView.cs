using UnityEngine;
using System;
using System.Collections.Generic;

namespace SowurShield.Inventory
{
    /// <summary>
    /// Builds and refreshes the slot UI for one container.
    ///
    /// Every container in the game hand-rolled this: <c>Inventory.SetupUI</c>/<c>CreateSlotUI</c>/
    /// <c>UpdateSlot</c>/<c>UpdateAllSlots</c>, <c>SellBox.SetupUI</c>/<c>CreateSellBoxSlotUI</c>,
    /// <c>FeedingTrough.SetupUI</c>/<c>RefreshSlots</c> — four copies of instantiate-prefab,
    /// assign-index, push-stack, subscribe-to-changes.
    ///
    /// All refreshing is driven by <c>InventoryContainer.OnSlotChanged</c>. That is only safe
    /// because nothing in the codebase mutates a container through the live reference
    /// <c>GetSlot</c> hands out (audited at the start of Etapa 3 — see
    /// review/04_CONTAINER_REFACTOR_PLAN.md §5.2). Anything added later that writes to a
    /// container MUST go through <c>SetSlot</c> or this view will silently go stale.
    /// </summary>
    public class ContainerView : MonoBehaviour
    {
        [Header("Slot UI")]
        [Tooltip("Parent the slot prefabs are instantiated under. Usually a grid layout.")]
        [SerializeField] private Transform slotParent;

        [Tooltip("Prefab carrying an InventorySlot component.")]
        [SerializeField] private GameObject slotPrefab;

        [Tooltip("Instantiated slots are named <prefix>_<index>, which is what scene debugging reads.")]
        [SerializeField] private string slotNamePrefix = "Slot";

        private InventoryContainer container;
        private IContainerPolicy policy;
        private Action<InventorySlot, int> configureSlot;

        private readonly List<InventorySlot> slotUIs = new List<InventorySlot>();
        private bool isBuilt;

        /// <summary>
        /// Fired after a slot's UI was refreshed, so the owner can react — the SellBox recomputes
        /// its total and box sprite here, the trough updates its fill sprite and status line.
        /// </summary>
        public event Action<int, ItemStack> OnSlotRefreshed;

        /// <summary>The bound container, or null before <see cref="Bind"/> is called.</summary>
        public IInventoryContainer Container => container;

        /// <summary>The rules applied to this container. Never null once bound.</summary>
        public IContainerPolicy Policy => policy ?? DefaultContainerPolicy.Instance;

        /// <summary>How many slot UIs were actually created.</summary>
        public int SlotCount => slotUIs.Count;

        /// <summary>True once the slot UIs exist.</summary>
        public bool IsBuilt => isBuilt;

        // =====================================================================
        // BINDING
        // =====================================================================

        /// <summary>
        /// Set the slot UI references from code instead of the Inspector.
        ///
        /// This exists so an existing container can adopt the view without any scene changes:
        /// it already holds its own slotParent/slotPrefab references, adds this component at
        /// runtime and hands them over. New containers can just wire the Inspector fields and
        /// skip this. Call before <see cref="Bind"/>.
        /// </summary>
        public void Configure(Transform slotParent, GameObject slotPrefab, string slotNamePrefix = null)
        {
            this.slotParent = slotParent;
            this.slotPrefab = slotPrefab;

            if (!string.IsNullOrEmpty(slotNamePrefix))
                this.slotNamePrefix = slotNamePrefix;
        }

        /// <summary>
        /// Attach this view to a container and build its slots. Safe to call again with a
        /// different container — the old subscription is dropped and the slots are rebuilt.
        /// </summary>
        /// <param name="configureSlot">
        /// Per-slot setup the owner needs and this view should not know about (the SellBox
        /// enabling its value/sellable overlay, the trough enabling drag-to-inventory mode).
        /// Called once per slot right after its index and stack are set.
        /// </param>
        public void Bind(
            InventoryContainer container,
            IContainerPolicy policy = null,
            Action<InventorySlot, int> configureSlot = null)
        {
            if (container == null)
            {
                Debug.LogWarning($"[ContainerView] {name}: Bind called with a null container.", this);
                return;
            }

            Unsubscribe();

            this.container = container;
            this.policy = policy;
            this.configureSlot = configureSlot;

            container.OnSlotChanged += HandleSlotChanged;
            container.OnSizeChanged += HandleSizeChanged;

            Rebuild();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (container == null) return;

            container.OnSlotChanged -= HandleSlotChanged;
            container.OnSizeChanged -= HandleSizeChanged;
        }

        // =====================================================================
        // BUILD / REFRESH
        // =====================================================================

        /// <summary>Destroy the current slot UIs and create them again from the container.</summary>
        public void Rebuild()
        {
            if (container == null) return;

            if (slotParent == null || slotPrefab == null)
            {
                Debug.LogWarning(
                    $"[ContainerView] {name}: slotParent or slotPrefab is not assigned — no slots will be created.", this);
                return;
            }

            ClearSlots();

            for (int i = 0; i < container.MaxSlots; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotParent);
                slotObj.name = $"{slotNamePrefix}_{i}";

                InventorySlot slotUI = slotObj.GetComponent<InventorySlot>();
                if (slotUI == null)
                {
                    Debug.LogWarning(
                        $"[ContainerView] {name}: slotPrefab has no InventorySlot component.", this);
                    Destroy(slotObj);
                    continue;
                }

                slotUIs.Add(slotUI);
                slotUI.SetSlotIndex(i);
                slotUI.SetItemStack(container.GetSlot(i));
                configureSlot?.Invoke(slotUI, i);
            }

            isBuilt = true;
        }

        /// <summary>Push every slot's contents back into its UI.</summary>
        public void Refresh()
        {
            if (container == null) return;

            for (int i = 0; i < slotUIs.Count; i++)
                RefreshSlot(i);
        }

        /// <summary>Push one slot's contents back into its UI.</summary>
        public void RefreshSlot(int index)
        {
            if (container == null) return;
            if (index < 0 || index >= slotUIs.Count) return;
            if (slotUIs[index] == null) return;

            ItemStack stack = container.GetSlot(index);
            slotUIs[index].SetItemStack(stack);
            OnSlotRefreshed?.Invoke(index, stack);
        }

        /// <summary>The slot UI at this index, or null if out of range.</summary>
        public InventorySlot GetSlotUI(int index)
        {
            return index >= 0 && index < slotUIs.Count ? slotUIs[index] : null;
        }

        /// <summary>Index of a slot UI belonging to this view, or -1 if it is not ours.</summary>
        public int IndexOf(InventorySlot slotUI)
        {
            return slotUI == null ? -1 : slotUIs.IndexOf(slotUI);
        }

        /// <summary>Run an action against every live slot UI.</summary>
        public void ForEachSlot(Action<InventorySlot, int> action)
        {
            if (action == null) return;

            for (int i = 0; i < slotUIs.Count; i++)
                if (slotUIs[i] != null)
                    action(slotUIs[i], i);
        }

        // =====================================================================
        // CONTAINER EVENTS
        // =====================================================================

        private void HandleSlotChanged(int index, ItemStack stack)
        {
            if (index < 0 || index >= slotUIs.Count || slotUIs[index] == null)
            {
                // The container changed a slot this view has no UI for — normal while the
                // container is loading before Bind, or if it grew without a rebuild.
                return;
            }

            slotUIs[index].SetItemStack(stack);
            OnSlotRefreshed?.Invoke(index, stack);
        }

        private void HandleSizeChanged(int newSize)
        {
            // The slot count no longer matches the UI, so the prefabs have to be recreated.
            if (isBuilt) Rebuild();
        }

        private void ClearSlots()
        {
            foreach (InventorySlot slotUI in slotUIs)
                if (slotUI != null)
                    Destroy(slotUI.gameObject);

            slotUIs.Clear();
            isBuilt = false;
        }
    }
}
