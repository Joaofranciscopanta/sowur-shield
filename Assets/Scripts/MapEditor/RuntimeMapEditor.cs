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
    
    private Keyboard keyboard;
    private KeyControl toggleKeyControl;
    
    [Header("References")]
    [SerializeField] private MapData currentMapData;
    [SerializeField] private DualGridTilemap dualGridTilemap;
    [SerializeField] private GameObject mapEditorUI;
    [SerializeField] private Camera editorCamera;
    [SerializeField] private GameObject gridOverlay;
    [SerializeField] private MapSerializer mapSerializer;
    
    [Header("Player & Game References")]
    [SerializeField] private PlayerMove player;
    [SerializeField] private CursorController cursorController;
    [SerializeField] private Canvas gameUI;
    
    // Editor state
    private bool isInitialized = false;
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

        // Sem autosave aqui: quem faz isso e o MapSerializer, a cada 60s, gravando
        // "<mapa>_autosave_HH-mm" com rotacao de 5 copias. Havia um segundo autosave
        // neste componente chamando SaveCurrentMap a cada 30s — ele enchia
        // Assets/Maps de "New Map <data>.asset", um por sessao e sem limite, e ainda
        // gravava por cima do arquivo do usuario sem ele ter pedido.
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
        // Nada de Ctrl+S / Ctrl+Z / Ctrl+O aqui. No Play Mode do Unity a Game View
        // disputa o foco com o resto do Editor, e um atalho com Ctrl as vezes vai
        // parar na janela errada — o usuario aperta e nada acontece, sem saber por
        // que. Salvar, desfazer e refazer sao BOTOES na paleta (MapEditorPalette).
        //
        // As teclas 1 e 2 ficam: nao usam modificador, entao nao competem com o
        // Editor, e ja existem como botao na paleta de qualquer forma.

        // Troca rapida de tipo. So oferecemos o que este tileset desenha de verdade:
        // o enum tem 15 valores, mas o dual grid e binario e o adaptador recusa o resto.
        // Ter 3=Water numa tecla so ensinaria o usuario a pintar sem efeito nenhum.
        if (keyboard != null)
        {
            if (keyboard[Key.Digit1].wasPressedThisFrame) selectedTileType = ExtendedTileType.Grass;
            if (keyboard[Key.Digit2].wasPressedThisFrame) selectedTileType = ExtendedTileType.Dirt;
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

        RecriarObjetos();
    }

    /// <summary>
    /// Instancia os objetos e NPCs gravados no mapa.
    ///
    /// Ate agora isto nao existia: o NPCPlacer gravava `npcSpawns` e nada os trazia
    /// de volta, entao carregar um mapa restaurava o chao e perdia tudo o que
    /// estivesse em cima dele. O PrefabCatalog resolve o `prefabPath` gravado.
    /// </summary>
    private void RecriarObjetos()
    {
        if (currentMapData == null) return;

        // Um pai unico: sem isto, recarregar o mapa empilharia copias soltas pela
        // hierarquia e nao haveria como limpar as anteriores.
        var raiz = GameObject.Find(RaizDeObjetosDoMapa);
        if (raiz != null) DestroyImmediate(raiz);
        raiz = new GameObject(RaizDeObjetosDoMapa);

        int recriados = 0, perdidos = 0;

        foreach (var obj in currentMapData.objectSpawns)
        {
            if (!obj.isActive) continue;
            var prefab = PrefabCatalog.Resolver(obj.prefabPath);
            if (prefab == null) { perdidos++; continue; }

            var instancia = Instantiate(prefab, obj.position, Quaternion.Euler(obj.rotation), raiz.transform);
            instancia.transform.localScale = obj.scale;
            recriados++;
        }

        foreach (var npc in currentMapData.npcSpawns)
        {
            if (!npc.isActive) continue;
            var prefab = PrefabCatalog.Resolver(npc.npcPrefabPath);
            if (prefab == null) { perdidos++; continue; }

            var instancia = Instantiate(prefab, npc.position,
                Quaternion.Euler(0f, 0f, npc.rotation), raiz.transform);
            if (!string.IsNullOrEmpty(npc.npcName)) instancia.name = npc.npcName;
            recriados++;
        }

        // Um prefab movido ou apagado desde que o mapa foi salvo some em silencio se
        // ninguem avisar — e o usuario acha que o editor perdeu o trabalho dele.
        if (perdidos > 0)
        {
            Debug.LogWarning($"[MapEditor] {perdidos} objeto(s) do mapa nao foram " +
                             "encontrados: o prefab foi movido ou apagado desde que " +
                             "o mapa foi salvo.");
        }
    }

    /// <summary>Nome do GameObject que agrupa o que o mapa instancia.</summary>
    public const string RaizDeObjetosDoMapa = "MapObjects (Editor)";
    
    public void SaveCurrentMap()
    {
        if (currentMapData == null) return;

        // Le a cena para o MapData antes de gravar.
        UpdateMapDataFromTilemap();

        // Ate 2026-09-03 este metodo parava aqui, com um `// For now, just update
        // metadata`: o mapa era atualizado em memoria e NADA ia para o disco, entao
        // sair do Play Mode perdia tudo o que se tinha pintado. O MapSerializer
        // (backup, autosave, asset + JSON) ja existia e nunca tinha sido chamado.
        var serializer = ObterSerializer();
        if (serializer != null)
        {
            serializer.SaveMapData(currentMapData);
        }
        else
        {
            // Sem serializer nao ha para onde salvar; avisar e melhor que fingir
            // que salvou, porque o proximo passo do usuario e fechar o jogo.
            Debug.LogWarning("[MapEditor] Sem MapSerializer: o mapa NAO foi gravado " +
                             "em disco. Adicione um MapSerializer a cena.");
        }

        currentMapData.UpdateMetadata();
        OnMapSaved?.Invoke();
    }

    /// <summary>
    /// O MapSerializer e criado sob demanda se ninguem o ligou no inspector: ele nao
    /// tem estado de cena, so pastas de destino, entao exigir montagem manual seria
    /// mais uma referencia para esquecer.
    /// </summary>
    /// <summary>
    /// Os mapas que existem no disco, para a paleta listar.
    ///
    /// Havia "Salvar mapa" e nenhum jeito de reabrir: o mapa salvo ficava
    /// inacessivel pelo proprio editor. O MapSerializer ja sabia listar e ler --
    /// como o resto deste editor, faltava so quem chamasse.
    /// </summary>
    public System.Collections.Generic.List<string> MapasDisponiveis()
    {
        var serializer = ObterSerializer();
        return serializer != null
            ? serializer.GetAvailableMaps()
            : new System.Collections.Generic.List<string>();
    }

    /// <summary>
    /// Carrega um mapa do disco pelo nome e aplica na cena.
    ///
    /// Devolve false quando o arquivo nao pode ser lido, para a paleta poder dizer
    /// isso em vez de deixar a tela igual e o usuario sem saber se clicou errado.
    /// </summary>
    public bool CarregarMapaDoDisco(string nome)
    {
        if (string.IsNullOrEmpty(nome)) return false;

        var serializer = ObterSerializer();
        if (serializer == null) return false;

        var dados = serializer.LoadMapData(nome);
        if (dados == null) return false;

        LoadMapData(dados);

        // Carregar substitui a cena inteira, entao o que estava na pilha de desfazer
        // se refere a um mapa que nao esta mais aberto -- desfazer depois disso
        // reporia tiles do mapa anterior por cima deste.
        History?.Limpar();
        return true;
    }

    private MapSerializer ObterSerializer()
    {
        if (mapSerializer != null) return mapSerializer;

        mapSerializer = FindFirstObjectByType<MapSerializer>();
        if (mapSerializer == null)
        {
            mapSerializer = gameObject.AddComponent<MapSerializer>();
        }
        return mapSerializer;
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

        // Lido ANTES de pintar: e o que o Ctrl+Z precisa restaurar.
        var antes = DualGridPaintAdapter.Read(dualGridTilemap, position);

        // Pinta na cena primeiro: se este tileset nao sabe desenhar o tipo pedido,
        // nao gravamos no MapData um dado que nunca vai reaparecer na tela.
        if (!DualGridPaintAdapter.Paint(dualGridTilemap, position, tileType))
            return;

        currentMapData.SetTileAt(position, tileType, selectedLayer);
        History?.RegistrarMudanca(position, antes, tileType);
    }

    private MapEditorHistory _history;
    /// <summary>Historico de desfazer/refazer, se houver um na cena.</summary>
    public MapEditorHistory History => _history != null
        ? _history
        : _history = GetComponent<MapEditorHistory>();
    
    public ExtendedTileType GetTileAtPosition(Vector3Int position)
    {
        if (currentMapData == null) return ExtendedTileType.None;
        
        var tileData = currentMapData.GetTileAt(position);
        return tileData?.tileType ?? ExtendedTileType.None;
    }
    
    // Validation and utility methods
    private void OnValidate()
    {
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