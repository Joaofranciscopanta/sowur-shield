using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
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
    private TextMeshProUGUI closeLabelRef;

    private UITheme theme;

    [SerializeField] private LocalizedString noInfoText; // table "Dialogue", key "dialogue.relationship.no_info"
    [SerializeField] private LocalizedString scoreText; // table "Dialogue", key "dialogue.relationship.score"
    [SerializeField] private LocalizedString belovedText; // table "Dialogue", key "dialogue.relationship.beloved"
    [SerializeField] private LocalizedString closeFriendText; // table "Dialogue", key "dialogue.relationship.close_friend"
    [SerializeField] private LocalizedString friendText; // table "Dialogue", key "dialogue.relationship.friend"
    [SerializeField] private LocalizedString acquaintanceText; // table "Dialogue", key "dialogue.relationship.acquaintance"
    [SerializeField] private LocalizedString tenseText; // table "Dialogue", key "dialogue.relationship.tense"
    [SerializeField] private LocalizedString hostileText; // table "Dialogue", key "dialogue.relationship.hostile"
    [SerializeField] private LocalizedString closeButtonText; // table "Dialogue", key "dialogue.gift.close" (shared "Close" label)
    [SerializeField] private LocalizedString codexHeaderText; // table "Dialogue", key "dialogue.relationship.codex_header"
    [SerializeField] private LocalizedString lockedTierText; // table "Dialogue", key "dialogue.relationship.locked_tier"
    [SerializeField] private LocalizedString tastesHeaderText; // table "Dialogue", key "dialogue.relationship.tastes_header"
    [SerializeField] private LocalizedString noTastesKnownText; // table "Dialogue", key "dialogue.relationship.no_tastes_known"
    // Gift-taste markers. Separate from the relationship-tier labels above (belovedText etc.):
    // those describe the overall bond, these describe one item's reception.
    [SerializeField] private LocalizedString lovedGiftText; // table "Dialogue", key "dialogue.gift.marker_loved"
    [SerializeField] private LocalizedString likedText; // table "Dialogue", key "dialogue.gift.marker_liked"
    [SerializeField] private LocalizedString dislikedText; // table "Dialogue", key "dialogue.gift.marker_disliked"

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<RelationshipUI>(FindObjectsInactive.Include) != null)
            return;

        var go = new GameObject("RelationshipUI");
        go.AddComponent<RelationshipUI>();
        DontDestroyOnLoad(go);
    }

    // This component is created at runtime (never saved to a scene/prefab), so the
    // Tools > Sowur Shield > Auto-Wire Localized Fields editor pass can never reach it —
    // wire its LocalizedString table/key references here instead.
    private void WireLocalizedStrings()
    {
        noInfoText = new LocalizedString("Dialogue", "dialogue.relationship.no_info");
        scoreText = new LocalizedString("Dialogue", "dialogue.relationship.score");
        belovedText = new LocalizedString("Dialogue", "dialogue.relationship.beloved");
        closeFriendText = new LocalizedString("Dialogue", "dialogue.relationship.close_friend");
        friendText = new LocalizedString("Dialogue", "dialogue.relationship.friend");
        acquaintanceText = new LocalizedString("Dialogue", "dialogue.relationship.acquaintance");
        tenseText = new LocalizedString("Dialogue", "dialogue.relationship.tense");
        hostileText = new LocalizedString("Dialogue", "dialogue.relationship.hostile");
        closeButtonText = new LocalizedString("Dialogue", "dialogue.gift.close");
        codexHeaderText = new LocalizedString("Dialogue", "dialogue.relationship.codex_header");
        lockedTierText = new LocalizedString("Dialogue", "dialogue.relationship.locked_tier");
        tastesHeaderText = new LocalizedString("Dialogue", "dialogue.relationship.tastes_header");
        noTastesKnownText = new LocalizedString("Dialogue", "dialogue.relationship.no_tastes_known");
        lovedGiftText = new LocalizedString("Dialogue", "dialogue.gift.marker_loved");
        likedText = new LocalizedString("Dialogue", "dialogue.gift.marker_liked");
        dislikedText = new LocalizedString("Dialogue", "dialogue.gift.marker_disliked");
    }

    private bool _buildSucceeded = false;

    private void Awake()
    {
        TryBuildUI();
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        if (loreTitleHeader != null) loreTitleHeader.text = codexHeaderText.SafeGetLocalizedString();
        if (closeLabelRef != null) closeLabelRef.text = closeButtonText.SafeGetLocalizedString();
        if (isOpen) RefreshPanel();
    }

    private void TryBuildUI()
    {
        theme = Resources.Load<UITheme>("UI/CozyUITheme");
        WireLocalizedStrings();
        try
        {
            BuildUI();
            _buildSucceeded = true;
        }
        catch (System.Exception e)
        {
            // Localization tables may not be configured yet (see MOBILE_LOCALIZATION_SETUP.md) —
            // fail safe rather than leaving a half-built panel visible over the menu, but keep
            // retrying on next OpenForNpc() instead of staying broken for the rest of the session.
            Debug.LogError($"[RelationshipUI] BuildUI failed (Localization not configured?): {e}");
            _buildSucceeded = false;
            gameObject.SetActive(false);
        }
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

        // `panel == null` catches a destroyed panel as well as one never built: this component
        // is DontDestroyOnLoad but the UI it builds is not re-parented, so a scene change (or
        // entering Play Mode after the objects were torn down) leaves _buildSucceeded true
        // while every child reference is a destroyed Unity object. Reading one then throws
        // MissingReferenceException from inside RefreshLore, which surfaced as a codex that
        // opened with an empty body. Uses `== null`, not `?.`/`??`, which do not respect
        // Unity's fake-null.
        if (!_buildSucceeded || panel == null || loreContainer == null)
        {
            gameObject.SetActive(true);
            _buildSucceeded = false;
            TryBuildUI();
            if (!_buildSucceeded)
                return;
        }

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
        // Reuse the components on a rebuild rather than stacking duplicates. AddComponent on an
        // object that already has a Canvas silently produces a second one.
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 51;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // Without an explicit reference resolution the scaler defaults to 800x600, so a
        // 579px-tall panel was drawn 1388px tall on a 1080p screen and ran off both edges.
        // 1920x1080 matches the four popup canvases standardised on Jul/1.
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        // DestroyImmediate, not Destroy: Destroy is deferred to end of frame, so the old panel
        // would still be a child while BuildCodexPanel runs, and — worse — the *old* panel's
        // subobjects stay alive long enough that the freshly assigned fields get overwritten by
        // nothing while the stale ones are torn down a frame later. That left loreContainer
        // pointing at a destroyed Transform while a live LoreContainer sat in the hierarchy,
        // and RefreshLore's `if (loreContainer == null) return;` swallowed every codex row.
        if (panel != null) DestroyImmediate(panel.gameObject);

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
        // 600, not the original 520: the frame art eats 80px of width that the old value
        // did not account for, which would squeeze the info column instead of the border.
        panel.sizeDelta = new Vector2(600, 0); // height driven by fitter

        // Cozy sprite kit, same treatment the other panels got Jul/26–Aug/1.
        UIThemeStyler.StylePanel(panelObj, theme);

        var rootVlg = panelObj.AddComponent<VerticalLayoutGroup>();
        // The wood panel art carries a painted frame roughly 40px thick per side at this
        // panel's width. The 9-slice border field is NOT the usable inset — content laid out
        // against the old 12px padding lands on the frame. See SOWUR_SHIELD_STATUS.md.
        rootVlg.padding = new RectOffset(40, 40, 36, 36);
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
        portLE.minWidth        = 150; // preferred alone still compresses under a tight row
        portLE.flexibleWidth   = 0;
        portraitImage = portraitObj.AddComponent<Image>();
        portraitImage.color = theme != null ? theme.woodLight : new Color(0.25f, 0.25f, 0.28f, 1f);
        portraitImage.preserveAspect = true;

        // Decorative frame over the portrait, matching the minimap's treatment. Added as a
        // child overlay rather than replacing the portrait Image, so RefreshPanel can keep
        // swapping the sprite underneath without touching the frame. Non-raycast so it never
        // eats clicks meant for the panel.
        Sprite frameSprite = Resources.Load<Sprite>("Sprites/UI/Frames/frame_decorative_border");
        if (frameSprite != null)
        {
            var frameObj = new GameObject("PortraitFrame");
            frameObj.transform.SetParent(portraitObj.transform, false);
            var frameRT = frameObj.AddComponent<RectTransform>();
            frameRT.anchorMin = Vector2.zero;
            frameRT.anchorMax = Vector2.one;
            frameRT.offsetMin = new Vector2(-6f, -6f);
            frameRT.offsetMax = new Vector2(6f, 6f);
            var frameImg = frameObj.AddComponent<Image>();
            frameImg.sprite = frameSprite;
            frameImg.type = Image.Type.Sliced;
            frameImg.raycastTarget = false;
        }

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
        // Gold on the wood panel: the NPC's name is the panel's heading and should read as
        // one. Cream is kept for the body text below, where gold would be lower contrast.
        nameText.color = theme != null ? theme.highlightGold : new Color(0.96f, 0.83f, 0.37f);
        SetPreferredHeight(nameText, 30);

        bioText = CreateLabel(infoObj.transform, noInfoText.SafeGetLocalizedString());
        bioText.fontSize  = 13;
        bioText.alignment = TextAlignmentOptions.TopLeft;
        bioText.textWrappingMode = TMPro.TextWrappingModes.Normal;
        SetPreferredHeight(bioText, 90);

        relationshipLabelText = CreateLabel(infoObj.transform, acquaintanceText.SafeGetLocalizedString());
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

        // Tier ticks. The bar spans -100..100, so a threshold at t sits at (t + 100) / 200.
        // Without these the bar shows a quantity but not progress toward anything.
        foreach (float threshold in TierThresholds)
        {
            var tick = new GameObject("Tick");
            tick.transform.SetParent(barBg.transform, false);
            var tickRT = tick.AddComponent<RectTransform>();
            float x = (threshold + 100f) / 200f;
            tickRT.anchorMin = new Vector2(x, 0f);
            tickRT.anchorMax = new Vector2(x, 1f);
            tickRT.pivot     = new Vector2(0.5f, 0.5f);
            tickRT.sizeDelta = new Vector2(2f, 0f);
            tickRT.anchoredPosition = Vector2.zero;
            var tickImg = tick.AddComponent<Image>();
            tickImg.color = new Color(0f, 0f, 0f, 0.35f);
            tickImg.raycastTarget = false;
        }

        relationshipValueText = CreateLabel(infoObj.transform, "0 / 100"); // placeholder, replaced by RefreshPanel via scoreText
        relationshipValueText.fontSize  = 13;
        relationshipValueText.alignment = TextAlignmentOptions.TopLeft;
        SetPreferredHeight(relationshipValueText, 18);

        // ── Row 2: Codex lore section ──
        GameObject divObj = new GameObject("Divider");
        divObj.transform.SetParent(panelObj.transform, false);
        divObj.AddComponent<Image>().color = theme != null ? theme.woodLight : new Color(0.3f, 0.3f, 0.35f, 1f);
        divObj.AddComponent<LayoutElement>().preferredHeight = 1;

        loreTitleHeader = CreateLabel(panelObj.transform, codexHeaderText.SafeGetLocalizedString());
        loreTitleHeader.fontSize  = 13;
        loreTitleHeader.fontStyle = FontStyles.Bold;
        loreTitleHeader.color     = highlightGold;
        loreTitleHeader.alignment = TextAlignmentOptions.TopLeft;
        SetPreferredHeight(loreTitleHeader, 20);
        loreTitleHeader.gameObject.SetActive(false);

        // RectTransform first, and only then cache the reference. `new GameObject()` starts
        // with a plain Transform; adding a RectTransform *replaces* it and destroys the
        // original. Caching before that swap left this field holding a destroyed Transform
        // while a perfectly live LoreContainer sat in the hierarchy — RefreshLore's null check
        // then swallowed every codex row with no error at all.
        GameObject loreContainerObj = new GameObject("LoreContainer", typeof(RectTransform));
        loreContainerObj.transform.SetParent(panelObj.transform, false);

        // A GameObject built with AddComponent<RectTransform> starts on point anchors with
        // sizeDelta 0. The parent VerticalLayoutGroup drives width, but the ContentSizeFitter
        // below only grows height once children report a preferred size — and with no
        // LayoutElement of its own this container measured (520, 0), so every codex row was
        // laid out into a zero-height rect and nothing appeared. Same family of bug as the
        // 0-width rows fixed on Aug/1.
        var loreRT = loreContainerObj.GetComponent<RectTransform>();
        loreRT.anchorMin = new Vector2(0f, 1f);
        loreRT.anchorMax = new Vector2(1f, 1f);
        loreRT.pivot     = new Vector2(0.5f, 1f);

        loreContainer = loreRT;

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
        closeButtonObj.AddComponent<LayoutElement>().preferredHeight = 44;

        Button closeButton = closeButtonObj.AddComponent<Button>();
        closeButton.onClick.AddListener(OnCloseButtonClicked);

        closeLabelRef = CreateLabel(closeButtonObj.transform, closeButtonText.SafeGetLocalizedString());
        closeLabelRef.alignment = TextAlignmentOptions.Center;
        closeLabelRef.fontSize  = 15;

        // StyleButton applies the gold sprite and darkens the label for it — it must run
        // after the label exists, since it looks the TMP text up as a child.
        UIThemeStyler.StyleButton(closeButton, theme);

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
        // Dark by default: this panel's sprite has a cream interior, so cream text (the old
        // default, chosen when the background was a flat woodDark fill) was invisible on it.
        // Callers that sit on the wood border — the NPC name, the close button — override.
        tmp.color = theme != null ? theme.textDark : Color.black;

        // Every label gets a LayoutElement, sized from the text unless a caller overrides it
        // via SetPreferredHeight. Without one, a stretch-anchored label inside a
        // VerticalLayoutGroup reports no preferred height and the group hands it the whole
        // panel — which is exactly what the "Codex" heading did, covering the entire window.
        var layoutElement = obj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 20f;

        return tmp;
    }

    /// <summary>
    /// Adds a <see cref="LayoutElement"/> with a fixed preferred height so the label
    /// behaves predictably inside the info column's VerticalLayoutGroup.
    /// </summary>
    private void SetPreferredHeight(TextMeshProUGUI label, float height)
    {
        // Reuses the LayoutElement CreateLabel already added — a second one on the same object
        // is ignored by Unity's layout system, so adding rather than reusing would silently
        // leave the default height in effect.
        LayoutElement layoutElement = label.GetComponent<LayoutElement>();
        if (layoutElement == null) layoutElement = label.gameObject.AddComponent<LayoutElement>();
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
        bioText.text = string.IsNullOrEmpty(bio) ? noInfoText.SafeGetLocalizedString() : bio;

        float level = targetNpc.GetRelationshipLevel();
        float normalized = (level + 100f) / 200f;

        relationshipFillImage.fillAmount = Mathf.Clamp01(normalized);
        relationshipLabelText.text = GetRelationshipLabel(level);
        scoreText.Arguments = new object[] { level };
        relationshipValueText.text = scoreText.SafeGetLocalizedString();

        RefreshLore();
    }

    private void RefreshLore()
    {
        // A destroyed container means the panel was rebuilt and this field went stale. Silently
        // returning here is what made the codex open with an empty body and no error at all, so
        // say something rather than nothing.
        if (loreContainer == null)
        {
            Debug.LogWarning("[RelationshipUI] loreContainer is missing — the codex body cannot " +
                             "be populated. The panel was likely rebuilt without refreshing this field.");
            return;
        }

        // Clear previous entries
        for (int i = loreContainer.childCount - 1; i >= 0; i--)
            Destroy(loreContainer.GetChild(i).gameObject);

        if (targetNpc == null)
        {
            if (loreTitleHeader != null) loreTitleHeader.gameObject.SetActive(false);
            return;
        }

        var entries = targetNpc.GetUnlockedLore();
        var locked   = targetNpc.GetLockedLore();
        int total    = targetNpc.GetTotalLoreCount();

        if (total == 0)
        {
            if (loreTitleHeader != null) loreTitleHeader.gameObject.SetActive(false);
            return;
        }

        if (loreTitleHeader != null)
        {
            loreTitleHeader.gameObject.SetActive(true);
            // "Codex (2/4)" — the count is what tells the player there is more to find.
            loreTitleHeader.text = $"{codexHeaderText.SafeGetLocalizedString()} ({entries.Length}/{total})";
        }

        foreach (var entry in entries)
            CreateLoreRow(entry, false);

        foreach (var entry in locked)
            CreateLoreRow(entry, true);

        RefreshDiscoveredTastes();
    }

    /// <summary>
    /// Builds one lore row. Locked rows show the requirement instead of the body, so the
    /// player can see both that more exists and what it costs.
    /// </summary>
    private void CreateLoreRow(NpcLoreEntry entry, bool isLocked)
    {
        // panel_wood_generic has a LIGHT (cream) interior, not a dark one — the wood is only
        // the border. Cream body text on it measured ~1.1 and was invisible; the same mistake
        // BattleResultsUI made on its light panels Jul/26. Body text is textDark (~12:1 on the
        // cream field) and headings keep gold, which still clears the bar for large bold text.
        Color gold  = theme != null ? theme.highlightGold : new Color(0.9f, 0.8f, 0.5f);
        Color body  = theme != null ? theme.textDark : new Color(0.18f, 0.16f, 0.15f);

        // Locked rows are dimmed rather than recoloured, so they read as inactive without
        // needing a second colour measured against the same background.
        const float lockedAlpha = 0.55f;

        if (!string.IsNullOrEmpty(entry.title))
        {
            var titleObj = new GameObject(isLocked ? "LoreTitleLocked" : "LoreTitle");
            titleObj.transform.SetParent(loreContainer, false);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = isLocked ? $"🔒 {entry.title}" : entry.title;
            titleTmp.fontSize = 12;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = isLocked ? new Color(gold.r, gold.g, gold.b, lockedAlpha) : gold;
            titleTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            titleObj.AddComponent<LayoutElement>().preferredHeight = 16;
        }

        var bodyObj = new GameObject(isLocked ? "LoreBodyLocked" : "LoreBody");
        bodyObj.transform.SetParent(loreContainer, false);
        var bodyTmp = bodyObj.AddComponent<TextMeshProUGUI>();

        if (isLocked)
        {
            // Show the tier name rather than the raw number: "Requires: Close Friend" reads
            // as a goal, "Requires: 40" reads as a debug value.
            lockedTierText.Arguments = new object[] { GetRelationshipLabel(entry.requiredRelationship) };
            bodyTmp.text = lockedTierText.SafeGetLocalizedString();
            bodyTmp.fontStyle = FontStyles.Italic;
            bodyTmp.color = new Color(body.r, body.g, body.b, lockedAlpha);
        }
        else
        {
            bodyTmp.text = entry.body;
            bodyTmp.color = body;
        }

        bodyTmp.fontSize = 11;
        bodyTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
        var bodyLE = bodyObj.AddComponent<LayoutElement>();
        bodyLE.preferredHeight = isLocked ? 16 : 32;
        bodyLE.flexibleHeight = 1;
    }

    /// <summary>
    /// Lists the gift tastes the player has actually discovered, as an "N/M" line plus one row
    /// per known preference. This is what makes the preference system legible — without it the
    /// multipliers are invisible and the player has no way to record what they learned.
    /// </summary>
    private void RefreshDiscoveredTastes()
    {
        string[] allPreferred = targetNpc.GetAllPreferredItemNames();
        if (allPreferred.Length == 0) return;

        var known = new System.Collections.Generic.List<string>();
        foreach (string itemName in allPreferred)
        {
            var reaction = targetNpc.GetDiscoveredReaction(itemName);
            if (!reaction.HasValue) continue;

            var item = SowurShield.Inventory.ItemDatabase.GetItem(itemName);
            string display = item != null ? item.GetDisplayName() : itemName;
            known.Add($"{GetReactionMarkup(reaction.Value)} {display}");
        }

        var headerObj = new GameObject("TastesHeader");
        headerObj.transform.SetParent(loreContainer, false);
        var headerTmp = headerObj.AddComponent<TextMeshProUGUI>();
        tastesHeaderText.Arguments = new object[] { known.Count, allPreferred.Length };
        headerTmp.text = tastesHeaderText.SafeGetLocalizedString();
        headerTmp.fontSize = 12;
        headerTmp.fontStyle = FontStyles.Bold;
        headerTmp.color = theme != null ? theme.highlightGold : new Color(0.9f, 0.8f, 0.5f);
        headerTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
        headerObj.AddComponent<LayoutElement>().preferredHeight = 18;

        var bodyObj = new GameObject("TastesBody");
        bodyObj.transform.SetParent(loreContainer, false);
        var bodyTmp = bodyObj.AddComponent<TextMeshProUGUI>();
        bodyTmp.text = known.Count > 0
            ? string.Join("\n", known)
            : noTastesKnownText.SafeGetLocalizedString();
        bodyTmp.fontSize = 11;
        // Dark, like the lore bodies: the panel's interior is cream, not wood.
        bodyTmp.color = theme != null ? theme.textDark : new Color(0.18f, 0.16f, 0.15f);
        bodyTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
        var bodyLE = bodyObj.AddComponent<LayoutElement>();
        bodyLE.preferredHeight = Mathf.Max(16, known.Count * 14);
        bodyLE.flexibleHeight = 1;
    }

    private string GetReactionMarkup(GiftReaction reaction)
    {
        Color c;
        string label;
        switch (reaction)
        {
            case GiftReaction.Loved:
                c = theme != null ? theme.highlightGold : new Color(0.96f, 0.83f, 0.37f);
                label = lovedGiftText.SafeGetLocalizedString();
                break;
            case GiftReaction.Liked:
                c = theme != null ? theme.positive : new Color(0.5f, 0.78f, 0.5f);
                label = likedText.SafeGetLocalizedString();
                break;
            case GiftReaction.Disliked:
                c = theme != null ? theme.negative : new Color(0.9f, 0.45f, 0.45f);
                label = dislikedText.SafeGetLocalizedString();
                break;
            default:
                return string.Empty;
        }
        return $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{label}</color>";
    }

    /// <summary>
    /// The affinity thresholds where the relationship label changes. Single source of truth:
    /// both <see cref="GetRelationshipLabel"/> and the bar's tier ticks read this, so a tier
    /// cannot be renumbered in one place and left stale in the other.
    /// </summary>
    private static readonly float[] TierThresholds = { -40f, -10f, 10f, 40f, 75f };

    /// <summary>
    /// Maps a relationship level (-100..100) to a descriptive label.
    /// </summary>
    private string GetRelationshipLabel(float level)
    {
        if (level >= 75f) return belovedText.SafeGetLocalizedString();
        if (level >= 40f) return closeFriendText.SafeGetLocalizedString();
        if (level >= 10f) return friendText.SafeGetLocalizedString();
        if (level >= -10f) return acquaintanceText.SafeGetLocalizedString();
        if (level >= -40f) return tenseText.SafeGetLocalizedString();
        return hostileText.SafeGetLocalizedString();
    }
}

} // namespace SowurShield.Dialogue
