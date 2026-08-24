using System.Collections.Generic;
using UnityEngine;
using SowurShield.Core;

namespace SowurShield.Minimap
{

/// <summary>
/// Lets the player drop their own markers on the fullscreen map, and remove them again.
///
/// Every marker on the minimap so far is placed by the game. Valheim's pins are the feature that
/// turns a map from something you read into something you keep notes on — "the good foraging
/// spot", "come back here with an axe" — and it costs almost nothing because the map already
/// knows how to draw markers.
///
/// Right-click on the fullscreen map places a pin; right-clicking an existing pin removes it.
/// Placement is fullscreen-only on purpose: the corner HUD is 200px across, where a click lands
/// several world units from where the player meant.
///
/// Pins are world positions, saved per slot. They use the existing <see cref="MinimapIcon"/>
/// pipeline so they sort, cluster and scale exactly like built-in markers.
/// </summary>
[DefaultExecutionOrder(-40)]
public class MinimapPinManager : MonoBehaviour, ISaveable
{
    [Header("Placement")]
    [Tooltip("Right-click the fullscreen map to place a pin, right-click a pin to remove it.")]
    [SerializeField] private bool allowPlacement = true;

    [Tooltip("Click within this many world units of a pin to remove it instead of placing one.")]
    [SerializeField] private float removeRadius = 1.2f;

    [Tooltip("Upper bound so a stuck input cannot fill the save with pins.")]
    [SerializeField] private int maxPins = 64;

    [Header("Appearance")]
    [SerializeField] private Color pinColor = new Color(0.98f, 0.85f, 0.30f, 1f);
    [SerializeField] private float pinSize = 0.95f;

    [Header("References")]
    [SerializeField] private MinimapController controller;
    [SerializeField] private MinimapUI minimapUI;
    [SerializeField] private MinimapCamera minimapCamera;

    private readonly List<GameObject> pinObjects = new List<GameObject>();
    private readonly List<Vector2> pinPositions = new List<Vector2>();

    private const string SaveKeyPins = "minimap_pins";

    /// <summary>Number of pins currently placed.</summary>
    public int PinCount => pinPositions.Count;

    /// <summary>Raised whenever pins are added or removed, so UI can react.</summary>
    public System.Action OnPinsChanged;

    // ============================================================================
    // LIFECYCLE
    // ============================================================================

    private void Start()
    {
        if (controller == null) controller = GetComponent<MinimapController>();
        if (controller == null) controller = MinimapController.Instance;
        if (minimapUI == null) minimapUI = GetComponent<MinimapUI>();
        if (minimapCamera == null) minimapCamera = FindFirstObjectByType<MinimapCamera>();

        // Registered in Start: SaveManager catches up late registrations, and the pins need the
        // controller resolved before any LoadData can rebuild them.
        SaveManager.Instance?.RegisterSaveable(this);
    }

    private void OnDestroy()
    {
        SaveManager.Instance?.UnregisterSaveable(this);
    }

    private void Update()
    {
        if (!allowPlacement) return;
        if (controller == null || !controller.IsInFullscreenMode) return;

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return;

        if (mouse.rightButton.wasPressedThisFrame)
        {
            if (TryScreenToWorld(mouse.position.ReadValue(), out Vector2 world))
                TogglePinAt(world);
        }
    }

    // ============================================================================
    // COORDINATE MAPPING
    // ============================================================================

    /// <summary>
    /// Turns a screen click into the world position it points at on the map.
    ///
    /// The map is a RenderTexture drawn into a panel, so the screen position has to be resolved
    /// against the panel's rect first and only then through the minimap camera — using
    /// Camera.ScreenToWorldPoint directly would interpret the click against the main game camera
    /// and land somewhere unrelated.
    /// </summary>
    private bool TryScreenToWorld(Vector2 screenPosition, out Vector2 world)
    {
        world = Vector2.zero;

        if (minimapUI == null || minimapCamera == null) return false;

        var panel = minimapUI.GetPanel();
        if (panel == null) return false;

        var canvas = panel.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panel, screenPosition, uiCamera, out Vector2 local))
            return false;

