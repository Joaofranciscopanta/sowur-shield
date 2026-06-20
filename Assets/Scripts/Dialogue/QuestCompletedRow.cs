using UnityEngine;
using TMPro;

namespace SowurShield.Dialogue
{

/// <summary>
/// Attach to the completed-quest row prefab — read-only journal entry.
/// Kept in its own file for the same fileID reason as QuestActiveRow.
/// </summary>
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
