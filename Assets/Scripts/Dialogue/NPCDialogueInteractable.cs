using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.Localization;
using SowurShield.Core;

namespace SowurShield.Dialogue
{
    public class NPCDialogueInteractable : MonoBehaviour, IInteractable
    {
        [Header("NPC Configuration")]
        [SerializeField] private string npcId;
        [SerializeField] private string npcDisplayName;
        [SerializeField] private Sprite npcPortrait;
        [Tooltip("Localized bio shown in the Codex. Falls back to npcBioFallback when unset.")]
        [SerializeField] private LocalizedString npcBioLocalized;
        [TextArea(2, 5)]
        [Tooltip("Legacy raw bio. Only used when npcBioLocalized is empty — it does not translate.")]
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

        [Header("General Shop")]
        [Tooltip("Assign a ShopData asset to give this NPC a \"Let's trade\" dialogue choice. " +
                 "Independent of the seed shop — a merchant can have either, both, or neither.")]
        [SerializeField] private ShopData shopData;

        [Header("Codex / Lore")]
        [Tooltip("Lore entries revealed progressively in the Relationship codex as affinity grows.")]
        [SerializeField] private NpcLoreEntry[] loreEntries = new NpcLoreEntry[0];

        [Header("Gift Preferences")]
        [Tooltip("Item names this NPC loves. Worth 2.5x their base gift value.")]
        [SerializeField] private string[] lovedGifts = new string[0];
        [Tooltip("Item names this NPC likes. Worth 1.5x their base gift value.")]
        [SerializeField] private string[] likedGifts = new string[0];
        [Tooltip("Item names this NPC dislikes. Costs affinity instead of granting it (-1x).")]
        [SerializeField] private string[] dislikedGifts = new string[0];

        [Tooltip("Affinity granted for the first conversation with this NPC each day. 0 disables it.")]
        [SerializeField] private float dailyTalkAffinity = 1f;

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
        ///
        /// Prefer the <see cref="Item"/> overload, which applies this NPC's taste. This one
        /// takes a bare value and cannot know what was given, so it always applies 1x.
        /// </summary>
        public void ReceiveGift(float affinityValue)
        {
            conversationMemory?.GiveGift(npcId, affinityValue);
        }

        /// <summary>
        /// Gives a specific item as a gift, scaling its <see cref="Item.giftAffinityValue"/>
        /// by this NPC's taste for it. Returns the reaction so the caller can show it.
        /// </summary>
        public GiftReaction ReceiveGift(SowurShield.Inventory.Item item)
        {
            if (item == null) return GiftReaction.Neutral;

            GiftReaction reaction = GetReactionTo(item);
            float finalValue = item.giftAffinityValue * GetMultiplierFor(reaction);

            conversationMemory?.GiveGift(npcId, finalValue);
            RememberTasteDiscovered(item, reaction);

            return reaction;
        }

        /// <summary>
        /// How this NPC feels about an item, without giving it.
        /// Used by the gift UI to preview, and by the codex to list discovered tastes.
        /// </summary>
        public GiftReaction GetReactionTo(SowurShield.Inventory.Item item)
        {
            if (item == null) return GiftReaction.Neutral;

            // Matched on itemName (the stable internal ID), never the display name, which is
            // localized and would make preferences break in Portuguese and Spanish.
            string key = item.itemName;

            if (System.Array.IndexOf(lovedGifts, key) >= 0)    return GiftReaction.Loved;
            if (System.Array.IndexOf(likedGifts, key) >= 0)    return GiftReaction.Liked;
            if (System.Array.IndexOf(dislikedGifts, key) >= 0) return GiftReaction.Disliked;

            return GiftReaction.Neutral;
        }

        /// <summary>
        /// Affinity multiplier per reaction tier. Neutral is 1x so that every item authored
        /// before preferences existed keeps behaving exactly as it did.
        /// </summary>
        public static float GetMultiplierFor(GiftReaction reaction)
        {
            switch (reaction)
            {
                case GiftReaction.Loved:    return 2.5f;
                case GiftReaction.Liked:    return 1.5f;
                case GiftReaction.Disliked: return -1f;
                default:                    return 1f;
            }
        }

