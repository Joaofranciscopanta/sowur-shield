namespace SowurShield.Combat
{

/// <summary>
/// How the player's units are controlled during a battle. Chosen in the team
/// assembler before the battle starts and carried into CombatScene on
/// <see cref="TeamAssemblerData.combatMode"/>.
/// </summary>
public enum CombatMode
{
    /// <summary>
    /// The battle freezes when one of the player's animals fills its turn gauge and waits
    /// for the player to pick an action for it. Enemies still act automatically.
    /// </summary>
    ActivePause = 0,

    /// <summary>
    /// The AI picks targets and skills for the player's animals too — no input required.
    /// This is the behaviour the game shipped with, and the default in tests.
    /// </summary>
    Auto = 1
}

}
