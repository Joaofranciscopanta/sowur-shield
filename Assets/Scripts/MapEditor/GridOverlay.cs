using UnityEngine;
using System.Collections.Generic;

namespace SowurShield.MapEditor
{

[RequireComponent(typeof(LineRenderer))]
public class GridOverlay : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private bool showGrid = true;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private int gridWidth = 100;
    [SerializeField] private int gridHeight = 100;
    [SerializeField] private Vector2 gridOffset = Vector2.zero;
    
    [Header("Visual Settings")]
    [SerializeField] private Color majorGridColor = new Color(1, 1, 1, 0.3f);
    [SerializeField] private Color minorGridColor = new Color(1, 1, 1, 0.1f);
    [SerializeField] private int majorGridInterval = 10;
    [SerializeField] private float majorLineWidth = 0.05f;
    [SerializeField] private float minorLineWidth = 0.02f;
    
    [Header("Dynamic Grid")]
    [SerializeField] private bool dynamicGrid = true;
    [SerializeField] private float minZoomToShowGrid = 0.5f;
    [SerializeField] private float maxZoomToShowGrid = 10f;
    
    [Header("Performance")]
    [SerializeField] private bool useObjectPooling = true;
    [SerializeField] private int maxGridLines = 1000;
    [SerializeField] private bool adaptiveDetail = true;
    
    // Runtime components
    private Camera targetCamera;
    private RuntimeMapEditor mapEditor;
    
    // Grid objects
    private GameObject gridParent;
    private List<LineRenderer> gridLines = new List<LineRenderer>();
    private List<LineRenderer> majorGridLines = new List<LineRenderer>();
    
    // Object pooling
    private Queue<LineRenderer> lineRendererPool = new Queue<LineRenderer>();
    
    // Dynamic updates
    private Vector3 lastCameraPosition;
    private float lastCameraSize;
    private bool needsGridUpdate = true;
    
    public bool IsGridVisible => showGrid && gridParent != null && gridParent.activeInHierarchy;
    
    // Events
    public System.Action<bool> OnGridToggled;
    public System.Action OnGridUpdated;
    
    private void Start()
    {
        InitializeGrid();
    }
    
    private void Update()
    {
        if (showGrid && mapEditor?.IsEditorActive == true)
        {
            UpdateDynamicGrid();
        }
    }
    
    private void InitializeGrid()
    {
        targetCamera = Camera.main;
        mapEditor = RuntimeMapEditor.Instance;
        
        // Create grid parent
        gridParent = new GameObject("Grid Overlay");
        gridParent.transform.SetParent(transform);
        
        // Subscribe to editor events
        if (mapEditor != null)
        {
            mapEditor.OnEditorToggled += OnEditorToggled;
        }
        
        // Initial grid generation
        GenerateGrid();
        
        // Set initial visibility
        SetGridVisibility(showGrid && mapEditor?.IsEditorActive == true);
        

    }
    
    private void OnEditorToggled(bool editorActive)
    {
        SetGridVisibility(showGrid && editorActive);
    }
    
    private void UpdateDynamicGrid()
    {
        if (!dynamicGrid || targetCamera == null) return;
        
        bool shouldUpdate = false;
        
        // Check if camera moved significantly
        if (Vector3.Distance(targetCamera.transform.position, lastCameraPosition) > gridSize * 0.5f)
        {
            shouldUpdate = true;
            lastCameraPosition = targetCamera.transform.position;
        }
        
        // Check if camera zoom changed
        float currentCameraSize = targetCamera.orthographicSize;
        if (Mathf.Abs(currentCameraSize - lastCameraSize) > 0.1f)
        {
            shouldUpdate = true;
            lastCameraSize = currentCameraSize;
        }
        
        // Update grid if needed
        if (shouldUpdate || needsGridUpdate)
        {
            UpdateGridForCamera();
            needsGridUpdate = false;
        }
        
        // Update grid visibility based on zoom level
        UpdateGridVisibilityByZoom();
    }
    
    private void UpdateGridVisibilityByZoom()
    {
        if (targetCamera == null) return;
        
        float cameraSize = targetCamera.orthographicSize;
        bool shouldShowGrid = cameraSize >= minZoomToShowGrid && cameraSize <= maxZoomToShowGrid;
        
        if (gridParent.activeInHierarchy != shouldShowGrid)
        {
            gridParent.SetActive(shouldShowGrid);
        }
        
        // Adjust grid opacity based on zoom
        if (shouldShowGrid)
        {
            float zoomFactor = Mathf.InverseLerp(maxZoomToShowGrid, minZoomToShowGrid, cameraSize);
            UpdateGridOpacity(zoomFactor);
        }
    }
    
    private void UpdateGridOpacity(float factor)
    {
        Color majorColor = majorGridColor;
        Color minorColor = minorGridColor;
        
        majorColor.a = majorGridColor.a * factor;
        minorColor.a = minorGridColor.a * factor;
        
        // Update major grid lines
        foreach (LineRenderer line in majorGridLines)
        {
            if (line != null)
            {
                line.material.color = majorColor;
            }
        }
        
        // Update minor grid lines
        foreach (LineRenderer line in gridLines)
        {
            if (line != null && !majorGridLines.Contains(line))
            {
                line.material.color = minorColor;
            }
        }
    }
    
