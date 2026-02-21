using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the health bar display above a combat unit.
/// Shows current/max HP and updates in real-time during combat.
///
/// SETUP IN UNITY:
/// 1. Create UI Canvas set to "World Space"
/// 2. Add Slider component for health bar
/// 3. Add TextMeshPro for HP text (optional)
/// 4. Attach this script to the health bar GameObject
/// 5. Assign references in Inspector
/// 6. CombatUnit will automatically create and manage this
/// </summary>
public class UnitHealthBar : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("Slider component for health bar")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("Text displaying current/max HP")]
    [SerializeField] private TextMeshProUGUI healthText;

    [Tooltip("Text displaying unit name (optional)")]
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Visual Settings")]
    [Tooltip("Color when health is high (>70%)")]
    [SerializeField] private Color highHealthColor = Color.green;

    [Tooltip("Color when health is medium (30-70%)")]
    [SerializeField] private Color mediumHealthColor = Color.yellow;

    [Tooltip("Color when health is low (<30%)")]
    [SerializeField] private Color lowHealthColor = Color.red;

    [Tooltip("Fill image of the slider (auto-found if not assigned)")]
    [SerializeField] private Image fillImage;

    [Header("Positioning")]
    [Tooltip("Offset above unit (world space)")]
    [SerializeField] private Vector3 offsetAboveUnit = new Vector3(0, 0.6f, 0);

    [Tooltip("Should this face the camera?")]
    [SerializeField] private bool faceCamera = true;

    // Target unit
    private CombatUnit targetUnit;

    // Camera reference
    private Camera mainCamera;

    private void Awake()
    {
        // Auto-find slider if not assigned
        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>();
        }

        // Auto-find fill image
        if (fillImage == null && healthSlider != null)
        {
            fillImage = healthSlider.fillRect?.GetComponent<Image>();
        }

        // Get main camera
        mainCamera = Camera.main;
    }

    /// <summary>
    /// Initialize health bar for a specific unit
    /// </summary>
    public void Initialize(CombatUnit unit)
    {
        targetUnit = unit;

        if (targetUnit == null)
        {
            return;
        }

        // Set unit name
        if (nameText != null)
        {
            nameText.text = targetUnit.unitName;
        }

        // Set initial values
        UpdateHealthBar(targetUnit.currentHealth, targetUnit.GetMaxHealth());
    }

    private void LateUpdate()
    {
        if (targetUnit == null) return;

        // Position above unit
        transform.position = targetUnit.transform.position + offsetAboveUnit;

        // Face camera if enabled
        if (faceCamera && mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }

    /// <summary>
    /// Update health bar display
    /// </summary>
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthSlider == null) return;

        // Update slider value
        float healthPercent = maxHealth > 0 ? currentHealth / maxHealth : 0f;
        healthSlider.value = healthPercent;

        // Update color based on health percentage
        UpdateHealthColor(healthPercent);

        // Update text
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }
    }

    /// <summary>
    /// Update fill color based on health percentage
    /// </summary>
    private void UpdateHealthColor(float healthPercent)
    {
        if (fillImage == null) return;

        if (healthPercent > 0.7f)
        {
            fillImage.color = highHealthColor;
        }
        else if (healthPercent > 0.3f)
        {
            fillImage.color = mediumHealthColor;
        }
        else
        {
            fillImage.color = lowHealthColor;
        }
    }

    /// <summary>
    /// Show health bar
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Hide health bar
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Static factory method to create health bar from prefab
    /// </summary>
    public static UnitHealthBar CreateHealthBar(CombatUnit unit, GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject healthBarObj = Instantiate(prefab, unit.transform.position, Quaternion.identity);
        UnitHealthBar healthBar = healthBarObj.GetComponent<UnitHealthBar>();

        if (healthBar == null)
        {
            Destroy(healthBarObj);
            return null;
        }

        healthBar.Initialize(unit);
        return healthBar;
    }
}
