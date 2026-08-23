using System.Collections.Generic;
using UnityEngine;

namespace SowurShield.Minimap
{

/// <summary>
/// Merges markers that are too close together to tell apart.
///
/// SampleScene has 20 animal markers inside a small pen. Measured against the real HUD — a 200px
/// panel showing a 32-unit span, so 6.3px per world unit — the two nearest sit 4.5px apart while
/// each marker is 2.1px wide. They overlap. The right-hand third of the minimap was a white
/// smear, and the count (the one fact those markers carry) was unreadable.
///
/// So markers of the same type that fall within a screen-space radius collapse into one marker
/// scaled up slightly, and the group's size is shown by that scale rather than by drawing every
/// member. This is what Don't Starve and RimWorld do; the alternative — shrinking icons until
/// they stop touching — just makes them invisible instead.
///
/// Clustering is recomputed on a timer rather than per frame: it is O(n²) over markers, and the
/// animals wander slowly enough that four updates a second is imperceptible.
/// </summary>
[DefaultExecutionOrder(-40)]
public class MinimapIconClusterer : MonoBehaviour
{
    [Header("Clustering")]
    // Judged from a render, not from theory. At one marker width (7px) the 20 animals collapsed
    // into 13 markers — barely thinner, but each one now scaled up, so the pen read *worse* than
    // before. The threshold has to exceed the flock's internal spacing to actually simplify it;
    // at 18px the same pen resolves to a handful of markers that can be counted at a glance.
    [Tooltip("Markers closer than this many HUD pixels merge. Must exceed typical marker spacing " +
             "to actually thin a crowd — merging only touching pairs makes things worse.")]
    [SerializeField] private float mergeDistancePixels = 18f;

    [Tooltip("Types that never merge — things you always want counted individually.")]
    [SerializeField]
    private List<MinimapIconType> neverCluster = new List<MinimapIconType>
    {
        MinimapIconType.Player,
        MinimapIconType.Quest,
        MinimapIconType.Waypoint,
    };

    [Header("Appearance")]
    // Kept modest on purpose: a cluster that grows too much reclaims the space clustering just
    // freed. 1.45x is enough to read as "several" without dominating the panel.
    [Tooltip("How much a cluster grows over a single marker, at the largest group size.")]
    [SerializeField] private float maxClusterScale = 1.45f;

    [Tooltip("Group size that reaches maxClusterScale.")]
    [SerializeField] private int scaleSaturationCount = 8;

    [Header("Timing")]
    [SerializeField] private float updateInterval = 0.25f;

    [Header("HUD Reference")]
    [Tooltip("Panel width in pixels the merge distance is judged against.")]
    [SerializeField] private float hudPanelPixels = 200f;

    private MinimapCamera minimapCamera;
    private MinimapController controller;
    private float nextUpdateTime;

    // Reused between passes so a 4Hz update does not allocate.
    private readonly List<MinimapIcon> candidates = new List<MinimapIcon>();
    private readonly List<int> clusterOf = new List<int>();
    private readonly List<Vector3> positions = new List<Vector3>();
    private readonly List<MinimapIconType> types = new List<MinimapIconType>();

    private void Start()
    {
        minimapCamera = FindFirstObjectByType<MinimapCamera>();
        controller = MinimapController.Instance;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextUpdateTime)
            return;

        nextUpdateTime = Time.unscaledTime + Mathf.Max(0.05f, updateInterval);

        Recluster();
    }

