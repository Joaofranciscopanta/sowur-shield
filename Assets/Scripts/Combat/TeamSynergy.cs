using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SowurShield.Animals;

namespace SowurShield.Combat
{

/// <summary>
/// The kinds of synergy a team can have. Used as the localization key suffix
/// ("combat.synergy.flock.name" / ".desc") and as the identity of a synergy in save data.
/// </summary>
public enum SynergyType
{
    Flock,          // 3+ of the same animal family
    MixedYard,      // 3+ different families
    FrontLine,      // 2+ Tanks in the front column
    WellCared,      // every member at 70+ happiness
    WellFed         // every member fed its preferred food
}

/// <summary>
/// One synergy currently active on a team, with the multipliers it grants.
/// </summary>
public class ActiveSynergy
{
    public SynergyType type;

    /// <summary>The family this applies to, for Flock. Empty for team-wide synergies.</summary>
    public string subject = "";

    /// <summary>How many team members qualify — shown in the UI ("Bando ×5").</summary>
    public int count;

    public float attackMultiplier = 1f;
    public float defenseMultiplier = 1f;
    public float speedMultiplier = 1f;
    public float healthMultiplier = 1f;

    /// <summary>Damage taken multiplier, applied to units behind the front column.</summary>
    public float damageTakenMultiplier = 1f;

    /// <summary>True when this synergy only buffs some members rather than the whole team.</summary>
    public bool isPartial;
}

/// <summary>
/// The single source of truth for team synergies.
///
/// This exists because the assembler screen and the battle used to disagree. The panel
/// listed a bonus for stacking the same *species* (reading AnimalData.canStack, which no
/// combat system ever read), while TurnManager granted +10% to 3+ of the same *combat
/// class*. A player could read "no synergies" on a team that was in fact buffed.
///
/// Both now call <see cref="Evaluate"/>, so what the assembler promises is exactly what
/// the battle applies. Family-count synergies are also counted over the assembled team —
/// CombatTeamSpawner used to ask AnimalRoster for the count across the whole farm, which
/// meant a 15-chicken farm had the flock bonuses permanently on no matter who was taken.
/// </summary>
public static class TeamSynergy
{
    // ── Tuning ────────────────────────────────────────────────────────────────
    // These are a starting proposal measured against existing stats, not playtested
    // values. Kept together so they can be retuned in one place.

    public const int FlockMinCount = 3;
    public const int FlockLargeCount = 5;
    private const float FlockAttack = 1.10f;
    private const float FlockAttackLarge = 1.18f;

    public const int MixedYardMinFamilies = 3;
    private const float MixedYardHealth = 1.15f;

    public const int FrontLineMinTanks = 2;
    private const float FrontLineDamageTaken = 0.80f;

    public const float WellCaredMinHappiness = 70f;
    private const float WellCaredSpeed = 1.10f;

    private const float WellFedAttack = 1.10f;

    /// <summary>The combat class that counts as a front-line defender.</summary>
    public const string TankClass = "Tank";

    /// <summary>
    /// A team member reduced to what synergy evaluation needs. Both the assembler
    /// (which has PositionedAnimal) and the battle (which has CombatUnit) can build
    /// this, so neither has to depend on the other's types.
    /// </summary>
    public struct Member
    {
        public string family;
        public string combatClass;
        public float happiness;
        public bool fedPreferredFood;
        public Vector2Int gridPosition;
    }

    /// <summary>
    /// Evaluate every synergy active for this team. Returns an empty list for an empty
    /// team. Order is stable so the UI does not reshuffle between refreshes.
    /// </summary>
    public static List<ActiveSynergy> Evaluate(IReadOnlyList<Member> members)
    {
        var result = new List<ActiveSynergy>();
        if (members == null || members.Count == 0) return result;

        // ── Flock: 3+ of the same family ──────────────────────────────────────
        var families = members
            .Where(m => !string.IsNullOrEmpty(m.family))
            .GroupBy(m => m.family)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key);

        foreach (var group in families)
        {
            int count = group.Count();
            if (count < FlockMinCount) continue;

            result.Add(new ActiveSynergy
            {
                type = SynergyType.Flock,
                subject = group.Key,
                count = count,
                attackMultiplier = count >= FlockLargeCount ? FlockAttackLarge : FlockAttack,
                defenseMultiplier = count >= FlockLargeCount ? FlockAttackLarge : FlockAttack,
                // Only members of this family are buffed, not the whole team.
                isPartial = count < members.Count
            });
        }

        // ── Mixed Yard: 3+ distinct families ──────────────────────────────────
        // The counterweight to Flock. Without it, stacking one family is always
        // the correct answer and team building has no decision in it.
        int distinctFamilies = members
            .Where(m => !string.IsNullOrEmpty(m.family))
            .Select(m => m.family)
            .Distinct()
            .Count();

