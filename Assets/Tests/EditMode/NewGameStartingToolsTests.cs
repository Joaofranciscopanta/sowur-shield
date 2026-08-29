using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SowurShield.Core;
using SowurShield.Inventory;

namespace SowurShield.Tests
{

/// <summary>
/// Guards the starting kit of a new game.
///
/// A new save opened with the hotbar at 0/9. Without a hoe and a watering can the player
/// cannot till or water, so the farming loop — the whole point of the game — was
/// unreachable from a fresh start, with no tutorial to explain the dead end either. The
/// tools were in the ItemDatabase the entire time; `new GameData()` simply handed over
/// nothing.
///
/// Two traps are pinned here, because both produced a silently empty hotbar:
///
/// 1. GameData ships inventoryItems already filled with empty entries, so Add()ing the
///    tools appended them at index 38-39 — past the hotbar, past inventorySize, and
///    dropped by Inventory.LoadData's Min(Count, inventorySize) loop.
/// 2. The names are ItemDatabase keys (Item.itemName), not asset file names. A mismatch
///    resolves to null and leaves the slot empty without an error.
/// </summary>
public class NewGameStartingToolsTests
{
    private static GameData CreateNewGameData()
    {
        MethodInfo factory = typeof(SaveManager).GetMethod(
            "CreateNewGameData", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(factory, Is.Not.Null,
            "SaveManager.CreateNewGameData is the single place a fresh save is built; " +
            "if it was renamed, this guard needs to follow it");

        return (GameData)factory.Invoke(null, null);
    }

    private static InventoryGameData.ItemStackData SlotAt(GameData data, int index)
    {
        List<InventoryGameData.ItemStackData> items = data.inventoryData.inventoryItems;
        Assert.That(items.Count, Is.GreaterThan(index),
            $"a new game should have at least {index + 1} inventory entries");
        return items[index];
    }

    [SetUp]
    public void SetUp() => ItemDatabase.ForceReload();

    [Test]
    public void NewGame_PutsTheHoeInTheFirstHotbarSlot()
    {
        var slot = SlotAt(CreateNewGameData(), 0);

        Assert.That(slot.itemName, Is.EqualTo(SaveManager.StartingToolHoe));
        Assert.That(slot.quantity, Is.EqualTo(1));
    }

    [Test]
    public void NewGame_PutsTheWateringCanInTheSecondHotbarSlot()
    {
        var slot = SlotAt(CreateNewGameData(), 1);

        Assert.That(slot.itemName, Is.EqualTo(SaveManager.StartingToolWateringCan));
        Assert.That(slot.quantity, Is.EqualTo(1));
    }

    /// <summary>
    /// The bug that survived the first fix attempt: the tools were present in the list but
    /// at an index no hotbar slot reads. Anything past inventorySize is invisible.
    /// </summary>
    [Test]
    public void StartingTools_LandWithinTheHotbar()
    {
        GameData data = CreateNewGameData();
        var starting = new[] { SaveManager.StartingToolHoe, SaveManager.StartingToolWateringCan };

        for (int i = 0; i < data.inventoryData.inventoryItems.Count; i++)
        {
            string name = data.inventoryData.inventoryItems[i].itemName;
            if (System.Array.IndexOf(starting, name) < 0)
                continue;

            Assert.That(i, Is.LessThan(9),
                $"'{name}' sits at index {i}, outside the 9 hotbar slots — the player would " +
                "never see it");
        }
    }

    [TestCase("Hoe")]
    [TestCase("WateringCan")]
    public void StartingToolNames_ResolveInTheItemDatabase(string itemName)
    {
        Item item = ItemDatabase.GetItem(itemName);

        Assert.That(item, Is.Not.Null,
            $"'{itemName}' must be an ItemDatabase key; an unresolvable name leaves the " +
            "starting slot silently empty");
    }
}

}
