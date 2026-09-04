using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SowurShield.Dialogue;

namespace SowurShield.MapEditor
{

public class NPCPlacer : MonoBehaviour
{
    [Header("NPC Placement Settings")]
    [SerializeField] private bool npcPlacementMode = false;
    [SerializeField] private GameObject npcPreviewPrefab;
    [SerializeField] private LayerMask npcLayer = 1;
    
    [Header("NPC Configuration")]
    [SerializeField] private NPCTemplate[] availableNPCTemplates;
    [SerializeField] private GameObject defaultNPCPrefab;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject placementPreview;
    [SerializeField] private Color validPlacementColor = Color.green;
    [SerializeField] private Color invalidPlacementColor = Color.red;
    [SerializeField] private float previewAlpha = 0.6f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip placementSound;
    [SerializeField] private AudioClip removeSound;
    [SerializeField] private AudioSource audioSource;
    
    // Runtime references
    private RuntimeMapEditor mapEditor;
    private Camera mainCamera;
    
    // Placement state
    private NPCTemplate selectedNPCTemplate;
    private GameObject previewInstance;
    private List<GameObject> placedNPCs = new List<GameObject>();
    
    // UI state
    private bool isPlacementValid = false;
    
    public bool IsNPCPlacementMode => npcPlacementMode;
    public NPCTemplate SelectedTemplate => selectedNPCTemplate;
    public int PlacedNPCCount => placedNPCs.Count;
    
    // Events
    public System.Action<NPCSpawnData> OnNPCPlaced;
    public System.Action<Vector3> OnNPCRemoved;
    public System.Action<bool> OnPlacementModeToggled;
    
    private void Start()
    {
        InitializeNPCPlacer();
    }
    
    private void Update()
    {
        if (mapEditor?.IsEditorActive == true && npcPlacementMode)
        {
            HandleNPCPlacement();
            UpdatePreview();
        }
    }
    
    private void InitializeNPCPlacer()
    {
        mapEditor = RuntimeMapEditor.Instance;
        mainCamera = Camera.main;
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.5f;
        }
        
        // Initialize with first available template
        if (availableNPCTemplates != null && availableNPCTemplates.Length > 0)
        {
            selectedNPCTemplate = availableNPCTemplates[0];
        }
        
        // Load existing NPCs from map data
        LoadExistingNPCs();
        
        // Create preview
        CreatePreview();
        

    }
    
    private void CreatePreview()
    {
        if (selectedNPCTemplate?.prefab != null)
        {
            previewInstance = Instantiate(selectedNPCTemplate.prefab);
            previewInstance.name = "NPC Preview";
            
            // Disable colliders and scripts for preview
            DisablePreviewComponents(previewInstance);
            
            // Make semi-transparent
            SetPreviewTransparency(previewInstance, previewAlpha);
            
            previewInstance.SetActive(false);
        }
        else if (npcPreviewPrefab != null)
        {
            previewInstance = Instantiate(npcPreviewPrefab);
            previewInstance.name = "NPC Preview";
            previewInstance.SetActive(false);
        }
    }
    
    private void DisablePreviewComponents(GameObject preview)
    {
        // Disable all colliders
        var colliders = preview.GetComponentsInChildren<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
        
        // Disable NPC scripts
        var npcScript = preview.GetComponent<NPCDialogueInteractable>();
        if (npcScript != null)
        {
            npcScript.enabled = false;
        }
        
        // Disable rigidbody
        var rb = preview.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }
    
