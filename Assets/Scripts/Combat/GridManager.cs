using UnityEngine;
using System.Collections.Generic;

namespace SowurShield.Combat
{

/// <summary>
/// Manages the 9x5 combat grid for auto-chess battles.
/// Creates grid cells, handles unit placement, and provides grid query methods.
///
/// GRID LAYOUT (LEFT vs RIGHT):
/// - 9 columns (X: 0-8, left to right)
/// - 5 rows (Y: 0-4, bottom to top)
/// - Columns 0-5: ENEMY side (LEFT, red) - 30 cells
/// - Columns 6-8: PLAYER side (RIGHT, green) - 15 cells
///
/// SETUP IN UNITY:
/// 1. Create empty GameObject named "GridManager"
/// 2. Add this component
/// 3. GridManager will auto-generate grid on Start()
/// 4. Place in Combat scene
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    [Tooltip("Number of columns (default: 9)")]
    [SerializeField] private int gridWidth = 9;

    [Tooltip("Number of rows (default: 5)")]
    [SerializeField] private int gridHeight = 5;

    [Tooltip("Size of each cell in world units")]
    [SerializeField] private float cellSize = 1f;

    [Tooltip("X columns that are player-controlled (6-8, RIGHT side)")]
    [SerializeField] private int playerColumns = 3; // Columns 6, 7, 8 (rightmost)

    [Header("Grid Visuals")]
    [Tooltip("Parent object to hold all grid cells")]
    [SerializeField] private Transform gridParent;

    [Header("UI Prefabs")]
    [Tooltip("Health bar prefab to assign to all spawned units")]
    [SerializeField] private GameObject healthBarPrefab;

    [Header("Runtime Data")]
    [Tooltip("2D array storing all grid cells [x, y]")]
    public GridCell[,] grid;

    // Grid bounds (for camera positioning, etc.)
    public Bounds GridBounds { get; private set; }

