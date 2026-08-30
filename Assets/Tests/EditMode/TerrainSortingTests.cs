using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using SowurShield.Core;

namespace SowurShield.Tests
{

/// <summary>
/// Guards the fix for the player being drawn underneath the ground.
///
/// <para>The ground tilemaps shipped on the Default sorting layer at orders 0 and -1, the same
/// layer as every character and prop. That was harmless while sprites used hand-picked orders
/// of 1 to 5, because all of them beat 0. The moment <see cref="YSortSprite"/> started deriving
/// sortingOrder from world position, everything north of the map origin took a negative order
/// and fell behind the floor -- 47 of 87 sprites, the player among them, who disappeared
/// outright when walking up the map.</para>
///
/// <para>What makes it worth a test is that every check the project had passed. The sprites
/// sorted correctly against each other, nothing was off-screen, no rect was zero-sized and no
/// warning was logged. Only comparing sprites against the *terrain* catches it.</para>
/// </summary>
public class TerrainSortingTests
{
    private const string GroundLayerName = "Ground";

    [Test]
    public void GroundLayer_SortsBelowDefault()
    {
        // The whole fix rests on this ordering. If someone renumbers the sorting layers so
        // Ground is no longer below Default, the terrain silently starts covering things again.
        Assert.IsTrue(SortingLayer.layers.Any(l => l.name == GroundLayerName),
            $"The '{GroundLayerName}' sorting layer is missing. It is what keeps the terrain " +
            "underneath everything standing on it.");

        int ground = SortingLayer.GetLayerValueFromName(GroundLayerName);
        int def = SortingLayer.GetLayerValueFromName("Default");

        Assert.Less(ground, def,
            $"'{GroundLayerName}' ({ground}) must sort below 'Default' ({def}), or a sprite " +
            "with a negative Y-derived order is drawn under the floor.");
    }

    [Test]
    public void YSortSprite_ProducesNegativeOrders_WhichIsWhyTerrainNeedsItsOwnLayer()
    {
        // This is the mechanism that broke the terrain, pinned down so the reason the Ground
        // layer exists stays visible: a sprite above the origin genuinely does get an order
        // below the tilemap's 0.
        var go = new GameObject("ysort_probe", typeof(SpriteRenderer));
        try
        {
            go.transform.position = new Vector3(0f, 10f, 0f);
            var sorter = go.AddComponent<YSortSprite>();
            sorter.Apply();

            int order = go.GetComponent<SpriteRenderer>().sortingOrder;

            Assert.Less(order, 0,
                "A sprite north of the origin should take a negative sorting order. If this " +
                "ever stops being true the Ground layer may look unnecessary -- it is not; " +
                "it is what stops that negative order burying the sprite.");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void TilemapRenderer_DefaultsAreNotSafeForYSorting()
    {
        // A fresh TilemapRenderer lands on Default at order 0, which is exactly the
        // configuration that caused the bug. Anyone adding a new ground tilemap has to move it
        // to the Ground layer, and this states that plainly.
        var go = new GameObject("tilemap_probe", typeof(Grid));
        try
        {
            var child = new GameObject("probe_map", typeof(Tilemap), typeof(TilemapRenderer));
            child.transform.SetParent(go.transform, false);
            var renderer = child.GetComponent<TilemapRenderer>();

            int ground = SortingLayer.GetLayerValueFromName(GroundLayerName);
            int actual = SortingLayer.GetLayerValueFromName(renderer.sortingLayerName);

            Assert.AreNotEqual(ground, actual,
                "A new TilemapRenderer is expected to start off the Ground layer; if Unity " +
                "ever changes that default this test can simply be removed.");

            // The point of the assertion above is the comment below it: a new ground tilemap
            // is NOT safe as it comes, and must be run through
            // Sowur Shield > Rendering > Assign Terrain Sorting Layers.
            Assert.AreEqual(0, renderer.sortingOrder,
                "Default order is 0 -- the same value that let Y-sorted sprites fall behind it.");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}

}
