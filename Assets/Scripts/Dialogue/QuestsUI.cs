using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization;
using SowurShield.Core;
using SowurShield.UI;

namespace SowurShield.Dialogue
{

/// <summary>
/// Expanded quest log window with two tabs: Active (every in-progress quest, all objectives)
/// and Completed (read-only journal of finished quests). Complements QuestTrackerUI, the
/// always-on corner HUD that only shows the single most recent active quest.
///
/// SETUP IN UNITY:
///   Built via Tools > Sowur Shield > Rebuild Quests UI (see QuestsUIBuilder.cs).
/// </summary>
public class QuestsUI : MonoBehaviour, IUIWindow
{
    [Header("Panel")]
    [SerializeField] private GameObject questsPanel;
    [SerializeField] private Button closeButton;

    [Header("Hotkey")]
    [Tooltip("Pressing this key toggles the Quests window open/closed (ignored while another window is blocking it).")]
    [SerializeField] private KeyCode toggleKey = KeyCode.J;

    [Header("Tabs")]
    [SerializeField] private Button activeTabButton;
    [SerializeField] private Button completedTabButton;
    [SerializeField] private GameObject activeTabPanel;
    [SerializeField] private GameObject completedTabPanel;

    [Header("Active Tab")]
    [SerializeField] private Transform activeListContainer;
    [SerializeField] private GameObject activeQuestRowPrefab;
    [SerializeField] private TextMeshProUGUI activeEmptyText;

    [Header("Completed Tab")]
    [SerializeField] private Transform completedListContainer;
    [SerializeField] private GameObject completedQuestRowPrefab;
    [SerializeField] private TextMeshProUGUI completedEmptyText;

    [Header("Theme")]
    [SerializeField] private UITheme theme;

    // The panel chrome shipped hardcoded in English ("Quests" / "Active" / "Completed" /
    // the two empty-state lines) while every quest's own title and description was already
    // localized — so a Portuguese player saw translated quests inside an English window.
    // Resolved by name from the builder-made hierarchy rather than by new Inspector fields,
    // so no scene re-wiring is needed.
    private TextMeshProUGUI titleTextRef;
    private TextMeshProUGUI activeTabLabel;
    private TextMeshProUGUI completedTabLabel;

    private LocalizedString titleText;
    private LocalizedString tabActiveText;
    private LocalizedString tabCompletedText;
    private LocalizedString emptyActiveText;
    private LocalizedString emptyCompletedText;
    private LocalizedString closeText;
    private TextMeshProUGUI closeLabel;

    private readonly List<GameObject> _activeRows = new List<GameObject>();
    private readonly List<GameObject> _completedRows = new List<GameObject>();

    // =========================================================================
    // IUIWindow
    // =========================================================================

    public string WindowName => "Quests";
    public int WindowPriority => SowurShield.Core.WindowPriority.Inventory;
    public bool IsWindowOpen => questsPanel != null && questsPanel.activeSelf;
    public bool CanCloseWithEsc => true;

    public void OpenWindow()
    {
        // Awake runs before the localization tables finish loading, so the chrome text applied
        // there falls back to the builder's English. Re-apply on open, when the tables are up.
        ApplyChromeText();

        if (questsPanel != null) questsPanel.SetActive(true);
        ShowTab(activeTabPanel, completedTabPanel, activeTabButton, completedTabButton);
        RefreshActiveTab();
        RefreshCompletedTab();
        DisablePlayerMovement();
    }

    public void CloseWindow()
    {
        if (questsPanel != null) questsPanel.SetActive(false);
        EnablePlayerMovement();
    }

    public void OnWindowBlocked(string blockedBy)
    {
        Debug.LogWarning($"[QuestsUI] Blocked by '{blockedBy}'");
    }

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        if (theme == null)
            theme = Resources.Load<UITheme>("UI/CozyUITheme");

        ResolveChromeLabels();
        WireLocalizedStrings();

        // Upgrade the builder's flat-color window to the shared sprite kit.
        // Tab buttons are left alone — ShowTab drives them via Button.colors tint.
        UIThemeStyler.StylePanel(questsPanel, theme);
        UIThemeStyler.StyleButton(closeButton, theme, UIThemeStyler.ButtonSmallPath);

