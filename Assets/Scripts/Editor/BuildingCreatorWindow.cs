using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Localization;
using SowurShield.Core;

namespace SowurShield.Editor
{

/// <summary>
/// Editor window for creating FarmBuildingData ScriptableObjects.
/// Provides a form UI with validation, preview of existing buildings,
/// and one-click asset creation into Resources/Buildings/.
///
/// OPEN: Tools > Sowur Shield > Building Creator
/// </summary>
public class BuildingCreatorWindow : EditorWindow
{
    // =========================================================================
    // Form state
    // =========================================================================

    private BuildingType _buildingType = BuildingType.Barn;
    private string _buildingName       = "New Building";
    private string _description        = "";
    private string _effectDescription  = "";
    private int    _goldCost           = 500;
    private string _materialItemName   = "";
    private int    _materialQuantity   = 0;
    private Sprite _icon               = null;

    // UI state
    private Vector2 _scrollForm;
    private Vector2 _scrollList;
    private string  _statusMessage     = "";
    private bool    _statusIsError     = false;
    private double  _statusShownAt;
    private const double STATUS_DISPLAY_SECONDS = 4.0;

    private List<FarmBuildingData> _existingBuildings = new List<FarmBuildingData>();
    private FarmBuildingData       _selectedPreview   = null;

    private static readonly string RESOURCE_PATH = "Assets/Resources/Buildings";

    // =========================================================================
    // Menu item
    // =========================================================================

    [MenuItem("Tools/Sowur Shield/Building Creator")]
    public static void Open()
    {
        BuildingCreatorWindow win = GetWindow<BuildingCreatorWindow>("Building Creator");
        win.minSize = new Vector2(520, 460);
        win.RefreshExistingBuildings();
    }

    // =========================================================================
    // GUI
    // =========================================================================

    private void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.BeginHorizontal();
        {
            // Left column — form
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            DrawForm();
            EditorGUILayout.EndVertical();

            GUILayout.Space(8);

            // Right column — existing buildings list
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawExistingList();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();

        DrawStatusBar();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(6);
        GUIStyle title = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("Farm Building Creator", title, GUILayout.Height(24));
        EditorGUILayout.LabelField("Creates FarmBuildingData assets in Resources/Buildings/",
            EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(4);
        DrawSeparator();
    }

    private void DrawForm()
    {
        _scrollForm = EditorGUILayout.BeginScrollView(_scrollForm);

        EditorGUILayout.LabelField("Building Properties", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        _buildingType = (BuildingType)EditorGUILayout.EnumPopup(
            new GUIContent("Type", "Maps to the BuildingType enum used by game systems."),
            _buildingType);

        _buildingName = EditorGUILayout.TextField(
            new GUIContent("Name", "Display name shown in the BuildingShopUI."),
            _buildingName);

        EditorGUILayout.LabelField(new GUIContent("Description", "Flavour text / lore."));
        _description = EditorGUILayout.TextArea(_description, GUILayout.Height(48));

        EditorGUILayout.LabelField(new GUIContent("Effect Description", "Short line shown in shop UI (e.g. 'Doubles animal capacity')."));
        _effectDescription = EditorGUILayout.TextArea(_effectDescription, GUILayout.Height(36));

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Cost", EditorStyles.boldLabel);

        _goldCost = Mathf.Max(0, EditorGUILayout.IntField(
            new GUIContent("Gold Cost", "Gold deducted from PlayerStats.money on purchase."),
            _goldCost));

        _materialItemName = EditorGUILayout.TextField(
            new GUIContent("Material Item Name", "Must match ItemDatabase key exactly. Leave blank for gold-only cost."),
            _materialItemName);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_materialItemName)))
        {
            _materialQuantity = Mathf.Max(0, EditorGUILayout.IntField(
                new GUIContent("Material Quantity", "Amount of the material item required."),
                _materialQuantity));
        }

        if (!string.IsNullOrWhiteSpace(_materialItemName))
        {
            EditorGUILayout.HelpBox(
                $"Player must have {_materialQuantity}x '{_materialItemName}' in inventory.",
                MessageType.Info);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);

