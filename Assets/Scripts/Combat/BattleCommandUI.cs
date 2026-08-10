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
    private TextMeshProUGUI attackLabel;
    private Button skillButton;
    private TextMeshProUGUI skillLabel;
    private Image skillIconImage;

    // Skill-icon geometry inside the Skill button. The inset clears the button
    // sprite's own frame art — at 4f the icon sat visually outside the raised
    // face, on the bevel, and read as a stray mark next to the button.
    private const float IconSize  = 40f;
    private const float IconInset = 12f;
    private Button defendButton;
    private TextMeshProUGUI defendLabel;
    private Button cancelTargetButton;
    private TextMeshProUGUI cancelLabel;

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
        panelRect.anchoredPosition = new Vector2(0f, 100f);
        // 316 tall, not 190: panel_wood_generic spends ~72px per side on frame art, so a
        // 190-tall panel leaves only ~46px of interior and the action row spilled over the
        // bottom frame. 316 leaves ~172px for the name, prompt and the 68px button row.
        // Widened to 700 so four captions fit on one line each once the Skill button
        // gives up horizontal room to its icon.
        panelRect.sizeDelta = new Vector2(700f, 316f);

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
        // Measured, not guessed: the frame art is ~72px per side at this panel width.
        float inset = onWood ? 72f : 20f;
        Color textColor = onWood
            ? (theme != null ? theme.textDark : new Color(0.18f, 0.12f, 0.06f))
            : Color.white;

        // Stack inside the interior, top-down: name, prompt, then the action row pinned
        // above the bottom frame. Heights are explicit so the total is verifiable against
        // the panel height rather than emerging from fractions of the inset.
        const float nameHeight = 32f;
        const float promptHeight = 28f;
        // 68, not 52: the Skill button carries an icon beside its caption, and at
        // 52 the icon shrank to ~26px (unreadable) while the caption wrapped onto
        // a second line and spilled out of the button.
        const float rowHeight = 68f;

        unitNameLabel = CreateLabel(panel.transform, "UnitName", 24, FontStyles.Bold, textColor);
        var nameRect = unitNameLabel.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.offsetMin = new Vector2(inset, -inset - nameHeight);
        nameRect.offsetMax = new Vector2(-inset, -inset);

        promptLabel = CreateLabel(panel.transform, "Prompt", 17, FontStyles.Italic, textColor);
        var promptRect = promptLabel.rectTransform;
        promptRect.anchorMin = new Vector2(0f, 1f);
        promptRect.anchorMax = new Vector2(1f, 1f);
        promptRect.pivot = new Vector2(0.5f, 1f);
        promptRect.offsetMin = new Vector2(inset, -inset - nameHeight - promptHeight);
        promptRect.offsetMax = new Vector2(-inset, -inset - nameHeight);

        // Action row sits just above the bottom frame art.
        var row = new GameObject("Actions");
        row.transform.SetParent(panel.transform, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 0f);
        rowRect.anchorMax = new Vector2(1f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.offsetMin = new Vector2(inset, inset);
        rowRect.offsetMax = new Vector2(-inset, inset + rowHeight);

        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        // childControlWidth must be on or childForceExpandWidth does nothing and every
        // button collapses to a zero-width point anchor.
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        // Labels are (re)filled in RefreshStaticLabels every time the panel opens, not
        // here: Awake runs before the localization tables finish loading, so anything
        // resolved at build time comes back as the English fallback and never updates.
        attackButton = CreateActionButton(row.transform, "AttackButton", "", "Attack", out attackLabel);
        attackButton.onClick.AddListener(OnAttackClicked);

        skillButton = CreateActionButton(row.transform, "SkillButton", "", "Skill", out skillLabel);
        skillButton.onClick.AddListener(OnSkillClicked);
        skillIconImage = CreateSkillIcon(skillButton.transform, skillLabel);

        defendButton = CreateActionButton(row.transform, "DefendButton", "", "Defend", out defendLabel);
        defendButton.onClick.AddListener(OnDefendClicked);

        cancelTargetButton = CreateActionButton(row.transform, "CancelButton", "", "Back", out cancelLabel);
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
        // Skill names are two words ("Precise Peck", "Supporter's Blessing"). Without
        // this they wrap onto a second line that overflows the button; auto-sizing
        // shrinks the text to fit on one line instead.
        label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;

        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(4f, 2f);
        labelRect.offsetMax = new Vector2(-4f, -2f);

        return button;
    }

    /// <summary>
    /// Square icon pinned to the left inside the Skill button, with the label
    /// inset to make room. Hidden whenever the active skill has no icon, so a
    /// skill still missing art shows the plain text button it always had.
    /// </summary>
    private Image CreateSkillIcon(Transform parent, TextMeshProUGUI label)
    {
        var obj = new GameObject("Icon");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        // Anchored to the left edge and stretched vertically, so the icon scales
        // with the button height instead of needing a hard-coded pixel size.
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot     = new Vector2(0f, 0.5f);

        float inset = SkillIconLeftInset(parent);
        rect.offsetMin = new Vector2(inset, IconInset);
        rect.offsetMax = new Vector2(inset + IconSize, -IconInset);

        var img = obj.AddComponent<Image>();
        img.preserveAspect = true;   // the source art is not square
        img.raycastTarget  = false;  // must not steal the button's clicks
        img.enabled        = false;  // no skill selected yet

        // Push the caption clear of the icon.
        var labelRect = label.rectTransform;
        labelRect.offsetMin = new Vector2(inset + IconSize + 4f, labelRect.offsetMin.y);

        return img;
    }

    /// <summary>
    /// Distance from the button's left edge to where its art actually starts.
    /// button_small_action is a 480px-wide sprite whose drawn face begins ~58px
    /// in, so a fixed inset puts the icon on the transparent margin outside the
    /// visible button. Derived from the sprite so it survives an art change.
    /// </summary>
    private float SkillIconLeftInset(Transform button)
    {
        float fallback = IconInset;

        var bimg = button.GetComponent<Image>();
        if (bimg == null || bimg.sprite == null) return fallback;

        var sprite = bimg.sprite;
        float declaredWidth = sprite.rect.width;
        if (declaredWidth <= 0f) return fallback;

        // Transparent padding on the left, in sprite pixels...
        float padPx = sprite.textureRect.x - sprite.rect.x;
        if (padPx <= 0f) return fallback;

        // ...scaled into the button's own width, plus a small breathing gap.
        var rect = button.GetComponent<RectTransform>();
        float scale = rect.rect.width / declaredWidth;
        return padPx * scale + IconInset;
    }

    // Captions are resolved when the panel opens, which covers the normal case. This
    // additionally catches a language change while the panel is already on screen —
    // possible because the battle is frozen waiting for a command.
    private void OnLocaleChanged(UnityEngine.Localization.Locale _)
    {
        if (panel == null || !panel.activeSelf) return;
        RefreshStaticLabels();
        RefreshSkillButton();
    }

    private void OnEnable()
    {
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        // Leaving the combat scene must close the panel. Unbind() alone is not enough:
        // it only runs when the bound manager changes, and Update may not get a frame
        // between the battle ending and the scene swap.
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDisable()
    {
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene from,
                                      UnityEngine.SceneManagement.Scene to)
    {
        Unbind();
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

        // Outside a battle the panel must never be on screen. This is the same
        // per-frame guard BattleHudOverlay and ConsumableBattleUI already had, and
        // its absence here is why this panel was the one that leaked out of combat.
        if (boundManager == null || !boundManager.combatActive)
        {
            if (panel != null && panel.activeSelf) HidePanel();
            return;
        }

        if (awaitingTargetFor.HasValue)
            PollForTargetClick();
    }

    private void Unbind()
    {
        // Hide before the null check: the manager can be destroyed (battle won, or the
        // scene changed) while the panel is still open, and that path never fires
        // OnPlayerTurnEnded. This object is DontDestroyOnLoad, so a panel left visible
        // then follows the player out of combat and sits on top of the team assembler
        // and the main menu, still showing the last unit's actions.
        HidePanel();

        if (boundManager == null) return;
        boundManager.OnPlayerTurnStarted -= HandlePlayerTurnStarted;
        boundManager.OnPlayerTurnEnded -= HandlePlayerTurnEnded;
        boundManager = null;
    }

    /// <summary>
    /// Clears the command panel and any leftover targeting state. Safe to call at any
    /// time, including before BuildUI has run.
    /// </summary>
    private void HidePanel()
    {
        commandingUnit = null;
        awaitingTargetFor = null;
        if (panel != null) panel.SetActive(false);
        TargetHighlighter.ClearAll();
    }

    private void HandlePlayerTurnStarted(CombatUnit unit)
    {
        commandingUnit = unit;
        awaitingTargetFor = null;
        ShowActionChoices();
        panel.SetActive(true);

        // Tell the manager the panel is up, which switches its wait from a short failsafe
        // countdown to an indefinite one. Reported last: if anything above threw, the
        // countdown should still be running, because that is exactly the case it guards.
        boundManager?.NotifyCommandUiReady();
    }

    private void HandlePlayerTurnEnded()
    {
        commandingUnit = null;
        awaitingTargetFor = null;
        panel.SetActive(false);
        TargetHighlighter.ClearAll();
    }

    // ── Action selection ───────────────────────────────────────────────────────

    /// <summary>
    /// Re-resolve the fixed button captions. Called every time the panel opens because
    /// Awake runs before the localization tables are ready — resolving once at build
    /// time leaves the English fallback baked in for the whole session.
    /// </summary>
    private void RefreshStaticLabels()
    {
        SetLocalized(attackLabel, attackText_Localized, "Attack");
        SetLocalized(defendLabel, defendText_Localized, "Defend");
        SetLocalized(cancelLabel, cancelText_Localized, "Back");
    }

    private static void SetLocalized(TextMeshProUGUI label, LocalizedString source, string fallback)
    {
        if (label == null) return;
        string text = source.SafeGetLocalizedString();
        label.text = string.IsNullOrEmpty(text) ? fallback : text;
    }

    private void ShowActionChoices()
    {
        if (commandingUnit != null)
            unitNameLabel.text = commandingUnit.unitName;

        string prompt = choosePromptText_Localized.SafeGetLocalizedString();
        promptLabel.text = string.IsNullOrEmpty(prompt) ? "Choose an action" : prompt;

        RefreshStaticLabels();
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
            SetSkillIcon(null, false);
            return;
        }

        bool ready = commandingUnit.GetReadySkill() != null;
        skillButton.interactable = ready;

        // Dim the icon on cooldown so the button reads as unavailable at a glance,
        // the same signal the greyed-out caption gives.
        SetSkillIcon(skill.skillIcon, ready);

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

    /// <summary>
    /// Shows the icon when the skill has one; falls back to the plain text-only
    /// button otherwise, including the label inset, so skills still awaiting art
    /// do not leave an empty gap.
    /// </summary>
    private void SetSkillIcon(Sprite icon, bool ready)
    {
        if (skillIconImage == null) return;

        bool show = icon != null;
        skillIconImage.enabled = show;
        skillIconImage.sprite  = icon;
        skillIconImage.color   = ready ? Color.white : new Color(1f, 1f, 1f, 0.45f);

        // Recomputed here, not just at construction: the button has no width during
        // Awake (the HorizontalLayoutGroup sizes it on the first layout pass), so an
        // inset derived from it back then would have been based on zero.
        float inset = SkillIconLeftInset(skillButton.transform);
        var iconRect = skillIconImage.rectTransform;
        iconRect.offsetMin = new Vector2(inset, IconInset);
        iconRect.offsetMax = new Vector2(inset + IconSize, -IconInset);

        float left = show ? inset + IconSize + 4f : 4f;
        var labelRect = skillLabel.rectTransform;
        labelRect.offsetMin = new Vector2(left, labelRect.offsetMin.y);
        // The caption also has to clear the button frame on the right, or a long
        // skill name runs out past the bevel.
        labelRect.offsetMax = new Vector2(-inset, labelRect.offsetMax.y);
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
