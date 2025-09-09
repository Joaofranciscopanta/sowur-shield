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
        
        // Hide cursor when inventory is open or dialogue is active
        if ((playerInventory != null && playerInventory.IsInventoryOpen) ||
            (dialogueUI != null && dialogueUI.IsDialogueActive)) {
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

        // Verificar por objetos interagíveis
        currentInteractableObject = CheckForInteractableAt(activeTilePos);
        cursorRenderer.color = (currentInteractableObject != null) ? interactableColor : normalColor;

        // Processar interações com clique
        // Don't process tool clicks while dragging inventory items or when mouse is over UI
        if (mouse.leftButton.wasPressedThisFrame && 
            !InventorySlot.IsAnySlotDragging && 
            !IsMouseOverUI()) {
            // Se há um interagível, interaja com ele
            if (currentInteractableObject != null) {
                IInteractable interactable = currentInteractableObject.GetComponent<IInteractable>();
                if (interactable != null) {
                    interactable.Interact();
                }
            }
            // Se não há interagível e temos uma enxada, crie um SoilBlock
            else if (IsUsingHoe()) {
                CreateSoilBlock(activeTilePos);
            }
        }
    }

    private GameObject CheckForInteractableAt(Vector3Int tilePos) {
        // Primeiro verifica o dicionário de SoilBlocks
        if (soilBlocks.TryGetValue(tilePos, out GameObject existingSoil)) {
            return existingSoil;
        }

        // Depois faz uma verificação física
        Vector2 worldPos = new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f);
        Collider2D hitCollider = Physics2D.OverlapCircle(worldPos, 0.4f, interactableLayer);

        if (hitCollider != null && hitCollider.GetComponent<IInteractable>() != null) {
            return hitCollider.gameObject;
        }

        return null;
    }

    private bool IsUsingHoe() {
        if (playerInventory == null) return false;

        Item selectedItem = playerInventory.GetSelectedItem();
        if (selectedItem == null) return false;

        // Verifica se o item tem a tag "Hoe"
        return selectedItem.itemTags != null && selectedItem.itemTags.Contains(hoeTag);
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
}
