using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SowurShield.Core;

namespace SowurShield.Tests
{

/// <summary>
/// Covers save slot management: naming, deleting and the backup-filename collision.
///
/// Written after the player reported that slots could only be overwritten, never renamed
/// or deleted. Two of the three defects were wiring rather than logic, so these tests pin
/// the data layer that the wiring depends on.
///
/// Every test drives a real SaveManager against a real directory under a temp root, because
/// the bugs here are all about what actually lands on disk. saveDirectoryPath is normally
/// assigned in Awake(), which does not run for a bare AddComponent, so each test seeds it.
/// </summary>
public class SaveSlotManagementTests
{
    private const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

    private GameObject _go;
    private SaveManager _sm;
    private string _root;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "SowurSlotTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_root);

        _go = new GameObject("TestSaveManager");
        _sm = _go.AddComponent<SaveManager>();
        typeof(SaveManager).GetField("saveDirectoryPath", Priv).SetValue(_sm, _root);
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    // ---------------------------------------------------------------- helpers

    private string SeedSlot(string slot, string customName = null, int day = 1, int money = 0)
    {
        string dir = Path.Combine(_root, slot);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "GameSave.json"), "{}");

        var info = new SaveSlotInfo
        {
            slotName = slot, isEmpty = false, currentDay = day, money = money,
            season = "Spring", year = 1, customName = customName,
            saveTimestamp = "2026-08-23 10:00:00"
        };
        string metaPath = Path.Combine(dir, "SlotMeta.json");
        File.WriteAllText(metaPath, JsonUtility.ToJson(info, true));
        return metaPath;
    }

    private SaveSlotInfo ReadMeta(string slot)
    {
        string p = Path.Combine(_root, slot, "SlotMeta.json");
        return File.Exists(p) ? JsonUtility.FromJson<SaveSlotInfo>(File.ReadAllText(p)) : null;
    }

    // ---------------------------------------------------------------- renaming

    [Test]
    public void RenameSlot_StoresTrimmedLabel()
    {
        SeedSlot("Slot1");
        Assert.IsTrue(_sm.RenameSlot("Slot1", "  Fazenda do Lucas  "));
        Assert.AreEqual("Fazenda do Lucas", ReadMeta("Slot1").customName);
    }

    [Test]
    public void RenameSlot_PreservesExistingSaveData()
    {
        SeedSlot("Slot1", day: 7, money: 1234);
        _sm.RenameSlot("Slot1", "Farm");

        var meta = ReadMeta("Slot1");
        Assert.AreEqual(7, meta.currentDay, "renaming must not disturb the slot day");
        Assert.AreEqual(1234, meta.money, "renaming must not disturb the slot money");
    }

    [Test]
    public void RenameSlot_CapsLengthAndStripsControlCharacters()
    {
        SeedSlot("Slot1");

        _sm.RenameSlot("Slot1", new string('A', 60));
        Assert.AreEqual(SaveManager.MaxSlotNameLength, ReadMeta("Slot1").customName.Length);

        _sm.RenameSlot("Slot1", "Fa\tzen\nda");
        Assert.AreEqual("Fazenda", ReadMeta("Slot1").customName,
            "tabs and newlines would corrupt the JSON round-trip and the UI label");
    }

    [Test]
    public void RenameSlot_BlankInputClearsBackToDefaultName()
    {
        SeedSlot("Slot1", customName: "Old Name");
        Assert.IsTrue(_sm.RenameSlot("Slot1", "   "));

        var meta = ReadMeta("Slot1");
        Assert.IsEmpty(meta.customName);
        Assert.AreEqual("Slot 1", meta.GetDisplayName("Auto Save", "Slot "));
    }

    [Test]
    public void RenameSlot_RefusesAutoSaveAndEmptySlots()
    {
        SeedSlot("AutoSave");
        Assert.IsFalse(_sm.RenameSlot("AutoSave", "mine"),
            "AutoSave is machine-managed; a custom label would be overwritten without warning");
        Assert.IsFalse(_sm.RenameSlot("Slot2", "ghost"),
            "an empty slot has no save to label");
    }

    /// <summary>
    /// The regression this design was most likely to introduce: WriteSlotMeta rebuilds the
    /// meta object out of GameData, which carries no label, so saving over a named slot used
    /// to wipe the name. Revert the carry-over in WriteSlotMeta and this fails.
    /// </summary>
    [Test]
    public void SavingOverANamedSlot_KeepsTheLabel()
    {
        SeedSlot("Slot1", customName: "Fazenda do Lucas", day: 3, money: 500);

        var data = new GameData
        {
            timeData = new TimeGameData { currentDay = 9, season = "Summer", year = 2 },
            playerData = new PlayerGameData { money = 7777 },
            totalPlayTime = 9999f,
            saveTimestamp = "2026-08-23 18:00:00"
        };
        typeof(SaveManager).GetMethod("WriteSlotMeta", Priv).Invoke(_sm, new object[] { "Slot1", data });

        var meta = ReadMeta("Slot1");
        Assert.AreEqual("Fazenda do Lucas", meta.customName, "the player label must survive a save");
        Assert.AreEqual(9, meta.currentDay, "the new game state must still be written");
        Assert.AreEqual(7777, meta.money);
    }

    // ---------------------------------------------------------------- display name

    [Test]
    public void GetDisplayName_PrefersCustomNameThenFallsBack()
    {
        var named = new SaveSlotInfo { slotName = "Slot2", customName = "My Farm" };
        Assert.AreEqual("My Farm", named.GetDisplayName("Auto Save", "Slot "));

        var plain = new SaveSlotInfo { slotName = "Slot2" };
        Assert.AreEqual("Slot 2", plain.GetDisplayName("Auto Save", "Slot "));

        var auto = new SaveSlotInfo { slotName = "AutoSave", isAutoSave = true };
        Assert.AreEqual("Auto Save", auto.GetDisplayName("Auto Save", "Slot "));
    }

    // ---------------------------------------------------------------- deleting

    [Test]
    public void DeleteSlot_RemovesEveryFileIncludingBackupsAndMeta()
    {
        SeedSlot("Slot1", customName: "Doomed");
        string dir = Path.Combine(_root, "Slot1");
        File.WriteAllText(Path.Combine(dir, "GameSave_backup_20260101_000000.json"), "{}");

        Assert.IsTrue(_sm.DeleteSlot("Slot1"));
        Assert.AreEqual(0, Directory.GetFiles(dir).Length);
        Assert.IsTrue(_sm.GetSlotInfo("Slot1").isEmpty,
            "a deleted slot must read back as empty, or the picker still offers it for Load");
    }

    [Test]
    public void DeleteSlot_RefusesAutoSave()
    {
        SeedSlot("AutoSave");
        Assert.IsFalse(_sm.DeleteSlot("AutoSave"));
        Assert.IsTrue(File.Exists(Path.Combine(_root, "AutoSave", "GameSave.json")));
    }

    // ---------------------------------------------------------------- backups

    /// <summary>
    /// Logged during the 2026-08-16 audit: "Failed to create backup: the file already exists".
    /// The timestamp is second-precision, so two saves inside one second collided and the
    /// second backup was lost. Revert the de-duplication and this fails.
    /// </summary>
    [Test]
    public void RapidBackups_WithinTheSameSecond_DoNotCollide()
    {
        SeedSlot("Slot1");
        typeof(SaveManager).GetField("activeSlotName", Priv).SetValue(_sm, "Slot1");

        var create = typeof(SaveManager).GetMethod("CreateBackupSave", Priv);
        for (int i = 0; i < 3; i++)
            create.Invoke(_sm, null);

        string[] backups = Directory.GetFiles(Path.Combine(_root, "Slot1"), "GameSave_backup_*.json");
        Assert.AreEqual(3, backups.Length,
            "three saves in the same second must produce three distinct backup files");
    }
}

} // namespace SowurShield.Tests
