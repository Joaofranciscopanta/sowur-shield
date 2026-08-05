using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using SowurShield.Core;

namespace SowurShield.Dialogue
{

public class DialogueTreeUI : MonoBehaviour, IUIWindow
{
    [Header("Core UI Elements")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject continueIndicator;
    
    [Header("Choice System")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private int maxChoicesDisplayed = 6;
    
    [Header("Portrait System")]
    [SerializeField] private PortraitManager portraitManager;
    
    [Header("Audio")]
    [SerializeField] private AudioClip dialogueOpenSound;
    [SerializeField] private AudioClip dialogueCloseSound;
    [SerializeField] private AudioClip choiceAppearSound;
    [SerializeField] private AudioSource uiAudioSource;
    
    [Header("Animation Settings")]
    [SerializeField] private float choiceStaggerDelay = 0.1f;
    
    // Core components
    private TypewriterEffect typewriter;
    private ConversationMemory conversationMemory;
    
    // Dialogue state
    private DialogueTree currentDialogueTree;
    private DialogueNode currentNode;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private bool canContinue = false;
    private bool isWaitingForChoice = false;
    
    // Choice management
    private List<ChoiceButton> activeChoiceButtons = new List<ChoiceButton>();
    private int selectedChoiceIndex = 0;

    // Extra choices (e.g. "Gift", "Relationship") appended to the start node's choice
    // list for the current conversation. Set via SetExtraStartNodeChoices() before
    // StartDialogue() is called; cleared when the dialogue ends.
    private List<DialogueChoice> extraStartNodeChoices = new List<DialogueChoice>();
    
    // Animation tracking
    private Coroutine currentDialogueCoroutine;
    
    // Events
    public System.Action OnDialogueStarted;
    public System.Action OnDialogueEnded;
    public System.Action<DialogueNode> OnNodeChanged;
    
    // Properties
    public bool IsDialogueActive => isDialogueActive;
    public bool IsWaitingForChoice => isWaitingForChoice;
    public DialogueNode CurrentNode => currentNode;

    // IUIWindow implementation
    public string WindowName => "Dialogue";
    public int WindowPriority => 30;
    public bool IsWindowOpen => isDialogueActive;
    public bool CanCloseWithEsc => true; // Allow closing dialogues with ESC
    
    private void Awake()
    {
        ValidateComponents();
        InitializeUI();
    }
    
    private void Start()
    {
        FindDependencies();
        RegisterWithUIManager();
    }

    private void OnDestroy()
    {
        UnregisterFromUIManager();
    }

    private void RegisterWithUIManager()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterWindow(this);
        }
    }

