using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Localization;
using SowurShield.Core;

namespace SowurShield.UI
{

/// <summary>
/// Self-spawning Steam-style achievement unlock toast: slides/fades in at the bottom-right
/// corner of the screen, holds, then fades out. Queues multiple unlocks so simultaneous
/// achievements don't overlap. No Editor tooling or scene wiring needed — it builds its own
/// canvas at runtime, same bootstrap pattern as SeedShopUI.
/// </summary>
public class AchievementNotificationUI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<AchievementNotificationUI>(FindObjectsInactive.Include) != null)
            return;

        var go = new GameObject("AchievementNotificationUI");
        go.AddComponent<AchievementNotificationUI>();
        DontDestroyOnLoad(go);
    }

    private const float ToastWidth = 320f;
    private const float ToastHeight = 80f;
    private const float FadeInDuration = 0.3f;
    private const float HoldDuration = 3.0f;
    private const float FadeOutDuration = 0.5f;

    private readonly Queue<AchievementData> _pending = new Queue<AchievementData>();
    private bool _isShowing;

    private GameObject _toastGO;
    private CanvasGroup _canvasGroup;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _unlockedLabel;
    private Image _iconImage;
    private UITheme _theme;

    [Header("Localization")]
    [SerializeField] private LocalizedString achievementUnlockedLocalized; // table "UI_Common", key "ui_common.achievement_unlocked"

    // This component is created at runtime (never saved to a scene/prefab), so the
    // Tools > Sowur Shield > Auto-Wire Localized Fields editor pass can never reach it —
    // wire its LocalizedString table/key reference here instead.
    private void WireLocalizedStrings()
    {
        achievementUnlockedLocalized = new LocalizedString("UI_Common", "ui_common.achievement_unlocked");
    }

    private bool _buildSucceeded = false;

    private void Awake()
    {
        TryBuildUI();
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        if (_unlockedLabel != null) _unlockedLabel.text = achievementUnlockedLocalized.SafeGetLocalizedString();
    }

    private void TryBuildUI()
    {
        WireLocalizedStrings();
        try
        {
            BuildUI();
            _buildSucceeded = true;
        }
        catch (System.Exception e)
        {
            // Localization tables may not be configured yet (see MOBILE_LOCALIZATION_SETUP.md) —
            // fail safe rather than leaving a half-built toast visible, but keep retrying on the
            // next unlock instead of staying broken for the rest of the session.
            Debug.LogError($"[AchievementNotificationUI] BuildUI failed (Localization not configured?): {e}");
            _buildSucceeded = false;
            gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
        else
            StartCoroutine(RetrySubscriptionNextFrame());
    }

    private IEnumerator RetrySubscriptionNextFrame()
    {
        yield return null;
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
    }

    private void OnDestroy()
    {
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void OnAchievementUnlocked(AchievementData achievement)
    {
        if (!_buildSucceeded)
        {
            gameObject.SetActive(true);
            TryBuildUI();
            if (!_buildSucceeded)
                return;
        }

        _pending.Enqueue(achievement);
        if (!_isShowing)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        _isShowing = true;
        while (_pending.Count > 0)
        {
            AchievementData achievement = _pending.Dequeue();
            yield return ShowToast(achievement);
        }
        _isShowing = false;
    }

    // =========================================================================
    // UI construction (runtime, no Editor tool needed — simple enough to build inline)
    // =========================================================================

    private void BuildUI()
    {
        _theme = Resources.Load<UITheme>("UI/CozyUITheme");
        Color backgroundDark = _theme != null ? _theme.woodDark : new Color(0.06f, 0.06f, 0.08f);
        Color highlightGold = _theme != null ? _theme.highlightGold : new Color(0.96f, 0.83f, 0.37f);
        Color textCream = _theme != null ? _theme.backgroundCream : Color.white;

        var canvasGO = new GameObject("AchievementToastCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // above all other game UI

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var toastGO = new GameObject("Toast");
        toastGO.transform.SetParent(canvasGO.transform, false);
        var toastRT = toastGO.AddComponent<RectTransform>();
        toastRT.anchorMin = new Vector2(1, 0);
        toastRT.anchorMax = new Vector2(1, 0);
        toastRT.pivot = new Vector2(1, 0);
        toastRT.anchoredPosition = new Vector2(-20, 20);
        toastRT.sizeDelta = new Vector2(ToastWidth, ToastHeight);

        toastGO.AddComponent<Image>().color = new Color(backgroundDark.r, backgroundDark.g, backgroundDark.b, 0.95f);
        _canvasGroup = toastGO.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;

        // Icon
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(toastGO.transform, false);
        _iconImage = iconGO.AddComponent<Image>();
        _iconImage.preserveAspect = true;
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(10, 0);
        iconRT.sizeDelta = new Vector2(56, 56);

        // "Achievement Unlocked" label
        var labelGO = new GameObject("UnlockedLabel");
        labelGO.transform.SetParent(toastGO.transform, false);
        _unlockedLabel = labelGO.AddComponent<TextMeshProUGUI>();
        _unlockedLabel.text = achievementUnlockedLocalized.SafeGetLocalizedString();
        _unlockedLabel.fontSize = 12;
        _unlockedLabel.fontStyle = FontStyles.Bold;
        _unlockedLabel.color = highlightGold;
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0.55f);
        labelRT.anchorMax = new Vector2(1, 1f);
        labelRT.offsetMin = new Vector2(76, 0);
        labelRT.offsetMax = new Vector2(-10, -8);

        // Title
        var titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(toastGO.transform, false);
        _titleText = titleGO.AddComponent<TextMeshProUGUI>();
        _titleText.fontSize = 16;
        _titleText.fontStyle = FontStyles.Bold;
        _titleText.color = textCream;
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0f);
        titleRT.anchorMax = new Vector2(1, 0.55f);
        titleRT.offsetMin = new Vector2(76, 8);
        titleRT.offsetMax = new Vector2(-10, 0);

        toastGO.SetActive(false);
        _toastGO = toastGO;
    }

    private IEnumerator ShowToast(AchievementData achievement)
    {
        if (_iconImage != null)
        {
            _iconImage.sprite = achievement.icon;
            _iconImage.enabled = achievement.icon != null;
        }
        if (_titleText != null)
            _titleText.text = achievement.title;

        _toastGO.SetActive(true);

        yield return Fade(0f, 1f, FadeInDuration);
        yield return new WaitForSeconds(HoldDuration);
        yield return Fade(1f, 0f, FadeOutDuration);

        _toastGO.SetActive(false);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        _canvasGroup.alpha = to;
    }
}

} // namespace SowurShield.UI