        if (distinctFamilies >= MixedYardMinFamilies)
        {
            result.Add(new ActiveSynergy
            {
                type = SynergyType.MixedYard,
                count = distinctFamilies,
                healthMultiplier = MixedYardHealth
            });
        }

        // ── Front Line: 2+ Tanks in the frontmost occupied column ─────────────
        // Gives the grid a job. The player's frontmost column is the lowest x they
        // occupy, since enemies come from the left.
        int frontColumn = members.Min(m => m.gridPosition.x);
        int tanksInFront = members.Count(m =>
            m.gridPosition.x == frontColumn &&
            string.Equals(m.combatClass, TankClass, System.StringComparison.OrdinalIgnoreCase));

        if (tanksInFront >= FrontLineMinTanks)
        {
            result.Add(new ActiveSynergy
            {
                type = SynergyType.FrontLine,
                count = tanksInFront,
                damageTakenMultiplier = FrontLineDamageTaken,
                isPartial = true // only units behind the front column benefit
            });
        }

        // ── Well Cared: every member at 70+ happiness ─────────────────────────
        // Ties the farm to the battle through the stat the game already simulates
        // daily and which so far only multiplied damage silently.
        if (members.All(m => m.happiness >= WellCaredMinHappiness))
        {
            result.Add(new ActiveSynergy
            {
                type = SynergyType.WellCared,
                count = members.Count,
                speedMultiplier = WellCaredSpeed
            });
        }

        // ── Well Fed: every member ate its preferred food ─────────────────────
        if (members.All(m => m.fedPreferredFood))
        {
            result.Add(new ActiveSynergy
            {
                type = SynergyType.WellFed,
                count = members.Count,
                attackMultiplier = WellFedAttack
            });
        }

        return result;
    }

    /// <summary>
    /// Does this synergy apply to this particular member? Team-wide synergies apply to
    /// everyone; Flock only to its own family; FrontLine only to units behind the front.
    /// </summary>
    public static bool AppliesTo(ActiveSynergy synergy, Member member, int frontColumn)
    {
        if (synergy == null) return false;

        switch (synergy.type)
        {
            case SynergyType.Flock:
                return string.Equals(member.family, synergy.subject,
                    System.StringComparison.OrdinalIgnoreCase);

            case SynergyType.FrontLine:
                // The shield protects the ranks behind it, not itself.
                return member.gridPosition.x > frontColumn;

            default:
                return true;
        }
    }

    /// <summary>Localization key for a synergy's display name.</summary>
    public static string NameKey(SynergyType type) =>
        $"combat.synergy.{type.ToString().ToLowerInvariant()}.name";

    /// <summary>Localization key for a synergy's description.</summary>
    public static string DescriptionKey(SynergyType type) =>
        $"combat.synergy.{type.ToString().ToLowerInvariant()}.desc";

    /// <summary>
    /// English fallback name, used when the localization tables have not loaded yet
    /// (Awake runs before they do) or when a key is missing.
    /// </summary>
    public static string FallbackName(SynergyType type)
    {
        switch (type)
        {
            case SynergyType.Flock:     return "Flock";
            case SynergyType.MixedYard: return "Mixed Yard";
            case SynergyType.FrontLine: return "Front Line";
            case SynergyType.WellCared: return "Well Cared For";
            case SynergyType.WellFed:   return "Well Fed";
            default: return type.ToString();
        }
    }

    /// <summary>
    /// Short "what it does" line, used as the fallback description and in tooltips.
    /// </summary>
    public static string FallbackDescription(ActiveSynergy synergy)
    {
        if (synergy == null) return "";

        switch (synergy.type)
        {
            case SynergyType.Flock:
                return synergy.count >= FlockLargeCount
                    ? "+18% attack and defense to this family"
                    : "+10% attack and defense to this family";
            case SynergyType.MixedYard:
                return "+15% health to the whole team";
            case SynergyType.FrontLine:
                return "-20% damage taken behind the front line";
            case SynergyType.WellCared:
                return "+10% speed to the whole team";
            case SynergyType.WellFed:
                return "+10% attack to the whole team";
            default:
                return "";
        }
    }

    /// <summary>
    /// Build the evaluation input from an assembled team. Lives here so the assembler
    /// and the spawner cannot drift apart in how they read the same data.
    /// </summary>
    public static List<Member> BuildMembers(IEnumerable<TeamAssemblerData.PositionedAnimal> team)
    {
        var members = new List<Member>();
        if (team == null) return members;

        foreach (var pa in team)
        {
            if (pa?.animalData == null) continue;

            members.Add(new Member
            {
                family = pa.animalData.animalFamily,
                combatClass = pa.animalData.combatClass,
                happiness = pa.happiness,
                fedPreferredFood = pa.fedPreferredFood,
                gridPosition = pa.gridPosition
            });
        }

        return members;
    }
}

} // namespace SowurShield.Combat
