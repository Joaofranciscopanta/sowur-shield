using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using SowurShield.Dialogue;
using SowurShield.Inventory;

namespace SowurShield.Tests
{

/// <summary>
/// Quests wire themselves to the rest of the game entirely through strings: an item name, an
/// npcId, a conversationId. Every one of those is resolved at runtime, and a miss produces a
/// quest objective that can never be completed — with no error at edit time, no error at build
/// time, and nothing in the console until a player picks the quest up and gets stuck.
///
/// Written after two real instances found on 2026-08-01 while populating the village:
///   - three new quests targeted TalkToNPC objectives by npcId, but ConversationMemory
///     notifies that objective with the *conversationId* (ConversationMemory.cs:163)
///   - the pre-existing get_to_know_maren quest was correct, which is how the mistake was
///     caught: it looked wrong next to the others and turned out to be the only right one
/// </summary>
public class QuestAndNpcIntegrityTests
{
    private static QuestData[] AllQuests() => Resources.LoadAll<QuestData>("Quests");

    /// <summary>
    /// Every conversationId defined anywhere in the project. TalkToNPC targets must be one of
    /// these, NOT an npcId — the two look interchangeable and are not.
    /// </summary>
    private static HashSet<string> AllConversationIds()
    {
        var ids = new HashSet<string>();
        foreach (var tree in Resources.LoadAll<DialogueTree>(""))
        {
            if (tree != null && !string.IsNullOrEmpty(tree.conversationId))
                ids.Add(tree.conversationId);
        }
        return ids;
    }

    [SetUp]
    public void SetUp()
    {
        ItemDatabase.ForceReload();
    }

    [Test]
    public void EveryCollectItemObjective_TargetsARealItem()
    {
        foreach (var quest in AllQuests())
        {
            foreach (var objective in quest.objectives)
            {
                if (objective.type != QuestObjectiveType.CollectItem) continue;

                Assert.That(ItemDatabase.GetItem(objective.targetId), Is.Not.Null,
                    $"Quest '{quest.questId}' asks the player to collect '{objective.targetId}', " +
                    "which is not in the ItemDatabase. The objective can never complete.");
            }
        }
    }

    /// <summary>
    /// The mistake this file exists for. TalkToNPC is notified with the conversationId, so
    /// targeting the npcId produces an objective that silently never advances.
    /// </summary>
    [Test]
    public void EveryTalkToNpcObjective_TargetsAConversationId_NotAnNpcId()
    {
        var conversationIds = AllConversationIds();
        Assert.That(conversationIds, Is.Not.Empty, "No DialogueTrees found — check Resources paths.");

        foreach (var quest in AllQuests())
        {
            foreach (var objective in quest.objectives)
            {
                if (objective.type != QuestObjectiveType.TalkToNPC) continue;

                Assert.That(conversationIds.Contains(objective.targetId), Is.True,
                    $"Quest '{quest.questId}' targets '{objective.targetId}' for TalkToNPC, which is " +
                    "not a known conversationId. ConversationMemory notifies this objective with the " +
                    "conversationId (e.g. \"maren_default\"), never the npcId (\"maren\") — an npcId " +
                    "here produces an objective that never advances.");
            }
        }
    }

    [Test]
    public void EveryItemReward_ExistsInTheItemDatabase()
    {
        foreach (var quest in AllQuests())
        {
            foreach (var reward in quest.rewardItems)
            {
                Assert.That(ItemDatabase.GetItem(reward.itemName), Is.Not.Null,
                    $"Quest '{quest.questId}' rewards '{reward.itemName}', which does not exist. " +
                    "The player completes the quest and receives nothing.");
            }
        }
    }

    [Test]
    public void EveryQuest_HasAtLeastOneObjective()
    {
        foreach (var quest in AllQuests())
        {
            Assert.That(quest.objectives.Count, Is.GreaterThan(0),
                $"Quest '{quest.questId}' has no objectives, so it can be accepted but never completed.");
        }
    }

    [Test]
    public void QuestIds_AreUnique()
    {
        var seen = new Dictionary<string, string>();
        foreach (var quest in AllQuests())
        {
            string existing;
            bool duplicate = seen.TryGetValue(quest.questId, out existing);

            Assert.That(duplicate, Is.False,
                $"Duplicate questId '{quest.questId}' on assets '{quest.name}' and '{existing}'. " +
                "Quest status is keyed by id, so two quests sharing one would share progress.");

            seen[quest.questId] = quest.name;
        }
    }

    /// <summary>
    /// Prerequisites are quest ids too, and a typo'd one makes a quest permanently unavailable
    /// rather than throwing.
    /// </summary>
    [Test]
    public void EveryPrerequisite_NamesARealQuest()
    {
        var known = new HashSet<string>();
        foreach (var quest in AllQuests()) known.Add(quest.questId);

        foreach (var quest in AllQuests())
        {
            foreach (string prerequisite in quest.prerequisiteQuestIds)
            {
                Assert.That(known.Contains(prerequisite), Is.True,
                    $"Quest '{quest.questId}' requires '{prerequisite}', which is not a real quest id. " +
                    "The quest would never become available.");
            }
        }
    }

    /// <summary>
    /// A villager with no dialogue tree is interactable but silent, which reads as a broken
    /// NPC rather than as unfinished content.
    /// </summary>
    [Test]
    public void EveryVillagerDialogueTree_HasAStartNodeAndText()
    {
        var trees = Resources.LoadAll<DialogueTree>("Dialogues/Villagers");
        Assert.That(trees, Is.Not.Empty, "No villager dialogue trees found.");

        foreach (var tree in trees)
        {
            var start = tree.GetStartNode();
            Assert.That(start, Is.Not.Null,
                $"'{tree.name}' has no node matching its startNodeId '{tree.startNodeId}'.");

            // A LocalizedString left on a name-only reference keeps KeyId 0 and resolves to
            // nothing at runtime — the villager opens an empty speech bubble.
            Assert.That(start.dialogueText.IsEmpty, Is.False,
                $"'{tree.name}' start node has an unbound dialogueText. It will render blank.");
        }
    }
}

} // namespace SowurShield.Tests
