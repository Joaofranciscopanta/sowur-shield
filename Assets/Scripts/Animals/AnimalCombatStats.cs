using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Combat statistics for animals used in autobattler combat scenes.
/// Stats are influenced by farming activities (petting, feeding, production).
/// </summary>
[System.Serializable]
public class AnimalCombatStats
{
    [Header("Base Combat Stats")]
    [Tooltip("Base attack damage")]
    public float baseAttack = 10f;

    [Tooltip("Base defense (damage reduction)")]
    public float baseDefense = 10f;

    [Tooltip("Base speed (turn order priority)")]
    public float baseSpeed = 10f;

    [Tooltip("Base accuracy (hit chance, 0-1)")]
    [Range(0f, 1f)]
    public float baseAccuracy = 1.0f; // 100% hit rate by default

    [Tooltip("Base maximum health")]
    public float baseHealth = 100f;

    [Header("Current State")]
    [Tooltip("Current health in combat")]
    public float currentHealth = 100f;

    [Tooltip("Current level (for future leveling system)")]
    public int level = 1;

    [Tooltip("Current experience (for future leveling system)")]
    public float experience = 0f;

    [Header("Growth Modifiers")]
    [Tooltip("Attack growth multiplier (1.0 = base, 2.0 = double)")]
    public float attackGrowth = 1f;

    [Tooltip("Defense growth multiplier")]
    public float defenseGrowth = 1f;

    [Tooltip("Speed growth multiplier")]
    public float speedGrowth = 1f;

    [Tooltip("Health growth multiplier")]
    public float healthGrowth = 1f;

    [Header("Happiness System")]
    [Tooltip("Current happiness level (0-100)")]
    [Range(0f, 100f)]
    public float happiness = 50f;

    [Header("Seasonal Bonuses")]
    [Tooltip("Current seasonal attack modifier")]
    public float seasonalAttackMod = 1f;

    [Tooltip("Current seasonal defense modifier")]
    public float seasonalDefenseMod = 1f;

    [Tooltip("Current seasonal speed modifier")]
    public float seasonalSpeedMod = 1f;

    [Header("Skills")]
    [Tooltip("Active skill (1 per animal)")]
    public string activeSkillId = "";

    [Tooltip("Passive skills (up to 4)")]
    public List<string> passiveSkillIds = new List<string>();

    // ============================================================================
    // CALCULATED PROPERTIES
    // ============================================================================

    /// <summary>
    /// Happiness multiplier applied to combat stats (0.5x to 1.5x)
    /// </summary>
    public float HappinessMultiplier => 0.5f + (happiness / 100f);

    /// <summary>
    /// Final attack stat with all modifiers applied
    /// </summary>
    public float CurrentAttack => baseAttack * attackGrowth * HappinessMultiplier * seasonalAttackMod;

    /// <summary>
    /// Final defense stat with all modifiers applied
    /// </summary>
    public float CurrentDefense => baseDefense * defenseGrowth * HappinessMultiplier * seasonalDefenseMod;

    /// <summary>
    /// Final speed stat with all modifiers applied
    /// </summary>
    public float CurrentSpeed => baseSpeed * speedGrowth * seasonalSpeedMod;

    /// <summary>
    /// Final health with growth modifier applied
    /// </summary>
    public float MaxHealth => baseHealth * healthGrowth;

    /// <summary>
    /// Final accuracy (clamped to 0-1)
    /// </summary>
    public float CurrentAccuracy => Mathf.Clamp01(baseAccuracy);

    // ============================================================================
    // STAT GROWTH METHODS
    // ============================================================================

