using UnityEngine;
using UnityEditor;
using SowurShield.Core;

namespace SowurShield.Editor
{

/// <summary>
/// Editor tool to ensure an AchievementManager exists in the scene — without it,
/// AchievementManager.Instance stays null and no achievement ever unlocks.
/// Menu: Tools > Sowur Shield > Ensure Achievement Manager
/// </summary>
public static class AchievementManagerSetup
{
    [MenuItem("Tools/Sowur Shield/Ensure Achievement Manager")]
    public static void EnsureAchievementManagerMenuItem()
    {
        var existing = Object.FindFirstObjectByType<AchievementManager>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("[AchievementManagerSetup] AchievementManager already present in the scene — nothing to do.");
            return;
        }

        var managerGO = new GameObject("AchievementManager");
        Undo.RegisterCreatedObjectUndo(managerGO, "Create AchievementManager");
        managerGO.AddComponent<AchievementManager>();

        Selection.activeGameObject = managerGO;
        EditorGUIUtility.PingObject(managerGO);

        EditorUtility.DisplayDialog("Done!",
            "AchievementManager created.\n\n" +
            "It loads every asset from Resources/Achievements and listens for quest " +
            "completions, crop harvests, animal purchases, stage clears, and item sales " +
            "to unlock achievements automatically — no further wiring needed.",
            "OK");
    }
}

} // namespace SowurShield.Editor
