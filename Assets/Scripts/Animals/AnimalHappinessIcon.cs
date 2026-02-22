using UnityEngine;

/// <summary>
/// Lightweight floating icon above an animal showing its happiness mood.
/// Displays one of 3 sprites: happy (≥70), neutral (40-69), or sad (<40).
/// Updates once per second for performance. Attach to an animal GameObject
/// or let Animal.cs add it dynamically.
/// </summary>
public class AnimalHappinessIcon : MonoBehaviour
{
    [Header("Mood Sprites")]
    [Tooltip("Displayed when happiness ≥ 70")]
    [SerializeField] private Sprite happySprite;
    [Tooltip("Displayed when happiness is 40-69")]
    [SerializeField] private Sprite neutralSprite;
    [Tooltip("Displayed when happiness < 40")]
    [SerializeField] private Sprite sadSprite;

    [Header("Position")]
    [SerializeField] private float verticalOffset = 1.2f;
    [SerializeField] private float iconScale = 0.4f;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 1f;
    [SerializeField] private bool showIcon = true;

    private Animal animal;
    private SpriteRenderer iconRenderer;
    private GameObject iconObject;
    private float lastUpdateTime;
    private Sprite currentSprite;

    private void Awake()
    {
        animal = GetComponent<Animal>();
        if (animal == null)
        {
            animal = GetComponentInParent<Animal>();
        }

        CreateIconObject();
    }

    private void CreateIconObject()
    {
        // Create a child object for the icon so it renders independently
        iconObject = new GameObject("HappinessIcon");
        iconObject.transform.SetParent(transform);
        iconObject.transform.localPosition = new Vector3(0f, verticalOffset, 0f);
        iconObject.transform.localScale = new Vector3(iconScale, iconScale, 1f);

        iconRenderer = iconObject.AddComponent<SpriteRenderer>();
        iconRenderer.sortingOrder = 10; // Render above the animal
        iconRenderer.enabled = showIcon;
    }

    private void Update()
    {
        if (animal == null || iconRenderer == null) return;

        // Position the icon above the animal (in case it moves)
        if (iconObject != null)
        {
            iconObject.transform.localPosition = new Vector3(0f, verticalOffset, 0f);
            // Counter-rotate to keep icon upright even if animal sprite rotates
            iconObject.transform.rotation = Quaternion.identity;
        }

        // Only update mood sprite once per interval for performance
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;

        UpdateMoodSprite();
    }

    private void UpdateMoodSprite()
    {
        if (!showIcon || iconRenderer == null) return;

        float happiness = animal.GetHappiness();
        Sprite targetSprite;

        if (happiness >= 70f)
            targetSprite = happySprite;
        else if (happiness >= 40f)
            targetSprite = neutralSprite;
        else
            targetSprite = sadSprite;

        // Only update if sprite changed
        if (targetSprite != currentSprite)
        {
            currentSprite = targetSprite;
            iconRenderer.sprite = targetSprite;
        }

        iconRenderer.enabled = showIcon && targetSprite != null;
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>Show or hide the happiness icon.</summary>
    public void SetVisible(bool visible)
    {
        showIcon = visible;
        if (iconRenderer != null)
            iconRenderer.enabled = visible && currentSprite != null;
    }

    /// <summary>Force an immediate mood update.</summary>
    public void ForceUpdate()
    {
        lastUpdateTime = 0f;
        UpdateMoodSprite();
    }

    private void OnDestroy()
    {
        if (iconObject != null)
            Destroy(iconObject);
    }
}