    /// <summary>
    /// Apply growth to a specific stat
    /// </summary>
    public void ApplyStatGrowth(string statName, float growthAmount)
    {
        switch (statName.ToLower())
        {
            case "attack":
                attackGrowth += growthAmount;
                attackGrowth = Mathf.Clamp(attackGrowth, 1f, 3f); // Cap at 3x growth
                break;

            case "defense":
                defenseGrowth += growthAmount;
                defenseGrowth = Mathf.Clamp(defenseGrowth, 1f, 3f);
                break;

            case "speed":
                speedGrowth += growthAmount;
                speedGrowth = Mathf.Clamp(speedGrowth, 1f, 3f);
                break;

            case "health":
                healthGrowth += growthAmount;
                healthGrowth = Mathf.Clamp(healthGrowth, 1f, 3f);
                break;

            case "all":
                // Apply to all stats equally
                ApplyStatGrowth("attack", growthAmount);
                ApplyStatGrowth("defense", growthAmount);
                ApplyStatGrowth("speed", growthAmount);
                ApplyStatGrowth("health", growthAmount);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Modify happiness value
    /// </summary>
    public void ModifyHappiness(float amount)
    {
        happiness = Mathf.Clamp(happiness + amount, 0f, 100f);
    }

    /// <summary>
    /// Apply daily happiness decay
    /// </summary>
    public void ApplyDailyHappinessDecay(bool wasPet, bool wasFed)
    {
        if (!wasPet)
        {
            ModifyHappiness(-0.5f); // Slow decay if not petted
        }

        if (!wasFed)
        {
            ModifyHappiness(-1f); // Faster decay if not fed
        }

        // Minimum happiness is 20 (0.7x multiplier)
        happiness = Mathf.Max(happiness, 20f);
    }

    /// <summary>
    /// Apply seasonal modifiers
    /// </summary>
    public void ApplySeasonalModifiers(float attackMod, float defenseMod, float speedMod)
    {
        seasonalAttackMod = attackMod;
        seasonalDefenseMod = defenseMod;
        seasonalSpeedMod = speedMod;
    }

    /// <summary>
    /// Reset seasonal modifiers to default
    /// </summary>
    public void ResetSeasonalModifiers()
    {
        seasonalAttackMod = 1f;
        seasonalDefenseMod = 1f;
        seasonalSpeedMod = 1f;
    }

    /// <summary>
    /// Set the active skill for this animal
    /// </summary>
    public void SetActiveSkill(string skillId)
    {
        activeSkillId = skillId;
    }

    /// <summary>
    /// Unlock a passive skill (max 4)
    /// </summary>
    public bool UnlockPassiveSkill(string skillId)
    {
        if (passiveSkillIds.Count >= 4)
        {
            return false;
        }

        if (!passiveSkillIds.Contains(skillId))
        {
            passiveSkillIds.Add(skillId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if a passive skill is unlocked
    /// </summary>
    public bool HasPassiveSkill(string skillId)
    {
        return passiveSkillIds.Contains(skillId);
    }

    /// <summary>
    /// Get total skill count (1 active + passives)
    /// </summary>
    public int GetTotalSkillCount()
    {
        int count = string.IsNullOrEmpty(activeSkillId) ? 0 : 1;
        count += passiveSkillIds.Count;
        return count;
    }

    // ============================================================================
    // INITIALIZATION
    // ============================================================================

    /// <summary>
    /// Initialize combat stats from base data
    /// </summary>
    public void Initialize(AnimalCombatStats baseStats)
    {
        if (baseStats == null) return;

        baseAttack = baseStats.baseAttack;
        baseDefense = baseStats.baseDefense;
        baseSpeed = baseStats.baseSpeed;
        baseAccuracy = baseStats.baseAccuracy;
        baseHealth = baseStats.baseHealth;

        currentHealth = MaxHealth;

        // Copy base skills
        activeSkillId = baseStats.activeSkillId;
        passiveSkillIds = new List<string>(baseStats.passiveSkillIds);
    }

    /// <summary>
    /// Reset growth modifiers to default
    /// </summary>
    public void ResetGrowth()
    {
        attackGrowth = 1f;
        defenseGrowth = 1f;
        speedGrowth = 1f;
        healthGrowth = 1f;
    }

    // ============================================================================
    // DEBUG & DISPLAY
    // ============================================================================

    /// <summary>
    /// Get a formatted string of all combat stats
    /// </summary>
    public string GetStatsDisplay()
    {
        return $"Attack: {CurrentAttack:F1} | Defense: {CurrentDefense:F1} | Speed: {CurrentSpeed:F1}\n" +
               $"Health: {currentHealth:F0}/{MaxHealth:F0} | Accuracy: {CurrentAccuracy * 100f:F0}%\n" +
               $"Happiness: {happiness:F0}/100 (x{HappinessMultiplier:F2} multiplier)";
    }

    /// <summary>
    /// Get growth multipliers display
    /// </summary>
    public string GetGrowthDisplay()
    {
        return $"Growth Modifiers:\n" +
               $"Attack: x{attackGrowth:F2} | Defense: x{defenseGrowth:F2}\n" +
               $"Speed: x{speedGrowth:F2} | Health: x{healthGrowth:F2}";
    }
}
