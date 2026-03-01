using UnityEngine;
using TMPro;
using SowurShield.Animals;

namespace SowurShield.Combat
{

/// <summary>
/// Wrapper component that connects Animal data to the combat system.
/// Each CombatUnit represents one animal or enemy in battle.
///
/// SETUP IN UNITY:
/// 1. Create prefab from: GameObject → 3D Object → Sphere
/// 2. Add this component
/// 3. Add TextMeshPro child for name display
/// 4. Assign Animal reference (for player units) or configure manually (for enemies)
///
/// RESPONSIBILITIES:
/// - Stores combat state (current health, turn gauge)
/// - Bridges Animal stats with combat mechanics
/// - Handles visual representation during battle
/// - Tracks grid position
/// </summary>
public class CombatUnit : MonoBehaviour
{
    [Header("Unit Data")]
    [Tooltip("Reference to Animal component (null for enemies)")]
    public Animal animal;

    [Tooltip("Display name for this unit")]
    public string unitName = "Unit";

    [Tooltip("Is this unit controlled by player? (false = enemy)")]
    public bool isPlayerUnit = true;

    [Header("Combat Stats - Runtime")]
    [Tooltip("Current health in this battle")]
    public float currentHealth;

    [Tooltip("Turn gauge (0-100, acts when reaches 100)")]
    public float turnGauge = 0f;

    [Tooltip("Current position on grid")]
    public Vector2Int gridPosition;

    [Header("Visual Components")]
    [Tooltip("Visual representation (sphere/sprite)")]
    public GameObject visualObject;

    [Tooltip("TextMeshPro for displaying name")]
    public TextMeshProUGUI nameText;

    [Tooltip("Health bar UI component")]
    public UnitHealthBar healthBarUI;

    [Tooltip("Health bar prefab (assigned in Inspector or GridManager)")]
    public GameObject healthBarPrefab;

    [Tooltip("Turn gauge bar slider (optional)")]
    public UnityEngine.UI.Slider turnGaugeBar;

    // Cached combat stats (from Animal or manually set)
    private float maxHealth;
    private float attack;
    private float defense;
    private float speed;
    private float accuracy;

    // Visual colors
    private Color playerColor = new Color(0.3f, 0.5f, 1f); // Blue
    private Color enemyColor = new Color(1f, 0.3f, 0.3f);  // Red

    // Flash coroutine tracking
    private Coroutine currentFlashCoroutine = null;

    /// <summary>
    /// Initialize this CombatUnit from an Animal
    /// </summary>
    public void InitializeFromAnimal(Animal sourceAnimal, bool playerControlled)
    {
        animal = sourceAnimal;
        isPlayerUnit = playerControlled;

        if (animal == null)
        {
            return;
        }

        // Get stats from Animal
        AnimalCombatStats stats = animal.GetCombatStats();
        if (stats == null)
        {
            return;
        }

        // Cache stats
        maxHealth = stats.MaxHealth;
        currentHealth = maxHealth;
        attack = stats.CurrentAttack;
        defense = stats.CurrentDefense;
        speed = stats.CurrentSpeed;
        accuracy = stats.CurrentAccuracy;

        // Set display name
        unitName = animal.GetDisplayName();

        // Setup visuals
        SetupVisuals();

        // Setup health bar
        SetupHealthBar();

    }

    /// <summary>
    /// Initialize this CombatUnit with manual stats (for testing or enemies)
    /// NOTE: Set isPlayerUnit BEFORE calling this!
    /// </summary>
    public void InitializeAsEnemy(string name, float hp, float atk, float def, float spd)
    {
        // NOTE: isPlayerUnit should be set before calling this method!
        // Don't override it here
        unitName = name;

        // Set stats manually
        maxHealth = hp;
        currentHealth = maxHealth;
        attack = atk;
        defense = def;
        speed = spd;
        accuracy = 1.0f; // Enemies have 100% accuracy by default

        // Setup visuals
        SetupVisuals();

        // Setup health bar
        SetupHealthBar();

    }

    /// <summary>
    /// Setup visual components
    /// </summary>
    private void SetupVisuals()
    {
        // Try to use sprite from animal data first
        if (animal != null && animal.AnimalData != null && animal.AnimalData.idleSprite != null)
        {
            CreateSpriteVisual(animal.AnimalData.idleSprite);
        }
        else
        {
            // Fall back to sphere for testing/enemies without sprites
            CreateSphereVisual();
        }
    }

