using NUnit.Framework;
using UnityEngine;
using SowurShield.Dialogue;
using SowurShield.Inventory;

namespace SowurShield.Tests
{

/// <summary>
/// Covers the gift-preference system added 2026-08-01: per-NPC taste multipliers, the
/// discovery record the codex reads, and the locked/unlocked lore split.
///
/// The multiplier table is the part worth pinning down. Neutral must stay exactly 1x —
/// every item authored before preferences existed relies on that to keep its old value, and
/// a "harmless" tweak there would silently rebalance all existing gift content.
/// </summary>
public class GiftPreferenceTests
{
    private GameObject _npcObject;
    private NPCDialogueInteractable _npc;
    private GameObject _memoryObject;
    private ConversationMemory _memory;

    [SetUp]
    public void SetUp()
    {
        // A real ConversationMemory is required, not optional scaffolding: the discovery
        // methods return early when it is null, which would make the "not yet discovered"
        // tests below pass without ever reaching the code they claim to cover.
        //
        // Built without going through Awake(): it calls DontDestroyOnLoad, which throws
        // outside Play Mode. ConversationMemory documents that its public API must work
        // without Awake having run, so InitializeMemorySystem is enough.
        _memoryObject = new GameObject("TestConversationMemory");
        var memory = _memoryObject.AddComponent<ConversationMemory>();
        typeof(ConversationMemory)
            .GetMethod("InitializeMemorySystem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(memory, null);

        _npcObject = new GameObject("TestNPC");
        _npc = _npcObject.AddComponent<NPCDialogueInteractable>();

        var so = new UnityEditor.SerializedObject(_npc);
        so.FindProperty("npcId").stringValue = "test_npc";
        so.ApplyModifiedPropertiesWithoutUndo();

        // NPCDialogueInteractable caches the singleton in Start(), which does not run in
        // EditMode — hand it the reference the same way Start would.
        _memory = memory;
        typeof(NPCDialogueInteractable)
            .GetField("conversationMemory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_npc, memory);
    }

    [TearDown]
    public void TearDown()
    {
        if (_npcObject != null) Object.DestroyImmediate(_npcObject);
        if (_memoryObject != null) Object.DestroyImmediate(_memoryObject);
    }

    private Item MakeItem(string itemName, float giftValue)
    {
        var item = ScriptableObject.CreateInstance<Item>();
        item.itemName = itemName;
        item.giftAffinityValue = giftValue;
        return item;
    }

    private void SetPreferences(string[] loved, string[] liked, string[] disliked)
    {
        var so = new UnityEditor.SerializedObject(_npc);
        SetArray(so, "lovedGifts", loved);
        SetArray(so, "likedGifts", liked);
        SetArray(so, "dislikedGifts", disliked);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private void SetArray(UnityEditor.SerializedObject so, string propertyName, string[] values)
    {
        var prop = so.FindProperty(propertyName);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    // =====================================================================
    // Multiplier table
    // =====================================================================

    [Test]
    public void NeutralMultiplier_IsExactlyOne_SoPreExistingGiftsAreUnchanged()
    {
        Assert.That(NPCDialogueInteractable.GetMultiplierFor(GiftReaction.Neutral), Is.EqualTo(1f),
            "Neutral must be 1x. Every item authored before gift preferences existed depends on " +
            "this to keep its original affinity value.");
    }

    [Test]
    public void DislikedMultiplier_IsNegative_NotMerelySmaller()
    {
        Assert.That(NPCDialogueInteractable.GetMultiplierFor(GiftReaction.Disliked), Is.LessThan(0f),
            "A disliked gift should cost affinity, otherwise it is just a weak gift and there is " +
            "no reason to avoid giving it.");
    }

    [Test]
    public void MultipliersAreOrdered_DislikedBelowNeutralBelowLikedBelowLoved()
    {
        float disliked = NPCDialogueInteractable.GetMultiplierFor(GiftReaction.Disliked);
        float neutral  = NPCDialogueInteractable.GetMultiplierFor(GiftReaction.Neutral);
        float liked    = NPCDialogueInteractable.GetMultiplierFor(GiftReaction.Liked);
        float loved    = NPCDialogueInteractable.GetMultiplierFor(GiftReaction.Loved);

        Assert.That(disliked, Is.LessThan(neutral));
        Assert.That(neutral,  Is.LessThan(liked));
        Assert.That(liked,    Is.LessThan(loved));
    }

    // =====================================================================
    // Reaction lookup
    // =====================================================================

    [Test]
    public void GetReactionTo_MatchesEachPreferenceList()
    {
        SetPreferences(
            loved:    new[] { "MysterySeed" },
            liked:    new[] { "Carrot" },
            disliked: new[] { "Feather" });

        Assert.That(_npc.GetReactionTo(MakeItem("MysterySeed", 10f)), Is.EqualTo(GiftReaction.Loved));
        Assert.That(_npc.GetReactionTo(MakeItem("Carrot", 10f)),      Is.EqualTo(GiftReaction.Liked));
        Assert.That(_npc.GetReactionTo(MakeItem("Feather", 10f)),     Is.EqualTo(GiftReaction.Disliked));
    }

    [Test]
    public void GetReactionTo_UnlistedItem_IsNeutral()
    {
        SetPreferences(new[] { "MysterySeed" }, new string[0], new string[0]);

        Assert.That(_npc.GetReactionTo(MakeItem("Wood", 10f)), Is.EqualTo(GiftReaction.Neutral),
            "An item nobody expressed an opinion about must be Neutral, not fall into a tier.");
    }

    [Test]
    public void GetReactionTo_NullItem_IsNeutral_AndDoesNotThrow()
    {
        Assert.That(_npc.GetReactionTo(null), Is.EqualTo(GiftReaction.Neutral));
    }

    /// <summary>
    /// Preferences are matched on the internal itemName, never the localized display name.
    /// Matching on the display name would make every preference silently stop working as soon
    /// as the player switched to Portuguese or Spanish.
    /// </summary>
    [Test]
    public void GetReactionTo_MatchesInternalName_NotDisplayName()
    {
        SetPreferences(new[] { "Pumpkin" }, new string[0], new string[0]);

        var item = MakeItem("Pumpkin", 10f);
        Assert.That(_npc.GetReactionTo(item), Is.EqualTo(GiftReaction.Loved));

        // An item whose internal name differs must not match, however it displays.
        var impostor = MakeItem("Abobora", 10f);
        Assert.That(_npc.GetReactionTo(impostor), Is.EqualTo(GiftReaction.Neutral));
    }

    [Test]
    public void GetAllPreferredItemNames_ReturnsEveryTier()
    {
        SetPreferences(
            loved:    new[] { "A", "B" },
            liked:    new[] { "C" },
            disliked: new[] { "D", "E" });

        Assert.That(_npc.GetAllPreferredItemNames().Length, Is.EqualTo(5));
    }

    // =====================================================================
    // Discovery (what the codex is allowed to show)
    // =====================================================================

    /// <summary>
    /// The codex must show what the player *learned*, not the answer key. An undiscovered
    /// preference has to read as unknown even though the NPC's taste is already defined.
    /// </summary>
    [Test]
    public void GetDiscoveredReaction_BeforeGifting_IsNull()
    {
        SetPreferences(new[] { "MysterySeed" }, new string[0], new string[0]);

        Assert.That(_npc.GetDiscoveredReaction("MysterySeed"), Is.Null,
            "A taste the player has never tested must not be pre-revealed in the codex.");
    }

    [Test]
    public void GetDiscoveredReaction_UnknownItemName_IsNull()
    {
        Assert.That(_npc.GetDiscoveredReaction("NoSuchItem"), Is.Null);
        Assert.That(_npc.GetDiscoveredReaction(null), Is.Null);
        Assert.That(_npc.GetDiscoveredReaction(""), Is.Null);
    }

    /// <summary>
    /// The full round trip: giving an item records the taste, and only that item's taste.
    /// This is the test that actually reaches the discovery storage — the "before gifting"
    /// ones above would pass even if the lookup were bypassed entirely.
    /// </summary>
    [Test]
    public void ReceiveGift_RecordsTheDiscovery_ForThatItemOnly()
    {
        SetPreferences(
            loved:    new[] { "MysterySeed" },
            liked:    new string[0],
            disliked: new[] { "Feather" });

        var reaction = _npc.ReceiveGift(MakeItem("MysterySeed", 10f));

        Assert.That(reaction, Is.EqualTo(GiftReaction.Loved), "ReceiveGift should report the reaction.");
        Assert.That(_npc.GetDiscoveredReaction("MysterySeed"), Is.EqualTo(GiftReaction.Loved),
            "Giving an item must record what was learned about it.");
        Assert.That(_npc.GetDiscoveredReaction("Feather"), Is.Null,
            "Giving one item must not reveal the NPC's taste for a different one.");
    }

    /// <summary>
    /// A neutral gift teaches the player nothing, so it should not occupy a codex row.
    /// </summary>
    [Test]
    public void ReceiveGift_NeutralItem_RecordsNoDiscovery()
    {
        SetPreferences(new[] { "MysterySeed" }, new string[0], new string[0]);

        _npc.ReceiveGift(MakeItem("Wood", 10f));

        Assert.That(_npc.GetDiscoveredReaction("Wood"), Is.Null,
            "\"They don't care about this\" is not a discovery worth listing in the codex.");
    }

    [Test]
    public void ReceiveGift_AppliesTheMultiplier_ToRelationship()
    {
        SetPreferences(new[] { "MysterySeed" }, new string[0], new string[0]);

        float before = _memory.GetRelationshipLevel("test_npc");
        _npc.ReceiveGift(MakeItem("MysterySeed", 10f));
        float after = _memory.GetRelationshipLevel("test_npc");

        // 10 base x 2.5 (Loved) = 25
        Assert.That(after - before, Is.EqualTo(25f).Within(0.01f),
            "A loved gift should apply the 2.5x multiplier, not the item's base value.");
    }

    [Test]
    public void ReceiveGift_DislikedItem_LowersRelationship()
    {
        SetPreferences(new string[0], new string[0], new[] { "Feather" });

        float before = _memory.GetRelationshipLevel("test_npc");
        _npc.ReceiveGift(MakeItem("Feather", 10f));
        float after = _memory.GetRelationshipLevel("test_npc");

        Assert.That(after, Is.LessThan(before),
            "A disliked gift must reduce affinity, not increase it by a smaller amount.");
    }
}

} // namespace SowurShield.Tests
