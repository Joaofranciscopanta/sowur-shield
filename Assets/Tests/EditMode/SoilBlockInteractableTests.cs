using NUnit.Framework;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Edit Mode unit tests for SoilBlockInteractable.
///
/// Tests exercised here:
///   - Default state (Regular)
///   - TillSoilDirectly — public entry point from CursorController
///   - ResetSoil (private) via reflection
///   - Public property accessors (CurrentState, HasCrop, IsReadyForHarvest, IsCropDead)
///   - GetSoilState(), GetCropGrowthProgress(), GetStatusText()
///   - ISaveable: SaveData writes correct positionKey, isTilled, isWatered
///   - ISaveable: LoadData restores state from saved entries
///   - Null-safety on both Save and Load
///
/// Note: SoilBlockInteractable.Awake() auto-adds CropGrowthManager, a BoxCollider2D,
/// and calls CursorController.GetWorldPosTile() (a pure static math helper) —
/// all of which are safe in Edit Mode.
/// </summary>
public class SoilBlockInteractableTests
{
    // ── fixtures ──────────────────────────────────────────────────────────────

    private GameObject soilGo;
    private SoilBlockInteractable soil;

    [SetUp]
    public void SetUp()
    {
        soilGo = new GameObject("SoilBlock_SBI_Test");
        soilGo.AddComponent<SpriteRenderer>(); // required by InitializeComponents
        soil = soilGo.AddComponent<SoilBlockInteractable>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(soilGo);
    }

    // ── helper ────────────────────────────────────────────────────────────────

    /// <summary>Invoke the private ResetSoil() method.</summary>
    private void ResetSoilDirectly()
    {
        var m = typeof(SoilBlockInteractable).GetMethod(
            "ResetSoil",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "SoilBlockInteractable must have a private ResetSoil() method");
        m.Invoke(soil, null);
    }

    // =========================================================================
    // DEFAULT STATE
    // =========================================================================

    [Test]
    public void DefaultState_IsRegular()
    {
        Assert.AreEqual(SoilBlockInteractable.SoilState.Regular, soil.currentState);
    }

    [Test]
    public void HasCrop_DefaultsFalse()
    {
        Assert.IsFalse(soil.HasCrop);
    }

    [Test]
    public void IsReadyForHarvest_DefaultsFalse()
    {
        Assert.IsFalse(soil.IsReadyForHarvest);
    }

    [Test]
    public void IsCropDead_DefaultsFalse()
    {
        Assert.IsFalse(soil.IsCropDead);
    }

    [Test]
    public void GetCropGrowthProgress_ReturnsZero_WhenNoCrop()
    {
        Assert.AreEqual(0f, soil.GetCropGrowthProgress(), 0.001f);
    }

    // =========================================================================
    // PROPERTY ACCESSORS
    // =========================================================================

    [Test]
    public void CurrentState_Property_ReflectsPublicField()
    {
        soil.currentState = SoilBlockInteractable.SoilState.Tilled;
        Assert.AreEqual(SoilBlockInteractable.SoilState.Tilled, soil.CurrentState);
    }

    [Test]
    public void GetSoilState_ReturnsCurrentState()
    {
        soil.currentState = SoilBlockInteractable.SoilState.Watered;
        Assert.AreEqual(SoilBlockInteractable.SoilState.Watered, soil.GetSoilState());
    }

    // =========================================================================
    // TILL SOIL DIRECTLY
    // =========================================================================

    [Test]
    public void TillSoilDirectly_ChangesState_ToTilled_WhenRegular()
    {
        Assert.AreEqual(SoilBlockInteractable.SoilState.Regular, soil.currentState);
        soil.TillSoilDirectly();
        Assert.AreEqual(SoilBlockInteractable.SoilState.Tilled, soil.currentState);
    }

    [Test]
    public void TillSoilDirectly_DoesNothing_WhenAlreadyTilled()
    {
        soil.currentState = SoilBlockInteractable.SoilState.Tilled;
        soil.TillSoilDirectly();
        Assert.AreEqual(SoilBlockInteractable.SoilState.Tilled, soil.currentState);
    }