        // Applies the localized chrome text and, from it, the close button's size and place.
        ApplyChromeText();

        // panel_wood_generic's wood is only the BORDER — its interior is cream. An earlier pass
        // read StylePanel as turning the whole panel to wood and tinted this text cream, which
        // put cream on cream: measured in Play Mode the title and both empty-state labels were
        // invisible, and the window read as completely blank.
        //
        // Measured insets confirm where each label actually sits: the title is 108px from the
        // panel top and the empty-state text 428px, both well inside the ~90px frame art, i.e.
        // on the cream field. So they take textDark (12.46:1) like every other label on cream.
        // StylePanelTitle stays the right helper for panels whose heading really is on the
        // border; this one's is not.
        //
        // The quest rows are deliberately left alone: their prefabs carry their own white
        // backing, so their dark text is already reading 5.78-7.36 against the row, not the wood.
        Color onCream = theme != null ? theme.textDark : Color.black;
        UIThemeStyler.TintText(titleTextRef, onCream);
        UIThemeStyler.TintText(activeEmptyText, onCream);
        UIThemeStyler.TintText(completedEmptyText, onCream);

        if (questsPanel != null) questsPanel.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(CloseQuests);
        if (activeTabButton != null) activeTabButton.onClick.AddListener(OnActiveTabClicked);
        if (completedTabButton != null) completedTabButton.onClick.AddListener(OnCompletedTabClicked);

        if (UIManager.Instance != null)
            UIManager.Instance.RegisterWindow(this);

        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        ApplyChromeText();
    }

    /// <summary>
    /// Re-place and re-proportion the close button for the themed window.
    ///
    /// Two defects, both measured in Play Mode:
    ///
    /// 1. POSITION. QuestsUIBuilder anchors it 8px from the panel's top-right, correct for the
    ///    flat window it built. StylePanel then wraps the panel in panel_wood_generic, whose
    ///    frame art is far thicker than 8px, so the button sat on the wooden frame — visually
    ///    detached from the window. The inset is derived from the sprite's own border scaled to
    ///    the panel, so it tracks the art rather than hardcoding a guess.
    ///
    /// 2. SIZE. The builder makes a 32x32 square for an "X" glyph, but the label is the word
    ///    "Fechar". button_small_action is 480x96 art (5:1), and CLAUDE.md records that a square
    ///    icon button cannot be built from this kit. At 32x32 the label auto-shrank to 9.8pt and
    ///    still needed 45.6px, overflowing into an unreadable smear. Giving the button the art's
    ///    own proportion lets the word fit at a readable size.
    /// </summary>
    private void InsetCloseButtonInsideFrame()
    {
        if (closeButton == null || questsPanel == null) return;

        var panelRT = questsPanel.transform as RectTransform;
        var btnRT = closeButton.transform as RectTransform;
        if (panelRT == null || btnRT == null) return;

        // Width from the label the button actually carries, not from the art's full 5:1 — a
        // word as short as "Fechar" in a 480x96-proportioned pill is mostly empty plaque. The
        // painted plaque covers ~71% of the rect (see CLAUDE.md), so the text budget is scaled
        // up accordingly, and the result is clamped so it can neither shrink back to the
        // unreadable 32px square nor stretch into a banner.
        const float CloseButtonHeight = 40f;
        const float PlaqueCoverage = 0.71f;
        // preferredWidth is stale until the text has been laid out once — without this the
        // measurement reflects the builder's "X" rather than the localized word just applied.
        if (closeLabel != null) closeLabel.ForceMeshUpdate();
        float labelWidth = closeLabel != null ? closeLabel.preferredWidth : 0f;
        float width = Mathf.Clamp((labelWidth + 24f) / PlaqueCoverage, 96f, 220f);
        btnRT.sizeDelta = new Vector2(width, CloseButtonHeight);

        Sprite frame = Resources.Load<Sprite>(UIThemeStyler.PanelWoodPath);
        if (frame == null || frame.rect.width <= 0f || frame.rect.height <= 0f) return;

        // The visible frame is wider than the 9-slice border: the art fades in over roughly
        // three times the border before the cream field starts.
        const float FrameArtMultiplier = 3f;
        float insetX = frame.border.z * (panelRT.rect.width / frame.rect.width) * FrameArtMultiplier;
        float insetY = frame.border.w * (panelRT.rect.height / frame.rect.height) * FrameArtMultiplier;

        btnRT.anchoredPosition = new Vector2(-insetX, -insetY);
    }

