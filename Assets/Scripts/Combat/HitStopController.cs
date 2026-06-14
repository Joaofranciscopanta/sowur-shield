using System.Collections;
using UnityEngine;

namespace SowurShield.Combat
{

/// <summary>
/// Listens for <see cref="TurnManager.OnBigHit"/> and applies a brief hit-stop
/// (time slow-down) plus a small camera shake for extra impact on crits and big hits.
///
/// Self-spawning: no scene wiring required. A hidden GameObject is created
/// automatically the first time a scene loads.
/// </summary>
public class HitStopController : MonoBehaviour
{
    [Tooltip("Time.timeScale value applied during hit-stop.")]
    [SerializeField] private float hitStopTimeScale = 0.05f;

    [Tooltip("Camera shake magnitude (world units).")]
    [SerializeField] private float shakeMagnitude = 0.1f;

    private Coroutine activeRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<HitStopController>() != null)
            return;

        var go = new GameObject("HitStopController");
        go.AddComponent<HitStopController>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        TurnManager.OnBigHit += HandleBigHit;
    }

    private void OnDisable()
    {
        TurnManager.OnBigHit -= HandleBigHit;
    }

    private void HandleBigHit(float duration)
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        float previousScale = Time.timeScale;
        Time.timeScale = hitStopTimeScale;

        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 originalCamPos = cam != null ? cam.localPosition : Vector3.zero;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (cam != null)
            {
                float t = 1f - Mathf.Clamp01(elapsed / duration);
                Vector2 offset = Random.insideUnitCircle * shakeMagnitude * t;
                cam.localPosition = originalCamPos + new Vector3(offset.x, offset.y, 0f);
            }

            yield return null;
        }

        if (cam != null)
            cam.localPosition = originalCamPos;

        Time.timeScale = previousScale;
        activeRoutine = null;
    }
}

}
