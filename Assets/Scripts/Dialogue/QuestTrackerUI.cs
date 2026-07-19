using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.Core;

namespace SowurShield.Dialogue
{

/// <summary>
/// Corner HUD showing the most recently started active quest + its first incomplete objective.
/// Appears when a quest starts, hides when no quests are active.
///
/// SETUP IN UNITY:
///   - Add to a UI Canvas (can be the same canvas as your HUD elements)
///   - Assign trackerPanel, questTitleText, objectiveText, progressBar in Inspector
///   - The panel auto-shows/hides based on active quests
/// </summary>
public class QuestTrackerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject trackerPanel;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private Image progressBar;

    private QuestData _trackedQuest;

    private void Start()
    {
        if (trackerPanel != null)
            trackerPanel.SetActive(false);

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted     += OnQuestStarted;
            QuestManager.Instance.OnObjectiveUpdated += OnObjectiveUpdated;
            QuestManager.Instance.OnQuestCompleted   += OnQuestCompleted;
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted     -= OnQuestStarted;
            QuestManager.Instance.OnObjectiveUpdated -= OnObjectiveUpdated;
            QuestManager.Instance.OnQuestCompleted   -= OnQuestCompleted;
        }
    }

    // =========================================================================
    // Event Handlers
    // =========================================================================

    private void OnQuestStarted(QuestData quest)
    {
        _trackedQuest = quest; // Track the most recent quest
        Refresh();
    }

    private void OnObjectiveUpdated(QuestData quest, int objIndex, int newCount)
    {
        if (_trackedQuest == null || quest.questId == _trackedQuest.questId)
            Refresh();
    }

    private void OnQuestCompleted(QuestData quest)
    {
        if (_trackedQuest != null && quest.questId == _trackedQuest.questId)
        {
            // Try to switch to another active quest
            _trackedQuest = null;
            foreach (string id in QuestManager.Instance.GetActiveQuestIds())
            {
                _trackedQuest = QuestManager.Instance.GetQuestData(id);
                break;
            }
        }
        Refresh();
    }

    // =========================================================================
    // Display
    // =========================================================================

    private void Refresh()
    {
        bool hasQuest = _trackedQuest != null &&
                        QuestManager.Instance != null &&
                        QuestManager.Instance.IsQuestActive(_trackedQuest.questId);

        if (trackerPanel != null)
            trackerPanel.SetActive(hasQuest);

        if (!hasQuest) return;

        if (questTitleText != null)
            questTitleText.text = _trackedQuest.questTitle.SafeGetLocalizedString();

        // Find first incomplete objective
        string objLine = "";
        if (_trackedQuest.objectives != null)
        {
            for (int i = 0; i < _trackedQuest.objectives.Count; i++)
            {
                var obj = _trackedQuest.objectives[i];
                int progress = QuestManager.Instance.GetObjectiveProgress(_trackedQuest.questId, i);
                if (progress < obj.requiredCount)
                {
                    string objDescText = obj.description.SafeGetLocalizedString();
                    if (obj.requiredCount > 1)
                        objLine = $"{objDescText} ({progress}/{obj.requiredCount})";
                    else
                        objLine = objDescText;
                    break;
                }
            }
        }

        if (objectiveText != null)
            objectiveText.text = objLine;

        if (progressBar != null)
            progressBar.fillAmount = QuestManager.Instance.GetQuestProgress(_trackedQuest.questId);
    }
}

} // namespace SowurShield.Dialogue