        /// <summary>
        /// Records that the player has learned this NPC's taste for an item, so the codex can
        /// list it. Only non-neutral reactions are worth remembering — "Maren doesn't care
        /// about this" is not a discovery.
        /// </summary>
        private void RememberTasteDiscovered(SowurShield.Inventory.Item item, GiftReaction reaction)
        {
            if (reaction == GiftReaction.Neutral || conversationMemory == null) return;

            conversationMemory.SetVariable(TasteKey(npcId, item.itemName), reaction.ToString());
        }

        /// <summary>
        /// The reaction the player has already discovered for this item, or null if they have
        /// never given it to this NPC. Deliberately does not fall back to
        /// <see cref="GetReactionTo"/> — the codex must show what was *learned*, not the answer.
        /// </summary>
        public GiftReaction? GetDiscoveredReaction(string itemName)
        {
            if (conversationMemory == null || string.IsNullOrEmpty(itemName)) return null;

            string stored = conversationMemory.GetVariable(TasteKey(npcId, itemName));
            if (string.IsNullOrEmpty(stored)) return null;

            GiftReaction parsed;
            return System.Enum.TryParse(stored, out parsed) ? parsed : (GiftReaction?)null;
        }

        /// <summary>
        /// Every item name this NPC has an opinion about, whether or not the player knows yet.
        /// The codex uses this to size the "N of M tastes discovered" progress line.
        /// </summary>
        public string[] GetAllPreferredItemNames()
        {
            var all = new List<string>();
            all.AddRange(lovedGifts);
            all.AddRange(likedGifts);
            all.AddRange(dislikedGifts);
            return all.ToArray();
        }

        private static string TasteKey(string npc, string itemName) => $"taste_{npc}_{itemName}";

        /// <summary>
        /// Portrait sprite for this NPC, used by RelationshipUI.
        ///
        /// Order: the explicitly assigned portrait, then this NPC's own art in
        /// Resources, then the dialogue's start-node portrait. The Resources step
        /// sits in front of the dialogue fallback on purpose — several NPCs have a
        /// start node still pointing at a shared placeholder from before they had
        /// portraits, which made the codex show the wrong face for them.
        /// </summary>
        public Sprite GetPortrait()
        {
            // The inspector reference is checked against the placeholder list too: the scene
            // has portrait_joana and friends wired directly on the component, so filtering
            // only inside LoadOwnPortrait left the silhouettes winning anyway.
            if (npcPortrait != null && !IsPlaceholderPortrait(npcPortrait)) return npcPortrait;

            Sprite own = LoadOwnPortrait();
            if (own != null) return own;

            // The villager's own world sprite, as a stand-in for a real portrait.
            //
            // Resources/Portraits holds 64x80 placeholders: featureless silhouettes with no
            // face at all, while every villager has 450x900 art with an actual drawn face
            // standing in the scene. Showing a blank silhouette next to their dialogue was
            // worse than showing the character themselves, so the sprite wins until the real
            // portraits are drawn. Reading it off the renderer also means an NPC added later
            // gets a face with no new art path to wire.
            var renderer = GetComponentInChildren<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null) return renderer.sprite;

            var startNode = defaultDialogue?.GetStartNode();
            return startNode?.speakerPortrait;
        }

        /// <summary>
        /// Looks up Resources/Portraits/portrait_{npcname}, matching the art naming
        /// convention. Cached, including a miss, so the codex does not hit Resources
        /// on every refresh.
        /// </summary>
        private Sprite LoadOwnPortrait()
        {
            if (_ownPortraitResolved) return _ownPortrait;
            _ownPortraitResolved = true;

            // npcId first: it is the stable identifier, while the display name is
            // localized and the GameObject name is whatever the scene calls it.
            string key = !string.IsNullOrEmpty(npcId) ? npcId
                       : !string.IsNullOrEmpty(npcDisplayName) ? npcDisplayName
                       : gameObject.name;
            if (string.IsNullOrEmpty(key)) return null;

            // "Tomás" -> "tomas": the scene uses accented display names while the
            // art files are plain ASCII.
            string slug = key.Trim().ToLowerInvariant();
            var sb = new System.Text.StringBuilder(slug.Length);
            foreach (char c in slug.Normalize(System.Text.NormalizationForm.FormD))
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                    != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            slug = sb.ToString().Replace(" ", "_");

            Sprite loaded = Resources.Load<Sprite>($"Portraits/portrait_{slug}");

            // Skip the placeholder portraits so the caller falls through to the villager's
            // world sprite. Eight of the nine files in Resources/Portraits are 64x80
            // five-colour silhouettes with no face; only Maren's is real art. Matching on
            // the known names rather than sniffing pixel counts keeps this obvious to read
            // and trivial to undo -- delete a name from the list as each portrait is drawn.
            if (loaded != null && IsPlaceholderPortrait(loaded))
                loaded = null;

            _ownPortrait = loaded;
            return _ownPortrait;
        }

