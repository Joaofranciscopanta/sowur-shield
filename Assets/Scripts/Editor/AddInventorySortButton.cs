using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SowurShield.Editor
{

/// <summary>
/// Puts a "Sort" button on the inventory panel and wires it to <see cref="Inventory.SortInventory"/>.
///
/// The sorting itself was already finished and correct -- it groups by item type, then by name,
/// compacts everything to the front of the storage area and leaves the hotbar alone. It simply
/// had no way to be triggered: InventoryUIManager declares sortByType/Name/Value/Rarity buttons
/// and hooks them up, but that component is not in the scene, so 36 storage slots shipped with
/// no sort control at all.
///
/// <para>One button rather than the four InventoryUIManager expects. Inventory.SortInventory()
/// is the parameterless type-then-name sort, which is the one people actually want; the other
/// three modes need a mode picker to be worth the panel space, and a row of four buttons on a
/// panel whose painted interior is already tight would crowd the grid.</para>
///
/// Menu: Sowur Shield > UI > Add Inventory Sort Button
/// </summary>
public static class AddInventorySortButton
{
    private const string ButtonName = "SortButton";

    // panel_wood_generic paints roughly 82/113/86/150px (left/right/top/bottom) inside its
    // 512px art and is asymmetric, so anchoring to the panel's rect edge would put the button
    // on the frame. This sits above the grid instead, which is centred and safely inside.
    private static readonly Vector2 ButtonSize = new Vector2(150f, 40f);

    [MenuItem("Sowur Shield/UI/Add Inventory Sort Button")]
    public static void Add()
    {
        // Presence check only -- the button is positioned against the panel, not the grid.
        // If there is no storage grid in the scene there is nothing to sort.
        bool hasStorage = Object.FindObjectsByType<RectTransform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Any(r => r.name == "StorageContainer");

        if (!hasStorage)
        {
            Debug.LogError("[AddInventorySortButton] No StorageContainer in the open scene.");
            return;
        }

        SowurShield.Inventory.Inventory inventory =
            Object.FindObjectsByType<SowurShield.Inventory.Inventory>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();

        if (inventory == null)
        {
            Debug.LogError("[AddInventorySortButton] No Inventory component in the open scene.");
            return;
        }

        // Parent to the panel background, not to the canvas. InventoryPanelBG is what
        // Inventory.ToggleInventory() switches on and off, so a button placed anywhere else
        // would hang on screen with the inventory closed. StorageContainer is not usable as
        // the parent either -- it carries a GridLayoutGroup that would seize the button and
        // lay it out as a 37th slot.
        RectTransform panel = Object.FindObjectsByType<RectTransform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(r => r.name == "InventoryPanelBG");

        if (panel == null)
        {
            Debug.LogError("[AddInventorySortButton] No InventoryPanelBG in the open scene; " +
                           "without it the button cannot follow the inventory's visibility.");
            return;
        }

        Transform parent = panel;

        Transform existing = parent.Find(ButtonName);
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        var go = new GameObject(ButtonName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create inventory sort button");
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        // Anchored to the panel's top-right corner, sitting just above the frame.
        //
        // There is no room for it inside. Measured on screen, the 36-slot grid overflows the
        // panel's painted interior on every side -- 46px past the top, 112px below the bottom
        // and 199px past the right -- so a button placed within the panel lands on either the
        // woodwork or the slots. Fixing that means re-laying out the grid, which is a larger
        // change than adding a sort control and is not what this tool is for.
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = ButtonSize;
        rect.anchoredPosition = new Vector2(-24f, 10f);

        var image = go.AddComponent<Image>();
        image.sprite = LoadSprite("Assets/Resources/Sprites/UI/Buttons/button_small_action.png");
        image.type = Image.Type.Sliced;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            button.onClick, inventory.SortInventory);

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        var labelRect = (RectTransform)labelGO.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        // The plaque art paints about 71% of its rect, so the label is inset against the
        // painted centre rather than the rect edge.
        labelRect.offsetMin = new Vector2(20f, 5f);
        labelRect.offsetMax = new Vector2(-20f, -5f);

        var label = labelGO.AddComponent<TextMeshProUGUI>();
        ApplyProjectFont(label);
        label.text = "Organizar";
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = 18f;
        // Dark on the plaque's light face -- panel_wood_generic's interior is cream, so a
        // pale label would vanish if the button art ever went translucent.
        label.color = new Color(0.176f, 0.165f, 0.149f);

        AddLocalizeEvent(labelGO, label, "ui_common.sort");

        EditorUtility.SetDirty(parent.gameObject);
        EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);

        Debug.Log($"[AddInventorySortButton] '{ButtonName}' added under '{parent.name}' and " +
                  $"wired to {inventory.name}.SortInventory(). Save the scene.");
    }

    /// <summary>
    /// Assigns the project font before anything touches the material -- a TextMeshProUGUI made
    /// from an editor script has none, and TMP throws on the first material-backed property.
    /// Nunito is also the only atlas in the project carrying accents.
    /// </summary>
    private static void ApplyProjectFont(TMP_Text label)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito SDF.asset");
        if (font != null) label.font = font;
    }

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

    private static Sprite LoadSprite(string path)
    {
        // These import as spriteMode Multiple, so LoadAssetAtPath<Sprite> returns null.
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite sprite) return sprite;

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}

}
