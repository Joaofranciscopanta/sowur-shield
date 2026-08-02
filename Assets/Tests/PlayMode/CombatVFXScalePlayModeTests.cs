using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using SowurShield.Combat;

/// <summary>
/// Play Mode regression tests for floating combat text on scaled units.
///
/// Player units are mirrored to face left (localScale.x = -5) while enemy sprites sit
/// around 0.08. Damage numbers and status icons are children of the unit, so they used to
/// inherit that transform and render mirrored and 5x oversized on player units — a bug
/// that only shows up on screen, never in a unit test that checks damage values.
/// </summary>
public class CombatVFXScalePlayModeTests
{
    private readonly List<Object> _cleanup = new List<Object>();

    private T Track<T>(T obj) where T : Object
    {
        _cleanup.Add(obj);
        return obj;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var obj in _cleanup)
            if (obj != null) Object.Destroy(obj);
        _cleanup.Clear();
        yield return null;
    }

    /// <summary>Build a unit whose transform is mirrored and scaled, like a real player unit.</summary>
    private CombatUnit CreateScaledUnit(Vector3 localScale)
    {
        var go = Track(new GameObject("ScaledUnit"));
        go.transform.localScale = localScale;

        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = true;
        unit.InitializeAsEnemy("ScaledUnit", 1000f, 10f, 0f, 10f);
        go.AddComponent<CombatUnitVFX>();
        return unit;
    }

    private static TextMeshPro FindFloatingText(CombatUnit unit)
    {
        foreach (var tmp in unit.GetComponentsInChildren<TextMeshPro>(true))
            if (tmp.gameObject.name == "FloatingText")
                return tmp;
        return null;
    }

    [UnityTest]
    public IEnumerator FloatingDamageText_IsNotMirroredOnAMirroredUnit()
    {
        var unit = CreateScaledUnit(new Vector3(-5f, 5f, 5f));
        yield return null; // let CombatUnitVFX.Awake subscribe

        unit.TakeDamage(50f);
        yield return null;

        var text = FindFloatingText(unit);
        Assert.IsNotNull(text, "Taking damage should spawn a FloatingText child.");
        Assert.Greater(text.transform.lossyScale.x, 0f,
            "Floating damage text must not inherit the unit's negative X scale, or the " +
            "number renders back-to-front on screen.");
    }

    [UnityTest]
    public IEnumerator FloatingDamageText_IsUnitScaleRegardlessOfParentScale()
    {
        var big = CreateScaledUnit(new Vector3(-5f, 5f, 5f));
        var small = CreateScaledUnit(new Vector3(0.08f, 0.08f, 0.08f));
        yield return null;

        big.TakeDamage(50f);
        small.TakeDamage(50f);
        yield return null;

        var bigText = FindFloatingText(big);
        var smallText = FindFloatingText(small);
        Assert.IsNotNull(bigText);
        Assert.IsNotNull(smallText);

        // Both teams must render damage numbers at the same on-screen size.
        Assert.AreEqual(1f, Mathf.Abs(bigText.transform.lossyScale.x), 0.01f,
            "Text on a 5x unit should end up at world scale 1.");
        Assert.AreEqual(1f, Mathf.Abs(smallText.transform.lossyScale.x), 0.01f,
            "Text on a 0.08x unit should also end up at world scale 1.");
    }

    [UnityTest]
    public IEnumerator StatusIcon_IsNotMirroredOnAMirroredUnit()
    {
        var unit = CreateScaledUnit(new Vector3(-5f, 5f, 5f));
        yield return null;

        unit.ApplyStatusEffect(StatusEffectType.Poison, 5f, 3);
        yield return null;

        TextMeshPro icon = null;
        foreach (var tmp in unit.GetComponentsInChildren<TextMeshPro>(true))
            if (tmp.gameObject.name.StartsWith("StatusIcon_"))
                icon = tmp;

        Assert.IsNotNull(icon, "Applying a status effect should spawn a status icon child.");
        Assert.Greater(icon.transform.lossyScale.x, 0f,
            "Status icons must not inherit the unit's negative X scale.");
    }

    [UnityTest]
    public IEnumerator FloatingText_SitsAboveTheUnitInWorldSpace()
    {
        var unit = CreateScaledUnit(new Vector3(-5f, 5f, 5f));
        yield return null;

        unit.TakeDamage(50f);
        yield return null;

        var text = FindFloatingText(unit);
        Assert.IsNotNull(text);
        Assert.Greater(text.transform.position.y, unit.transform.position.y,
            "Damage numbers should appear above the unit, not below or on top of it.");
    }
}