    /// <summary>
    /// Create sprite-based visual
    /// </summary>
    private void CreateSpriteVisual(Sprite sprite)
    {
        // Check if we already have a sprite renderer (created by CombatTestSpawner)
        SpriteRenderer existingSpriteRenderer = GetComponent<SpriteRenderer>();

        if (existingSpriteRenderer != null && existingSpriteRenderer.sprite != null)
        {
            // Already has a sprite - just use it as-is (CombatTestSpawner set it up)
            visualObject = gameObject;
        }
        else if (existingSpriteRenderer != null)
        {
            // Has sprite renderer but no sprite - assign it
            visualObject = gameObject;
            existingSpriteRenderer.sprite = sprite;
            existingSpriteRenderer.color = Color.white;
            existingSpriteRenderer.sortingOrder = 10;
        }
        else
        {
            // No sprite renderer - create one
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = 10;
            visualObject = gameObject;
        }

        // Normalize sprite size to fit in grid cell (0.8 units fits nicely in 1x1 cell)
        NormalizeSpriteSize(existingSpriteRenderer != null ? existingSpriteRenderer : GetComponent<SpriteRenderer>());

        // Flip player units to face right (towards enemies)
        if (isPlayerUnit)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    /// <summary>
    /// Normalize sprite size to fit in grid cell (makes all sprites roughly same size)
    /// </summary>
    private void NormalizeSpriteSize(SpriteRenderer sr)
    {
        if (sr == null || sr.sprite == null) return;

        // Target size: sprite should be about 0.8 units tall (fits in 1x1 grid cell with padding)
        float targetHeight = 0.8f;

        // Get sprite's pixel height and pixels-per-unit
        float spriteHeight = sr.sprite.rect.height;
        float pixelsPerUnit = sr.sprite.pixelsPerUnit;

        // Calculate world height of sprite at scale 1
        float worldHeight = spriteHeight / pixelsPerUnit;

        // Calculate scale needed to reach target height
        float scale = targetHeight / worldHeight;

        // Apply uniform scale
        transform.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// Create sphere visual (fallback for testing)
    /// </summary>
    private void CreateSphereVisual()
    {
        // Use existing renderer on this GameObject if present, or create new visual
        Renderer existingRenderer = GetComponent<Renderer>();

        if (existingRenderer != null)
        {
            // Use this GameObject's renderer
            visualObject = gameObject;

            // Color based on side
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = isPlayerUnit ? playerColor : enemyColor;
            existingRenderer.material = mat;
        }
        else if (visualObject == null)
        {
            // Create visual sphere if not present
            visualObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualObject.name = "Visual";
            visualObject.transform.SetParent(transform);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localScale = Vector3.one * 0.5f;

            // Color based on side
            Renderer renderer = visualObject.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = isPlayerUnit ? playerColor : enemyColor;
            renderer.material = mat;

            // Remove collider (we use grid for positioning)
            Destroy(visualObject.GetComponent<Collider>());
        }

        // Note: Name text disabled for 2D sprites - will add proper UI later

        // Update health bar if present
        UpdateHealthBar();
        UpdateTurnGaugeBar();
    }

    /// <summary>
    /// Check if this unit is still alive
    /// </summary>
    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    /// <summary>
    /// Take damage from an attack
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;


        // Update visuals
        UpdateHealthBar();
        FlashDamage();

        // Check death
        if (!IsAlive())
        {
            Die();
        }
    }

    /// <summary>
    /// Heal this unit
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;


        UpdateHealthBar();
    }

    /// <summary>
    /// Handle unit death
    /// </summary>
    private void Die()
    {

        // Visual feedback
        if (visualObject != null)
        {
            Renderer renderer = visualObject.GetComponent<Renderer>();
            renderer.material.color = Color.gray;
        }

        // TODO: Play death animation, disable unit, etc.
    }

    /// <summary>
    /// Update turn gauge (called by TurnManager each frame)
    /// </summary>
    public void UpdateTurnGauge(float deltaTime)
    {
        if (!IsAlive()) return;

        // Fill gauge based on speed
        turnGauge += speed * deltaTime;

        // Clamp to 100
        if (turnGauge > 100f)
        {
            turnGauge = 100f;
        }

        UpdateTurnGaugeBar();
    }

