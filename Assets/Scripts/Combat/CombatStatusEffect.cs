namespace SowurShield.Combat
{

/// <summary>
/// Types of status effects that can be applied during combat.
/// </summary>
public enum StatusEffectType
{
    Stun,     // Unit skips its next turn
    Shield,   // Reduces incoming damage by value% for duration turns
    Burn,     // Deals value damage at the start of each turn for duration turns
    Poison,   // Deals value damage at the start of each turn for duration turns; stacks independently
    Weakness, // Reduces attack/defense by value% for duration turns
}

/// <summary>
/// A single active status effect on a CombatUnit.
/// Managed by CombatUnit.ApplyStatusEffect / TickStatusEffects.
/// </summary>
[System.Serializable]
public class CombatStatusEffect
{
    public StatusEffectType type;
    /// <summary>
    /// Burn: damage per turn.
    /// Shield: damage reduction fraction (0–1, e.g. 0.3 = 30% reduction).
    /// Stun: unused.
    /// </summary>
    public float value;
    public int turnsRemaining;

    public CombatStatusEffect(StatusEffectType type, float value, int turns)
    {
        this.type = type;
        this.value = value;
        this.turnsRemaining = turns;
    }
}

} // namespace SowurShield.Combat
