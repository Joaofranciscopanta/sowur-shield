using UnityEngine;
using TMPro;
using System.Collections;

public class SlotNumberIndicator : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI numberTextTMP;

    [Header("Animation Settings")]
    public float pulseScale = 1.2f;
    public float pulseDuration = 0.3f;
    public AnimationCurve pulseCurve = new AnimationCurve(new Keyframe(0, 0, 0, 2), new Keyframe(1, 1, 0, 0));

    [Header("Visual Settings")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.6f);
    public Color highlightColor = new Color(1f, 1f, 0.5f, 0.9f);
    public Color selectedColor = new Color(1f, 0.8f, 0.2f, 1f);

    private Coroutine currentAnimation;
    private Vector3 originalScale;
    private bool isSelected = false;

    private void Awake()
    {
        if (numberTextTMP == null)
        {
            numberTextTMP = GetComponent<TextMeshProUGUI>();
        }

        originalScale = transform.localScale;
        SetState(SlotState.Normal);
    }

    public enum SlotState
    {
        Normal,
        Highlighted,
        Selected
    }

    public void SetState(SlotState state)
    {
        if (numberTextTMP == null) return;

        Color targetColor = state switch
        {
            SlotState.Normal => normalColor,
            SlotState.Highlighted => highlightColor,
            SlotState.Selected => selectedColor,
            _ => normalColor
        };

        numberTextTMP.color = targetColor;
        isSelected = (state == SlotState.Selected);

        if (state == SlotState.Selected)
        {
            PulseAnimation();
        }
    }

    public void PulseAnimation()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        currentAnimation = StartCoroutine(DoPulseAnimation());
    }

    private IEnumerator DoPulseAnimation()
    {
        float elapsed = 0f;
        Vector3 targetScale = originalScale * pulseScale;

        // Scale up
        while (elapsed < pulseDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (pulseDuration * 0.5f);
            progress = pulseCurve.Evaluate(progress);

            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            yield return null;
        }

        elapsed = 0f;

        // Scale down
        while (elapsed < pulseDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (pulseDuration * 0.5f);
            progress = pulseCurve.Evaluate(progress);

            transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            yield return null;
        }

        transform.localScale = originalScale;
        currentAnimation = null;
    }

    private void OnDestroy()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
    }
}
