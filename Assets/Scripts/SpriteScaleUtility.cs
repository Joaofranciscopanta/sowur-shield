using UnityEngine;

namespace SowurShield.Core
{

/// <summary>
/// Sprites in this project come from asset packs with wildly different native pixel sizes
/// (character sheets, UI icons, enemy art). Anything that spawns a bare GameObject + SpriteRenderer
/// at runtime needs to normalize scale by the sprite's own size, or objects end up wrong-sized
/// relative to hand-placed/hand-tuned scene content. Centralizes the two normalization strategies
/// already used independently across Combat and Animals/Core before this was pulled out.
/// </summary>
public static class SpriteScaleUtility
{
    /// <summary>Uniform scale so the sprite's height equals targetHeight world units.</summary>
    public static float GetScaleForTargetHeight(Sprite sprite, float targetHeight)
    {
        if (sprite == null) return 1f;
        float height = sprite.bounds.size.y;
        return height > 0f ? targetHeight / height : 1f;
    }

    /// <summary>Uniform scale so the sprite's larger dimension equals targetSize world units.</summary>
    public static float GetScaleForTargetMaxDimension(Sprite sprite, float targetSize)
    {
        if (sprite == null) return 1f;
        float maxDim = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        return maxDim > 0f ? targetSize / maxDim : 1f;
    }
}

} // namespace SowurShield.Core
