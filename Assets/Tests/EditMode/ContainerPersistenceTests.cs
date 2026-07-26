using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SowurShield.Core;
using SowurShield.Inventory;

/// <summary>
/// Edit Mode tests for the shared container save format and the v1 → v2 migration —
/// Etapa 5 of review/04_CONTAINER_REFACTOR_PLAN.md.
///
/// SellBox and FeedingTrough used to hand-write a per-slot loop into worldStrings/
/// worldCounters. Both now go through ContainerPersistence, which is what a chest or crafting
/// bench added later will use too.
///
/// This is also the first exercise of the version-dispatch scaffolding built by TASK-004,
/// which had never actually run a migration.
/// </summary>
public class ContainerPersistenceTests
{
    private Item carrot;
    private Item tomato;
    private GameData gameData;

    private GameObject saveManagerHost;
    private SaveManager saveManager;

    [SetUp]
    public void SetUp()
    {
        carrot = MakeItem("Carrot");
        tomato = MakeItem("Tomato");
        gameData = new GameData();

        saveManagerHost = new GameObject("SaveManager_Test");
        saveManager = saveManagerHost.AddComponent<SaveManager>();

        InstallTestItemDatabase(carrot, tomato);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(saveManagerHost);
        Object.DestroyImmediate(carrot);
        Object.DestroyImmediate(tomato);
        ResetItemDatabase();
    }

    private static Item MakeItem(string name)
    {
        var item = ScriptableObject.CreateInstance<Item>();
        item.itemName = name;
        item.isStackable = true;
        item.maxStackSize = 10;
        item.baseValue = 5;
        return item;
    }

    // =========================================================================
    // ROUND TRIP
    // =========================================================================

    [Test]
    public void SaveThenLoad_RestoresContentsAtTheirOriginalSlots()
    {
        var source = new InventoryContainer(6, "SellBox_Box1");
        source.SetSlot(0, new ItemStack(carrot, 3));
        source.SetSlot(4, new ItemStack(tomato, 7));

        ContainerPersistence.Save(gameData, source);

        var restored = new InventoryContainer(6, "SellBox_Box1");
        Assert.IsTrue(ContainerPersistence.Load(gameData, restored));

        Assert.AreEqual(carrot, restored.GetSlot(0).item);
        Assert.AreEqual(3, restored.GetSlot(0).quantity);
        Assert.AreEqual(tomato, restored.GetSlot(4).item);
        Assert.AreEqual(7, restored.GetSlot(4).quantity);
        Assert.IsTrue(restored.GetSlot(1).IsEmpty);
    }

    [Test]
    public void ContainersAreKeptApartByID()
    {
        var box = new InventoryContainer(4, "SellBox_Box1");
        var trough = new InventoryContainer(4, "FeedingTrough_Trough1");
        box.SetSlot(0, new ItemStack(carrot, 2));
        trough.SetSlot(0, new ItemStack(tomato, 5));

        ContainerPersistence.Save(gameData, box);
        ContainerPersistence.Save(gameData, trough);

        var restoredBox = new InventoryContainer(4, "SellBox_Box1");
        var restoredTrough = new InventoryContainer(4, "FeedingTrough_Trough1");
        ContainerPersistence.Load(gameData, restoredBox);
        ContainerPersistence.Load(gameData, restoredTrough);

        Assert.AreEqual(carrot, restoredBox.GetSlot(0).item);
        Assert.AreEqual(tomato, restoredTrough.GetSlot(0).item);
    }

    [Test]
    public void SavingTwice_ReplacesTheEntryInsteadOfAppending()
    {
        var box = new InventoryContainer(4, "SellBox_Box1");
        box.SetSlot(0, new ItemStack(carrot, 2));
        ContainerPersistence.Save(gameData, box);

        box.SetSlot(0, new ItemStack(carrot, 9));
        ContainerPersistence.Save(gameData, box);

        Assert.AreEqual(1, gameData.containerData.containers.Count, "no duplicate entries");

        var restored = new InventoryContainer(4, "SellBox_Box1");
        ContainerPersistence.Load(gameData, restored);
        Assert.AreEqual(9, restored.GetSlot(0).quantity, "the newer save wins");
    }

