using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor window for creating Item ScriptableObjects and their matching GroundItem prefabs.
/// Centralizes item creation so you can go from "I need an Egg item" to a fully configured
/// Item asset + GroundItem prefab in one click.
///
/// Access via: Tools > Item System > Item Creator
/// Context menu: Right-click any Item asset > Generate GroundItem Prefab
/// </summary>
public class ItemCreatorWindow : EditorWindow
{
    // Paths
    private const string ITEMS_FOLDER = "Assets/Resources/Items";
    private const string PREFABS_FOLDER = "Assets/Resources/Prefabs/GroundItems";

    // Scroll positions
    private Vector2 mainScrollPos;
    private Vector2 overviewScrollPos;

    // --- Create New Item fields ---
    private string newItemName = "New Item";
    private string newItemDescription = "A useful item";
    private Sprite newItemIcon;
    private ItemType newItemType = ItemType.Resource;
    private ItemRarity newItemRarity = ItemRarity.Common;
    private bool newItemStackable = true;
    private int newItemMaxStack = 99;
    private int newItemBaseValue = 1;
    private bool newItemCanBeSold = true;
    private bool newItemIsConsumable = false;
    private int newItemEnergyRestore = 0;
    private int newItemHealthRestore = 0;
    private List<string> newItemTags = new List<string>();
    private string newTagInput = "";
    private bool newItemCreatePrefab = true;

    // --- Generate Prefab from Existing Item ---
    private Item existingItem;

    // --- Overview ---
    private bool showOverview = true;
    private List<ItemOverviewEntry> overviewEntries = new List<ItemOverviewEntry>();
    private bool overviewNeedsRefresh = true;

    private struct ItemOverviewEntry
    {
        public Item item;
        public string itemPath;
        public bool hasPrefab;
        public string prefabPath;
    }

    [MenuItem("Tools/Item System/Item Creator")]
    public static void ShowWindow()
    {
        var window = GetWindow<ItemCreatorWindow>("Item Creator");
        window.minSize = new Vector2(400, 500);
    }

    private void OnEnable()
    {
        overviewNeedsRefresh = true;
    }

    private void OnFocus()
    {
        overviewNeedsRefresh = true;
    }

    private void OnGUI()
    {
        mainScrollPos = EditorGUILayout.BeginScrollView(mainScrollPos);

        // ═══════════════════════════════════════════════
        // Section 1: Create New Item
        // ═══════════════════════════════════════════════
        DrawCreateNewItemSection();

        EditorGUILayout.Space(10);
        DrawSeparator();
        EditorGUILayout.Space(10);

        // ═══════════════════════════════════════════════
        // Section 2: Generate Prefab from Existing Item
        // ═══════════════════════════════════════════════
        DrawExistingItemSection();

        EditorGUILayout.Space(10);
        DrawSeparator();
        EditorGUILayout.Space(10);

        // ═══════════════════════════════════════════════
        // Section 3: Item Overview
        // ═══════════════════════════════════════════════
        DrawItemOverview();

        EditorGUILayout.EndScrollView();
    }

