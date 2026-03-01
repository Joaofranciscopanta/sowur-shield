using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SowurShield.Farming;

namespace SowurShield.MapEditor
{

public class MapEditorUI : MonoBehaviour
{
    [Header("Main Panels")]
    [SerializeField] private GameObject mainEditorPanel;
    [SerializeField] private GameObject tilePalettePanel;
    [SerializeField] private GameObject toolsPanel;
    [SerializeField] private GameObject propertiesPanel;
    [SerializeField] private GameObject minimapPanel;
    
    [Header("Tools")]
    [SerializeField] private Button paintBrushButton;
    [SerializeField] private Button fillBrushButton;
    [SerializeField] private Button lineBrushButton;
    [SerializeField] private Button rectangleBrushButton;
    [SerializeField] private Button eraserButton;
    [SerializeField] private Button npcPlacerButton;
    
    [Header("Tile Palette")]
    [SerializeField] private Transform tileButtonsParent;
    [SerializeField] private GameObject tileButtonPrefab;
    [SerializeField] private ScrollRect tilePaletteScrollRect;
    
    [Header("Properties")]
    [SerializeField] private TextMeshProUGUI selectedTileText;
    [SerializeField] private TextMeshProUGUI selectedToolText;
    [SerializeField] private Slider brushSizeSlider;
    [SerializeField] private TextMeshProUGUI brushSizeText;
    [SerializeField] private Toggle collisionToggle;
    [SerializeField] private Dropdown layerDropdown;
    
    [Header("Map Info")]
    [SerializeField] private TextMeshProUGUI mapNameText;
    [SerializeField] private TextMeshProUGUI coordinatesText;
    [SerializeField] private TextMeshProUGUI tilesCountText;
    
    [Header("File Operations")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button newMapButton;
    [SerializeField] private Button exportButton;
    
    [Header("Minimap")]
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private RenderTexture minimapRenderTexture;
    [SerializeField] private Camera minimapCamera;
    
    [Header("Shortcuts Info")]
    [SerializeField] private GameObject shortcutsPanel;
    [SerializeField] private Button helpButton;
    
    // Runtime references
    private RuntimeMapEditor mapEditor;
    private ExtendedDualGridTilemap extendedTilemap;
    private BrushTool brushTool;
    private NPCPlacer npcPlacer;
    
    // UI state
    private List<TileButton> tileButtons = new List<TileButton>();
    private ExtendedTileType currentSelectedTile = ExtendedTileType.Grass;
    private BrushType currentSelectedTool = BrushType.Paint;
    private bool isInitialized = false;
    
    // Events
    public System.Action<ExtendedTileType> OnTileSelected;
    public System.Action<BrushType> OnToolSelected;
    public System.Action<int> OnBrushSizeChanged;
    
    private void Start()
    {
        InitializeUI();
    }
    
    private void OnEnable()
    {
        if (isInitialized)
        {
            RefreshUI();
        }
    }
    
    private void Update()
    {
        UpdateRealTimeInfo();
    }
    
    private void InitializeUI()
    {
        // Get references
        mapEditor = RuntimeMapEditor.Instance;
        extendedTilemap = FindFirstObjectByType<ExtendedDualGridTilemap>();
        brushTool = FindFirstObjectByType<BrushTool>();
        npcPlacer = FindFirstObjectByType<NPCPlacer>();
        
        // Subscribe to events
        if (mapEditor != null)
        {
            mapEditor.OnEditorToggled += OnEditorToggled;
            mapEditor.OnMapLoaded += OnMapLoaded;
            mapEditor.OnMapSaved += OnMapSaved;
        }
        
        // Setup UI elements
        SetupToolButtons();
        SetupTilePalette();
        SetupPropertiesPanel();
        SetupFileOperations();
        SetupMinimap();
        
        // Initialize with default values
        SelectTool(BrushType.Paint);
        SelectTile(ExtendedTileType.Grass);
        
        isInitialized = true;

    }
    
    private void SetupToolButtons()
    {
        if (paintBrushButton != null)
            paintBrushButton.onClick.AddListener(() => SelectTool(BrushType.Paint));
        
        if (fillBrushButton != null)
            fillBrushButton.onClick.AddListener(() => SelectTool(BrushType.Fill));
        
        if (lineBrushButton != null)
            lineBrushButton.onClick.AddListener(() => SelectTool(BrushType.Line));
        
        if (rectangleBrushButton != null)
            rectangleBrushButton.onClick.AddListener(() => SelectTool(BrushType.Rectangle));
        
        if (eraserButton != null)
            eraserButton.onClick.AddListener(() => SelectTool(BrushType.Eraser));
        
        if (npcPlacerButton != null)
            npcPlacerButton.onClick.AddListener(() => SelectNPCPlacer());
    }
    
    private void SetupTilePalette()
    {
        if (tileButtonsParent == null || tileButtonPrefab == null) return;
        
        // Clear existing buttons
        foreach (Transform child in tileButtonsParent)
        {
            Destroy(child.gameObject);
        }
        tileButtons.Clear();
        
        // Create buttons for each tile type
        ExtendedTileType[] tileTypes = System.Enum.GetValues(typeof(ExtendedTileType)) as ExtendedTileType[];
        
        foreach (ExtendedTileType tileType in tileTypes)
        {
            if (tileType == ExtendedTileType.None) continue;
            
            GameObject buttonObj = Instantiate(tileButtonPrefab, tileButtonsParent);
            TileButton tileButton = buttonObj.GetComponent<TileButton>();
            
            if (tileButton == null)
            {
                tileButton = buttonObj.AddComponent<TileButton>();
            }
            
            tileButton.Initialize(tileType, this);
            tileButtons.Add(tileButton);
        }
    }
    
    private void SetupPropertiesPanel()
    {
        if (brushSizeSlider != null)
        {
            brushSizeSlider.onValueChanged.AddListener(HandleBrushSizeChanged);
            brushSizeSlider.value = 1;
        }
        
        if (collisionToggle != null)
        {
            collisionToggle.onValueChanged.AddListener(OnCollisionToggled);
        }
        
        if (layerDropdown != null)
        {
            layerDropdown.options.Clear();
            layerDropdown.options.Add(new Dropdown.OptionData("Ground (0)"));
            layerDropdown.options.Add(new Dropdown.OptionData("Decoration (1)"));
            layerDropdown.options.Add(new Dropdown.OptionData("Objects (2)"));
            layerDropdown.onValueChanged.AddListener(OnLayerChanged);
        }
    }
    
    private void SetupFileOperations()
    {
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveMap);
        
        if (loadButton != null)
            loadButton.onClick.AddListener(ShowLoadDialog);
        
        if (newMapButton != null)
            newMapButton.onClick.AddListener(CreateNewMap);
        
        if (exportButton != null)
            exportButton.onClick.AddListener(ShowExportDialog);
        
        if (helpButton != null)
            helpButton.onClick.AddListener(ToggleHelpPanel);
    }
    
