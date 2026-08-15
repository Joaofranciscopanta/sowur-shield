using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SowurShield.Inventory;

/// <summary>
/// Drag-and-drop inside the player's inventory, exercised through the real path:
/// SlotDragHandler.BeginDrag (which empties the source slot in the container) followed by
/// SlotTransferRouter.Route (which is supposed to put it back and then move it).
///
/// The bug these cover destroyed items outright. SlotTransferRouter.Resolve decided whether
/// the drag had already emptied the container by checking whether the slot had an OwnerView.
/// That was a correct proxy until Inventory adopted ContainerView — after which player slots
/// had BOTH an InventoryManager and an OwnerView, took the view branch, and were reported as
/// non-destructive. The router then skipped the restore for a drag that HAD emptied the slot,
/// so the item was gone from the container before the transfer ran and the drop silently ate
/// it: not in the source, not in the destination, not on the floor.
///
/// Play Mode because InventorySlot is a MonoBehaviour with a full Awake chain.
/// </summary>
public class SlotTransferRouterPlayModeTests
{
    private const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private Inventory inventory;

    /// <summary>
    /// Loads SampleScene and uses the real player inventory rather than building one.
    /// Inventory needs a wired slot UI (hotbarParent/storageParent/slotPrefab) to create any
    /// slots at all, and the scene already has that; a synthetic one would test a different
    /// object than the game runs.
    ///
    /// The load is not optional. The PlayMode runner starts on an empty generated scene, so
    /// without it FindFirstObjectByType returns null and both tests Assert.Ignore — which
    /// looks green in the summary while protecting nothing. That is exactly how a regression
    /// this severe could slip through, so it is worth the extra second per run.
    /// </summary>
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        if (Object.FindFirstObjectByType<Inventory>() == null)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
        }

        inventory = Object.FindFirstObjectByType<Inventory>();
        Assert.IsNotNull(inventory,
            "SampleScene did not provide an Inventory. It must be in Build Settings for these " +
            "tests to load it.");

        yield return null;
    }

    private InventorySlot SlotAt(int containerIndex)
    {
        foreach (var slot in Object.FindObjectsByType<InventorySlot>(FindObjectsInactive.Include,
                                                                     FindObjectsSortMode.None))
        {
            if (inventory.IndexOfSlot(slot) == containerIndex) return slot;
        }
        return null;
    }

    private ItemStack StackAt(int index) => inventory.Container.GetSlot(index);

    /// <summary>Runs the two calls OnBeginDrag and OnDrop make, in that order.</summary>
    private static void DragFromTo(InventorySlot from, InventorySlot to)
    {
        var handler = from.GetType().GetField("dragHandler", Priv).GetValue(from);
        var stack = from.GetType().GetField("itemStack", Priv).GetValue(from);

        handler.GetType().GetMethod("BeginDrag").Invoke(handler, new object[] { stack, false, from });
        SlotTransferRouter.Route(from, to);
    }

    [UnityTest]
    public IEnumerator DraggingToAnEmptySlot_MovesTheItemInsteadOfDestroyingIt()
    {
        // Seed a known state so the test does not depend on whatever the save holds.
        var item = Resources.LoadAll<Item>("Items").Length > 0
            ? Resources.LoadAll<Item>("Items")[0]
            : null;
        if (item == null) Assert.Ignore("No Item assets under Resources/Items.");

        inventory.Container.SetSlot(0, new ItemStack(item, 3));
        inventory.Container.SetSlot(1, new ItemStack());
        yield return null;

        InventorySlot from = SlotAt(0), to = SlotAt(1);
        Assert.IsNotNull(from, "No slot resolved for container index 0.");
        Assert.IsNotNull(to, "No slot resolved for container index 1.");

        DragFromTo(from, to);
        yield return null;

        Assert.IsTrue(StackAt(0).IsEmpty, "The source slot should have handed the item over.");
        Assert.AreSame(item, StackAt(1).item,
            "The item vanished: it left the source slot and never reached the destination.");
        Assert.AreEqual(3, StackAt(1).quantity, "The whole stack should have moved.");
    }

    /// <summary>
    /// Releasing the mouse between two slots, or on the seam where they meet, means no slot
    /// receives an OnDrop — only OnEndDrag runs. That path suppressed SlotDragHandler's
    /// recovery for any slot with an OwnerView, which after Inventory adopted ContainerView
    /// meant every player slot: the stack was gone from the container and no ground item was
    /// ever spawned. Reported from play as "solto entre os slots e o item some".
    /// </summary>
    [UnityTest]
    public IEnumerator ReleasingBetweenSlots_DropsTheItemRatherThanDestroyingIt()
    {
        var items = Resources.LoadAll<Item>("Items");
        if (items.Length == 0) Assert.Ignore("No Item assets under Resources/Items.");

        inventory.Container.SetSlot(0, new ItemStack(items[0], 3));
        yield return null;

        InventorySlot from = SlotAt(0);
        Assert.IsNotNull(from);

        int groundBefore = Object.FindObjectsByType<SowurShield.Core.GroundItem>(
            FindObjectsSortMode.None).Length;

        // Begin the drag, then end it without any slot calling OnDrop.
        var pointer = new UnityEngine.EventSystems.PointerEventData(
            UnityEngine.EventSystems.EventSystem.current)
        {
            button = UnityEngine.EventSystems.PointerEventData.InputButton.Left,
            position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
        };
        from.OnBeginDrag(pointer);
        from.OnEndDrag(pointer);
        yield return null;

        int groundAfter = Object.FindObjectsByType<SowurShield.Core.GroundItem>(
            FindObjectsSortMode.None).Length;

        bool backInSlot = !StackAt(0).IsEmpty;
        bool onTheFloor = groundAfter > groundBefore;

        Assert.IsTrue(backInSlot || onTheFloor,
            "The stack was destroyed: it is neither back in its slot nor on the ground.");
    }

    [UnityTest]
    public IEnumerator DraggingOntoAnotherItem_KeepsBothStacks()
    {
        var items = Resources.LoadAll<Item>("Items");
        if (items.Length < 2) Assert.Ignore("Need two distinct Item assets to test a swap.");

        inventory.Container.SetSlot(0, new ItemStack(items[0], 2));
        inventory.Container.SetSlot(1, new ItemStack(items[1], 5));
        yield return null;

        DragFromTo(SlotAt(0), SlotAt(1));
        yield return null;

        // Whether the containers swap or refuse is the transfer service's call; what must never
        // happen is either stack disappearing.
        bool firstSurvives = StackAt(0).item == items[0] || StackAt(1).item == items[0];
        bool secondSurvives = StackAt(0).item == items[1] || StackAt(1).item == items[1];

        Assert.IsTrue(firstSurvives, "The dragged stack was destroyed by the drop.");
        Assert.IsTrue(secondSurvives, "The stack already in the destination was destroyed.");
    }
}