    // ───────────────────────────────────────────────
    // Section 1: Create New Item + Prefab
    // ───────────────────────────────────────────────
    private void DrawCreateNewItemSection()
    {
        GUILayout.Label("Create New Item", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Basic Info
        EditorGUILayout.LabelField("Basic Info", EditorStyles.miniLabel);
        newItemName = EditorGUILayout.TextField("Item Name", newItemName);
        newItemDescription = EditorGUILayout.TextField("Description", newItemDescription);
        newItemIcon = (Sprite)EditorGUILayout.ObjectField("Icon", newItemIcon, typeof(Sprite), false);
        newItemType = (ItemType)EditorGUILayout.EnumPopup("Item Type", newItemType);
        newItemRarity = (ItemRarity)EditorGUILayout.EnumPopup("Rarity", newItemRarity);

        EditorGUILayout.Space(3);

        // Stacking
        EditorGUILayout.LabelField("Stacking", EditorStyles.miniLabel);
        newItemStackable = EditorGUILayout.Toggle("Is Stackable", newItemStackable);
        if (newItemStackable)
        {
            newItemMaxStack = EditorGUILayout.IntField("Max Stack Size", newItemMaxStack);
        }

        EditorGUILayout.Space(3);

        // Value
        EditorGUILayout.LabelField("Value", EditorStyles.miniLabel);
        newItemBaseValue = EditorGUILayout.IntField("Base Value", newItemBaseValue);
        newItemCanBeSold = EditorGUILayout.Toggle("Can Be Sold", newItemCanBeSold);

        EditorGUILayout.Space(3);

        // Consumable
        EditorGUILayout.LabelField("Consumable", EditorStyles.miniLabel);
        newItemIsConsumable = EditorGUILayout.Toggle("Is Consumable", newItemIsConsumable);
        if (newItemIsConsumable)
        {
            newItemEnergyRestore = EditorGUILayout.IntField("Energy Restore", newItemEnergyRestore);
            newItemHealthRestore = EditorGUILayout.IntField("Health Restore", newItemHealthRestore);
        }

        EditorGUILayout.Space(3);

        // Tags
        EditorGUILayout.LabelField("Item Tags", EditorStyles.miniLabel);
        for (int i = 0; i < newItemTags.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            newItemTags[i] = EditorGUILayout.TextField(newItemTags[i]);
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                newItemTags.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.BeginHorizontal();
        newTagInput = EditorGUILayout.TextField(newTagInput);
        if (GUILayout.Button("Add Tag", GUILayout.Width(70)))
        {
            if (!string.IsNullOrWhiteSpace(newTagInput))
            {
                newItemTags.Add(newTagInput.Trim());
                newTagInput = "";
                GUI.FocusControl(null);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Prefab toggle
        newItemCreatePrefab = EditorGUILayout.Toggle("Also Create GroundItem Prefab", newItemCreatePrefab);

        EditorGUILayout.Space(5);

        // Validation
        bool canCreate = !string.IsNullOrWhiteSpace(newItemName);
        string existingPath = Path.Combine(ITEMS_FOLDER, newItemName + ".asset");
        if (canCreate && File.Exists(existingPath))
        {
            EditorGUILayout.HelpBox($"An item named '{newItemName}' already exists at {existingPath}. It will be overwritten.", MessageType.Warning);
        }

        // Create button
        GUI.enabled = canCreate;
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button(newItemCreatePrefab ? "Create Item + GroundItem Prefab" : "Create Item Only", GUILayout.Height(30)))
        {
            Item createdItem = CreateItemAsset();
            if (createdItem != null && newItemCreatePrefab)
            {
                CreateGroundItemPrefab(createdItem);
            }
            overviewNeedsRefresh = true;
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
    }

    // ───────────────────────────────────────────────
    // Section 2: Generate Prefab from Existing Item
    // ───────────────────────────────────────────────
    private void DrawExistingItemSection()
    {
        GUILayout.Label("Generate Prefab from Existing Item", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        existingItem = (Item)EditorGUILayout.ObjectField("Item Asset", existingItem, typeof(Item), false);

        EditorGUILayout.Space(3);

        GUI.enabled = existingItem != null;
        if (GUILayout.Button("Generate GroundItem Prefab", GUILayout.Height(25)))
        {
            CreateGroundItemPrefab(existingItem);
            overviewNeedsRefresh = true;
        }
        GUI.enabled = true;

        EditorGUILayout.HelpBox(
            "Drag an existing Item ScriptableObject here to generate a matching GroundItem prefab.\n" +
            "You can also right-click any Item asset in the Project window > 'Generate GroundItem Prefab'.",
            MessageType.Info);
    }

    // ───────────────────────────────────────────────
    // Section 3: Item Overview
    // ───────────────────────────────────────────────
    private void DrawItemOverview()
    {
        showOverview = EditorGUILayout.Foldout(showOverview, "Item Overview", true, EditorStyles.foldoutHeader);

        if (!showOverview) return;

        if (overviewNeedsRefresh)
        {
            RefreshOverview();
            overviewNeedsRefresh = false;
        }

        if (overviewEntries.Count == 0)
        {
            EditorGUILayout.HelpBox("No items found in Resources. Create some items first!", MessageType.Info);
            return;
        }

        // Stats
        int withPrefab = 0;
        int withoutPrefab = 0;
        foreach (var entry in overviewEntries)
        {
            if (entry.hasPrefab) withPrefab++;
            else withoutPrefab++;
        }

        EditorGUILayout.LabelField($"Total: {overviewEntries.Count} items  |  With Prefab: {withPrefab}  |  Missing Prefab: {withoutPrefab}");
        EditorGUILayout.Space(3);

        // Batch generate button
        if (withoutPrefab > 0)
        {
            GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
            if (GUILayout.Button($"Generate {withoutPrefab} Missing Prefab(s)", GUILayout.Height(25)))
            {
                int created = 0;
                foreach (var entry in overviewEntries)
                {
                    if (!entry.hasPrefab && entry.item != null)
                    {
                        CreateGroundItemPrefab(entry.item);
                        created++;
                    }
                }
                Debug.Log($"[ItemCreator] Created {created} GroundItem prefabs.");
                overviewNeedsRefresh = true;
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space(3);

        // Item list
        overviewScrollPos = EditorGUILayout.BeginScrollView(overviewScrollPos, GUILayout.MaxHeight(300));

        foreach (var entry in overviewEntries)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Icon preview
            if (entry.item != null && entry.item.icon != null)
            {
                GUILayout.Label(AssetPreview.GetAssetPreview(entry.item.icon) ?? Texture2D.grayTexture,
                    GUILayout.Width(32), GUILayout.Height(32));
            }
            else
            {
                GUILayout.Label(Texture2D.grayTexture, GUILayout.Width(32), GUILayout.Height(32));
            }

            // Item info
            EditorGUILayout.BeginVertical();
            string itemDisplayName = entry.item != null ? entry.item.itemName : "???";
            EditorGUILayout.LabelField(itemDisplayName, EditorStyles.boldLabel);

            if (entry.hasPrefab)
            {
                var prevColor = GUI.contentColor;
                GUI.contentColor = Color.green;
                EditorGUILayout.LabelField("✓ Prefab exists", EditorStyles.miniLabel);
                GUI.contentColor = prevColor;
            }
            else
            {
                var prevColor = GUI.contentColor;
                GUI.contentColor = new Color(1f, 0.4f, 0.4f);
                EditorGUILayout.LabelField("✗ No prefab", EditorStyles.miniLabel);
                GUI.contentColor = prevColor;
            }
            EditorGUILayout.EndVertical();

            // Action buttons
            if (!entry.hasPrefab)
            {
                if (GUILayout.Button("Create\nPrefab", GUILayout.Width(60), GUILayout.Height(32)))
                {
                    CreateGroundItemPrefab(entry.item);
                    overviewNeedsRefresh = true;
                }
            }
            else
            {
                if (GUILayout.Button("Select\nPrefab", GUILayout.Width(60), GUILayout.Height(32)))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.prefabPath);
                    if (prefab != null)
                    {
                        Selection.activeObject = prefab;
                        EditorGUIUtility.PingObject(prefab);
                    }
                }
            }

            if (GUILayout.Button("Select\nItem", GUILayout.Width(60), GUILayout.Height(32)))
            {
                Selection.activeObject = entry.item;
                EditorGUIUtility.PingObject(entry.item);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(3);
        if (GUILayout.Button("Refresh Overview"))
        {
            overviewNeedsRefresh = true;
            Repaint();
        }
    }

    // ═══════════════════════════════════════════════
    // Core Methods
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Creates an Item ScriptableObject from the current form fields.
    /// Saves to Assets/Resources/Items/{itemName}.asset
    /// </summary>
    private Item CreateItemAsset()
    {
        EnsureFolderExists(ITEMS_FOLDER);

        Item item = ScriptableObject.CreateInstance<Item>();
        item.itemName = newItemName.Trim();
        item.description = newItemDescription;
        item.icon = newItemIcon;
        item.itemType = newItemType;
        item.rarity = newItemRarity;
        item.isStackable = newItemStackable;
        item.maxStackSize = newItemMaxStack;
        item.baseValue = newItemBaseValue;
        item.canBeSold = newItemCanBeSold;
        item.isConsumable = newItemIsConsumable;
        item.energyRestore = newItemEnergyRestore;
        item.healthRestore = newItemHealthRestore;
        item.itemTags = new List<string>(newItemTags);

        string assetPath = Path.Combine(ITEMS_FOLDER, item.itemName + ".asset");

        AssetDatabase.CreateAsset(item, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = item;
        EditorGUIUtility.PingObject(item);

        Debug.Log($"[ItemCreator] Created Item asset: {assetPath}");
        return item;
    }

    /// <summary>
    /// Creates a GroundItem prefab from an existing Item ScriptableObject.
    /// The prefab includes SpriteRenderer, BoxCollider2D (trigger), and GroundItem component.
    /// Saves to Assets/Resources/Prefabs/GroundItems/{itemName}_GroundItem.prefab
    /// </summary>
    public static GameObject CreateGroundItemPrefab(Item item)
    {
        if (item == null)
        {
            Debug.LogWarning("[ItemCreator] Cannot create prefab: Item is null.");
            return null;
        }

        EnsureFolderExists(PREFABS_FOLDER);

        string safeName = item.itemName.Replace(" ", "");
        string prefabPath = Path.Combine(PREFABS_FOLDER, safeName + "_GroundItem.prefab");

        // Create temporary GameObject to build the prefab
        GameObject tempObj = new GameObject(safeName + "_GroundItem");

        try
        {
            // Tag — must match existing GroundItem.prefab
            tempObj.tag = "Interactable";

            // SpriteRenderer — displays the item icon in the world
            SpriteRenderer sr = tempObj.AddComponent<SpriteRenderer>();
            if (item.icon != null)
            {
                sr.sprite = item.icon;
            }
            sr.sortingOrder = 3; // Match existing GroundItem prefab

            // BoxCollider2D — trigger for player pickup detection
            BoxCollider2D col = tempObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            // Auto-size collider to sprite bounds
            if (item.icon != null)
            {
                col.size = new Vector2(item.icon.bounds.size.x, item.icon.bounds.size.y);
            }
            else
            {
                col.size = new Vector2(0.72f, 0.85f); // Default from existing prefab
            }

            // GroundItem component — handles pickup logic, floating, etc.
            GroundItem groundItem = tempObj.AddComponent<GroundItem>();

            // Save as prefab FIRST, then assign the item reference via SerializedObject
            // (this ensures proper serialization)
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempObj, prefabPath);

            // Now assign the Item reference on the prefab asset directly
            if (prefab != null)
            {
                GroundItem prefabGroundItem = prefab.GetComponent<GroundItem>();
                if (prefabGroundItem != null)
                {
                    SerializedObject so = new SerializedObject(prefabGroundItem);
                    so.FindProperty("item").objectReferenceValue = item;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                AssetDatabase.SaveAssets();

                Debug.Log($"[ItemCreator] Created GroundItem prefab: {prefabPath}");
                return prefab;
            }
        }
        finally
        {
            // Always clean up the temp scene object
            DestroyImmediate(tempObj);
        }

        return null;
    }

    // ═══════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════

    private void RefreshOverview()
    {
        overviewEntries.Clear();

        // Find ALL Item assets in the project (not just Resources/Items)
        string[] guids = AssetDatabase.FindAssets("t:Item");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Item item = AssetDatabase.LoadAssetAtPath<Item>(path);
            if (item == null) continue;

            // Check if a matching prefab exists
            string safeName = item.itemName.Replace(" ", "");
            string expectedPrefabPath = Path.Combine(PREFABS_FOLDER, safeName + "_GroundItem.prefab");
            bool hasPrefab = File.Exists(expectedPrefabPath);

            overviewEntries.Add(new ItemOverviewEntry
            {
                item = item,
                itemPath = path,
                hasPrefab = hasPrefab,
                prefabPath = expectedPrefabPath
            });
        }

        // Sort: items without prefabs first, then alphabetical
        overviewEntries.Sort((a, b) =>
        {
            if (a.hasPrefab != b.hasPrefab)
                return a.hasPrefab ? 1 : -1;
            string nameA = a.item != null ? a.item.itemName : "";
            string nameB = b.item != null ? b.item.itemName : "";
            return string.Compare(nameA, nameB, System.StringComparison.OrdinalIgnoreCase);
        });
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        // Build the path incrementally
        string[] parts = folderPath.Split('/');
        string current = parts[0]; // "Assets"

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private static void DrawSeparator()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }

    // ═══════════════════════════════════════════════
    // Context Menu: Right-click Item asset → Generate GroundItem Prefab
    // ═══════════════════════════════════════════════

    [MenuItem("Assets/Generate GroundItem Prefab", true)]
    private static bool ValidateGenerateGroundItemPrefab()
    {
        return Selection.activeObject is Item;
    }

    [MenuItem("Assets/Generate GroundItem Prefab")]
    private static void GenerateGroundItemPrefabFromSelection()
    {
        Item item = Selection.activeObject as Item;
        if (item != null)
        {
            GameObject prefab = CreateGroundItemPrefab(item);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
        }
    }
}
