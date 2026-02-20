using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Play Mode integration tests for PlayerStats.
/// Full MonoBehaviour lifecycle runs: Awake, Start, Update.
/// Tests validate event firing, stat initialization, and level-up flow.
/// </summary>
public class PlayerStatsPlayModeTests
{
    private GameObject go;
    private PlayerStats stats;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        go = new GameObject("PlayerStats_PlayMode");
        stats = go.AddComponent<PlayerStats>();
        yield return null; // allow Awake + Start to complete
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(go);
        yield return null;
    }

    // =========================================================================
    // INITIALIZATION
    // =========================================================================

    [UnityTest]
    public IEnumerator Initializes_WithCurrentHealthEqualToMaxHealth()
    {
        yield return null;
        Assert.AreEqual(stats.maxHealth, stats.currentHealth);
    }

    [UnityTest]
    public IEnumerator Initializes_WithCurrentEnergyEqualToMaxEnergy()
    {
        yield return null;
        Assert.AreEqual(stats.maxEnergy, stats.currentEnergy);
    }

    // =========================================================================
    // EVENTS
    // =========================================================================

    [UnityTest]
    public IEnumerator AddMoney_FiresOnMoneyChangedEvent_WithCorrectValue()
    {
        int received = -1;
        stats.OnMoneyChanged += v => received = v;

        stats.money = 0;
        stats.AddMoney(150);

        yield return null;
        Assert.AreEqual(150, received);
    }

    [UnityTest]
    public IEnumerator SpendMoney_FiresOnMoneyChangedEvent_WhenSuccessful()
    {
        int fired = 0;
        stats.OnMoneyChanged += _ => fired++;
        stats.money = 200;

        stats.SpendMoney(50);
        yield return null;

        Assert.AreEqual(1, fired);
    }

    [UnityTest]
    public IEnumerator SpendMoney_DoesNotFireEvent_WhenInsufficient()
    {
        int fired = 0;
        stats.OnMoneyChanged += _ => fired++;
        stats.money = 10;

        stats.SpendMoney(100);
        yield return null;

        Assert.AreEqual(0, fired);
    }

    [UnityTest]
    public IEnumerator UseEnergy_FiresOnEnergyChangedEvent()
    {
        int fired = 0;
        stats.OnEnergyChanged += (cur, max) => fired++;

        stats.currentEnergy = 100;
        stats.UseEnergy(20);
        yield return null;

        Assert.AreEqual(1, fired);
    }

    [UnityTest]
    public IEnumerator RestoreEnergy_FiresOnEnergyChangedEvent()
    {
        int fired = 0;
        stats.OnEnergyChanged += (cur, max) => fired++;

        stats.currentEnergy = 50;
        stats.maxEnergy = 100;
        stats.RestoreEnergy(30);
        yield return null;

        Assert.AreEqual(1, fired);
    }

    [UnityTest]
    public IEnumerator RestoreHealth_FiresOnHealthChangedEvent()
    {
        int fired = 0;
        stats.OnHealthChanged += (cur, max) => fired++;

        stats.currentHealth = 50;
        stats.maxHealth = 100;
        stats.RestoreHealth(20);
        yield return null;

        Assert.AreEqual(1, fired);
    }

    // =========================================================================
    // LEVEL UP FLOW
    // =========================================================================

    [UnityTest]
    public IEnumerator LevelUp_RestoresFullHealthAndEnergy()
    {
        stats.maxHealth = 100;
        stats.maxEnergy = 100;
        stats.currentHealth = 40;
        stats.currentEnergy = 25;
        stats.experience = 0f;
        stats.experienceToNextLevel = 10f;

        stats.AddExperience(10f);
        yield return null;

        Assert.AreEqual(stats.maxHealth, stats.currentHealth,
            "Health should be fully restored on level up");
        Assert.AreEqual(stats.maxEnergy, stats.currentEnergy,
            "Energy should be fully restored on level up");
    }

    [UnityTest]
    public IEnumerator LevelUp_FiresOnLevelChangedEvent()
    {
        int newLevel = -1;
        stats.OnLevelChanged += lvl => newLevel = lvl;
        stats.experience = 0f;
        stats.experienceToNextLevel = 10f;

        stats.AddExperience(10f);
        yield return null;

        Assert.AreEqual(2, newLevel);
    }

    [UnityTest]
    public IEnumerator LevelUp_IncreasesMaxHealthAndEnergy()
    {
        int prevMaxHealth = stats.maxHealth;
        int prevMaxEnergy = stats.maxEnergy;
        stats.experience = 0f;
        stats.experienceToNextLevel = 10f;

        stats.AddExperience(10f);
        yield return null;

        Assert.Greater(stats.maxHealth, prevMaxHealth);
        Assert.Greater(stats.maxEnergy, prevMaxEnergy);
    }

    // =========================================================================
    // SAVE / LOAD IN PLAY MODE
    // =========================================================================

    [UnityTest]
    public IEnumerator SaveThenLoad_ProducesIdenticalStats()
    {
        stats.currentHealth = 55;
        stats.maxHealth = 110;
        stats.currentEnergy = 70;
        stats.money = 333;
        stats.playerLevel = 3;

        var saved = new GameData();
        stats.SaveData(saved);

        // Create a second fresh component and load into it
        var go2 = new GameObject("PlayerStats_Loaded");
        var stats2 = go2.AddComponent<PlayerStats>();
        yield return null;

        stats2.LoadData(saved);
        yield return null;

        Assert.AreEqual(stats.currentHealth, stats2.currentHealth);
        Assert.AreEqual(stats.maxHealth, stats2.maxHealth);
        Assert.AreEqual(stats.currentEnergy, stats2.currentEnergy);
        Assert.AreEqual(stats.money, stats2.money);
        Assert.AreEqual(stats.playerLevel, stats2.playerLevel);

        Object.Destroy(go2);
        yield return null;
    }
}
