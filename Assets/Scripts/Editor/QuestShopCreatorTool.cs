using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SowurShield.Dialogue;

namespace SowurShield.Editor
{

/// <summary>
/// Editor tool that scaffolds starter QuestData and ShopData ScriptableObject assets,
/// so designers can tweak them in the Inspector instead of hand-authoring YAML.
///
/// RUN:
///   Tools > Sowur Shield > Create Example Quests
///   Tools > Sowur Shield > Create Example Shop
///   Tools > Sowur Shield > Create Example Quests + Shop
///
/// Quests are written to Resources/Quests/ (auto-loaded by QuestManager at runtime).
/// Shops are written to Resources/Shops/ (assign the asset to a ShopNPC in the scene).
///
/// IMPORTANT: itemName / cropName / npcId / stageName fields below are placeholders —
/// edit each asset so its match keys line up with your ItemDatabase, CropData,
/// ConversationMemory npc ids, and StageData stage names. See QuestObjectiveType docs.
/// </summary>
public static class QuestShopCreatorTool
{
    private const string QuestFolder = "Assets/Resources/Quests";
    private const string ShopFolder  = "Assets/Resources/Shops";

    // =========================================================================
    // Menu entry points
    // =========================================================================

    [MenuItem("Tools/Sowur Shield/Create Example Quests + Shop")]
    public static void CreateAll()
    {
        CreateExampleQuests();
        CreateExampleShop();
    }

    [MenuItem("Tools/Sowur Shield/Create Example Quests")]
    public static void CreateExampleQuests()
    {
        EnsureFolder(QuestFolder);

        CreateQuest(
            id: "welcome_harvest",
            title: "Welcome Harvest",
            description: "Harvest 3 carrots to get your farm started.",
            objectives: new List<QuestObjective>
            {
                MakeObjective("Harvest 3 carrots", QuestObjectiveType.HarvestCrop, "Carrot", 3),
            },
            prereqs: null,
            gold: 100,
            itemRewards: null,
            relationshipRewards: null);

        CreateQuest(
            id: "egg_collector",
            title: "Egg Collector",
            description: "Gather 5 eggs from your chickens.",
            objectives: new List<QuestObjective>
            {
                MakeObjective("Collect 5 eggs", QuestObjectiveType.CollectItem, "Egg", 5),
            },
            prereqs: new List<string> { "welcome_harvest" },
            gold: 150,
            itemRewards: new List<QuestItemReward>
            {
                new QuestItemReward { itemName = "Carrot", quantity = 3 },
            },
            relationshipRewards: null);

        CreateQuest(
            id: "first_victory",
            title: "First Victory",
            description: "Win your first battle in the Meadow.",
            objectives: new List<QuestObjective>
            {
                // targetId must match a StageData.stageName — edit to your first stage.
                MakeObjective("Win a Meadow battle", QuestObjectiveType.CompleteBattle, "Meadow 1", 1),
            },
            prereqs: null,
            gold: 200,
            itemRewards: null,
            relationshipRewards: null);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[QuestShopCreatorTool] Example quests created in {QuestFolder}/ — edit match keys in the Inspector.");
    }

    [MenuItem("Tools/Sowur Shield/Create Example Shop")]
    public static void CreateExampleShop()
    {
        EnsureFolder(ShopFolder);

        string path = $"{ShopFolder}/GeneralStore.asset";
        if (AssetDatabase.LoadAssetAtPath<ShopData>(path) != null)
        {
            Debug.Log($"[QuestShopCreatorTool] {path} already exists — skipping.");
            return;
        }

        ShopData shop = ScriptableObject.CreateInstance<ShopData>();
        shop.shopTitle       = "General Store";
        shop.shopkeeperNpcId = "merchant"; // edit to a real ConversationMemory npc id for the friendship discount
        shop.items = new List<ShopItemEntry>
        {
            // itemName must match ItemDatabase keys exactly. -1 maxStock = unlimited.
            new ShopItemEntry { itemName = "Carrot",  basePrice = 12, maxStock = -1 },
            new ShopItemEntry { itemName = "Cabbage", basePrice = 20, maxStock = -1 },
            new ShopItemEntry { itemName = "Medicine", basePrice = 80, maxStock = 5 },
        };

        AssetDatabase.CreateAsset(shop, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[QuestShopCreatorTool] Created {path} — assign it to a ShopNPC and fix itemName keys.");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static void CreateQuest(
        string id, string title, string description,
        List<QuestObjective> objectives, List<string> prereqs,
        int gold, List<QuestItemReward> itemRewards,
        List<QuestRelationshipReward> relationshipRewards)
    {
        string path = $"{QuestFolder}/{id}.asset";
        if (AssetDatabase.LoadAssetAtPath<QuestData>(path) != null)
        {
            Debug.Log($"[QuestShopCreatorTool] {path} already exists — skipping.");
            return;
        }

        QuestData quest = ScriptableObject.CreateInstance<QuestData>();
        quest.questId             = id;
        quest.questTitle          = title;
        quest.questDescription    = description;
        quest.objectives          = objectives ?? new List<QuestObjective>();
        quest.prerequisiteQuestIds = prereqs ?? new List<string>();
        quest.rewardGold          = gold;
        quest.rewardItems         = itemRewards ?? new List<QuestItemReward>();
        quest.rewardRelationships = relationshipRewards ?? new List<QuestRelationshipReward>();

        AssetDatabase.CreateAsset(quest, path);
        Debug.Log($"[QuestShopCreatorTool] Created {path}");
    }

    private static QuestObjective MakeObjective(string description, QuestObjectiveType type, string targetId, int count)
    {
        return new QuestObjective
        {
            description   = description,
            type          = type,
            targetId      = targetId,
            requiredCount = count,
        };
    }

    private static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder)) return;

        // Create each missing path segment ("Assets/Resources/Quests" → Assets, Resources, Quests).
        string[] parts = assetFolder.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}

} // namespace SowurShield.Editor
