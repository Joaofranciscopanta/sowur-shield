using System.Collections.Generic;
using SowurShield.Combat;
using SowurShield.Inventory;

namespace SowurShield.Combat
{

/// <summary>
/// Data container carrying computed reward info from TurnManager to BattleResultsUI.
/// Computed by TurnManager.EndBattle(), consumed by BattleResultsUI.AwardRewards().
/// </summary>
[System.Serializable]
public class CombatRewardData
{
    public bool isVictory;
    public int goldReward;
    public int xpReward;
    public List<(Item item, int quantity)> lootDrops = new List<(Item, int)>();
    public float animalHappinessBonus;
    public List<CombatUnit> survivingPlayerUnits = new List<CombatUnit>();
}

} // namespace SowurShield.Combat
