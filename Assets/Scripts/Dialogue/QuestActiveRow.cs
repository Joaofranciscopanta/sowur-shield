using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SowurShield.UI;
using SowurShield.Core;

namespace SowurShield.Dialogue
{

/// <summary>
/// Attach to the active-quest row prefab. Wire all fields in the Inspector.
/// Objective sub-rows are built dynamically since QuestData.objectives is unbounded.
///
/// Kept in its own file (not alongside QuestsUI) so the Editor builder that creates this
/// prefab can reference it with the standard fileID 11500000 — a secondary class sharing a
/// .cs file with another MonoBehaviour gets a different, non-obvious fileID that can't
/// safely be hand-written into prefab YAML.
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
        if (titleText != null) titleText.text = data.questTitle.SafeGetLocalizedString();
        if (descriptionText != null) descriptionText.text = data.questDescription.SafeGetLocalizedString();

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
            string objDescText = obj.description.SafeGetLocalizedString();
            lineText.text = obj.requiredCount > 1
                ? $"{prefix}{objDescText} ({objProgress}/{obj.requiredCount})"
                : $"{prefix}{objDescText}";
            lineText.color = done
                ? (theme != null ? theme.positive : new Color(0.5f, 0.78f, 0.52f))
                : (theme != null ? theme.textDark : Color.white);
        }
    }
}

} // namespace SowurShield.Dialogue
