using UnityEngine;
using SowurShield.Core;

namespace SowurShield.Animals
{

/// <summary>
/// Attach to an NPC/signpost GameObject to open the AnimalMarketUI when interacted with.
/// Requires a Collider2D (IsTrigger recommended) and an AnimalMarketUI in the scene.
///
/// SETUP IN UNITY:
///   1. Add this script to the NPC/signpost GameObject
///   2. Assign marketData (AnimalMarketData ScriptableObject)
///   3. AnimalMarketUI.Instance is resolved automatically — no manual UI wiring needed
/// </summary>
public class AnimalMarketNPC : MonoBehaviour, IInteractable
{
    [Header("Market")]
    [SerializeField] private AnimalMarketData marketData;

    [Header("Appearance")]
    [SerializeField] private string npcName = "Animal Trader";

    [Header("Interaction")]
    [SerializeField] private float interactionRange = 2f;

    private void Awake()
    {
        // Fallback for missing Inspector wiring — the shipped SampleScene NPC had a
        // null marketData, leaving the market permanently unopenable.
        if (marketData == null)
        {
            marketData = Resources.Load<AnimalMarketData>("AnimalMarkets/FarmAnimalMarket");
            if (marketData != null)
                Debug.LogWarning($"[AnimalMarketNPC] '{npcName}' had no marketData assigned — loaded FarmAnimalMarket from Resources as fallback.");
        }
    }

    public bool CanInteract()
    {
        return marketData != null && AnimalMarketUI.Instance != null;
    }

    public void Interact()
    {
        if (marketData == null)
        {
            Debug.LogWarning($"[AnimalMarketNPC] '{npcName}' is missing AnimalMarketData.");
            return;
        }
        if (AnimalMarketUI.Instance == null)
        {
            Debug.LogWarning($"[AnimalMarketNPC] '{npcName}' could not find an AnimalMarketUI in the scene.");
            return;
        }
        AnimalMarketUI.Instance.OpenMarket(marketData);
    }

    public string GetInteractionPrompt()
    {
        return $"[E] Animal Market — {npcName}";
    }

    public float GetInteractionRange() => interactionRange;
}

} // namespace SowurShield.Animals
