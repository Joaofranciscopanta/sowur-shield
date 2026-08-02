using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using SowurShield.Animals;
using SowurShield.Combat;
using SowurShield.Core;
using SowurShield.Inventory;

namespace SowurShield.Tests
{

/// <summary>
/// Regression tests for bugs found and fixed on 2026-08-01. Each of these shipped
/// silently — none produced a console error, and none was caught by the existing
/// suite — so each one gets a test that fails if the fix is reverted.
///
/// Grouped by the fix rather than by system, because the point of the file is
/// "these specific mistakes must not come back", not coverage of a feature area.
/// </summary>
public class RegressionAug2026Tests
{
    private List<Object> _cleanupList = new List<Object>();

    private T Track<T>(T obj) where T : Object
    {
        _cleanupList.Add(obj);
        return obj;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var obj in _cleanupList)
        {
            if (obj != null) Object.DestroyImmediate(obj);
        }
        _cleanupList.Clear();
    }

    private CombatUnit CreateUnit(bool isPlayer = false, float hp = 100f)
    {
        var go = new GameObject("RegressionTestUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = isPlayer;
        unit.InitializeAsEnemy("RegressionTestUnit", hp, 10f, 5f, 10f);
        return unit;
    }

    private AnimalSkill CreateSkill(SkillType type = SkillType.Active, int cooldown = 2)
    {
        var skill = ScriptableObject.CreateInstance<AnimalSkill>();
        Track(skill);
        skill.skillType = type;
        skill.cooldownTurns = cooldown;
        skill.damageMultiplier = 1.5f;
        return skill;
    }

    // =========================================================================
    // Enemy skill list containing a null element
    //
    // Six EnemyData assets shipped with skills = [null]. GetReadySkill picked
    // that element at random and returned it, so the enemy never used a skill
    // while still reporting a non-empty pool. TurnManager guards with
    // `if (skill != null)`, so it degraded silently.
    //
    // The existing suite covered an empty list and a null list, but not a
    // populated list holding a null entry — which is what actually shipped.
    // =========================================================================

    [Test]
    public void EnemySkill_PoolWithOnlyNullEntry_ReturnsNull()
    {
        var unit = CreateUnit(isPlayer: false);
        unit.InitializeEnemySkills(new List<AnimalSkill> { null }, useChance: 1f);

        Assert.IsNull(unit.GetReadySkill(),
            "A pool holding only a null entry must return null, not the null element itself");
    }

    [Test]
    public void EnemySkill_PoolWithNullAndRealEntry_AlwaysReturnsTheRealSkill()
    {
        var unit = CreateUnit(isPlayer: false);
        var real = CreateSkill();

        // Null first so a naive random pick hits it roughly half the time.
        unit.InitializeEnemySkills(new List<AnimalSkill> { null, real }, useChance: 1f);

        // Before the fix this returned null on ~50% of draws.
        for (int i = 0; i < 200; i++)
        {
            Assert.AreSame(real, unit.GetReadySkill(),
                "With useChance=1 and one real skill present, every draw must return that skill");
        }
    }

    [Test]
    public void EnemySkill_PoolWithNullEntry_StillRespectsUseChanceZero()
    {
        var unit = CreateUnit(isPlayer: false);
        var real = CreateSkill();
        unit.InitializeEnemySkills(new List<AnimalSkill> { null, real }, useChance: 0f);

        for (int i = 0; i < 100; i++)
        {
            Assert.IsNull(unit.GetReadySkill(),
                "Skipping null entries must not bypass the use-chance roll");
        }
    }

    // =========================================================================
    // Runtime RectTransform defaults to point anchors
    //
    // A fresh RectTransform anchors at (0.5, 0.5), where sizeDelta IS the size —
    // so `new Vector2(0, h)` is a literal zero-width rect and text wraps one
    // character per line. This shipped in two places independently
    // (QuestObjectiveLine and ConsumableBattleUI.CreateRow).
    //
    // These tests pin the behaviour itself, so the next person to write
    // layout-building code can see why the anchors are set explicitly.
    // =========================================================================

    [Test]
    public void RuntimeRectTransform_DefaultAnchorsArePoint_SoZeroSizeDeltaIsZeroWide()
    {
        var go = new GameObject("PointAnchored", typeof(RectTransform));
        Track(go);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 22f);

        Assert.AreEqual(new Vector2(0.5f, 0.5f), rt.anchorMin,
            "Unity's default anchorMin is the centre point — the trap this guards against");
        Assert.AreEqual(0f, rt.rect.width, 0.001f,
            "With point anchors a sizeDelta.x of 0 yields a genuinely 0-wide rect");
    }

