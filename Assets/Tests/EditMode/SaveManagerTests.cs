using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SowurShield.Core;

/// <summary>
/// Edit Mode tests for SaveManager.
/// Awake() runs when AddComponent is called; Start() does not run in Edit Mode.
/// </summary>
public class SaveManagerTests
{
    private GameObject go;
    private SaveManager manager;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("SaveManager_Test");
        manager = go.AddComponent<SaveManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
    }

    private GameData InvokeMigrateSave(GameData data)
    {
        MethodInfo method = typeof(SaveManager).GetMethod("MigrateSave", BindingFlags.NonPublic | BindingFlags.Instance);
        return (GameData)method.Invoke(manager, new object[] { data });
    }

    [Test]
    public void MigrateSave_BelowCurrentVersion_BumpsToCurrentVersion()
    {
        var data = new GameData();
        data.saveVersion = GameData.CURRENT_SAVE_VERSION - 1;

        var migrated = InvokeMigrateSave(data);

        Assert.AreEqual(GameData.CURRENT_SAVE_VERSION, migrated.saveVersion);
    }

    [Test]
    public void MigrateSave_AtCurrentVersion_IsNoOp()
    {
        var data = new GameData();
        data.saveVersion = GameData.CURRENT_SAVE_VERSION;

        var migrated = InvokeMigrateSave(data);

        Assert.AreEqual(GameData.CURRENT_SAVE_VERSION, migrated.saveVersion);
    }
}
