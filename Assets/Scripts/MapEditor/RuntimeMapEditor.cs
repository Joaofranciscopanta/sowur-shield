using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Collections;
using SowurShield.Core;
using SowurShield.Farming;

namespace SowurShield.MapEditor
{

public class RuntimeMapEditor : MonoBehaviour
{
    public static RuntimeMapEditor Instance { get; private set; }
    
    [Header("Map Editor Settings")]
    [SerializeField] private bool editorEnabled = false;
    // B de "build". Escolhida por ser uma tecla normal e livre: as de movimento e
    // acao ja estao no PlayerControls (wasd, e, k, m, tab, escape, 0-9, setas), J e o
    // painel de quests, e F1 ja e o debugKey do InventoryDebugger — apertar F1 abriria
    // as duas coisas ao mesmo tempo.
    [SerializeField] private string toggleKeyName = "b";
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

    /// <summary>O dual grid que este editor pinta. O preview precisa dele para
    /// converter a posicao do mouse em celula pelo mesmo caminho que o pincel.</summary>
    public SowurShield.Farming.DualGridTilemap DualGrid => dualGridTilemap;
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

        // gameUI NAO e descoberto automaticamente. Havia um
        // `gameUI = FindFirstObjectByType<Canvas>()` aqui, e a cena tem 11 canvases
        // ativos — o primeiro e o SellingBoxCanvas. Como ExitEditorMode faz
        // gameUI.SetActive(true), so por existir na cena o editor ABRIA a UI da
        // caixa de venda no boot. Se ninguem ligou o campo no inspector, nao mexemos
        // em canvas nenhum.

        // Estado inicial fechado, sem passar por ExitEditorMode: aquele caminho
        // mexe no jogador e na UI, e no boot nao ha nada para restaurar.
        editorEnabled = false;
        
        // Create default map data if none assigned
        if (currentMapData == null)
        {
            CreateNewMapData();
        }
        
        isInitialized = true;

    }
    
    private void HandleInput()
    {
        if (!isInitialized || keyboard == null) return;
        
        // Abre/fecha o editor (tecla em toggleKeyName)
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

        }
        
        // Undo (Ctrl+Z) - TODO: Implement undo system
        if (keyboard != null && (keyboard[Key.LeftCtrl].isPressed || keyboard[Key.RightCtrl].isPressed) && keyboard[Key.Z].wasPressedThisFrame)
        {

        }
        
        // Troca rapida de tipo. So oferecemos o que este tileset desenha de verdade:
        // o enum tem 15 valores, mas o dual grid e binario e o adaptador recusa o resto.
        // Ter 3=Water numa tecla so ensinaria o usuario a pintar sem efeito nenhum.
        if (keyboard != null)
        {
            if (keyboard[Key.Digit1].wasPressedThisFrame) selectedTileType = ExtendedTileType.Grass;
            if (keyboard[Key.Digit2].wasPressedThisFrame) selectedTileType = ExtendedTileType.Dirt;
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
        
        // A grade NAO e ligada/desligada aqui. O GridOverlay se inscreve em
        // OnEditorToggled e cuida da propria visibilidade — desativar o GameObject
        // inteiro impedia o Start() dele de rodar, entao a grade nunca chegava a ser
        // construida e nenhuma linha aparecia.
        
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
        
        // Ver EnterEditorMode: a visibilidade da grade e do proprio GridOverlay.
        
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
        

    }
    
    public void CreateNewMapData()
    {
        currentMapData = ScriptableObject.CreateInstance<MapData>();
        currentMapData.mapName = "New Map " + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        currentMapData.createdBy = System.Environment.UserName;

    }
    
    public void LoadMapData(MapData mapData)
    {
        if (mapData == null)
        {

            return;
        }
        
        currentMapData = mapData;
        
        // Apply map data to the tilemap
        ApplyMapDataToTilemap();
        
        OnMapLoaded?.Invoke(currentMapData);

    }
    
    private void ApplyMapDataToTilemap()
    {
        if (currentMapData == null || dualGridTilemap == null) return;

        // O adaptador limpa, aplica e faz UM refresh no fim. Ver DualGridPaintAdapter
        // para por que a pintura e binaria e por que grama e "celula vazia".
        DualGridPaintAdapter.Apply(dualGridTilemap, currentMapData);

        // NPCs e objetos ainda nao sao aplicados: o NPCPlacer guarda npcSpawns no
        // MapData, mas instanciar de volta precisa de um catalogo de prefabs por id,
        // que ainda nao existe. Fase 3.
    }
    
    public void SaveCurrentMap()
    {
        if (currentMapData == null)
        {

            return;
        }
        
        // Update map data from current tilemap state
        UpdateMapDataFromTilemap();
        
        // Save to assets (this will be handled by MapSerializer)
        // For now, just update metadata
        currentMapData.UpdateMetadata();
        
        OnMapSaved?.Invoke();

    }
    
    private void AutoSaveMap()
    {
        SaveCurrentMap();

    }
    
    private void UpdateMapDataFromTilemap()
    {
        if (currentMapData == null || dualGridTilemap == null) return;

        // Le a cena e reescreve o tileData. Sem isto, salvar um mapa que voce nao
        // pintou nesta sessao gravava um MapData vazio por cima do mundo existente.
        DualGridPaintAdapter.CaptureInto(dualGridTilemap, currentMapData);
    }
    
    // Tool methods for other systems
    public void SetTileAtPosition(Vector3Int position, ExtendedTileType tileType)
    {
        if (currentMapData == null) return;

        // Pinta na cena primeiro: se este tileset nao sabe desenhar o tipo pedido,
        // nao gravamos no MapData um dado que nunca vai reaparecer na tela.
        if (!DualGridPaintAdapter.Paint(dualGridTilemap, position, tileType))
            return;

        currentMapData.SetTileAt(position, tileType, selectedLayer);
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
} // namespace SowurShield.MapEditor