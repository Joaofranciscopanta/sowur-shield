using UnityEngine;
using SowurShield.Inventory;
using SowurShield.Core;

namespace SowurShield.Dialogue
{
    [System.Serializable]
    public class DialogueEffect
    {
        [Header("Effect Type")]
        public EffectType effectType;

        [Header("Effect Parameters")]
        public string effectKey;
        public string effectValue;
        public float numericValue;

        [Header("Description")]
        [TextArea(2, 4)]
        public string description; // Editor helper

        public void Execute()
        {
            var memory = ConversationMemory.Instance;
            if (memory == null)
            {
                return;
            }

            switch (effectType)
            {
                case EffectType.SetVariable:
                    memory.SetVariable(effectKey, effectValue);
                    break;

                case EffectType.ModifyRelationship:
                    memory.ModifyRelationship(effectKey, numericValue);
                    break;

                case EffectType.SetQuestStatus:
                    memory.SetQuestStatus(effectKey, effectValue);
                    break;

                case EffectType.StartQuest:
                    StartQuest();
                    break;

                case EffectType.GiveItem:
                    GiveItemToInventory(effectKey, Mathf.RoundToInt(numericValue));
                    break;

                case EffectType.TakeItem:
                    TakeItemFromInventory(effectKey, Mathf.RoundToInt(numericValue));
                    break;

                case EffectType.PlaySound:
                    PlaySoundEffect();
                    break;

                case EffectType.TriggerEvent:
                    TriggerCustomEvent();
                    break;

                default:
                    break;
            }
        }

        private void GiveItemToInventory(string itemName, int count)
        {
            if (string.IsNullOrEmpty(itemName) || count <= 0) return;

            Item item = ItemDatabase.GetItem(itemName);
            if (item == null)
            {
                Debug.LogWarning($"[DialogueEffect] GiveItem: item '{itemName}' not found in ItemDatabase.");
                return;
            }

            Inventory.Inventory inv = Object.FindFirstObjectByType<Inventory.Inventory>();
            if (inv == null)
            {
                Debug.LogWarning("[DialogueEffect] GiveItem: no Inventory found in scene.");
                return;
            }

            if (!inv.AddItem(item, count))
                Debug.LogWarning($"[DialogueEffect] GiveItem: inventory full — could not add {count}x '{itemName}'.");
        }

        private void TakeItemFromInventory(string itemName, int count)
        {
            if (string.IsNullOrEmpty(itemName) || count <= 0) return;

            Item item = ItemDatabase.GetItem(itemName);
            if (item == null)
            {
                Debug.LogWarning($"[DialogueEffect] TakeItem: item '{itemName}' not found in ItemDatabase.");
                return;
            }

            Inventory.Inventory inv = Object.FindFirstObjectByType<Inventory.Inventory>();
            if (inv == null)
            {
                Debug.LogWarning("[DialogueEffect] TakeItem: no Inventory found in scene.");
                return;
            }

            if (!inv.RemoveItem(item, count))
                Debug.LogWarning($"[DialogueEffect] TakeItem: could not remove {count}x '{itemName}' — not enough in inventory.");
        }

        private void PlaySoundEffect()
        {
            // Integration point for your audio system
            var audioSource = Object.FindFirstObjectByType<AudioSource>();
            if (audioSource != null && !string.IsNullOrEmpty(effectKey))
            {
                // You would load the audio clip by name/path
                // AudioClip clip = Resources.Load<AudioClip>(effectKey);
                // if (clip != null) audioSource.PlayOneShot(clip);
            }
        }

        private void TriggerCustomEvent()
        {
            // Integration point for custom game events
            // This could trigger quest updates, cutscenes, etc.

            // Example: You could use Unity Events or a custom event system
            // GameEvents.Instance?.TriggerEvent(effectKey, effectValue);
        }

        /// <summary>effectKey is the QuestData.questId to start. No-ops if already active/completed or prerequisites aren't met.</summary>
        private void StartQuest()
        {
            if (string.IsNullOrEmpty(effectKey)) return;

            if (QuestManager.Instance == null)
            {
                Debug.LogWarning("[DialogueEffect] StartQuest: no QuestManager in scene.");
                return;
            }

            QuestManager.Instance.StartQuest(effectKey);
        }
    }

    public enum EffectType
    {
        SetVariable,        // Set a custom game variable
        ModifyRelationship, // Change relationship level with NPC
        SetQuestStatus,     // Update quest status
        GiveItem,          // Add items to player inventory
        TakeItem,          // Remove items from player inventory
        PlaySound,         // Play a sound effect
        TriggerEvent,      // Trigger custom game event
        StartQuest         // Start a quest by QuestData.questId (effectKey)
    }
} // namespace SowurShield.Dialogue
