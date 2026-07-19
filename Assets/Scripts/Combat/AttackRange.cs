namespace SowurShield.Combat
{

/// <summary>
/// Positional attack range classification for combat units.
/// </summary>
public enum AttackRange
{
    /// <summary>Can target any column on the opposing side (default, preserves existing behavior).</summary>
    Ranged,

    /// <summary>Can only target the opposing side's front column, unless it is empty.</summary>
    Melee,
}

} // namespace SowurShield.Combat
