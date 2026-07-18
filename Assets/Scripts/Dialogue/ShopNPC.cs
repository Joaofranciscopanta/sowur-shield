using UnityEngine;
using SowurShield.Core;

namespace SowurShield.Dialogue
{

/// <summary>
/// Attach to an NPC GameObject to make it open a shop when interacted with (E key / left-click).
/// Requires a Collider2D (IsTrigger recommended) and a ShopUI in the scene.
///
/// SETUP IN UNITY:
///   1. Add this script to the NPC GameObject
///   2. Assign shopData (ShopData ScriptableObject)
///   3. Assign shopUI (ShopUI in scene)
///   4. Add Collider2D (circle, IsTrigger = true)
/// </summary>
public class ShopNPC : MonoBehaviour, IInteractable
{
    [Header("Shop")]
    [SerializeField] private ShopData shopData;
    [SerializeField] private ShopUI shopUI;

    [Header("Appearance")]
    [SerializeField] private string npcName = "Merchant";

    [Header("Interaction")]
    [SerializeField] private float interactionRange = 2f;

    // =========================================================================
    // IInteractable
    // =========================================================================

    public bool CanInteract()
    {
        return shopData != null && shopUI != null;
    }

    public void Interact()
    {
        if (shopData == null || shopUI == null)
        {
            Debug.LogWarning($"[ShopNPC] '{npcName}' is missing ShopData or ShopUI reference.");
            return;
        }
        shopUI.OpenShop(shopData);
    }

    public string GetInteractionPrompt()
    {
        return $"[E] Shop — {npcName}";
    }

    public float GetInteractionRange() => interactionRange;
}

} // namespace SowurShield.Dialogue