        // Local point -> 0..1 across the panel, accounting for its pivot.
        var rect = panel.rect;
        float u = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
        float v = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);

        if (u < 0f || u > 1f || v < 0f || v > 1f)
            return false; // clicked outside the map

        var cam = minimapCamera.GetCamera();
        if (cam == null) return false;

        Vector3 point = cam.ViewportToWorldPoint(new Vector3(u, v, Mathf.Abs(cam.transform.position.z)));
        world = new Vector2(point.x, point.y);
        return true;
    }

    // ============================================================================
    // PIN MANAGEMENT
    // ============================================================================

    /// <summary>Removes a nearby pin if there is one, otherwise places a new pin.</summary>
    public void TogglePinAt(Vector2 worldPosition)
    {
        int existing = FindPinNear(worldPosition, removeRadius);

        if (existing >= 0)
            RemovePinAt(existing);
        else
            AddPin(worldPosition);
    }

    private int FindPinNear(Vector2 worldPosition, float radius)
    {
        float best = radius * radius;
        int found = -1;

        for (int i = 0; i < pinPositions.Count; i++)
        {
            float d = (pinPositions[i] - worldPosition).sqrMagnitude;
            if (d <= best) { best = d; found = i; }
        }

        return found;
    }

    /// <summary>Places a pin at a world position.</summary>
    public bool AddPin(Vector2 worldPosition)
    {
        if (pinPositions.Count >= maxPins)
            return false;

        var go = new GameObject($"MinimapPin_{pinPositions.Count}");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);

        // Built as a MinimapIcon rather than a bespoke sprite so pins inherit sorting, clustering
        // and the outlined marker shapes for free.
        var icon = go.AddComponent<MinimapIcon>();
        icon.SetIconType(MinimapIconType.Waypoint);
        icon.SetIconColor(pinColor);
        icon.SetIconSize(pinSize);

        pinObjects.Add(go);
        pinPositions.Add(worldPosition);

        OnPinsChanged?.Invoke();
        return true;
    }

    private void RemovePinAt(int index)
    {
        if (index < 0 || index >= pinPositions.Count) return;

        if (pinObjects[index] != null)
            Destroy(pinObjects[index]);

        pinObjects.RemoveAt(index);
        pinPositions.RemoveAt(index);

        OnPinsChanged?.Invoke();
    }

    /// <summary>Removes every pin.</summary>
    [ContextMenu("Clear All Pins")]
    public void ClearAllPins()
    {
        for (int i = pinObjects.Count - 1; i >= 0; i--)
        {
            if (pinObjects[i] == null) continue;

            // DestroyImmediate, not Destroy: LoadData clears and then immediately rebuilds, and
            // deferred Destroy leaves the old pin GameObjects alive until end of frame. They
            // would sit on the map alongside the freshly loaded ones — the clear-then-rebuild
            // trap this project has already hit three times.
            DestroyImmediate(pinObjects[i]);
        }

        pinObjects.Clear();
        pinPositions.Clear();
        OnPinsChanged?.Invoke();
    }

    // ============================================================================
    // SAVE / LOAD
    // ============================================================================

    /// <summary>
    /// Stored as "x,y;x,y;..." with invariant culture.
    ///
    /// The culture matters: this machine formats floats with a comma decimal separator, so
    /// ToString() without InvariantCulture would write "1,15" and the reader would split it into
    /// two coordinates. Saves would silently corrupt on some machines and not others.
    /// </summary>
    public void SaveData(GameData gameData)
    {
        if (pinPositions.Count == 0)
        {
            gameData.worldData.worldStrings.Remove(SaveKeyPins);
            return;
        }

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < pinPositions.Count; i++)
        {
            if (i > 0) sb.Append(';');
            sb.Append(pinPositions[i].x.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(pinPositions[i].y.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }

        gameData.worldData.worldStrings[SaveKeyPins] = sb.ToString();
    }

    public void LoadData(GameData gameData)
    {
        ClearAllPins();

        if (!gameData.worldData.worldStrings.ContainsKey(SaveKeyPins))
            return;

        string raw = gameData.worldData.worldStrings[SaveKeyPins];
        if (string.IsNullOrEmpty(raw)) return;

        foreach (var entry in raw.Split(';'))
        {
            if (string.IsNullOrEmpty(entry)) continue;

            var parts = entry.Split(',');
            if (parts.Length != 2) continue;

            if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out float y))
            {
                AddPin(new Vector2(x, y));
            }
        }
    }
}

} // namespace SowurShield.Minimap
