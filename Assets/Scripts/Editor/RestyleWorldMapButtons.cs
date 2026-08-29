using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
        BuildChrome(template.transform.parent, controller);

        EditorSceneManager.MarkSceneDirty(template.gameObject.scene);

        Debug.Log($"[RestyleWorldMapButtons] Template restyled to {ButtonSize.x}x{ButtonSize.y} " +
                  "with plaque art. The other 24 clone it at runtime. Save the scene.");
    }

    /// <summary>
    /// Adds the map's title and a visible way out.
    ///
    /// The screen had neither: twenty-five plaques over an illustration, and nothing saying
    /// what it was or how to leave. ESC did close it — WorldMapUIController.CanCloseWithEsc
    /// is true — but a player has no way to know that, and a full-screen window with no exit
    /// control reads as a soft lock.
    ///
    /// Both are rebuilt from scratch each run so re-running the tool cannot stack duplicates.
    /// </summary>
    private static void BuildChrome(Transform parent, WorldMapUIController controller)
    {
        if (parent == null) return;

        foreach (string name in new[] { ChromeTitleName, ChromeBackName })
        {
            Transform existing = parent.Find(name);
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // Title: top-centre, over the illustration's empty sky band.
        var titleGO = new GameObject(ChromeTitleName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(titleGO, "Create world map title");
        titleGO.transform.SetParent(parent, false);

        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot     = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(520f, 64f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);

        var title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "Mapa-Múndi";
        title.fontSize = 34f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
        // Cream on the map's dark foliage, with an outline so it holds over the lighter
        // path running down the middle of the illustration.
        title.color = new Color(0.969f, 0.949f, 0.910f);
        title.outlineWidth = 0.22f;
        title.outlineColor = new Color32(40, 30, 20, 255);
        AddLocalizeEvent(title.gameObject, title, "ui_common.world_map_title");

        // Back button: bottom-centre, below the grid.
        var backGO = new GameObject(ChromeBackName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(backGO, "Create world map back button");
        backGO.transform.SetParent(parent, false);

        var backRect = backGO.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.pivot     = new Vector2(0.5f, 0f);
        backRect.sizeDelta = new Vector2(240f, 48f); // 5:1, matching the plaque art
        backRect.anchoredPosition = new Vector2(0f, 26f);

        var backImage = backGO.AddComponent<Image>();
        backImage.sprite = LoadSprite(LockedPath);
        backImage.type = Image.Type.Sliced;

        var backButton = backGO.AddComponent<Button>();
        backButton.targetGraphic = backImage;
        if (controller != null)
        {
            // Persistent listener, so the wiring survives in the scene file rather than
            // needing to be re-added at runtime.
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                backButton.onClick, controller.CloseMap);
        }

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(backGO.transform, false);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        // 45px per side would swallow a 240px button; the plaque's 71% still leaves room at
        // 30 for a single short word.
        labelRect.offsetMin = new Vector2(30f, 6f);
        labelRect.offsetMax = new Vector2(-30f, -6f);

        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = "Fechar";
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.176f, 0.165f, 0.149f);
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 13f;
        label.fontSizeMax = 18f;
        AddLocalizeEvent(label.gameObject, label, "ui_common.close");
    }

    /// <summary>
    /// Binds a label to the UI_Common table, matching how the rest of the project's static
    /// text is localized (see Editor/LocalizeStaticLabels).
    /// </summary>
    private static void AddLocalizeEvent(GameObject target, TMP_Text label, string key)
    {
        var evt = target.AddComponent<UnityEngine.Localization.Components.LocalizeStringEvent>();

        var so = new SerializedObject(evt);
        SerializedProperty reference = so.FindProperty("m_StringReference");
        reference.FindPropertyRelative("m_TableReference")
                 .FindPropertyRelative("m_TableCollectionName").stringValue = "UI_Common";
        reference.FindPropertyRelative("m_TableEntryReference")
                 .FindPropertyRelative("m_Key").stringValue = key;
        so.ApplyModifiedProperties();

        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            evt.OnUpdateString, new UnityEngine.Events.UnityAction<string>(label.SetText));
    }

    private const string ChromeTitleName = "WorldMapTitle";
    private const string ChromeBackName  = "WorldMapBackButton";

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
