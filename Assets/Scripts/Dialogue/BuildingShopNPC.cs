using UnityEngine;
using SowurShield.Core;

namespace SowurShield.Dialogue
{

/// <summary>
/// Attach to an NPC/signpost GameObject to open the farm BuildingShopUI when interacted with.
/// Requires a Collider2D (IsTrigger recommended) and a BuildingShopUI in the scene.
///
/// SETUP IN UNITY:
///   1. Add this script to the NPC/signpost GameObject
///   2. Add Collider2D (circle, IsTrigger = true)
///   3. BuildingShopUI.Instance is resolved automatically — no manual wiring needed
/// </summary>
public class BuildingShopNPC : MonoBehaviour, IInteractable
{
    [Header("Appearance")]
    [SerializeField] private string npcName = "Builder";

    [Header("Interaction")]
    [SerializeField] private float interactionRange = 2f;

    public bool CanInteract()
    {
        return BuildingShopUI.Instance != null;
    }

    public void Interact()
    {
        if (BuildingShopUI.Instance == null)
        {
            Debug.LogWarning($"[BuildingShopNPC] '{npcName}' could not find a BuildingShopUI in the scene.");
            return;
        }
        BuildingShopUI.Instance.OpenShop();
    }

    public string GetInteractionPrompt()
    {
        return $"[E] Buildings — {npcName}";
    }

    public float GetInteractionRange() => interactionRange;
}

} // namespace SowurShield.Dialogue