    /// <summary>
    /// Find the builder-made chrome labels by name. QuestsUIBuilder names them TitleText and
    /// gives each tab button a "Text" child; resolving here keeps the scene wiring untouched.
    /// </summary>
    private void ResolveChromeLabels()
    {
        if (questsPanel != null)
        {
            foreach (TextMeshProUGUI t in questsPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.GetComponentInParent<Button>() != null) continue;
                if (t.gameObject.name.IndexOf("Title", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    titleTextRef = t;
                    break;
                }
            }
        }

        if (activeTabButton != null)
            activeTabLabel = activeTabButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (completedTabButton != null)
            completedTabLabel = completedTabButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (closeButton != null)
            closeLabel = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void WireLocalizedStrings()
    {
        titleText          = new LocalizedString("Dialogue", "dialogue.quests.title");
        tabActiveText      = new LocalizedString("Dialogue", "dialogue.quests.tab_active");
        tabCompletedText   = new LocalizedString("Dialogue", "dialogue.quests.tab_completed");
        emptyActiveText    = new LocalizedString("Dialogue", "dialogue.quests.empty_active");
        emptyCompletedText = new LocalizedString("Dialogue", "dialogue.quests.empty_completed");
        // The builder labels this button "X". Every other close control in the game spells the
        // word, and ui_common.close already carries it in all three languages.
        closeText          = new LocalizedString("UI_Common", "ui_common.close");
    }

    /// <summary>
    /// Push the localized chrome strings onto the labels. SafeGetLocalizedString returns an
    /// empty string when the tables are not ready yet, so each assignment is guarded — an
    /// unguarded write would blank the window instead of leaving the builder's English in
    /// place until the language is applied.
    /// </summary>
    private void ApplyChromeText()
    {
        SetIfLocalized(titleTextRef, titleText);
        SetIfLocalized(activeTabLabel, tabActiveText);
        SetIfLocalized(completedTabLabel, tabCompletedText);
        SetIfLocalized(activeEmptyText, emptyActiveText);
        SetIfLocalized(completedEmptyText, emptyCompletedText);
        SetIfLocalized(closeLabel, closeText);

        // The close button's width is derived from its label, so it has to be recomputed
        // whenever that label changes — on open and on a language switch, not just in Awake.
        InsetCloseButtonInsideFrame();
    }

    private static void SetIfLocalized(TextMeshProUGUI label, LocalizedString source)
    {
        if (label == null || source == null) return;
        string value = source.SafeGetLocalizedString();
        if (!string.IsNullOrEmpty(value)) label.text = value;
    }

    private void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted += OnQuestStarted;
            QuestManager.Instance.OnObjectiveUpdated += OnObjectiveUpdated;
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (IsWindowOpen)
                CloseQuests();
            else
                OpenQuests();
        }
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;

