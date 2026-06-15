using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.Core;

namespace SowurShield.Dialogue
{

/// <summary>
/// Self-spawning "Codex"-style UI that shows the player's relationship progress
/// with a nearby NPC: portrait, name, bio and an affection bar. Built procedurally
/// (no scene wiring required), following the same pattern as
/// <see cref="SowurShield.Combat.ConsumableBattleUI"/> and <see cref="GiftSelectionUI"/>.
/// </summary>
public class RelationshipUI : MonoBehaviour, IUIWindow
{
    private RectTransform panel;
    private bool isOpen = false;

    private NPCDialogueInteractable targetNpc;

    // Panel content references
    private Image portraitImage;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI bioText;
    private TextMeshProUGUI relationshipLabelText;
    private TextMeshProUGUI relationshipValueText;
    private Image relationshipFillImage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<RelationshipUI>() != null)
            return;

        var go = new GameObject("RelationshipUI");
        go.AddComponent<RelationshipUI>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        BuildUI();
    }

    // =========================================================================
    // IUIWindow Implementation
    // =========================================================================

    public string WindowName => "RelationshipUI";
    public int WindowPriority => SowurShield.Core.WindowPriority.Dialogue;
    public bool IsWindowOpen => isOpen;
    public bool CanCloseWithEsc => true;

    public void OpenWindow()
    {
        RefreshPanel();

        panel.gameObject.SetActive(true);
        isOpen = true;

        FindFirstObjectByType<PlayerMove>()?.DisableMovement();
    }

    public void CloseWindow()
    {
        panel.gameObject.SetActive(false);
        isOpen = false;
        targetNpc = null;

        FindFirstObjectByType<PlayerMove>()?.EnableMovement();
    }

    public void OnWindowBlocked(string blockedBy) { }

    /// <summary>
    /// Opens the relationship "Codex" panel for the given NPC. Called from the
    /// "View relationship" dialogue choice after the dialogue window has closed.
    /// </summary>
    public void OpenForNpc(NPCDialogueInteractable npc)
    {
        if (npc == null)
            return;

        targetNpc = npc;

        if (UIManager.Instance != null)
            UIManager.Instance.TryOpenWindow(this);
        else
            OpenWindow();
    }

    // =========================================================================
    // UI Construction
    // =========================================================================

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 51;

        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        gameObject.AddComponent<GraphicRaycaster>();

        BuildCodexPanel();
    }

    /// <summary>
    /// Builds the centered "Codex" panel: portrait on the left, NPC info
    /// (name, bio, relationship bar) on the right.
    /// </summary>
    private void BuildCodexPanel()
    {
        GameObject panelObj = new GameObject("RelationshipPanel");
        panelObj.transform.SetParent(transform, false);

        panel = panelObj.AddComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(500, 400);

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.06f, 0.1f, 0.95f);

        // --- Left column: portrait ---
        GameObject portraitObj = new GameObject("Portrait");
        portraitObj.transform.SetParent(panel, false);

        RectTransform portraitRect = portraitObj.AddComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0, 0);
        portraitRect.anchorMax = new Vector2(0, 1);
        portraitRect.pivot = new Vector2(0, 0.5f);
        portraitRect.anchoredPosition = new Vector2(10, 0);
        portraitRect.sizeDelta = new Vector2(180, -20);

        portraitImage = portraitObj.AddComponent<Image>();
        portraitImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        portraitImage.preserveAspect = true;

        // --- Right column: name, bio, relationship bar ---
        GameObject infoObj = new GameObject("Info");
        infoObj.transform.SetParent(panel, false);

        RectTransform infoRect = infoObj.AddComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0, 0);
        infoRect.anchorMax = new Vector2(1, 1);
        infoRect.offsetMin = new Vector2(200, 10);
        infoRect.offsetMax = new Vector2(-10, -10);

        VerticalLayoutGroup infoLayout = infoObj.AddComponent<VerticalLayoutGroup>();
        infoLayout.spacing = 8;
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = true;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;
        infoLayout.childAlignment = TextAnchor.UpperLeft;

        // Name
        nameText = CreateLabel(infoObj.transform, "NPC Name");
        nameText.fontSize = 24;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.TopLeft;
        SetPreferredHeight(nameText, 32);

        // Bio
        bioText = CreateLabel(infoObj.transform, "No information available yet.");
        bioText.fontSize = 14;
        bioText.alignment = TextAlignmentOptions.TopLeft;
        bioText.textWrappingMode = TMPro.TextWrappingModes.Normal;
        SetPreferredHeight(bioText, 140);

        // Relationship level label (e.g. "Acquaintance")
        relationshipLabelText = CreateLabel(infoObj.transform, "Acquaintance");
        relationshipLabelText.fontSize = 16;
        relationshipLabelText.fontStyle = FontStyles.Bold;
        relationshipLabelText.alignment = TextAlignmentOptions.TopLeft;
        SetPreferredHeight(relationshipLabelText, 24);

        // Relationship bar (background + fill)
        GameObject barBgObj = new GameObject("RelationshipBarBackground");
        barBgObj.transform.SetParent(infoObj.transform, false);

        RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
        barBgRect.sizeDelta = new Vector2(280, 20);

        LayoutElement barLayoutElement = barBgObj.AddComponent<LayoutElement>();
        barLayoutElement.preferredWidth = 280;
        barLayoutElement.preferredHeight = 20;

        Image barBgImage = barBgObj.AddComponent<Image>();
        barBgImage.color = new Color(0.15f, 0.15f, 0.18f, 1f);

        GameObject barFillObj = new GameObject("RelationshipBarFill");
        barFillObj.transform.SetParent(barBgObj.transform, false);

        RectTransform barFillRect = barFillObj.AddComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;

        relationshipFillImage = barFillObj.AddComponent<Image>();
        relationshipFillImage.color = new Color(0.8f, 0.3f, 0.5f, 1f);
        relationshipFillImage.type = Image.Type.Filled;
        relationshipFillImage.fillMethod = Image.FillMethod.Horizontal;
        relationshipFillImage.fillAmount = 0.5f;

        // Relationship numeric value (e.g. "0 / 100")
        relationshipValueText = CreateLabel(infoObj.transform, "0 / 100");
        relationshipValueText.fontSize = 14;
        relationshipValueText.alignment = TextAlignmentOptions.TopLeft;
        SetPreferredHeight(relationshipValueText, 20);

        // --- Close button (bottom-right of panel) ---
        GameObject closeButtonObj = new GameObject("CloseButton");
        closeButtonObj.transform.SetParent(panel, false);

        RectTransform closeRect = closeButtonObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 0);
        closeRect.anchorMax = new Vector2(1, 0);
        closeRect.pivot = new Vector2(1, 0);
        closeRect.anchoredPosition = new Vector2(-10, 10);
        closeRect.sizeDelta = new Vector2(90, 32);

        Image closeImage = closeButtonObj.AddComponent<Image>();
        closeImage.color = new Color(0.3f, 0.2f, 0.2f, 0.9f);

        Button closeButton = closeButtonObj.AddComponent<Button>();
        closeButton.onClick.AddListener(OnCloseButtonClicked);

        TextMeshProUGUI closeLabel = CreateLabel(closeButtonObj.transform, "Close");
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.fontSize = 16;

        // Hidden by default; opened via OpenWindow().
        panelObj.SetActive(false);
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.color = Color.white;

        return tmp;
    }

    /// <summary>
    /// Adds a <see cref="LayoutElement"/> with a fixed preferred height so the label
    /// behaves predictably inside the info column's VerticalLayoutGroup.
    /// </summary>
    private void SetPreferredHeight(TextMeshProUGUI label, float height)
    {
        LayoutElement layoutElement = label.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = height;
    }

    // =========================================================================
    // Button / Panel
    // =========================================================================

    private void OnCloseButtonClicked()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TryCloseWindow(this);
        else
            CloseWindow();
    }

    /// <summary>
    /// Populates the panel's portrait, name, bio and relationship bar from
    /// <see cref="targetNpc"/>. Called every time the panel is opened, since
    /// the relationship level can change (e.g. after giving a gift).
    /// </summary>
    private void RefreshPanel()
    {
        if (targetNpc == null)
            return;

        Sprite portrait = targetNpc.GetPortrait();
        if (portrait != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.color = Color.white;
        }
        else
        {
            portraitImage.sprite = null;
            portraitImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        }

        nameText.text = targetNpc.GetNPCDisplayName();

        string bio = targetNpc.GetBio();
        bioText.text = string.IsNullOrEmpty(bio) ? "No information available yet." : bio;

        float level = targetNpc.GetRelationshipLevel();
        float normalized = (level + 100f) / 200f;

        relationshipFillImage.fillAmount = Mathf.Clamp01(normalized);
        relationshipLabelText.text = GetRelationshipLabel(level);
        relationshipValueText.text = $"{level:F0} / 100";
    }

    /// <summary>
    /// Maps a relationship level (-100..100) to a descriptive label.
    /// </summary>
    private string GetRelationshipLabel(float level)
    {
        if (level >= 75f) return "Beloved";
        if (level >= 40f) return "Close Friend";
        if (level >= 10f) return "Friend";
        if (level >= -10f) return "Acquaintance";
        if (level >= -40f) return "Tense";
        return "Hostile";
    }
}

} // namespace SowurShield.Dialogue
