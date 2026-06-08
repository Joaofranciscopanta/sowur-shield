using UnityEngine;
using UnityEditor;
using SowurShield.Animals;

namespace SowurShield.Editor
{

/// <summary>
/// Editor tool that creates the 4th animal: Rabbit (Winter).
/// Also creates a matching RabbitFur item asset.
///
/// RUN: Tools > Sowur Shield > Create Animal Assets
///
/// After running:
///   1. Assign Rabbit sprite to Assets/Resources/Animals/Rabbit.asset → idleSprite
///   2. Assign icon sprite to Assets/Resources/Items/RabbitFur.asset → itemIcon
///   3. Create RabbitFur_GroundItem prefab in Resources/Prefabs/GroundItems/
///      and assign to Rabbit.asset → groundItemPrefab
/// </summary>
public static class AnimalCreatorTool
{
    [MenuItem("Tools/Sowur Shield/Create Animal Assets")]
    public static void CreateAnimalAssets()
    {
        CreateRabbit();
        CreateRabbitFurItem();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AnimalCreatorTool] Rabbit + RabbitFur created. Assign sprites in Inspector.");
    }

    private static void CreateRabbit()
    {
        string path = "Assets/Resources/Animals/Rabbit.asset";

        // Don't overwrite if it already exists
        if (AssetDatabase.LoadAssetAtPath<AnimalData>(path) != null)
        {
            Debug.Log("[AnimalCreatorTool] Rabbit.asset already exists — skipping.");
            return;
        }

        AnimalData rabbit = ScriptableObject.CreateInstance<AnimalData>();
        rabbit.animalName  = "Rabbit";
        rabbit.animalType  = "Rabbit";
        rabbit.animalClass = "Mammal";
        rabbit.animalFamily = "Leporidae";

        // Feeding — rabbits eat carrots
        rabbit.dailyFoodRequirements = new System.Collections.Generic.List<FoodRequirement>
        {
            new FoodRequirement { itemName = "Carrot", quantityPerDay = 1 }
        };

        // Production — drops Rabbit Fur every 3 days
        rabbit.canProduce          = true;
        rabbit.produceItemName     = "Rabbit Fur";
        rabbit.produceOnlyIfFed    = true;
        rabbit.productionIntervalDays = 3;
        rabbit.minProduceAmount    = 1;
        rabbit.maxProduceAmount    = 2;
        rabbit.happinessProductionBonus = 0.5f; // +1 when happy

        // Stats
        rabbit.pettingStatBonus    = 0.01f;
        rabbit.feedingStatBonus    = 0.02f;

        // Seasonal — Winter animal
        rabbit.preferredSeason     = "Winter";
        rabbit.seasonalAttackBonus  = 1.2f;
        rabbit.seasonalDefenseBonus = 1.15f;
        rabbit.seasonalSpeedBonus   = 1.25f; // Rabbits are fast

        // Illness
        rabbit.illnessThresholdDays = 3;
        rabbit.illnessCureItemName  = "Medicine";
        rabbit.illnessStatPenalty   = 0.5f;

        // Base combat stats (nimble, low health)
        rabbit.baseCombatStats = new AnimalCombatStats
        {
            baseAttack  = 8f,
            baseDefense = 5f,
            baseSpeed   = 18f, // Fastest base speed
            baseHealth  = 60f
        };

        AssetDatabase.CreateAsset(rabbit, path);
        Debug.Log($"[AnimalCreatorTool] Created {path}");
    }

    private static void CreateRabbitFurItem()
    {
        // We need an Item ScriptableObject; check what Item looks like
        string path = "Assets/Resources/Items/RabbitFur.asset";

        if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null)
        {
            Debug.Log("[AnimalCreatorTool] RabbitFur.asset already exists — skipping.");
            return;
        }

        // Look for the Item type
        var itemType = System.Type.GetType("SowurShield.Inventory.Item, SowurShield.Runtime");
        if (itemType == null)
        {
            // Try finding it via all assemblies
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                itemType = asm.GetType("SowurShield.Inventory.Item");
                if (itemType != null) break;
            }
        }

        if (itemType == null || !typeof(ScriptableObject).IsAssignableFrom(itemType))
        {
            Debug.LogError("[AnimalCreatorTool] Cannot find SowurShield.Inventory.Item type — create RabbitFur.asset manually.");
            return;
        }

        ScriptableObject item = ScriptableObject.CreateInstance(itemType);

        // Set fields via reflection — match InventoryItem.Item field names exactly
        SetField(item, "itemName",    "Rabbit Fur");
        SetField(item, "description", "Soft fur harvested from a happy rabbit. Used in crafting.");
        SetField(item, "baseValue",   5);
        SetField(item, "isStackable", true);
        SetField(item, "maxStackSize", 99);

        AssetDatabase.CreateAsset(item, path);
        Debug.Log($"[AnimalCreatorTool] Created {path}");
    }

    private static void SetField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }
}

} // namespace SowurShield.Editor
