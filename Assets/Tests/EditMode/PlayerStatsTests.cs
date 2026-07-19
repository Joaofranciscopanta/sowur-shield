using NUnit.Framework;
using UnityEngine;
using SowurShield.Core;

/// <summary>
/// Edit Mode tests for PlayerStats.
/// Awake() runs when AddComponent is called; Start() does not run in Edit Mode.
/// SaveManager and UIManager will be null — that's expected and safe.
/// </summary>
public class PlayerStatsTests
{
    private GameObject go;
    private PlayerStats stats;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("PlayerStats_Test");
        stats = go.AddComponent<PlayerStats>();
        // Awake initializes currentHealth = maxHealth, currentEnergy = maxEnergy
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
    }

    // =========================================================================
    // MONEY
    // =========================================================================

    [Test]
    public void AddMoney_IncreasesMoneyByAmount()
    {
        stats.money = 0;
        stats.AddMoney(50);
        Assert.AreEqual(50, stats.money);
    }

    [Test]
    public void AddMoney_AccumulatesCorrectly()
    {
        stats.money = 100;
        stats.AddMoney(75);
        Assert.AreEqual(175, stats.money);
    }

    [Test]
    public void AddMoney_FiresOnMoneyChangedEvent()
    {
        int received = -1;
        stats.OnMoneyChanged += amount => received = amount;
        stats.money = 0;
        stats.AddMoney(200);
        Assert.AreEqual(200, received);
    }

    [Test]
    public void SpendMoney_DecreasesMoneyWhenSufficient()
    {
        stats.money = 100;
        bool result = stats.SpendMoney(60);
        Assert.IsTrue(result);
        Assert.AreEqual(40, stats.money);
    }

    [Test]
    public void SpendMoney_ReturnsFalseWhenInsufficient()
    {
        stats.money = 30;
        bool result = stats.SpendMoney(50);
        Assert.IsFalse(result);
    }

    [Test]
    public void SpendMoney_DoesNotChangeMoneyWhenInsufficient()
    {
        stats.money = 30;
        stats.SpendMoney(50);
        Assert.AreEqual(30, stats.money);
    }

    [Test]
    public void SpendMoney_AllowsSpendingExactBalance()
    {
        stats.money = 50;
        bool result = stats.SpendMoney(50);
        Assert.IsTrue(result);
        Assert.AreEqual(0, stats.money);
    }

    [Test]
    public void HasMoney_ReturnsTrueWhenExactAmount()
    {
        stats.money = 100;
        Assert.IsTrue(stats.HasMoney(100));
    }

    [Test]
    public void HasMoney_ReturnsTrueWhenMoreThanEnough()
    {
        stats.money = 200;
        Assert.IsTrue(stats.HasMoney(50));
    }

    [Test]
    public void HasMoney_ReturnsFalseWhenInsufficient()
    {
        stats.money = 30;
        Assert.IsFalse(stats.HasMoney(50));
    }

    // =========================================================================
    // ENERGY
    // =========================================================================

    [Test]
    public void UseEnergy_DecreasesEnergyByAmount()
    {
        stats.currentEnergy = 100;
        stats.UseEnergy(30);
        Assert.AreEqual(70, stats.currentEnergy);
    }

    [Test]
    public void UseEnergy_ClampsAtZero()
    {
        stats.currentEnergy = 10;
        stats.UseEnergy(50);
        Assert.AreEqual(0, stats.currentEnergy);
    }

    [Test]
    public void UseEnergy_FiresOnEnergyChangedEvent()
    {
        int fired = 0;
        stats.OnEnergyChanged += (cur, max) => fired++;
        stats.currentEnergy = 100;
        stats.UseEnergy(20);
        Assert.AreEqual(1, fired);
    }

    [Test]
    public void UseEnergy_DoesNotFireEvent_WhenEnergyAlreadyZero()
    {
        int fired = 0;
        stats.OnEnergyChanged += (cur, max) => fired++;
        stats.currentEnergy = 0;
        stats.UseEnergy(20);
        Assert.AreEqual(0, fired);
    }

    [Test]
    public void RestoreEnergy_IncreasesEnergyByAmount()
    {
        stats.currentEnergy = 50;
        stats.maxEnergy = 100;
        stats.RestoreEnergy(30);
        Assert.AreEqual(80, stats.currentEnergy);
    }

    [Test]
    public void RestoreEnergy_ClampsAtMaxEnergy()
    {
        stats.currentEnergy = 90;
        stats.maxEnergy = 100;
        stats.RestoreEnergy(50);
        Assert.AreEqual(100, stats.currentEnergy);
    }

    [Test]
    public void HasEnergy_ReturnsTrueWhenExactAmount()
    {
        stats.currentEnergy = 50;
        Assert.IsTrue(stats.HasEnergy(50));
    }

    [Test]
    public void HasEnergy_ReturnsFalseWhenInsufficient()
    {
        stats.currentEnergy = 30;
        Assert.IsFalse(stats.HasEnergy(50));
    }

    // =========================================================================
    // HEALTH
    // =========================================================================

    [Test]
    public void RestoreHealth_IncreasesHealthByAmount()
    {
        stats.currentHealth = 40;
        stats.maxHealth = 100;
        stats.RestoreHealth(20);
        Assert.AreEqual(60, stats.currentHealth);
    }

    [Test]
    public void RestoreHealth_ClampsAtMaxHealth()
    {
        stats.currentHealth = 95;
        stats.maxHealth = 100;
        stats.RestoreHealth(50);
        Assert.AreEqual(100, stats.currentHealth);
    }

    [Test]
    public void SetHealth_ClampsBetweenZeroAndMax()
    {
        stats.maxHealth = 100;
        stats.SetHealth(150f);
        Assert.AreEqual(100, stats.currentHealth);

        stats.SetHealth(-10f);
        Assert.AreEqual(0, stats.currentHealth);
    }

    // =========================================================================
    // EXPERIENCE & LEVELING
    // =========================================================================

    [Test]
    public void AddExperience_IncreasesExperience()
    {
        stats.experience = 0f;
        stats.experienceToNextLevel = 100f;
        stats.AddExperience(40f);
        Assert.AreEqual(40f, stats.experience, 0.001f);
    }

    [Test]
    public void AddExperience_TriggersLevelUp_WhenThresholdReached()
    {
        stats.playerLevel = 1;
        stats.experience = 0f;
        stats.experienceToNextLevel = 100f;
        stats.AddExperience(100f);
        Assert.AreEqual(2, stats.playerLevel);
    }

    [Test]
    public void LevelUp_IncreasesMaxHealth()
    {
        int initialMax = stats.maxHealth;
        stats.playerLevel = 1;
        stats.experience = 0f;
        stats.experienceToNextLevel = 10f;
        stats.AddExperience(10f);
        Assert.AreEqual(initialMax + 10, stats.maxHealth);
    }

    [Test]
    public void LevelUp_IncreasesMaxEnergy()
    {
        int initialMax = stats.maxEnergy;
        stats.playerLevel = 1;
        stats.experience = 0f;
        stats.experienceToNextLevel = 10f;
        stats.AddExperience(10f);
        Assert.AreEqual(initialMax + 5, stats.maxEnergy);
    }

    [Test]
    public void AddExperience_HandlesChainedLevelUps()
    {
        stats.playerLevel = 1;
        stats.experience = 0f;
        stats.experienceToNextLevel = 10f;
        // Each level threshold grows by 20%: 10 → 12 → 14.4
        // 10+12 = 22 exp needed for 2 level-ups
        stats.AddExperience(22f);
        Assert.GreaterOrEqual(stats.playerLevel, 3);
    }

    [Test]
    public void AddExperience_FiresOnLevelChangedEvent()
    {
        int newLevel = -1;
        stats.OnLevelChanged += lvl => newLevel = lvl;
        stats.experience = 0f;
        stats.experienceToNextLevel = 10f;
        stats.AddExperience(10f);
        Assert.AreEqual(2, newLevel);
    }

    // =========================================================================
    // SAVE / LOAD
    // =========================================================================

    [Test]
    public void SaveData_WritesAllFieldsToGameData()
    {
        stats.currentHealth = 75;
        stats.maxHealth = 110;
        stats.currentEnergy = 60;
        stats.maxEnergy = 105;
        stats.money = 350;
        stats.playerLevel = 4;
        stats.experience = 55f;
        stats.experienceToNextLevel = 120f;

        var data = new GameData();
        stats.SaveData(data);

        Assert.AreEqual(75f, data.playerData.health);
        Assert.AreEqual(110f, data.playerData.maxHealth);
        Assert.AreEqual(60f, data.playerData.energy);
        Assert.AreEqual(105f, data.playerData.maxEnergy);
        Assert.AreEqual(350, data.playerData.money);
        Assert.AreEqual(4, data.playerData.playerLevel);
        Assert.AreEqual(55f, data.playerData.experience, 0.001f);
        Assert.AreEqual(120f, data.playerData.experienceToNextLevel, 0.001f);
    }

    [Test]
    public void LoadData_RestoresAllFieldsFromGameData()
    {
        var data = new GameData();
        data.playerData.health = 45f;
        data.playerData.maxHealth = 130f;
        data.playerData.energy = 80f;
        data.playerData.maxEnergy = 115f;
        data.playerData.money = 500;
        data.playerData.playerLevel = 6;
        data.playerData.experience = 33f;

        stats.LoadData(data);

        Assert.AreEqual(45, stats.currentHealth);
        Assert.AreEqual(130, stats.maxHealth);
        Assert.AreEqual(80, stats.currentEnergy);
        Assert.AreEqual(115, stats.maxEnergy);
        Assert.AreEqual(500, stats.money);
        Assert.AreEqual(6, stats.playerLevel);
        Assert.AreEqual(33f, stats.experience, 0.001f);
    }

    [Test]
    public void SaveLoad_RoundTrip_PreservesAllValues()
    {
        stats.currentHealth = 60;
        stats.maxHealth = 120;
        stats.currentEnergy = 40;
        stats.maxEnergy = 110;
        stats.money = 777;
        stats.playerLevel = 5;
        stats.experience = 88f;
        stats.experienceToNextLevel = 200f;

        var data = new GameData();
        stats.SaveData(data);

        var go2 = new GameObject();
        var stats2 = go2.AddComponent<PlayerStats>();
        stats2.LoadData(data);

        Assert.AreEqual(stats.currentHealth, stats2.currentHealth);
        Assert.AreEqual(stats.maxHealth, stats2.maxHealth);
        Assert.AreEqual(stats.currentEnergy, stats2.currentEnergy);
        Assert.AreEqual(stats.maxEnergy, stats2.maxEnergy);
        Assert.AreEqual(stats.money, stats2.money);
        Assert.AreEqual(stats.playerLevel, stats2.playerLevel);
        Assert.AreEqual(stats.experience, stats2.experience, 0.001f);
        Assert.AreEqual(stats.experienceToNextLevel, stats2.experienceToNextLevel, 0.001f);

        Object.DestroyImmediate(go2);
    }
}
