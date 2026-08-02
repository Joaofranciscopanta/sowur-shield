using UnityEngine;
using System.Collections.Generic;

namespace SowurShield.Combat
{

/// <summary>
/// Tints the units that can currently be clicked as a target during active-pause
/// target selection, and restores their original colours afterwards.
///
/// Static because only one selection can be open at a time — the battle is frozen
/// while the player chooses.
/// </summary>
public static class TargetHighlighter
{
    /// <summary>Tint applied to selectable targets.</summary>
    private static readonly Color HighlightTint = new Color(1f, 0.55f, 0.55f);

    // Original colour per renderer, so highlighting is fully reversible even if the
    // sprite already had a tint (flash effects, status colouring).
    private static readonly Dictionary<SpriteRenderer, Color> originalColors =
        new Dictionary<SpriteRenderer, Color>();

    /// <summary>Highlight every given unit as a selectable target.</summary>
    public static void HighlightAll(IEnumerable<CombatUnit> units)
    {
        ClearAll();
        if (units == null) return;

        foreach (CombatUnit unit in units)
        {
            if (unit == null) continue;

            foreach (SpriteRenderer sr in unit.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr == null || originalColors.ContainsKey(sr)) continue;
                originalColors[sr] = sr.color;
                sr.color = HighlightTint;
            }
        }
    }

    /// <summary>Restore every highlighted renderer to the colour it had before.</summary>
    public static void ClearAll()
    {
        foreach (var pair in originalColors)
        {
            // The unit can be destroyed while highlighted (killed mid-selection).
            if (pair.Key != null)
                pair.Key.color = pair.Value;
        }
        originalColors.Clear();
    }
}

}