    private void SetupMinimap()
    {
        if (minimapCamera != null && minimapRenderTexture != null)
        {
            minimapCamera.targetTexture = minimapRenderTexture;
            
            if (minimapImage != null)
            {
                minimapImage.texture = minimapRenderTexture;
            }
        }
    }
    
    public void SelectTile(ExtendedTileType tileType)
    {
        currentSelectedTile = tileType;
        
        // Update visual feedback
        foreach (var button in tileButtons)
        {
            button.SetSelected(button.TileType == tileType);
        }
        
        // Update mapEditor
        if (mapEditor != null)
        {
            mapEditor.selectedTileType = tileType;
        }
        
        OnTileSelected?.Invoke(tileType);
        UpdatePropertiesDisplay();
    }
    
    public void SelectTool(BrushType brushType)
    {
        currentSelectedTool = brushType;
        
        // Update tool button visual feedback
        UpdateToolButtonSelection(brushType);
        
        // Update mapEditor
        if (mapEditor != null)
        {
            mapEditor.selectedBrush = brushType;
        }
        
        // Update brush tool
        if (brushTool != null)
        {
            brushTool.SetBrushType(brushType);
        }
        
        OnToolSelected?.Invoke(brushType);
        UpdatePropertiesDisplay();
    }
    
    private void SelectNPCPlacer()
    {
        // Toggle NPC placement mode
        if (npcPlacer != null)
        {
            npcPlacer.ToggleNPCPlacementMode();
        }
        
        UpdatePropertiesDisplay();
    }
    
    private void UpdateToolButtonSelection(BrushType selectedBrush)
    {
        // Reset all tool buttons
        SetToolButtonHighlight(paintBrushButton, selectedBrush == BrushType.Paint);
        SetToolButtonHighlight(fillBrushButton, selectedBrush == BrushType.Fill);
        SetToolButtonHighlight(lineBrushButton, selectedBrush == BrushType.Line);
        SetToolButtonHighlight(rectangleBrushButton, selectedBrush == BrushType.Rectangle);
        SetToolButtonHighlight(eraserButton, selectedBrush == BrushType.Eraser);
    }
    
    private void SetToolButtonHighlight(Button button, bool highlighted)
    {
        if (button == null) return;
        
        ColorBlock colors = button.colors;
        colors.normalColor = highlighted ? Color.yellow : Color.white;
        button.colors = colors;
    }
    
