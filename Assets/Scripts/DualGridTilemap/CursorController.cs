using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

public partial class CursorController : MonoBehaviour {
    public DualGridTilemap dualGridTilemap;
    public Transform playerTransform;
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
    private Inventory playerInventory;
    private DialogueTreeUI dialogueUI;

    void Start() {
        mouse = Mouse.current;
        mainCamera = Camera.main;

        cursorRenderer = GetComponent<SpriteRenderer>();
        if (cursorRenderer == null) {
            cursorRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (mainCamera == null) {
            Debug.LogError("Main Camera não encontrada!");
        }
        if (playerTransform == null) {
            Debug.LogError("Player Transform não atribuído!");
        }

        // Encontrar o inventário do jogador
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) {
            playerInventory = player.GetComponent<Inventory>();
        }
        
        // Encontrar o DialogueTreeUI
        dialogueUI = FindFirstObjectByType<DialogueTreeUI>();
    }

    void Update() {
        if (mouse == null || mainCamera == null || playerTransform == null) return;
        
        // Hide cursor when inventory is open, dialogue is active, or SellBox is open
        if ((playerInventory != null && playerInventory.IsInventoryOpen) ||
            (dialogueUI != null && dialogueUI.IsDialogueActive) ||
            IsSellBoxOpen()) {
            cursorRenderer.enabled = false;
            return;
        } else {
            cursorRenderer.enabled = true;
        }

        // Obter posição do cursor
        Vector2 mouseScreenPos = mouse.position.ReadValue();
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
        currentInteractableObject = CheckForInteractableAt(activeTilePos);
        UpdateCursorVisual(activeTilePos);

        // Process left-click interactions with proper priority
        // Don't process clicks while dragging inventory items or when mouse is over UI
        if (mouse.leftButton.wasPressedThisFrame && 
            !InventorySlot.IsAnySlotDragging && 
            !IsMouseOverUI()) {
            
            ProcessHexInteraction(activeTilePos);
        }
    }

    /// <summary>
    /// Process left-click interaction at the specified hex position with proper priority:
    /// 1. Objects in the hex (SellBox, NPCs, etc.) - HIGHEST PRIORITY
    /// 2. Tools in hand (Hoe, WateringCan, etc.) - LOWEST PRIORITY
    /// </summary>
    private void ProcessHexInteraction(Vector3Int hexPos) {
        // PRIORITY 1: Check for interactable objects at this hex position
        GameObject interactableAtHex = CheckForInteractableAt(hexPos);
        
        if (interactableAtHex != null) {
            IInteractable interactable = interactableAtHex.GetComponent<IInteractable>();
            if (interactable != null) {
                Debug.Log($"[CursorController] Interacting with object: {interactableAtHex.name} at hex {hexPos}");
                interactable.Interact();
                return; // Object interaction takes priority - stop here
            }
        }
        
        // PRIORITY 2: No objects in hex, check for tool usage
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
        if (playerInventory == null) return false;
        
        Item selectedItem = playerInventory.GetSelectedItem();
        return selectedItem != null && HasToolTag(selectedItem);
    }
    
    /// <summary>
    /// Check if item has any tool-related tags
    /// </summary>
    private bool HasToolTag(Item item) {
        if (item == null || item.itemTags == null) return false;
        
        // Check for any tool tags
        return item.itemTags.Contains("Hoe") || 
               item.itemTags.Contains("WateringCan") || 
               item.itemTags.Contains("Shovel") ||
               item.itemTags.Contains("Tool"); // Generic tool tag
    }
    
    /// <summary>
    /// Process tool usage based on the specific tool type
    /// </summary>
    private void ProcessToolUsage(Item tool, Vector3Int hexPos) {
        if (tool.itemTags.Contains("Hoe")) {
            Debug.Log($"[CursorController] Using Hoe at hex {hexPos}");
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
    /// Update cursor visual feedback based on what's available at the hex position
    /// </summary>
    private void UpdateCursorVisual(Vector3Int hexPos) {
        // Priority 1: Objects take precedence for visual feedback
        if (currentInteractableObject != null) {
            cursorRenderer.color = interactableColor; // Green for interactable objects
            return;
        }
        
        // Priority 2: Show tool feedback if we have a tool
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

    private GameObject CheckForInteractableAt(Vector3Int tilePos) {
        // First check the SoilBlocks dictionary
        if (soilBlocks.TryGetValue(tilePos, out GameObject existingSoil)) {
            Debug.Log($"[CursorController] Found SoilBlock at {tilePos}: {existingSoil.name}");
            return existingSoil;
        }

        // Then do a physical check for other interactables
        Vector2 worldPos = new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f);
        
        // Try multiple detection methods to be thorough
        
        // Method 1: Layer-based overlap circle
        Collider2D hitCollider = Physics2D.OverlapCircle(worldPos, 0.6f, interactableLayer);
        if (hitCollider != null && hitCollider.GetComponent<IInteractable>() != null) {
            Debug.Log($"[CursorController] Found layered interactable at {tilePos}: {hitCollider.name} on layer {hitCollider.gameObject.layer}");
            return hitCollider.gameObject;
        }
        
        // Method 2: Check all colliders in area (no layer restriction)
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(worldPos, 0.6f);
        foreach (var collider in allColliders) {
            IInteractable interactable = collider.GetComponent<IInteractable>();
            if (interactable != null) {
                Debug.Log($"[CursorController] Found general interactable at {tilePos}: {collider.name} on layer {collider.gameObject.layer}");
                return collider.gameObject;
            }
        }
        
        // Method 3: Direct check for SellBox components in area
        foreach (var collider in allColliders) {
            if (collider.GetComponent<SellBox>() != null) {
                Debug.Log($"[CursorController] Found SellBox directly at {tilePos}: {collider.name}");
                return collider.gameObject;
            }
        }

        Debug.Log($"[CursorController] No interactables found at {tilePos}");
        return null;
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
