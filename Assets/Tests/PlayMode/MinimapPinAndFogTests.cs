using System.Collections;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SowurShield.Core;
using SowurShield.Minimap;

/// <summary>
/// Play Mode tests for the player-placed map pins and the fog-of-war mask.
///
/// Both persist into the save file, and both had a failure mode that only shows up on some
/// machines or after a reload — the kind that is invisible in a single manual playthrough.
/// </summary>
public class MinimapPinAndFogTests
{
    private GameObject host;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("MinimapPinAndFogTestHost");
    }

    [TearDown]
    public void TearDown()
    {
        if (host != null)
            Object.DestroyImmediate(host);
    }

    // ============================================================================
    // PINS
    // ============================================================================

    [UnityTest]
    public IEnumerator Pins_SurviveASaveLoadRoundTrip_WithFractionalCoordinates()
    {
        var pins = host.AddComponent<MinimapPinManager>();
        yield return null; // let Start run

        pins.AddPin(new Vector2(1.25f, -3.75f));
        pins.AddPin(new Vector2(-0.5f, 12.125f));
        Assert.AreEqual(2, pins.PinCount);

        var data = new GameData();
        pins.SaveData(data);

        pins.ClearAllPins();
        Assert.AreEqual(0, pins.PinCount, "ClearAllPins should leave nothing behind");

        pins.LoadData(data);

        Assert.AreEqual(2, pins.PinCount, "both pins should come back");
    }

    /// <summary>
    /// The separator is a comma and so is this machine's decimal separator (pt-BR). Serialising a
    /// pin at x=1.25 with the current culture writes "1,25", which the reader then splits into two
    /// coordinates — the save corrupts on some machines and not others. Pinning the culture inside
    /// the test proves the writer does not depend on the ambient one.
    /// </summary>
    [UnityTest]
    public IEnumerator Pins_RoundTripUnderACommaDecimalCulture()
    {
        var original = Thread.CurrentThread.CurrentCulture;

        var pins = host.AddComponent<MinimapPinManager>();
        yield return null;

        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-BR");

            pins.AddPin(new Vector2(1.5f, -2.25f));

            var data = new GameData();
            pins.SaveData(data);

            string raw = data.worldData.worldStrings["minimap_pins"];
            Assert.IsFalse(raw.Contains("1,5"),
                $"coordinates must not be written with a comma decimal separator, got '{raw}'");

            pins.ClearAllPins();
            pins.LoadData(data);

            Assert.AreEqual(1, pins.PinCount, "the pin must survive a comma-decimal culture");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [UnityTest]
    public IEnumerator TogglePin_RemovesANearbyPinInsteadOfStacking()
    {
        var pins = host.AddComponent<MinimapPinManager>();
        yield return null;

        pins.AddPin(new Vector2(5f, 5f));
        Assert.AreEqual(1, pins.PinCount);

        // Within removeRadius (1.2 by default) — should remove, not add a second.
        pins.TogglePinAt(new Vector2(5.3f, 5.1f));
        Assert.AreEqual(0, pins.PinCount, "clicking an existing pin should remove it");

        // Far away — should place a new one.
        pins.TogglePinAt(new Vector2(20f, -20f));
        Assert.AreEqual(1, pins.PinCount, "clicking empty map should place a pin");
    }

    /// <summary>
    /// LoadData clears and immediately rebuilds. With a deferred Destroy the old pin GameObjects
    /// outlive the clear and sit on the map beside the freshly loaded ones.
    /// </summary>
    [UnityTest]
    public IEnumerator LoadingPins_DoesNotLeaveTheOldPinObjectsBehind()
    {
        var pins = host.AddComponent<MinimapPinManager>();
        yield return null;

        pins.AddPin(new Vector2(1f, 1f));
        pins.AddPin(new Vector2(2f, 2f));

        var data = new GameData();
        pins.SaveData(data);

        pins.LoadData(data); // clear + rebuild in one call

        Assert.AreEqual(2, pins.PinCount);

        int liveObjects = 0;
        foreach (Transform child in host.transform)
        {
            if (child.name.StartsWith("MinimapPin_")) liveObjects++;
        }

        Assert.AreEqual(2, liveObjects,
            "a deferred Destroy would leave the pre-load pin objects alive alongside the new ones");
    }

    [UnityTest]
    public IEnumerator Pins_AreCappedSoAStuckInputCannotFillTheSave()
    {
        var pins = host.AddComponent<MinimapPinManager>();
        yield return null;

        for (int i = 0; i < 200; i++)
            pins.AddPin(new Vector2(i * 3f, 0f));

        Assert.LessOrEqual(pins.PinCount, 64, "pin count must respect maxPins");
    }

    [UnityTest]
    public IEnumerator LoadingASaveWithNoPins_ClearsExistingOnes()
    {
        var pins = host.AddComponent<MinimapPinManager>();
        yield return null;

        pins.AddPin(new Vector2(4f, 4f));
        Assert.AreEqual(1, pins.PinCount);

        // A save from a slot where the player never placed a pin.
        pins.LoadData(new GameData());

        Assert.AreEqual(0, pins.PinCount,
            "pins from the previous slot must not leak into one that has none");
    }

    // ============================================================================
    // FOG OF WAR
    // ============================================================================

    [UnityTest]
    public IEnumerator Fog_RevealIsARatchet_AndNeverRefogsClearedGround()
    {
        var fog = host.AddComponent<MinimapFogOfWar>();
        yield return null; // Start
        yield return null; // deferred Build

        fog.RevealAll();
        float afterRevealAll = fog.GetExploredFraction();
        Assert.Greater(afterRevealAll, 0.99f, "RevealAll should clear the whole mask");

        // Revealing a small circle must not reduce what is already explored.
        fog.RevealAround(Vector3.zero);
        Assert.GreaterOrEqual(fog.GetExploredFraction(), afterRevealAll,
            "revealing must never lower the explored fraction");
    }

    [UnityTest]
    public IEnumerator Fog_MaskSurvivesASaveLoadRoundTrip()
    {
        var fog = host.AddComponent<MinimapFogOfWar>();
        yield return null;
        yield return null;

        fog.RevealAll();
        float explored = fog.GetExploredFraction();

        var data = new GameData();
        fog.SaveData(data);

        Assert.IsTrue(data.worldData.worldStrings.ContainsKey("minimap_fog_mask"),
            "the fog mask must be written to the save");

        // A fresh instance, as if the game had been reloaded.
        var reloadHost = new GameObject("ReloadedFogHost");
        try
        {
            var reloaded = reloadHost.AddComponent<MinimapFogOfWar>();
            yield return null;
            yield return null;

            reloaded.LoadData(data);

            Assert.AreEqual(explored, reloaded.GetExploredFraction(), 0.02f,
                "a reload must not re-fog ground the player had already cleared");
        }
        finally
        {
            Object.DestroyImmediate(reloadHost);
        }
    }

    [UnityTest]
    public IEnumerator Fog_IgnoresAMaskSavedAtADifferentResolution()
    {
        var fog = host.AddComponent<MinimapFogOfWar>();
        yield return null;
        yield return null;

        fog.RevealAll();

        var data = new GameData();
        fog.SaveData(data);

        // Simulate the mask resolution having been changed since the save was written.
        data.worldData.worldCounters["minimap_fog_res"] = 999;

        // Must not throw, and must not apply a mismatched mask.
        Assert.DoesNotThrow(() => fog.LoadData(data));
    }

    [UnityTest]
    public IEnumerator Fog_SurvivesACorruptMaskString()
    {
        var fog = host.AddComponent<MinimapFogOfWar>();
        yield return null;
        yield return null;

        var data = new GameData();
        data.worldData.worldStrings["minimap_fog_mask"] = "not valid base64 !!!";
        data.worldData.worldCounters["minimap_fog_res"] = 128;

        Assert.DoesNotThrow(() => fog.LoadData(data),
            "a corrupt save must not take the game down");
    }
}