    private void UnregisterFromUIManager()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterWindow(this);
        }
    }

    // IUIWindow implementation methods
    public void OpenWindow()
    {
        // This is called by UIManager when window can open
        // The actual dialogue opening logic continues from StartDialogue
    }

    public void CloseWindow()
    {
        // Force close dialogue
        EndDialogue();
    }

    public void OnWindowBlocked(string blockedBy)
    {
    }
    
    private void ValidateComponents()
    {
        if (dialoguePanel == null)
        
        if (dialogueText == null)
        
        if (choicePanel == null)
        
        if (choiceContainer == null)
        
        if (choiceButtonPrefab == null)

        
        if (portraitManager == null)
            portraitManager = GetComponentInChildren<PortraitManager>();
        
        if (uiAudioSource == null)
            uiAudioSource = GetComponent<AudioSource>();
    }
    
    private void InitializeUI()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        
        if (choicePanel != null)
            choicePanel.SetActive(false);
        
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        
        if (continueIndicator != null)
            continueIndicator.SetActive(false);
    }
    
    private void FindDependencies()
    {
        // Find TypewriterEffect
        typewriter = GetComponent<TypewriterEffect>();
        if (typewriter == null)
            typewriter = GetComponentInChildren<TypewriterEffect>();
        if (typewriter == null)
            typewriter = FindFirstObjectByType<TypewriterEffect>();
        
        
        // Find ConversationMemory
        conversationMemory = ConversationMemory.Instance;
    }
    
    /// <summary>
    /// Starts a dialogue tree conversation
    /// </summary>
    public void StartDialogue(DialogueTree dialogueTree)
    {
        if (dialogueTree == null)
        {
            return;
        }

        if (isDialogueActive)
        {
            return;
        }

        // Use UIManager to try opening this window
        if (UIManager.Instance != null && !UIManager.Instance.TryOpenWindow(this))
        {
            return;
        }
        
        // Validate tree
        if (!dialogueTree.ValidateTree())
        {
            return;
        }
        
        currentDialogueTree = dialogueTree;
        isDialogueActive = true;
        
        // Show dialogue panel
        ShowDialoguePanel();
        
        // Start from the beginning or resume from last position
        string startNodeId = dialogueTree.startNodeId;
        if (conversationMemory != null)
        {
            string lastNode = conversationMemory.GetLastNodeReached(dialogueTree.conversationId);
            if (!string.IsNullOrEmpty(lastNode) && !dialogueTree.isRepeatable)
            {
                startNodeId = lastNode;
            }
        }
        
        // Begin dialogue
        var startNode = dialogueTree.GetNode(startNodeId);
        if (startNode != null)
        {
            ShowNode(startNode);
        }
        else
        {
            EndDialogue();
        }
        
        OnDialogueStarted?.Invoke();
    }
    
    /// <summary>
    /// Shows a specific dialogue node
    /// </summary>
    private void ShowNode(DialogueNode node)
    {
        if (node == null) return;
        
        // Check if node should be displayed
        if (!node.ShouldDisplay())
        {
            // Skip to next available node
            var nextNode = currentDialogueTree.GetNextAvailableNode(node.nodeId);
            if (nextNode != null)
            {
                ShowNode(nextNode);
                return;
            }
            else
            {
                EndDialogue();
                return;
            }
        }
        
        currentNode = node;
        
        // Update conversation memory
        if (conversationMemory != null && currentDialogueTree != null)
        {
            conversationMemory.SetLastNodeReached(currentDialogueTree.conversationId, node.nodeId);
        }
        
        // Execute node effects
        node.ExecuteNodeEffects();
        
        // Update speaker name
        if (speakerNameText != null)
        {
            speakerNameText.text = node.GetSpeakerDisplayName();
        }
        
        // Handle portrait
        if (portraitManager != null && node.speakerPortrait != null)
        {
            portraitManager.ShowPortrait(node.speakerPortrait, node.speakerPosition, true);
        }
        
        // Start dialogue coroutine
        if (currentDialogueCoroutine != null)
        {
            StopCoroutine(currentDialogueCoroutine);
        }
        
        currentDialogueCoroutine = StartCoroutine(ProcessNodeCoroutine(node));
        
        OnNodeChanged?.Invoke(node);
    }
    
    private IEnumerator ProcessNodeCoroutine(DialogueNode node)
    {
        // Hide choices initially
        HideChoices();

        // String tables load asynchronously — wait rather than show an empty box if the
        // player opens dialogue in the brief window before LocalizationManager finishes preloading.
        //
        // Bounded: this used to be an unbounded WaitUntil, and when the flag never flipped the
        // coroutine parked here forever with the speaker name shown and the body never written.
        // Proceeding after the timeout degrades to a possibly-empty line, which the block below
        // reports — far better than a dialogue box that ignores every key but Esc.
        if (!LocalizationManager.AreTablesReady)
        {
            float waitedSeconds = 0f;
            const float MaxWaitSeconds = 5f;
            while (!LocalizationManager.AreTablesReady && waitedSeconds < MaxWaitSeconds)
            {
                waitedSeconds += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!LocalizationManager.AreTablesReady)
            {
                Debug.LogWarning("[DialogueTreeUI] String tables were not ready after " +
                                 $"{MaxWaitSeconds}s — continuing anyway. Text may be missing. " +
                                 "Is LocalizationManager present/bootstrapped?");
            }
        }

        // Show dialogue text with typewriter effect
        string resolvedDialogueText = node.dialogueText.SafeGetLocalizedString();

        // An unresolvable line must not leave the previous node's text (or the editor
        // placeholder) on screen: that reads as a frozen dialogue box that only Esc closes.
        // Clearing and logging makes the missing entry visible instead of silent.
        if (string.IsNullOrEmpty(resolvedDialogueText))
        {
            Debug.LogWarning($"[DialogueTreeUI] Node '{node.nodeId}' resolved to an empty string " +
                             $"(table '{node.dialogueText.TableReference.TableCollectionName}', " +
                             $"key id {node.dialogueText.TableEntryReference.KeyId}). " +
                             "Check the entry exists in the string table.");
            if (dialogueText != null) dialogueText.text = string.Empty;
        }
        else
        {
            isTyping = true;
            canContinue = false;

            yield return typewriter.Run(resolvedDialogueText, dialogueText);

            isTyping = false;
        }
        
        // Handle node type
        switch (node.nodeType)
        {
            case NodeType.Dialogue:
                yield return HandleDialogueNode(node);
                break;
                
            case NodeType.Choice:
                yield return HandleChoiceNode(node);
                break;
                
            case NodeType.Event:
                yield return HandleEventNode(node);
                break;
        }
    }
    
    private IEnumerator HandleDialogueNode(DialogueNode node)
    {
        var availableChoices = GetChoicesWithExtras(node);

        if (availableChoices.Length > 0)
        {
            // Show choices
            yield return ShowChoicesCoroutine(availableChoices);
        }
        else
        {
            // Wait for continue input or auto-advance
            if (node.autoAdvanceDelay > 0)
            {
                yield return new WaitForSeconds(node.autoAdvanceDelay);
                ContinueToNextNode();
            }
            else
            {
                canContinue = true;
                ShowContinueIndicator(true);
                yield return new WaitUntil(() => !canContinue);
                ContinueToNextNode();
            }
        }
    }

    private IEnumerator HandleChoiceNode(DialogueNode node)
    {
        var availableChoices = GetChoicesWithExtras(node);

        if (availableChoices.Length > 0)
        {
            yield return ShowChoicesCoroutine(availableChoices);
        }
        else
        {
            ContinueToNextNode();
        }
    }

    /// <summary>
    /// Returns this node's available choices, with the extra start-node-only choices
    /// (e.g. "Gift", "Relationship") appended when this is the conversation's start node.
    /// </summary>
    private DialogueChoice[] GetChoicesWithExtras(DialogueNode node)
    {
        var availableChoices = node.GetAvailableChoices();

        if (extraStartNodeChoices.Count == 0 || currentDialogueTree == null ||
            node.nodeId != currentDialogueTree.startNodeId)
        {
            return availableChoices;
        }

        var combined = new DialogueChoice[availableChoices.Length + extraStartNodeChoices.Count];
        availableChoices.CopyTo(combined, 0);
        extraStartNodeChoices.CopyTo(combined, availableChoices.Length);
        return combined;
    }

    /// <summary>
    /// Sets extra choices (e.g. "Gift", "Relationship") to be appended to the start node's
    /// choice list for the next conversation started via StartDialogue(). Cleared automatically
    /// when the dialogue ends. Pass null or an empty list to show no extra choices.
    /// </summary>
    public void SetExtraStartNodeChoices(List<DialogueChoice> choices)
    {
        extraStartNodeChoices.Clear();
        if (choices != null)
            extraStartNodeChoices.AddRange(choices);
    }
    
    private IEnumerator HandleEventNode(DialogueNode node)
    {
        // Event nodes could trigger cutscenes, sounds, etc.
        // For now, just continue automatically
        yield return new WaitForSeconds(0.5f);
        ContinueToNextNode();
    }
    
    private IEnumerator ShowChoicesCoroutine(DialogueChoice[] choices)
    {
        isWaitingForChoice = true;
        ShowContinueIndicator(false);
        
        // Play choice sound
        if (choiceAppearSound != null && uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(choiceAppearSound);
        }
        
        // Show choice panel
        if (choicePanel != null)
            choicePanel.SetActive(true);
        
        // Build every button FIRST, then animate. The previous version yielded inside the
        // creation loop with the button already scaled to zero, so anything that interrupted
        // the coroutine (or a tween that failed to run) left the list half-built and the
        // buttons that did exist stuck at scale 0 — visible as an empty choice panel.
        var spawned = new List<GameObject>();
        for (int i = 0; i < choices.Length && i < maxChoicesDisplayed; i++)
        {
            var buttonObj = Instantiate(choiceButtonPrefab, choiceContainer);
            var choiceButton = buttonObj.GetComponent<ChoiceButton>();
            if (choiceButton == null)
            {
                Destroy(buttonObj);
                continue;
            }

            choiceButton.Initialize(choices[i], OnChoiceSelected);
            activeChoiceButtons.Add(choiceButton);
            spawned.Add(buttonObj);
        }

        // Let the layout group place them at full size before anything is scaled down,
        // otherwise the group measures zero-sized children and collapses the panel.
        Canvas.ForceUpdateCanvases();
        if (choiceContainer is RectTransform containerRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);

        for (int i = 0; i < spawned.Count; i++)
        {
            var buttonTransform = spawned[i].transform;
            buttonTransform.localScale = Vector3.zero;
            // SetUpdate(true): dialogue runs with the game paused in some contexts, and a
            // timescale-dependent tween never completes there — which leaves the button
            // permanently invisible at scale 0.
            buttonTransform.DOScale(Vector3.one, 0.3f)
                           .SetDelay(i * choiceStaggerDelay)
                           .SetEase(Ease.OutBack)
                           .SetUpdate(true);
        }

        yield return null;
        
        // Select first choice for keyboard navigation
        if (activeChoiceButtons.Count > 0)
        {
            selectedChoiceIndex = 0;
            activeChoiceButtons[0].SetHighlighted(true);
        }
    }
    
    private void OnChoiceSelected(DialogueChoice choice)
    {
        if (!isWaitingForChoice) return;
        
        isWaitingForChoice = false;
        
        // Record choice in memory
        if (conversationMemory != null && currentDialogueTree != null && currentNode != null)
        {
            conversationMemory.RecordChoice(
                currentDialogueTree.conversationId,
                currentNode.nodeId,
                choice.GetDisplayText(),
                choice.nextNodeId
            );
        }
        
        // Hide choices
        HideChoices();
        
        // Continue to next node or end dialogue
        if (choice.isExitChoice || string.IsNullOrEmpty(choice.nextNodeId))
        {
            EndDialogue();

            // Run any runtime-only callback (e.g. opening the Gift/Relationship panel)
            // after the dialogue window has closed, so it isn't blocked by UIManager's
            // single-window stack.
            choice.onSelectedRuntime?.Invoke();
        }
        else
        {
            var nextNode = currentDialogueTree.GetNode(choice.nextNodeId);
            if (nextNode != null)
            {
                ShowNode(nextNode);
            }
            else
            {
                EndDialogue();
            }
        }
    }
    
    private void ContinueToNextNode()
    {
        ShowContinueIndicator(false);
        
        if (currentNode == null || currentDialogueTree == null)
        {
            EndDialogue();
            return;
        }
        
        // Get next node
        if (!string.IsNullOrEmpty(currentNode.nextNodeId))
        {
            var nextNode = currentDialogueTree.GetNode(currentNode.nextNodeId);
            if (nextNode != null)
            {
                ShowNode(nextNode);
            }
            else
            {
                EndDialogue();
            }
        }
        else
        {
            EndDialogue();
        }
    }
    
    /// <summary>
    /// Ends the current dialogue
    /// </summary>
    public void EndDialogue()
    {
        if (!isDialogueActive) return;
        
        // Mark conversation as completed
        if (conversationMemory != null && currentDialogueTree != null)
        {
            conversationMemory.CompleteConversation(currentDialogueTree.conversationId);
        }
        
        // Clean up
        if (currentDialogueCoroutine != null)
        {
            StopCoroutine(currentDialogueCoroutine);
            currentDialogueCoroutine = null;
        }
        
        HideChoices();
        ShowContinueIndicator(false);
        
        // Hide portraits
        if (portraitManager != null)
            portraitManager.HideAllPortraits();
        
        // Hide dialogue panel
        HideDialoguePanel();
        
        // Reset state
        isDialogueActive = false;
        isTyping = false;
        canContinue = false;
        isWaitingForChoice = false;
        currentDialogueTree = null;
        currentNode = null;
        extraStartNodeChoices.Clear();

        // Notify UIManager that dialogue has ended
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TryCloseWindow(this);
        }

        OnDialogueEnded?.Invoke();
    }
    
    private void ShowDialoguePanel()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            
            // Play sound
            if (dialogueOpenSound != null && uiAudioSource != null)
            {
                uiAudioSource.PlayOneShot(dialogueOpenSound);
            }
        }
    }
    
    private void HideDialoguePanel()
    {
        if (dialoguePanel != null)
        {
            // Play sound
            if (dialogueCloseSound != null && uiAudioSource != null)
            {
                uiAudioSource.PlayOneShot(dialogueCloseSound);
            }
            
            dialoguePanel.SetActive(false);
        }
    }
    
    private void HideChoices()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);
        
        // Clean up choice buttons
        foreach (var button in activeChoiceButtons)
        {
            if (button != null && button.gameObject != null)
                Destroy(button.gameObject);
        }
        activeChoiceButtons.Clear();
        selectedChoiceIndex = 0;
    }
    
    private void ShowContinueIndicator(bool show)
    {
        if (continueIndicator != null)
            continueIndicator.SetActive(show);
        
        if (continueButton != null)
            continueButton.gameObject.SetActive(show);
    }
    
    private void OnContinueButtonClicked()
    {
        if (canContinue && !isWaitingForChoice)
        {
            canContinue = false;
        }
    }
    
    private void Update()
    {
        HandleInput();
    }
    
    private void HandleInput()
    {
        if (!isDialogueActive) return;

        // Handle continue/skip input
        bool continuePressed = (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame) ||
                              (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) ||
                              (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !IsMouseOverChoice());
        
        if (continuePressed)
        {
            if (isTyping && typewriter != null)
            {
                typewriter.Skip();
            }
            else if (canContinue && !isWaitingForChoice)
            {
                canContinue = false;
            }
        }
        
        // Handle choice navigation
        if (isWaitingForChoice && activeChoiceButtons.Count > 0)
        {
            HandleChoiceNavigation();
        }
    }
    
    private void HandleChoiceNavigation()
    {
        if (Keyboard.current == null)
            return;

        bool navigationChanged = false;

        // Vertical navigation with W/S keys
        if (Keyboard.current.wKey.wasPressedThisFrame ||
            Keyboard.current.upArrowKey.wasPressedThisFrame ||
            (Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame))
        {
            selectedChoiceIndex = (selectedChoiceIndex - 1 + activeChoiceButtons.Count) % activeChoiceButtons.Count;
            navigationChanged = true;
        }
        else if (Keyboard.current.sKey.wasPressedThisFrame ||
                 Keyboard.current.downArrowKey.wasPressedThisFrame ||
                 (Gamepad.current != null && Gamepad.current.dpad.down.wasPressedThisFrame))
        {
            selectedChoiceIndex = (selectedChoiceIndex + 1) % activeChoiceButtons.Count;
            navigationChanged = true;
        }

        // Selection with E key
        if (Keyboard.current.eKey.wasPressedThisFrame ||
            Keyboard.current.enterKey.wasPressedThisFrame ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
        {
            if (selectedChoiceIndex >= 0 && selectedChoiceIndex < activeChoiceButtons.Count)
            {
                var selectedChoice = activeChoiceButtons[selectedChoiceIndex].AssociatedChoice;
                if (selectedChoice != null && selectedChoice.IsAvailable())
                {
                    OnChoiceSelected(selectedChoice);
                }
            }
        }
        
        // Update visual selection
        if (navigationChanged)
        {
            for (int i = 0; i < activeChoiceButtons.Count; i++)
            {
                activeChoiceButtons[i].SetHighlighted(i == selectedChoiceIndex);
            }
        }
    }
    
    /// <summary>
    /// Checks if the mouse is currently over any choice button
    /// </summary>
    private bool IsMouseOverChoice()
    {
        if (!isWaitingForChoice || activeChoiceButtons.Count == 0)
            return false;

        if (Mouse.current == null)
            return false;

        // Check if mouse is over any UI element (including choice buttons)
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // Check if any of the raycast results are choice buttons
        foreach (var result in results)
        {
            if (result.gameObject.GetComponentInParent<ChoiceButton>() != null)
                return true;
        }

        return false;
    }
}

} // namespace SowurShield.Dialogue