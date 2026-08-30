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

            AdoptActiveQuest();
        }

        // After adoption, not from OnEnable: OnEnable runs before Start, so the coroutine
        // would wait on localization and then refresh a tracker that had no quest yet.
        // Switching language mid-game must re-resolve the two labels as well.
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale _) => Refresh();

    /// <summary>
    /// Retries the labels until the string tables actually answer.
    /// </summary>
    /// <remarks>
    /// Waiting on LocalizationSettings.InitializationOperation is not sufficient: it can
    /// report IsDone on the first frame, before the tables are fetched, so a refresh driven
    /// off it still writes empty strings and nothing ever corrects them. Retrying while the
    /// title is blank is the version that actually works, and it costs one string compare per
    /// frame only until the first successful resolve.
    /// </remarks>
    private void Update()
    {
        if (_trackedQuest == null) return;
        if (questTitleText == null || !string.IsNullOrEmpty(questTitleText.text)) return;

        Refresh();
    }

    /// <summary>
    /// Picks up a quest that is already running, so the tracker is not blank when it starts
    /// after the quest does.
    /// </summary>
    /// <remarks>
    /// The panel is only ever shown from OnQuestStarted, so a quest begun before this
    /// component subscribed was invisible. That is the normal case for a new game: SaveManager
    /// opens the first quest during its own Start(), which can run before this one. It also
    /// covers a loaded save, where quests are restored rather than started.
    /// </remarks>
    private void AdoptActiveQuest()
    {
        if (_trackedQuest != null) return;

        var ids = QuestManager.Instance.GetActiveQuestIds();
        if (ids == null) return;

        foreach (string id in ids)
        {
            QuestData quest = QuestManager.Instance.GetQuestData(id);
            if (quest == null) continue;

            _trackedQuest = quest;
            Refresh();
            return;
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
        bool wasTracked = _trackedQuest != null && quest.questId == _trackedQuest.questId;

        if (wasTracked)
        {
            // Try to switch to another active quest
            _trackedQuest = null;
            foreach (string id in QuestManager.Instance.GetActiveQuestIds())
            {
                _trackedQuest = QuestManager.Instance.GetQuestData(id);
                break;
            }
        }

        // Acknowledge the finish before the panel changes. Completing a quest was the one
        // payoff moment in the loop with no feedback at all -- the tracker simply vanished,
        // or silently swapped to the next objective, and the only sign anything had happened
        // was the money going up.
        if (wasTracked && isActiveAndEnabled)
            StartCoroutine(CelebrateThenRefresh());
        else
            Refresh();
    }

    /// <summary>
    /// A short flourish on the panel, then the normal refresh.
    /// </summary>
    private System.Collections.IEnumerator CelebrateThenRefresh()
    {
        RectTransform panel = trackerPanel != null
            ? trackerPanel.GetComponent<RectTransform>() : null;

        if (panel != null)
        {
            // Fill the bar the rest of the way first: the objective that finished the quest
            // often lands the same frame, so jumping straight to the next quest hid the bar
            // ever reaching full.
            if (progressBar != null) progressBar.fillAmount = 1f;

            Vector3 baseScale = panel.localScale;
            const float punch = 0.06f;
            const float half = CelebrationSeconds * 0.5f;

            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                panel.localScale = baseScale * (1f + punch * (t / half));
                yield return null;
            }
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                panel.localScale = baseScale * (1f + punch * (1f - t / half));
                yield return null;
            }

            panel.localScale = baseScale;

            // Let the full bar read for a beat before the panel moves on.
            yield return new WaitForSecondsRealtime(HoldSeconds);
        }

        Refresh();
    }

    // Short enough not to delay the next objective, long enough to register as deliberate.
    private const float CelebrationSeconds = 0.24f;
    private const float HoldSeconds = 0.55f;

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
