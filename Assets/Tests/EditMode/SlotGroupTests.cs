using NUnit.Framework;
using SowurShield.Inventory;

/// <summary>
/// Edit Mode tests for ContainerView.SlotGroup — the index arithmetic that decides which
/// container slots each parent transform renders.
///
/// SlotGroup is a plain serializable class, so it tests without a scene even though
/// ContainerView itself is a MonoBehaviour that needs real slot prefabs to build anything.
/// </summary>
public class SlotGroupTests
{
    [Test]
    public void ExplicitCount_CoversExactlyThatManySlots()
    {
        var group = new SlotGroup(null, startIndex: 0, count: 9);

        Assert.AreEqual(9, group.EndIndexExclusive(45));
    }

    [Test]
    public void ZeroCount_MeansEverythingToTheEnd()
    {
        // How the SellBox and the trough are configured: one group, whole container.
        var group = new SlotGroup(null, startIndex: 0, count: 0);

        Assert.AreEqual(12, group.EndIndexExclusive(12));
    }

    [Test]
    public void ZeroCount_FromAnOffset_CoversTheRemainder()
    {
        // The inventory's storage grid: slot 9 to the end, whatever the size is.
        var group = new SlotGroup(null, startIndex: 9, count: 0);

        Assert.AreEqual(45, group.EndIndexExclusive(45));
    }

    [Test]
    public void CountRunningPastTheContainer_IsClamped()
    {
        var group = new SlotGroup(null, startIndex: 0, count: 999);

        Assert.AreEqual(12, group.EndIndexExclusive(12));
    }

    [Test]
    public void StartBeyondTheContainer_CoversNothing()
    {
        var group = new SlotGroup(null, startIndex: 50, count: 5);

        Assert.LessOrEqual(group.EndIndexExclusive(12), 50,
            "an end at or below the start means the build loop does nothing");
    }

    [Test]
    public void TwoGroups_TileTheContainerWithoutGapsOrOverlap()
    {
        // The player inventory: hotbar 0-8, storage 9-44.
        const int total = 45;
        var hotbar = new SlotGroup(null, 0, 9);
        var storage = new SlotGroup(null, 9, 0, startActive: false);

        Assert.AreEqual(9, hotbar.EndIndexExclusive(total));
        Assert.AreEqual(9, storage.startIndex, "storage picks up exactly where the hotbar ends");
        Assert.AreEqual(total, storage.EndIndexExclusive(total));
    }

    [Test]
    public void DefaultsAreTheSingleGroupCase()
    {
        var group = new SlotGroup();

        Assert.AreEqual(0, group.startIndex);
        Assert.AreEqual(0, group.count);
        Assert.IsTrue(group.startActive);
        Assert.AreEqual(20, group.EndIndexExclusive(20));
    }

    [Test]
    public void StartActiveIsOptOut()
    {
        Assert.IsTrue(new SlotGroup(null, 0, 4).startActive);
        Assert.IsFalse(new SlotGroup(null, 0, 4, startActive: false).startActive);
    }
}
