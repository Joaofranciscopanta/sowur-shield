using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
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

        // Upgrade the builder's flat-color window to the shared sprite kit.
        // Tab buttons are left alone — ShowTab drives them via Button.colors tint.
        UIThemeStyler.StylePanel(questsPanel, theme);
        UIThemeStyler.StyleButton(closeButton, theme, UIThemeStyler.ButtonSmallPath);

        // StylePanel just swapped the builder's cream background for the wood sprite, which
        // inverts what the panel-level text needs. QuestsUIBuilder painted the heading and both
        // empty-state labels textDark to suit that cream: on wood they measure 1.68 / 2.44 / 3.23
        // (dark / mid / light), i.e. invisible on the darkest tone and failing on all three.
        //
        // Cream for the same reason StylePanelTitle uses it rather than gold — the tone under a
        // panel sprite isn't fixed, and cream holds across the range (7.60 / 5.24 / 3.96) where
        // gold drops to 3.01 on woodLight.
        //
        // The quest rows are deliberately left alone: their prefabs carry their own white
        // backing, so their dark text is already reading 5.78–7.36 against the row, not the wood.
        UIThemeStyler.StylePanelTitle(questsPanel, theme);
        UIThemeStyler.TintText(activeEmptyText, theme != null ? theme.backgroundCream : Color.white);
        UIThemeStyler.TintText(completedEmptyText, theme != null ? theme.backgroundCream : Color.white);

        if (questsPanel != null) questsPanel.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(CloseQuests);
        if (activeTabButton != null) activeTabButton.onClick.AddListener(OnActiveTabClicked);
        if (completedTabButton != null) completedTabButton.onClick.AddListener(OnCompletedTabClicked);

        if (UIManager.Instance != null)
            UIManager.Instance.RegisterWindow(this);
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
