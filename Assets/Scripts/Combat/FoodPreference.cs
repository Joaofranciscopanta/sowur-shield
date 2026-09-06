using System.Collections.Generic;
using SowurShield.Animals;

namespace SowurShield.Combat
{

/// <summary>
/// Which crop each animal family actually likes.
///
/// Before this, 26 of the 28 AnimalData assets asked for one CarrotSeed — cows included —
/// while the farm grows six crops. Feeding was a toll to pay, not a choice to make: the
/// battle button stayed greyed out until you clicked "Feed All", and nothing about which
/// food you used ever mattered.
///
/// Now any food keeps an animal at normal strength, and its *preferred* food earns a bonus
/// and counts toward the Well Fed synergy. The requirement stays at one item per animal
/// deliberately — with 15 grid slots, asking for two or three would turn every battle into
/// a pantry calculation.
///
/// Kept as a lookup here rather than a field on AnimalData so the 28 assets don't all have
/// to be re-edited, and so the mapping is readable in one place.
/// </summary>
public static class FoodPreference
{
    /// <summary>Family name (as in AnimalData.animalFamily) → preferred item name.</summary>
    private static readonly Dictionary<string, string> PreferredByFamily =
        new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
    {
        // Bulky feed for the bulky animals — pairs with their Tank role.
        { "Bovidae",     "Cabbage" },
        // What chickens already ate; kept so the common case needs no new farming.
        { "Galliformes", "CarrotSeed" },
        { "Anatidae",    "Radish" },
        { "Leporidae",   "Carrot" },
        { "Passeridae",  "PumpkinSeed" },
    };

    /// <summary>
    /// The item this animal prefers, or empty when its family has no preference set.
    /// </summary>
    public static string GetPreferredFood(AnimalData data)
    {
        if (data == null || string.IsNullOrEmpty(data.animalFamily)) return "";

        return PreferredByFamily.TryGetValue(data.animalFamily, out string item) ? item : "";
    }

    /// <summary>Is this item the one the animal prefers?</summary>
    public static bool IsPreferred(AnimalData data, string itemName)
    {
        string preferred = GetPreferredFood(data);
        return !string.IsNullOrEmpty(preferred) &&
               string.Equals(preferred, itemName, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Stat multiplier for an animal that goes into battle without eating.
    ///
    /// This replaces the hard block on starting a battle. An unfed animal still fights;
    /// it just fights worse, which is a cost the player can choose to accept.
    /// </summary>
    public const float UnfedStatPenalty = 0.75f;
}

} // namespace SowurShield.Combat
