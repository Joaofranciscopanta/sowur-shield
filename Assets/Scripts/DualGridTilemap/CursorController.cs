using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using SowurShield.Core;
using SowurShield.Inventory;
using SowurShield.Dialogue;
using SowurShield.MapEditor;

namespace SowurShield.Farming
{

public partial class CursorController : MonoBehaviour {
    public DualGridTilemap dualGridTilemap;
    public Transform playerTransform;
    [SerializeField] private GameBalance balance;
    public float maxDistance = 2f;

    [Header("Solo")]
    public GameObject soilBlockPrefab;
    public string hoeTag = "Hoe";  // Tag para identificar a enxada

    [Header("Interação")]
    public LayerMask interactableLayer;
    public Color normalColor = Color.white;
    public Color interactableColor = Color.green;

    private Mouse mouse;
    private Camera mainCamera;
    private SpriteRenderer cursorRenderer;
    private Dictionary<Vector3Int, GameObject> soilBlocks = new Dictionary<Vector3Int, GameObject>();
    private GameObject currentInteractableObject;
    private SowurShield.Inventory.Inventory playerInventory;
    private SowurShield.Core.PlayerMove playerMove;
    private DialogueTreeUI dialogueUI;
    private string sortingLayerOriginal;
    private int sortingOrderOriginal;
    private bool emModoEditor;

    void Start() {
        if (balance == null)
            balance = Resources.Load<GameBalance>("GameBalance");
        if (balance != null)
            maxDistance = balance.maxToolDistance;

        mouse = Mouse.current;
        mainCamera = Camera.main;

        cursorRenderer = GetComponent<SpriteRenderer>();
        if (cursorRenderer == null) {
            cursorRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        // O editor de mapa sobe o cursor para WorldUI enquanto esta aberto; guardamos
        // o valor original para devolver ao fechar, senao o cursor do JOGO fica com o
        // sorting do editor para sempre.
        sortingLayerOriginal = cursorRenderer.sortingLayerName;
        sortingOrderOriginal = cursorRenderer.sortingOrder;

        // Encontrar o inventário do jogador
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) {
            playerInventory = player.GetComponent<SowurShield.Inventory.Inventory>();
            playerMove = player.GetComponent<SowurShield.Core.PlayerMove>();
        }
        
        // Encontrar o DialogueTreeUI
        dialogueUI = FindFirstObjectByType<DialogueTreeUI>();
    }

    void Update() {
        // Reencontrar a camera quando a referencia morre.
        //
        // mainCamera e capturado no Start, e apontava para a camera da SampleScene. Com
        // interiores cada cena tem a SUA camera: entrar numa casa destroi aquela, e este
        // Update saia logo na primeira linha — o indicador do rato ficava CONGELADO no
        // ultimo sitio onde estava, sem erro nenhum na consola.
        if (mainCamera == null) mainCamera = Camera.main;

        // playerTransform tambem: o jogador atravessa cenas, mas uma cena que traga a
        // sua copia deixa esta referencia a apontar para o objeto destruido.
        if (playerTransform == null)
        {
            var jogador = GameObject.FindGameObjectWithTag("Player");
            if (jogador != null) playerTransform = jogador.transform;
        }

        if (mouse == null || mainCamera == null || playerTransform == null) return;
        
        // Com o editor de mapa aberto o cursor continua VISIVEL — e o mesmo indicador
        // que o jogo ja usa, entao o editor nao precisa desenhar outro — mas fica
        // inerte: nao interage com NPC, cama, SellBox nem usa a ferramenta da mao.
        // Sem isto o mesmo clique pintava um tile E arava o chao embaixo dele.
        //
        // O limite de maxDistance tambem nao vale aqui: ele existe para o jogador nao
        // alcancar o que esta longe, e quem constroi precisa pintar a tela inteira.
        if (RuntimeMapEditor.Instance != null && RuntimeMapEditor.Instance.IsEditorActive) {
            emModoEditor = true;
            cursorRenderer.enabled = true;
            AcompanharMouseSemInteragir();
            return;
        }

        if (emModoEditor) {
            emModoEditor = false;
            cursorRenderer.sortingLayerName = sortingLayerOriginal;
            cursorRenderer.sortingOrder = sortingOrderOriginal;
            cursorRenderer.color = normalColor;
        }

        // Hide cursor when inventory is open, dialogue is active, or SellBox is open
        if ((playerInventory != null && playerInventory.IsInventoryOpen) ||
            (dialogueUI != null && dialogueUI.IsDialogueActive) ||
            IsSellBoxOpen()) {
            cursorRenderer.enabled = false;
            return;
        } else {
            cursorRenderer.enabled = true;
        }

        // Obter posição do cursor (stick direito do gamepad tem prioridade quando ativo)
        Vector2 mouseScreenPos = GamepadVirtualCursor.OverridePosition ?? mouse.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane));
        Vector3Int tilePos = GetWorldPosTile(mouseWorldPos);