    [Test]
    public void EmptyingAContainerThenSaving_ClearsItOnLoad()
    {
        var box = new InventoryContainer(4, "SellBox_Box1");
        box.SetSlot(0, new ItemStack(carrot, 5));
        ContainerPersistence.Save(gameData, box);

        box.ClearAll();
        ContainerPersistence.Save(gameData, box);

        var restored = new InventoryContainer(4, "SellBox_Box1");
        restored.SetSlot(0, new ItemStack(tomato, 1));
        ContainerPersistence.Load(gameData, restored);

        Assert.AreEqual(0, restored.GetAllItems().Count,
            "an emptied container must not resurrect its old contents");
    }

    // =========================================================================
    // GUARDS
    // =========================================================================

    [Test]
    public void LoadingAContainerThatWasNeverSaved_ReturnsFalseAndLeavesItAlone()
    {
        // Matters because callers must not clear a container on a miss: a brand new game, or a
        // v1 save whose contents the migration dropped, both land here.
        var box = new InventoryContainer(4, "SellBox_NeverSaved");
        box.SetSlot(0, new ItemStack(carrot, 4));

        Assert.IsFalse(ContainerPersistence.Load(gameData, box));
        Assert.AreEqual(4, box.GetSlot(0).quantity);
    }

    [Test]
    public void NullArguments_AreHarmless()
    {
        var box = new InventoryContainer(4, "SellBox_Box1");

        Assert.DoesNotThrow(() => ContainerPersistence.Save(null, box));
        Assert.DoesNotThrow(() => ContainerPersistence.Save(gameData, null));
        Assert.IsFalse(ContainerPersistence.Load(null, box));
        Assert.IsFalse(ContainerPersistence.Load(gameData, null));
    }

    [Test]
    public void ContainerWithoutAnID_IsNotSaved()
    {
        var box = new InventoryContainer(4, "");
        box.SetSlot(0, new ItemStack(carrot, 1));

        ContainerPersistence.Save(gameData, box);

        Assert.AreEqual(0, gameData.containerData.containers.Count);
    }

    // =========================================================================
    // COLLECTION
    // =========================================================================

    [Test]
    public void Find_ReturnsNullForUnknownOrEmptyID()
    {
        Assert.IsNull(gameData.containerData.Find("nope"));
        Assert.IsNull(gameData.containerData.Find(null));
        Assert.IsNull(gameData.containerData.Find(""));
    }

    [Test]
    public void Store_IgnoresNullOrUnidentifiedData()
    {
        gameData.containerData.Store(null);
        gameData.containerData.Store(new InventoryContainer.ContainerSaveData { containerID = "" });

        Assert.AreEqual(0, gameData.containerData.containers.Count);
    }

    // =========================================================================
    // MIGRATION v1 -> v2
    // =========================================================================

    private GameData InvokeMigrateSave(GameData data)
    {
        MethodInfo method = typeof(SaveManager).GetMethod(
            "MigrateSave", BindingFlags.NonPublic | BindingFlags.Instance);
        return (GameData)method.Invoke(saveManager, new object[] { data });
    }

    private static GameData MakeV1SaveWithLegacyContainerKeys()
    {
        var data = new GameData { saveVersion = 1 };
        data.worldData.worldStrings["sellbox_SellBox_slot0_item"] = "Carrot";
        data.worldData.worldCounters["sellbox_SellBox_slot0_qty"] = 3;
        data.worldData.worldStrings["feedingtrough_Trough_slot2_item"] = "Tomato";
        data.worldData.worldCounters["feedingtrough_Trough_slot2_qty"] = 5;
        data.worldData.worldCounters["feedingtrough_Trough_slotCount"] = 12;
        return data;
    }

    [Test]
    public void MigrateV1ToV2_BumpsTheVersion()
    {
        GameData migrated = InvokeMigrateSave(MakeV1SaveWithLegacyContainerKeys());

        Assert.AreEqual(GameData.CURRENT_SAVE_VERSION, migrated.saveVersion);
        Assert.AreEqual(2, GameData.CURRENT_SAVE_VERSION, "Etapa 5 raised the format to v2");
    }

