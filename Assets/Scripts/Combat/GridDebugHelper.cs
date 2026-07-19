using UnityEngine;

namespace SowurShield.Combat
{

/// <summary>
/// DEBUG HELPER: Attach this to GridManager to verify setup is correct
/// This will help diagnose why the grid isn't appearing
/// </summary>
public class GridDebugHelper : MonoBehaviour
{
    private void Start()
    {

        // Check for GridManager
        GridManager gm = FindFirstObjectByType<GridManager>();
        if (gm == null)
        {
        }
        else
        {

            // Wait for grid generation
            Invoke(nameof(CheckGridAfterStart), 0.5f);
        }

        // Check for camera
        Camera cam = Camera.main;
        if (cam == null)
        {
        }
        else
        {
        }
    }

    private void CheckGridAfterStart()
    {
        GridManager gm = FindFirstObjectByType<GridManager>();

        if (gm == null) return;

        if (gm.grid == null)
        {
        }
        else
        {

            // Count actual GameObjects
            int cellCount = 0;
            foreach (GridCell cell in gm.grid)
            {
                if (cell != null) cellCount++;
            }


            // Check if cells have visuals
            if (cellCount > 0)
            {
                GridCell firstCell = gm.grid[0, 0];
                if (firstCell.cellVisual == null)
                {
                }
                else
                {

                    // Check renderer
                    Renderer r = firstCell.cellVisual.GetComponent<Renderer>();
                    if (r == null)
                    {
                    }
                    else
                    {
                    }
                }
            }
        }

        // List all grid-related objects
        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        int gridObjectCount = 0;
        foreach (Transform t in allTransforms)
        {
            if (t.name.Contains("Grid") || t.name.Contains("Cell"))
            {
                gridObjectCount++;
            }
        }

    }
}

} // namespace SowurShield.Combat
