using UnityEngine;

namespace SowurShield.Dialogue
{
    [System.Serializable]
    public class DialogueNode
    {
        [Header("Node Identity")]
        public string nodeId;

        [Header("Speaker Information")]
        public string speakerName;
        public Sprite speakerPortrait;
        public EmotionType speakerEmotion = EmotionType.Neutral;
        public SpeakerPosition speakerPosition = SpeakerPosition.Left;

        [Header("Dialogue Content")]
        [TextArea(3, 6)]
        public string dialogueText;

        [Header("Node Behavior")]
        public NodeType nodeType = NodeType.Dialogue;

        [Header("Player Choices")]
        public DialogueChoice[] choices = new DialogueChoice[0];

        [Header("Auto-Continue")]
        public string nextNodeId; // For linear dialogue or after choices are exhausted
        public float autoAdvanceDelay = 0f; // 0 = wait for player input, >0 = auto advance after delay

        [Header("Conditions")]
        public DialogueCondition[] displayConditions = new DialogueCondition[0];

        [Header("Effects")]
        public DialogueEffect[] nodeEffects = new DialogueEffect[0]; // Effects that trigger when this node is displayed

        [Header("Audio")]
        public AudioClip voiceClip; // Optional voice acting
        public AudioClip ambientSound; // Optional ambient sound for this node

        [Header("Editor Helpers")]
        [TextArea(2, 4)]
        public string editorNote;
        public Vector2 editorPosition; // For visual node editor

        /// <summary>
        /// Checks if this node should be displayed based on its conditions
        /// </summary>
        public bool ShouldDisplay()
        {
            if (displayConditions == null || displayConditions.Length == 0)
                return true;

            // All conditions must be met
            foreach (var condition in displayConditions)
            {
                if (!condition.IsConditionMet())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets available choices for this node
        /// </summary>
        public DialogueChoice[] GetAvailableChoices()
        {
            if (choices == null || choices.Length == 0)
                return new DialogueChoice[0];

            var availableChoices = new System.Collections.Generic.List<DialogueChoice>();

            foreach (var choice in choices)
            {
                if (choice.IsAvailable())
                    availableChoices.Add(choice);
            }

            return availableChoices.ToArray();
        }

        /// <summary>
        /// Executes all effects associated with this node
        /// </summary>
        public void ExecuteNodeEffects()
        {
            if (nodeEffects == null) return;

            foreach (var effect in nodeEffects)
            {
                effect.Execute();
            }
        }

        /// <summary>
        /// Validates that this node is properly configured
        /// </summary>
        public bool IsValid()
        {
            bool isValid = true;

            if (string.IsNullOrEmpty(nodeId))
            {
                isValid = false;
            }

            if (string.IsNullOrEmpty(dialogueText) && nodeType == NodeType.Dialogue)
            {
            }

            if (nodeType == NodeType.Choice && (choices == null || choices.Length == 0))
            {
                isValid = false;
            }

            // Validate all choices
            if (choices != null)
            {
                foreach (var choice in choices)
                {
                    if (!choice.IsValid())
                        isValid = false;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Gets the display name for the speaker, handling empty names gracefully
        /// </summary>
        public string GetSpeakerDisplayName()
        {
            return string.IsNullOrEmpty(speakerName) ? "???" : speakerName;
        }

        /// <summary>
        /// Determines if this node has any choices for the player
        /// </summary>
        public bool HasChoices()
        {
            return GetAvailableChoices().Length > 0;
        }
    }

    public enum NodeType
    {
        Dialogue,  // Standard dialogue node
        Choice,    // Node that presents choices to player
        Event      // Special node that triggers events/cutscenes
    }

    public enum EmotionType
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Surprised,
        Confused,
        Excited,
        Worried
    }

    public enum SpeakerPosition
    {
        Left,
        Right,
        Center
    }
} // namespace SowurShield.Dialogue
