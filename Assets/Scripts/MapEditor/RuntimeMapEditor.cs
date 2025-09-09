using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Collections;

public class RuntimeMapEditor : MonoBehaviour
{
    public static RuntimeMapEditor Instance { get; private set; }
    
    [Header("Map Editor Settings")]
    [SerializeField] private bool editorEnabled = false;
    [SerializeField] private string toggleKeyName = "f1";
    [SerializeField] private float autoSaveInterval = 30f;
    
    private Keyboard keyboard;
    private KeyControl toggleKeyControl;
    
    [Header("References")]
    [SerializeField] private MapData currentMapData;
    [SerializeField] private DualGridTilemap dualGridTilemap;
    [SerializeField] private GameObject mapEditorUI;
    [SerializeField] private Camera editorCamera;
    [SerializeField] private GameObject gridOverlay;
    
    [Header("Player & Game References")]
    [SerializeField] private PlayerMove player;
    [SerializeField] private CursorController cursorController;
    [SerializeField] private Canvas gameUI;
    
    // Editor state
    private bool isInitialized = false;
    private float lastAutoSaveTime;
    private Vector3 playerPositionBeforeEditor;
    
    // Current editor settings
    [HideInInspector] public ExtendedTileType selectedTileType = ExtendedTileType.Grass;
    [HideInInspector] public int selectedLayer = 0;
    [HideInInspector] public BrushType selectedBrush = BrushType.Paint;
    
    public bool IsEditorActive => editorEnabled;
    public MapData CurrentMapData => currentMapData;
    
