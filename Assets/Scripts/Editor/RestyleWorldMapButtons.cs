using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SowurShield.Worldmap;

namespace SowurShield.Editor
{

/// <summary>
/// Restyles the world map's stage buttons from Unity's default grey rectangle to the game's
/// own wood-and-cream plaque art.
///
/// The map is a painted illustration, and twenty-five `UISprite` rectangles sat on top of it
/// covering roughly 40% of the screen — placeholder chrome over finished art. Gold marks a
/// playable stage, cream a locked one, so progression reads at a glance instead of relying
/// on the disabled tint alone.
///
/// Only the template button is touched. WorldMapUiController clones it for the other
/// twenty-four at runtime, so the styling propagates without this tool knowing about them.
///
/// Size follows the art's proportion, not the old rect. The button kit is 600x120 — a 5:1
/// plaque — and the previous 253x78 was 3.2:1, stretching the frame corners. 310x62 keeps
/// 5:1 exactly and still fits five columns: 5 x 310 + 4 x 50 spacing = 1750 inside an
/// 1890px map. (This is the same trap that made the save-slot delete button invisible:
/// nine-slice borders never compress, so a rect that fights the art's ratio distorts it.)
///
/// Menu: Sowur Shield > UI > Restyle World Map Buttons
/// </summary>
public static class RestyleWorldMapButtons
{
    private const string UnlockedPath = "Assets/Resources/Sprites/UI/Buttons/button_primary.png";
    private const string LockedPath   = "Assets/Resources/Sprites/UI/Buttons/button_secondary.png";

    // 5:1, matching the 600x120 source art. Width is driven by the longest stage name against
    // a plaque whose painted centre is only 71% of the rect — see WorldMapUiController.
    private static readonly Vector2 ButtonSize = new Vector2(350f, 70f);

    // 5 x 350 + 4 x 30 = 1870, inside the 1890px map.
    private static readonly Vector2 ButtonSpacing = new Vector2(30f, 40f);

    [MenuItem("Sowur Shield/UI/Restyle World Map Buttons")]
    public static void Restyle()
    {
        StageButton template = Resources.FindObjectsOfTypeAll<StageButton>()
            .FirstOrDefault(b => b != null
                              && b.gameObject.scene.name != null
                              && !b.name.StartsWith("StageButton_Generated_"));

        if (template == null)
        {
            Debug.LogError("[RestyleWorldMapButtons] No template StageButton in the open " +
                           "scene. Open SampleScene first.");
            return;
        }

        Sprite unlocked = LoadSprite(UnlockedPath);
        Sprite locked   = LoadSprite(LockedPath);
        if (unlocked == null || locked == null)
        {
            Debug.LogError("[RestyleWorldMapButtons] Could not load the button art.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(template.gameObject, "Restyle world map buttons");

        var image = template.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = unlocked;
            image.type = Image.Type.Sliced;
        }

        var rect = template.GetComponent<RectTransform>();
        if (rect != null)
            rect.sizeDelta = ButtonSize;

        var so = new SerializedObject(template);
        so.FindProperty("unlockedSprite").objectReferenceValue = unlocked;
        so.FindProperty("lockedSprite").objectReferenceValue   = locked;
        if (image != null)
            so.FindProperty("buttonImage").objectReferenceValue = image;
        so.ApplyModifiedProperties();

        // The label is deliberately left alone here. WorldMapUiController rewrites its font
        // sizing, wrapping and margins for every button — the template included — each time
        // the map opens, so anything set from this tool is overwritten before it is ever
        // seen. The values that keep long names off the painted frame live there instead.

        // The controller's cell size is serialized on the scene object, so changing the field
        // default in code does nothing to an existing scene — it has to be written here too,
        // or the grid keeps laying out 300x110 cells around 310x62 buttons.
        var controller = Resources.FindObjectsOfTypeAll<WorldMapUIController>()
            .FirstOrDefault(c => c != null && c.gameObject.scene.name != null);

        if (controller != null)
        {
            var cso = new SerializedObject(controller);
            SerializedProperty cell    = cso.FindProperty("flatButtonCellSize");
            SerializedProperty spacing = cso.FindProperty("flatButtonSpacing");
            if (cell != null)
            {
                Undo.RecordObject(controller, "Resize world map grid cells");
                cell.vector2Value = ButtonSize;
                if (spacing != null)
                    spacing.vector2Value = ButtonSpacing;
                cso.ApplyModifiedProperties();
                EditorUtility.SetDirty(controller);
            }
        }
        else
        {
            Debug.LogWarning("[RestyleWorldMapButtons] No WorldMapUIController found; the " +
                             "grid will still lay out at its old cell size.");
        }

        EditorUtility.SetDirty(template);
        EditorSceneManager.MarkSceneDirty(template.gameObject.scene);

        Debug.Log($"[RestyleWorldMapButtons] Template restyled to {ButtonSize.x}x{ButtonSize.y} " +
                  "with plaque art. The other 24 clone it at runtime. Save the scene.");
    }

    private static Sprite LoadSprite(string path)
    {
        // These import as spriteMode Multiple, so LoadAssetAtPath<Sprite> returns null.
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite sprite)
                return sprite;

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}

}
