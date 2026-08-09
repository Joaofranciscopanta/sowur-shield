using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SowurShield.Core;

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
        Object.DestroyImmediate(go);
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

        Object.DestroyImmediate(go2);
        yield return null;
    }

    // =========================================================================
    // COMBAT REWARDS
    //
    // CombatScene has no PlayerStats, so BattleResultsUI stashes gold and XP on the
    // persistent TeamAssemblerData and PlayerStats collects them when SampleScene loads.
    //
    // The bug these cover: PlayerStats.Start() used to award them immediately, and
    // SaveManager.Start() then called LoadGame(), whose LoadData assigns
    // `money = gameData.playerData.money` outright — wiping the reward. Both scripts sit at
    // execution order 0, so which Start() ran first was arbitrary, making it look
    // intermittent. The pending value had already been zeroed, so the gold was gone from
    // both places: the player finished a battle and their money did not move.
    // =========================================================================

    [UnityTest]
    public IEnumerator PendingCombatGold_SurvivesALoadThatRunsAfterIt()
    {
        var teamData = SowurShield.Combat.TeamAssemblerData.Instance;
        int savedGold = teamData.pendingGoldReward;
        float savedXp = teamData.pendingXpReward;

        try
        {
            var go2 = new GameObject("PlayerStats_RewardOrder");
            var stats2 = go2.AddComponent<PlayerStats>();
            yield return null;   // Awake + Start

            stats2.money = 67;

            // A save captured before the battle: this is what the load will apply.
            var preBattle = new GameData();
            stats2.SaveData(preBattle);

            // Battle winnings arrive, then the load lands on top — the exact order that
            // erased them. Applying the reward first mimics the old Start().
            teamData.pendingGoldReward = 500;
            teamData.pendingXpReward = 0f;

            var apply = typeof(PlayerStats).GetMethod("ApplyPendingCombatRewards",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(apply, "ApplyPendingCombatRewards is gone — update this test with it.");
            apply.Invoke(stats2, null);

            Assert.AreEqual(567, stats2.money, "The reward was not credited at all.");

            // The real defence is ordering, enforced in Start(). What this asserts is the
            // consequence that made the bug permanent rather than merely late: once the
            // reward is consumed it must already be inside anything that gets saved, so a
            // later save/load round-trip carries it instead of reverting to the old total.
            var afterBattle = new GameData();
            stats2.SaveData(afterBattle);
            stats2.LoadData(afterBattle);
            yield return null;

            Assert.AreEqual(567, stats2.money,
                "Combat gold was lost to a save/load round-trip after being awarded.");

            Object.DestroyImmediate(go2);
            yield return null;
        }
        finally
        {
            teamData.pendingGoldReward = savedGold;
            teamData.pendingXpReward = savedXp;
        }
    }

    [UnityTest]
    public IEnumerator PendingCombatRewards_AreConsumedExactlyOnce()
    {
        var teamData = SowurShield.Combat.TeamAssemblerData.Instance;
        int savedGold = teamData.pendingGoldReward;
        float savedXp = teamData.pendingXpReward;

        try
        {
            var go2 = new GameObject("PlayerStats_RewardOnce");
            var stats2 = go2.AddComponent<PlayerStats>();
            yield return null;

            stats2.money = 0;
            teamData.pendingGoldReward = 250;
            teamData.pendingXpReward = 0f;

            var apply = typeof(PlayerStats).GetMethod("ApplyPendingCombatRewards",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            apply.Invoke(stats2, null);
            apply.Invoke(stats2, null);   // a second scene load must not pay again
            yield return null;

            Assert.AreEqual(250, stats2.money, "Combat gold was awarded more than once.");
            Assert.AreEqual(0, teamData.pendingGoldReward, "Pending gold was not cleared.");

            Object.DestroyImmediate(go2);
            yield return null;
        }
        finally
        {
            teamData.pendingGoldReward = savedGold;
            teamData.pendingXpReward = savedXp;
        }
    }
}
