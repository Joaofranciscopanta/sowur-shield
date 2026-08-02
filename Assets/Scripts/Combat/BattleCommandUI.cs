using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization;
using SowurShield.Animals;
using SowurShield.Core;
using SowurShield.UI;

namespace SowurShield.Combat
{

/// <summary>
/// Command panel for active-pause combat: when one of the player's animals fills its
/// turn gauge the battle freezes and this panel asks what it should do.
///
/// Self-spawning and built procedurally (same pattern as ConsumableBattleUI and
/// BattleHudOverlay) so it needs no scene wiring. Attack and Skill then ask for a
/// target by clicking an enemy; Defend and a cancelled target selection resolve
/// without one.
/// </summary>
public class BattleCommandUI : MonoBehaviour
{
    private GameObject panel;
    private TextMeshProUGUI unitNameLabel;
    private TextMeshProUGUI promptLabel;
    private Button attackButton;
    private Button skillButton;
    private TextMeshProUGUI skillLabel;
    private Button defendButton;
    private Button cancelTargetButton;

    private TurnManager boundManager;
    private CombatUnit commandingUnit;

    /// <summary>Which command is waiting for the player to click a target (null = none).</summary>
    private TurnManager.PlayerActionType? awaitingTargetFor;

    private UITheme theme;

    [SerializeField] private LocalizedString attackText_Localized;      // table "Combat", key "combat.command.attack"
    [SerializeField] private LocalizedString defendText_Localized;      // table "Combat", key "combat.command.defend"
    [SerializeField] private LocalizedString cancelText_Localized;      // table "Combat", key "combat.command.cancel"
    [SerializeField] private LocalizedString choosePromptText_Localized;// table "Combat", key "combat.command.choose_action"
    [SerializeField] private LocalizedString targetPromptText_Localized;// table "Combat", key "combat.command.choose_target"
    [SerializeField] private LocalizedString skillCooldownText_Localized; // table "Combat", key "combat.command.skill_cooldown"

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<BattleCommandUI>() != null)
            return;