        // Limitar distância do cursor ao jogador
        Vector3Int playerTilePos = GetWorldPosTile(playerTransform.position);
        float distance = Vector2.Distance(
            new Vector2(tilePos.x, tilePos.y),
            new Vector2(playerTilePos.x, playerTilePos.y)
        );

        Vector3Int activeTilePos;
        Vector3 cursorPosition;

        if (distance <= maxDistance) {
            activeTilePos = tilePos;
            cursorPosition = tilePos + new Vector3(0.5f, 0.5f, -1);
        } else {
            // Limitar a posição do cursor à distância máxima
            Vector2 direction = new Vector2(tilePos.x - playerTilePos.x, tilePos.y - playerTilePos.y).normalized;
            Vector2 limitedPos = new Vector2(playerTilePos.x, playerTilePos.y) + direction * maxDistance;
            activeTilePos = new Vector3Int(Mathf.FloorToInt(limitedPos.x), Mathf.FloorToInt(limitedPos.y), 0);
            cursorPosition = activeTilePos + new Vector3(0.5f, 0.5f, -1);
        }

        // Atualizar posição do cursor
        transform.position = cursorPosition;

        // Check for interactable objects and update cursor visual feedback
        // First check for direct mouse hits (higher priority for visual feedback)
        GameObject directHit = CheckForDirectMouseHit();
        if (directHit != null) {
            currentInteractableObject = directHit;
        } else {
            // Fallback to grid-based detection for soil blocks and similar objects only
            currentInteractableObject = CheckForGridObjectsOnly(activeTilePos);
        }
        UpdateCursorVisual(activeTilePos);

