using System.Collections;
using UnityEngine;

namespace SowurShield.Combat
{

/// <summary>
/// Listens for <see cref="TurnManager.OnTelegraph"/> and briefly brightens the
/// acting unit and its target so players can see who's about to act before
/// the action resolves.
///
/// Self-spawning: no scene wiring required.
/// </summary>
public class TelegraphHighlighter : MonoBehaviour
{
    [Tooltip("How long the highlight stays visible.")]
    [SerializeField] private float highlightDuration = 0.25f;

    [Tooltip("Color of the telegraph glow ring.")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.6f, 0.6f);

    private TurnManager subscribedManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<TelegraphHighlighter>() != null)
            return;

        var go = new GameObject("TelegraphHighlighter");
        go.AddComponent<TelegraphHighlighter>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        // TurnManager.Instance may not exist yet when this object is created;
        // keep trying to subscribe until it appears (e.g. after scene load).
        if (subscribedManager == null && TurnManager.Instance != null)
        {
            subscribedManager = TurnManager.Instance;
            subscribedManager.OnTelegraph += HandleTelegraph;
        }
        else if (subscribedManager != null && TurnManager.Instance != subscribedManager)
        {
            // A new TurnManager instance took over (new battle) — resubscribe.
            subscribedManager.OnTelegraph -= HandleTelegraph;
            subscribedManager = null;
        }
    }

    private void OnDestroy()
    {
        if (subscribedManager != null)
            subscribedManager.OnTelegraph -= HandleTelegraph;
    }

    private void HandleTelegraph(TurnManager.TelegraphInfo info)
    {
        if (info.actor != null)
            StartCoroutine(HighlightCoroutine(info.actor));

        if (info.target != null && info.target != info.actor)
            StartCoroutine(HighlightCoroutine(info.target));
    }

    private IEnumerator HighlightCoroutine(CombatUnit unit)
    {
        if (unit == null || unit.visualObject == null)
            yield break;

        SpriteRenderer targetSR = unit.visualObject.GetComponent<SpriteRenderer>();
        if (targetSR == null)
            yield break;

        // Spawn a temporary glow sprite behind the unit, sized to match it.
        GameObject glow = new GameObject("TelegraphGlow");
        glow.transform.SetParent(unit.transform, false);
        glow.transform.localPosition = new Vector3(0, 0, 0.01f);
        // Local scale is a plain 1.25, NOT the unit's scale * 1.25: the glow is a
        // child, so it already inherits the unit's scale. Multiplying by it again
        // squared the value — a unit at 5.33 produced a 35.56 glow, which rendered
        // as a huge translucent sprite covering the board. The unit's own scale is
        // also negative on X (units face right by mirroring), which flipped it.
        glow.transform.localScale = Vector3.one * 1.25f;

        SpriteRenderer glowSR = glow.AddComponent<SpriteRenderer>();
        glowSR.sprite = targetSR.sprite;
        glowSR.color = highlightColor;
        glowSR.sortingOrder = targetSR.sortingOrder - 1;

        float elapsed = 0f;
        while (elapsed < highlightDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (glow != null)
            Destroy(glow);
    }
}

}
