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
    /// <summary>
    /// A contiguous run of slots rendered under one parent.
    ///
    /// Most containers are a single group covering everything. Groups exist for the ones that
    /// are not: the player inventory splits 0-8 into a hotbar and the rest into a storage grid
    /// that starts hidden, and a crafting bench will want its input and output slots under
    /// different parents while remaining ONE container.
    /// </summary>
    [System.Serializable]
    public class SlotGroup
    {
        [Tooltip("Where these slots are instantiated. Usually a grid layout.")]
        public Transform parent;

        [Tooltip("First container slot index this group renders.")]
        public int startIndex;

        [Tooltip("How many slots. 0 means 'everything from startIndex to the end'.")]
        public int count;

        [Tooltip("Whether these slots start visible. The inventory's storage grid does not.")]
        public bool startActive = true;

        [Tooltip("Slots are named <prefix>_<index>, which is what scene debugging reads.")]
        public string namePrefix = "Slot";

        public SlotGroup() { }

        public SlotGroup(Transform parent, int startIndex, int count,
            bool startActive = true, string namePrefix = "Slot")
        {
            this.parent = parent;
            this.startIndex = startIndex;
            this.count = count;
            this.startActive = startActive;
            this.namePrefix = namePrefix;
        }

        /// <summary>Last index (exclusive) this group covers, given the container's size.</summary>
        public int EndIndexExclusive(int containerSlots)
        {
            int end = count > 0 ? startIndex + count : containerSlots;
            return Mathf.Min(end, containerSlots);
        }
    }

    public class ContainerView : MonoBehaviour
    {
        [Header("Slot UI")]
        [Tooltip("Prefab carrying an InventorySlot component.")]
        [SerializeField] private GameObject slotPrefab;

        [Tooltip("Where the slots go. Leave as one entry unless the container is split across parents.")]
        [SerializeField] private List<SlotGroup> slotGroups = new List<SlotGroup>();

        private InventoryContainer container;
        private IContainerPolicy policy;
        private Action<InventorySlot, int> configureSlot;

        private readonly List<InventorySlot> slotUIs = new List<InventorySlot>();

        /// <summary>Visibility per group index, so Rebuild can restore it. See SetGroupActive.</summary>
        private readonly Dictionary<int, bool> groupActive = new Dictionary<int, bool>();

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
        /// Single-parent setup: every slot under one transform. Covers the SellBox, the feeding
        /// trough and any plain chest.
        ///
        /// Set from code rather than the Inspector so an existing container can adopt the view
        /// with no scene changes: it already holds its own slotParent/slotPrefab references,
        /// adds this component at runtime and hands them over. New containers can wire the
        /// Inspector fields and skip this. Call before <see cref="Bind"/>.
        /// </summary>
        public void Configure(Transform slotParent, GameObject slotPrefab, string slotNamePrefix = null)
        {
            Configure(slotPrefab, new SlotGroup(
                slotParent, 0, 0, true,
                string.IsNullOrEmpty(slotNamePrefix) ? "Slot" : slotNamePrefix));
        }

        /// <summary>
        /// Multi-parent setup: the container's slots are split across several transforms, each
        /// with its own index range and initial visibility. Groups are rendered in the order
        /// given. Call before <see cref="Bind"/>.
        /// </summary>
        public void Configure(GameObject slotPrefab, params SlotGroup[] groups)
        {
            this.slotPrefab = slotPrefab;

            slotGroups.Clear();
            if (groups != null)
                foreach (SlotGroup group in groups)
                    if (group != null)
                        slotGroups.Add(group);
        }

        /// <summary>
        /// Show or hide every slot belonging to one group — how the inventory reveals its
        /// storage grid without the container knowing anything about visibility.
        /// </summary>
        public void SetGroupActive(int groupIndex, bool active)
        {
            if (container == null) return;
            if (groupIndex < 0 || groupIndex >= slotGroups.Count) return;

            // Remembered so a Rebuild can restore it. A resize rebuilds every slot, and fresh
            // slots come back at the group's startActive — so without this, growing the
            // inventory while it was open blanked the storage grid until the next toggle.
            groupActive[groupIndex] = active;

            SlotGroup group = slotGroups[groupIndex];
            int end = group.EndIndexExclusive(container.MaxSlots);

            for (int i = group.startIndex; i < end; i++)
            {
                InventorySlot slotUI = GetSlotUI(i);
                if (slotUI != null)
                    slotUI.gameObject.SetActive(active);
            }
        }

        /// <summary>Current visibility per group, seeded from each group's startActive.</summary>
        private bool IsGroupActive(int groupIndex)
        {
            if (groupActive.TryGetValue(groupIndex, out bool active)) return active;
            return slotGroups[groupIndex].startActive;
        }

        /// <summary>How many slot groups this view renders.</summary>
        public int GroupCount => slotGroups.Count;

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

            if (slotPrefab == null || slotGroups.Count == 0)
            {
                Debug.LogWarning(
                    $"[ContainerView] {name}: slotPrefab or slot groups not configured — no slots will be created.", this);
                return;
            }

            ClearSlots();

            // Indexed by CONTAINER slot index, not creation order, so a container split across
            // several parents still answers GetSlotUI(i) correctly. Indices no group covers
            // stay null.
            for (int i = 0; i < container.MaxSlots; i++)
                slotUIs.Add(null);

            for (int g = 0; g < slotGroups.Count; g++)
            {
                SlotGroup group = slotGroups[g];

                if (group.parent == null)
                {
                    Debug.LogWarning($"[ContainerView] {name}: a slot group has no parent — skipped.", this);
                    continue;
                }

                // Whatever the group is showing right now, not what it started as — a rebuild
                // triggered by a resize must not close a grid the player has open.
                bool active = IsGroupActive(g);
                int end = group.EndIndexExclusive(container.MaxSlots);

                for (int i = Mathf.Max(0, group.startIndex); i < end; i++)
                {
                    GameObject slotObj = Instantiate(slotPrefab, group.parent);
                    slotObj.name = $"{group.namePrefix}_{i}";

                    InventorySlot slotUI = slotObj.GetComponent<InventorySlot>();
                    if (slotUI == null)
                    {
                        Debug.LogWarning(
                            $"[ContainerView] {name}: slotPrefab has no InventorySlot component.", this);
                        Destroy(slotObj);
                        continue;
                    }

                    slotUIs[i] = slotUI;
                    slotUI.OwnerView = this;   // lets SlotTransferRouter resolve the slot's container
                    slotUI.SetSlotIndex(i);
                    slotUI.SetItemStack(container.GetSlot(i));
                    configureSlot?.Invoke(slotUI, i);

                    if (!active)
                        slotObj.SetActive(false);
                }
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
                    DestroySlotObject(slotUI.gameObject);

            slotUIs.Clear();
            isBuilt = false;
        }

        /// <summary>
        /// Remove a slot from its parent and destroy it.
        ///
        /// Destroy is deferred to the end of the frame, but Rebuild clears and re-instantiates
        /// within a single frame — so without the explicit unparenting the outgoing slots were
        /// still children while the new ones were created, and the grid layout kept laying them
        /// out. Growing a 45-slot inventory once left 81 children under a parent that should
        /// hold 45: 36 orphans, duplicated by name and unknown to the view. They are inactive
        /// while the panel is closed, which is why this stayed invisible, but a resize with the
        /// inventory open renders them on top of the live slots.
        ///
        /// Only Inventory reaches this: SellBox and the trough are fixed-size and build once.
        /// </summary>
        private static void DestroySlotObject(GameObject slotObject)
        {
            slotObject.transform.SetParent(null, false);

            // Edit Mode (tests, editor tooling) rejects Destroy.
            if (Application.isPlaying) Destroy(slotObject);
            else DestroyImmediate(slotObject);
        }
    }
}