        var go = new GameObject("BattleCommandUI");
        go.AddComponent<BattleCommandUI>();
        DontDestroyOnLoad(go);
    }

    // Created at runtime, so the Tools > Sowur Shield > Auto-Wire Localized Fields pass
    // can never reach it — wire the table/key references here instead.
    private void WireLocalizedStrings()
    {
        attackText_Localized       = new LocalizedString("Combat", "combat.command.attack");
        defendText_Localized       = new LocalizedString("Combat", "combat.command.defend");
        cancelText_Localized       = new LocalizedString("Combat", "combat.command.cancel");
        choosePromptText_Localized = new LocalizedString("Combat", "combat.command.choose_action");
        targetPromptText_Localized = new LocalizedString("Combat", "combat.command.choose_target");
        skillCooldownText_Localized = new LocalizedString("Combat", "combat.command.skill_cooldown");
    }

    private void Awake()
    {
        theme = Resources.Load<UITheme>("UI/CozyUITheme");
        WireLocalizedStrings();
        BuildUI();
    }

    private void OnDestroy() => Unbind();

    // ── UI construction ────────────────────────────────────────────────────────

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Above BattleHudOverlay/ConsumableBattleUI (both 100) so the command panel is
        // never covered while the battle is frozen waiting on it.
        canvas.sortingOrder = 120;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        panel = new GameObject("CommandPanel");
        panel.transform.SetParent(transform, false);

        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 110f);
        panelRect.sizeDelta = new Vector2(560f, 190f);

        var bg = panel.AddComponent<Image>();
        Sprite panelSprite = Resources.Load<Sprite>("Sprites/UI/Panels/panel_wood_generic");
        if (panelSprite != null)
        {
            bg.sprite = panelSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
        }
        else
        {
            bg.color = theme != null
                ? new Color(theme.woodDark.r, theme.woodDark.g, theme.woodDark.b, 0.95f)
                : new Color(0.15f, 0.12f, 0.1f, 0.95f);
        }

        // panel_wood_generic spends roughly 72px per side on its frame art and its
        // interior is cream, so content is inset and text must be dark to stay readable.
        bool onWood = panelSprite != null;
        float inset = onWood ? 60f : 20f;
        Color textColor = onWood
            ? (theme != null ? theme.textDark : new Color(0.18f, 0.12f, 0.06f))
            : Color.white;

        unitNameLabel = CreateLabel(panel.transform, "UnitName", 24, FontStyles.Bold, textColor);
        var nameRect = unitNameLabel.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.offsetMin = new Vector2(inset, 0f);
        nameRect.offsetMax = new Vector2(-inset, -inset * 0.45f);
        nameRect.sizeDelta = new Vector2(nameRect.sizeDelta.x, 30f);

        promptLabel = CreateLabel(panel.transform, "Prompt", 17, FontStyles.Italic, textColor);
        var promptRect = promptLabel.rectTransform;
        promptRect.anchorMin = new Vector2(0f, 1f);
        promptRect.anchorMax = new Vector2(1f, 1f);
        promptRect.pivot = new Vector2(0.5f, 1f);
        promptRect.offsetMin = new Vector2(inset, 0f);
        promptRect.offsetMax = new Vector2(-inset, -inset * 0.45f - 32f);
        promptRect.sizeDelta = new Vector2(promptRect.sizeDelta.x, 26f);

        // Action row along the bottom of the panel interior.
        var row = new GameObject("Actions");
        row.transform.SetParent(panel.transform, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 0f);
        rowRect.anchorMax = new Vector2(1f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.offsetMin = new Vector2(inset, inset * 0.5f);
        rowRect.offsetMax = new Vector2(-inset, 0f);
        rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, 52f);

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        // childControlWidth must be on or childForceExpandWidth does nothing and every
        // button collapses to a zero-width point anchor.
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        attackButton = CreateActionButton(row.transform, "AttackButton",
            attackText_Localized.SafeGetLocalizedString(), "Attack", out _);
        attackButton.onClick.AddListener(OnAttackClicked);

        skillButton = CreateActionButton(row.transform, "SkillButton", "", "Skill", out skillLabel);
        skillButton.onClick.AddListener(OnSkillClicked);

        defendButton = CreateActionButton(row.transform, "DefendButton",
            defendText_Localized.SafeGetLocalizedString(), "Defend", out _);
        defendButton.onClick.AddListener(OnDefendClicked);

        cancelTargetButton = CreateActionButton(row.transform, "CancelButton",
            cancelText_Localized.SafeGetLocalizedString(), "Cancel", out _);
        cancelTargetButton.onClick.AddListener(OnCancelTargetClicked);

        panel.SetActive(false);
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string name, float fontSize,
        FontStyles style, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button CreateActionButton(Transform parent, string name, string text,
        string fallbackText, out TextMeshProUGUI label)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();

        var image = obj.AddComponent<Image>();
        Sprite sprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_small_action");
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }
        else
        {
            image.color = theme != null
                ? new Color(theme.woodDark.r, theme.woodDark.g, theme.woodDark.b, 0.9f)
                : new Color(0.25f, 0.22f, 0.2f, 0.9f);
        }

        var button = obj.AddComponent<Button>();

        label = CreateLabel(obj.transform, "Label", 18, FontStyles.Bold,
            sprite != null ? new Color(0.18f, 0.12f, 0.06f) : Color.white);
        label.text = string.IsNullOrEmpty(text) ? fallbackText : text;
        label.enableAutoSizing = true;
        label.fontSizeMin = 11;
        label.fontSizeMax = 18;

        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(4f, 2f);
        labelRect.offsetMax = new Vector2(-4f, -2f);

        return button;
    }

    // ── Binding to the active TurnManager ──────────────────────────────────────

    private void Update()
    {
        // The manager is recreated with each CombatScene load, so rebind when it changes.
        if (boundManager != TurnManager.Instance)
        {
            Unbind();
            boundManager = TurnManager.Instance;
            if (boundManager != null)
            {
                boundManager.OnPlayerTurnStarted += HandlePlayerTurnStarted;
                boundManager.OnPlayerTurnEnded += HandlePlayerTurnEnded;
            }
        }

        if (awaitingTargetFor.HasValue)
            PollForTargetClick();
    }

    private void Unbind()
    {
        if (boundManager == null) return;
        boundManager.OnPlayerTurnStarted -= HandlePlayerTurnStarted;
        boundManager.OnPlayerTurnEnded -= HandlePlayerTurnEnded;
        boundManager = null;
    }

    private void HandlePlayerTurnStarted(CombatUnit unit)
    {
        commandingUnit = unit;
        awaitingTargetFor = null;
        ShowActionChoices();
        panel.SetActive(true);
    }

    private void HandlePlayerTurnEnded()
    {
        commandingUnit = null;
        awaitingTargetFor = null;
        panel.SetActive(false);
        TargetHighlighter.ClearAll();
    }

    // ── Action selection ───────────────────────────────────────────────────────

    private void ShowActionChoices()
    {
        if (commandingUnit != null)
            unitNameLabel.text = commandingUnit.unitName;

        string prompt = choosePromptText_Localized.SafeGetLocalizedString();
        promptLabel.text = string.IsNullOrEmpty(prompt) ? "Choose an action" : prompt;

        RefreshSkillButton();

        attackButton.gameObject.SetActive(true);
        skillButton.gameObject.SetActive(true);
        defendButton.gameObject.SetActive(true);
        cancelTargetButton.gameObject.SetActive(false);
    }

    private void RefreshSkillButton()
    {
        AnimalSkill skill = commandingUnit != null ? commandingUnit.GetPlayerActiveSkill() : null;

        if (skill == null)
        {
            // Nothing to show — most animals without an assigned active skill.
            skillButton.interactable = false;
            skillLabel.text = "—";
            return;
        }

        bool ready = commandingUnit.GetReadySkill() != null;
        skillButton.interactable = ready;

        if (ready)
        {
            skillLabel.text = skill.skillName;
        }
        else
        {
            int turns = commandingUnit.GetSkillCooldownRemaining();
            skillCooldownText_Localized.Arguments = new object[] { skill.skillName, turns };
            string text = skillCooldownText_Localized.SafeGetLocalizedString();
            skillLabel.text = string.IsNullOrEmpty(text) ? $"{skill.skillName} [{turns}]" : text;
        }
    }

    private void OnAttackClicked() => BeginTargetSelection(TurnManager.PlayerActionType.Attack);

    private void OnSkillClicked() => BeginTargetSelection(TurnManager.PlayerActionType.Skill);

    private void OnDefendClicked()
    {
        if (boundManager == null) return;
        boundManager.SubmitDefend();
    }

    private void OnCancelTargetClicked()
    {
        awaitingTargetFor = null;
        TargetHighlighter.ClearAll();
        ShowActionChoices();
    }

    // ── Target selection ───────────────────────────────────────────────────────

    private void BeginTargetSelection(TurnManager.PlayerActionType actionType)
    {
        if (boundManager == null || commandingUnit == null) return;

        List<CombatUnit> targets = GetSelectableTargets();
        if (targets.Count == 0) return;

        // Only one possible target — picking it for the player saves a pointless click.
        if (targets.Count == 1)
        {
            SubmitWithTarget(actionType, targets[0]);
            return;
        }

        awaitingTargetFor = actionType;

        string prompt = targetPromptText_Localized.SafeGetLocalizedString();
        promptLabel.text = string.IsNullOrEmpty(prompt) ? "Click an enemy to target" : prompt;

        attackButton.gameObject.SetActive(false);
        skillButton.gameObject.SetActive(false);
        defendButton.gameObject.SetActive(false);
        cancelTargetButton.gameObject.SetActive(true);

        TargetHighlighter.HighlightAll(targets);
    }

    private List<CombatUnit> GetSelectableTargets()
    {
        var result = new List<CombatUnit>();
        if (boundManager == null) return result;

        foreach (CombatUnit unit in boundManager.GetEnemyUnits())
            if (unit != null && unit.IsAlive())
                result.Add(unit);

        return result;
    }

    private void PollForTargetClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;

        CombatUnit clicked = FindUnitAt(world);
        if (clicked == null) return;

        SubmitWithTarget(awaitingTargetFor.Value, clicked);
    }

    /// <summary>
    /// Hit-test the selectable targets against a world point using their renderer bounds.
    /// Bounds rather than colliders: combat units have no Collider2D, and adding them
    /// just for targeting risks interfering with existing physics.
    /// </summary>
    private CombatUnit FindUnitAt(Vector3 worldPoint)
    {
        CombatUnit best = null;
        float bestDistance = float.MaxValue;

        foreach (CombatUnit unit in GetSelectableTargets())
        {
            var renderer = unit.GetComponentInChildren<Renderer>();
            if (renderer == null) continue;

            Bounds bounds = renderer.bounds;
            if (worldPoint.x < bounds.min.x || worldPoint.x > bounds.max.x) continue;
            if (worldPoint.y < bounds.min.y || worldPoint.y > bounds.max.y) continue;

            // Overlapping sprites: prefer whichever centre is closest to the click.
            float distance = Vector2.Distance(worldPoint, bounds.center);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = unit;
            }
        }

        return best;
    }

    private void SubmitWithTarget(TurnManager.PlayerActionType actionType, CombatUnit target)
    {
        if (boundManager == null) return;

        bool accepted = actionType == TurnManager.PlayerActionType.Skill
            ? boundManager.SubmitSkill(target)
            : boundManager.SubmitAttack(target);

        if (accepted)
        {
            awaitingTargetFor = null;
            TargetHighlighter.ClearAll();
            return;
        }

        // Rejected (skill went on cooldown, turn already submitted) — return to the
        // action list rather than leaving the player clicking a dead panel.
        OnCancelTargetClicked();
    }
}

}