        _icon = (Sprite)EditorGUILayout.ObjectField(
            new GUIContent("Icon", "Sprite shown in BuildingShopUI row."),
            _icon, typeof(Sprite), false);

        EditorGUILayout.Space(10);

        // Validation
        string validationError = GetValidationError();
        if (!string.IsNullOrEmpty(validationError))
        {
            EditorGUILayout.HelpBox(validationError, MessageType.Warning);
        }

        // File preview
        string targetPath = GetTargetPath();
        bool alreadyExists = File.Exists(targetPath);
        if (alreadyExists)
        {
            EditorGUILayout.HelpBox(
                $"Asset already exists at:\n{targetPath}\nCreating will overwrite it.",
                MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(validationError)))
        {
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.85f, 0.45f);
            if (GUILayout.Button(alreadyExists ? "Overwrite Building Asset" : "Create Building Asset",
                btnStyle, GUILayout.Height(32)))
            {
                CreateBuildingAsset();
            }
            GUI.backgroundColor = prev;
        }

        GUILayout.Space(4);
        if (GUILayout.Button("Reset Form"))
            ResetForm();

        EditorGUILayout.EndScrollView();
    }

    private void DrawExistingList()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Existing Buildings", EditorStyles.boldLabel);
        if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(56)))
            RefreshExistingBuildings();
        EditorGUILayout.EndHorizontal();

        DrawSeparator();

        _scrollList = EditorGUILayout.BeginScrollView(_scrollList);

        if (_existingBuildings.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No FarmBuildingData assets found in Resources/Buildings/.\nCreate one using the form on the left.",
                MessageType.Info);
        }
        else
        {
            foreach (var building in _existingBuildings)
            {
                if (building == null) continue;

                bool isSelected = _selectedPreview == building;
                Color prevBg = GUI.backgroundColor;
                if (isSelected) GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);

                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (building.icon != null)
                        {
                            Texture2D tex = AssetPreview.GetAssetPreview(building.icon);
                            if (tex != null)
                                GUILayout.Label(tex, GUILayout.Width(32), GUILayout.Height(32));
                            else
                                GUILayout.Space(36);
                        }
                        else
                        {
                            GUILayout.Space(36);
                        }

                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField(building.buildingName.SafeGetLocalizedString(), EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(
                            $"{building.buildingType} | {building.goldCost}g" +
                            (string.IsNullOrEmpty(building.materialItemName)
                                ? ""
                                : $" + {building.materialQuantity}x {building.materialItemName}"),
                            EditorStyles.miniLabel);
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndHorizontal();

                    string effectDescText = building.effectDescription.SafeGetLocalizedString();
                    if (!string.IsNullOrEmpty(effectDescText))
                        EditorGUILayout.LabelField(effectDescText, EditorStyles.wordWrappedMiniLabel);

                    EditorGUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Load into Form", EditorStyles.miniButton))
                            LoadIntoForm(building);
                        if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(40)))
                            EditorGUIUtility.PingObject(building);
                        if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                            Selection.activeObject = building;

                        Color prev2 = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                        if (GUILayout.Button("Delete", EditorStyles.miniButton, GUILayout.Width(48)))
                            DeleteBuilding(building);
                        GUI.backgroundColor = prev2;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();

                GUI.backgroundColor = prevBg;
                GUILayout.Space(2);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawStatusBar()
    {
        if (string.IsNullOrEmpty(_statusMessage)) return;

        // Auto-clear after timeout
        if (EditorApplication.timeSinceStartup - _statusShownAt > STATUS_DISPLAY_SECONDS)
        {
            _statusMessage = "";
            return;
        }

        DrawSeparator();
        GUIStyle style = new GUIStyle(EditorStyles.helpBox)
        {
            normal = { textColor = _statusIsError ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.1f, 0.6f, 0.1f) },
            fontStyle = FontStyle.Bold
        };
        EditorGUILayout.LabelField(_statusMessage, style);
        Repaint(); // Keep repaint running so auto-clear fires
    }

    // =========================================================================
    // Actions
    // =========================================================================

    // Wires buildingName/description/effectDescription to table entries keyed by buildingType, but
    // does not write the form's typed text into the String Table — that text only exists as form
    // state here. After creating the asset, add the actual EN/PT/ES strings for these keys via
    // Tools > Sowur Shield > Import Localization CSV (or the Localization Tables window directly).
    private void CreateBuildingAsset()
    {
        EnsureResourceFolder();

        string path = GetTargetPath();

        FarmBuildingData asset = ScriptableObject.CreateInstance<FarmBuildingData>();
        asset.buildingType      = _buildingType;
        asset.buildingName      = new LocalizedString("Farming", $"building.{_buildingType}.name");
        asset.description       = new LocalizedString("Farming", $"building.{_buildingType}.description");
        asset.effectDescription = new LocalizedString("Farming", $"building.{_buildingType}.effect");
        asset.goldCost          = _goldCost;
        asset.materialItemName  = _materialItemName.Trim();
        asset.materialQuantity  = _materialQuantity;
        asset.icon              = _icon;

        // Overwrite if exists
        FarmBuildingData existing = AssetDatabase.LoadAssetAtPath<FarmBuildingData>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(asset, existing);
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }

        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<FarmBuildingData>(path));

        SetStatus($"Created: {path}", false);
        RefreshExistingBuildings();
    }

    private void DeleteBuilding(FarmBuildingData building)
    {
        string path = AssetDatabase.GetAssetPath(building);
        if (string.IsNullOrEmpty(path)) return;

        if (!EditorUtility.DisplayDialog(
            "Delete Building",
            $"Delete '{building.buildingName.SafeGetLocalizedString()}'?\n\n{path}",
            "Delete", "Cancel"))
        {
            return;
        }

        AssetDatabase.DeleteAsset(path);
        AssetDatabase.Refresh();
        SetStatus($"Deleted: {building.buildingName.SafeGetLocalizedString()}", false);
        if (_selectedPreview == building) _selectedPreview = null;
        RefreshExistingBuildings();
    }

    private void LoadIntoForm(FarmBuildingData data)
    {
        _buildingType      = data.buildingType;
        _buildingName      = data.buildingName.SafeGetLocalizedString();
        _description       = data.description.SafeGetLocalizedString();
        _effectDescription = data.effectDescription.SafeGetLocalizedString();
        _goldCost          = data.goldCost;
        _materialItemName  = data.materialItemName;
        _materialQuantity  = data.materialQuantity;
        _icon              = data.icon;
        _selectedPreview   = data;
        SetStatus($"Loaded '{_buildingName}' into form.", false);
    }

    private void ResetForm()
    {
        _buildingType      = BuildingType.Barn;
        _buildingName      = "New Building";
        _description       = "";
        _effectDescription = "";
        _goldCost          = 500;
        _materialItemName  = "";
        _materialQuantity  = 0;
        _icon              = null;
        _selectedPreview   = null;
        _statusMessage     = "";
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private string GetValidationError()
    {
        if (string.IsNullOrWhiteSpace(_buildingName))
            return "Building Name is required.";
        if (_goldCost < 0)
            return "Gold Cost cannot be negative.";
        if (!string.IsNullOrWhiteSpace(_materialItemName) && _materialQuantity <= 0)
            return "Material Quantity must be > 0 when a Material Item Name is set.";
        return null;
    }

    private string GetTargetPath()
    {
        string safeName = _buildingName.Trim().Replace(" ", "");
        return $"{RESOURCE_PATH}/{safeName}.asset";
    }

    private void EnsureResourceFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(RESOURCE_PATH))
            AssetDatabase.CreateFolder("Assets/Resources", "Buildings");
    }

    private void RefreshExistingBuildings()
    {
        _existingBuildings.Clear();
        string[] guids = AssetDatabase.FindAssets("t:FarmBuildingData", new[] { RESOURCE_PATH });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            FarmBuildingData data = AssetDatabase.LoadAssetAtPath<FarmBuildingData>(path);
            if (data != null) _existingBuildings.Add(data);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        _statusMessage  = message;
        _statusIsError  = isError;
        _statusShownAt  = EditorApplication.timeSinceStartup;
    }

    private static void DrawSeparator()
    {
        EditorGUILayout.Space(2);
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.4f, 0.4f, 0.4f, 0.5f));
        EditorGUILayout.Space(2);
    }
}

} // namespace SowurShield.Editor
