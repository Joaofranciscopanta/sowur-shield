using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

public partial class CursorController : MonoBehaviour {
    public DualGridTilemap dualGridTilemap;
    public Transform playerTransform;
    public float maxDistance = 2f;

    [Header("Soil")]
    public GameObject soilBlockPrefab;
    public string hoeTag = "Hoe";

    [Header("Interaction")]
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

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) {
            playerInventory = player.GetComponent<Inventory>();
        }

        dialogueUI = FindFirstObjectByType<DialogueTreeUI>();
    }

    void Update() {
        if (mouse == null || mainCamera == null || playerTransform == null) return;

        if ((playerInventory != null && playerInventory.IsInventoryOpen) ||
            (dialogueUI != null && dialogueUI.IsDialogueActive) ||
            IsSellBoxOpen()) {
            cursorRenderer.enabled = false;
            return;
        } else {
            cursorRenderer.enabled = true;
        }

        Vector2 mouseScreenPos = mouse.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane));
        Vector3Int tilePos = GetWorldPosTile(mouseWorldPos);

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
            Vector2 direction = new Vector2(tilePos.x - playerTilePos.x, tilePos.y - playerTilePos.y).normalized;
            Vector2 limitedPos = new Vector2(playerTilePos.x, playerTilePos.y) + direction * maxDistance;
            activeTilePos = new Vector3Int(Mathf.FloorToInt(limitedPos.x), Mathf.FloorToInt(limitedPos.y), 0);
            cursorPosition = activeTilePos + new Vector3(0.5f, 0.5f, -1);
        }

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

        if (mouse.leftButton.wasPressedThisFrame &&
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
        if (playerInventory == null) {
            return false;
        }

        Item selectedItem = playerInventory.GetSelectedItem();
        return selectedItem != null && HasToolTag(selectedItem);
    }
    
    /// <summary>
    /// Check if item has any tool-related tags
    /// </summary>
    private bool HasToolTag(Item item) {
        if (item == null || item.itemTags == null) {
            return false;
        }

        // Check for any tool tags
        bool hasTool = item.itemTags.Contains("Hoe") ||
                       item.itemTags.Contains("WateringCan") ||
                       item.itemTags.Contains("Shovel") ||
                       item.itemTags.Contains("Tool"); // Generic tool tag

        return hasTool;
    }
    
    /// <summary>
    /// Process tool usage based on the specific tool type
    /// </summary>
    private void ProcessToolUsage(Item tool, Vector3Int hexPos) {
        if (tool.itemTags.Contains("Hoe")) {
            CreateSoilBlock(hexPos);
        }
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
            return !soilBlocks.ContainsKey(hexPos);
        }

        return true;
    }

    /// <summary>
    /// Check for direct mouse cursor collision with interactable objects using raycasting
    /// This ensures SellBox and similar objects only respond when the cursor is actually over them
    /// </summary>
    private GameObject CheckForDirectMouseHit() {
        if (mouse == null || mainCamera == null) return null;

        Vector2 mouseScreenPos = mouse.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, mainCamera.nearClipPlane));
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero, 0f, interactableLayer);

        if (hit.collider != null) {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null) {
                float distanceToPlayer = Vector2.Distance(hit.collider.transform.position, playerTransform.position);
                float interactionRange = GetInteractionRangeForObject(hit.collider.gameObject);

                if (distanceToPlayer <= interactionRange) {
                    return hit.collider.gameObject;
                }
            }
        }

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
        if (obj.GetComponent<SellBox>() != null) {
            return obj.GetComponent<SellBox>().GetInteractionRange();
        }

        if (obj.GetComponent<NPCDialogueInteractable>() != null) {
            return obj.GetComponent<NPCDialogueInteractable>().GetInteractionRange();
        }

        return maxDistance;
    }

    /// <summary>
    /// Check specifically for grid-based objects that should use hex-detection (soil blocks, beds)
    /// This method excludes SellBox and NPCs which should only respond to direct cursor hits
    /// </summary>
    private GameObject CheckForGridObjectsOnly(Vector3Int tilePos) {
        if (soilBlocks.TryGetValue(tilePos, out GameObject existingSoil)) {
            return existingSoil;
        }

        Vector2 worldPos = new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f);

        Collider2D[] allColliders = Physics2D.OverlapCircleAll(worldPos, 0.3f);
        foreach (var collider in allColliders) {
            if (collider.GetComponent<SoilBlockInteractable>() != null ||
                collider.GetComponent<BedInteractable>() != null) {
                return collider.gameObject;
            }
        }

        return null;
    }

    private GameObject CheckForInteractableAt(Vector3Int tilePos) {
        if (soilBlocks.TryGetValue(tilePos, out GameObject existingSoil)) {
            return existingSoil;
        }

        Vector2 worldPos = new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f);

        Collider2D hitCollider = Physics2D.OverlapCircle(worldPos, 0.6f, interactableLayer);
        if (hitCollider != null && hitCollider.GetComponent<IInteractable>() != null) {
            return hitCollider.gameObject;
        }

        Collider2D[] allColliders = Physics2D.OverlapCircleAll(worldPos, 0.6f);
        foreach (var collider in allColliders) {
            IInteractable interactable = collider.GetComponent<IInteractable>();
            if (interactable != null) {
                return collider.gameObject;
            }
        }

        foreach (var collider in allColliders) {
            if (collider.GetComponent<SellBox>() != null) {
                return collider.gameObject;
            }
        }

        return null;
    }


    private void CreateSoilBlock(Vector3Int tilePos) {
        if (soilBlocks.ContainsKey(tilePos)) return;

        dualGridTilemap.SetCell(tilePos, dualGridTilemap.dirtPlaceholderTile);

        Vector3 worldPos = new Vector3(tilePos.x + 0.5f, tilePos.y + 0.5f, 0);
        GameObject newSoilBlock = Instantiate(soilBlockPrefab, worldPos, Quaternion.identity);

        soilBlocks.Add(tilePos, newSoilBlock);

        SoilBlockInteractable soilScript = newSoilBlock.GetComponent<SoilBlockInteractable>();
        if (soilScript != null) {
            soilScript.TillSoilDirectly();
            soilScript.Initialize(playerInventory);
        }
    }

    public static Vector3Int GetWorldPosTile(Vector3 worldPos) {
        int xInt = Mathf.FloorToInt(worldPos.x);
        int yInt = Mathf.FloorToInt(worldPos.y);
        return new(xInt, yInt, 0);
    }

    public void UnregisterSoilBlock(Vector3Int position) {
        if (soilBlocks.ContainsKey(position)) {
            soilBlocks.Remove(position);
        }
    }

    private bool IsMouseOverUI() {
        return UnityEngine.EventSystems.EventSystem.current != null && 
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

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
