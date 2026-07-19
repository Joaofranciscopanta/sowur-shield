using System.Collections.Generic;
using SowurShield.Combat;

namespace SowurShield.Worldmap
{

/// <summary>
/// Static utility for querying biome unlock and completion state without
/// touching StageManager's internal collections directly.
/// </summary>
public static class BiomeUnlockChecker
{
    /// <summary>
    /// Returns true if the biome's boss stage has been completed.
    /// A boss stage is any stage in the theme whose bossEnemy is not null.
    /// </summary>
    public static bool IsBiomeComplete(StageTheme theme, List<StageData> biomeStages, HashSet<string> completedStages)
    {
        if (biomeStages == null || completedStages == null) return false;

        foreach (StageData stage in biomeStages)
        {
            if (stage == null) continue;
            if (stage.theme != theme) continue;
            if (stage.bossEnemy != null && completedStages.Contains(stage.stageName))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if any stage in this biome is accessible:
    /// either unlockedByDefault, or its prerequisite is in completedStages.
    /// </summary>
    public static bool IsBiomeUnlocked(List<StageData> biomeStages, HashSet<string> completedStages)
    {
        if (biomeStages == null || completedStages == null) return false;

        foreach (StageData stage in biomeStages)
        {
            if (stage == null) continue;

            if (stage.unlockedByDefault)
                return true;

            if (stage.prerequisiteStage != null &&
                completedStages.Contains(stage.prerequisiteStage.stageName))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the number of completed stages within the given biome stage list.
    /// </summary>
    public static int GetCompletedCount(List<StageData> biomeStages, HashSet<string> completedStages)
    {
        if (biomeStages == null || completedStages == null) return 0;

        int count = 0;
        foreach (StageData stage in biomeStages)
        {
            if (stage != null && completedStages.Contains(stage.stageName))
                count++;
        }

        return count;
    }
}

} // namespace SowurShield.Worldmap