        if (UIManager.Instance != null)
            UIManager.Instance.UnregisterWindow(this);

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= OnQuestStarted;
            QuestManager.Instance.OnObjectiveUpdated -= OnObjectiveUpdated;
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
        }
    }

    // =========================================================================
    // Public API
    // =========================================================================

    public void OpenQuests()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TryOpenWindow(this);
        else
            OpenWindow();
    }

    public void CloseQuests()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TryCloseWindow(this);
        else
            CloseWindow();
    }

    // =========================================================================
    // QuestManager event handlers
    // =========================================================================

    private void OnQuestStarted(QuestData quest)
    {
        // Closed windows just rebuild from scratch in OpenWindow() — no need to track
        // dirtiness while invisible.
        if (IsWindowOpen) RefreshActiveTab();
    }

    private void OnObjectiveUpdated(QuestData quest, int objIndex, int newCount)
    {
        if (IsWindowOpen) RefreshActiveTab();
    }

    private void OnQuestCompleted(QuestData quest)
    {
        if (IsWindowOpen)
        {
            RefreshActiveTab();
            RefreshCompletedTab();
        }
    }

    // =========================================================================
    // Tabs
    // =========================================================================

    private void OnActiveTabClicked() => ShowTab(activeTabPanel, completedTabPanel, activeTabButton, completedTabButton);
    private void OnCompletedTabClicked() => ShowTab(completedTabPanel, activeTabPanel, completedTabButton, activeTabButton);

    private void ShowTab(GameObject toShow, GameObject toHide, Button selectedButton, Button otherButton)
    {
        if (toShow != null) toShow.SetActive(true);
        if (toHide != null) toHide.SetActive(false);

        Color selectedColor = theme != null ? theme.highlightGold : new Color(1f, 0.8f, 0.3f);
        Color otherColor = theme != null ? theme.backgroundTan : new Color(0.5f, 0.5f, 0.5f);

        // Both tab tints are LIGHT (gold / tan), but QuestsUIBuilder paints the tab labels
        // white — measured 1.5:1 on gold and 1.3:1 on tan, i.e. effectively invisible. The
        // label has to be darkened alongside the tint, or the button reads as blank.
        Color tabLabel = theme != null ? theme.textDark : Color.black;

        if (selectedButton != null)
        {
            var c = selectedButton.colors;
            c.normalColor = selectedColor;
            selectedButton.colors = c;
            TintTabLabel(selectedButton, tabLabel);
        }
        if (otherButton != null)
        {
            var c = otherButton.colors;
            c.normalColor = otherColor;
            otherButton.colors = c;
            TintTabLabel(otherButton, tabLabel);
        }
    }

    private void TintTabLabel(Button button, Color color)
    {
        var label = button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (label != null) label.color = color;
    }

    // =========================================================================
    // Active tab — rebuild
    // =========================================================================

    private void RefreshActiveTab()
    {
        ClearRows(_activeRows);
        if (activeListContainer == null || activeQuestRowPrefab == null) return;

        var activeIds = new List<string>(QuestManager.Instance != null
            ? QuestManager.Instance.GetActiveQuestIds()
            : new List<string>());

        if (activeEmptyText != null)
            activeEmptyText.gameObject.SetActive(activeIds.Count == 0);

        foreach (string questId in activeIds)
        {
            QuestData data = QuestManager.Instance.GetQuestData(questId);
            if (data == null) continue;

            GameObject rowGO = Instantiate(activeQuestRowPrefab, activeListContainer);
            _activeRows.Add(rowGO);
            PopulateActiveRow(rowGO, data);
        }
    }

    private void PopulateActiveRow(GameObject rowGO, QuestData data)
    {
        var row = rowGO.GetComponent<QuestActiveRow>();
        if (row == null) row = rowGO.AddComponent<QuestActiveRow>();
        row.Populate(data, theme);
    }

    // =========================================================================
    // Completed tab — rebuild
    // =========================================================================

    private void RefreshCompletedTab()
    {
        ClearRows(_completedRows);
        if (completedListContainer == null || completedQuestRowPrefab == null) return;

        var completedIds = new List<string>(QuestManager.Instance != null
            ? QuestManager.Instance.GetCompletedQuestIds()
            : new List<string>());

        if (completedEmptyText != null)
            completedEmptyText.gameObject.SetActive(completedIds.Count == 0);

        foreach (string questId in completedIds)
        {
            QuestData data = QuestManager.Instance.GetQuestData(questId);
            if (data == null) continue;

            GameObject rowGO = Instantiate(completedQuestRowPrefab, completedListContainer);
            _completedRows.Add(rowGO);

            var row = rowGO.GetComponent<QuestCompletedRow>();
            if (row == null) row = rowGO.AddComponent<QuestCompletedRow>();
            row.Populate(data);
        }
    }

    private void ClearRows(List<GameObject> rows)
    {
        foreach (var row in rows)
            if (row != null) Destroy(row);
        rows.Clear();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private void DisablePlayerMovement()
    {
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        player?.DisableMovement();
    }

    private void EnablePlayerMovement()
    {
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        player?.EnableMovement();
    }
}

} // namespace SowurShield.Dialogue
