using UnityEngine;
using SowurShield.Inventory;

namespace SowurShield.Dialogue
{
    [System.Serializable]
    public class DialogueCondition
    {
        [Header("Condition Settings")]
        public ConditionType conditionType;
        public string conditionKey;
        public ConditionOperator conditionOperator;
        public string conditionValue;
        public bool invertCondition = false;

        [Header("Description")]
        [TextArea(2, 4)]
        public string description; // Editor helper for understanding what this condition does

        public bool IsConditionMet()
        {
            bool result = false;

            // Get conversation memory instance
            var memory = ConversationMemory.Instance;
            if (memory == null)
            {
                return invertCondition; // Return inverted false if no memory system
            }

            switch (conditionType)
            {
                case ConditionType.ConversationCompleted:
                    result = memory.HasCompletedConversation(conditionKey);
                    break;

                case ConditionType.ChoiceMade:
                    result = memory.HasMadeChoice(conditionKey, conditionValue);
                    break;

                case ConditionType.RelationshipLevel:
                    if (float.TryParse(conditionValue, out float targetLevel))
                    {
                        float currentLevel = memory.GetRelationshipLevel(conditionKey);
                        result = EvaluateNumericCondition(currentLevel, targetLevel, conditionOperator);
                    }
                    break;

                case ConditionType.QuestStatus:
                    // This would integrate with your quest system if available
                    // For now, we'll use a simple string comparison
                    string currentStatus = memory.GetQuestStatus(conditionKey);
                    result = string.Equals(currentStatus, conditionValue, System.StringComparison.OrdinalIgnoreCase);
                    break;

                case ConditionType.VariableCheck:
                    string variableValue = memory.GetVariable(conditionKey);
                    result = EvaluateStringCondition(variableValue, conditionValue, conditionOperator);
                    break;

                case ConditionType.InventoryItem:
                    if (int.TryParse(conditionValue, out int requiredAmount))
                    {
                        int currentAmount = 0;
                        Item item = ItemDatabase.GetItem(conditionKey);
                        Inventory.Inventory inv = Object.FindFirstObjectByType<Inventory.Inventory>();
                        if (item != null && inv != null)
                            currentAmount = inv.GetItemCount(item);
                        result = EvaluateNumericCondition(currentAmount, requiredAmount, conditionOperator);
                    }
                    break;

                case ConditionType.Always:
                    result = true;
                    break;

                default:
                    result = false;
                    break;
            }

            return invertCondition ? !result : result;
        }

        private bool EvaluateNumericCondition(float current, float target, ConditionOperator op)
        {
            switch (op)
            {
                case ConditionOperator.Equals: return Mathf.Approximately(current, target);
                case ConditionOperator.NotEquals: return !Mathf.Approximately(current, target);
                case ConditionOperator.GreaterThan: return current > target;
                case ConditionOperator.LessThan: return current < target;
                case ConditionOperator.GreaterOrEqual: return current >= target;
                case ConditionOperator.LessOrEqual: return current <= target;
                default: return false;
            }
        }

        private bool EvaluateStringCondition(string current, string target, ConditionOperator op)
        {
            if (current == null) current = "";
            if (target == null) target = "";

            switch (op)
            {
                case ConditionOperator.Equals:
                    return string.Equals(current, target, System.StringComparison.OrdinalIgnoreCase);
                case ConditionOperator.NotEquals:
                    return !string.Equals(current, target, System.StringComparison.OrdinalIgnoreCase);
                case ConditionOperator.Contains:
                    return current.ToLower().Contains(target.ToLower());
                default:
                    return false;
            }
        }
    }

    public enum ConditionType
    {
        Always,                // Always true - useful for default branches
        ConversationCompleted, // Has the player completed a specific conversation?
        ChoiceMade,           // Has the player made a specific choice before?
        RelationshipLevel,    // Check relationship level with NPC
        QuestStatus,          // Check quest completion status
        VariableCheck,        // Check custom game variable
        InventoryItem         // Check if player has specific items
    }

    public enum ConditionOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual,
        Contains
    }
} // namespace SowurShield.Dialogue