    // Events
    public System.Action<bool> OnEditorToggled;
    public System.Action<MapData> OnMapLoaded;
    public System.Action OnMapSaved;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        keyboard = Keyboard.current;
        if (keyboard != null)
        {
            toggleKeyControl = keyboard[toggleKeyName] as KeyControl;
        }
        InitializeEditor();
    }
    
    private void Update()
    {
        HandleInput();
        HandleAutoSave();
    }
    
    private void InitializeEditor()
    {
        // Find references if not assigned
        if (player == null)
            player = FindFirstObjectByType<PlayerMove>();
        
        if (cursorController == null)
            cursorController = FindFirstObjectByType<CursorController>();
        
        if (dualGridTilemap == null)
            dualGridTilemap = FindFirstObjectByType<DualGridTilemap>();
        
        if (gameUI == null)
            gameUI = FindFirstObjectByType<Canvas>();
        
        // Initialize with editor disabled
        SetEditorMode(false);
        
        // Create default map data if none assigned
        if (currentMapData == null)
        {
            CreateNewMapData();
        }
        
        isInitialized = true;
        Debug.Log("RuntimeMapEditor initialized successfully");
    }
    
    private void HandleInput()
    {
        if (!isInitialized || keyboard == null) return;
        
        // Toggle editor with F1 key
        if (toggleKeyControl != null && toggleKeyControl.wasPressedThisFrame)
        {
            ToggleEditor();
        }
        
        // Additional editor shortcuts when active
        if (editorEnabled)
        {
            HandleEditorInput();
        }
    }
    
    private void HandleEditorInput()
    {
        // Save shortcut (Ctrl+S)
        if (keyboard != null && (keyboard[Key.LeftCtrl].isPressed || keyboard[Key.RightCtrl].isPressed) && keyboard[Key.S].wasPressedThisFrame)
        {
            SaveCurrentMap();
        }
        
        // Load shortcut (Ctrl+O)
        if (keyboard != null && (keyboard[Key.LeftCtrl].isPressed || keyboard[Key.RightCtrl].isPressed) && keyboard[Key.O].wasPressedThisFrame)
        {
            // TODO: Implement load dialog
            Debug.Log("Load map dialog - TODO");
        }
        
        // Undo (Ctrl+Z) - TODO: Implement undo system
        if (keyboard != null && (keyboard[Key.LeftCtrl].isPressed || keyboard[Key.RightCtrl].isPressed) && keyboard[Key.Z].wasPressedThisFrame)
        {
            Debug.Log("Undo - TODO");
        }
        
        // Quick tile type switching (1-5 keys)
        if (keyboard != null)
        {
            if (keyboard[Key.Digit1].wasPressedThisFrame) selectedTileType = ExtendedTileType.Grass;
            if (keyboard[Key.Digit2].wasPressedThisFrame) selectedTileType = ExtendedTileType.Dirt;
            if (keyboard[Key.Digit3].wasPressedThisFrame) selectedTileType = ExtendedTileType.Water;
            if (keyboard[Key.Digit4].wasPressedThisFrame) selectedTileType = ExtendedTileType.Stone;
            if (keyboard[Key.Digit5].wasPressedThisFrame) selectedTileType = ExtendedTileType.Sand;
        }
    }
    
    private void HandleAutoSave()
    {
        if (editorEnabled && Time.time - lastAutoSaveTime >= autoSaveInterval)
        {
            AutoSaveMap();
            lastAutoSaveTime = Time.time;
        }
    }
    
    public void ToggleEditor()
    {
        SetEditorMode(!editorEnabled);
    }
    
    public void SetEditorMode(bool enabled)
    {
        editorEnabled = enabled;
        
        if (enabled)
        {
            EnterEditorMode();
        }
        else
        {
            ExitEditorMode();
        }
        
        OnEditorToggled?.Invoke(editorEnabled);
        Debug.Log($"Map Editor {(enabled ? "ENABLED" : "DISABLED")}");
    }
    
    private void EnterEditorMode()
    {
        // Store player position
        if (player != null)
        {
            playerPositionBeforeEditor = player.transform.position;
            player.DisableMovement();
        }
        
        // Enable cursor controller for tile editing
        if (cursorController != null)
        {
            cursorController.enabled = true;
        }
        
        // Show editor UI
        if (mapEditorUI != null)
        {
            mapEditorUI.SetActive(true);
        }
        
        // Show grid overlay
        if (gridOverlay != null)
        {
            gridOverlay.SetActive(true);
        }
        
        // Hide game UI
        if (gameUI != null)
        {
            gameUI.gameObject.SetActive(false);
        }
        
        // Enable editor camera if available
        if (editorCamera != null)
        {
            editorCamera.gameObject.SetActive(true);
        }
        
        // Set cursor to visible and unlocked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Pause any time-based systems
        Time.timeScale = 1f; // Keep normal for editor responsiveness
        
        Debug.Log("Entered Map Editor Mode");
    }
    
    private void ExitEditorMode()
    {
        // Re-enable player movement
        if (player != null)
        {
            player.EnableMovement();
            // Optionally restore player position
            // player.transform.position = playerPositionBeforeEditor;
        }
        
        // Disable cursor controller
        if (cursorController != null)
        {
            // Don't disable completely, just make it not interfere with dialogue
            // cursorController.enabled = false;
        }
        
        // Hide editor UI
        if (mapEditorUI != null)
        {
            mapEditorUI.SetActive(false);
        }
        
        // Hide grid overlay
        if (gridOverlay != null)
        {
            gridOverlay.SetActive(false);
        }
        
        // Show game UI
        if (gameUI != null)
        {
            gameUI.gameObject.SetActive(true);
        }
        
        // Disable editor camera
        if (editorCamera != null)
        {
            editorCamera.gameObject.SetActive(false);
        }
        
        // Resume normal game cursor settings
        // This will be handled by the game's normal systems
        
        Debug.Log("Exited Map Editor Mode");
    }
    
    public void CreateNewMapData()
    {
        currentMapData = ScriptableObject.CreateInstance<MapData>();
        currentMapData.mapName = "New Map " + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        currentMapData.createdBy = System.Environment.UserName;
        Debug.Log($"Created new map data: {currentMapData.mapName}");
    }
    
    public void LoadMapData(MapData mapData)
    {
        if (mapData == null)
        {
            Debug.LogError("Cannot load null map data!");
            return;
        }
        
        currentMapData = mapData;
        
        // Apply map data to the tilemap
        ApplyMapDataToTilemap();
        
        OnMapLoaded?.Invoke(currentMapData);
        Debug.Log($"Loaded map: {currentMapData.mapName}");
    }
    
    private void ApplyMapDataToTilemap()
    {
        if (currentMapData == null || dualGridTilemap == null) return;
        
        // Clear existing tiles
        // TODO: Implement clear method in DualGridTilemap
        
        // Apply tile data
        foreach (var tileEntry in currentMapData.tileData)
        {
            // TODO: Convert ExtendedTileType to appropriate tile and apply
            // This will need the extended DualGridTilemap system
        }
        
        // Apply NPC spawns, objects, etc.
        // TODO: Implement object spawning
        
        Debug.Log($"Applied {currentMapData.tileData.Count} tiles from map data");
    }
    
    public void SaveCurrentMap()
    {
        if (currentMapData == null)
        {
            Debug.LogError("No map data to save!");
            return;
        }
        
        // Update map data from current tilemap state
        UpdateMapDataFromTilemap();
        
        // Save to assets (this will be handled by MapSerializer)
        // For now, just update metadata
        currentMapData.UpdateMetadata();
        
        OnMapSaved?.Invoke();
        Debug.Log($"Saved map: {currentMapData.mapName}");
    }
    
    private void AutoSaveMap()
    {
        SaveCurrentMap();
        Debug.Log($"Auto-saved map at {System.DateTime.Now:HH:mm:ss}");
    }
    
    private void UpdateMapDataFromTilemap()
    {
        if (currentMapData == null) return;
        
        // TODO: Scan current tilemap state and update MapData
        // This will need integration with extended DualGridTilemap
        
        Debug.Log("Updated map data from current tilemap state");
    }
    
    // Tool methods for other systems
    public void SetTileAtPosition(Vector3Int position, ExtendedTileType tileType)
    {
        if (currentMapData == null) return;
        
        currentMapData.SetTileAt(position, tileType, selectedLayer);
        
        // Apply to visual tilemap
        // TODO: Convert ExtendedTileType to actual tile and apply via DualGridTilemap
    }
    
    public ExtendedTileType GetTileAtPosition(Vector3Int position)
    {
        if (currentMapData == null) return ExtendedTileType.None;
        
        var tileData = currentMapData.GetTileAt(position);
        return tileData?.tileType ?? ExtendedTileType.None;
    }
    
    // Validation and utility methods
    private void OnValidate()
    {
        if (autoSaveInterval < 10f)
            autoSaveInterval = 10f;
    }
    
    private void OnDestroy()
    {
        if (editorEnabled)
        {
            // Auto-save before destroying
            SaveCurrentMap();
        }
    }
}

public enum BrushType
{
    Paint,
    Fill,
    Line,
    Rectangle,
    Circle,
    Eraser
}