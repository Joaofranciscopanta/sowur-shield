using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using SowurShield.Core;

namespace SowurShield.Animals
{

/// <summary>
/// Re-instantiates animals bought at runtime via AnimalMarketUI. Purchased animals are
/// plain runtime GameObjects (see AnimalMarketUI.SpawnPurchasedAnimal) — they don't exist
/// in any scene file, so unlike hand-placed animals they vanish on scene reload (e.g.
/// returning from CombatScene) or on a disk load unless something recreates them first.
/// Mirrors WorldLoader's soil-block recreation pattern.
///
/// IMPORTANT: this listens to SceneManager.sceneLoaded directly rather than relying on
/// SceneTransitionManager.OnGameSceneLoaded. SceneTransitionManager is never actually
/// instantiated anywhere in the project (no scene, prefab, or bootstrap creates it — every
/// call site just falls back to a direct SceneManager.LoadScene), so anything that only
/// runs through it never executes in the real game. This was the actual root cause of
/// purchased animals (and, separately, the inventory snapshot restore) not surviving a
/// return from CombatScene.
/// </summary>
public class AnimalPurchaseLoader : MonoBehaviour
{
    public static AnimalPurchaseLoader Instance { get; private set; }

    // Self-bootstrapping (like TestItemSpawner) instead of requiring manual scene wiring
    // (like WorldLoader does) — this must exist reliably for purchased animals to survive
    // any scene reload, not just the ones a scene author remembered to wire it into.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        var go = new GameObject("AnimalPurchaseLoader");
        go.AddComponent<AnimalPurchaseLoader>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Covers an explicit "load save" (disk read).
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnLoadCompleted += OnGameLoaded;

            if (SaveManager.Instance.CurrentGameData != null)
                StartCoroutine(RecreatePurchasedAnimalsCoroutine());
        }
        else
        {
            StartCoroutine(WaitForSaveManager());
        }

        // Covers a scene reload with no disk load involved (e.g. CombatScene ->
        // SampleScene on battle return) — see class remarks on why this can't rely on
        // SceneTransitionManager. GameSceneReloadHandler handles the equivalent
        // ReapplyLoadedDataToRegisteredObjects step for everything else (e.g. GroundItem).
        SceneManager.sceneLoaded += OnAnySceneLoaded;
    }

    private IEnumerator WaitForSaveManager()
    {
        yield return new WaitForSeconds(0.5f);

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnLoadCompleted += OnGameLoaded;

            if (SaveManager.Instance.CurrentGameData != null)
                StartCoroutine(RecreatePurchasedAnimalsCoroutine());
        }
    }

    private void OnGameLoaded(bool success)
    {
        if (success)
            StartCoroutine(RecreatePurchasedAnimalsCoroutine());
    }

    private void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "SampleScene") return;
        // sceneLoaded fires after Awake() but before Start() of the new scene's objects —
        // wait a frame so hand-placed AnimalZones etc. have fully initialized before we
        // search for one to assign (see GameSceneReloadHandler for the same guard, which
        // was needed for a confirmed real bug on SellBox).
        StartCoroutine(RecreateNextFrame());
    }

    private IEnumerator RecreateNextFrame()
    {
        yield return null;
        if (SaveManager.Instance?.CurrentGameData != null)
            RecreatePurchasedAnimals(SaveManager.Instance.CurrentGameData);
    }

    private IEnumerator RecreatePurchasedAnimalsCoroutine()
    {
        yield return null; // let other systems (zones, GameBalance) initialize first

        if (SaveManager.Instance?.CurrentGameData != null)
            RecreatePurchasedAnimals(SaveManager.Instance.CurrentGameData);
    }

    /// <summary>
    /// Recreates any purchased-animal GameObject recorded in gameData that isn't already
    /// present in the scene, then lets Animal.LoadData restore its saved attributes.
    /// Called by SceneTransitionManager.OnGameSceneLoaded before the general
    /// ReapplyLoadedDataToRegisteredObjects pass, and by SaveManager after a disk load.
    /// </summary>
    public void RecreatePurchasedAnimals(GameData gameData)
    {
        if (gameData?.worldData?.purchasedAnimals == null) return;
        StartCoroutine(RecreatePurchasedAnimalsRoutine(gameData));
    }

    private IEnumerator RecreatePurchasedAnimalsRoutine(GameData gameData)
    {
        AnimalData[] allAnimalData = null; // lazy-load only if needed
        var created = new System.Collections.Generic.List<Animal>();

        foreach (var record in gameData.worldData.purchasedAnimals)
        {
            if (string.IsNullOrEmpty(record.gameObjectName)) continue;
            if (GameObject.Find(record.gameObjectName) != null) continue; // already exists

            allAnimalData ??= Resources.LoadAll<AnimalData>("Animals");
            AnimalData data = allAnimalData.FirstOrDefault(a =>
                a != null && string.Equals(a.animalName, record.animalDataName, System.StringComparison.OrdinalIgnoreCase));

            if (data == null)
            {
                Debug.LogWarning($"[AnimalPurchaseLoader] Could not find AnimalData '{record.animalDataName}' for purchased animal '{record.gameObjectName}' — skipping.");
                continue;
            }

            AnimalZone zone = null;
            if (!string.IsNullOrEmpty(record.zoneName))
            {
                GameObject zoneGO = GameObject.Find(record.zoneName);
                zone = zoneGO != null ? zoneGO.GetComponent<AnimalZone>() : null;
            }
            zone ??= FindObjectsByType<AnimalZone>(FindObjectsSortMode.None).FirstOrDefault(z => !z.IsFull);

            if (zone == null)
            {
                Debug.LogWarning($"[AnimalPurchaseLoader] No AnimalZone available for purchased animal '{record.gameObjectName}' — skipping.");
                continue;
            }

            GameObject go = new GameObject(record.gameObjectName);
            go.transform.position = zone.GetRandomPointInZone();

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = data.idleSprite;
            sr.sortingOrder = 5;
            float scale = SpriteScaleUtility.GetScaleForTargetMaxDimension(sr.sprite, 0.56f);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var animal = go.AddComponent<Animal>();
            animal.InitializeFromMarket(data, zone); // before Start(), which first reads animalData

            go.AddComponent<AnimalAI>();
            created.Add(animal);
        }

        if (created.Count == 0) yield break;

        // Animal.Start() (next frame) initializes default attributes — e.g. happiness from
        // GameBalance — which would clobber anything applied before it runs. Wait for Start()
        // to have executed, then apply the saved attributes on top, mirroring WorldLoader's
        // manual LoadData call for recreated soil blocks.
        yield return null;

        foreach (var animal in created)
        {
            if (animal != null)
                animal.LoadData(gameData);
        }
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnLoadCompleted -= OnGameLoaded;

        SceneManager.sceneLoaded -= OnAnySceneLoaded;

        if (Instance == this)
            Instance = null;
    }
}

} // namespace SowurShield.Animals
