using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace SowurShield.Combat
{

/// <summary>
/// Lightweight per-unit VFX: status effect icons and floating damage/heal numbers.
/// Built procedurally (no prefabs required) and attached automatically by CombatUnit.
/// </summary>
[RequireComponent(typeof(CombatUnit))]
public class CombatUnitVFX : MonoBehaviour
{
    [Tooltip("Offset above the unit where status icons are shown.")]
    [SerializeField] private Vector3 statusIconOffset = new Vector3(0, 0.95f, 0);

    [Tooltip("Spacing between status icons.")]
    [SerializeField] private float statusIconSpacing = 0.22f;

    [Tooltip("Offset above the unit where floating text spawns.")]
    [SerializeField] private Vector3 floatingTextOffset = new Vector3(0, 0.4f, 0);

    private CombatUnit unit;
    private Camera mainCamera;
    private readonly Dictionary<StatusEffectType, GameObject> activeStatusIcons = new Dictionary<StatusEffectType, GameObject>();

    private static readonly Dictionary<StatusEffectType, (string label, Color color)> StatusIconStyles =
        new Dictionary<StatusEffectType, (string, Color)>
    {
        { StatusEffectType.Stun,     ("STN", new Color(0.9f, 0.9f, 0.3f)) },
        { StatusEffectType.Shield,   ("SHD", new Color(0.4f, 0.7f, 1f)) },
        { StatusEffectType.Burn,     ("BRN", new Color(1f, 0.5f, 0.1f)) },
        { StatusEffectType.Poison,   ("PSN", new Color(0.6f, 0.9f, 0.2f)) },
        { StatusEffectType.Weakness, ("WKN", new Color(0.8f, 0.4f, 0.8f)) },
    };

    private void Awake()
    {
        unit = GetComponent<CombatUnit>();
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        unit.OnStatusApplied += HandleStatusApplied;
        unit.OnStatusExpired += HandleStatusExpired;
        unit.OnDamageTaken += HandleDamageTaken;
        unit.OnHealed += HandleHealed;
    }

    private void OnDisable()
    {
        unit.OnStatusApplied -= HandleStatusApplied;
        unit.OnStatusExpired -= HandleStatusExpired;
        unit.OnDamageTaken -= HandleDamageTaken;
        unit.OnHealed -= HandleHealed;
    }

    // ── Status icons ───────────────────────────────────────────────────────

    private void HandleStatusApplied(StatusEffectType type)
    {
        unit.TriggerStatusAnimation(type);

        if (activeStatusIcons.ContainsKey(type))
            return;

        if (!StatusIconStyles.TryGetValue(type, out var style))
            return;

        GameObject iconObj = new GameObject($"StatusIcon_{type}");
        iconObj.transform.SetParent(transform, false);

        TextMeshPro tmp = iconObj.AddComponent<TextMeshPro>();
        tmp.text = style.label;
        tmp.fontSize = 2.5f;
        tmp.color = style.color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 20;

        activeStatusIcons[type] = iconObj;
        RepositionStatusIcons();
    }

    private void HandleStatusExpired(StatusEffectType type)
    {
        if (activeStatusIcons.TryGetValue(type, out GameObject iconObj))
        {
            activeStatusIcons.Remove(type);
            if (iconObj != null)
                Destroy(iconObj);
            RepositionStatusIcons();
        }
    }

    private void RepositionStatusIcons()
    {
        int i = 0;
        float totalWidth = (activeStatusIcons.Count - 1) * statusIconSpacing;
        foreach (var icon in activeStatusIcons.Values)
        {
            if (icon == null) continue;
            float x = -totalWidth / 2f + i * statusIconSpacing;
            icon.transform.localPosition = statusIconOffset + new Vector3(x, 0, 0);
            i++;
        }
    }

    // ── Floating damage/heal text ────────────────────────────────────────

    private void HandleDamageTaken(float amount, bool isCrit)
    {
        if (amount <= 0f) return;

        Color color = isCrit ? new Color(1f, 0.85f, 0.1f) : new Color(1f, 0.25f, 0.25f);
        float fontSize = isCrit ? 4f : 3f;
        string text = isCrit ? $"{Mathf.CeilToInt(amount)}!" : Mathf.CeilToInt(amount).ToString();

        SpawnFloatingText(text, color, fontSize);
    }

    private void HandleHealed(float amount)
    {
        if (amount <= 0f) return;

        SpawnFloatingText($"+{Mathf.CeilToInt(amount)}", new Color(0.3f, 1f, 0.4f), 3f);
    }

    private void SpawnFloatingText(string text, Color color, float fontSize)
    {
        GameObject textObj = new GameObject("FloatingText");
        textObj.transform.SetParent(transform, false);
        textObj.transform.localPosition = floatingTextOffset;

        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 30;

        StartCoroutine(FloatAndFadeCoroutine(textObj, tmp));
    }

    private IEnumerator FloatAndFadeCoroutine(GameObject textObj, TextMeshPro tmp)
    {
        const float duration = 0.8f;
        const float riseDistance = 0.6f;

        Vector3 startPos = textObj.transform.position;
        Vector3 endPos = startPos + Vector3.up * riseDistance;
        Color startColor = tmp.color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            textObj.transform.position = Vector3.Lerp(startPos, endPos, t);
            tmp.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

            if (mainCamera != null)
                textObj.transform.rotation = mainCamera.transform.rotation;

            yield return null;
        }

        Destroy(textObj);
    }
}

}
