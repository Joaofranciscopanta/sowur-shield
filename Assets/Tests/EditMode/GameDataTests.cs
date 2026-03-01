using NUnit.Framework;
using SowurShield.Core;

/// <summary>
/// Edit Mode tests for GameData and GameDataExtensions.
/// Pure C# — no MonoBehaviours or scenes required.
/// </summary>
public class GameDataTests
{
    private GameData data;

    [SetUp]
    public void SetUp()
    {
        data = new GameData();
    }

    // =========================================================================
    // CONSTRUCTOR INITIALIZATION
    // =========================================================================

    [Test]
    public void Constructor_InitializesAllSubsystems()
    {
        Assert.IsNotNull(data.playerData);
        Assert.IsNotNull(data.worldData);
        Assert.IsNotNull(data.timeData);
        Assert.IsNotNull(data.inventoryData);
        Assert.IsNotNull(data.farmingData);
        Assert.IsNotNull(data.relationshipData);
        Assert.IsNotNull(data.combatData);
        Assert.IsNotNull(data.progressData);
    }

    [Test]
    public void Constructor_SetsSaveVersionToDefault()
    {
        Assert.AreEqual("1.0.0", data.saveVersion);
    }

    [Test]
    public void Constructor_SetsSaveTimestamp()
    {
        Assert.IsNotNull(data.saveTimestamp);
        Assert.IsNotEmpty(data.saveTimestamp);
    }

    // =========================================================================
    // PLAYER GAME DATA DEFAULTS
    // =========================================================================

    [Test]
    public void PlayerGameData_HasDefaultStartingMoney()
    {
        Assert.Greater(data.playerData.money, 0);
    }

    [Test]
    public void PlayerGameData_StartsAtLevelOne()
    {
        Assert.AreEqual(1, data.playerData.playerLevel);
    }

    [Test]
    public void PlayerGameData_HasAllDefaultSkills()
    {
        Assert.IsTrue(data.playerData.skillLevels.ContainsKey("Farming"));
        Assert.IsTrue(data.playerData.skillLevels.ContainsKey("Combat"));
        Assert.IsTrue(data.playerData.skillLevels.ContainsKey("Social"));
        Assert.IsTrue(data.playerData.skillLevels.ContainsKey("Cooking"));
    }

    [Test]
    public void PlayerGameData_AllDefaultSkillLevels_AreOne()
    {
        foreach (var skill in data.playerData.skillLevels)
            Assert.AreEqual(1, skill.Value, $"Skill '{skill.Key}' should start at level 1");
    }

    [Test]
    public void PlayerGameData_DefaultHealth_IsPositive()
    {
        Assert.Greater(data.playerData.health, 0f);
    }

    [Test]
    public void PlayerGameData_DefaultEnergy_IsPositive()
    {
        Assert.Greater(data.playerData.energy, 0f);
    }

    // =========================================================================
    // WORLD DATA DEFAULTS
    // =========================================================================

    [Test]
    public void WorldGameData_DiscoveredAreas_ContainsFarm()
    {
        Assert.Contains("Farm", data.worldData.discoveredAreas);
    }

    [Test]
    public void WorldGameData_CurrentWeather_IsSunny()
    {
        Assert.AreEqual("Sunny", data.worldData.currentWeather);
    }

    // =========================================================================
    // TIME DATA DEFAULTS
    // =========================================================================

    [Test]
    public void TimeGameData_StartsOnDayOne()
    {
        Assert.AreEqual(1, data.timeData.currentDay);
    }

    [Test]
    public void TimeGameData_StartsInSpring()
    {
        Assert.AreEqual("Spring", data.timeData.season);
    }

    [Test]
    public void TimeGameData_StartsInYearOne()
    {
        Assert.AreEqual(1, data.timeData.year);
    }

    // =========================================================================
    // PROGRESS DATA DEFAULTS
    // =========================================================================

    [Test]
    public void ProgressGameData_HasMasterVolumeDefaultOfOne()
    {
        Assert.AreEqual(1.0f, data.progressData.gameSettings["masterVolume"], 0.001f);
    }

    [Test]
    public void ProgressGameData_HasMusicVolumeDefault()
    {
        Assert.IsTrue(data.progressData.gameSettings.ContainsKey("musicVolume"));
    }

    [Test]
    public void ProgressGameData_HasSfxVolumeDefault()
    {
        Assert.IsTrue(data.progressData.gameSettings.ContainsKey("sfxVolume"));
    }

    [Test]
    public void ProgressGameData_AutoSave_EnabledByDefault()
    {
        Assert.IsTrue(data.progressData.gamePreferences["autoSave"]);
    }

