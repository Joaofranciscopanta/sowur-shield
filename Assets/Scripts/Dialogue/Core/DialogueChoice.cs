using UnityEngine;

namespace SowurShield.Dialogue
{
    [System.Serializable]
    public class DialogueChoice
    {
        [Header("Choice Text")]
        [TextArea(2, 4)]
        public string choiceText;

        [Header("Navigation")]
        public string nextNodeId;

        [Header("Requirements")]
        public DialogueCondition[] requirements = new DialogueCondition[0];

        [Header("Choice Effects")]
        public DialogueEffect[] effects = new DialogueEffect[0];

        [Header("Display Settings")]
        public bool isExitChoice = false; // Marks this as a choice that ends the conversation
        public Color choiceColor = Color.white; // Color tint for this choice in UI

        [Header("Editor Helper")]
        [TextArea(1, 3)]
        public string editorNote; // Internal note for designers

        /// <summary>
        /// Checks if this choice should be available to the player
        /// </summary>
        public bool IsAvailable()
        {
            if (requirements == null || requirements.Length == 0)
                return true;

            // All requirements must be met
            foreach (var requirement in requirements)
            {
                if (!requirement.IsConditionMet())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Executes all effects associated with this choice
        /// </summary>
        public void ExecuteEffects()
        {
            if (effects == null) return;

            foreach (var effect in effects)
            {
                effect.Execute();
            }
        }

        /// <summary>
        /// Gets display text for the choice, with potential modifications
        /// </summary>
        public string GetDisplayText()
        {
            // Future enhancement: Could modify text based on conditions
            // e.g., "[Requires 100 coins] Buy the sword" if player doesn't have enough
            return choiceText;
        }

        /// <summary>
        /// Validates that this choice is properly configured
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(choiceText))
            {
                return false;
            }

            if (!isExitChoice && string.IsNullOrEmpty(nextNodeId))
            {
                return false;
            }

            return true;
        }
    }
} // namespace SowurShield.Dialogue
