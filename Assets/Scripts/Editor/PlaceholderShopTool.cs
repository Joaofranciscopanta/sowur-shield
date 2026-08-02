using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Localization;
using UnityEditor.Localization;
using SowurShield.Dialogue;

namespace SowurShield.Editor
{
/// <summary>
/// Creates placeholder ShopData assets for the merchant-profile villagers and wires them to
/// the matching NPC in the open scene.
///
/// Context: ShopUI/ShopData/ShopItemRow were all implemented, but no ShopData asset had ever
/// been authored, so the general shop system was unreachable in game — only Maren's separate
/// seed shop was. This gives four villagers a working shop so the flow can be played and
/// tested end to end.
///
/// Stock is drawn from item names that already exist in ItemDatabase; a ShopItemEntry whose
/// itemName does not resolve is silently unbuyable, which reads as a broken shop rather than
/// as missing content. Prices are round placeholder numbers, not balance work.
///
/// Idempotent: re-running overwrites the same assets and re-assigns the same references.
/// </summary>
public static class PlaceholderShopTool
{
    private const string ShopFolder = "Assets/Resources/Shops";
    private const string TableName = "Dialogue";

    /// <summary>
    /// One merchant's placeholder inventory. Trades were chosen to match each villager's
    /// established role, so the shop contents are guessable from who they are.
    /// </summary>
    private struct ShopDef
    {
        public string npcId;
        public string titleEn, titlePt, titleEs;
        public (string item, int price)[] stock;
    }

    private static readonly ShopDef[] Shops =
    {
        new ShopDef {
            npcId = "tomas",
            titleEn = "Tomás's Forge", titlePt = "Forja do Tomás", titleEs = "Forja de Tomás",
            stock = new[] { ("Hoe", 120), ("Axe", 140), ("Shovel", 110), ("WateringCan", 90) }
        },
        new ShopDef {
            npcId = "isabela",
            titleEn = "Isabela's Bakery", titlePt = "Padaria da Isabela", titleEs = "Panadería de Isabela",
            stock = new[] { ("Bread", 25), ("Milk", 18), ("Egg", 12), ("Apple", 15) }
        },
        new ShopDef {
            npcId = "clara",
            titleEn = "Clara's Remedies", titlePt = "Remédios da Clara", titleEs = "Remedios de Clara",
            stock = new[] { ("Medicine", 60), ("CarrotSeed", 20), ("CabbageSeed", 22), ("RadishSeed", 18) }
        },
        new ShopDef {
            npcId = "rui",
            titleEn = "Rui's Workshop", titlePt = "Oficina do Rui", titleEs = "Taller de Rui",
            stock = new[] { ("Wood", 8), ("FishingRod", 100), ("Feather", 14) }
        },
    };

