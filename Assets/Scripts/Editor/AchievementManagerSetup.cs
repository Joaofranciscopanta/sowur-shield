using UnityEngine;
using UnityEditor;
using SowurShield.Core;
using SowurShield.Animals;
using SowurShield.Dialogue;

namespace SowurShield.Editor
{

/// <summary>
/// Editor tool to ensure the game's core singleton managers exist in the scene.
/// Each of these silently does nothing (Instance stays null, no error) when missing —
/// InteractionManager missing means no NPC/animal/soil is interactable via E; QuestManager
/// missing means quests never start; AnimalRoster missing means animals never get listed
/// for sale; AchievementManager missing means achievements never unlock.
/// Menu: Tools > Sowur Shield > Ensure Core Managers
/// </summary>
public static class AchievementManagerSetup
{
    [MenuItem("Tools/Sowur Shield/Ensure Core Managers")]
    public static void EnsureCoreManagersMenuItem()
    {
        int created = 0;
        created += EnsureSingleton<InteractionManager>("InteractionManager") ? 1 : 0;
        created += EnsureSingleton<QuestManager>("QuestManager") ? 1 : 0;
        created += EnsureSingleton<AnimalRoster>("AnimalRoster") ? 1 : 0;
        created += EnsureSingleton<AchievementManager>("AchievementManager") ? 1 : 0;

        if (created > 0)
        {
            EditorUtility.DisplayDialog("Done!",
                $"Created {created} missing manager(s).\n\n" +
                "InteractionManager: required for ANY NPC/animal/soil E-key interaction.\n" +
                "QuestManager: required for quests to start/track/complete.\n" +
                "AnimalRoster: required for AnimalMarketUI's Sell tab and the animal-owned achievement.\n" +
                "AchievementManager: required for any achievement to unlock.",
                "OK");
        }
        else
        {
            Debug.Log("[AchievementManagerSetup] All core managers already present in the scene — nothing to do.");
        }
    }

    [MenuItem("Tools/Sowur Shield/Ensure Achievement Manager")]
    public static void EnsureAchievementManagerMenuItem()
    {
        bool created = EnsureSingleton<AchievementManager>("AchievementManager");
        if (created)
        {
            EditorUtility.DisplayDialog("Done!",
                "AchievementManager created.\n\n" +
                "It loads every asset from Resources/Achievements and listens for quest " +
                "completions, crop harvests, animal purchases, stage clears, and item sales " +
                "to unlock achievements automatically — no further wiring needed.",
                "OK");
        }
        else
        {
            Debug.Log("[AchievementManagerSetup] AchievementManager already present in the scene — nothing to do.");
        }
    }

    /// <summary>Creates a GameObject with the given component if none exists yet. Returns true if created.</summary>
    private static bool EnsureSingleton<T>(string gameObjectName) where T : MonoBehaviour
    {
        var existing = Object.FindFirstObjectByType<T>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            return false;
        }

        var go = new GameObject(gameObjectName);
        Undo.RegisterCreatedObjectUndo(go, $"Create {gameObjectName}");
        go.AddComponent<T>();

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        return true;
    }
}

} // namespace SowurShield.Editor
