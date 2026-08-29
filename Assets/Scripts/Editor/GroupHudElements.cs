using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SowurShield.Editor
{

/// <summary>
/// Gathers the always-on HUD readouts under one parent, so the UI canvas stops being a flat
/// list where persistent chrome, pop-up windows and nested canvases sit as equal siblings.
///
/// The visual audit filed this as "consolidate the HUD into 2 groups". The canvas had 19
/// direct children with the three stamina pieces, the two money pieces and the three
/// top-centre pieces interleaved among windows, which makes it easy to move one part of a
/// readout and leave its background behind.
///
/// <para>Only the persistent readouts move. Windows (Inventory, StorageContainer,
/// MinimapPanel, TroughPanel, MenuPanel) keep their place, because they are opened and closed
/// by UIManager and several are looked up by name; and nested canvases (SellingBoxCanvas,
/// DialogueCanvas, WorldMap, SleepUICanvas) are left strictly alone, since re-parenting a
/// canvas changes its sorting context.</para>
///
/// <para>The group is a plain RectTransform stretched over the whole canvas with no layout
/// component, so every child keeps the anchors and positions it already had. This is
/// deliberately a tidying pass, not a re-layout.</para>
///
/// Menu: Sowur Shield > UI > Group HUD Elements
/// </summary>
public static class GroupHudElements
{
    private const string GroupName = "HUD";

    /// <summary>
    /// The always-on readouts, in the order they should appear under the group.
    /// </summary>
    private static readonly string[] HudElements =
    {
        "StaminaBarBG", "StaminaIcon", "StaminaSlider",
        "MoneyPanelBG", "MoneyText",
        "TopCenterPanelBG", "Days", "TimeText",
    };

    [MenuItem("Sowur Shield/UI/Group HUD Elements")]
    public static void Group()
    {
        Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                         FindObjectsSortMode.None)
            .FirstOrDefault(c => c.name == "UI" && c.isRootCanvas);

        if (canvas == null)
        {
            Debug.LogError("[GroupHudElements] No root canvas named 'UI' in the open scene.");
            return;
        }

        Transform group = canvas.transform.Find(GroupName);
        if (group == null)
        {
            var go = new GameObject(GroupName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create HUD group");
            go.transform.SetParent(canvas.transform, false);

            var rect = (RectTransform)go.transform;
            // Stretched over the canvas with zero offsets, so children keep their own anchors
            // and anchoredPositions untouched.
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            group = go.transform;
        }

        // Behind every window, so an opened panel covers the HUD rather than the reverse.
        group.SetSiblingIndex(0);

        int moved = 0, missing = 0;
        foreach (string name in HudElements)
        {
            Transform child = canvas.transform.Find(name);
            if (child == null)
            {
                if (group.Find(name) == null) missing++;
                continue;   // already inside the group
            }

            // worldPositionStays:false keeps the anchored layout rather than trying to
            // preserve a world position the stretched parent would reinterpret.
            Undo.SetTransformParent(child, group, "Group HUD element");
            child.SetSiblingIndex(group.childCount - 1);
            moved++;
        }

        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log($"[GroupHudElements] {moved} moved under '{GroupName}', {missing} not found. " +
                  $"Canvas now has {canvas.transform.childCount} direct children. Save the scene.");
    }
}

}
