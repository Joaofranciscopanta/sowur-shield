using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using SowurShield.Core;

namespace SowurShield.Combat
{

/// <summary>
/// Self-spawning overlay that shows the active battle modifier banner and the
/// player's current combo counter. Built procedurally (no scene wiring required)
/// and polls <see cref="TurnManager.Instance"/> each frame.
/// </summary>
public class BattleHudOverlay : MonoBehaviour
{
    private TextMeshProUGUI modifierText;
    private TextMeshProUGUI comboText;

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

        gameObject.AddComponent<GraphicRaycaster>().enabled = false;

        modifierText = CreateText("ModifierBanner", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), 28, FontStyles.Bold);
        modifierText.alignment = TextAlignmentOptions.Center;
        modifierText.color = new Color(1f, 0.95f, 0.6f);
        AddTextOutline(modifierText);
        modifierText.gameObject.SetActive(false);

        comboText = CreateText("ComboCounter", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40, 60), 32, FontStyles.Bold);
        comboText.alignment = TextAlignmentOptions.Right;
        comboText.color = new Color(1f, 0.6f, 0.2f);
        AddTextOutline(comboText);
        comboText.gameObject.SetActive(false);
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

        return tmp;
    }

    private void Update()
    {
        if (TurnManager.Instance == null)
        {
            if (modifierText.gameObject.activeSelf) modifierText.gameObject.SetActive(false);
            if (comboText.gameObject.activeSelf) comboText.gameObject.SetActive(false);
            lastShownModifier = BattleModifierType.None;
            activeManager = null;
            return;
        }

        if (activeManager != TurnManager.Instance)
        {
            activeManager = TurnManager.Instance;
            lastShownModifier = BattleModifierType.None;
        }

        UpdateModifierBanner();
        UpdateComboCounter();
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
