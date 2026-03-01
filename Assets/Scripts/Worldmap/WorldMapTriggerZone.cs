using UnityEngine;
using SowurShield.Core;
using SowurShield.Combat;

namespace SowurShield.Worldmap
{

/// <summary>
/// Trigger zone in farm scene that opens the Team Assembler UI.
/// Place this on a collider where you want combat to be accessible.
///
/// SETUP IN UNITY:
/// 1. Create GameObject with Collider2D (set IsTrigger = true)
/// 2. Add this script
/// 3. Configure zone name and difficulty
/// 4. Ensure TeamAssemblerUI exists in scene
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WorldMapTriggerZone : MonoBehaviour, IInteractable
{

    public GameObject WorldMap;

    [Header("Zone Configuration")]
    [Tooltip("Display name for this combat zone")]
    [SerializeField] private string zoneName = "Dark Forest";

    [Tooltip("Difficulty level (affects enemy stats and rewards)")]
    [SerializeField] [Range(1, 10)] private int zoneDifficulty = 1;

    [Header("Visual Feedback")]
    [Tooltip("Sprite to show when player is near")]
    [SerializeField] private GameObject interactPrompt;

    [Tooltip("Interaction range")]
    [SerializeField] private float interactionRange = 2f;
    private Transform playerTransform;
    private bool playerNearby = false;

    private void Awake()
    {
        // Ensure collider is trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Hide prompt initially
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
    }

    private void Start()
    {
        // Register with InteractionManager
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.RegisterInteractable(this);
        }
    }

    private void OnDestroy()
    {
        // Unregister from InteractionManager
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.UnregisterInteractable(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerTransform = other.transform;
            playerNearby = true;

            // Show interact prompt
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(true);
            }

        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerTransform = null;
            playerNearby = false;

            // Hide interact prompt
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(false);
            }

        }
    }

    // ============================================================================
    // IINTERACTABLE IMPLEMENTATION
    // ============================================================================

    public void Interact()
    {
        if (!playerNearby)
        {
            return;
        }

        // Don't reopen if UI is already open (prevents destroying drag operations!)
        if (TeamAssemblerUI.Instance != null && TeamAssemblerUI.Instance.IsOpen())
        {
            return;
        }


        // Open Team Assembler UI
        if (TeamAssemblerUI.Instance != null)
        {
            WorldMap.SetActive(true);
            Time.timeScale = 0f; // pausa o jogo
            GameMenuManager.Instance.SetMapOpen(true);
        }
        else
        {
        }
    }

    public string GetInteractPrompt()
    {
        return $"Press E to enter {zoneName} (Difficulty: {zoneDifficulty})";
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public string GetInteractionPrompt() => $"Enter {zoneName}";

    public float GetInteractionRange()
    {
        return interactionRange;
    }

    public bool CanInteract()
    {
        return playerNearby;
    }

    // ============================================================================
    // GIZMOS (for editor visualization)
    // ============================================================================

    private void OnDrawGizmos()
    {
        // Draw interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        // Draw zone area
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawCube(transform.position, col.bounds.size);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw zone name
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"{zoneName}\nDifficulty: {zoneDifficulty}");
        #endif
    }
}

} // namespace SowurShield.Worldmap