        // Process left-click interactions with proper priority
        // Don't process clicks while dragging inventory items or when mouse is over UI
        bool clickPressed = mouse.leftButton.wasPressedThisFrame || GamepadVirtualCursor.WasClickPressedThisFrame();
        if (clickPressed &&
            !InventorySlot.IsAnySlotDragging &&
            !IsMouseOverUI()) {
            ProcessHexInteraction(activeTilePos);
        }
    }

    /// <summary>
    /// Process left-click interaction at the specified hex position with proper priority:
    /// 1. Direct cursor collision with objects (SellBox, NPCs, etc.) - HIGHEST PRIORITY
    /// 2. Tools in hand (Hoe, WateringCan, etc.) - LOWEST PRIORITY
    /// </summary>
    private void ProcessHexInteraction(Vector3Int hexPos) {
        // PRIORITY 1: Check for direct mouse collision with interactable objects
        GameObject directHitObject = CheckForDirectMouseHit();

        if (directHitObject != null) {
            IInteractable interactable = directHitObject.GetComponent<IInteractable>();
            if (interactable != null) {
                interactable.Interact();
                return; // Direct hit takes priority - stop here
            }
        }

        // PRIORITY 2: Check for grid-based objects only (soil blocks) using precise hex detection
        GameObject soilBlockAtHex = CheckForGridObjectsOnly(hexPos);

        if (soilBlockAtHex != null) {
            IInteractable interactable = soilBlockAtHex.GetComponent<IInteractable>();
            if (interactable != null) {
                interactable.Interact();
                return;
            }
        }

        // PRIORITY 3: No objects found, check for tool usage
        if (HasToolInHand()) {
            Item selectedTool = playerInventory.GetSelectedItem();
            if (selectedTool != null) {
                ProcessToolUsage(selectedTool, hexPos);
            }
        }
    }
    
    /// <summary>
    /// Check if player has any tool in their hand
    /// </summary>
    private bool HasToolInHand() {
        if (playerInventory == null)
            return false;

        Item selectedItem = playerInventory.GetSelectedItem();
        return selectedItem != null && HasToolTag(selectedItem);
    }
    
    /// <summary>
    /// Check if item has any tool-related tags
    /// </summary>
    private bool HasToolTag(Item item) {
        if (item == null || item.itemTags == null)
            return false;

        return item.itemTags.Contains("Hoe") ||
               item.itemTags.Contains("WateringCan") ||
               item.itemTags.Contains("Shovel") ||
               item.itemTags.Contains("Axe") ||
               item.itemTags.Contains("FishingRod") ||
               item.itemTags.Contains("Tool");
    }
    
    /// <summary>
    /// Process tool usage based on the specific tool type
    /// </summary>
    private void ProcessToolUsage(Item tool, Vector3Int hexPos) {
        // Dispara a animacao de acao correspondente a ferramenta em uso
        if (playerMove != null) {
            Vector3 target = hexPos + new Vector3(0.5f, 0.5f, 0f);
            playerMove.FaceDirection((Vector2)(target - playerTransform.position));
            if (tool.itemTags.Contains("Hoe") || tool.itemTags.Contains("Shovel"))
                playerMove.TriggerActionAnimation("Hoe");
            else if (tool.itemTags.Contains("WateringCan"))
                playerMove.TriggerActionAnimation("Water");
            else if (tool.itemTags.Contains("Axe"))
                playerMove.TriggerActionAnimation("Axe");
            else if (tool.itemTags.Contains("FishingRod"))
                playerMove.TriggerActionAnimation("Fish");
        }

        // Pesca: detectar FishingSpot proximo e disparar rotina
        if (tool.itemTags.Contains("FishingRod")) {
            ProcessFishing(hexPos);
            return; // Pesca toma conta do resto
        }

        // Efeito de respingo no tile regado (aparece na direcao correta)
        if (tool.itemTags.Contains("WateringCan")) {
            WaterSplashEffect.Spawn(hexPos + new Vector3(0.5f, 0.5f, 0f));
        }

        if (tool.itemTags.Contains("Hoe")) {
            CreateSoilBlock(hexPos);
        }
        // Add other tool types here as needed:
        // else if (tool.itemTags.Contains("WateringCan")) {
        //     ProcessWateringCan(hexPos);
        // }
        // else if (tool.itemTags.Contains("Shovel")) {
        //     ProcessShovel(hexPos);
        // }
    }
    
    /// <summary>
    /// Update cursor visual feedback based on what's available at the cursor position
    /// </summary>
    private void UpdateCursorVisual(Vector3Int hexPos) {
        // Priority 1: Any interactable object (direct hits or grid objects)
        if (currentInteractableObject != null) {
            cursorRenderer.color = interactableColor; // Green for any interactable objects
            return;
        }
        
        // Priority 2: Show tool feedback if we have a tool and no objects are in the way
        if (HasToolInHand()) {
            Item selectedTool = playerInventory.GetSelectedItem();
            if (selectedTool != null && CanUseToolAt(selectedTool, hexPos)) {
                cursorRenderer.color = Color.yellow; // Yellow for tool usage
                return;
            }
        }
        
        // Default: Normal cursor
        cursorRenderer.color = normalColor;
    }
    
    /// <summary>
    /// Check if the selected tool can be used at the specified hex position
    /// </summary>
    private bool CanUseToolAt(Item tool, Vector3Int hexPos) {
        if (tool.itemTags.Contains("Hoe")) {
            // Hoe can be used if there's no SoilBlock already there
            return !soilBlocks.ContainsKey(hexPos);
        }
        
        // Add checks for other tools as needed
        // if (tool.itemTags.Contains("WateringCan")) {
        //     return CanWaterAt(hexPos);
        // }
        
        return true; // Default: tool can be used
    }

    /// <summary>
    /// Check for direct mouse cursor collision with interactable objects using raycasting
    /// This ensures SellBox and similar objects only respond when the cursor is actually over them
    /// </summary>
    private GameObject CheckForDirectMouseHit() {
        if (mouse == null || mainCamera == null) return null;

        // Get cursor position in world space (gamepad virtual cursor takes priority when active)
        Vector2 mouseScreenPos = GamepadVirtualCursor.OverridePosition ?? mouse.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane));
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        
        // Raycast from mouse position to find interactable objects
        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero, 0f, interactableLayer);
        
        if (hit.collider != null) {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null) {
                // Check if the player is within interaction range
                float distanceToPlayer = Vector2.Distance(hit.collider.transform.position, playerTransform.position);
                float interactionRange = GetInteractionRangeForObject(hit.collider.gameObject);
                
                if (distanceToPlayer <= interactionRange) {

                    return hit.collider.gameObject;
                }
                else {

                }
            }
        }
        
        // Fallback: Use OverlapPoint for objects without proper colliders or edge cases
        Collider2D[] overlapping = Physics2D.OverlapPointAll(mousePos2D);
        foreach (var collider in overlapping) {
            IInteractable interactable = collider.GetComponent<IInteractable>();
            if (interactable != null) {
                float distanceToPlayer = Vector2.Distance(collider.transform.position, playerTransform.position);
                float interactionRange = GetInteractionRangeForObject(collider.gameObject);
                
                if (distanceToPlayer <= interactionRange) {

                    return collider.gameObject;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Get the appropriate interaction range for different types of objects
    /// </summary>
    private float GetInteractionRangeForObject(GameObject obj) {
        // SellBox has its own interaction range
        if (obj.GetComponent<SellBox>() != null) {
            return obj.GetComponent<SellBox>().GetInteractionRange();
        }
        
        // NPCs might have different ranges
        if (obj.GetComponent<NPCDialogueInteractable>() != null) {
            return obj.GetComponent<NPCDialogueInteractable>().GetInteractionRange();
        }
        
        // Default interaction range
        return maxDistance;
    }

    /// <summary>
    /// Check specifically for grid-based objects that should use hex-detection (soil blocks, beds)
    /// This method excludes SellBox and NPCs which should only respond to direct cursor hits
    /// </summary>
    private GameObject CheckForGridObjectsOnly(Vector3Int tilePos) {
        // Check the SoilBlocks dictionary first (most precise)
        if (soilBlocks.TryGetValue(tilePos, out GameObject existingSoil)) {

            return existingSoil;
        }

        // Check for other grid-aligned objects at this position
        Vector2 worldPos = new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f);
        
        // Use a smaller, more precise detection radius for grid objects
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(worldPos, 0.3f);
        foreach (var collider in allColliders) {
            // Only return objects that should use grid-based interaction
            if (collider.GetComponent<SoilBlockInteractable>() != null ||
                collider.GetComponent<BedInteractable>() != null) {

                return collider.gameObject;
            }
        }

        return null;
    }

    private GameObject CheckForInteractableAt(Vector3Int tilePos) {
        // First check the SoilBlocks dictionary
        if (soilBlocks.TryGetValue(tilePos, out GameObject existingSoil)) {

            return existingSoil;
        }

        // Then do a physical check for other interactables
        Vector2 worldPos = new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f);
        
        // Try multiple detection methods to be thorough
        
        // Method 1: Layer-based overlap circle
        Collider2D hitCollider = Physics2D.OverlapCircle(worldPos, 0.6f, interactableLayer);
        if (hitCollider != null && hitCollider.GetComponent<IInteractable>() != null) {

            return hitCollider.gameObject;
        }
        
        // Method 2: Check all colliders in area (no layer restriction)
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(worldPos, 0.6f);
        foreach (var collider in allColliders) {
            IInteractable interactable = collider.GetComponent<IInteractable>();
            if (interactable != null) {

                return collider.gameObject;
            }
        }
        
        // Method 3: Direct check for SellBox components in area
        foreach (var collider in allColliders) {
            if (collider.GetComponent<SellBox>() != null) {

                return collider.gameObject;
            }
        }


        return null;
    }


    /// <summary>
    /// Procura um FishingSpot proximo e inicia a pesca.
    /// </summary>
    private void ProcessFishing(Vector3Int hexPos) {
        Vector3 worldPos = hexPos + new Vector3(0.5f, 0.5f, 0f);
        float searchRadius = 1.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, searchRadius);
        foreach (var hit in hits) {
            var spot = hit.GetComponent<SowurShield.Farming.FishingSpot>();
            if (spot != null) {
                spot.Interact();
                return;
            }
        }
        // Sem FishingSpot? Tenta na posicao do cursor mesmo
        hits = Physics2D.OverlapCircleAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), searchRadius);
        foreach (var hit in hits) {
            var spot = hit.GetComponent<SowurShield.Farming.FishingSpot>();
            if (spot != null) {
                spot.Interact();
                return;
            }
        }
    }

    private void CreateSoilBlock(Vector3Int tilePos) {
        // Verifica se já existe um SoilBlock nesta posição
        if (soilBlocks.ContainsKey(tilePos)) return;

        // Cria o tile visual
        dualGridTilemap.SetCell(tilePos, dualGridTilemap.dirtPlaceholderTile);

        // Cria o objeto SoilBlock
        Vector3 worldPos = new Vector3(tilePos.x + 0.5f, tilePos.y + 0.5f, 0);
        GameObject newSoilBlock = Instantiate(soilBlockPrefab, worldPos, Quaternion.identity);

        // Rastreia o bloco no dicionário
        soilBlocks.Add(tilePos, newSoilBlock);

        // Configura o bloco como já arado
        SoilBlockInteractable soilScript = newSoilBlock.GetComponent<SoilBlockInteractable>();
        if (soilScript != null) {
            soilScript.TillSoilDirectly();

            // Opcionalmente, você pode passar referências necessárias
            soilScript.Initialize(playerInventory);
        }
    }

    /// <summary>
    /// Move o indicador para a celula sob o mouse sem disparar nenhuma interacao.
    /// Usado enquanto o editor de mapa esta aberto: o jogo empresta o proprio cursor
    /// ao editor em vez de este desenhar um quadrado seu.
    ///
    /// Diferencas em relacao ao Update normal: sem limite de maxDistance (quem
    /// constroi precisa alcancar a tela toda) e sem deteccao de interagiveis, entao
    /// NPC, cama e SellBox nao respondem enquanto se pinta.
    /// </summary>
    private void AcompanharMouseSemInteragir() {
        Vector2 screenPos = GamepadVirtualCursor.OverridePosition ?? mouse.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, mainCamera.nearClipPlane));

        // Pelo tilemap, e nao por FloorToInt: e o mesmo caminho que o pincel usa, entao
        // o indicador nunca aponta para uma celula diferente da que sera pintada.
        Vector3Int tilePos;
        var dual = RuntimeMapEditor.Instance.DualGrid;
        if (dual != null && dual.placeholderTilemap != null) {
            tilePos = dual.placeholderTilemap.WorldToCell(worldPos);
            tilePos.z = 0;
        } else {
            tilePos = GetWorldPosTile(worldPos);
        }

        transform.position = tilePos + new Vector3(0.5f, 0.5f, -1);

        // A cor diz o que o clique fara: apagar ou pintar. Verde e reservado para
        // interagiveis, que aqui nao existem, entao nao ha ambiguidade.
        var editor = RuntimeMapEditor.Instance;
        bool apagando = editor.selectedBrush == MapEditor.BrushType.Eraser
                     || editor.selectedTileType == MapEditor.ExtendedTileType.Grass;
        cursorRenderer.color = apagando
            ? new Color(1f, 0.55f, 0.55f)
            : new Color(1f, 0.93f, 0.55f);

        // O indicador do jogo desenha em Default/0 e ficaria sob o proprio tilemap
        // do chao enquanto se pinta.
        cursorRenderer.sortingLayerName = "WorldUI";
        cursorRenderer.sortingOrder = 1001;
    }

    public static Vector3Int GetWorldPosTile(Vector3 worldPos) {
        int xInt = Mathf.FloorToInt(worldPos.x);
        int yInt = Mathf.FloorToInt(worldPos.y);
        return new(xInt, yInt, 0);
    }

    // Método para gerenciar a remoção de um SoilBlock
    public void UnregisterSoilBlock(Vector3Int position) {
        if (soilBlocks.ContainsKey(position)) {
            soilBlocks.Remove(position);
        }
    }
    
    // Helper method to check if mouse is over UI element
    private bool IsMouseOverUI() {
        return UnityEngine.EventSystems.EventSystem.current != null && 
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
    
    // Helper method to check if any SellBox is currently open
    private bool IsSellBoxOpen() {
        SellBox[] sellBoxes = FindObjectsByType<SellBox>(FindObjectsSortMode.None);
        foreach (SellBox sellBox in sellBoxes) {
            if (sellBox != null && sellBox.IsOpen) {
                return true;
            }
        }
        return false;
    }
}

} // namespace SowurShield.Farming
