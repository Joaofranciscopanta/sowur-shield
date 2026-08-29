using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using SowurShield.Core;

namespace SowurShield.Combat
{

/// <summary>
/// Self-spawning overlay that shows the active battle modifier banner, the
/// player's current combo counter, and the 1x/2x battle speed toggle. Built
/// procedurally (no scene wiring required) and polls
/// <see cref="TurnManager.Instance"/> each frame.
/// </summary>
public class BattleHudOverlay : MonoBehaviour
{
    private TextMeshProUGUI modifierText;
    private TextMeshProUGUI comboText;
    private GameObject speedButtonObj;
    private TextMeshProUGUI speedButtonLabel;

    [SerializeField] private LocalizedString comboText_Localized; // table "Combat", key "combat.hud.combo"

    private TurnManager activeManager;
    private BattleModifierType lastShownModifier = BattleModifierType.None;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<BattleHudOverlay>() != null)
            return;

        var go = new GameObject("BattleHudOverlay");
        go.AddComponent<BattleHudOverlay>();
        DontDestroyOnLoad(go);
    }

    // This component is created at runtime (never saved to a scene/prefab), so the
    // Tools > Sowur Shield > Auto-Wire Localized Fields editor pass can never reach it —
    // wire its LocalizedString table/key reference here instead.
    private void WireLocalizedStrings()
    {
        comboText_Localized = new LocalizedString("Combat", "combat.hud.combo");
    }

    private void Awake()
    {
        WireLocalizedStrings();
        BuildUI();
    }

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Raycaster stays enabled for the speed button. The two text objects set
        // raycastTarget = false so they never swallow clicks meant for the battlefield.
        gameObject.AddComponent<GraphicRaycaster>();

        // y = -110, not -40. The status top bar (turn counter, team counts) occupies the first
        // ~80px of the screen, so a banner at -40 was drawn straight through "Your Team: n/n" —
        // 241px of direct overlap, with both strings legible only as a smear. Clearing the bar
        // costs nothing here because the banner is transient and the space below it is empty.
        modifierText = CreateText("ModifierBanner", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110), 28, FontStyles.Bold);
        modifierText.alignment = TextAlignmentOptions.Center;
        modifierText.color = new Color(1f, 0.95f, 0.6f);
        AddTextOutline(modifierText);
        modifierText.gameObject.SetActive(false);

        comboText = CreateText("ComboCounter", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40, 60), 32, FontStyles.Bold);
        comboText.alignment = TextAlignmentOptions.Right;
        comboText.color = new Color(1f, 0.6f, 0.2f);
        AddTextOutline(comboText);
        comboText.gameObject.SetActive(false);

        BuildSpeedButton();
    }

    /// <summary>
    /// Bottom-right 1x/2x battle speed toggle. Sits below the combo counter (which
    /// anchors at y=60) and mirrors the ConsumableBattleUI items button on the
    /// opposite corner, so the two never overlap.
    /// </summary>
    private void BuildSpeedButton()
    {
        speedButtonObj = new GameObject("SpeedButton");
        speedButtonObj.transform.SetParent(transform, false);

        RectTransform rect = speedButtonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-40, 20);
        rect.sizeDelta = new Vector2(90, 40);

        Image image = speedButtonObj.AddComponent<Image>();
        Sprite sprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_small_action");
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }
        else
        {
            // Sprite missing from Resources — flat tint keeps the button usable.
            image.color = new Color(0.2f, 0.2f, 0.25f, 0.85f);
        }

        speedButtonObj.AddComponent<Button>().onClick.AddListener(OnSpeedButtonClicked);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(speedButtonObj.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        speedButtonLabel = labelObj.AddComponent<TextMeshProUGUI>();
        speedButtonLabel.fontSize = 24;
        speedButtonLabel.fontStyle = FontStyles.Bold;
        speedButtonLabel.alignment = TextAlignmentOptions.Center;
        speedButtonLabel.raycastTarget = false;
        // The gold button sprite needs dark text; cream only reads on the flat wood tint.
        speedButtonLabel.color = sprite != null ? new Color(0.18f, 0.12f, 0.06f) : Color.white;

        speedButtonObj.SetActive(false);
    }

    private void OnSpeedButtonClicked()
    {
        if (activeManager == null) return;
        RefreshSpeedLabel(activeManager.ToggleSpeedMultiplier());
    }

    // Speed is a raw multiplier shown as "1x"/"2x" — the same glyph in every supported
    // language, so it deliberately skips the localization table.
    private void RefreshSpeedLabel(float multiplier)
    {
        if (speedButtonLabel != null)
            speedButtonLabel.text = multiplier >= 2f ? "2x" : "1x";
    }

    // Floating HUD text sits directly over the stage art with no backing panel, so it needs
    // a dark outline to stay readable against bright backgrounds.
    private static void AddTextOutline(TextMeshProUGUI tmp)
    {
        tmp.fontMaterial.EnableKeyword("OUTLINE_ON");
        tmp.outlineColor = new Color32(30, 20, 10, 255);
        tmp.outlineWidth = 0.25f;
    }

    private TextMeshProUGUI CreateText(string name, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, float fontSize, FontStyles style)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(500, 60);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        // The overlay's raycaster is on for the speed button; HUD labels must not
        // block clicks aimed at the battlefield underneath them.
        tmp.raycastTarget = false;

        return tmp;
    }

    private void Update()
    {
        if (TurnManager.Instance == null)
        {
            if (modifierText.gameObject.activeSelf) modifierText.gameObject.SetActive(false);
            if (comboText.gameObject.activeSelf) comboText.gameObject.SetActive(false);
            if (speedButtonObj.activeSelf) speedButtonObj.SetActive(false);
            lastShownModifier = BattleModifierType.None;
            activeManager = null;
            return;
        }

        if (activeManager != TurnManager.Instance)
        {
            activeManager = TurnManager.Instance;
            lastShownModifier = BattleModifierType.None;
            // New battle: adopt whatever speed it restored from PlayerPrefs.
            RefreshSpeedLabel(activeManager.SpeedMultiplier);
        }

        UpdateModifierBanner();
        UpdateComboCounter();
        UpdateSpeedButton();
    }

    private void UpdateSpeedButton()
    {
        bool shouldShow = activeManager.combatActive;
        if (speedButtonObj.activeSelf != shouldShow)
            speedButtonObj.SetActive(shouldShow);
    }

    private void UpdateModifierBanner()
    {
        BattleModifier mod = activeManager.GetActiveModifier();
        if (mod == null || mod.type == BattleModifierType.None)
        {
            if (modifierText.gameObject.activeSelf)
                modifierText.gameObject.SetActive(false);
            return;
        }

        if (mod.type != lastShownModifier)
        {
            modifierText.text = mod.GetDescription();
            modifierText.gameObject.SetActive(true);
            lastShownModifier = mod.type;
        }
    }

    private void UpdateComboCounter()
    {
        int combo = activeManager.GetComboCount();
        if (combo <= 1)
        {
            if (comboText.gameObject.activeSelf)
                comboText.gameObject.SetActive(false);
            return;
        }

        comboText_Localized.Arguments = new object[] { combo };
        string text = comboText_Localized.SafeGetLocalizedString();
        comboText.text = string.IsNullOrEmpty(text) ? $"Combo x{combo}!" : text;
        if (!comboText.gameObject.activeSelf)
            comboText.gameObject.SetActive(true);
    }
}

}