    // Singleton pattern
    public static GridManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Create grid on start
        GenerateGrid();
    }

    /// <summary>
    /// Generate the entire 9x5 grid
    /// </summary>
    public void GenerateGrid()
    {
        // Create parent object if not assigned
        if (gridParent == null)
        {
            GameObject parentObj = new GameObject("Grid");
            parentObj.transform.SetParent(transform);
            gridParent = parentObj.transform;
        }

        // Initialize 2D array
        grid = new GridCell[gridWidth, gridHeight];

        // Calculate grid center offset to center the grid at world origin (2D: X, Y)
        float gridWorldWidth = gridWidth * cellSize;
        float gridWorldHeight = gridHeight * cellSize;
        Vector3 gridOffset = new Vector3(-gridWorldWidth / 2f + cellSize / 2f, -gridWorldHeight / 2f + cellSize / 2f, 0);

        // Generate each cell
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Create cell GameObject
                GameObject cellObj = new GameObject($"Cell_{x}_{y}");
                cellObj.transform.SetParent(gridParent);

                // Position cell in world space (2D: X, Y plane instead of X, Z)
                Vector3 cellPosition = gridOffset + new Vector3(x * cellSize, y * cellSize, 0);
                cellObj.transform.position = cellPosition;

                // Add GridCell component
                GridCell cell = cellObj.AddComponent<GridCell>();

                // Determine if this is player side (columns 6-8, RIGHT side)
                bool isPlayerSide = x >= (gridWidth - playerColumns);

                // Initialize cell
                cell.Initialize(x, y, isPlayerSide);

                // Store in grid array
                grid[x, y] = cell;
            }
        }

        // Calculate grid bounds
        GridBounds = new Bounds(
            gridParent.position,
            new Vector3(gridWorldWidth, 0.1f, gridWorldHeight)
        );
    }

    /// <summary>
    /// Get cell at specific grid coordinates
    /// </summary>
    public GridCell GetCell(int x, int y)
    {
        if (!IsValidPosition(x, y))
        {
            return null;
        }

        return grid[x, y];
    }

    /// <summary>
    /// Get cell at Vector2Int position
    /// </summary>
    public GridCell GetCell(Vector2Int position)
    {
        return GetCell(position.x, position.y);
    }

    /// <summary>
    /// Check if grid coordinates are valid
    /// </summary>
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    /// <summary>
    /// Get all cells on player side (columns 6-8, RIGHT side)
    /// </summary>
    public List<GridCell> GetPlayerSideCells()
    {
        List<GridCell> cells = new List<GridCell>();

        int playerStartColumn = gridWidth - playerColumns; // Column 6

        for (int x = playerStartColumn; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                cells.Add(grid[x, y]);
            }
        }

        return cells;
    }

    /// <summary>
    /// Get all cells on enemy side (columns 0-5, LEFT side)
    /// </summary>
    public List<GridCell> GetEnemySideCells()
    {
        List<GridCell> cells = new List<GridCell>();

        int playerStartColumn = gridWidth - playerColumns;

        for (int x = 0; x < playerStartColumn; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                cells.Add(grid[x, y]);
            }
        }

        return cells;
    }

    /// <summary>
    /// Get all empty cells on player side
    /// </summary>
    public List<GridCell> GetEmptyPlayerCells()
    {
        List<GridCell> emptyCells = new List<GridCell>();
        List<GridCell> playerCells = GetPlayerSideCells();

        foreach (GridCell cell in playerCells)
        {
            if (cell.IsEmpty())
            {
                emptyCells.Add(cell);
            }
        }

        return emptyCells;
    }

    /// <summary>
    /// Get all cells within range of a position
    /// </summary>
    public List<GridCell> GetCellsInRange(Vector2Int center, int range)
    {
        List<GridCell> cellsInRange = new List<GridCell>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int distance = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);

                if (distance <= range)
                {
                    cellsInRange.Add(grid[x, y]);
                }
            }
        }

        return cellsInRange;
    }

    /// <summary>
    /// Get all units currently on the grid
    /// </summary>
    public List<CombatUnit> GetAllUnits()
    {
        List<CombatUnit> units = new List<CombatUnit>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = grid[x, y];
                if (!cell.IsEmpty())
                {
                    units.Add(cell.occupyingUnit);
                }
            }
        }

        return units;
    }

    /// <summary>
    /// Get all player units on the grid
    /// </summary>
    public List<CombatUnit> GetPlayerUnits()
    {
        List<CombatUnit> units = new List<CombatUnit>();
        List<GridCell> playerCells = GetPlayerSideCells();

        foreach (GridCell cell in playerCells)
        {
            if (!cell.IsEmpty())
            {
                units.Add(cell.occupyingUnit);
            }
        }

        return units;
    }

    /// <summary>
    /// Get all enemy units on the grid
    /// </summary>
    public List<CombatUnit> GetEnemyUnits()
    {
        List<CombatUnit> units = new List<CombatUnit>();
        List<GridCell> enemyCells = GetEnemySideCells();

        foreach (GridCell cell in enemyCells)
        {
            if (!cell.IsEmpty())
            {
                units.Add(cell.occupyingUnit);
            }
        }

        return units;
    }

    /// <summary>
    /// Find nearest enemy to a given position
    /// </summary>
    public CombatUnit GetNearestEnemy(Vector2Int position, bool isPlayerUnit)
    {
        List<CombatUnit> enemies = isPlayerUnit ? GetEnemyUnits() : GetPlayerUnits();

        if (enemies.Count == 0) return null;

        CombatUnit nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (CombatUnit enemy in enemies)
        {
            if (!enemy.IsAlive()) continue;

            float distance = Vector2Int.Distance(position, enemy.gridPosition);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

    /// <summary>
    /// Place unit at specific grid position
    /// </summary>
    public bool PlaceUnitAt(CombatUnit unit, int x, int y)
    {
        GridCell cell = GetCell(x, y);
        if (cell == null) return false;

        // Assign health bar prefab to unit if available
        if (healthBarPrefab != null && unit != null)
        {
            unit.healthBarPrefab = healthBarPrefab;
            unit.CreateHealthBar();
        }

        return cell.PlaceUnit(unit);
    }

    /// <summary>
    /// Place unit at Vector2Int position
    /// </summary>
    public bool PlaceUnitAt(CombatUnit unit, Vector2Int position)
    {
        return PlaceUnitAt(unit, position.x, position.y);
    }

    /// <summary>
    /// Clear all units from the grid (for battle reset)
    /// </summary>
    public void ClearAllUnits()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y].ClearUnit();
            }
        }

    }

    /// <summary>
    /// Get grid cell under mouse cursor (for future click interactions)
    /// </summary>
    public GridCell GetCellUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            // Check if hit object is a grid cell
            GridCell cell = hit.collider.GetComponentInParent<GridCell>();
            return cell;
        }

        return null;
    }

    /// <summary>
    /// Get the health bar prefab assigned to this GridManager
    /// </summary>
    public GameObject GetHealthBarPrefab()
    {
        return healthBarPrefab;
    }

    /// <summary>
    /// Set the health bar prefab (useful for runtime configuration)
    /// </summary>
    public void SetHealthBarPrefab(GameObject prefab)
    {
        healthBarPrefab = prefab;
    }

    private void OnDrawGizmos()
    {
        // Draw grid bounds in editor
        if (grid != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(GridBounds.center, GridBounds.size);
        }
    }
}

} // namespace SowurShield.Combat
