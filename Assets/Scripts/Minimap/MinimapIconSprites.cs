using System.Collections.Generic;
using UnityEngine;

namespace SowurShield.Minimap
{

/// <summary>
/// Procedurally drawn marker sprites for the minimap, cached per shape.
///
/// The minimap deliberately does not reuse world art. At the HUD size (a 200px panel showing a
/// 32-unit span) a detailed character or building sprite reduces to a few muddy pixels — the
/// first render of the working minimap looked like scattered confetti precisely because it was
/// the game's own sprites shrunk. Markers need silhouette, not detail: a handful of high-contrast
/// shapes that stay identifiable at ~8px.
///
/// Drawn in code rather than authored as assets for the same reason the stamina bolt was: it is
/// simple geometry, and AI image generation is unconfigured in this project.
///
/// Every shape carries a dark outline. Without one, a marker vanishes wherever the terrain
/// underneath happens to match its colour — the farm is almost entirely one shade of green, so a
/// green player marker on green grass was invisible in exactly the place it mattered most.
/// </summary>
public static class MinimapIconSprites
{
    private const int Resolution = 32;
    private const float PixelsPerUnit = 32f;

    private static readonly Dictionary<Shape, Sprite> Cache = new Dictionary<Shape, Sprite>();

    private enum Shape { Chevron, Square, Diamond, Dot, Star }

    /// <summary>White shapes, tinted per icon by the renderer's `iconColor`.</summary>
    public static Sprite ForType(MinimapIconType type)
    {
        return Get(ShapeFor(type));
    }

    private static Shape ShapeFor(MinimapIconType type)
    {
        switch (type)
        {
            case MinimapIconType.Player:      return Shape.Chevron;
            case MinimapIconType.NPC:         return Shape.Diamond;
            case MinimapIconType.Enemy:       return Shape.Diamond;
            case MinimapIconType.SellBox:     return Shape.Square;
            case MinimapIconType.Bed:         return Shape.Square;
            case MinimapIconType.Building:    return Shape.Square;
            case MinimapIconType.CropField:   return Shape.Square;
            case MinimapIconType.Quest:       return Shape.Star;
            case MinimapIconType.Waypoint:    return Shape.Star;
            case MinimapIconType.Collectible: return Shape.Dot;
            default:                          return Shape.Dot;
        }
    }

    private static Sprite Get(Shape shape)
    {
        Sprite cached;
        if (Cache.TryGetValue(shape, out cached) && cached != null)
            return cached;

        Sprite created = Draw(shape);
        Cache[shape] = created;
        return created;
    }

    private static Sprite Draw(Shape shape)
    {
        var tex = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.name = "MinimapIcon_" + shape;

        var clear = new Color(0f, 0f, 0f, 0f);
        var fill = Color.white;
        var outline = new Color(0.08f, 0.07f, 0.05f, 1f);

        // Two passes: mark the shape's interior, then darken the pixels on its rim. Doing the
        // outline as a separate pass keeps each shape's maths to "is this point inside".
        var inside = new bool[Resolution, Resolution];
        for (int y = 0; y < Resolution; y++)
            for (int x = 0; x < Resolution; x++)
                inside[x, y] = IsInside(shape, x, y);

        for (int y = 0; y < Resolution; y++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                if (!inside[x, y]) { tex.SetPixel(x, y, clear); continue; }
                tex.SetPixel(x, y, IsRim(inside, x, y) ? outline : fill);
            }
        }

        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, Resolution, Resolution),
                             new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    /// <summary>A filled pixel with at least one empty (or off-texture) neighbour is on the rim.</summary>
    private static bool IsRim(bool[,] inside, int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= Resolution || ny >= Resolution) return true;
                if (!inside[nx, ny]) return true;
            }
        }
        return false;
    }

    private static bool IsInside(Shape shape, int px, int py)
    {
        // Normalised to -1..1 with the centre at 0.
        float x = (px + 0.5f) / Resolution * 2f - 1f;
        float y = (py + 0.5f) / Resolution * 2f - 1f;

        switch (shape)
        {
            case Shape.Square:
                return Mathf.Abs(x) <= 0.72f && Mathf.Abs(y) <= 0.72f;

            case Shape.Diamond:
                return Mathf.Abs(x) + Mathf.Abs(y) <= 0.92f;

            case Shape.Dot:
                return (x * x + y * y) <= 0.62f * 0.62f;

            case Shape.Chevron:
            {
                // An upward arrowhead: outer triangle minus a smaller triangle bitten out of the
                // base, which is what turns a plain triangle into an arrow.
                //
                // The first version computed the notch as a lerp against the local half-width,
                // so near x=0 the cut reached highest and hollowed out the arrow's *middle* —
                // it rendered as a huge open "V" instead of a marker. Two explicit triangles are
                // both correct and far easier to reason about.
                const float tipY = 0.80f, baseY = -0.70f, halfBase = 0.72f;

                if (y > tipY || y < baseY) return false;

                // Outer triangle: full width at the base, converging to a point at the tip.
                float spanOuter = Mathf.Lerp(halfBase, 0f, Mathf.InverseLerp(baseY, tipY, y));
                if (Mathf.Abs(x) > spanOuter) return false;

                // Notch: a triangle rising from the base to 45% of the height, removed.
                const float notchTopY = -0.05f;
                if (y <= notchTopY)
                {
                    float spanNotch = Mathf.Lerp(halfBase * 0.62f, 0f,
                                                 Mathf.InverseLerp(baseY, notchTopY, y));
                    if (Mathf.Abs(x) < spanNotch) return false;
                }

                return true;
            }

            case Shape.Star:
            {
                // Four-pointed sparkle — distinct from the diamond at small sizes because the
                // edges pinch inward rather than running straight.
                float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
                return Mathf.Pow(ax, 0.55f) + Mathf.Pow(ay, 0.55f) <= 1.05f;
            }
        }
        return false;
    }
}

} // namespace SowurShield.Minimap
