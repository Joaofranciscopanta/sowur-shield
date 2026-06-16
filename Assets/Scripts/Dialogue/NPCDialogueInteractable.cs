using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using SowurShield.Core;

namespace SowurShield.Dialogue
{
    public class NPCDialogueInteractable : MonoBehaviour, IInteractable
    {
        [Header("NPC Configuration")]
        [SerializeField] private string npcId;
        [SerializeField] private string npcDisplayName;
        [SerializeField] private Sprite npcPortrait;
        [TextArea(2, 5)]
        [SerializeField] private string npcBio;

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

        [Header("Relationship & Gifting")]
        [Tooltip("If disabled, the \"Give a gift\" dialogue choice is never shown for this NPC.")]
        [SerializeField] private bool enableGifting = true;

        [Header("Seed Shop")]
        [Tooltip("If enabled, a \"Browse seeds\" dialogue choice opens the seed shop for this NPC.")]
        [SerializeField] private bool enableSeedShop = false;

        [Header("Codex / Lore")]
        [Tooltip("Lore entries revealed progressively in the Relationship codex as affinity grows.")]
        [SerializeField] private NpcLoreEntry[] loreEntries = new NpcLoreEntry[0];

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

            }
            else
            {

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
            // Generate NPC ID if not set. Based on gameObject.name (not GetInstanceID()) so
            // it stays stable across play sessions/saves; scene NPC names must be unique.
            if (string.IsNullOrEmpty(npcId))
            {
                npcId = $"npc_{gameObject.name}";
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


            }
        }

        public bool CanInteract()
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
            {
                if (defaultDialogue != null && ShouldShowDialogue(defaultDialogue))
                    return defaultDialogue;
                return null;
            }

            DialogueTree bestDialogue = null;
            int highestPriority = int.MinValue;

            foreach (var dialogue in availableDialogues)
            {
                if (dialogue == null) continue;
                if (!ShouldShowDialogue(dialogue)) continue;

                if (dialogue.priority > highestPriority)
                {
                    highestPriority = dialogue.priority;
                    bestDialogue = dialogue;
                }
            }

            if (bestDialogue == null && defaultDialogue != null && ShouldShowDialogue(defaultDialogue))
                bestDialogue = defaultDialogue;

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