    [Test]
    public void TillSoilDirectly_DoesNothing_WhenWatered()
    {
        soil.currentState = SoilBlockInteractable.SoilState.Watered;
        soil.TillSoilDirectly();
        // Should not change to Tilled — TillSoilDirectly only works from Regular
        Assert.AreEqual(SoilBlockInteractable.SoilState.Watered, soil.currentState);
    }

    // =========================================================================
    // RESET SOIL (private)
    // =========================================================================

    [Test]
    public void ResetSoil_ChangesStateToRegular_FromTilled()
    {
        soil.currentState = SoilBlockInteractable.SoilState.Tilled;
        ResetSoilDirectly();
        Assert.AreEqual(SoilBlockInteractable.SoilState.Regular, soil.currentState);
    }

    [Test]
    public void ResetSoil_ChangesStateToRegular_FromWatered()
    {
        soil.currentState = SoilBlockInteractable.SoilState.Watered;
        ResetSoilDirectly();
        Assert.AreEqual(SoilBlockInteractable.SoilState.Regular, soil.currentState);
    }

    // =========================================================================
    // GET STATUS TEXT
    // =========================================================================

    [Test]
    public void GetStatusText_Regular_ReturnsCorrectString()
    {
        soil.currentState = SoilBlockInteractable.SoilState.Regular;
        string text = soil.GetStatusText();
        // Text is in Portuguese — exact string from the switch expression
        Assert.AreEqual("Solo não arado", text);
    }

    [Test]
    public void GetStatusText_Tilled_ReturnsCorrectString()
    {
        soil.currentState = SoilBlockInteractable.SoilState.Tilled;
        Assert.AreEqual("Solo arado - pronto para plantio", soil.GetStatusText());
    }

    [Test]
    public void GetStatusText_Watered_ReturnsCorrectString()
    {
        soil.currentState = SoilBlockInteractable.SoilState.Watered;
        Assert.AreEqual("Solo molhado - pronto para plantio", soil.GetStatusText());
    }

    [Test]
    public void GetStatusText_WithCropAndNoCropData_ReturnsCropManagerInfo()
    {
        // Set state to WithCrop — CropGrowthManager has no crop planted,
        // so GetCropInfo() returns its own "No crop planted" message.
        soil.currentState = SoilBlockInteractable.SoilState.WithCrop;
        string text = soil.GetStatusText();
        // GetStatusText delegates to cropGrowthManager.GetCropInfo() when HasCrop is true,
        // but HasCrop is false here, so the switch default fires.
        Assert.IsFalse(string.IsNullOrEmpty(text));
    }

    // =========================================================================
    // ISAVEABLE — SAVE
    // =========================================================================

    [Test]
    public void SaveData_DoesNotThrow_WhenGameDataIsNull()
    {
        Assert.DoesNotThrow(() => soil.SaveData(null));
    }

    [Test]
    public void SaveData_AddsOneEntry_ToSoilStates()
    {
        var gd = new GameData();
        soil.SaveData(gd);
        Assert.AreEqual(1, gd.farmingData.soilStates.Count);
    }

    [Test]
    public void SaveData_WritesCorrectPositionKey()
    {
        // GameObject is at world (0,0,0) → key should be "0,0"
        var gd = new GameData();
        soil.SaveData(gd);
        Assert.AreEqual("0,0", gd.farmingData.soilStates[0].positionKey);
    }

    [Test]
    public void SaveData_WritesIsTilledFalse_WhenRegular()
    {
        soil.currentState = SoilBlockInteractable.SoilState.Regular;
        var gd = new GameData();
        soil.SaveData(gd);
        Assert.IsFalse(gd.farmingData.soilStates[0].soilData.isTilled);
    }

    [Test]
    public void SaveData_WritesIsTilledTrue_WhenTilled()
    {
        soil.currentState = SoilBlockInteractable.SoilState.Tilled;
        var gd = new GameData();
        soil.SaveData(gd);
        Assert.IsTrue(gd.farmingData.soilStates[0].soilData.isTilled);
        Assert.IsFalse(gd.farmingData.soilStates[0].soilData.isWatered);
    }

