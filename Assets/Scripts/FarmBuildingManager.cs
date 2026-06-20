using UnityEngine;
using System.Collections.Generic;
using SowurShield.Inventory;

namespace SowurShield.Core
{

/// <summary>
/// Singleton that tracks which farm buildings have been constructed.
/// Other systems (SoilBlockInteractable, AnimalZone) query IsBuilt() to
/// modify their behaviour accordingly.
///
/// Save key: worldFlags["building_{BuildingType}"] = true/false
///
/// SETUP IN UNITY:
///   Add this MonoBehaviour to any persistent GameObject in SampleScene.
/// </summary>
public class FarmBuildingManager : MonoBehaviour, ISaveable
{
    public static FarmBuildingManager Instance { get; private set; }

    // Events
    public System.Action<BuildingType> OnBuildingConstructed;

    private readonly HashSet<BuildingType> _builtBuildings = new HashSet<BuildingType>();
    private readonly Dictionary<BuildingType, GameObject> _spawnedWorldObjects = new Dictionary<BuildingType, GameObject>();

    private const string SAVE_PREFIX = "building_";

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (SaveManager.Instance != null)
                SaveManager.Instance.RegisterSaveable(this);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (Instance == this && SaveManager.Instance != null)
            SaveManager.Instance.RegisterSaveable(this);
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.UnregisterSaveable(this);
    }

    // =========================================================================
    // Public API
    // =========================================================================

    public bool IsBuilt(BuildingType type) => _builtBuildings.Contains(type);

    /// <summary>
    /// Attempt to construct a building. Deducts gold and material cost on success.
    /// Returns false if already built, insufficient funds, or missing materials.
    /// </summary>
    public bool TryBuild(FarmBuildingData data, PlayerStats playerStats, Inventory.Inventory inventory)
    {
        if (data == null) return false;

        if (_builtBuildings.Contains(data.buildingType))
        {
            Debug.LogWarning($"[FarmBuildingManager] '{data.buildingName}' is already built.");
            return false;
        }

        // Check gold
        if (!playerStats.HasMoney(data.goldCost))
        {
            Debug.LogWarning($"[FarmBuildingManager] Not enough gold for '{data.buildingName}'.");
            return false;
        }

        // Check material
        if (!string.IsNullOrEmpty(data.materialItemName) && data.materialQuantity > 0)
        {
            Item mat = ItemDatabase.GetItem(data.materialItemName);
            if (mat == null || !inventory.HasItem(mat, data.materialQuantity))
            {
                Debug.LogWarning($"[FarmBuildingManager] Missing materials for '{data.buildingName}'.");
                return false;
            }
            inventory.RemoveItem(mat, data.materialQuantity);
        }

        // Deduct gold and build
        playerStats.SpendMoney(data.goldCost);
        _builtBuildings.Add(data.buildingType);

        SpawnWorldObject(data);

        OnBuildingConstructed?.Invoke(data.buildingType);
        return true;
    }

    /// <summary>
    /// Instantiates data.worldPrefab at data.worldPosition if configured, and not already
    /// spawned for this building type. No-op for buildings with no worldPrefab assigned.
    /// </summary>
    private void SpawnWorldObject(FarmBuildingData data)
    {
        if (data == null || data.worldPrefab == null) return;
        if (_spawnedWorldObjects.TryGetValue(data.buildingType, out GameObject existing) && existing != null) return;

        GameObject instance = Instantiate(data.worldPrefab, data.worldPosition, Quaternion.identity);
        instance.name = $"{data.buildingType}_Building";
        _spawnedWorldObjects[data.buildingType] = instance;
    }

    /// <summary>
    /// Looks up the FarmBuildingData asset for a given type from Resources/Buildings
    /// (same source BuildingShopUI reads from) and spawns its world object if not present.
    /// Used by LoadData() to restore world objects for buildings built in a previous session.
    /// </summary>
    private void SpawnWorldObjectIfBuilt(BuildingType type)
    {
        if (!_builtBuildings.Contains(type)) return;

        FarmBuildingData[] allBuildings = Resources.LoadAll<FarmBuildingData>("Buildings");
        foreach (FarmBuildingData data in allBuildings)
        {
            if (data.buildingType == type)
            {
                SpawnWorldObject(data);
                return;
            }
        }
    }

    // =========================================================================
    // Convenience queries
    // =========================================================================

    /// <summary>Greenhouse lets crops grow in any season.</summary>
    public bool HasGreenhouse => IsBuilt(BuildingType.Greenhouse);

    /// <summary>Barn doubles the animal zone capacity (default 5 → 10).</summary>
    public bool HasBarn => IsBuilt(BuildingType.Barn);

    /// <summary>Silo lets harvesting one ready crop harvest all other ready crops on the farm.</summary>
    public bool HasHarvestAllUpgrade => IsBuilt(BuildingType.Silo);

    /// <summary>Workshop gives a chance to refund the planted seed when harvesting.</summary>
    public bool HasLuckySeedUpgrade => IsBuilt(BuildingType.Workshop);

    // =========================================================================
    // ISaveable
    // =========================================================================

    public void SaveData(GameData gameData)
    {
        if (gameData?.worldData == null) return;

        foreach (BuildingType type in System.Enum.GetValues(typeof(BuildingType)))
        {
            gameData.worldData.worldFlags[SAVE_PREFIX + type.ToString()] =
                _builtBuildings.Contains(type);
        }
    }

    public void LoadData(GameData gameData)
    {
        if (gameData?.worldData == null) return;

        _builtBuildings.Clear();
        var flags = gameData.worldData.worldFlags;

        foreach (BuildingType type in System.Enum.GetValues(typeof(BuildingType)))
        {
            string key = SAVE_PREFIX + type.ToString();
            if (flags.TryGetValue(key, out bool built) && built)
                _builtBuildings.Add(type);
        }

        // Restore world objects for already-built buildings — needed both on the initial
        // load and whenever SampleScene is reloaded (e.g. returning from CombatScene),
        // since the previously spawned instances are destroyed along with the old scene.
        foreach (BuildingType type in _builtBuildings)
            SpawnWorldObjectIfBuilt(type);
    }
}

} // namespace SowurShield.Core