    private void UpdateGridForCamera()
    {
        if (targetCamera == null) return;
        
        // Calculate visible area
        float cameraHeight = targetCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * targetCamera.aspect;
        
        Vector3 cameraPos = targetCamera.transform.position;
        
        // Calculate grid bounds based on camera view
        int minX = Mathf.FloorToInt((cameraPos.x - cameraWidth / 2 - gridOffset.x) / gridSize) - 2;
        int maxX = Mathf.CeilToInt((cameraPos.x + cameraWidth / 2 - gridOffset.x) / gridSize) + 2;
        int minY = Mathf.FloorToInt((cameraPos.y - cameraHeight / 2 - gridOffset.y) / gridSize) - 2;
        int maxY = Mathf.CeilToInt((cameraPos.y + cameraHeight / 2 - gridOffset.y) / gridSize) + 2;
        
        // Regenerate grid with new bounds
        GenerateDynamicGrid(minX, maxX, minY, maxY);
    }
    
    private void GenerateGrid()
    {
        ClearGrid();
        
        if (!dynamicGrid)
        {
            // Generate static grid
            GenerateStaticGrid();
        }
        else
        {
            // Dynamic grid will be generated in UpdateGridForCamera
            UpdateGridForCamera();
        }
    }
    
    private void GenerateStaticGrid()
    {
        int halfWidth = gridWidth / 2;
        int halfHeight = gridHeight / 2;
        
        // Generate vertical lines
        for (int x = -halfWidth; x <= halfWidth; x++)
        {
            bool isMajorLine = (x % majorGridInterval == 0);
            CreateGridLine(
                new Vector3(x * gridSize + gridOffset.x, -halfHeight * gridSize + gridOffset.y, 0),
                new Vector3(x * gridSize + gridOffset.x, halfHeight * gridSize + gridOffset.y, 0),
                isMajorLine
            );
        }
        
        // Generate horizontal lines
        for (int y = -halfHeight; y <= halfHeight; y++)
        {
            bool isMajorLine = (y % majorGridInterval == 0);
            CreateGridLine(
                new Vector3(-halfWidth * gridSize + gridOffset.x, y * gridSize + gridOffset.y, 0),
                new Vector3(halfWidth * gridSize + gridOffset.x, y * gridSize + gridOffset.y, 0),
                isMajorLine
            );
        }
    }
    
    private void GenerateDynamicGrid(int minX, int maxX, int minY, int maxY)
    {
        if (!adaptiveDetail)
        {
            ClearGrid();
        }
        else
        {
            // Reuse existing lines where possible
            ReturnLinesToPool();
        }
        
        int lineCount = 0;
        
        // Generate vertical lines
        for (int x = minX; x <= maxX && lineCount < maxGridLines; x++)
        {
            bool isMajorLine = (x % majorGridInterval == 0);
            CreateGridLine(
                new Vector3(x * gridSize + gridOffset.x, minY * gridSize + gridOffset.y, 0),
                new Vector3(x * gridSize + gridOffset.x, maxY * gridSize + gridOffset.y, 0),
                isMajorLine
            );
            lineCount++;
        }
        
        // Generate horizontal lines
        for (int y = minY; y <= maxY && lineCount < maxGridLines; y++)
        {
            bool isMajorLine = (y % majorGridInterval == 0);
            CreateGridLine(
                new Vector3(minX * gridSize + gridOffset.x, y * gridSize + gridOffset.y, 0),
                new Vector3(maxX * gridSize + gridOffset.x, y * gridSize + gridOffset.y, 0),
                isMajorLine
            );
            lineCount++;
        }
        
        OnGridUpdated?.Invoke();
    }
    
    private void CreateGridLine(Vector3 start, Vector3 end, bool isMajorLine)
    {
        LineRenderer line = GetLineRenderer();
        
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        
        if (isMajorLine)
        {
            line.material.color = majorGridColor;
            line.startWidth = majorLineWidth;
            line.endWidth = majorLineWidth;
            line.sortingOrder = 1;
            majorGridLines.Add(line);
        }
        else
        {
            line.material.color = minorGridColor;
            line.startWidth = minorLineWidth;
            line.endWidth = minorLineWidth;
            line.sortingOrder = 0;
        }
        
        line.material = GetGridMaterial();
        line.useWorldSpace = true;
        
        gridLines.Add(line);
    }
    
    private LineRenderer GetLineRenderer()
    {
        LineRenderer line;
        
        if (useObjectPooling && lineRendererPool.Count > 0)
        {
            line = lineRendererPool.Dequeue();
            line.gameObject.SetActive(true);
        }
        else
        {
            GameObject lineObj = new GameObject("GridLine");
            lineObj.transform.SetParent(gridParent.transform);
            line = lineObj.AddComponent<LineRenderer>();
        }
        
        return line;
    }
    
