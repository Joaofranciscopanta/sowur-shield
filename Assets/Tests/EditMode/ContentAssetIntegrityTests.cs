using NUnit.Framework;
using UnityEngine;
using SowurShield.Animals;
using SowurShield.Inventory;

namespace SowurShield.Tests
{

/// <summary>
/// Guards the string-keyed links between assets. The project wires animals to items by
/// *name* (`ItemDatabase.GetItem(animalData.illnessCureItemName)`), so a missing asset or a
/// renamed item does not break the build, does not throw, and is invisible until someone
/// plays far enough to hit it.
///
/// That is exactly how the illness system shipped broken: `Medicine` was referenced by all
/// 28 AnimalData assets and existed nowhere, so `Animal.CureIllness` hit its
/// `Debug.LogWarning` and returned on every attempt. An ill animal loses 50% combat stats
/// and stops producing, and nothing in the game could cure it.
/// </summary>
public class ContentAssetIntegrityTests
{
    [SetUp]
    public void SetUp()
    {
        // The lookup is a static cache; a previous fixture may have left it stale.
        ItemDatabase.ForceReload();
    }

    /// <summary>
    /// The bug that motivated this file. Fails if Medicine.asset is deleted or its
    /// itemName drifts away from what the AnimalData assets ask for.
    /// </summary>
    [Test]
    public void EveryAnimal_CureItem_ResolvesInItemDatabase()
    {
        var animals = Resources.LoadAll<AnimalData>("Animals");
        Assert.That(animals, Is.Not.Empty, "No AnimalData assets loaded from Resources/Animals.");

        foreach (var animal in animals)
        {
            if (string.IsNullOrEmpty(animal.illnessCureItemName)) continue;

            Assert.That(ItemDatabase.GetItem(animal.illnessCureItemName), Is.Not.Null,
                $"'{animal.name}' is cured by '{animal.illnessCureItemName}', which does not exist " +
                "in the ItemDatabase. CureIllness() will warn and return, leaving the animal " +
                "permanently ill (-50% combat stats, no production).");
        }
    }

    /// <summary>
    /// Same failure mode on the feeding path: FeedingTrough resolves each requirement by
    /// name and skips it with a warning on a miss, so the animal silently starves.
    /// </summary>
    [Test]
    public void EveryAnimal_FoodRequirements_ResolveInItemDatabase()
    {
        var animals = Resources.LoadAll<AnimalData>("Animals");

        foreach (var animal in animals)
        {
            if (animal.dailyFoodRequirements == null) continue;

            foreach (var req in animal.dailyFoodRequirements)
            {
                Assert.That(ItemDatabase.GetItem(req.itemName), Is.Not.Null,
                    $"'{animal.name}' eats '{req.itemName}', which is not in the ItemDatabase. " +
                    "FeedingTrough will log a warning and skip feeding it.");
            }
        }
    }

    /// <summary>
    /// Production resolves produceItemName through the same name lookup.
    /// </summary>
    [Test]
    public void EveryProducingAnimal_ProduceItem_ResolvesInItemDatabase()
    {
        var animals = Resources.LoadAll<AnimalData>("Animals");

        foreach (var animal in animals)
        {
            if (!animal.canProduce || string.IsNullOrEmpty(animal.produceItemName)) continue;

            Assert.That(ItemDatabase.GetItem(animal.produceItemName), Is.Not.Null,
                $"'{animal.name}' produces '{animal.produceItemName}', which is not in the ItemDatabase.");
        }
    }

    /// <summary>
    /// ItemDatabase.Initialize() keeps the first asset it sees for a given itemName and
    /// silently drops the rest, so a duplicate makes lookups depend on load order. Two
    /// duplicate Apple assets were removed on 2026-08-01 for this reason.
    /// </summary>
    [Test]
    public void ItemNames_AreUnique_AcrossAllResources()
    {
        var items = Resources.LoadAll<Item>("");
        var seen = new System.Collections.Generic.Dictionary<string, string>();

        foreach (var item in items)
        {
            if (item == null) continue;

            string existing;
            bool isDuplicate = seen.TryGetValue(item.itemName, out existing);

            Assert.That(isDuplicate, Is.False,
                $"Duplicate itemName '{item.itemName}' on '{item.name}' and '{existing}'. " +
                "ItemDatabase keeps only the first and drops the other, making lookups order-dependent.");

            seen[item.itemName] = item.name;
        }
    }

    /// <summary>
    /// Rabbit is one of the four documented animals but had no asset at all until
    /// 2026-08-01 — AnimalCreatorTool, which creates it, had never been run.
    /// </summary>
    [Test]
    public void Rabbit_Exists_AndIsFullyWired()
    {
        var rabbit = Resources.Load<AnimalData>("Animals/Rabbit");
        Assert.That(rabbit, Is.Not.Null, "Rabbit.asset is missing from Resources/Animals.");

        Assert.That(rabbit.animalName, Is.EqualTo("Rabbit"));
        Assert.That(rabbit.preferredSeason, Is.EqualTo("Winter"));
        Assert.That(rabbit.produceItemName, Is.EqualTo("Rabbit Fur"));

        // Production spawns this prefab; a null one means harvesting drops nothing.
        Assert.That(rabbit.groundItemPrefab, Is.Not.Null,
            "Rabbit.groundItemPrefab is null — Animal.SpawnProduce() cannot drop Rabbit Fur.");
    }

    /// <summary>
    /// A produce item whose GroundItem prefab points at the wrong Item would drop the
    /// wrong thing — a mismatch no other test would notice.
    /// </summary>
    [Test]
    public void RabbitFurGroundItem_CarriesTheRabbitFurItem()
    {
        var prefab = Resources.Load<GameObject>("Prefabs/GroundItems/RabbitFur_GroundItem");
        Assert.That(prefab, Is.Not.Null, "RabbitFur_GroundItem prefab is missing.");

        var groundItem = prefab.GetComponent<SowurShield.Core.GroundItem>();
        Assert.That(groundItem, Is.Not.Null, "Prefab has no GroundItem component.");

        var expected = ItemDatabase.GetItem("Rabbit Fur");
        Assert.That(expected, Is.Not.Null, "'Rabbit Fur' is not in the ItemDatabase.");
        Assert.That(groundItem.item, Is.EqualTo(expected),
            "RabbitFur_GroundItem does not carry the 'Rabbit Fur' item.");
    }
}

} // namespace SowurShield.Tests