    [Test]
    public void ProgressGameData_CropsHarvestedCounter_StartsAtZero()
    {
        Assert.AreEqual(0, data.progressData.statisticCounters["cropsHarvested"]);
    }

    // =========================================================================
    // WORLD FLAG EXTENSIONS
    // =========================================================================

    [Test]
    public void GetWorldFlag_ReturnsFalse_ForNonExistentFlag()
    {
        Assert.IsFalse(data.GetWorldFlag("this_flag_does_not_exist"));
    }

    [Test]
    public void SetWorldFlag_True_ThenGetWorldFlag_ReturnsTrue()
    {
        data.SetWorldFlag("tutorial_done", true);
        Assert.IsTrue(data.GetWorldFlag("tutorial_done"));
    }

    [Test]
    public void SetWorldFlag_False_ThenGetWorldFlag_ReturnsFalse()
    {
        data.SetWorldFlag("quest_active", true);
        data.SetWorldFlag("quest_active", false);
        Assert.IsFalse(data.GetWorldFlag("quest_active"));
    }

    [Test]
    public void SetWorldFlag_CanSetMultipleDistinctFlags()
    {
        data.SetWorldFlag("flag_a", true);
        data.SetWorldFlag("flag_b", false);
        data.SetWorldFlag("flag_c", true);

        Assert.IsTrue(data.GetWorldFlag("flag_a"));
        Assert.IsFalse(data.GetWorldFlag("flag_b"));
        Assert.IsTrue(data.GetWorldFlag("flag_c"));
    }

    // =========================================================================
    // WORLD COUNTER EXTENSIONS
    // =========================================================================

    [Test]
    public void GetWorldCounter_ReturnsZero_ForNonExistentCounter()
    {
        Assert.AreEqual(0, data.GetWorldCounter("nonexistent"));
    }

    [Test]
    public void SetWorldCounter_ThenGetWorldCounter_ReturnsCorrectValue()
    {
        data.SetWorldCounter("crops_planted", 42);
        Assert.AreEqual(42, data.GetWorldCounter("crops_planted"));
    }

    [Test]
    public void SetWorldCounter_OverwritesPreviousValue()
    {
        data.SetWorldCounter("gold", 100);
        data.SetWorldCounter("gold", 250);
        Assert.AreEqual(250, data.GetWorldCounter("gold"));
    }

    [Test]
    public void IncrementWorldCounter_IncreasesByOne_ByDefault()
    {
        data.SetWorldCounter("days", 5);
        data.IncrementWorldCounter("days");
        Assert.AreEqual(6, data.GetWorldCounter("days"));
    }

    [Test]
    public void IncrementWorldCounter_IncreasesByCustomAmount()
    {
        data.SetWorldCounter("gold", 100);
        data.IncrementWorldCounter("gold", 50);
        Assert.AreEqual(150, data.GetWorldCounter("gold"));
    }

    [Test]
    public void IncrementWorldCounter_CreatesCounter_StartingFromZero()
    {
        data.IncrementWorldCounter("brand_new_counter", 7);
        Assert.AreEqual(7, data.GetWorldCounter("brand_new_counter"));
    }

    [Test]
    public void IncrementWorldCounter_CanBeCalledMultipleTimes()
    {
        for (int i = 0; i < 5; i++)
            data.IncrementWorldCounter("steps");

        Assert.AreEqual(5, data.GetWorldCounter("steps"));
    }

    // =========================================================================
    // INVENTORY GAME DATA
    // =========================================================================

    [Test]
    public void InventoryGameData_InitializesWithCorrectSlotCount()
    {
        Assert.AreEqual(data.inventoryData.inventorySize, data.inventoryData.inventoryItems.Count);
    }

    [Test]
    public void InventoryGameData_AllSlots_StartEmpty()
    {
        foreach (var slot in data.inventoryData.inventoryItems)
            Assert.IsTrue(slot.IsEmpty, "All inventory slots should start empty");
    }

    [Test]
    public void ItemStackData_IsEmpty_True_ForDefaultConstruction()
    {
        var stackData = new InventoryGameData.ItemStackData();
        Assert.IsTrue(stackData.IsEmpty);
    }

    [Test]
    public void ItemStackData_IsEmpty_False_WhenNameAndQuantitySet()
    {
        var stackData = new InventoryGameData.ItemStackData("Carrot", 3);
        Assert.IsFalse(stackData.IsEmpty);
    }

    [Test]
    public void ItemStackData_IsEmpty_True_WhenQuantityIsZero()
    {
        var stackData = new InventoryGameData.ItemStackData("Carrot", 0);
        Assert.IsTrue(stackData.IsEmpty);
    }
}