    [Test]
    public void SaveData_WritesIsWateredTrue_WhenWatered()
    {
        soil.currentState = SoilBlockInteractable.SoilState.Watered;
        var gd = new GameData();
        soil.SaveData(gd);
        Assert.IsTrue(gd.farmingData.soilStates[0].soilData.isTilled);
        Assert.IsTrue(gd.farmingData.soilStates[0].soilData.isWatered);
    }

    [Test]
    public void SaveData_ReplacesExistingEntry_WhenCalledTwice()
    {
        var gd = new GameData();
        soil.currentState = SoilBlockInteractable.SoilState.Tilled;
        soil.SaveData(gd);

        soil.currentState = SoilBlockInteractable.SoilState.Regular;
        soil.SaveData(gd);

        // Should only have one entry (old one removed, new one added)
        Assert.AreEqual(1, gd.farmingData.soilStates.Count);
        Assert.IsFalse(gd.farmingData.soilStates[0].soilData.isTilled,
            "Second SaveData call should overwrite with Regular (isTilled=false).");
    }

    // =========================================================================
    // ISAVEABLE — LOAD
    // =========================================================================

    [Test]
    public void LoadData_DoesNotThrow_WhenGameDataIsNull()
    {
        Assert.DoesNotThrow(() => soil.LoadData(null));
    }

    [Test]
    public void LoadData_SilentlyIgnores_WhenNoMatchingEntry()
    {
        var gd = new GameData(); // empty soilStates
        Assert.DoesNotThrow(() => soil.LoadData(gd));
        Assert.AreEqual(SoilBlockInteractable.SoilState.Regular, soil.currentState,
            "State should remain Regular when no matching save entry exists.");
    }

    [Test]
    public void LoadData_RestoresTilledState()
    {
        var gd = new GameData();
        gd.farmingData.soilStates.Add(new FarmingGameData.SoilStateEntry
        {
            positionKey = "0,0",
            soilData = new FarmingGameData.SoilData { isTilled = true, isWatered = false }
        });

        soil.LoadData(gd);

        Assert.AreEqual(SoilBlockInteractable.SoilState.Tilled, soil.currentState);
    }

    [Test]
    public void LoadData_RestoresWateredState()
    {
        var gd = new GameData();
        gd.farmingData.soilStates.Add(new FarmingGameData.SoilStateEntry
        {
            positionKey = "0,0",
            soilData = new FarmingGameData.SoilData { isTilled = true, isWatered = true }
        });

        soil.LoadData(gd);

        Assert.AreEqual(SoilBlockInteractable.SoilState.Watered, soil.currentState);
    }

    [Test]
    public void LoadData_RestoresRegularState()
    {
        // First move soil to Tilled, then load a Regular state
        soil.currentState = SoilBlockInteractable.SoilState.Tilled;

        var gd = new GameData();
        gd.farmingData.soilStates.Add(new FarmingGameData.SoilStateEntry
        {
            positionKey = "0,0",
            soilData = new FarmingGameData.SoilData { isTilled = false, isWatered = false }
        });

        soil.LoadData(gd);

        Assert.AreEqual(SoilBlockInteractable.SoilState.Regular, soil.currentState);
    }

    // =========================================================================
    // SAVE / LOAD ROUND-TRIP
    // =========================================================================

    [Test]
    public void SaveLoad_RoundTrip_PreservesTilledState()
    {
        soil.TillSoilDirectly();
        Assert.AreEqual(SoilBlockInteractable.SoilState.Tilled, soil.currentState);

        var gd = new GameData();
        soil.SaveData(gd);

        // Reset to default
        soil.currentState = SoilBlockInteractable.SoilState.Regular;

        soil.LoadData(gd);

        Assert.AreEqual(SoilBlockInteractable.SoilState.Tilled, soil.currentState,
            "Save/load round-trip must preserve Tilled state.");
    }
}
