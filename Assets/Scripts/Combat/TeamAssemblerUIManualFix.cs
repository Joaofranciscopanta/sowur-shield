using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SowurShield.Combat
{

/// <summary>
/// Simple script to manually assign references to TeamAssemblerUI.
/// Attach to TeamAssemblerUI GameObject and click "Assign References" in Inspector.
/// </summary>
public class TeamAssemblerUIManualFix : MonoBehaviour
{
#if UNITY_EDITOR
    [ContextMenu("Assign References")]
    private void AssignReferences()
    {
        TeamAssemblerUI ui = GetComponent<TeamAssemblerUI>();
        if (ui == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(ui);

        // Find and assign each reference
        AssignByPath(so, "assemblerPanel", "AssemblerPanel");
        AssignByPath(so, "animalSelectionPanel", "AssemblerPanel/AnimalSelectionPanel");
        AssignByPath(so, "gridPanel", "AssemblerPanel/GridPanel");
        AssignByPath(so, "animalCardContainer", "AssemblerPanel/AnimalSelectionPanel/Scroll View/Viewport/Content");
        AssignByPath(so, "gridContainer", "AssemblerPanel/GridPanel/GridContainer");

        // Assign prefabs
        AssignPrefab(so, "gridSlotPrefab", "Assets/Prefabs/Combat/GridSlotPrefab.prefab");
        AssignPrefab(so, "animalCardPrefab", "Assets/Prefabs/Combat/AnimalCardPrefab.prefab");

        // Assign UI elements
        AssignByPath(so, "zoneNameText", "AssemblerPanel/InfoPanel/ZoneNameText");
        AssignByPath(so, "teamSizeText", "AssemblerPanel/InfoPanel/TeamSizeText");
        AssignByPath(so, "foodRequirementsText", "AssemblerPanel/InfoPanel/FoodRequirementsText");
        AssignByPath(so, "synergiesText", "AssemblerPanel/InfoPanel/SynergiesText");

        // Assign buttons
        AssignByPath(so, "feedAllButton", "AssemblerPanel/ButtonContainer/FeedAllButton");
        AssignByPath(so, "clearGridButton", "AssemblerPanel/ButtonContainer/ClearGridButton");
        AssignByPath(so, "startBattleButton", "AssemblerPanel/ButtonContainer/StartBattleButton");
        AssignByPath(so, "cancelButton", "AssemblerPanel/ButtonContainer/CancelButton");

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ui);

    }

    private void AssignByPath(SerializedObject so, string propertyName, string path)
    {
        Transform target = transform.Find(path);
        if (target == null)
        {
            return;
        }

        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            return;
        }

        // Assign based on type
        if (propertyName.Contains("Text"))
        {
            prop.objectReferenceValue = target.GetComponent<TextMeshProUGUI>();
        }
        else if (propertyName.Contains("Button"))
        {
            prop.objectReferenceValue = target.GetComponent<Button>();
        }
        else if (propertyName.Contains("Panel"))
        {
            prop.objectReferenceValue = target.gameObject;
        }
        else
        {
            prop.objectReferenceValue = target;
        }

    }

    private void AssignPrefab(SerializedObject so, string propertyName, string assetPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            return;
        }

        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            prop.objectReferenceValue = prefab;
        }
    }
#endif
}

} // namespace SowurShield.Combat
