using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SowurShield.Inventory;

namespace SowurShield.Tests
{

/// <summary>
/// Guards the recovery path in <see cref="ItemDatabase"/>.
///
/// The shops shipped with no stock. `clara_Shop` asks for Medicine, CarrotSeed, CabbageSeed
/// and RadishSeed; all four exist as assets carrying exactly those `itemName` values, and all
/// four were logged as "not found in ItemDatabase" every time the shop opened.
///
/// The cause was an ordering hole in the singleton, not the data. `Instance` only called
/// `Initialize()` from inside its `if (instance == null)` branch, so the very first caller
/// decided the outcome for the whole session: if it ran before the Resources items were
/// loadable, it cached an empty dictionary, and every later access found a non-null instance
/// and skipped initialization entirely. `GetItem` then returned null for every name until
/// play mode ended — which also explains the shop's gold reading 0 against the HUD's 567 and
/// its title staying untranslated. All three were this one bug.
///
/// These tests reproduce that state directly rather than trusting the happy path: they force
/// the statics into "initialized but empty" and assert the next access repairs it.
/// </summary>
public class ItemDatabaseRecoveryTests
{
    private static FieldInfo LookupField =>
        typeof(ItemDatabase).GetField("itemLookup", BindingFlags.NonPublic | BindingFlags.Static);

    private static FieldInfo InitializedField =>
        typeof(ItemDatabase).GetField("isInitialized", BindingFlags.NonPublic | BindingFlags.Static);

    private static Dictionary<string, Item> Lookup =>
        (Dictionary<string, Item>)LookupField.GetValue(null);

    [SetUp]
    public void SetUp() => ItemDatabase.ForceReload();

    [TearDown]
    public void TearDown() => ItemDatabase.ForceReload();

    /// <summary>
    /// The exact poisoned state the bug left behind: a live instance, isInitialized latched
    /// true, and nothing in the lookup. Before the fix this survived for the whole session
    /// because nothing ever called Initialize() again.
    /// </summary>
    [Test]
    public void GetItem_RepairsAnEmptyLookupThatWasMarkedInitialized()
    {
        Lookup.Clear();
        InitializedField.SetValue(null, true);
        Assert.That(Lookup, Is.Empty, "precondition: the lookup starts poisoned");

        Item medicine = ItemDatabase.GetItem("Medicine");

        Assert.That(medicine, Is.Not.Null,
            "GetItem must reload rather than trust an empty lookup that claims to be initialized");
        Assert.That(Lookup, Is.Not.Empty, "the repaired lookup should hold the loaded items");
    }

    /// <summary>
    /// An empty load means "too early", not "there are no items". Latching isInitialized on
    /// that result is what froze the emptiness in place.
    /// </summary>
    [Test]
    public void Initialize_DoesNotLatchInitializedWhenNothingLoaded()
    {
        var database = ScriptableObject.CreateInstance<ItemDatabase>();
        database.autoLoadFromResources = false;
        database.items = new List<Item>();

        Lookup.Clear();
        InitializedField.SetValue(null, false);

        database.Initialize();

        Assert.That((bool)InitializedField.GetValue(null), Is.False,
            "an empty load must stay retryable so a later, better-timed call can populate it");

        Object.DestroyImmediate(database);
    }

    /// <summary>
    /// The four names clara_Shop asks for. Named individually so a failure says which item
    /// broke instead of just "the shop is empty".
    /// </summary>
    [TestCase("Medicine")]
    [TestCase("CarrotSeed")]
    [TestCase("CabbageSeed")]
    [TestCase("RadishSeed")]
    public void ShopCatalogueItems_ResolveByName(string itemName)
    {
        Item item = ItemDatabase.GetItem(itemName);

        Assert.That(item, Is.Not.Null,
            $"'{itemName}' is referenced by a ShopData asset; an unresolvable name silently " +
            "drops the row and the shop renders empty");
        Assert.That(item.itemName, Is.EqualTo(itemName),
            "the lookup key must be the item's own itemName, not its file name");
    }
}

}
