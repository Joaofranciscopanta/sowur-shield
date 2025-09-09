using UnityEngine;

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
            Debug.LogWarning("ConversationMemory not found. Effect cannot be executed.");
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
                
            case EffectType.GiveItem:
                // This would integrate with your inventory system
                // For now, we'll track it in memory
                memory.GiveItem(effectKey, Mathf.RoundToInt(numericValue));
                break;
                
            case EffectType.TakeItem:
                memory.TakeItem(effectKey, Mathf.RoundToInt(numericValue));
                break;
                
            case EffectType.PlaySound:
                PlaySoundEffect();
                break;
                
            case EffectType.TriggerEvent:
                TriggerCustomEvent();
                break;
                
            default:
                Debug.LogWarning($"Unknown effect type: {effectType}");
                break;
        }
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
            Debug.Log($"Would play sound: {effectKey}");
        }
    }
    
    private void TriggerCustomEvent()
    {
        // Integration point for custom game events
        // This could trigger quest updates, cutscenes, etc.
        Debug.Log($"Triggering custom event: {effectKey} with value: {effectValue}");
        
        // Example: You could use Unity Events or a custom event system
        // GameEvents.Instance?.TriggerEvent(effectKey, effectValue);
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
    TriggerEvent       // Trigger custom game event
}