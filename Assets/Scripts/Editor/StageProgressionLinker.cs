using UnityEngine;
using UnityEditor;
using System.Linq;
using SowurShield.Combat;

namespace SowurShield.Editor
{

/// <summary>
/// One-click linker for the world map's stage progression chain.
/// Wires sequential prerequisiteStage links within each biome and chains each
/// biome's entry stage to the previous biome's boss, so BiomeUnlockChecker can
/// progress players from Meadow through Forest, Cave, Mountain and Volcano.
/// Menu: Tools > Combat > Link Stage Progression
/// Also runnable headless via -executeMethod SowurShield.Editor.StageProgressionLinker.LinkAll
/// </summary>
public static class StageProgressionLinker
{
    [MenuItem("Tools/Combat/Link Stage Progression")]
    public static void LinkAll()
    {
        StageData[] allStages = Resources.LoadAll<StageData>("Stages");

        StageData[] meadow   = OrderByNumber(allStages, StageTheme.Meadow);
        StageData[] forest   = OrderByNumber(allStages, StageTheme.Forest);
        StageData[] cave     = OrderByNumber(allStages, StageTheme.Cave);
        StageData[] mountain = OrderByNumber(allStages, StageTheme.Mountain);
        StageData[] volcano  = OrderByNumber(allStages, StageTheme.Volcano);

        int linked = 0;

        // Intra-biome sequential chains (each stage requires the previous one).
        linked += LinkSequential(meadow);
        linked += LinkSequential(forest);
        linked += LinkSequential(cave);
        linked += LinkSequential(mountain);
        linked += LinkSequential(volcano);

        // Cross-biome chains: each biome's entry stage requires the previous
        // biome's boss (last stage in its ordered array).
        linked += LinkCrossBiome(meadow, forest);
        linked += LinkCrossBiome(forest, cave);
        linked += LinkCrossBiome(cave, mountain);
        linked += LinkCrossBiome(mountain, volcano);

        // Only the very first stage of the whole progression is unlocked by default.
        linked += SetUnlockedByDefault(meadow, forest, cave, mountain, volcano);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[StageProgressionLinker] Linked {linked} stage field(s) across " +
            $"{meadow.Length + forest.Length + cave.Length + mountain.Length + volcano.Length} stages.");
    }

    private static StageData[] OrderByNumber(StageData[] stages, StageTheme theme)
    {
        return stages.Where(s => s != null && s.theme == theme)
                      .OrderBy(s => s.stageNumber)
                      .ToArray();
    }

    /// <summary>
    /// Sets stages[i].prerequisiteStage = stages[i-1] for i > 0.
    /// Does not touch stages[0] — cross-biome linking handles entry stages.
    /// </summary>
    private static int LinkSequential(StageData[] stages)
    {
        int changed = 0;
        for (int i = 1; i < stages.Length; i++)
        {
            if (stages[i].prerequisiteStage != stages[i - 1])
            {
                stages[i].prerequisiteStage = stages[i - 1];
                EditorUtility.SetDirty(stages[i]);
                changed++;
            }
        }
        return changed;
    }

    /// <summary>
    /// Sets the first stage of nextBiome's prerequisite to the last stage
    /// (boss) of previousBiome.
    /// </summary>
    private static int LinkCrossBiome(StageData[] previousBiome, StageData[] nextBiome)
    {
        if (previousBiome.Length == 0 || nextBiome.Length == 0) return 0;

        StageData boss = previousBiome[previousBiome.Length - 1];
        StageData entry = nextBiome[0];

        if (entry.prerequisiteStage != boss)
        {
            entry.prerequisiteStage = boss;
            EditorUtility.SetDirty(entry);
            return 1;
        }
        return 0;
    }

    /// <summary>
    /// Ensures exactly the first stage of the first biome is unlockedByDefault,
    /// and every other stage is not.
    /// </summary>
    private static int SetUnlockedByDefault(params StageData[][] biomesInOrder)
    {
        int changed = 0;
        bool first = true;

        foreach (StageData[] biome in biomesInOrder)
        {
            foreach (StageData stage in biome)
            {
                bool shouldBeUnlocked = first;
                first = false;

                if (stage.unlockedByDefault != shouldBeUnlocked)
                {
                    stage.unlockedByDefault = shouldBeUnlocked;
                    EditorUtility.SetDirty(stage);
                    changed++;
                }
            }
        }

        return changed;
    }
}

} // namespace SowurShield.Editor