    private void ReturnLinesToPool()
    {
        if (!useObjectPooling) return;
        
        foreach (LineRenderer line in gridLines)
        {
            if (line != null)
            {
                line.gameObject.SetActive(false);
                lineRendererPool.Enqueue(line);
            }
        }
        
        gridLines.Clear();
        majorGridLines.Clear();
    }
    
    private Material GetGridMaterial()
    {
        // Create or get a simple unlit material for grid lines
        Material gridMaterial = new Material(Shader.Find("Sprites/Default"));
        gridMaterial.color = Color.white;
        return gridMaterial;
    }
    
    private void ClearGrid()
    {
        if (useObjectPooling)
        {
            ReturnLinesToPool();
        }
        else
        {
            // Destroy all grid lines
            foreach (LineRenderer line in gridLines)
            {
                if (line != null)
                {
                    DestroyImmediate(line.gameObject);
                }
            }
            
            gridLines.Clear();
            majorGridLines.Clear();
        }
    }
    
    // Public methods
    public void SetGridVisibility(bool visible)
    {
        showGrid = visible;
        
        if (gridParent != null)
        {
            gridParent.SetActive(visible);
        }
        
        OnGridToggled?.Invoke(visible);
    }
    
    public void ToggleGrid()
    {
        SetGridVisibility(!showGrid);
    }
    
    public void SetGridSize(float size)
    {
        gridSize = Mathf.Max(0.1f, size);
        needsGridUpdate = true;
    }
    
    public void SetGridDimensions(int width, int height)
    {
        gridWidth = Mathf.Max(10, width);
        gridHeight = Mathf.Max(10, height);
        
        if (!dynamicGrid)
        {
            GenerateGrid();
        }
    }
    
    public void SetGridOffset(Vector2 offset)
    {
        gridOffset = offset;
        needsGridUpdate = true;
    }
    
    public void SetMajorGridInterval(int interval)
    {
        majorGridInterval = Mathf.Max(1, interval);
        needsGridUpdate = true;
    }
    
    public void SetGridColors(Color majorColor, Color minorColor)
    {
        majorGridColor = majorColor;
        minorGridColor = minorColor;
        
        // Update existing lines
        foreach (LineRenderer line in majorGridLines)
        {
            if (line != null)
            {
                line.material.color = majorGridColor;
            }
        }
        
        foreach (LineRenderer line in gridLines)
        {
            if (line != null && !majorGridLines.Contains(line))
            {
                line.material.color = minorGridColor;
            }
        }
    }
    
    public void RefreshGrid()
    {
        needsGridUpdate = true;
    }
    
    // Snap utilities
    public Vector3 SnapToGrid(Vector3 worldPosition)
    {
        float snappedX = Mathf.Round((worldPosition.x - gridOffset.x) / gridSize) * gridSize + gridOffset.x;
        float snappedY = Mathf.Round((worldPosition.y - gridOffset.y) / gridSize) * gridSize + gridOffset.y;
        
        return new Vector3(snappedX, snappedY, worldPosition.z);
    }
    
    public Vector3Int GetGridCoordinates(Vector3 worldPosition)
    {
        int gridX = Mathf.RoundToInt((worldPosition.x - gridOffset.x) / gridSize);
        int gridY = Mathf.RoundToInt((worldPosition.y - gridOffset.y) / gridSize);
        
        return new Vector3Int(gridX, gridY, 0);
    }
    
    public Vector3 GridToWorldPosition(Vector3Int gridCoordinates)
    {
        float worldX = gridCoordinates.x * gridSize + gridOffset.x;
        float worldY = gridCoordinates.y * gridSize + gridOffset.y;
        
        return new Vector3(worldX, worldY, 0);
    }
    
    // Debug information
    public int GetActiveLineCount()
    {
        return gridLines.Count;
    }
    
    public int GetPooledLineCount()
    {
        return lineRendererPool.Count;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (mapEditor != null)
        {
            mapEditor.OnEditorToggled -= OnEditorToggled;
        }
        
        ClearGrid();
    }
    
    // Editor gizmos
    private void OnDrawGizmos()
    {
        if (!showGrid || Application.isPlaying) return;

        // Draw a simplified version in editor
        Gizmos.color = majorGridColor;

        // Draw a few sample grid lines
        for (int x = -5; x <= 5; x++)
        {
            if (x % majorGridInterval == 0)
            {
                Vector3 start = new Vector3(x * gridSize + gridOffset.x, -5 * gridSize + gridOffset.y, 0);
                Vector3 end = new Vector3(x * gridSize + gridOffset.x, 5 * gridSize + gridOffset.y, 0);
                Gizmos.DrawLine(start, end);
            }
        }

        for (int y = -5; y <= 5; y++)
        {
            if (y % majorGridInterval == 0)
            {
                Vector3 start = new Vector3(-5 * gridSize + gridOffset.x, y * gridSize + gridOffset.y, 0);
                Vector3 end = new Vector3(5 * gridSize + gridOffset.x, y * gridSize + gridOffset.y, 0);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}
} // namespace SowurShield.MapEditor