    private void HandleBrushSizeChanged(float size)
    {
        int brushSize = Mathf.RoundToInt(size);
        
        if (brushSizeText != null)
        {
            brushSizeText.text = brushSize.ToString();
        }
        
        if (brushTool != null)
        {
            brushTool.SetBrushSize(brushSize);
        }
        
        OnBrushSizeChanged?.Invoke(brushSize);
    }
    
    private void OnCollisionToggled(bool hasCollision)
    {
        // Update tile properties
        // TODO: Implement collision property system

    }
    
    private void OnLayerChanged(int layerIndex)
    {
        if (mapEditor != null)
        {
            mapEditor.selectedLayer = layerIndex;
        }

    }
    
    private void UpdatePropertiesDisplay()
    {
        if (selectedTileText != null)
        {
            selectedTileText.text = $"Tile: {currentSelectedTile}";
        }
        
        if (selectedToolText != null)
        {
            selectedToolText.text = $"Tool: {currentSelectedTool}";
        }
    }
    
    private void UpdateRealTimeInfo()
    {
        // Update coordinates based on cursor position
        if (coordinatesText != null)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int tilePos = new Vector3Int(
                Mathf.FloorToInt(mouseWorldPos.x),
                Mathf.FloorToInt(mouseWorldPos.y),
                0
            );
            coordinatesText.text = $"Pos: {tilePos.x}, {tilePos.y}";
        }
        
        // Update map info
        if (mapEditor?.CurrentMapData != null)
        {
            if (mapNameText != null)
            {
                mapNameText.text = $"Map: {mapEditor.CurrentMapData.mapName}";
            }
            
            if (tilesCountText != null)
            {
                tilesCountText.text = $"Tiles: {mapEditor.CurrentMapData.tileData.Count}";
            }
        }
    }
    
    private void RefreshUI()
    {
        UpdatePropertiesDisplay();
        UpdateToolButtonSelection(currentSelectedTool);
        
        foreach (var button in tileButtons)
        {
            button.SetSelected(button.TileType == currentSelectedTile);
        }
    }
    
    // File operations
    private void SaveMap()
    {
        if (mapEditor != null)
        {
            mapEditor.SaveCurrentMap();
        }
    }
    
    private void ShowLoadDialog()
    {
        // TODO: Implement load dialog
        // For now, just show available maps in console
        var serializer = MapSerializer.Instance;
        if (serializer != null)
        {
            var availableMaps = serializer.GetAvailableMaps();

        }
    }
    
    private void CreateNewMap()
    {
        if (mapEditor != null)
        {
            mapEditor.CreateNewMapData();
            RefreshUI();
        }
    }
    
    private void ShowExportDialog()
    {
        // TODO: Implement export dialog

    }
    
    private void ToggleHelpPanel()
    {
        if (shortcutsPanel != null)
        {
            shortcutsPanel.SetActive(!shortcutsPanel.activeInHierarchy);
        }
    }
    
    // Event handlers
    private void OnEditorToggled(bool enabled)
    {
        gameObject.SetActive(enabled);
        
        if (enabled)
        {
            RefreshUI();
        }
    }
    
    private void OnMapLoaded(MapData mapData)
    {
        RefreshUI();

    }
    
    private void OnMapSaved()
    {
        RefreshUI();
        // Could show a temporary "Saved!" message
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (mapEditor != null)
        {
            mapEditor.OnEditorToggled -= OnEditorToggled;
            mapEditor.OnMapLoaded -= OnMapLoaded;
            mapEditor.OnMapSaved -= OnMapSaved;
        }
    }
}

// Helper component for tile buttons
public class TileButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI label;
    
    private ExtendedTileType tileType;
    private MapEditorUI editorUI;
    
    public ExtendedTileType TileType => tileType;
    
    public void Initialize(ExtendedTileType type, MapEditorUI ui)
    {
        tileType = type;
        editorUI = ui;
        
        if (button == null)
            button = GetComponent<Button>();
        
        if (label != null)
            label.text = type.ToString();
        
        if (button != null)
        {
            button.onClick.AddListener(() => editorUI.SelectTile(tileType));
        }
        
        // TODO: Set icon based on tile type
        SetupIcon();
    }
    
    private void SetupIcon()
    {
        // TODO: Load icon sprite based on tile type
        // This would typically load from Resources or a TileLibrary
    }
    
    public void SetSelected(bool selected)
    {
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = selected ? Color.green : Color.white;
            button.colors = colors;
        }
    }
}
} // namespace SowurShield.MapEditor