        /// <summary>
        /// Villagers whose Resources/Portraits file is still a blank silhouette. Remove a
        /// name here the moment its real portrait lands and it takes over again.
        /// </summary>
        private static readonly System.Collections.Generic.HashSet<string> PlaceholderPortraits =
            new System.Collections.Generic.HashSet<string>
            {
                "bento", "clara", "elias", "isabela", "joana", "nara", "rui", "tomas",
            };

        /// <summary>
        /// True for the blank silhouettes in Resources/Portraits, matched on the asset name
        /// so it catches them whether they arrive from Resources or from an inspector slot.
        /// </summary>
        private static bool IsPlaceholderPortrait(Sprite sprite)
        {
            if (sprite == null) return false;
            string n = sprite.name;
            const string prefix = "portrait_";
            if (!n.StartsWith(prefix)) return false;

            string slug = n.Substring(prefix.Length).ToLowerInvariant();

            // The importer slices these as Multiple, so the sprite is "portrait_joana_0"
            // rather than "portrait_joana". Drop a trailing _<digits> before matching.
            int underscore = slug.LastIndexOf('_');
            if (underscore > 0 && slug.Substring(underscore + 1).All(char.IsDigit))
                slug = slug.Substring(0, underscore);

            return PlaceholderPortraits.Contains(slug);
        }

        private Sprite _ownPortrait;
        private bool _ownPortraitResolved;

        /// <summary>
        /// Short bio/flavor text for this NPC, shown in RelationshipUI.
        ///
        /// Prefers the localized entry and falls back to the legacy raw string, so an NPC
        /// that has not been migrated yet still shows its bio instead of going blank.
        /// The raw field does not translate — that was the Codex-stays-in-Portuguese bug.
        /// </summary>
        public string GetBio()
        {
            string localized = npcBioLocalized.SafeGetLocalizedString();
            return string.IsNullOrEmpty(localized) ? npcBio : localized;
        }

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

            // First conversation of the day is worth a little affinity. Awarded here rather
            // than on dialogue *completion* so that it cannot be farmed by re-opening and
            // closing the same conversation — TryAwardDailyTalk is itself idempotent per day.
            conversationMemory?.TryAwardDailyTalk(npcId, dailyTalkAffinity);

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