    /// <summary>
    /// Check if this unit is ready to act (gauge full)
    /// </summary>
    public bool IsReadyToAct()
    {
        return IsAlive() && turnGauge >= 100f;
    }

    /// <summary>
    /// Reset turn gauge after acting (overflow system - subtract 100)
    /// </summary>
    public void ResetTurnGauge()
    {
        turnGauge -= 100f;
        if (turnGauge < 0) turnGauge = 0f; // Safety clamp
        UpdateTurnGaugeBar();
    }

    /// <summary>
    /// Setup health bar UI
    /// </summary>
    private void SetupHealthBar()
    {
        // Create health bar from prefab if available
        if (healthBarPrefab != null && healthBarUI == null)
        {
            healthBarUI = UnitHealthBar.CreateHealthBar(this, healthBarPrefab);
        }
        else if (healthBarUI != null)
        {
            // Health bar already exists, just initialize it
            healthBarUI.Initialize(this);
        }

        // Update health bar
        UpdateHealthBar();
    }

    /// <summary>
    /// Public method to create health bar (called after prefab is assigned)
    /// </summary>
    public void CreateHealthBar()
    {
        if (healthBarPrefab != null && healthBarUI == null)
        {
            SetupHealthBar();
        }
    }

    /// <summary>
    /// Update health bar visual
    /// </summary>
    private void UpdateHealthBar()
    {
        if (healthBarUI != null)
        {
            healthBarUI.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// Update turn gauge bar visual
    /// </summary>
    private void UpdateTurnGaugeBar()
    {
        if (turnGaugeBar != null)
        {
            turnGaugeBar.value = turnGauge / 100f;
        }
    }

    /// <summary>
    /// Flash red when taking damage (visual feedback)
    /// </summary>
    private void FlashDamage()
    {
        if (visualObject != null)
        {
            // Stop any existing flash to prevent color overlap
            if (currentFlashCoroutine != null)
            {
                StopCoroutine(currentFlashCoroutine);
            }

            currentFlashCoroutine = StartCoroutine(FlashColorCoroutine(Color.red));
        }
    }

    /// <summary>
    /// Flash yellow when attacking (visual feedback)
    /// </summary>
    public void FlashAttack()
    {
        if (visualObject != null)
        {
            // Stop any existing flash to prevent color overlap
            if (currentFlashCoroutine != null)
            {
                StopCoroutine(currentFlashCoroutine);
            }

            currentFlashCoroutine = StartCoroutine(FlashColorCoroutine(Color.yellow));
        }
    }

    private System.Collections.IEnumerator FlashColorCoroutine(Color flashColor)
    {
        // Try sprite renderer first
        SpriteRenderer spriteRenderer = visualObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Flash to specified color
            spriteRenderer.color = flashColor;

            // Wait
            yield return new WaitForSeconds(0.1f);

            // Return to white (default sprite color)
            if (IsAlive())
            {
                spriteRenderer.color = Color.white;
            }
        }
        else
        {
            // Fall back to 3D renderer (spheres)
            Renderer renderer = visualObject.GetComponent<Renderer>();
            if (renderer == null) yield break;

            // Flash to specified color
            renderer.material.color = flashColor;

            // Wait
            yield return new WaitForSeconds(0.1f);

            // Return to team color
            if (IsAlive())
            {
                renderer.material.color = isPlayerUnit ? playerColor : enemyColor;
            }
        }

        currentFlashCoroutine = null;
    }

    private void ResetColor()
    {
        if (visualObject != null && IsAlive())
        {
            Renderer renderer = visualObject.GetComponent<Renderer>();
            renderer.material.color = isPlayerUnit ? playerColor : enemyColor;
        }
    }

    // ============================================================================
    // PUBLIC ACCESSORS (for other combat systems to read stats)
    // ============================================================================

    public float GetMaxHealth() => maxHealth;
    public float GetAttack() => attack;
    public float GetDefense() => defense;
    public float GetSpeed() => speed;
    public float GetAccuracy() => accuracy;

    public float GetHealthPercent() => currentHealth / maxHealth;

    /// <summary>
    /// Get reference to source Animal (null for enemies)
    /// </summary>
    public Animal GetSourceAnimal() => animal;
}

} // namespace SowurShield.Combat