                return;
            }

            if (isDialogueActive || !CanInteract()) return;

            StartDialogue();
        }

        /// <summary>
        /// Whether a gift can still be given to this NPC today.
        /// </summary>
        public bool CanGiftToday()
        {
            return conversationMemory != null && conversationMemory.CanGiftToday(npcId);
        }

        /// <summary>
        /// Gives a gift to this NPC: applies the relationship change via ConversationMemory
        /// and records today as the last gift day (one gift per NPC per in-game day).
        /// </summary>
        public void ReceiveGift(float affinityValue)
        {
            conversationMemory?.GiveGift(npcId, affinityValue);
        }

        /// <summary>
        /// Portrait sprite for this NPC, used by RelationshipUI. Falls back to the
        /// default dialogue's start-node speaker portrait if not explicitly assigned.
        /// </summary>
        public Sprite GetPortrait()
        {
            if (npcPortrait != null) return npcPortrait;

            var startNode = defaultDialogue?.GetStartNode();
            return startNode?.speakerPortrait;
        }

        /// <summary>
        /// Short bio/flavor text for this NPC, shown in RelationshipUI.
        /// </summary>
        public string GetBio() => npcBio;

        /// <summary>
        /// Current relationship level with this NPC (-100..100), via ConversationMemory.
        /// </summary>
        public float GetRelationshipLevel()
        {
            return conversationMemory != null ? conversationMemory.GetRelationshipLevel(npcId) : 0f;
        }

        /// <summary>
        /// Starts dialogue with this NPC
        /// </summary>
        public void StartDialogue()
        {
            var dialogueToShow = GetBestAvailableDialogue();
            if (dialogueToShow == null)
                return;

            if (dialogueUI == null)
                return;

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

            // Append "Gift" / "Relationship" as extra choices on the start node, so they
            // appear inside the dialogue menu instead of as floating screen buttons.
            dialogueUI.SetExtraStartNodeChoices(BuildExtraChoices());

            // Start the dialogue
            dialogueUI.StartDialogue(dialogueToShow);

            OnDialogueStarted?.Invoke(dialogueToShow);
        }

        /// <summary>
        /// Builds the "Gift" and "Relationship" choices appended to the start node of this
        /// NPC's dialogue. Both are exit choices: selecting one closes the dialogue box and
        /// opens the corresponding panel (GiftSelectionUI / RelationshipUI).
        /// </summary>
        private List<DialogueChoice> BuildExtraChoices()
        {
            var choices = new List<DialogueChoice>();

            if (enableGifting)
            {
                bool canGift = CanGiftToday();
                choices.Add(new DialogueChoice
                {
                    choiceText = canGift ? "Give a gift" : "Give a gift (already gifted today)",
                    isExitChoice = true,
                    choiceColor = canGift ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f),
                    onSelectedRuntime = canGift ? () =>
                    {
                        var giftUI = FindFirstObjectByType<GiftSelectionUI>();
                        if (giftUI != null)
                            giftUI.OpenForNpc(this);
                    } : null
                });
            }

            choices.Add(new DialogueChoice
            {
                choiceText = "View relationship",
                isExitChoice = true,
                onSelectedRuntime = () =>
                {
                    var relationshipUI = FindFirstObjectByType<RelationshipUI>();
                    if (relationshipUI != null)
                        relationshipUI.OpenForNpc(this);
                }
            });

            if (enableSeedShop)
            {
                choices.Add(new DialogueChoice
                {
                    choiceText = "Browse seeds",
                    isExitChoice = true,
                    onSelectedRuntime = () =>
                    {
                        var seedShopUI = FindFirstObjectByType<SeedShopUI>();
                        if (seedShopUI != null)
                            seedShopUI.Open();
                    }
                });
            }

            return choices;
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

        /// <summary>
        /// Returns lore entries unlocked at the current relationship level,
        /// ordered by required level ascending.
        /// </summary>
        public NpcLoreEntry[] GetUnlockedLore()
        {
            float level = GetRelationshipLevel();
            var unlocked = new List<NpcLoreEntry>();
            foreach (var entry in loreEntries)
            {
                if (level >= entry.requiredRelationship)
                    unlocked.Add(entry);
            }
            unlocked.Sort((a, b) => a.requiredRelationship.CompareTo(b.requiredRelationship));
            return unlocked.ToArray();
        }

        // Methods for InteractionManager
        public string GetInteractionPrompt() => $"Talk to {npcDisplayName}";

        public float GetInteractionRange()
        {
            return interactionRange;
        }

        public bool IsDialogueActive()
        {
            return isDialogueActive;
        }

        public string GetNPCDisplayName() => npcDisplayName;

        public string GetNPCId() => npcId;

        public void SetPromptVisibility(bool visible)
        {
            // InteractionManager drives proximity when active, so playerInRange
            // (used by CanInteract/Interact and GiftSelectionUI) must be kept in
            // sync here too — CheckPlayerDistance() only runs in fallback mode.
            playerInRange = visible;

            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(visible);

            }
            else
            {

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
    /// <summary>
    /// A single codex entry revealed when the player reaches <see cref="requiredRelationship"/>.
    /// </summary>
    [System.Serializable]
    public class NpcLoreEntry
    {
        [Tooltip("Minimum relationship level (-100..100) needed to see this entry.")]
        public float requiredRelationship = 0f;
        [Tooltip("Short header shown as a section title in the codex.")]
        public string title;
        [TextArea(2, 5)]
        [Tooltip("The lore text revealed at this tier.")]
        public string body;
    }
} // namespace SowurShield.Dialogue