            // Hand over this NPC's face for nodes that carry no portrait of their own.
            // Only 8 of the project's 66 dialogue nodes set speakerPortrait, so without this
            // the frame stayed empty for almost every conversation even though all nine
            // villagers have portrait art in Resources/Portraits.
            dialogueUI.SetDefaultSpeakerPortrait(GetPortrait(), SpeakerPosition.Left);

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
                    choiceText = canGift
                        ? new LocalizedString("Dialogue", "dialogue.choice.give_gift")
                        : new LocalizedString("Dialogue", "dialogue.choice.give_gift_already_today"),
                    isExitChoice = true,
                    choiceColor = canGift ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f),
                    onSelectedRuntime = canGift ? () =>
                    {
                        var giftUI = FindFirstObjectByType<GiftSelectionUI>(FindObjectsInactive.Include);
                        if (giftUI != null)
                            giftUI.OpenForNpc(this);
                    } : null
                });
            }

            choices.Add(new DialogueChoice
            {
                choiceText = new LocalizedString("Dialogue", "dialogue.choice.view_relationship"),
                isExitChoice = true,
                onSelectedRuntime = () =>
                {
                    var relationshipUI = FindFirstObjectByType<RelationshipUI>(FindObjectsInactive.Include);
                    if (relationshipUI != null)
                        relationshipUI.OpenForNpc(this);
                }
            });

            if (enableSeedShop)
            {
                choices.Add(new DialogueChoice
                {
                    choiceText = new LocalizedString("Dialogue", "dialogue.choice.browse_seeds"),
                    isExitChoice = true,
                    onSelectedRuntime = () =>
                    {
                        var seedShopUI = FindFirstObjectByType<SeedShopUI>(FindObjectsInactive.Include);
                        if (seedShopUI != null)
                            seedShopUI.Open();
                    }
                });
            }

            if (shopData != null)
            {
                choices.Add(new DialogueChoice
                {
                    choiceText = new LocalizedString("Dialogue", "dialogue.choice.browse_shop"),
                    isExitChoice = true,
                    onSelectedRuntime = () =>
                    {
                        var shopUI = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
                        if (shopUI != null)
                            shopUI.OpenShop(shopData);
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

            // Restore the "Press E" prompt if the player is still in range. The
            // InteractionManager only pushes SetPromptVisibility on interactable
            // *transitions*, and after a dialogue the current interactable is still
            // this NPC — so without this the prompt stays hidden until the player
            // walks out of range and back (KNOWN_BUGS: Maren re-interact).
            if (player != null &&
                Vector3.Distance(player.position, transform.position) <= GetInteractionRange())
            {
                SetPromptVisibility(true);
            }

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

        /// <summary>
        /// Lore entries the player has not reached yet, sorted by requirement. The codex shows
        /// these as locked rows: a codex that visibly has more to reveal is the point of a
        /// codex, whereas hiding them makes a partly-filled one look finished.
        /// </summary>
        public NpcLoreEntry[] GetLockedLore()
        {
            float level = GetRelationshipLevel();
            var locked = new List<NpcLoreEntry>();
            foreach (var entry in loreEntries)
            {
                if (level < entry.requiredRelationship)
                    locked.Add(entry);
            }
            locked.Sort((a, b) => a.requiredRelationship.CompareTo(b.requiredRelationship));
            return locked.ToArray();
        }

        /// <summary>
        /// Total number of lore entries authored for this NPC, unlocked or not.
        /// </summary>
        public int GetTotalLoreCount() => loreEntries != null ? loreEntries.Length : 0;

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
    /// How an NPC feels about a gifted item. Order matters for display: worst to best is
    /// Disliked → Neutral → Liked → Loved.
    /// </summary>
    public enum GiftReaction
    {
        Disliked,
        Neutral,
        Liked,
        Loved
    }

    /// <summary>
    /// A single codex entry revealed when the player reaches <see cref="requiredRelationship"/>.
    /// </summary>
    [System.Serializable]
    public class NpcLoreEntry
    {
        [Tooltip("Minimum relationship level (-100..100) needed to see this entry.")]
        public float requiredRelationship = 0f;

        [Tooltip("Localized section title. Falls back to the raw title when unset.")]
        public LocalizedString titleLocalized;
        [Tooltip("Legacy raw title. Only used when titleLocalized is empty — it does not translate.")]
        public string title;

        [Tooltip("Localized lore text. Falls back to the raw body when unset.")]
        public LocalizedString bodyLocalized;
        [TextArea(2, 5)]
        [Tooltip("Legacy raw lore text. Only used when bodyLocalized is empty — it does not translate.")]
        public string body;

        /// <summary>
        /// Title in the active language, falling back to the legacy raw string so an
        /// unmigrated entry still renders instead of showing an empty codex row.
        /// </summary>
        public string GetTitle()
        {
            string localized = titleLocalized.SafeGetLocalizedString();
            return string.IsNullOrEmpty(localized) ? title : localized;
        }

        /// <summary>Lore body in the active language, with the same fallback as <see cref="GetTitle"/>.</summary>
        public string GetBody()
        {
            string localized = bodyLocalized.SafeGetLocalizedString();
            return string.IsNullOrEmpty(localized) ? body : localized;
        }
    }
} // namespace SowurShield.Dialogue
