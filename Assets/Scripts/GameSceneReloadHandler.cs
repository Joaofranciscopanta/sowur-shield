using UnityEngine;
using UnityEngine.SceneManagement;

namespace SowurShield.Core
{

/// <summary>
/// Runs the "farm scene just (re)loaded" steps that SceneTransitionManager.OnGameSceneLoaded
/// is supposed to own, but never does — SceneTransitionManager is never instantiated
/// anywhere in the project (no scene/prefab wires it in, and every call site that checks
/// SceneTransitionManager.Instance falls back to a plain SceneManager.LoadScene). This
/// listens to SceneManager.sceneLoaded directly so scene-reload bookkeeping runs
/// unconditionally, most importantly when the player returns from CombatScene.
///
/// AnimalPurchaseLoader independently listens for the same event to recreate purchased
/// animals; this handler covers the rest of ReapplyLoadedDataToRegisteredObjects
/// (e.g. GroundItem restoring "already picked up" state) that SceneTransitionManager
/// used to gate.
/// </summary>
public class GameSceneReloadHandler : MonoBehaviour
{
    private static GameSceneReloadHandler instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        var go = new GameObject("GameSceneReloadHandler");
        go.AddComponent<GameSceneReloadHandler>();
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnAnySceneLoaded;
    }

    private void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "SampleScene") return;
        if (SaveManager.Instance == null) return;

        // Newly instantiated scene objects (e.g. GroundItem) register themselves with
        // SaveManager on Awake but never get their persisted state otherwise — only an
        // explicit LoadGame() call does that. Re-apply it here so items already picked
        // up before a trip to CombatScene don't reappear when the farm scene reloads.
        SaveManager.Instance.ReapplyLoadedDataToRegisteredObjects();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnAnySceneLoaded;

        if (instance == this)
            instance = null;
    }
}

} // namespace SowurShield.Core
