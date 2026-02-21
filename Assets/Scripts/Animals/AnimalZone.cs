using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a zone where animals can wander and move around.
/// Attach this to a GameObject with a Collider2D to define the boundaries.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AnimalZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [Tooltip("Visual color for the zone in editor")]
    public Color gizmoColor = new Color(0.3f, 0.8f, 0.3f, 0.3f);

    [Tooltip("Show zone boundaries in editor")]
    public bool showGizmos = true;

    private Collider2D zoneCollider;
    private List<Animal> animalsInZone = new List<Animal>();

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();

        if (zoneCollider == null)
        {
        }
        else
        {
            zoneCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// Get a random point within the zone boundaries
    /// </summary>
    public Vector2 GetRandomPointInZone()
    {
        if (zoneCollider == null) return transform.position;

        Bounds bounds = zoneCollider.bounds;
        Vector2 randomPoint;
        int maxAttempts = 30;
        int attempts = 0;

        // Try to find a point inside the collider
        do
        {
            randomPoint = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );
            attempts++;
        }
        while (!IsPointInZone(randomPoint) && attempts < maxAttempts);

        // Fallback to center if no valid point found
        if (attempts >= maxAttempts)
        {
            return bounds.center;
        }

        return randomPoint;
    }

    /// <summary>
    /// Get a random point within a specific radius from a position, staying within zone
    /// </summary>
    public Vector2 GetRandomPointNear(Vector2 position, float radius)
    {
        Vector2 randomOffset = Random.insideUnitCircle * radius;
        Vector2 targetPoint = position + randomOffset;

        // Clamp to zone boundaries
        if (!IsPointInZone(targetPoint))
        {
            // If outside zone, try to find a point between current position and target
            Vector2 direction = (targetPoint - position).normalized;
            float distance = Vector2.Distance(position, targetPoint);

            for (float d = distance; d > 0.5f; d -= 0.5f)
            {
                Vector2 testPoint = position + direction * d;
                if (IsPointInZone(testPoint))
                {
                    return testPoint;
                }
            }

            // Fallback to current position if nothing works
            return position;
        }

        return targetPoint;
    }

    /// <summary>
    /// Check if a point is within the zone
    /// </summary>
    public bool IsPointInZone(Vector2 point)
    {
        if (zoneCollider == null) return false;
        return zoneCollider.OverlapPoint(point);
    }

    /// <summary>
    /// Register an animal to this zone
    /// </summary>
    public void RegisterAnimal(Animal animal)
    {
        if (!animalsInZone.Contains(animal))
        {
            animalsInZone.Add(animal);
        }
    }

    /// <summary>
    /// Unregister an animal from this zone
    /// </summary>
    public void UnregisterAnimal(Animal animal)
    {
        animalsInZone.Remove(animal);
    }

    /// <summary>
    /// Get all animals currently in this zone
    /// </summary>
    public List<Animal> GetAnimals()
    {
        return new List<Animal>(animalsInZone);
    }

    /// <summary>
    /// Get the center point of the zone
    /// </summary>
    public Vector2 GetZoneCenter()
    {
        if (zoneCollider == null) return transform.position;
        return zoneCollider.bounds.center;
    }

    /// <summary>
    /// Get the bounds of the zone
    /// </summary>
    public Bounds GetZoneBounds()
    {
        if (zoneCollider == null) return new Bounds(transform.position, Vector3.one);
        return zoneCollider.bounds;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = gizmoColor;

        // Draw zone boundaries
        if (col is BoxCollider2D boxCol)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCol.offset, boxCol.size);
        }
        else if (col is PolygonCollider2D polyCol)
        {
            // Draw polygon outline
            Vector2[] points = polyCol.points;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 start = transform.TransformPoint(points[i]);
                Vector2 end = transform.TransformPoint(points[(i + 1) % points.Length]);
                Gizmos.DrawLine(start, end);
            }
        }
        else if (col is CircleCollider2D circleCol)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(circleCol.offset, circleCol.radius);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Draw more opaque when selected
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.6f);

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        if (col is BoxCollider2D boxCol)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCol.offset, boxCol.size);
        }
        else if (col is CircleCollider2D circleCol)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(circleCol.offset, circleCol.radius);
        }
    }
}
