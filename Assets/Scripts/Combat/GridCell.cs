using UnityEngine;

namespace SowurShield.Combat
{

/// <summary>
/// Represents a single cell in the 9x5 combat grid.
/// Each cell can hold one CombatUnit (animal or enemy) and displays visual feedback.
///
/// SETUP IN UNITY:
/// - This component is added automatically by GridManager
/// - GridManager will assign gridX, gridY coordinates
/// - Visual quad/sprite will be created as child object
/// </summary>
public class GridCell : MonoBehaviour
{
    [Header("Grid Position")]
    [Tooltip("X coordinate in grid (0-8, left to right)")]
    public int gridX;

    [Tooltip("Y coordinate in grid (0-4, bottom to top)")]
    public int gridY;

    [Header("Cell State")]
    [Tooltip("Unit currently occupying this cell (null if empty)")]
    public CombatUnit occupyingUnit = null;

    [Tooltip("Is this cell on the player's side? (columns 6-8, RIGHT side)")]
    public bool isPlayerSide = false;

    [Header("Visual Components")]
    [Tooltip("Visual representation of the cell (quad/sprite)")]
    public GameObject cellVisual;

    [Tooltip("Highlight object shown on hover")]
    public GameObject highlightVisual;

    // Cell colors
    private Color playerSideColor = new Color(0.3f, 0.8f, 0.3f, 0.5f); // Green
    private Color enemySideColor = new Color(0.8f, 0.3f, 0.3f, 0.5f);  // Red
    private Color highlightColor = new Color(1f, 1f, 0.3f, 0.7f);      // Yellow

    // Components
    private Renderer cellRenderer;
    private Renderer highlightRenderer;

    /// <summary>
    /// Initialize this grid cell with position and side
    /// Called by GridManager during grid creation
    /// </summary>
    public void Initialize(int x, int y, bool playerSide)
    {
        gridX = x;
        gridY = y;
        isPlayerSide = playerSide;

        // Create visual representation
        CreateCellVisual();
        CreateHighlightVisual();

        // Set default color based on side
        UpdateCellColor();
    }

    /// <summary>
    /// Create the base visual quad for this cell
    /// </summary>
    private void CreateCellVisual()
    {
        // Create quad mesh (2D: facing camera)
        cellVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cellVisual.name = "CellVisual";
        cellVisual.transform.SetParent(transform);
        cellVisual.transform.localPosition = Vector3.zero;
        cellVisual.transform.localRotation = Quaternion.identity; // Face camera (no rotation)
        cellVisual.transform.localScale = new Vector3(0.9f, 0.9f, 1f); // Slightly smaller than 1x1 for grid lines

        // Get renderer
        cellRenderer = cellVisual.GetComponent<Renderer>();

        // Create material with Sprites/Default shader (Unlit/Color is stripped from builds)
        Material mat = new Material(Shader.Find("Sprites/Default"));
        cellRenderer.material = mat;

        // Ensure grid cells render behind sprites
        cellRenderer.sortingLayerName = "Default";
        cellRenderer.sortingOrder = -10;

        // Remove collider (we'll use GridManager for mouse detection)
        Destroy(cellVisual.GetComponent<Collider>());
    }

    /// <summary>
    /// Create highlight visual (hidden by default)
    /// </summary>
    private void CreateHighlightVisual()
    {
        highlightVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        highlightVisual.name = "HighlightVisual";
        highlightVisual.transform.SetParent(transform);
        highlightVisual.transform.localPosition = new Vector3(0, 0, -0.01f); // Slightly in front
        highlightVisual.transform.localRotation = Quaternion.identity; // Face camera
        highlightVisual.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

        // Get renderer and set color
        highlightRenderer = highlightVisual.GetComponent<Renderer>();

        // Create material with Sprites/Default shader (Unlit/Color is stripped from builds)
        Material highlightMat = new Material(Shader.Find("Sprites/Default"));
        highlightMat.color = highlightColor;
        highlightRenderer.material = highlightMat;

        // Ensure highlight renders behind sprites but above cell base
        highlightRenderer.sortingLayerName = "Default";
        highlightRenderer.sortingOrder = -9;

        // Hide by default
        highlightVisual.SetActive(false);

        // Remove collider
        Destroy(highlightVisual.GetComponent<Collider>());
    }

    /// <summary>
    /// Update cell color based on side
    /// </summary>
    private void UpdateCellColor()
    {
        if (cellRenderer != null)
        {
            cellRenderer.material.color = isPlayerSide ? playerSideColor : enemySideColor;
        }
    }

    /// <summary>
    /// Check if this cell is empty (no unit occupying)
    /// </summary>
    public bool IsEmpty()
    {
        return occupyingUnit == null;
    }

    /// <summary>
    /// Place a unit in this cell
    /// </summary>
    public bool PlaceUnit(CombatUnit unit)
    {
        if (!IsEmpty())
        {
            return false;
        }

        occupyingUnit = unit;

        // Position unit at this cell's world position
        if (unit != null)
        {
            unit.transform.position = transform.position + new Vector3(0, 0, -0.5f); // In front of grid
            unit.gridPosition = new Vector2Int(gridX, gridY);
        }

        return true;
    }

    /// <summary>
    /// Remove unit from this cell
    /// </summary>
    public void ClearUnit()
    {
        occupyingUnit = null;
    }

    /// <summary>
    /// Show highlight (e.g., on mouse hover)
    /// </summary>
    public void ShowHighlight()
    {
        if (highlightVisual != null)
        {
            highlightVisual.SetActive(true);
        }
    }

    /// <summary>
    /// Hide highlight
    /// </summary>
    public void HideHighlight()
    {
        if (highlightVisual != null)
        {
            highlightVisual.SetActive(false);
        }
    }

    /// <summary>
    /// Get world position of this cell's center
    /// </summary>
    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// Calculate distance to another cell (for range calculations)
    /// </summary>
    public float DistanceTo(GridCell other)
    {
        if (other == null) return float.MaxValue;

        // Use grid coordinates for accurate distance
        int dx = Mathf.Abs(gridX - other.gridX);
        int dy = Mathf.Abs(gridY - other.gridY);

        // Return Manhattan distance (grid-based movement)
        return dx + dy;
    }

    /// <summary>
    /// Check if this cell is adjacent to another cell (within 1 tile)
    /// </summary>
    public bool IsAdjacentTo(GridCell other)
    {
        if (other == null) return false;
        return DistanceTo(other) == 1;
    }
}

} // namespace SowurShield.Combat
