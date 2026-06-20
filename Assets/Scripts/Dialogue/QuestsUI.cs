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
    private bool _dirty = true;

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
        _dirty = false;
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
        if (IsWindowOpen) RefreshActiveTab();
        else _dirty = true;
    }

    private void OnObjectiveUpdated(QuestData quest, int objIndex, int newCount)
    {
        if (IsWindowOpen) RefreshActiveTab();
        else _dirty = true;
    }

    private void OnQuestCompleted(QuestData quest)
    {
        if (IsWindowOpen)
        {
            RefreshActiveTab();
            RefreshCompletedTab();
        }
        else
        {
            _dirty = true;
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

        if (selectedButton != null)
        {
            var c = selectedButton.colors;
            c.normalColor = selectedColor;
            selectedButton.colors = c;
        }
        if (otherButton != null)
        {
            var c = otherButton.colors;
            c.normalColor = otherColor;
            otherButton.colors = c;
        }
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

// =============================================================================
// QuestActiveRow component
// =============================================================================

/// <summary>
/// Attach to the active-quest row prefab. Wire all fields in the Inspector.
/// Objective sub-rows are built dynamically since QuestData.objectives is unbounded.
/// </summary>
public class QuestActiveRow : MonoBehaviour
{
    [Header("Wire these in the Prefab Inspector")]
    [SerializeField] public TextMeshProUGUI titleText;
    [SerializeField] public TextMeshProUGUI descriptionText;
    [SerializeField] public Image progressBar;
    [SerializeField] public Transform objectiveContainer;
    [SerializeField] public GameObject objectiveLinePrefab;

    private readonly List<GameObject> _objectiveLines = new List<GameObject>();

    public void Populate(QuestData data, UITheme theme)
    {
        if (titleText != null) titleText.text = data.questTitle;
        if (descriptionText != null) descriptionText.text = data.questDescription;

        float progress = QuestManager.Instance.GetQuestProgress(data.questId);
        if (progressBar != null)
        {
            progressBar.fillAmount = progress;
            Color positive = theme != null ? theme.positive : new Color(0.5f, 0.78f, 0.52f);
            Color inProgress = theme != null ? theme.highlightGold : new Color(0.96f, 0.83f, 0.37f);
            progressBar.color = progress >= 0.99f ? positive : inProgress;
        }

        foreach (var line in _objectiveLines)
            if (line != null) Destroy(line);
        _objectiveLines.Clear();

        if (objectiveContainer == null || objectiveLinePrefab == null || data.objectives == null)
            return;

        for (int i = 0; i < data.objectives.Count; i++)
        {
            var obj = data.objectives[i];
            int objProgress = QuestManager.Instance.GetObjectiveProgress(data.questId, i);

            GameObject lineGO = Instantiate(objectiveLinePrefab, objectiveContainer);
            _objectiveLines.Add(lineGO);

            var lineText = lineGO.GetComponent<TextMeshProUGUI>();
            if (lineText == null) continue;

            bool done = objProgress >= obj.requiredCount;
            string prefix = done ? "✓ " : "• ";
            lineText.text = obj.requiredCount > 1
                ? $"{prefix}{obj.description} ({objProgress}/{obj.requiredCount})"
                : $"{prefix}{obj.description}";
            lineText.color = done
                ? (theme != null ? theme.positive : new Color(0.5f, 0.78f, 0.52f))
                : (theme != null ? theme.textDark : Color.white);
        }
    }
}

// =============================================================================
// QuestCompletedRow component
// =============================================================================

/// <summary>Attach to the completed-quest row prefab — read-only journal entry.</summary>
public class QuestCompletedRow : MonoBehaviour
{
    [Header("Wire these in the Prefab Inspector")]
    [SerializeField] public TextMeshProUGUI titleText;
    [SerializeField] public TextMeshProUGUI descriptionText;

    public void Populate(QuestData data)
    {
        if (titleText != null) titleText.text = data.questTitle;
        if (descriptionText != null) descriptionText.text = data.questDescription;
    }
}

} // namespace SowurShield.Dialogue
