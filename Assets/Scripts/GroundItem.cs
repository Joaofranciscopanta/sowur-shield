using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using SowurShield.Inventory;

namespace SowurShield.Core
{

public class GroundItem : MonoBehaviour, IInteractable, ISaveable
{
    public Item item;
    public int quantity = 1;
    private bool playerInRange = false;
    public bool itemPicked = false;

    // Visual components
    private SpriteRenderer spriteRenderer;
    private float initialY;

    // Visual effects settings
    [Header("Visual Effects")]
    public bool enableFloating = true;
    public float floatHeight = 0.1f;
    public float floatSpeed = 1.0f;
    public bool enableRotation = true;
    public float rotationSpeed = 30.0f;

    [Header("Feedback Effects")]
    public GameObject pickupEffectPrefab;
    public AudioClip pickupSound;

    [Header("Visuals")]
    public Color highlightColor = new Color(1f, 1f, 0.5f, 1f);
    private Color originalColor;

    [Header("Area Collection")]
    public bool enableAreaCollection = true;
    public float collectionRadius = 1.5f;
    public GameObject areaCollectionEffectPrefab;
    public AudioClip areaSoundEffect;

    [Header("Item Movement")]
    public float moveToPlayerSpeed = 15f;
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float minimumMoveTime = 0.1f;
    public float maximumMoveTime = 0.3f;

    // Reference to the player and their inventory
    private Transform playerTransform;
    private SowurShield.Inventory.Inventory playerInventory;

    // Hover label (world-space, built procedurally)
    private GameObject hoverLabel;
    private bool hoverShowing = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisual();
        SaveManager.Instance?.RegisterSaveable(this);
    }

    private void OnDestroy()
    {
        if (hoverLabel != null)
            Destroy(hoverLabel);
        SaveManager.Instance?.UnregisterSaveable(this);
    }

    private void Start()
    {
        initialY = transform.position.y;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        BuildHoverLabel();
        StartCoroutine(SpawnAnimation());
    }

    private void Update()
    {
        if (!itemPicked && enableFloating)
        {
            float y = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, initialY + y, transform.position.z);
        }

