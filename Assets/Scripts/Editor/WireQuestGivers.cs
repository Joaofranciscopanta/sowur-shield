using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SowurShield.Dialogue;

namespace SowurShield.Editor
{

/// <summary>
/// Gives each villager a quest to hand out, so every quest in the project can actually be
/// reached and every objective type gets exercised by playing.
///
/// <para>Seven quests are written, translated and wired to their reward payouts, but only
/// Maren's dialogue tree carried a StartQuest effect. The other six were unreachable: nothing
/// in the game could start them, and four of the eight achievements referenced quests that
/// could never begin.</para>
///
/// <para>The assignment below spreads the four auto-tracked objective types across the
/// villagers so a single playthrough touches farming, gathering, animals, the shop loop,
/// conversation and combat:</para>
///
/// <list type="bullet">
/// <item><b>Joana</b> - meet_the_village (TalkToNPC x3): sends the player round the village,
/// which is also how they discover the other quest givers.</item>
/// <item><b>Isabela</b> - isabela_pantry (CollectItem: eggs + milk): exercises the animals,
/// the feeding trough and the ground-item pickup path.</item>
/// <item><b>Clara</b> - clara_remedy (CollectItem: cabbage + radish): exercises planting,
/// watering and harvesting crops other than the tutorial carrot.</item>
/// <item><b>Bento</b> - egg_collector (CollectItem: eggs): a short one that overlaps
/// Isabela's, so stacked objectives on the same item get tested.</item>
/// <item><b>Elias</b> - clear_sunny_fields (CompleteBattle): the only route into combat, and
/// it already carries first_harvest as a prerequisite, so prerequisite gating gets tested
/// too.</item>
/// </list>
///
/// <para>Maren keeps get_to_know_maren and the player starts with first_harvest, so all seven
/// are reachable. The tool is idempotent: it replaces the effect on the node it manages rather
/// than appending, so running it twice does not stack duplicates.</para>
///
/// Menu: Sowur Shield > Quests > Wire Quest Givers
/// </summary>
public static class WireQuestGivers
{
    /// <summary>Dialogue tree asset name -> quest id it should offer.</summary>
    private static readonly (string tree, string questId, string note)[] Assignments =
    {
        ("joana_Default",    "meet_the_village",   "Sends the player round the village"),
        ("isabela_Default",  "isabela_pantry",     "Eggs and milk: animals + pickups"),
        ("clara_Default",    "clara_remedy",       "Cabbage and radish: the farming loop"),
        ("bento_Default",    "egg_collector",      "Overlaps Isabela's egg objective"),
        ("elias_Default",    "clear_sunny_fields", "The way into combat; gated on first_harvest"),

        // Maren has five trees and picks one by relationship, so the effect has to be on all
        // of the conversational ones. Hers was only on Maren_Default: at relationship 81 she
        // greets from Maren_Beloved instead and her quest became permanently unreachable --
        // befriending her locked you out of her own quest. GiftReaction is deliberately left
        // out; it is a one-line response to a present, not a conversation.
        ("Maren_Default",        "get_to_know_maren", "Her own quest, at any relationship"),
        ("Maren_Friend",         "get_to_know_maren", "Her own quest, at any relationship"),
        ("Maren_Beloved",        "get_to_know_maren", "Her own quest, at any relationship"),
        ("Maren_SeasonalSpring", "get_to_know_maren", "Her own quest, at any relationship"),
    };

    // DialogueEffect.EffectType.StartQuest. Taken from the enum rather than hard-coded so a
    // reordering of that enum breaks the build here instead of silently firing the wrong
    // effect.
    private static int StartQuestEffect => (int)EffectType.StartQuest;

    [MenuItem("Sowur Shield/Quests/Wire Quest Givers")]
    public static void Wire()
    {
        var known = new HashSet<string>(
            AssetDatabase.FindAssets("t:QuestData")
                .Select(g => AssetDatabase.LoadAssetAtPath<QuestData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(q => q != null)
                .Select(q => new SerializedObject(q).FindProperty("questId").stringValue));

        int wired = 0, skipped = 0;

        foreach ((string treeName, string questId, string note) in Assignments)
        {
            if (!known.Contains(questId))
            {
                Debug.LogWarning($"[WireQuestGivers] No QuestData with id '{questId}'; skipping.");
                skipped++;
                continue;
            }

            string path = AssetDatabase.FindAssets("t:DialogueTree")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == treeName);

            if (path == null)
            {
                Debug.LogWarning($"[WireQuestGivers] No dialogue tree '{treeName}'; skipping.");
                skipped++;
                continue;
            }

            var tree = AssetDatabase.LoadAssetAtPath<DialogueTree>(path);
            if (tree == null) { skipped++; continue; }

            if (AddStartQuestEffect(tree, questId, note)) wired++;
            else skipped++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[WireQuestGivers] {wired} quest giver(s) wired, {skipped} skipped.");
    }

    /// <summary>
    /// Puts a StartQuest effect on the tree's first node. StartQuest is a no-op when the quest
    /// is already active or finished, so firing it on every greeting is safe.
    /// </summary>
    private static bool AddStartQuestEffect(DialogueTree tree, string questId, string note)
    {
        var so = new SerializedObject(tree);
        SerializedProperty nodes = so.FindProperty("nodes");

        if (nodes == null || nodes.arraySize == 0)
        {
            Debug.LogWarning($"[WireQuestGivers] '{tree.name}' has no nodes.");
            return false;
        }

        SerializedProperty node = nodes.GetArrayElementAtIndex(0);
        SerializedProperty effects = node.FindPropertyRelative("nodeEffects");
        if (effects == null)
        {
            Debug.LogWarning($"[WireQuestGivers] '{tree.name}' node 0 has no nodeEffects.");
            return false;
        }

        // Replace an existing StartQuest for this same quest rather than appending, so the
        // tool can be re-run without stacking duplicates.
        for (int i = 0; i < effects.arraySize; i++)
        {
            SerializedProperty e = effects.GetArrayElementAtIndex(i);
            if (e.FindPropertyRelative("effectType").enumValueIndex == StartQuestEffect &&
                e.FindPropertyRelative("effectKey").stringValue == questId)
                return true;   // already wired
        }

        effects.InsertArrayElementAtIndex(effects.arraySize);
        SerializedProperty added = effects.GetArrayElementAtIndex(effects.arraySize - 1);
        added.FindPropertyRelative("effectType").enumValueIndex = StartQuestEffect;
        added.FindPropertyRelative("effectKey").stringValue = questId;
        added.FindPropertyRelative("effectValue").stringValue = string.Empty;
        added.FindPropertyRelative("numericValue").floatValue = 0f;
        added.FindPropertyRelative("description").stringValue =
            $"Offer '{questId}'. {note}. StartQuest is a no-op if already active or done.";

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(tree);

        Debug.Log($"[WireQuestGivers] {tree.name} now offers '{questId}'.");
        return true;
    }
}

}
