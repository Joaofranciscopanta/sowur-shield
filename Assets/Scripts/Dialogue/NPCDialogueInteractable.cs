using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class NPCDialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("NPC Configuration")]
    [SerializeField] private string npcId;
    [SerializeField] private string npcDisplayName;
    
    [Header("Dialogue Trees")]
    [SerializeField] private DialogueTree[] availableDialogues = new DialogueTree[0];
    [SerializeField] private DialogueTree defaultDialogue;
    
    
    [Header("UI References")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private float interactionRange = 3f;
    
    [Header("Behavior Settings")]
    [SerializeField] private bool allowRepeatedConversations = true;
    [SerializeField] private float cooldownBetweenInteractions = 1f;
    [SerializeField] private bool disableMovementDuringDialogue = true;
    
    [Header("Audio")]
    [SerializeField] private AudioClip interactionSound;
    [SerializeField] private AudioSource audioSource;
    
    // Internal state
    private bool playerInRange = false;
    private bool isDialogueActive = false;
    private float lastInteractionTime = 0f;
    private Transform player;
    private DialogueTreeUI dialogueUI;
    private ConversationMemory conversationMemory;
    private PlayerMove playerMovement;
    
    
    // Events
    public System.Action OnInteractionAvailable;
    public System.Action OnInteractionUnavailable;
    public System.Action<DialogueTree> OnDialogueStarted;
    public System.Action OnDialogueEnded;
    
    private void Start()
    {
        InitializeNPC();
        FindDependencies();
        
        // Delay registration to ensure InteractionManager is ready
        StartCoroutine(RegisterAfterFrame());
    }
    
    private System.Collections.IEnumerator RegisterAfterFrame()
    {
        // Wait for InteractionManager to be ready
        while (InteractionManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        yield return new WaitForEndOfFrame();
        RegisterWithInteractionManager();
    }
    
    private void OnDestroy()
    {
        UnregisterFromInteractionManager();
    }
    
    private void RegisterWithInteractionManager()
    {
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.RegisterInteractable(this);
            Debug.Log($"NPC '{npcDisplayName}' registered with InteractionManager");
        }
        else
        {
            Debug.LogWarning($"NPC '{npcDisplayName}': InteractionManager.Instance is null! Cannot register.");
        }
    }
    
    private void UnregisterFromInteractionManager()
    {
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.UnregisterInteractable(this);
        }
    }
    
    private void Update()
    {
        // If InteractionManager is not available, use fallback to original behavior
        if (InteractionManager.Instance == null)
        {
            CheckPlayerDistance();
        }
        // Otherwise, CheckPlayerDistance is handled by InteractionManager
    }
    
    private void InitializeNPC()
    {
        // Generate NPC ID if not set
        if (string.IsNullOrEmpty(npcId))
        {
            npcId = $"npc_{gameObject.name}_{GetInstanceID()}";
        }
        
        // Set display name if not set
        if (string.IsNullOrEmpty(npcDisplayName))
        {
            npcDisplayName = gameObject.name;
        }
        
        // Setup audio
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && interactionSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.volume = 0.7f;
            }
        }
        
        // Hide UI elements initially
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
            
        if (dialogueBox != null)
            dialogueBox.SetActive(false);
    }
    
    private void FindDependencies()
    {
        // Find player
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            var playerObj = FindFirstObjectByType<PlayerMove>();
            if (playerObj != null)
                player = playerObj.transform;
        }
        
        // Find dialogue UI
        dialogueUI = FindFirstObjectByType<DialogueTreeUI>();
        if (dialogueUI == null)
        {
            Debug.LogWarning($"NPCDialogueInteractable '{npcDisplayName}': DialogueTreeUI not found in scene!");
        }
        
        // Find conversation memory
        conversationMemory = ConversationMemory.Instance;
        
        // Find player movement component
        if (player != null)
            playerMovement = player.GetComponent<PlayerMove>();
    }
    
    
    private void CheckPlayerDistance()
    {
        // This method is kept for manual checks or special cases
        // Normal interaction management is handled by InteractionManager
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        bool wasInRange = playerInRange;
        
        // Check if player is in range and dialogue is not active
        playerInRange = distance <= interactionRange && !isDialogueActive && CanInteract();
        
        // Update interaction prompt visibility only if InteractionManager is not handling it
        if (InteractionManager.Instance == null && playerInRange != wasInRange)
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(playerInRange);
            
            if (playerInRange)
                OnInteractionAvailable?.Invoke();
            else
                OnInteractionUnavailable?.Invoke();
                
            Debug.Log($"NPC '{npcDisplayName}': Fallback prompt visibility set to {playerInRange}");
        }
    }
    
    private bool CanInteract()
    {
        // Check cooldown
        if (Time.time - lastInteractionTime < cooldownBetweenInteractions)
            return false;
        
        // Check if any dialogue is available
        return GetBestAvailableDialogue() != null;
    }
    
    /// <summary>
    /// Gets the best dialogue tree to show based on conditions and priority
    /// </summary>
    private DialogueTree GetBestAvailableDialogue()
    {
        if (availableDialogues == null || availableDialogues.Length == 0)
            return null;
        
        DialogueTree bestDialogue = null;
        int highestPriority = int.MinValue;
        
        foreach (var dialogue in availableDialogues)
        {
            if (dialogue == null) continue;
            
            // Check if dialogue should be shown
            if (!ShouldShowDialogue(dialogue)) continue;
            
            // Check priority
            if (dialogue.priority > highestPriority)
            {
                highestPriority = dialogue.priority;
                bestDialogue = dialogue;
            }
        }
        
        // Fall back to default dialogue if no prioritized dialogue found
        if (bestDialogue == null && defaultDialogue != null && ShouldShowDialogue(defaultDialogue))
        {
            bestDialogue = defaultDialogue;
        }
        
        return bestDialogue;
    }
    
    private bool ShouldShowDialogue(DialogueTree dialogue)
    {
        if (dialogue == null) return false;
        
        // Check if repeatable
        if (!allowRepeatedConversations && !dialogue.isRepeatable && conversationMemory != null)
        {
            if (conversationMemory.HasCompletedConversation(dialogue.conversationId))
                return false;
        }
        
        // Check start node conditions
        var startNode = dialogue.GetStartNode();
        if (startNode != null && !startNode.ShouldDisplay())
            return false;
        
        return true;
    }
    
    /// <summary>
    /// Implementation of IInteractable interface
    /// </summary>
    public void Interact()
    {
        // When using InteractionManager, skip range check as it's handled by the manager
        // When using fallback system, check range
        if (InteractionManager.Instance == null && !playerInRange)
        {
            Debug.Log($"NPC '{npcDisplayName}': Cannot interact - player not in range");
            return;
        }
        
        if (isDialogueActive || !CanInteract()) return;
        
        StartDialogue();
    }
    
    /// <summary>
    /// Starts dialogue with this NPC
    /// </summary>
    public void StartDialogue()
    {
        var dialogueToShow = GetBestAvailableDialogue();
        if (dialogueToShow == null)
        {
            Debug.LogWarning($"NPCDialogueInteractable '{npcDisplayName}': No available dialogue to show!");
            return;
        }
        
        if (dialogueUI == null)
        {
            Debug.LogError($"NPCDialogueInteractable '{npcDisplayName}': DialogueTreeUI not found!");
            return;
        }
        
        // Update state
        isDialogueActive = true;
        lastInteractionTime = Time.time;
        
        // Hide interaction prompt
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
        
        // Show dialogue box
        if (dialogueBox != null)
            dialogueBox.SetActive(true);
        
        // Play interaction sound
        if (interactionSound != null && audioSource != null)
            audioSource.PlayOneShot(interactionSound);
        
        // Disable player movement if requested
        if (disableMovementDuringDialogue && playerMovement != null)
            playerMovement.DisableMovement();
        
        // Subscribe to dialogue end event
        if (dialogueUI != null)
        {
            dialogueUI.OnDialogueEnded += OnDialogueEndedCallback;
        }
        
        // Start the dialogue
        dialogueUI.StartDialogue(dialogueToShow);
        
        OnDialogueStarted?.Invoke(dialogueToShow);
    }
    
    private void OnDialogueEndedCallback()
    {
        // Unsubscribe from event
        if (dialogueUI != null)
            dialogueUI.OnDialogueEnded -= OnDialogueEndedCallback;
        
        // Update state
        isDialogueActive = false;
        
        // Hide dialogue box
        if (dialogueBox != null)
            dialogueBox.SetActive(false);
        
        // Re-enable player movement
        if (disableMovementDuringDialogue && playerMovement != null)
            playerMovement.EnableMovement();
        
        OnDialogueEnded?.Invoke();
    }
    
    /// <summary>
    /// Adds a dialogue tree to this NPC's available dialogues
    /// </summary>
    public void AddDialogue(DialogueTree dialogue)
    {
        if (dialogue == null) return;
        
        var dialogueList = new List<DialogueTree>(availableDialogues);
        if (!dialogueList.Contains(dialogue))
        {
            dialogueList.Add(dialogue);
            availableDialogues = dialogueList.ToArray();
        }
    }
    
    /// <summary>
    /// Removes a dialogue tree from this NPC's available dialogues
    /// </summary>
    public void RemoveDialogue(DialogueTree dialogue)
    {
        if (dialogue == null) return;
        
        var dialogueList = new List<DialogueTree>(availableDialogues);
        dialogueList.Remove(dialogue);
        availableDialogues = dialogueList.ToArray();
    }
    
    /// <summary>
    /// Sets the default dialogue for this NPC
    /// </summary>
    public void SetDefaultDialogue(DialogueTree dialogue)
    {
        defaultDialogue = dialogue;
        
        // Add to available dialogues if not present
        if (dialogue != null)
            AddDialogue(dialogue);
    }
    
    /// <summary>
    /// Forces an immediate interaction (bypasses range and cooldown checks)
    /// </summary>
    public void ForceInteraction()
    {
        StartDialogue();
    }
    
    /// <summary>
    /// Gets information about this NPC for debugging
    /// </summary>
    public string GetNPCInfo()
    {
        var info = $"NPC: {npcDisplayName} (ID: {npcId})\n";
        info += $"Available Dialogues: {availableDialogues?.Length ?? 0}\n";
        info += $"Default Dialogue: {(defaultDialogue != null ? defaultDialogue.name : "None")}\n";
        info += $"Player In Range: {playerInRange}\n";
        info += $"Dialogue Active: {isDialogueActive}\n";
        info += $"Can Interact: {CanInteract()}\n";
        
        if (conversationMemory != null)
        {
            info += $"Relationship Level: {conversationMemory.GetRelationshipLevel(npcId)}\n";
        }
        
        return info;
    }
    
    // Methods for InteractionManager
    public float GetInteractionRange()
    {
        return interactionRange;
    }
    
    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
    
    public string GetNPCDisplayName()
    {
        return npcDisplayName;
    }
    
    public void SetPromptVisibility(bool visible)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(visible);
            Debug.Log($"NPC '{npcDisplayName}': Prompt visibility set to {visible}");
        }
        else
        {
            Debug.LogWarning($"NPC '{npcDisplayName}': interactionPrompt is null!");
        }
        
        if (visible)
            OnInteractionAvailable?.Invoke();
        else
            OnInteractionUnavailable?.Invoke();
    }
    
    // Visualization in Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        if (playerInRange)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactionRange * 0.8f);
        }
        
        // Draw line to player if in range
        if (player != null && playerInRange)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
    
    // Validation in editor
    private void OnValidate()
    {
        // Ensure interaction range is positive
        interactionRange = Mathf.Max(0.1f, interactionRange);
        
        // Ensure cooldown is not negative
        cooldownBetweenInteractions = Mathf.Max(0f, cooldownBetweenInteractions);
    }
}