    /// <summary>
    /// No modal dialog on purpose: this menu item is also driven through automation, and
    /// EditorUtility.DisplayDialog blocks the editor until a human clicks it.
    /// </summary>
    [MenuItem("Tools/NPC/Create Placeholder Shops")]
    public static void CreateShops()
    {
        EnsureFolder();

        var collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
        if (collection == null)
        {
            Debug.LogError($"[PlaceholderShopTool] String table '{TableName}' not found — " +
                           "shop titles would render blank. Aborting.");
            return;
        }

        // Shared choice label, written once rather than per shop: every merchant uses the
        // same "Let's trade" option, matching how browse_seeds is handled.
        WriteEntry(collection, "dialogue.choice.browse_shop",
            en: "Let's trade.", pt: "Vamos negociar.", es: "Comerciemos.");

        var validItems = LoadValidItemNames();
        int created = 0, assigned = 0;

        foreach (var def in Shops)
        {
            string assetPath = $"{ShopFolder}/{def.npcId}_Shop.asset";

            var shop = AssetDatabase.LoadAssetAtPath<ShopData>(assetPath);
            bool isNew = shop == null;
            if (isNew) shop = ScriptableObject.CreateInstance<ShopData>();

            shop.shopkeeperNpcId = def.npcId;

            string titleKey = $"shop.{def.npcId}.title";
            long titleId = WriteEntry(collection, titleKey, def.titleEn, def.titlePt, def.titleEs);
            shop.shopTitle = BindLocalizedString(titleKey, titleId);

            shop.items = new List<ShopItemEntry>();
            foreach (var (item, price) in def.stock)
            {
                if (!validItems.Contains(item))
                {
                    Debug.LogWarning($"[PlaceholderShopTool] '{item}' is not in ItemDatabase — " +
                                     $"skipping it in {def.npcId}'s shop, where it would be unbuyable.");
                    continue;
                }

                shop.items.Add(new ShopItemEntry { itemName = item, basePrice = price });
            }

            if (isNew) AssetDatabase.CreateAsset(shop, assetPath);
            EditorUtility.SetDirty(shop);
            created++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(collection.SharedData);

        assigned = AssignShopsToNpcs();

        Debug.Log($"[PlaceholderShopTool] {created} shops written, {assigned} assigned to NPCs " +
                  "in the open scene. Save the scene (Ctrl+S).");
    }

    /// <summary>
    /// Item names that actually resolve through Resources. A ShopItemEntry pointing at a
    /// missing item produces a row the player cannot buy, with no error to explain why.
    /// </summary>
    private static HashSet<string> LoadValidItemNames()
    {
        var names = new HashSet<string>();
        foreach (var item in Resources.LoadAll<SowurShield.Inventory.Item>(""))
        {
            if (item != null && !string.IsNullOrEmpty(item.itemName))
                names.Add(item.itemName);
        }
        return names;
    }

    /// <summary>Points each merchant NPC's shopData field at its freshly written asset.</summary>
    private static int AssignShopsToNpcs()
    {
        var npcs = Object.FindObjectsByType<NPCDialogueInteractable>(FindObjectsSortMode.None);
        int assigned = 0;

        foreach (var npc in npcs)
        {
            string npcId = npc.GetNPCId();
            if (string.IsNullOrEmpty(npcId)) continue;

            bool isMerchant = false;
            foreach (var def in Shops)
            {
                if (def.npcId == npcId) { isMerchant = true; break; }
            }
            if (!isMerchant) continue;

            var shop = AssetDatabase.LoadAssetAtPath<ShopData>($"{ShopFolder}/{npcId}_Shop.asset");
            if (shop == null) continue;

            var so = new SerializedObject(npc);
            var prop = so.FindProperty("shopData");
            if (prop == null) continue;

            prop.objectReferenceValue = shop;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(npc);
            assigned++;
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return assigned;
    }

    private static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(ShopFolder)) return;
        AssetDatabase.CreateFolder("Assets/Resources", "Shops");
    }

    /// <summary>Writes one key into all three locales and returns its shared-entry id.</summary>
    private static long WriteEntry(StringTableCollection collection, string key,
                                   string en, string pt, string es)
    {
        var shared = collection.SharedData;
        var sharedEntry = shared.GetEntry(key) ?? shared.AddKey(key);
        if (sharedEntry == null) return 0;

        foreach (var table in collection.StringTables)
        {
            string code = table.LocaleIdentifier.Code;
            string value = code.StartsWith("pt") ? pt : (code.StartsWith("es") ? es : en);
            table.AddEntry(key, value);
            EditorUtility.SetDirty(table);
        }

        return sharedEntry.Id;
    }

    /// <summary>
    /// Builds a LocalizedString bound by key id. Binding by name alone leaves the id at 0,
    /// which resolves to nothing at runtime and renders an empty label — the trap documented
    /// in VillagerDialogueFactory.
    /// </summary>
    private static LocalizedString BindLocalizedString(string key, long keyId)
    {
        var localized = new LocalizedString(TableName, key);
        if (keyId != 0) localized.TableEntryReference = keyId;
        return localized;
    }
}
} // namespace SowurShield.Editor