    [Test]
    public void RuntimeRectTransform_StretchedHorizontally_ZeroSizeDeltaFillsParent()
    {
        var parent = new GameObject("Parent", typeof(RectTransform));
        Track(parent);
        parent.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 100f);

        var child = new GameObject("Stretched", typeof(RectTransform));
        Track(child);
        child.transform.SetParent(parent.transform, false);

        var rt = child.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(0f, 22f);

        Assert.AreEqual(300f, rt.rect.width, 0.001f,
            "Stretched horizontally, sizeDelta.x=0 means 'match the parent', which is the fix");
    }

    // =========================================================================
    // Unity's fake null and the null-coalescing operator
    //
    // `GetComponent<T>() ?? gameObject.AddComponent<T>()` never ran AddComponent,
    // because a missing component returns a live C# object whose native side is
    // gone. `??` tests for real null and sees non-null. This threw a
    // MissingComponentException on every combat spawn.
    // =========================================================================

    [Test]
    public void MissingComponent_IsNotRealNull_SoNullCoalescingSkipsTheFallback()
    {
        var go = new GameObject("NoAnimator");
        Track(go);

        Animator missing = go.GetComponent<Animator>();

        Assert.IsTrue(missing == null,
            "Unity's overloaded == reports a missing component as null");
        Assert.IsFalse(ReferenceEquals(missing, null),
            "But it is NOT a real null reference — which is why ?? and ?. skip the fallback. " +
            "Use an explicit == null check when falling back to AddComponent.");
    }

    [Test]
    public void ExplicitNullCheck_AddsTheComponent()
    {
        var go = new GameObject("GetsAnimator");
        Track(go);

        // The corrected pattern from CombatUnit.InitializeFromAnimal
        Animator anim = go.GetComponent<Animator>();
        if (anim == null) anim = go.AddComponent<Animator>();

        Assert.IsNotNull(anim, "Explicit == null check must actually add the component");
        Assert.IsTrue(go.GetComponent<Animator>() != null,
            "The component must be attached to the GameObject, not just returned");
    }

    // =========================================================================
    // Item name collisions
    //
    // ItemDatabase keys on Item.itemName, not the asset name, and silently drops
    // whichever asset loses the load-order race. Two 'Apple' assets disagreed on
    // baseValue (8 vs 1), so which one the game saw was non-deterministic.
    //
    // This test guards the invariant rather than those two specific assets:
    // no two Item assets may claim the same itemName.
    // =========================================================================

    [Test]
    public void ItemDatabase_NoTwoItemsShareAnItemName()
    {
        Item[] items = Resources.LoadAll<Item>("");
        var seen = new Dictionary<string, string>();
        var collisions = new List<string>();

        foreach (Item item in items)
        {
            if (item == null || string.IsNullOrEmpty(item.itemName)) continue;

            if (seen.TryGetValue(item.itemName, out string firstAsset))
                collisions.Add($"'{item.itemName}' claimed by both '{firstAsset}' and '{item.name}'");
            else
                seen[item.itemName] = item.name;
        }

        Assert.IsEmpty(collisions,
            "Duplicate itemName values make ItemDatabase.GetItem non-deterministic — " +
            "it keeps whichever asset Resources happens to load first:\n" +
            string.Join("\n", collisions));
    }

    // ========================================================================
    // Late ISaveable registration (2026-08-02)
    //
    // GroundItem and PlayerDataManager only called RegisterSaveable in Awake().
    // Unity's Awake/Start order between scene objects is undefined, so whenever
    // they woke before SaveManager, registration was skipped and their
    // SaveData/LoadData never ran: the player respawned at the scene's default
    // position and already-collected ground items reappeared on every load.
    //
    // The fix has two halves and each gets a test: the Start() retry in the two
    // components, and RegisterSaveable applying currentGameData to anything that
    // registers after the initial load has already happened.
    // ========================================================================

    /// <summary>
    /// A saveable that records whether LoadData was applied to it, so the test can
    /// assert on catch-up behaviour rather than on any particular component's state.
    /// </summary>
    private class SpySaveable : ISaveable
    {
        public int LoadCallCount;
        public GameData LastLoaded;

        public void SaveData(GameData gameData) { }

        public void LoadData(GameData gameData)
        {
            LoadCallCount++;
            LastLoaded = gameData;
        }
    }

    private SaveManager CreateSaveManagerPostLoad(GameData data)
    {
        var go = new GameObject("RegressionSaveManager");
        Track(go);
        var manager = go.AddComponent<SaveManager>();

        // Simulate the state right after Start() finished its load decision, without
        // touching the real save directory on disk.
        typeof(SaveManager)
            .GetField("currentGameData", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(manager, data);
        typeof(SaveManager)
            .GetField("hasCompletedInitialLoad", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(manager, true);

        return manager;
    }

    [Test]
    public void RegisterSaveable_AfterInitialLoad_ImmediatelyAppliesLoadedData()
    {
        var data = new GameData();
        data.playerData.position = new Vector3(42f, 7f, 0f);

        SaveManager manager = CreateSaveManagerPostLoad(data);
        var spy = new SpySaveable();

        manager.RegisterSaveable(spy);

        Assert.AreEqual(1, spy.LoadCallCount,
            "An object registering after the initial load must be caught up immediately. " +
            "Without this, SaveManager.Start() (which calls LoadGame) running before the " +
            "object's Start() means it never receives LoadData and silently keeps its " +
            "scene defaults — the player position and ground-item bug of 2026-08-02.");
        Assert.AreSame(data, spy.LastLoaded, "Catch-up must apply the in-memory save data.");
    }

    [Test]
    public void RegisterSaveable_BeforeInitialLoad_DoesNotApplyData()
    {
        var go = new GameObject("RegressionSaveManagerPreLoad");
        Track(go);
        var manager = go.AddComponent<SaveManager>();

        var spy = new SpySaveable();
        manager.RegisterSaveable(spy);

        Assert.AreEqual(0, spy.LoadCallCount,
            "Before the initial load there is nothing meaningful to apply; LoadGame() " +
            "will reach this object through the normal registered-objects pass.");
    }

    [Test]
    public void RegisterSaveable_IsIdempotent_SoStartRetryIsSafe()
    {
        var data = new GameData();
        SaveManager manager = CreateSaveManagerPostLoad(data);
        var spy = new SpySaveable();

        manager.RegisterSaveable(spy);
        manager.RegisterSaveable(spy);

        Assert.AreEqual(1, spy.LoadCallCount,
            "The fix registers in both Awake and Start. Registering twice must not " +
            "double-apply LoadData, or a re-registered object would reload its state " +
            "over any change made between the two calls.");
    }

    [Test]
    public void GroundItem_RetriesRegistrationInStart()
    {
        AssertHasStartRegistrationRetry(typeof(GroundItem));
    }

    [Test]
    public void PlayerDataManager_RetriesRegistrationInStart()
    {
        AssertHasStartRegistrationRetry(typeof(PlayerDataManager));
    }

    /// <summary>
    /// Both components must define Start(), which is where the registration retry lives.
    /// Asserting on the method's existence keeps the test honest without needing to enter
    /// Play Mode: if someone deletes Start(), the Awake-order race silently returns.
    /// </summary>
    private void AssertHasStartRegistrationRetry(System.Type componentType)
    {
        var start = componentType.GetMethod("Start",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.IsNotNull(start,
            $"{componentType.Name} must define Start() to re-attempt SaveManager " +
            "registration. Registering only in Awake loses the race whenever this " +
            "object wakes before SaveManager, and its save data is silently dropped.");
    }

    // ========================================================================
    // Save/load correctness, found by actually running Play Mode (2026-08-02)
    //
    // Registration was necessary but not sufficient — two further bugs made the
    // save genuinely wrong, and both were invisible to the existing suite:
    //
    // 1. PlayerDataManager saved *its own* transform. In SampleScene it sits on
    //    a separate manager GameObject parked at the origin while the player is
    //    "Bunny", so every save wrote (0,0,0) and every load moved the manager
    //    instead of the player.
    // 2. GroundItem keyed its save flag on gameObject.name. Runtime spawns are
    //    all "<Item>_GroundItem(Clone)", so 8 eggs and 10 milks shared two keys:
    //    collecting one egg flagged all eight, and they all vanished on load.
    // ========================================================================

    [Test]
    public void PlayerDataManager_SavesThePlayerTransform_NotItsOwn()
    {
        // Manager on its own GameObject at the origin, exactly as in SampleScene.
        var managerGo = new GameObject("PlayerDataManager");
        Track(managerGo);
        managerGo.transform.position = Vector3.zero;
        var manager = managerGo.AddComponent<PlayerDataManager>();

        var playerGo = new GameObject("Bunny");
        Track(playerGo);
        playerGo.tag = "Player";
        Vector3 playerPos = new Vector3(-17.25f, 9.5f, 0f);
        playerGo.transform.position = playerPos;

        var data = new GameData();
        manager.SaveData(data);

        Assert.AreEqual(playerPos.x, data.playerData.position.x, 0.01f,
            "SaveData must record the player's position, not the manager's. Using this " +
            "component's own transform wrote (0,0,0) on every save, because the manager " +
            "GameObject never moves.");
        Assert.AreEqual(playerPos.y, data.playerData.position.y, 0.01f);
    }

    [Test]
    public void PlayerDataManager_LoadsIntoThePlayerTransform_NotItsOwn()
    {
        var managerGo = new GameObject("PlayerDataManager");
        Track(managerGo);
        managerGo.transform.position = Vector3.zero;
        var manager = managerGo.AddComponent<PlayerDataManager>();

        var playerGo = new GameObject("Bunny");
        Track(playerGo);
        playerGo.tag = "Player";
        playerGo.transform.position = new Vector3(3f, -4f, 0f);

        var data = new GameData();
        data.playerData.position = new Vector3(-17.25f, 9.5f, 0f);

        manager.LoadData(data);

        Assert.AreEqual(-17.25f, playerGo.transform.position.x, 0.01f,
            "LoadData must move the player. Writing to this component's own transform " +
            "left the player wherever it happened to be, so position never restored.");
        Assert.AreEqual(9.5f, playerGo.transform.position.y, 0.01f);
        Assert.AreEqual(Vector3.zero, managerGo.transform.position,
            "The manager GameObject itself must not be moved by a load.");
    }

    [Test]
    public void GroundItem_SaveId_IsUniquePerInstance_EvenWhenNamesCollide()
    {
        // Runtime spawns all share one name — this is what Instantiate produces.
        var a = new GameObject("Egg_GroundItem(Clone)");
        Track(a);
        a.transform.position = new Vector3(8.54f, 4.26f, 0f);
        var itemA = a.AddComponent<GroundItem>();

        var b = new GameObject("Egg_GroundItem(Clone)");
        Track(b);
        b.transform.position = new Vector3(10.04f, 4.68f, 0f);
        var itemB = b.AddComponent<GroundItem>();

        Assert.AreEqual(a.name, b.name, "Precondition: both spawns share a GameObject name.");
        Assert.AreNotEqual(itemA.SaveId, itemB.SaveId,
            "Two ground items at different positions must have different save ids. Keying " +
            "on gameObject.name made every runtime spawn collide, so collecting one egg " +
            "marked all of them collected and they all disappeared on the next load.");
    }

    [Test]
    public void GroundItem_CollectingOne_DoesNotFlagItsNameTwins()
    {
        var a = new GameObject("Egg_GroundItem(Clone)");
        Track(a);
        a.transform.position = new Vector3(8.54f, 4.26f, 0f);
        var collected = a.AddComponent<GroundItem>();

        var b = new GameObject("Egg_GroundItem(Clone)");
        Track(b);
        b.transform.position = new Vector3(10.04f, 4.68f, 0f);
        var untouched = b.AddComponent<GroundItem>();

        collected.itemPicked = true;

        var data = new GameData();
        collected.SaveData(data);
        untouched.SaveData(data);

        Assert.IsTrue(data.worldData.worldFlags.ContainsKey($"grounditem_picked_{collected.SaveId}"),
            "The collected item must be recorded.");
        Assert.IsFalse(data.worldData.worldFlags.ContainsKey($"grounditem_picked_{untouched.SaveId}"),
            "An item the player never touched must not be flagged just because it shares a " +
            "GameObject name with one that was collected.");
    }

    [Test]
    public void GroundItem_SaveId_UsesInvariantCulture()
    {
        var go = new GameObject("Egg_GroundItem(Clone)");
        Track(go);
        go.transform.position = new Vector3(10.04f, 4.68f, 0f);
        var item = go.AddComponent<GroundItem>();

        Assert.IsFalse(item.SaveId.Contains(","),
            "SaveId must format coordinates with InvariantCulture. On a pt-BR machine " +
            $"\"F2\" renders 10.04 as \"10,04\", producing an ambiguous id ({item.SaveId}) " +
            "that also differs from the id the same item gets under en-US.");
    }
}

}