    /// <summary>
    /// Recomputes groups and applies them to the icons.
    ///
    /// In fullscreen the map shows the same world across a much larger panel, so markers that
    /// collide on the HUD are comfortably separate there. The merge threshold is therefore
    /// evaluated in HUD pixels and scales with the panel, which means fullscreen naturally
    /// declusters — exactly the behaviour you want when the player opens the map to look closely.
    /// </summary>
    private void Recluster()
    {
        if (minimapCamera == null)
        {
            minimapCamera = FindFirstObjectByType<MinimapCamera>();
            if (minimapCamera == null) return;
        }

        CollectCandidates();

        if (candidates.Count == 0)
            return;

        float mergeWorldDistance = MergeDistanceInWorldUnits();
        float mergeSqr = mergeWorldDistance * mergeWorldDistance;

        // Single-link grouping: walk the list, attach each marker to the first existing group of
        // the same type within range, else start a new one. Good enough for tens of markers and
        // far simpler to reason about than a spatial index.
        clusterOf.Clear();
        var groupCentres = new List<Vector3>();
        var groupTypes = new List<MinimapIconType>();
        var groupCounts = new List<int>();

        for (int i = 0; i < candidates.Count; i++)
        {
            int assigned = -1;

            for (int gi = 0; gi < groupCentres.Count; gi++)
            {
                if (groupTypes[gi] != types[i]) continue;
                if ((groupCentres[gi] - positions[i]).sqrMagnitude > mergeSqr) continue;
                assigned = gi;
                break;
            }

            if (assigned < 0)
            {
                groupCentres.Add(positions[i]);
                groupTypes.Add(types[i]);
                groupCounts.Add(1);
                clusterOf.Add(groupCentres.Count - 1);
            }
            else
            {
                int n = groupCounts[assigned] + 1;
                // Running mean keeps the cluster marker centred on its members.
                groupCentres[assigned] = Vector3.Lerp(groupCentres[assigned], positions[i], 1f / n);
                groupCounts[assigned] = n;
                clusterOf.Add(assigned);
            }
        }

        ApplyGrouping(groupCounts);
    }

    private void CollectCandidates()
    {
        candidates.Clear();
        positions.Clear();
        types.Clear();

        foreach (var icon in FindObjectsByType<MinimapIcon>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!icon.isActiveAndEnabled) continue;

            var type = icon.IconType;
            if (neverCluster.Contains(type))
            {
                // Still make sure a previously-clustered icon is restored.
                icon.ApplyClusterState(1);
                continue;
            }

            candidates.Add(icon);
            positions.Add(icon.transform.position);
            types.Add(type);
        }
    }

    /// <summary>
    /// The merge radius is defined in HUD pixels but compared in world units, so it has to be
    /// converted through the camera's current view — which changes with every zoom step.
    /// </summary>
    private float MergeDistanceInWorldUnits()
    {
        float worldSpan = minimapCamera.CurrentOrthographicSize() * 2f;

        float panelPixels = hudPanelPixels;
        if (controller != null && controller.IsInFullscreenMode)
        {
            // Fullscreen shows the same span across a bigger panel; markers separate on their own.
            panelPixels = Mathf.Max(hudPanelPixels, FullscreenPanelPixels());
        }

        if (panelPixels <= 0f) return 0f;

        float worldUnitsPerPixel = worldSpan / panelPixels;
        return mergeDistancePixels * worldUnitsPerPixel;
    }

    private float FullscreenPanelPixels()
    {
        var ui = controller != null ? controller.UI : null;
        if (ui == null) return hudPanelPixels;

        var panel = ui.GetPanel();
        return panel != null ? Mathf.Max(panel.sizeDelta.x, panel.sizeDelta.y) : hudPanelPixels;
    }

    private void ApplyGrouping(List<int> groupCounts)
    {
        // One icon per group stays visible and is scaled to reflect the count; the rest hide.
        var groupRepresented = new bool[groupCounts.Count];

        for (int i = 0; i < candidates.Count; i++)
        {
            int g = clusterOf[i];
            int count = groupCounts[g];

            if (!groupRepresented[g])
            {
                groupRepresented[g] = true;
                candidates[i].ApplyClusterState(count, ScaleForCount(count));
            }
            else
            {
                candidates[i].ApplyClusterState(0); // hidden member
            }
        }
    }

    private float ScaleForCount(int count)
    {
        if (count <= 1) return 1f;

        float t = Mathf.InverseLerp(1f, Mathf.Max(2, scaleSaturationCount), count);
        return Mathf.Lerp(1f, maxClusterScale, Mathf.Sqrt(t));
    }
}

} // namespace SowurShield.Minimap
