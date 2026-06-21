using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.Core;
using SowurShield.UI;

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

    // Lore section (built dynamically on open)
    private Transform loreContainer;
    private TextMeshProUGUI loreTitleHeader;

    private UITheme theme;

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
        theme = Resources.Load<UITheme>("UI/CozyUITheme");
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
        Color backgroundDark = theme != null ? theme.woodDark : new Color(0.08f, 0.06f, 0.1f);
        Color textCream = theme != null ? theme.backgroundCream : Color.white;
        Color highlightGold = theme != null ? theme.highlightGold : new Color(0.96f, 0.83f, 0.37f);

        // Root panel — VerticalLayoutGroup drives height; fixed width 520px
        GameObject panelObj = new GameObject("RelationshipPanel");
        panelObj.transform.SetParent(transform, false);

        panel = panelObj.AddComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot    = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(520, 0); // height driven by fitter

        panelObj.AddComponent<Image>().color = new Color(backgroundDark.r, backgroundDark.g, backgroundDark.b, 0.95f);

        var rootVlg = panelObj.AddComponent<VerticalLayoutGroup>();
        rootVlg.padding = new RectOffset(12, 12, 12, 12);
        rootVlg.spacing = 0;
        rootVlg.childControlWidth  = true;
        rootVlg.childControlHeight = true;
        rootVlg.childForceExpandWidth  = true;
        rootVlg.childForceExpandHeight = false;

        panelObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Row 1: portrait (left, fixed) + info column (right, flex) ──
        GameObject rowObj = new GameObject("HeaderRow");
        rowObj.transform.SetParent(panelObj.transform, false);
        var rowLE = rowObj.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 200;

        var hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childControlWidth  = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        // Portrait
        GameObject portraitObj = new GameObject("Portrait");
        portraitObj.transform.SetParent(rowObj.transform, false);
        var portLE = portraitObj.AddComponent<LayoutElement>();
        portLE.preferredWidth  = 150;
        portLE.flexibleWidth   = 0;
        portraitImage = portraitObj.AddComponent<Image>();
        portraitImage.color = theme != null ? theme.woodLight : new Color(0.25f, 0.25f, 0.28f, 1f);
        portraitImage.preserveAspect = true;

        // Info column (name, bio, bar, value)
        GameObject infoObj = new GameObject("Info");
        infoObj.transform.SetParent(rowObj.transform, false);
        infoObj.AddComponent<LayoutElement>().flexibleWidth = 1;

        var infoVlg = infoObj.AddComponent<VerticalLayoutGroup>();
        infoVlg.spacing = 6;
        infoVlg.childControlWidth  = true;
        infoVlg.childControlHeight = true;
        infoVlg.childForceExpandWidth  = true;
        infoVlg.childForceExpandHeight = false;

        nameText = CreateLabel(infoObj.transform, "NPC Name");
        nameText.fontSize  = 22;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.TopLeft;
        SetPreferredHeight(nameText, 30);

        bioText = CreateLabel(infoObj.transform, "No information available yet.");
        bioText.fontSize  = 13;
        bioText.alignment = TextAlignmentOptions.TopLeft;
        bioText.textWrappingMode = TMPro.TextWrappingModes.Normal;
        SetPreferredHeight(bioText, 90);

        relationshipLabelText = CreateLabel(infoObj.transform, "Acquaintance");
        relationshipLabelText.fontSize  = 14;
        relationshipLabelText.fontStyle = FontStyles.Bold;
        relationshipLabelText.alignment = TextAlignmentOptions.TopLeft;
        SetPreferredHeight(relationshipLabelText, 22);

        // Bar
        GameObject barBg = new GameObject("BarBg");
        barBg.transform.SetParent(infoObj.transform, false);
        barBg.AddComponent<LayoutElement>().preferredHeight = 16;
        barBg.AddComponent<Image>().color = theme != null ? theme.woodDark : new Color(0.15f, 0.15f, 0.18f, 1f);

        GameObject barFill = new GameObject("BarFill");
        barFill.transform.SetParent(barBg.transform, false);
        var barFillRT = barFill.AddComponent<RectTransform>();
        barFillRT.anchorMin = Vector2.zero;
        barFillRT.anchorMax = Vector2.one;
        barFillRT.offsetMin = barFillRT.offsetMax = Vector2.zero;
        relationshipFillImage = barFill.AddComponent<Image>();
        relationshipFillImage.color      = highlightGold;
        relationshipFillImage.type       = Image.Type.Filled;
        relationshipFillImage.fillMethod = Image.FillMethod.Horizontal;
        relationshipFillImage.fillAmount = 0.5f;

        relationshipValueText = CreateLabel(infoObj.transform, "0 / 100");
        relationshipValueText.fontSize  = 13;
        relationshipValueText.alignment = TextAlignmentOptions.TopLeft;
        SetPreferredHeight(relationshipValueText, 18);

        // ── Row 2: Codex lore section ──
        GameObject divObj = new GameObject("Divider");
        divObj.transform.SetParent(panelObj.transform, false);
        divObj.AddComponent<Image>().color = theme != null ? theme.woodLight : new Color(0.3f, 0.3f, 0.35f, 1f);
        divObj.AddComponent<LayoutElement>().preferredHeight = 1;

        loreTitleHeader = CreateLabel(panelObj.transform, "Codex");
        loreTitleHeader.fontSize  = 13;
        loreTitleHeader.fontStyle = FontStyles.Bold;
        loreTitleHeader.color     = highlightGold;
        loreTitleHeader.alignment = TextAlignmentOptions.TopLeft;
        SetPreferredHeight(loreTitleHeader, 20);
        loreTitleHeader.gameObject.SetActive(false);

        GameObject loreContainerObj = new GameObject("LoreContainer");
        loreContainerObj.transform.SetParent(panelObj.transform, false);
        loreContainer = loreContainerObj.transform;

        var loreVlg = loreContainerObj.AddComponent<VerticalLayoutGroup>();
        loreVlg.spacing = 4;
        loreVlg.childControlWidth  = true;
        loreVlg.childControlHeight = true;
        loreVlg.childForceExpandWidth  = true;
        loreVlg.childForceExpandHeight = false;
        loreContainerObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Row 3: Close button ──
        GameObject closeButtonObj = new GameObject("CloseButton");
        closeButtonObj.transform.SetParent(panelObj.transform, false);
        closeButtonObj.AddComponent<LayoutElement>().preferredHeight = 36;
        closeButtonObj.AddComponent<Image>().color = theme != null ? theme.woodDark : new Color(0.3f, 0.2f, 0.2f, 0.9f);

        Button closeButton = closeButtonObj.AddComponent<Button>();
        closeButton.onClick.AddListener(OnCloseButtonClicked);

        TextMeshProUGUI closeLabel = CreateLabel(closeButtonObj.transform, "Close");
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.fontSize  = 15;

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
        tmp.color = theme != null ? theme.backgroundCream : Color.white;

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
            portraitImage.color = theme != null ? theme.woodLight : new Color(0.3f, 0.3f, 0.3f, 1f);
        }

        nameText.text = targetNpc.GetNPCDisplayName();

        string bio = targetNpc.GetBio();
        bioText.text = string.IsNullOrEmpty(bio) ? "No information available yet." : bio;

        float level = targetNpc.GetRelationshipLevel();
        float normalized = (level + 100f) / 200f;

        relationshipFillImage.fillAmount = Mathf.Clamp01(normalized);
        relationshipLabelText.text = GetRelationshipLabel(level);
        relationshipValueText.text = $"{level:F0} / 100";

        RefreshLore();
    }

    private void RefreshLore()
    {
        if (loreContainer == null) return;

        // Clear previous entries
        for (int i = loreContainer.childCount - 1; i >= 0; i--)
            Destroy(loreContainer.GetChild(i).gameObject);

        var entries = targetNpc?.GetUnlockedLore();
        if (entries == null || entries.Length == 0)
        {
            if (loreTitleHeader != null) loreTitleHeader.gameObject.SetActive(false);
            return;
        }

        if (loreTitleHeader != null) loreTitleHeader.gameObject.SetActive(true);

        foreach (var entry in entries)
        {
            // Title
            if (!string.IsNullOrEmpty(entry.title))
            {
                var titleObj = new GameObject("LoreTitle");
                titleObj.transform.SetParent(loreContainer, false);
                var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
                titleTmp.text = entry.title;
                titleTmp.fontSize = 12;
                titleTmp.fontStyle = FontStyles.Bold;
                titleTmp.color = theme != null ? theme.highlightGold : new Color(0.9f, 0.8f, 0.5f);
                titleTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
                titleObj.AddComponent<LayoutElement>().preferredHeight = 16;
            }

            // Body
            var bodyObj = new GameObject("LoreBody");
            bodyObj.transform.SetParent(loreContainer, false);
            var bodyTmp = bodyObj.AddComponent<TextMeshProUGUI>();
            bodyTmp.text = entry.body;
            bodyTmp.fontSize = 11;
            bodyTmp.color = theme != null ? theme.backgroundCream : new Color(0.8f, 0.8f, 0.8f);
            bodyTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            var bodyLE = bodyObj.AddComponent<LayoutElement>();
            bodyLE.preferredHeight = 32;
            bodyLE.flexibleHeight = 1;
        }
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
