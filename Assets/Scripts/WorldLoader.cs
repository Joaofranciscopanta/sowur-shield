using UnityEngine;
using System.Collections;
using System.Linq;

public class WorldLoader : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject soilBlockPrefab;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    public static WorldLoader Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Load soil prefab from Resources
        if (soilBlockPrefab == null)
        {
            soilBlockPrefab = Resources.Load<GameObject>("Soil");
            if (soilBlockPrefab == null)
            {
            }
        }
        
        // Subscribe to SaveManager load events and check if already loaded
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnLoadCompleted += OnGameLoaded;
            
            // Check if game data was already loaded (in case we missed the event)
            if (SaveManager.Instance.CurrentGameData != null)
            {
                StartCoroutine(RecreateWorldObjectsCoroutine());
            }
        }
        else
        {
            StartCoroutine(WaitForSaveManager());
        }
    }
    
    private IEnumerator WaitForSaveManager()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnLoadCompleted += OnGameLoaded;
                
            // Check if game data was already loaded (in case we missed the event)
            if (SaveManager.Instance.CurrentGameData != null)
            {
                StartCoroutine(RecreateWorldObjectsCoroutine());
            }
        }
        else
        {
        }
    }
    
    private void OnGameLoaded(bool success)
    {
        if (success)
        {
            StartCoroutine(RecreateWorldObjectsCoroutine());
        }
    }
    
    private IEnumerator RecreateWorldObjectsCoroutine()
    {
        // Wait a frame for other systems to load
        yield return null;
        
        if (SaveManager.Instance?.CurrentGameData != null)
        {
            RecreateWorldObjects(SaveManager.Instance.CurrentGameData);
        }
    }
    
    private void RecreateWorldObjects(GameData gameData)
    {
        if (soilBlockPrefab == null)
        {
            return;
        }
        
        
        // Get all positions that need soil blocks
        var allPositions = new System.Collections.Generic.HashSet<Vector2Int>();
        
        // Add positions from soil states
        foreach (var soilStateEntry in gameData.farmingData.soilStates)
        {
            if (TryParsePosition(soilStateEntry.positionKey, out Vector2Int pos))
            {
                allPositions.Add(pos);
            }
        }
        
        // Add positions from crops
        foreach (var crop in gameData.farmingData.activeCrops)
        {
            allPositions.Add(crop.position);
        }
        
        // Create soil blocks for all positions
        foreach (var position in allPositions)
        {
            // Create at center of hex, like CursorController does (tilePos + 0.5f)
            Vector3 worldPos = new Vector3(position.x + 0.5f, position.y + 0.5f, 0);
            
            // Check if soil block already exists at this position
            if (!HasSoilBlockAtPosition(worldPos))
            {
                GameObject newSoilBlock = Instantiate(soilBlockPrefab, worldPos, Quaternion.identity);
                
                
                // Manually call LoadData on the newly created components since SaveManager already finished loading
                var soilComponent = newSoilBlock.GetComponent<SoilBlockInteractable>();
                if (soilComponent != null)
                {
                    soilComponent.LoadData(gameData);
                }
                
                var cropComponent = newSoilBlock.GetComponent<CropGrowthManager>();
                if (cropComponent != null)
                {
                    cropComponent.LoadData(gameData);
                }
            }
        }
    }
    
    private bool TryParsePosition(string positionKey, out Vector2Int position)
    {
        position = Vector2Int.zero;
        string[] parts = positionKey.Split(',');
        
        if (parts.Length == 2 && 
            int.TryParse(parts[0], out int x) && 
            int.TryParse(parts[1], out int y))
        {
            position = new Vector2Int(x, y);
            return true;
        }
        
        return false;
    }
    
    private bool HasSoilBlockAtPosition(Vector3 worldPos)
    {
        SoilBlockInteractable[] allSoilBlocks = FindObjectsByType<SoilBlockInteractable>(FindObjectsSortMode.None);
        
        foreach (var soilBlock in allSoilBlocks)
        {
            float distance = Vector3.Distance(soilBlock.transform.position, worldPos);
            if (distance < 0.1f)
            {
                return true;
            }
        }
        
        return false;
    }
    
    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnLoadCompleted -= OnGameLoaded;
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}