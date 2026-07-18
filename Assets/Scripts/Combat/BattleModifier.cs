using UnityEngine.Localization;
using SowurShield.Core;

namespace SowurShield.Combat
{

/// <summary>
/// Random per-battle modifiers that add variety to encounters.
/// </summary>
public enum BattleModifierType
{
    /// <summary>No modifier — a normal battle.</summary>
    None,

    /// <summary>All units' turn gauges fill twice as fast (faster-paced battle).</summary>
    DoubleSpeed,

    /// <summary>All accuracy rolls are reduced by a flat fraction (more misses).</summary>
    LowVisibility,

    /// <summary>All units are healed a small flat amount at the start of each round.</summary>
    HealingRain,

    /// <summary>All damage dealt and received is doubled (high-risk, high-reward).</summary>
    GlassCannon,
}

/// <summary>
/// A single active battle modifier, applied for the duration of a battle.
/// </summary>
[System.Serializable]
public class BattleModifier
{
    public BattleModifierType type;

    public BattleModifier(BattleModifierType type)
    {
        this.type = type;
    }

    /// <summary>Player-facing description, resolved from the Combat table by modifier type.</summary>
    public string GetDescription()
    {
        return new LocalizedString("Combat", $"combat.modifier.{type}.description").SafeGetLocalizedString();
    }

    /// <summary>Flat accuracy reduction applied by LowVisibility.</summary>
    public const float LowVisibilityAccuracyPenalty = 0.2f;

    /// <summary>Flat heal amount applied to all units each round by HealingRain.</summary>
    public const float HealingRainAmount = 5f;

    /// <summary>Damage multiplier (both dealt and received) applied by GlassCannon.</summary>
    public const float GlassCannonMultiplier = 2f;
}

} // namespace SowurShield.Combat