    private void SetPreviewTransparency(GameObject obj, float alpha)
    {
        var renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
    
    private void HandleNPCPlacement()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3 placementPos = SnapToGrid(mouseWorldPos);
        
        // Check if placement position is valid
        isPlacementValid = IsValidPlacementPosition(placementPos);
        
        // Handle placement input
        if (Input.GetMouseButtonDown(0))
        {
            if (isPlacementValid)
            {
                PlaceNPC(placementPos);
            }
        }
        else if (Input.GetMouseButtonDown(1)) // Right click to remove
        {
            RemoveNPCAtPosition(placementPos);
        }
        
        // ESC to exit placement mode
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleNPCPlacementMode();
        }
    }
    
    private void UpdatePreview()
    {
        if (previewInstance == null) return;
        
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3 previewPos = SnapToGrid(mouseWorldPos);
        
        previewInstance.transform.position = previewPos;
        previewInstance.SetActive(true);
        
        // Update preview color based on validity
        Color previewColor = isPlacementValid ? validPlacementColor : invalidPlacementColor;
        previewColor.a = previewAlpha;
        
        var renderers = previewInstance.GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers)
        {
            Color color = previewColor;
            color.a = previewAlpha;
            renderer.color = color;
        }
    }
    
    private Vector3 SnapToGrid(Vector3 worldPos)
    {
        return new Vector3(
            Mathf.Floor(worldPos.x) + 0.5f,
            Mathf.Floor(worldPos.y) + 0.5f,
            0
        );
    }
    
    private bool IsValidPlacementPosition(Vector3 position)
    {
        // Check if there's already an NPC at this position
        if (IsNPCAtPosition(position))
        {
            return false;
        }
        
        // Check if position is within map bounds
        if (mapEditor?.CurrentMapData != null)
        {
            Vector2Int mapSize = mapEditor.CurrentMapData.mapSize;
            if (position.x < -mapSize.x/2 || position.x > mapSize.x/2 ||
                position.y < -mapSize.y/2 || position.y > mapSize.y/2)
            {
                return false;
            }
        }
        
        // Check for collisions with other objects
        Collider2D collider = Physics2D.OverlapCircle(position, 0.4f, ~npcLayer);
        if (collider != null)
        {
            return false;
        }
        
        return true;
    }
    
    private bool IsNPCAtPosition(Vector3 position)
    {
        foreach (GameObject npc in placedNPCs)
        {
            if (npc != null && Vector3.Distance(npc.transform.position, position) < 0.5f)
            {
                return true;
            }
        }
        return false;
    }
    
    private void PlaceNPC(Vector3 position)
    {
        if (selectedNPCTemplate == null)
        {

            return;
        }
        
        GameObject npcPrefab = selectedNPCTemplate.prefab != null ? selectedNPCTemplate.prefab : defaultNPCPrefab;
        
        if (npcPrefab == null)
        {

            return;
        }
        
        // Instantiate NPC
        GameObject newNPC = Instantiate(npcPrefab, position, Quaternion.identity);
        newNPC.name = $"{selectedNPCTemplate.npcName}_{placedNPCs.Count + 1}";
        
        // Configure NPC
        ConfigureNPC(newNPC, selectedNPCTemplate);
        
        // Add to placed NPCs list
        placedNPCs.Add(newNPC);
        
        // Create spawn data
        NPCSpawnData spawnData = new NPCSpawnData
        {
            position = position,
            npcId = selectedNPCTemplate.npcId,
            npcName = selectedNPCTemplate.npcName,
            dialogueIds = selectedNPCTemplate.dialogueIds,
            rotation = 0f,
            isActive = true,
            npcPrefabPath = GetPrefabPath(npcPrefab)
        };
        
        // Add to map data
        if (mapEditor?.CurrentMapData != null)
        {
            mapEditor.CurrentMapData.npcSpawns.Add(spawnData);
            mapEditor.CurrentMapData.UpdateMetadata();
        }
        
        // Play sound
        PlaySound(placementSound);
        
        // Trigger event
        OnNPCPlaced?.Invoke(spawnData);
        

    }
    
    private void ConfigureNPC(GameObject npc, NPCTemplate template)
    {
        // Configure NPCDialogueInteractable if present
        NPCDialogueInteractable dialogueScript = npc.GetComponent<NPCDialogueInteractable>();
        if (dialogueScript != null)
        {
            // Use reflection or public methods to set properties
            // This is a simplified version - you might need to adjust based on your NPCDialogueInteractable structure
            
            // Set NPC name and ID through public methods if available
            // dialogueScript.SetNPCInfo(template.npcId, template.npcName);
            
            // Set dialogues if the component has public methods for this
            // dialogueScript.SetDialogues(template.dialogues);
        }
        
        // Configure sprite if different from template
        if (template.sprite != null)
        {
            SpriteRenderer spriteRenderer = npc.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = template.sprite;
            }
        }
        
        // Set layer
        npc.layer = npcLayer;
    }
    
    private void RemoveNPCAtPosition(Vector3 position)
    {
        GameObject npcToRemove = null;
        
        foreach (GameObject npc in placedNPCs)
        {
            if (npc != null && Vector3.Distance(npc.transform.position, position) < 0.5f)
            {
                npcToRemove = npc;
                break;
            }
        }
        
        if (npcToRemove != null)
        {
            RemoveNPC(npcToRemove);
        }
    }
    
    private void RemoveNPC(GameObject npc)
    {
        Vector3 npcPosition = npc.transform.position;
        
        // Remove from placed NPCs list
        placedNPCs.Remove(npc);
        
        // Remove from map data
        if (mapEditor?.CurrentMapData != null)
        {
            mapEditor.CurrentMapData.RemoveNPCSpawn(npcPosition);
            mapEditor.CurrentMapData.UpdateMetadata();
        }
        
        // Destroy GameObject
        Destroy(npc);
        
        // Play sound
        PlaySound(removeSound);
        
        // Trigger event
        OnNPCRemoved?.Invoke(npcPosition);
        

    }
    
    private void LoadExistingNPCs()
    {
        if (mapEditor?.CurrentMapData?.npcSpawns == null) return;
        
        foreach (NPCSpawnData spawnData in mapEditor.CurrentMapData.npcSpawns)
        {
            // Find appropriate template
            NPCTemplate template = FindTemplateById(spawnData.npcId);
            if (template == null)
            {

                continue;
            }
            
            GameObject npcPrefab = template.prefab != null ? template.prefab : defaultNPCPrefab;
            if (npcPrefab == null) continue;
            
            // Instantiate NPC
            GameObject npc = Instantiate(npcPrefab, spawnData.position, 
                                       Quaternion.Euler(0, 0, spawnData.rotation));
            npc.name = $"{spawnData.npcName}_loaded";
            
            ConfigureNPC(npc, template);
            placedNPCs.Add(npc);
        }
        

    }
    
    private NPCTemplate FindTemplateById(string npcId)
    {
        return availableNPCTemplates?.FirstOrDefault(t => t.npcId == npcId);
    }
    
    /// <summary>
    /// O CAMINHO do prefab, nao o nome.
    ///
    /// Isto devolvia `prefab.name` com um "For now, return the name", e um nome nao
    /// resolve de volta: o NPC era gravado no mapa e nunca reaparecia ao carregar,
    /// porque o PrefabCatalog procura pelo caminho.
    /// </summary>
    private string GetPrefabPath(GameObject prefab)
    {
        if (prefab == null) return "";
#if UNITY_EDITOR
        var caminho = UnityEditor.AssetDatabase.GetAssetPath(prefab);
        if (!string.IsNullOrEmpty(caminho)) return caminho;

        // Uma instancia de cena nao tem caminho proprio; pedimos o do prefab de origem.
        var origem = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(prefab);
        if (origem != null) return UnityEditor.AssetDatabase.GetAssetPath(origem);
#endif
        return "";
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Public methods for external control
    public void ToggleNPCPlacementMode()
    {
        SetNPCPlacementMode(!npcPlacementMode);
    }
    
    public void SetNPCPlacementMode(bool enabled)
    {
        npcPlacementMode = enabled;
        
        if (previewInstance != null)
        {
            previewInstance.SetActive(enabled);
        }
        
        OnPlacementModeToggled?.Invoke(npcPlacementMode);
        

    }
    
    public void SelectNPCTemplate(int templateIndex)
    {
        if (availableNPCTemplates == null || templateIndex < 0 || templateIndex >= availableNPCTemplates.Length)
        {

            return;
        }
        
        selectedNPCTemplate = availableNPCTemplates[templateIndex];
        
        // Recreate preview with new template
        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }
        CreatePreview();
        

    }
    
    public void SelectNPCTemplate(string npcId)
    {
        NPCTemplate template = FindTemplateById(npcId);
        if (template != null)
        {
            selectedNPCTemplate = template;
            
            // Recreate preview
            if (previewInstance != null)
            {
                Destroy(previewInstance);
            }
            CreatePreview();
        }
    }
    
    public void ClearAllNPCs()
    {
        while (placedNPCs.Count > 0)
        {
            RemoveNPC(placedNPCs[0]);
        }
        

    }
    
    public List<NPCSpawnData> GetAllNPCSpawnData()
    {
        List<NPCSpawnData> spawnDataList = new List<NPCSpawnData>();
        
        foreach (GameObject npc in placedNPCs)
        {
            if (npc == null) continue;
            
            // Extract spawn data from placed NPC
            NPCSpawnData spawnData = new NPCSpawnData
            {
                position = npc.transform.position,
                rotation = npc.transform.eulerAngles.z,
                isActive = npc.activeInHierarchy
            };
            
            // Get additional data from NPC component if available
            NPCDialogueInteractable dialogueScript = npc.GetComponent<NPCDialogueInteractable>();
            if (dialogueScript != null)
            {
                spawnData.npcName = dialogueScript.GetNPCDisplayName();
                // spawnData.npcId = dialogueScript.GetNPCId(); // If such method exists
            }
            
            spawnDataList.Add(spawnData);
        }
        
        return spawnDataList;
    }
    
    private void OnDisable()
    {
        if (previewInstance != null)
        {
            previewInstance.SetActive(false);
        }
    }
    
    private void OnDestroy()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }
    }
    
    // Gizmos for debugging
    private void OnDrawGizmos()
    {
        if (!npcPlacementMode) return;
        
        // Draw placement area
        Gizmos.color = isPlacementValid ? Color.green : Color.red;
        Vector3 mouseWorldPos = mainCamera != null ? mainCamera.ScreenToWorldPoint(Input.mousePosition) : Vector3.zero;
        Vector3 snapPos = SnapToGrid(mouseWorldPos);
        Gizmos.DrawWireCube(snapPos, Vector3.one);
        
        // Draw existing NPCs
        Gizmos.color = Color.cyan;
        foreach (GameObject npc in placedNPCs)
        {
            if (npc != null)
            {
                Gizmos.DrawWireCube(npc.transform.position, Vector3.one * 0.8f);
            }
        }
    }
}

[System.Serializable]
public class NPCTemplate
{
    [Header("Basic Info")]
    public string npcId;
    public string npcName;
    public string description;
    
    [Header("Visual")]
    public GameObject prefab;
    public Sprite sprite;
    public Sprite icon; // For UI display
    
    [Header("Dialogue")]
    public string[] dialogueIds;
    public DialogueTree[] dialogues; // Direct references
    
    [Header("Behavior")]
    public bool allowRepeatedConversations = true;
    public float interactionRange = 2f;
    public bool disableMovementDuringDialogue = true;
    
    [Header("Audio")]
    public AudioClip interactionSound;
    
    [Header("Custom Data")]
    public string customData;
}
} // namespace SowurShield.MapEditor