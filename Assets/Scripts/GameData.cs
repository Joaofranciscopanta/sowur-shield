using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SowurShield.Inventory;

namespace SowurShield.Core
{

/// <summary>
/// Master data structure containing all persistent game state
/// This class holds all data that needs to be saved and loaded
/// </summary>
[System.Serializable]
public class GameData
{
    [Header("Save Metadata")]
    public int saveVersion = 1;
    public const int CURRENT_SAVE_VERSION = 1;
    public string saveTimestamp;
    public float totalPlayTime = 0f;
    public int saveCount = 0;

    [Header("Player Data")]
    public PlayerGameData playerData;

    [Header("World Data")]
    public WorldGameData worldData;

    [Header("Time Data")]
    public TimeGameData timeData;

    [Header("Inventory Data")]
    public InventoryGameData inventoryData;

    [Header("Farming Data")]
    public FarmingGameData farmingData;

    [Header("Relationship Data")]
    public RelationshipGameData relationshipData;

    [Header("Combat Data")]
    public CombatGameData combatData;

    [Header("Progress Data")]
    public ProgressGameData progressData;

    public GameData()
    {
        // Initialize all data structures
        playerData = new PlayerGameData();
        worldData = new WorldGameData();
        timeData = new TimeGameData();
        inventoryData = new InventoryGameData();
        farmingData = new FarmingGameData();
        relationshipData = new RelationshipGameData();
        combatData = new CombatGameData();
        progressData = new ProgressGameData();

        // Set metadata
        saveTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

// ============================================================================
// PLAYER DATA
// ============================================================================

[System.Serializable]
public class PlayerGameData
{
    [Header("Position & Movement")]
    public Vector3 position = Vector3.zero;
    public float rotation = 0f;
    public string currentSceneName = "";

    [Header("Bed Spawn")]
    public Vector3 lastBedPosition = Vector3.zero;
    public bool hasSleptInBed = false;

    [Header("Stats")]
    public float health = 100f;
    public float maxHealth = 100f;
    public float energy = 100f;
    public float maxEnergy = 100f;
    public int playerLevel = 1;
    public float experience = 0f;
    public float experienceToNextLevel = 100f;

    [Header("Currency")]
    public int money = 100;
    public int premiumCurrency = 0;

    [Header("Skills")]
    public Dictionary<string, int> skillLevels = new Dictionary<string, int>();
    public Dictionary<string, float> skillExperience = new Dictionary<string, float>();

    public PlayerGameData()
    {
        // Initialize default skills
        skillLevels["Farming"] = 1;
        skillLevels["Combat"] = 1;
        skillLevels["Social"] = 1;
        skillLevels["Cooking"] = 1;

        skillExperience["Farming"] = 0f;
        skillExperience["Combat"] = 0f;
        skillExperience["Social"] = 0f;
        skillExperience["Cooking"] = 0f;
    }
}

// ============================================================================
// WORLD DATA
// ============================================================================

[System.Serializable]
public class WorldGameData
{
    [Header("World State")]
    public Dictionary<string, bool> worldFlags = new Dictionary<string, bool>();
    public Dictionary<string, int> worldCounters = new Dictionary<string, int>();
    public Dictionary<string, string> worldStrings = new Dictionary<string, string>();

    [Header("Discovered Locations")]
    public List<string> discoveredAreas = new List<string>();
    public List<string> unlockedFeatures = new List<string>();

    [Header("Weather")]
    public string currentWeather = "Sunny";
    public int weatherDuration = 0;

    public WorldGameData()
    {
        // Initialize with starting area
        discoveredAreas.Add("Farm");
    }
}

// ============================================================================
// TIME DATA
// ============================================================================

[System.Serializable]
public class TimeGameData
{
    [Header("Current Time")]
    public int currentDay = 1;
    public float dayProgress = 0.25f; // 6:00 AM start
    public string season = "Spring";
    public int year = 1;

    [Header("Time Settings")]
    public bool timeFlowEnabled = true;
    public float minutesPerRealSecond = 10f;

    [Header("Special Events")]
    public Dictionary<string, bool> completedEvents = new Dictionary<string, bool>();
    public List<string> scheduledEvents = new List<string>();
}

// ============================================================================
// INVENTORY DATA
// ============================================================================

[System.Serializable]
public class InventoryGameData
{
    [Header("Inventory")]
    public List<ItemStackData> inventoryItems = new List<ItemStackData>();
    public int selectedSlotIndex = 0;
    public int inventorySize = 36;

    [Header("Storage")]
    public Dictionary<string, List<ItemStackData>> storageContainers = new Dictionary<string, List<ItemStackData>>();

    [System.Serializable]
    public class ItemStackData
    {
        public string itemName = "";
        public int quantity = 0;
        public Dictionary<string, string> itemData = new Dictionary<string, string>(); // For special item properties

        public ItemStackData() { }

        public ItemStackData(string itemName, int quantity)
        {
            this.itemName = itemName;
            this.quantity = quantity;
        }

        // Convert from existing ItemStack
        public ItemStackData(ItemStack itemStack)
        {
            if (!itemStack.IsEmpty)
            {
                itemName = itemStack.item.itemName;
                quantity = itemStack.quantity;
            }
        }

        public bool IsEmpty => string.IsNullOrEmpty(itemName) || quantity <= 0;
    }

    public InventoryGameData()
    {
        // Initialize with empty inventory
        for (int i = 0; i < inventorySize; i++)
        {
            inventoryItems.Add(new ItemStackData());
        }
    }
}

// ============================================================================
// FARMING DATA
// ============================================================================

[System.Serializable]
public class FarmingGameData
{
    [Header("Crops")]
    public List<CropSaveData> activeCrops = new List<CropSaveData>();

    [Header("Soil State")]
    public List<SoilStateEntry> soilStates = new List<SoilStateEntry>();

    [Header("Farm Buildings")]
    public List<BuildingData> farmBuildings = new List<BuildingData>();

    [System.Serializable]
    public class CropSaveData
    {
        public Vector2Int position;
        public string cropType = "";
        public int growthStage = 0;
        public float growthProgress = 0f;
        public bool isWatered = false;
        public bool isReady = false;
        public int quality = 1; // 1-5 star quality system
        public string plantedDate = "";
    }

    [System.Serializable]
    public class SoilStateEntry
    {
        public string positionKey = "";
        public SoilData soilData = new SoilData();
    }

    [System.Serializable]
    public class SoilData
    {
        public bool isTilled = false;
        public bool isWatered = false;
        public float fertility = 1.0f;
        public string soilType = "Normal";
        // Simplified nutrients - no Dictionary for JSON serialization
        public float nitrogen = 1.0f;
        public float phosphorus = 1.0f;
        public float potassium = 1.0f;
    }

    [System.Serializable]
    public class BuildingData
    {
        public string buildingType = "";
        public Vector2Int position;
        public int level = 1;
        public Dictionary<string, string> buildingData = new Dictionary<string, string>();
        public bool isConstructed = false;
    }
}

// ============================================================================
// RELATIONSHIP DATA
// ============================================================================

[System.Serializable]
public class RelationshipGameData
{
    [Header("NPC Relationships")]
    public Dictionary<string, NPCRelationshipData> npcRelationships = new Dictionary<string, NPCRelationshipData>();

    [Header("Dialogue Progress")]
    public Dictionary<string, DialogueProgressData> dialogueProgress = new Dictionary<string, DialogueProgressData>();

    [Header("Romance")]
    public string currentRomancePartner = "";
    public Dictionary<string, bool> romanceFlags = new Dictionary<string, bool>();

    [System.Serializable]
    public class NPCRelationshipData
    {
        public string npcName = "";
        public int friendshipLevel = 0;
        public float friendshipPoints = 0f;
        public int romanceLevel = 0;
        public float romancePoints = 0f;
        public bool isRomanceable = false;
        public bool isMarried = false;
        public Dictionary<string, bool> relationshipFlags = new Dictionary<string, bool>();
        public List<string> receivedGifts = new List<string>(); // Track daily gifts
        public string lastGiftDate = "";
    }

    [System.Serializable]
    public class DialogueProgressData
    {
        public string npcName = "";
        public List<string> completedDialogues = new List<string>();
        public Dictionary<string, int> dialogueChoiceHistory = new Dictionary<string, int>();
        public string lastDialogueDate = "";
    }
}

// ============================================================================
// COMBAT DATA
// ============================================================================

[System.Serializable]
public class CombatGameData
{
    [Header("Animals")]
    public List<AnimalSaveData> ownedAnimals = new List<AnimalSaveData>();

    [Header("Combat Stats")]
    public int battlesWon = 0;
    public int battlesLost = 0;
    public int enemiesDefeated = 0;
    public int farmDefended = 0;

    [Header("Combat Unlocks")]
    public List<string> unlockedAnimalTypes = new List<string>();
    public Dictionary<string, bool> combatFeatures = new Dictionary<string, bool>();

    [System.Serializable]
    public class AnimalSaveData
    {
        public string animalId = "";
        public string animalType = "";
        public string animalName = "";
        public int level = 1;
        public float experience = 0f;
        public float health = 100f;
        public float maxHealth = 100f;
        public Dictionary<string, float> stats = new Dictionary<string, float>(); // attack, defense, speed, etc.
        public List<string> abilities = new List<string>();
        public Dictionary<string, string> animalData = new Dictionary<string, string>(); // special properties
        public bool isActive = true;
        public Vector2Int barnPosition = Vector2Int.zero;

        public AnimalSaveData()
        {
            animalId = System.Guid.NewGuid().ToString();

            // Initialize base stats
            stats["attack"] = 10f;
            stats["defense"] = 10f;
            stats["speed"] = 10f;
            stats["accuracy"] = 0.8f;
        }
    }
}

// ============================================================================
// PROGRESS DATA
// ============================================================================

[System.Serializable]
public class ProgressGameData
{
    [Header("Achievements")]
    public Dictionary<string, bool> achievementsUnlocked = new Dictionary<string, bool>();
    public Dictionary<string, float> achievementProgress = new Dictionary<string, float>();

    [Header("Game Milestones")]
    public Dictionary<string, bool> milestonesReached = new Dictionary<string, bool>();
    public Dictionary<string, int> statisticCounters = new Dictionary<string, int>();

    [Header("Tutorial Progress")]
    public Dictionary<string, bool> tutorialSteps = new Dictionary<string, bool>();
    public bool hasCompletedTutorial = false;

    [Header("Settings")]
    public Dictionary<string, float> gameSettings = new Dictionary<string, float>();
    public Dictionary<string, bool> gamePreferences = new Dictionary<string, bool>();

    public ProgressGameData()
    {
        // Initialize default settings
        gameSettings["masterVolume"] = 1.0f;
        gameSettings["musicVolume"] = 0.8f;
        gameSettings["sfxVolume"] = 1.0f;

        gamePreferences["showTutorialTips"] = true;
        gamePreferences["autoSave"] = true;

        // Initialize stat counters
        statisticCounters["cropsHarvested"] = 0;
        statisticCounters["itemsCrafted"] = 0;
        statisticCounters["distanceTraveled"] = 0;
        statisticCounters["animalsRaised"] = 0;
    }
}

// ============================================================================
// UTILITY METHODS
// ============================================================================

public static class GameDataExtensions
{
    /// <summary>
    /// Get a world flag value, returns false if not found
    /// </summary>
    public static bool GetWorldFlag(this GameData gameData, string flagName)
    {
        return gameData.worldData.worldFlags.ContainsKey(flagName) && gameData.worldData.worldFlags[flagName];
    }

    /// <summary>
    /// Set a world flag value
    /// </summary>
    public static void SetWorldFlag(this GameData gameData, string flagName, bool value)
    {
        gameData.worldData.worldFlags[flagName] = value;
    }

    /// <summary>
    /// Get a world counter value, returns 0 if not found
    /// </summary>
    public static int GetWorldCounter(this GameData gameData, string counterName)
    {
        return gameData.worldData.worldCounters.ContainsKey(counterName) ? gameData.worldData.worldCounters[counterName] : 0;
    }

    /// <summary>
    /// Set a world counter value
    /// </summary>
    public static void SetWorldCounter(this GameData gameData, string counterName, int value)
    {
        gameData.worldData.worldCounters[counterName] = value;
    }

    /// <summary>
    /// Increment a world counter
    /// </summary>
    public static void IncrementWorldCounter(this GameData gameData, string counterName, int amount = 1)
    {
        int currentValue = gameData.GetWorldCounter(counterName);
        gameData.SetWorldCounter(counterName, currentValue + amount);
    }
}

} // namespace SowurShield.Core