        if (!itemPicked && enableRotation)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }

        if (!itemPicked && item != null && hoverLabel != null)
        {
            bool over = IsMouseOver();
            if (over && !hoverShowing)
            {
                hoverLabel.SetActive(true);
                hoverShowing = true;
            }
            else if (!over && hoverShowing)
            {
                hoverLabel.SetActive(false);
                hoverShowing = false;
            }
        }
    }

    private void BuildHoverLabel()
    {
        if (item == null) return;

        hoverLabel = new GameObject("HoverLabel");
        hoverLabel.transform.SetParent(transform, false);
        hoverLabel.transform.localPosition = new Vector3(0f, 0.6f, 0f);

        var canvas = hoverLabel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 200;
        hoverLabel.transform.localScale = Vector3.one * 0.012f;

        var rt = hoverLabel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120, 22);

        var bg = new GameObject("Bg");
        bg.transform.SetParent(hoverLabel.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        var labelObj = new GameObject("Text");
        labelObj.transform.SetParent(hoverLabel.transform, false);
        var tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = item.GetDisplayName();
        tmp.fontSize = 14;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(4, 0);
        labelRt.offsetMax = new Vector2(-4, 0);

        hoverLabel.SetActive(false);
    }

    private bool IsMouseOver()
    {
        if (Camera.main == null) return false;
        Vector2 worldMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(worldMouse, Vector2.zero);
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }

    private IEnumerator SpawnAnimation()
    {
        transform.position += new Vector3(0, 0.3f, 0);
        Vector3 targetPos = new Vector3(transform.position.x, initialY, transform.position.z);

        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, elapsed/duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerTransform = other.transform;

            // Get reference to the player's inventory
            PlayerMove playerMove = other.GetComponent<PlayerMove>();
            if (playerMove != null)
            {
                playerInventory = playerMove.GetInventory();
            }

            // Highlight when player approaches
            if (spriteRenderer != null)
            {
                spriteRenderer.color = highlightColor;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            // Remove highlight when player walks away
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    public string GetInteractionPrompt() => item != null ? $"Pick up {item.GetDisplayName()}" : "Pick up";
    public bool CanInteract() => !itemPicked && playerInRange;
    public float GetInteractionRange() => collectionRadius;

    public void Interact()
    {
        if (!itemPicked && playerTransform != null && playerInventory != null)
        {
            if (enableAreaCollection)
            {
                // Collect all items in the area
                CollectAllItemsInArea();
            }
            else
            {
                // Collect only this item
                PickupItem();
            }
        }
    }

    private void CollectAllItemsInArea()
    {
        // Find all nearby GroundItems
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, collectionRadius);
        List<GroundItem> itemsToCollect = new List<GroundItem>();

        // Add this item first to guarantee it is collected
        itemsToCollect.Add(this);

        // Find other items in the area
        foreach (Collider2D col in colliders)
        {
            GroundItem otherItem = col.GetComponent<GroundItem>();
            if (otherItem != null && otherItem != this && !otherItem.itemPicked)
            {
                itemsToCollect.Add(otherItem);
            }
        }

        // If only this item exists, use normal pickup
        if (itemsToCollect.Count == 1)
        {
            PickupItem();
            return;
        }

        // Start collection with movement toward the player
        StartCoroutine(CollectItemsWithAnimation(itemsToCollect));
    }

    private IEnumerator CollectItemsWithAnimation(List<GroundItem> itemsToCollect)
    {
        int collectedCount = 0;
        List<GroundItem> successfullyCollected = new List<GroundItem>();

        // First check whether all items fit in the inventory
        foreach (GroundItem groundItem in itemsToCollect)
        {
            groundItem.itemPicked = true;

            if (playerInventory != null)
            {
                bool canAdd = playerInventory.CanAdd(groundItem.item, 1);
                if (!canAdd)
                {
                    groundItem.itemPicked = false;
                }
            }
        }

        // Play movement animation for items that will be collected
        List<Coroutine> movementCoroutines = new List<Coroutine>();

        foreach (GroundItem groundItem in itemsToCollect)
        {
            if (groundItem.itemPicked)
            {
                // Calculate movement time based on distance
                float distance = Vector2.Distance(groundItem.transform.position, playerTransform.position);
                float moveTime = Mathf.Lerp(minimumMoveTime, maximumMoveTime,
                                           distance / collectionRadius);

                // Start movement coroutine and store the reference
                Coroutine moveCo = StartCoroutine(groundItem.MoveToPlayer(playerTransform, moveTime));
                movementCoroutines.Add(moveCo);

                // Add to the successfully collected list
                successfullyCollected.Add(groundItem);
            }
        }

        // Small stagger delay to vary movement start times
        foreach (GroundItem groundItem in successfullyCollected)
        {
            yield return new WaitForSeconds(0.05f);

            // Add to inventory while items are moving
            bool added = playerInventory.Add(groundItem.item, 1);
            if (added)
            {
                collectedCount++;
            }
        }

        // Wait for all movement coroutines to finish
        foreach (Coroutine co in movementCoroutines)
        {
            yield return co;
        }

        // Destroy collected items
        foreach (GroundItem groundItem in successfullyCollected)
        {
            groundItem.PlayPickupEffects();
            Destroy(groundItem.gameObject, 0.1f);
        }

        // If multiple items were collected, play the area collection effect
        if (collectedCount > 1)
        {
            PlayAreaCollectionEffect(collectedCount);
        }
    }

    private IEnumerator MoveToPlayer(Transform target, float duration)
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = target.position;
        float elapsed = 0;

        // Disable floating while moving toward player
        enableFloating = false;

        // Slightly increase rotation speed during movement
        float originalRotationSpeed = rotationSpeed;
        rotationSpeed = originalRotationSpeed * 2;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float curvedT = movementCurve.Evaluate(t);

            // Move toward player using the animation curve
            transform.position = Vector3.Lerp(startPosition, endPosition, curvedT);

            // Gradually shrink as it approaches the player
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.5f, curvedT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap to player center
        transform.position = endPosition;
    }

    private void PlayAreaCollectionEffect(int count)
    {
        // Area collection visual effect
        if (areaCollectionEffectPrefab != null && playerTransform != null)
        {
            GameObject effect = Instantiate(areaCollectionEffectPrefab,
                                           playerTransform.position,
                                           Quaternion.identity);

            // Scale effect intensity based on item count
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var emission = ps.emission;
                emission.rateOverTime = count * 5;
            }

            Destroy(effect, 2f);
        }

        // Area collection sound effect
        if (areaSoundEffect != null && playerTransform != null)
        {
            AudioSource.PlayClipAtPoint(areaSoundEffect, playerTransform.position);
        }

        // Show floating text with collected count
        ShowFloatingText($"+{count} items");
    }

    private void ShowFloatingText(string message)
    {
        // Implement here if a floating text system is available
    }

    private void PickupItem()
    {
        if (playerInventory != null && playerTransform != null)
        {
            bool canAdd = playerInventory.CanAdd(item, 1);
            if (canAdd)
            {
                itemPicked = true;
                StartCoroutine(CollectWithAnimation());
            }
            else
            {
                // Inventory full - item stays on ground
            }
        }
    }

    private IEnumerator CollectWithAnimation()
    {
        // Move toward player before adding to inventory
        yield return StartCoroutine(MoveToPlayer(playerTransform, minimumMoveTime));

        // Add to inventory after movement completes
        bool added = playerInventory.Add(item, quantity);
        if (added)
        {
            PlayPickupEffects();
            Destroy(gameObject, 0.1f);
        }
        else
        {
            // Something went wrong — reset pickup state
            itemPicked = false;
        }
    }

    public void PlayPickupEffects()
    {
        // Pickup particle effect
        if (pickupEffectPrefab != null && playerTransform != null)
        {
            GameObject effect = Instantiate(pickupEffectPrefab,
                                           playerTransform.position,
                                           Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Pickup sound effect
        if (pickupSound != null && playerTransform != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, playerTransform.position);
        }
    }

    private void UpdateVisual()
    {
        if (item != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = item.icon;
        }
    }

    public void SetItem(Item newItem)
    {
        item = newItem;
        quantity = 1;
        UpdateVisual();
        RebuildHoverLabel();
    }

    public void SetItem(Item newItem, int newQuantity)
    {
        item = newItem;
        quantity = Mathf.Max(1, newQuantity);
        UpdateVisual();
        RebuildHoverLabel();
    }

    public void SetItemStack(ItemStack itemStack)
    {
        if (itemStack != null && !itemStack.IsEmpty)
        {
            item = itemStack.item;
            quantity = itemStack.quantity;
            UpdateVisual();
            RebuildHoverLabel();
        }
    }

    private void RebuildHoverLabel()
    {
        if (hoverLabel != null)
            Destroy(hoverLabel);
        hoverShowing = false;
        if (Application.isPlaying)
            BuildHoverLabel();
    }

    // ============================================================================
    // ISAVEABLE IMPLEMENTATION
    // ============================================================================

    public void SaveData(GameData gameData)
    {
        if (itemPicked)
            gameData.worldData.worldFlags[$"grounditem_picked_{gameObject.name}"] = true;
    }

    public void LoadData(GameData gameData)
    {
        string key = $"grounditem_picked_{gameObject.name}";
        if (gameData.worldData.worldFlags.TryGetValue(key, out bool picked) && picked)
            Destroy(gameObject);
    }

    // Visualize collection radius in the editor
    private void OnDrawGizmosSelected()
    {
        if (enableAreaCollection)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, collectionRadius);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, collectionRadius);
        }
    }
}

} // namespace SowurShield.Core