    [Test]
    public void MigrateV1ToV2_StripsTheLegacyContainerKeys()
    {
        GameData migrated = InvokeMigrateSave(MakeV1SaveWithLegacyContainerKeys());

        Assert.IsFalse(migrated.worldData.worldStrings.ContainsKey("sellbox_SellBox_slot0_item"));
        Assert.IsFalse(migrated.worldData.worldCounters.ContainsKey("sellbox_SellBox_slot0_qty"));
        Assert.IsFalse(migrated.worldData.worldStrings.ContainsKey("feedingtrough_Trough_slot2_item"));
        Assert.IsFalse(migrated.worldData.worldCounters.ContainsKey("feedingtrough_Trough_slot2_qty"));
        Assert.IsFalse(migrated.worldData.worldCounters.ContainsKey("feedingtrough_Trough_slotCount"));
    }

    [Test]
    public void MigrateV1ToV2_LeavesEveryOtherWorldKeyAlone()
    {
        var data = MakeV1SaveWithLegacyContainerKeys();
        data.worldData.worldFlags["animal_Clucky_isIll"] = true;
        data.worldData.worldCounters["animal_Clucky_neglectDays"] = 2;
        data.worldData.worldStrings["animal_Clucky_customName"] = "Clucky";

        GameData migrated = InvokeMigrateSave(data);

        Assert.IsTrue(migrated.worldData.worldFlags["animal_Clucky_isIll"]);
        Assert.AreEqual(2, migrated.worldData.worldCounters["animal_Clucky_neglectDays"]);
        Assert.AreEqual("Clucky", migrated.worldData.worldStrings["animal_Clucky_customName"]);
    }

    [Test]
    public void MigrateV1ToV2_LeavesContainersEmpty_WhichIsTheAgreedTradeOff()
    {
        GameData migrated = InvokeMigrateSave(MakeV1SaveWithLegacyContainerKeys());

        Assert.IsNotNull(migrated.containerData);
        Assert.AreEqual(0, migrated.containerData.containers.Count,
            "v1 contents are deliberately not converted — see the plan's Etapa 5 decision");
    }

    [Test]
    public void MigratingAV1SaveWithNoContainerKeys_IsSilentAndStillBumps()
    {
        var data = new GameData { saveVersion = 1 };

        GameData migrated = InvokeMigrateSave(data);

        Assert.AreEqual(GameData.CURRENT_SAVE_VERSION, migrated.saveVersion);
        Assert.AreEqual(0, migrated.containerData.containers.Count);
    }

    [Test]
    public void ACurrentSave_IsNotMigrated()
    {
        var data = new GameData();
        data.worldData.worldStrings["sellbox_SellBox_slot0_item"] = "Carrot";

        GameData migrated = InvokeMigrateSave(data);

        Assert.AreEqual(GameData.CURRENT_SAVE_VERSION, migrated.saveVersion);
        Assert.IsTrue(migrated.worldData.worldStrings.ContainsKey("sellbox_SellBox_slot0_item"),
            "already-current saves must not be touched, whatever their keys look like");
    }

    // =========================================================================
    // ItemDatabase test seam (same approach as InventoryContainerTests)
    // =========================================================================

    private static void InstallTestItemDatabase(params Item[] items)
    {
        var db = ScriptableObject.CreateInstance<ItemDatabase>();
        db.autoLoadFromResources = false;
        db.items = new List<Item>(items);

        SetStatic("instance", db);
        SetStatic("isInitialized", false);
        GetLookup().Clear();

        db.Initialize();
    }

    private static void ResetItemDatabase()
    {
        SetStatic("instance", null);
        SetStatic("isInitialized", false);
        GetLookup().Clear();
    }

    private static void SetStatic(string fieldName, object value)
    {
        typeof(ItemDatabase)
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
            ?.SetValue(null, value);
    }

    private static Dictionary<string, Item> GetLookup()
    {
        return typeof(ItemDatabase)
            .GetField("itemLookup", BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null) as Dictionary<string, Item> ?? new Dictionary<string, Item>();
    }
}
