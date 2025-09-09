using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BrushTool : MonoBehaviour
{
    [Header("Brush Settings")]
    [SerializeField] private BrushType currentBrushType = BrushType.Paint;
    [SerializeField] private int brushSize = 1;
    [SerializeField] private float brushSpacing = 0.1f;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject brushPreviewPrefab;
    [SerializeField] private LineRenderer linePreview;
    [SerializeField] private GameObject rectanglePreview;
    
    [Header("Audio")]
    [SerializeField] private AudioClip paintSound;
    [SerializeField] private AudioClip eraseSound;
    [SerializeField] private AudioSource audioSource;
    
    // Runtime references
    private RuntimeMapEditor mapEditor;
    private ExtendedDualGridTilemap extendedTilemap;
    private Camera mainCamera;
    
    // Brush state
    private bool isMouseDown = false;
    private Vector3Int lastPaintPosition;
    private Vector3Int lineStartPosition;
    private Vector3Int rectangleStartPosition;
    private bool isDrawingLine = false;
    private bool isDrawingRectangle = false;
    
    // Visual feedback
    private GameObject brushPreviewInstance;
    private List<Vector3Int> previewTiles = new List<Vector3Int>();
    
    // Performance
    private float lastPaintTime = 0f;
    private const float PAINT_COOLDOWN = 0.05f;
    
    public BrushType CurrentBrushType => currentBrushType;
    public int BrushSize => brushSize;
    
    // Events
    public System.Action<Vector3Int, ExtendedTileType> OnTilePainted;
    public System.Action<Vector3Int> OnTileErased;
    
    private void Start()
    {
        InitializeBrushTool();
    }
    
    private void Update()
    {
        if (mapEditor?.IsEditorActive == true)
        {
            HandleInput();
            UpdateVisualFeedback();
        }
    }
    
    private void InitializeBrushTool()
    {
        mapEditor = RuntimeMapEditor.Instance;
        extendedTilemap = FindFirstObjectByType<ExtendedDualGridTilemap>();
        mainCamera = Camera.main;
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.3f;
        }
        
        CreateBrushPreview();
        
        Debug.Log("BrushTool initialized");
    }
    
    private void CreateBrushPreview()
    {
        if (brushPreviewPrefab != null)
        {
            brushPreviewInstance = Instantiate(brushPreviewPrefab);
            brushPreviewInstance.name = "Brush Preview";
            brushPreviewInstance.SetActive(false);
        }
    }
    
    private void HandleInput()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int currentTilePos = WorldToTilePosition(mouseWorldPos);
        
        // Handle different brush types
        switch (currentBrushType)
        {
            case BrushType.Paint:
                HandlePaintBrush(currentTilePos);
                break;
                
            case BrushType.Fill:
                HandleFillBrush(currentTilePos);
                break;
                
            case BrushType.Line:
                HandleLineBrush(currentTilePos);
                break;
                
            case BrushType.Rectangle:
                HandleRectangleBrush(currentTilePos);
                break;
                
            case BrushType.Circle:
                HandleCircleBrush(currentTilePos);
                break;
                
            case BrushType.Eraser:
                HandleEraserBrush(currentTilePos);
                break;
        }
    }
    
    private void HandlePaintBrush(Vector3Int tilePos)
    {
        if (Input.GetMouseButtonDown(0))
        {
            isMouseDown = true;
            PaintTiles(GetBrushArea(tilePos), mapEditor.selectedTileType);
            lastPaintPosition = tilePos;
        }
        else if (Input.GetMouseButton(0) && isMouseDown)
        {
            // Continuous painting with spacing control
            if (Vector3Int.Distance(lastPaintPosition, tilePos) >= brushSpacing || 
                Time.time - lastPaintTime >= PAINT_COOLDOWN)
            {
                PaintTiles(GetBrushArea(tilePos), mapEditor.selectedTileType);
                lastPaintPosition = tilePos;
                lastPaintTime = Time.time;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMouseDown = false;
        }
    }
    
    private void HandleFillBrush(Vector3Int tilePos)
    {
        if (Input.GetMouseButtonDown(0))
        {
            ExtendedTileType targetTileType = extendedTilemap.GetTileTypeAt(tilePos);
            if (targetTileType != mapEditor.selectedTileType)
            {
                StartCoroutine(FloodFill(tilePos, targetTileType, mapEditor.selectedTileType));
            }
        }
    }
    
    private void HandleLineBrush(Vector3Int tilePos)
    {
        if (Input.GetMouseButtonDown(0))
        {
            lineStartPosition = tilePos;
            isDrawingLine = true;
        }
        else if (Input.GetMouseButtonUp(0) && isDrawingLine)
        {
            DrawLine(lineStartPosition, tilePos, mapEditor.selectedTileType);
            isDrawingLine = false;
        }
    }
    
    private void HandleRectangleBrush(Vector3Int tilePos)
    {
        if (Input.GetMouseButtonDown(0))
        {
            rectangleStartPosition = tilePos;
            isDrawingRectangle = true;
        }
        else if (Input.GetMouseButtonUp(0) && isDrawingRectangle)
        {
            DrawRectangle(rectangleStartPosition, tilePos, mapEditor.selectedTileType);
            isDrawingRectangle = false;
        }
    }
    
    private void HandleCircleBrush(Vector3Int tilePos)
    {
        if (Input.GetMouseButtonDown(0))
        {
            DrawCircle(tilePos, brushSize, mapEditor.selectedTileType);
        }
    }
    
    private void HandleEraserBrush(Vector3Int tilePos)
    {
        if (Input.GetMouseButtonDown(0))
        {
            isMouseDown = true;
            EraseTiles(GetBrushArea(tilePos));
            lastPaintPosition = tilePos;
        }
        else if (Input.GetMouseButton(0) && isMouseDown)
        {
            if (Vector3Int.Distance(lastPaintPosition, tilePos) >= brushSpacing)
            {
                EraseTiles(GetBrushArea(tilePos));
                lastPaintPosition = tilePos;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMouseDown = false;
        }
    }
    
    private List<Vector3Int> GetBrushArea(Vector3Int centerPos)
    {
        List<Vector3Int> area = new List<Vector3Int>();
        
        int radius = brushSize / 2;
        
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int pos = centerPos + new Vector3Int(x, y, 0);
                
                // For circular brush, check distance
                if (brushSize > 1)
                {
                    float distance = Vector2.Distance(Vector2.zero, new Vector2(x, y));
                    if (distance <= radius)
                    {
                        area.Add(pos);
                    }
                }
                else
                {
                    area.Add(pos);
                }
            }
        }
        
        return area;
    }
    
    private void PaintTiles(List<Vector3Int> positions, ExtendedTileType tileType)
    {
        if (extendedTilemap == null) return;
        
        foreach (Vector3Int pos in positions)
        {
            extendedTilemap.SetCell(pos, tileType, mapEditor.selectedLayer);
            OnTilePainted?.Invoke(pos, tileType);
        }
        
        PlaySound(paintSound);
    }
    
    private void EraseTiles(List<Vector3Int> positions)
    {
        if (extendedTilemap == null) return;
        
        foreach (Vector3Int pos in positions)
        {
            extendedTilemap.SetCell(pos, ExtendedTileType.None, mapEditor.selectedLayer);
            OnTileErased?.Invoke(pos);
        }
        
        PlaySound(eraseSound);
    }
    
    private IEnumerator FloodFill(Vector3Int startPos, ExtendedTileType targetType, ExtendedTileType replaceType)
    {
        if (targetType == replaceType) yield break;
        
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        
        queue.Enqueue(startPos);
        int processedThisFrame = 0;
        const int MAX_PER_FRAME = 100; // Limit to prevent frame drops
        
        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            
            if (visited.Contains(current)) continue;
            visited.Add(current);
            
            ExtendedTileType currentType = extendedTilemap.GetTileTypeAt(current);
            if (currentType != targetType) continue;
            
            // Paint this tile
            extendedTilemap.SetCell(current, replaceType, mapEditor.selectedLayer);
            OnTilePainted?.Invoke(current, replaceType);
            
            // Add neighbors to queue
            Vector3Int[] neighbors = {
                current + Vector3Int.up,
                current + Vector3Int.down,
                current + Vector3Int.left,
                current + Vector3Int.right
            };
            
            foreach (Vector3Int neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
            
            processedThisFrame++;
            if (processedThisFrame >= MAX_PER_FRAME)
            {
                processedThisFrame = 0;
                yield return null; // Wait for next frame
            }
        }
        
        PlaySound(paintSound);
        Debug.Log($"Flood fill completed. Filled {visited.Count} tiles.");
    }
    
    private void DrawLine(Vector3Int start, Vector3Int end, ExtendedTileType tileType)
    {
        List<Vector3Int> linePoints = GetLinePoints(start, end);
        PaintTiles(linePoints, tileType);
    }
    
    private List<Vector3Int> GetLinePoints(Vector3Int start, Vector3Int end)
    {
        List<Vector3Int> points = new List<Vector3Int>();
        
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);
        
        int x = start.x;
        int y = start.y;
        
        int x_inc = (end.x > start.x) ? 1 : -1;
        int y_inc = (end.y > start.y) ? 1 : -1;
        
        int error = dx - dy;
        
        for (int i = 0; i < dx + dy; i++)
        {
            points.Add(new Vector3Int(x, y, 0));
            
            if (x == end.x && y == end.y) break;
            
            int error2 = 2 * error;
            
            if (error2 > -dy)
            {
                error -= dy;
                x += x_inc;
            }
            
            if (error2 < dx)
            {
                error += dx;
                y += y_inc;
            }
        }
        
        return points;
    }
    
    private void DrawRectangle(Vector3Int start, Vector3Int end, ExtendedTileType tileType)
    {
        List<Vector3Int> rectanglePoints = new List<Vector3Int>();
        
        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);
        
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                rectanglePoints.Add(new Vector3Int(x, y, 0));
            }
        }
        
        PaintTiles(rectanglePoints, tileType);
    }
    
    private void DrawCircle(Vector3Int center, int radius, ExtendedTileType tileType)
    {
        List<Vector3Int> circlePoints = new List<Vector3Int>();
        
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance <= radius)
                {
                    circlePoints.Add(center + new Vector3Int(x, y, 0));
                }
            }
        }
        
        PaintTiles(circlePoints, tileType);
    }
    
    private void UpdateVisualFeedback()
    {
        if (mapEditor?.IsEditorActive != true) return;
        
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int currentTilePos = WorldToTilePosition(mouseWorldPos);
        
        // Update brush preview position
        if (brushPreviewInstance != null)
        {
            brushPreviewInstance.transform.position = new Vector3(currentTilePos.x + 0.5f, currentTilePos.y + 0.5f, -1);
            brushPreviewInstance.SetActive(true);
            
            // Scale preview based on brush size
            float scale = brushSize * 1.0f;
            brushPreviewInstance.transform.localScale = Vector3.one * scale;
        }
        
        // Update line preview
        if (isDrawingLine && linePreview != null)
        {
            UpdateLinePreview(lineStartPosition, currentTilePos);
        }
        
        // Update rectangle preview
        if (isDrawingRectangle && rectanglePreview != null)
        {
            UpdateRectanglePreview(rectangleStartPosition, currentTilePos);
        }
    }
    
    private void UpdateLinePreview(Vector3Int start, Vector3Int end)
    {
        if (linePreview == null) return;
        
        linePreview.gameObject.SetActive(true);
        linePreview.positionCount = 2;
        linePreview.SetPosition(0, new Vector3(start.x + 0.5f, start.y + 0.5f, -1));
        linePreview.SetPosition(1, new Vector3(end.x + 0.5f, end.y + 0.5f, -1));
    }
    
    private void UpdateRectanglePreview(Vector3Int start, Vector3Int end)
    {
        if (rectanglePreview == null) return;
        
        Vector3 center = new Vector3(
            (start.x + end.x) * 0.5f + 0.5f,
            (start.y + end.y) * 0.5f + 0.5f,
            -1
        );
        
        Vector3 size = new Vector3(
            Mathf.Abs(end.x - start.x) + 1,
            Mathf.Abs(end.y - start.y) + 1,
            1
        );
        
        rectanglePreview.transform.position = center;
        rectanglePreview.transform.localScale = size;
        rectanglePreview.SetActive(true);
    }
    
    private Vector3Int WorldToTilePosition(Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(worldPos.x),
            Mathf.FloorToInt(worldPos.y),
            0
        );
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Public methods for external control
    public void SetBrushType(BrushType brushType)
    {
        currentBrushType = brushType;
        
        // Hide previews when switching tools
        HideAllPreviews();
        
        Debug.Log($"Brush type changed to: {brushType}");
    }
    
    public void SetBrushSize(int size)
    {
        brushSize = Mathf.Clamp(size, 1, 10);
        Debug.Log($"Brush size changed to: {brushSize}");
    }
    
    private void HideAllPreviews()
    {
        if (brushPreviewInstance != null)
            brushPreviewInstance.SetActive(false);
        
        if (linePreview != null)
            linePreview.gameObject.SetActive(false);
        
        if (rectanglePreview != null)
            rectanglePreview.SetActive(false);
    }
    
    private void OnDisable()
    {
        HideAllPreviews();
    }
    
    private void OnDestroy()
    {
        if (brushPreviewInstance != null)
        {
            Destroy(brushPreviewInstance);
        }
    }
}