using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.Localization;
using SowurShield.Animals;
using SowurShield.Core;
using SowurShield.Inventory;

namespace SowurShield.Combat
{

/// <summary>
/// The pre-combat team assembler: pick animals, place them on the grid, feed them, and
/// start the battle.
///
/// The whole screen is built in code by <see cref="TeamAssemblerLayout"/> each time it
/// opens. Previously the layout lived partly in the scene, partly in three unreferenced
/// editor scripts, and partly in runtime re-anchoring here — so nothing owned it and it
/// drifted. The scene now supplies only a canvas.
///
/// What changed for the player:
/// - The synergy list is real: it comes from <see cref="TeamSynergy"/>, the same evaluator
///   the battle uses, so what is promised here is what is applied there.
/// - Animals are grouped by family with a search box, instead of one flat 20-entry list
///   that showed 9 at a time.
/// - Team profiles save a whole line-up and restore it in one click.
/// - Feeding is a choice, not a gate: unfed animals fight at a penalty.
/// </summary>
public class TeamAssemblerUI : MonoBehaviour
{
    [Header("Scene Management")]
    [SerializeField] private string combatSceneName = "CombatScene";

    [Header("Grid Setup")]
    [SerializeField] private int gridWidth = 9;
    [SerializeField] private int gridHeight = 5;
    [SerializeField] private int playerColumns = 3; // rightmost columns (6-8)

    // ── Runtime UI, all built in BuildUI() ────────────────────────────────────
    private GameObject assemblerPanel;
    private RectTransform cardListContent;
    private RectTransform gridContainer;
    private RectTransform synergyListContent;
    private RectTransform profileRow;

    private TextMeshProUGUI zoneNameText;
    private TextMeshProUGUI teamSizeText;
    private TextMeshProUGUI foodSummaryText;
    private TextMeshProUGUI enemyPreviewText;
    private TMP_InputField searchField;
    private TextMeshProUGUI combatModeLabel;
    private TextMeshProUGUI frontLabel;
    private TextMeshProUGUI backLabel;
    private Button startBattleButton;
    private TextMeshProUGUI startBattleLabel;
    private RectTransform buttonRow;

    // ── Runtime data ──────────────────────────────────────────────────────────
    private readonly List<AnimalSelectionCard> animalCards = new List<AnimalSelectionCard>();
    private readonly List<GridPositionSlot> gridSlots = new List<GridPositionSlot>();
    private readonly List<Animal> availableAnimals = new List<Animal>();
    private string searchFilter = "";

    /// <summary>Height of the saved-profiles row, reserved above the button row.</summary>
    private const float ProfileRowHeight = 34f;

    /// <summary>
    /// Vertical space kept free at the bottom of the screen for the farm's hotbar, which
    /// renders above this canvas. Measured: the hotbar occupies y 6..54.
    /// </summary>
    private const float HotbarClearance = 60f;

    /// <summary>
    /// Static captions built in code, with the Combat-table key and English fallback they
    /// came from, so they can be re-resolved.
    ///
    /// BuildUI can run before the localization tables finish loading — and did, leaving
    /// every column title in English on a Portuguese build. The neighbouring scene labels
    /// carry a LocalizeStringEvent that re-resolves itself; these are created at runtime
    /// and have to do it by hand.
    /// </summary>
    private readonly List<(TextMeshProUGUI label, string key, string fallback)> staticLabels
        = new List<(TextMeshProUGUI, string, string)>();

    /// <summary>Create a label whose caption is re-resolved on locale change.</summary>
    private TextMeshProUGUI CreateLocalizedLabel(string name, Transform parent,
        string key, string fallback, float fontSize, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var label = TeamAssemblerLayout.CreateLabel(name, parent, Localize(key, fallback),
            fontSize, color, alignment);
        staticLabels.Add((label, key, fallback));
        return label;
    }

    /// <summary>Re-resolve every code-built caption in the current language.</summary>
    private void RefreshStaticLabels()
    {
        foreach (var entry in staticLabels)
        {
            if (entry.label == null) continue;
            entry.label.text = Localize(entry.key, entry.fallback);
        }

        // These two carry a direction arrow around the caption.
        if (frontLabel != null)
            frontLabel.text = "< " + Localize("combat.teamassembler.front_line", "Front");
        if (backLabel != null)
            backLabel.text = Localize("combat.teamassembler.back_line", "Back") + " >";

        // Buttons are sized in BuildFooter, which runs before the localization tables are
        // ready — so they get measured against the English captions and the longer
        // Portuguese ones overflow the painted pill ("Alimentar Todo", "Iniciar Batalh").
        // Re-measure now that every caption is final.
        ResizeButtonRow();
    }

    public static TeamAssemblerUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _)
    {
        if (IsOpen()) RefreshEverything();
    }

    private void Start()
    {
        // Returning here from a battle's "Retry" reopens the assembler for the same stage.
        if (TeamAssemblerData.Instance.pendingReopenAssembler)
        {
            TeamAssemblerData.Instance.pendingReopenAssembler = false;
            OpenAssembler();
        }
    }

    public bool IsOpen() => assemblerPanel != null && assemblerPanel.activeSelf;

    // ══════════════════════════════════════════════════════════════════════════
    // OPEN / CLOSE
    // ══════════════════════════════════════════════════════════════════════════

    public void OpenAssembler()
    {
        TeamAssemblerData.Instance.ClearTeam();
        searchFilter = "";

        StageData selectedStage = StageManager.GetSelectedStage();
        if (selectedStage != null)
        {
            TeamAssemblerData.Instance.zoneName = selectedStage.GetDisplayName();
            TeamAssemblerData.Instance.zoneDifficulty = selectedStage.difficulty;
        }

        // Rebuild from scratch: the roster can change between openings (animals bought,
        // sold or fallen ill), and a rebuilt screen cannot inherit stale references.
        if (assemblerPanel != null) Destroy(assemblerPanel);
        BuildUI();

        FindAvailableAnimals();
        BuildGrid();
        BuildAnimalList();
        BuildProfileRow();

        assemblerPanel.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        DisablePlayerMovement();

        RefreshEverything();
    }

    public void CloseAssembler()
    {
        if (assemblerPanel != null) assemblerPanel.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        EnablePlayerMovement();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UI CONSTRUCTION
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Three columns: the roster on the left, the grid in the middle, the briefing on the
    /// right, with a title bar above and the controls below. Widths are fractions of the
    /// screen so this holds at any resolution.
    /// </summary>
    private void BuildUI()
    {
        var theme = TeamAssemblerLayout.Theme;
        float pad = theme != null ? theme.spacingL : 16f;

        staticLabels.Clear();

        var root = TeamAssemblerLayout.CreateRect("AssemblerPanel", transform);
        assemblerPanel = root.gameObject;

        // A full-screen scrim, so the farm behind stops competing for attention. The
        // InfoPanel used to be a translucent rectangle over live gameplay, which read as
        // a hole in the screen.
        var scrim = root.gameObject.AddComponent<Image>();
        scrim.color = new Color(0.10f, 0.07f, 0.04f, 0.72f);

        // ── Title bar ─────────────────────────────────────────────────────────
        var header = TeamAssemblerLayout.CreateRect("Header", root);
        TeamAssemblerLayout.SetSize(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -pad),
            new Vector2(560f, TeamAssemblerLayout.HeaderHeight));

        // The title gets its own plate so it stops fighting the farm's clock and quest
        // tracker behind it. A flat fill, NOT panel_wood_generic: that sprite's 9-slice
        // borders exceed a 56px-tall rect, and a sliced sprite whose borders don't fit
        // draws NOTHING at all — no error, no warning (see CLAUDE.md).
        var headerPlate = header.gameObject.AddComponent<Image>();
        headerPlate.color = new Color(0.28f, 0.18f, 0.09f, 0.95f);

        // Gold on the dark plate, not headingOnLight — that colour is for cream surfaces.
        zoneNameText = TeamAssemblerLayout.CreateLabel("ZoneName", header, "",
            theme != null ? theme.fontSizeH2 : 24f,
            theme != null ? theme.highlightGold : new Color(0.96f, 0.83f, 0.37f),
            TextAlignmentOptions.Center);
        zoneNameText.rectTransform.offsetMin = new Vector2(24f, 8f);
        zoneNameText.rectTransform.offsetMax = new Vector2(-24f, -8f);

        // ── Column bands ──────────────────────────────────────────────────────
        float top = TeamAssemblerLayout.HeaderHeight + pad * 2f;
        // Room for both footer rows (buttons + profiles), which otherwise overlap: the
        // first pass anchored the profile row inside the button row's band.
        float bottom = HotbarClearance + TeamAssemblerLayout.FooterHeight + ProfileRowHeight + pad * 2f;

        var left = TeamAssemblerLayout.CreatePanel("RosterPanel", root);
        left.anchorMin = new Vector2(0f, 0f);
        left.anchorMax = new Vector2(0.40f, 1f);
        left.offsetMin = new Vector2(pad, bottom);
        left.offsetMax = new Vector2(-pad * 0.5f, -top);

        var middle = TeamAssemblerLayout.CreatePanel("GridPanel", root);
        middle.anchorMin = new Vector2(0.40f, 0f);
        middle.anchorMax = new Vector2(0.70f, 1f);
        middle.offsetMin = new Vector2(pad * 0.5f, bottom);
        middle.offsetMax = new Vector2(-pad * 0.5f, -top);

        var right = TeamAssemblerLayout.CreatePanel("BriefingPanel", root);
        right.anchorMin = new Vector2(0.70f, 0f);
        right.anchorMax = new Vector2(1f, 1f);
        right.offsetMin = new Vector2(pad * 0.5f, bottom);
        right.offsetMax = new Vector2(-pad, -top);

        BuildRosterColumn(left);
        BuildGridColumn(middle);
        BuildBriefingColumn(right);
        BuildFooter(root);
    }

    /// <summary>
    /// Left column: search, then the animal list. The list is grouped by family with a
    /// header per group, which is what makes 20 animals navigable.
    /// </summary>
    private void BuildRosterColumn(RectTransform parent)
    {
        var theme = TeamAssemblerLayout.Theme;
        float side = TeamAssemblerLayout.FrameInsetSide;
        float top = TeamAssemblerLayout.FrameInsetTop;
        float bottom = TeamAssemblerLayout.FrameInsetBottom;

        var title = CreateLocalizedLabel("RosterTitle", parent,
            "combat.teamassemblersetup.available_animals", "Available Animals",
            theme != null ? theme.fontSizeH2 : 24f,
            theme != null ? theme.headingOnLight : new Color(0.48f, 0.31f, 0.07f),
            TextAlignmentOptions.Center);
        TeamAssemblerLayout.SetSize(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -top), new Vector2(-side * 2f, 32f));

        searchField = TeamAssemblerLayout.CreateInputField("SearchField", parent,
            Localize("combat.teamassembler.search_placeholder", "Search animals..."));
        TeamAssemblerLayout.SetSize(searchField.GetComponent<RectTransform>(),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -(top + 38f)), new Vector2(-side * 2f, 34f));
        searchField.onValueChanged.AddListener(OnSearchChanged);

        var listHost = TeamAssemblerLayout.CreateRect("RosterList", parent);
        TeamAssemblerLayout.Fill(listHost, side, bottom, side, top + 80f);
        // expandChildWidth off: cards keep the card_animal 5:1 ratio.
        cardListContent = TeamAssemblerLayout.CreateScrollView("Scroll", listHost,
            TeamAssemblerLayout.CardSpacing, expandChildWidth: false);
    }

    /// <summary>
    /// Middle column: the placement grid, with a legend that finally says what the columns
    /// mean. The grid used to label each slot with its raw coordinate ("6,2") and nothing
    /// indicated that the left column meets the enemy first.
    /// </summary>
    private void BuildGridColumn(RectTransform parent)
    {
        var theme = TeamAssemblerLayout.Theme;
        float side = TeamAssemblerLayout.FrameInsetSide;
        float top = TeamAssemblerLayout.FrameInsetTop;
        float bottom = TeamAssemblerLayout.FrameInsetBottom;

        var title = CreateLocalizedLabel("GridTitle", parent,
            "combat.teamassembler.formation", "Formation",
            theme != null ? theme.fontSizeH2 : 24f,
            theme != null ? theme.headingOnLight : new Color(0.48f, 0.31f, 0.07f),
            TextAlignmentOptions.Center);
        TeamAssemblerLayout.SetSize(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -top), new Vector2(-side * 2f, 32f));

        // "Front" marker above the leftmost column, "Back" below the rightmost.
        // Plain ASCII arrows: Nunito SDF has no glyph for U+25C0/U+25B6 and they render as
        // tofu boxes. Verified with TMP_FontAsset.HasCharacter before shipping them.
        frontLabel = TeamAssemblerLayout.CreateLabel("FrontLabel", parent,
            "< " + Localize("combat.teamassembler.front_line", "Front"),
            theme != null ? theme.fontSizeCaption : 12f,
            theme != null ? theme.warning : new Color(1f, 0.72f, 0.30f),
            TextAlignmentOptions.Left);
        TeamAssemblerLayout.SetSize(frontLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -(top + 34f)), new Vector2(-side * 2f, 20f));

        var gridHost = TeamAssemblerLayout.CreateRect("GridHost", parent);
        TeamAssemblerLayout.Fill(gridHost, side, bottom + 44f, side, top + 58f);

        gridContainer = TeamAssemblerLayout.CreateRect("GridContainer", gridHost);
        gridContainer.anchorMin = new Vector2(0.5f, 0.5f);
        gridContainer.anchorMax = new Vector2(0.5f, 0.5f);
        gridContainer.pivot = new Vector2(0.5f, 0.5f);
        gridContainer.anchoredPosition = Vector2.zero;

        var glg = gridContainer.gameObject.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(TeamAssemblerLayout.SlotSize, TeamAssemblerLayout.SlotSize);
        glg.spacing = new Vector2(TeamAssemblerLayout.SlotSpacing, TeamAssemblerLayout.SlotSpacing);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.MiddleCenter;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = playerColumns;

        backLabel = TeamAssemblerLayout.CreateLabel("BackLabel", parent,
            Localize("combat.teamassembler.back_line", "Back") + " >",
            theme != null ? theme.fontSizeCaption : 12f,
            theme != null ? theme.positive : new Color(0.51f, 0.78f, 0.52f),
            TextAlignmentOptions.Right);
        TeamAssemblerLayout.SetSize(backLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, bottom + 26f), new Vector2(-side * 2f, 20f));

        var hint = CreateLocalizedLabel("GridHint", parent,
            "combat.teamassembler.grid_hint", "Drag an animal onto a slot",
            theme != null ? theme.fontSizeCaption : 12f,
            new Color(0.45f, 0.42f, 0.38f),
            TextAlignmentOptions.Center);
        TeamAssemblerLayout.SetSize(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, bottom + 4f), new Vector2(-side * 2f, 18f));
    }

    /// <summary>
    /// Right column: what the team is, what it will face, and which synergies are live.
    /// This is the panel that used to be a translucent rectangle with three lines of text.
    /// </summary>
    private void BuildBriefingColumn(RectTransform parent)
    {
        var theme = TeamAssemblerLayout.Theme;
        float side = TeamAssemblerLayout.FrameInsetSide;
        float top = TeamAssemblerLayout.FrameInsetTop;
        float bottom = TeamAssemblerLayout.FrameInsetBottom;
        float width = -side * 2f;

        // Stacked top-down, each block's offset being the running total of what is above
        // it, so a change to one height does not silently overlap the next.
        float y = top;

        teamSizeText = TeamAssemblerLayout.CreateLabel("TeamSize", parent, "",
            theme != null ? theme.fontSizeH2 : 24f,
            theme != null ? theme.headingOnLight : new Color(0.48f, 0.31f, 0.07f),
            TextAlignmentOptions.Center);
        TeamAssemblerLayout.SetSize(teamSizeText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -y), new Vector2(width, 30f));
        y += 36f;

        enemyPreviewText = TeamAssemblerLayout.CreateLabel("EnemyPreview", parent, "",
            theme != null ? theme.fontSizeSmall : 14f,
            theme != null ? theme.textDark : new Color(0.18f, 0.16f, 0.15f),
            TextAlignmentOptions.TopLeft);
        TeamAssemblerLayout.SetSize(enemyPreviewText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -y), new Vector2(width, 88f));
        y += 94f;

        foodSummaryText = TeamAssemblerLayout.CreateLabel("FoodSummary", parent, "",
            theme != null ? theme.fontSizeSmall : 14f,
            theme != null ? theme.textDark : new Color(0.18f, 0.16f, 0.15f),
            TextAlignmentOptions.TopLeft);
        TeamAssemblerLayout.SetSize(foodSummaryText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -y), new Vector2(width, 76f));
        y += 82f;

        var synergyTitle = CreateLocalizedLabel("SynergyTitle", parent,
            "combat.teamassembler.synergies_header", "Synergies",
            theme != null ? theme.fontSizeBody : 18f,
            theme != null ? theme.headingOnLight : new Color(0.48f, 0.31f, 0.07f),
            TextAlignmentOptions.Left);
        TeamAssemblerLayout.SetSize(synergyTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -y), new Vector2(width, 24f));
        y += 28f;

        var synergyHost = TeamAssemblerLayout.CreateRect("SynergyList", parent);
        TeamAssemblerLayout.Fill(synergyHost, side, bottom, side, y);
        synergyListContent = TeamAssemblerLayout.CreateScrollView("Scroll", synergyHost, 6f);
    }

    /// <summary>Bottom bar: profiles above, then the action buttons.</summary>
    private void BuildFooter(RectTransform root)
    {
        var theme = TeamAssemblerLayout.Theme;
        float pad = theme != null ? theme.spacingL : 16f;

        // The two footer rows are stacked, not overlapping: buttons sit at the bottom, the
        // profile row directly above them. The first pass anchored both inside the same
        // band and they drew on top of each other.
        float buttonHeight = theme != null ? theme.buttonHeight : 44f;
        // Clear the farm's hotbar, which occupies the bottom ~54px of the screen and draws
        // over this canvas. Buttons sitting at y 16..60 were half-hidden behind it.
        float buttonCentre = HotbarClearance + pad + buttonHeight * 0.5f;
        float profileCentre = buttonCentre + buttonHeight * 0.5f + pad * 0.75f + ProfileRowHeight * 0.5f;

        profileRow = TeamAssemblerLayout.CreateRect("ProfileRow", root);
        TeamAssemblerLayout.SetSize(profileRow, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, profileCentre),
            new Vector2(-pad * 2f, ProfileRowHeight));

        var profileLayout = profileRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        profileLayout.spacing = 8f;
        profileLayout.childAlignment = TextAnchor.MiddleCenter;
        profileLayout.childControlWidth = true;
        profileLayout.childControlHeight = true;
        profileLayout.childForceExpandWidth = false;
        profileLayout.childForceExpandHeight = false;

        buttonRow = TeamAssemblerLayout.CreateRect("ButtonRow", root);
        TeamAssemblerLayout.SetSize(buttonRow, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, buttonCentre),
            new Vector2(-pad * 2f, buttonHeight));

        var rowLayout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        var modeButton = TeamAssemblerLayout.CreateButton("CombatModeToggle", buttonRow, "",
            "Buttons/button_secondary", OnCombatModeToggleClicked);
        combatModeLabel = modeButton.GetComponentInChildren<TextMeshProUGUI>(true);
        AddButtonSizing(modeButton);

        var feedButton = TeamAssemblerLayout.CreateButton("FeedAll", buttonRow,
            Localize("combat.teamassembler.feed_all", "Feed All"),
            "Buttons/button_small_action", OnFeedAllClicked);
        AddButtonSizing(feedButton);

        var clearButton = TeamAssemblerLayout.CreateButton("ClearGrid", buttonRow,
            Localize("combat.teamassembler.clear_grid", "Clear Grid"),
            "Buttons/button_danger", OnClearGridClicked);
        AddButtonSizing(clearButton);

        startBattleButton = TeamAssemblerLayout.CreateButton("StartBattle", buttonRow,
            Localize("combat.teamassembler.start_battle", "Start Battle"),
            "Buttons/button_primary", OnStartBattleClicked);
        startBattleLabel = startBattleButton.GetComponentInChildren<TextMeshProUGUI>(true);
        AddButtonSizing(startBattleButton);

        var cancelButton = TeamAssemblerLayout.CreateButton("Cancel", buttonRow,
            Localize("combat.teamassembler.cancel", "Cancel"),
            "Buttons/button_secondary", OnCancelClicked);
        AddButtonSizing(cancelButton);
    }

    /// <summary>Re-apply caption-based sizing to every button in the footer row.</summary>
    private void ResizeButtonRow()
    {
        if (buttonRow == null) return;

        foreach (Transform child in buttonRow)
        {
            var button = child.GetComponent<Button>();
            if (button != null) ApplyButtonSizing(button);
        }

        // The group measures on the next layout pass otherwise, and one frame would show
        // the buttons at their old widths.
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonRow);
    }

    /// <summary>
    /// Size a button to its own caption. Every button used to carry the same 170px width,
    /// picked against the English strings, so longer Portuguese captions overflowed and
    /// auto-sizing shrank them to different point sizes in the same row.
    /// </summary>
    private void AddButtonSizing(Button button)
    {
        button.gameObject.AddComponent<LayoutElement>();
        ApplyButtonSizing(button);
    }

    /// <summary>Measure this button's caption and set its width from it.</summary>
    private void ApplyButtonSizing(Button button)
    {
        var theme = TeamAssemblerLayout.Theme;
        var layout = button.GetComponent<LayoutElement>();
        if (layout == null) layout = button.gameObject.AddComponent<LayoutElement>();
        var label = button.GetComponentInChildren<TextMeshProUGUI>(true);

        layout.minHeight = theme != null ? theme.buttonHeight : 44f;
        layout.preferredHeight = layout.minHeight;

        float needed = 160f;
        if (label != null && !string.IsNullOrEmpty(label.text))
        {
            // Measure unconstrained: GetPreferredValues() with no arguments reports the
            // width the label has already been given, not the width it wants.
            // The caption lives inside the painted pill, which is (1 - 2*ButtonArtInset)
            // of the rect — so the rect has to be that much wider than the text itself.
            float textWidth = label.GetPreferredValues(label.text, Mathf.Infinity, Mathf.Infinity).x;
            // +48 rather than +20: measured on screen, the tighter figure left only 5-7px
            // between the caption and the pill's painted edge, which reads as cramped even
            // though nothing is technically clipped.
            needed = textWidth / (1f - TeamAssemblerLayout.ButtonArtInset * 2f) + 48f;
        }

        float min = theme != null ? theme.buttonMinWidth : 160f;
        float max = theme != null ? theme.buttonMaxWidth : 560f;
        layout.preferredWidth = Mathf.Clamp(needed, min, max);
        layout.minWidth = layout.preferredWidth;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CONTENT
    // ══════════════════════════════════════════════════════════════════════════

    private void FindAvailableAnimals()
    {
        availableAnimals.Clear();
        availableAnimals.AddRange(FindObjectsByType<Animal>(FindObjectsSortMode.None));
    }

    private void BuildGrid()
    {
        foreach (var slot in gridSlots)
            if (slot != null) DestroyImmediate(slot.gameObject);
        gridSlots.Clear();

        if (gridContainer == null) return;

        float totalWidth = playerColumns * TeamAssemblerLayout.SlotSize +
            (playerColumns - 1) * TeamAssemblerLayout.SlotSpacing;
        float totalHeight = gridHeight * TeamAssemblerLayout.SlotSize +
            (gridHeight - 1) * TeamAssemblerLayout.SlotSpacing;
        gridContainer.sizeDelta = new Vector2(totalWidth, totalHeight);

        int playerStartColumn = gridWidth - playerColumns;

        // Top row first so the GridLayoutGroup's rows read the same way as combat.
        for (int y = gridHeight - 1; y >= 0; y--)
        {
            for (int x = playerStartColumn; x < gridWidth; x++)
            {
                // Built here rather than from the scene prefab: that prefab labels every
                // slot with its raw grid coordinate ("6,2"), which tells the player
                // nothing, and has no fed indicator.
                GridPositionSlot slot = BuildSlot(gridContainer);
                if (slot == null) continue;

                slot.gameObject.name = $"Slot_{x}_{y}";
                slot.Initialize(new Vector2Int(x, y));
                gridSlots.Add(slot);
            }
        }
    }

    /// <summary>Build one grid slot from the art, when no prefab is supplied.</summary>
    private GridPositionSlot BuildSlot(Transform parent)
    {
        var rt = TeamAssemblerLayout.CreateRect("Slot", parent);

        var background = rt.gameObject.AddComponent<Image>();
        background.sprite = TeamAssemblerLayout.LoadSprite("Slots/slot_grid_empty");
        background.type = Image.Type.Simple;
        background.color = Color.white;

        var icon = TeamAssemblerLayout.CreateRect("AnimalIcon", rt);
        TeamAssemblerLayout.Fill(icon, 18f, 18f, 18f, 18f);
        var iconImage = icon.gameObject.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.enabled = false;

        var fed = TeamAssemblerLayout.CreateRect("FedIndicator", rt);
        TeamAssemblerLayout.SetSize(fed, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(14f, 14f));
        var fedImage = fed.gameObject.AddComponent<Image>();
        fedImage.raycastTarget = false;

        var slot = rt.gameObject.AddComponent<GridPositionSlot>();
        slot.AssignBuiltReferences(background, iconImage, fedImage);
        return slot;
    }

    /// <summary>
    /// Build the roster list, grouped by animal family and filtered by the search box.
    /// Grouping is what makes the list navigable: a flat list of 20 showed 9 at a time,
    /// with no way to find a specific animal except scrolling.
    /// </summary>
    private void BuildAnimalList()
    {
        foreach (var card in animalCards)
            if (card != null) DestroyImmediate(card.gameObject);
        animalCards.Clear();

        if (cardListContent == null) return;

        // Destroy() is deferred to end of frame, so a clear-then-rebuild would see the old
        // headers; DestroyImmediate is required here.
        for (int i = cardListContent.childCount - 1; i >= 0; i--)
            DestroyImmediate(cardListContent.GetChild(i).gameObject);

        var matching = availableAnimals
            .Where(a => a != null && a.AnimalData != null && MatchesSearch(a))
            .OrderBy(a => a.AnimalData.animalFamily)
            .ThenBy(a => a.GetDisplayName())
            .ToList();

        if (matching.Count == 0)
        {
            var empty = TeamAssemblerLayout.CreateLabel("EmptyState", cardListContent,
                Localize("combat.teamassembler.no_matches", "No animals match that search."),
                TeamAssemblerLayout.Theme != null ? TeamAssemblerLayout.Theme.fontSizeSmall : 14f,
                new Color(0.45f, 0.42f, 0.38f), TextAlignmentOptions.Center);
            var emptyLayout = empty.gameObject.AddComponent<LayoutElement>();
            emptyLayout.preferredHeight = 40f;
            return;
        }

        string currentFamily = null;
        foreach (var animal in matching)
        {
            string family = animal.AnimalData.animalFamily;
            if (family != currentFamily)
            {
                currentFamily = family;
                BuildFamilyHeader(family, matching.Count(a => a.AnimalData.animalFamily == family));
            }

            // Always built here, never from the scene prefab: that prefab has no happiness
            // bar and no frame reference wired, and it carries a dark brown Image on its
            // root under the cream card art — which, since the art paints only ~76% of the
            // rect, showed through as a brown band down the left of every row.
            var card = BuildCard(cardListContent);
            if (card == null) continue;

            var cardLayout = card.GetComponent<LayoutElement>();
            if (cardLayout == null) cardLayout = card.gameObject.AddComponent<LayoutElement>();
            cardLayout.preferredHeight = TeamAssemblerLayout.CardHeight;
            cardLayout.minHeight = TeamAssemblerLayout.CardHeight;
            // The art is 5:1 — a card wider than that stretches the middle of the 9-slice.
            // childForceExpandWidth would override preferredWidth, so the layout group has
            // it off and the card keeps the art's proportion whatever the panel width.
            cardLayout.preferredWidth = TeamAssemblerLayout.CardWidth;
            cardLayout.flexibleWidth = 0f;

            card.Initialize(animal);
            animalCards.Add(card);
        }
    }

    private void BuildFamilyHeader(string family, int count)
    {
        var theme = TeamAssemblerLayout.Theme;
        string label = string.IsNullOrEmpty(family)
            ? Localize("combat.teamassembler.family_other", "Other")
            : LocalizeFamily(family);

        var header = TeamAssemblerLayout.CreateLabel($"Family_{family}", cardListContent,
            $"{label}  ({count})",
            theme != null ? theme.fontSizeSmall : 14f,
            theme != null ? theme.headingOnLight : new Color(0.48f, 0.31f, 0.07f),
            TextAlignmentOptions.Left);
        header.fontStyle = FontStyles.Bold;

        var layout = header.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 26f;
        layout.minHeight = 26f;
        // Headers span the full list width even though the cards below them do not.
        layout.preferredWidth = TeamAssemblerLayout.CardWidth;
    }

    /// <summary>
    /// Build one animal card from the art.
    ///
    /// The root deliberately has NO Image of its own. The old card had a dark brown Image
    /// on the root with the cream card_animal art drawn on top of it, and since the art
    /// paints only ~76% of the rect width, the rest showed through as a brown band down
    /// the left of every row — the single biggest reason the list looked unfinished.
    /// </summary>
    private AnimalSelectionCard BuildCard(Transform parent)
    {
        var rt = TeamAssemblerLayout.CreateRect("AnimalCard", parent);

        var frame = TeamAssemblerLayout.CreateRect("CardBackgroundFrame", rt);
        var frameImage = frame.gameObject.AddComponent<Image>();
        frameImage.sprite = TeamAssemblerLayout.LoadSprite("Cards/card_animal");
        frameImage.type = Image.Type.Sliced;
        frameImage.color = Color.white;

        var portrait = TeamAssemblerLayout.CreateRect("Portrait", rt);
        TeamAssemblerLayout.SetSize(portrait, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(64f, 64f));
        var portraitImage = portrait.gameObject.AddComponent<Image>();
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;

        // Text sits to the right of the portrait, inside the painted area of the card.
        var theme = TeamAssemblerLayout.Theme;
        Color ink = theme != null ? theme.textDark : new Color(0.18f, 0.16f, 0.15f);

        var name = TeamAssemblerLayout.CreateLabel("NameText", rt, "",
            theme != null ? theme.fontSizeBody : 18f, ink, TextAlignmentOptions.TopLeft);
        TeamAssemblerLayout.SetSize(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 1f), new Vector2(104f, -14f), new Vector2(-150f, 26f));

        var happiness = TeamAssemblerLayout.CreateLabel("HappinessText", rt, "",
            theme != null ? theme.fontSizeCaption : 12f, ink, TextAlignmentOptions.TopLeft);
        TeamAssemblerLayout.SetSize(happiness.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 1f), new Vector2(104f, -40f), new Vector2(-150f, 20f));

        var bar = TeamAssemblerLayout.CreateRect("HappinessBar", rt);
        TeamAssemblerLayout.SetSize(bar, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(104f, -62f), new Vector2(150f, 8f));
        var barImage = bar.gameObject.AddComponent<Image>();
        barImage.type = Image.Type.Filled;
        barImage.fillMethod = Image.FillMethod.Horizontal;
        barImage.raycastTarget = false;

        var food = TeamAssemblerLayout.CreateLabel("FoodStatusText", rt, "",
            theme != null ? theme.fontSizeCaption : 12f, ink, TextAlignmentOptions.TopLeft);
        TeamAssemblerLayout.SetSize(food.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 1f), new Vector2(104f, -74f), new Vector2(-150f, 22f));

        var card = rt.gameObject.AddComponent<AnimalSelectionCard>();
        card.AssignBuiltReferences(frameImage, portraitImage, name, happiness, barImage, food);
        return card;
    }

    private void BuildProfileRow()
    {
        if (profileRow == null) return;

        for (int i = profileRow.childCount - 1; i >= 0; i--)
            DestroyImmediate(profileRow.GetChild(i).gameObject);

        var manager = TeamProfileManager.Instance;
        var theme = TeamAssemblerLayout.Theme;

        var label = TeamAssemblerLayout.CreateLabel("ProfileLabel", profileRow,
            Localize("combat.teamassembler.profiles", "Saved teams:"),
            theme != null ? theme.fontSizeCaption : 12f,
            theme != null ? theme.highlightGold : new Color(0.96f, 0.83f, 0.37f),
            TextAlignmentOptions.Right);
        var labelLayout = label.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 110f;
        labelLayout.minWidth = 110f;

        foreach (var profile in manager.Profiles.ToList())
        {
            var captured = profile;
            var button = TeamAssemblerLayout.CreateButton($"Profile_{profile.profileName}",
                profileRow, $"{profile.profileName} ({profile.Count})",
                "Buttons/button_secondary", () => OnProfileClicked(captured));

            // Same art-aware sizing as the footer buttons: the caption lives inside the
            // painted pill, which is narrower than the rect.
            var buttonLayout = button.gameObject.AddComponent<LayoutElement>();
            var buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);
            float textWidth = buttonLabel != null
                ? buttonLabel.GetPreferredValues(buttonLabel.text, Mathf.Infinity, Mathf.Infinity).x
                : 100f;
            buttonLayout.preferredWidth = Mathf.Clamp(
                textWidth / (1f - TeamAssemblerLayout.ButtonArtInset * 2f) + 48f, 150f, 320f);
            buttonLayout.minWidth = buttonLayout.preferredWidth;
            buttonLayout.preferredHeight = ProfileRowHeight;
            buttonLayout.minHeight = ProfileRowHeight;
        }

        if (manager.CanAddProfile)
        {
            var saveButton = TeamAssemblerLayout.CreateButton("SaveProfile", profileRow,
                Localize("combat.teamassembler.save_team", "+ Save current"),
                "Buttons/button_small_action", OnSaveProfileClicked);

            var saveLayout = saveButton.gameObject.AddComponent<LayoutElement>();
            var saveLabel = saveButton.GetComponentInChildren<TextMeshProUGUI>(true);
            float saveText = saveLabel != null
                ? saveLabel.GetPreferredValues(saveLabel.text, Mathf.Infinity, Mathf.Infinity).x
                : 120f;
            saveLayout.preferredWidth = Mathf.Clamp(
                saveText / (1f - TeamAssemblerLayout.ButtonArtInset * 2f) + 48f, 170f, 320f);
            saveLayout.minWidth = saveLayout.preferredWidth;
            saveLayout.preferredHeight = ProfileRowHeight;
            saveLayout.minHeight = ProfileRowHeight;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // REFRESH
    // ══════════════════════════════════════════════════════════════════════════

    private void RefreshEverything()
    {
        RefreshStaticLabels();
        RefreshHeader();
        RefreshCombatModeLabel();
        UpdateInfoDisplay();
    }

    private void RefreshHeader()
    {
        if (zoneNameText != null)
            zoneNameText.text = TeamAssemblerData.Instance.zoneName;
    }

    /// <summary>Refresh every card and slot, e.g. after feeding or clearing.</summary>
    private void RefreshAllCardsAndSlots()
    {
        foreach (var card in animalCards)
            if (card != null) card.RefreshCard();

        foreach (var slot in gridSlots)
            if (slot != null) slot.UpdateVisuals();
    }

    /// <summary>
    /// Update the briefing column: team size, food, enemies and live synergies.
    /// </summary>
    public void UpdateInfoDisplay()
    {
        var data = TeamAssemblerData.Instance;
        int teamSize = data.GetTeamSize();

        if (teamSizeText != null)
            teamSizeText.text = $"{Localize("combat.teamassembler.team", "Team")} {teamSize}/{gridSlots.Count}";

        RefreshFoodSummary();
        RefreshEnemyPreview();
        RefreshSynergyList();

        if (startBattleButton != null)
        {
            // Feeding no longer gates the battle; only an empty team does.
            startBattleButton.interactable = data.IsTeamValid();

            if (startBattleLabel != null)
            {
                int unfed = data.GetUnfedCount();
                // "(5!)" and not "(5 ⚠)": the project font has no glyph for U+26A0, so it
                // rendered as a tofu box. Same trap as the emoji in the string tables.
                string caption = unfed > 0
                    ? Localize("combat.teamassembler.start_battle_hungry", "Start Battle") + $" ({unfed}!)"
                    : Localize("combat.teamassembler.start_battle", "Start Battle");

                // Re-measure only when the caption actually changed: this runs on every
                // placement, and a forced layout rebuild per drag is wasted work.
                if (startBattleLabel.text != caption)
                {
                    startBattleLabel.text = caption;
                    ResizeButtonRow();
                }
            }
        }
    }

    private void RefreshFoodSummary()
    {
        if (foodSummaryText == null) return;

        var data = TeamAssemblerData.Instance;
        if (data.team.Count == 0)
        {
            foodSummaryText.text = "";
            return;
        }

        int unfed = data.GetUnfedCount();
        if (unfed == 0)
        {
            foodSummaryText.text = Localize("combat.teamassembler.all_fed", "All animals fed!");
            foodSummaryText.color = TeamAssemblerLayout.Theme != null
                ? TeamAssemblerLayout.Theme.positive : new Color(0.20f, 0.50f, 0.22f);
            return;
        }

        var requirements = data.GetTotalFoodRequirements();
        var lines = new List<string>
        {
            string.Format(
                Localize("combat.teamassembler.unfed_warning", "{0} unfed — they fight at -25%"),
                unfed)
        };

        foreach (var req in requirements)
        {
            Item item = ItemDatabase.GetItem(req.Key);
            string itemName = item != null ? item.GetDisplayName() : req.Key;
            lines.Add($"  {req.Value}x {itemName}");
        }

        foodSummaryText.text = string.Join("\n", lines);
        foodSummaryText.color = TeamAssemblerLayout.Theme != null
            ? TeamAssemblerLayout.Theme.warning : new Color(0.60f, 0.36f, 0.02f);
    }

    /// <summary>
    /// Show what the team is about to fight. Without this, building a team is guesswork
    /// and no synergy matters, because there is nothing to decide against.
    /// </summary>
    private void RefreshEnemyPreview()
    {
        if (enemyPreviewText == null) return;

        StageData stage = StageManager.GetSelectedStage();
        if (stage == null)
        {
            enemyPreviewText.text = "";
            return;
        }

        var lines = new List<string>
        {
            $"{Localize("combat.teamassembler.difficulty", "Difficulty")}: {stage.difficulty}",
            $"{Localize("combat.teamassembler.recommended", "Recommended")}: {stage.recommendedTeamSize}"
        };

        var enemyNames = stage.enemySpawns
            .Where(s => s != null && s.enemy != null)
            .Select(s => s.enemy.GetDisplayName())
            .Distinct()
            .Take(4)
            .ToList();

        if (enemyNames.Count > 0)
            lines.Add($"{Localize("combat.teamassembler.enemies", "Enemies")}: {string.Join(", ", enemyNames)}");

        if (stage.bossEnemy != null)
            lines.Add($"{Localize("combat.teamassembler.boss", "Boss")}: {stage.bossEnemy.GetDisplayName()}");

        enemyPreviewText.text = string.Join("\n", lines);
    }

    /// <summary>
    /// List the synergies that are actually active, from the same evaluator the battle
    /// uses. Inactive ones are listed greyed out with their requirement, so the player can
    /// see what they are one animal away from.
    /// </summary>
    private void RefreshSynergyList()
    {
        if (synergyListContent == null) return;

        for (int i = synergyListContent.childCount - 1; i >= 0; i--)
            DestroyImmediate(synergyListContent.GetChild(i).gameObject);

        var theme = TeamAssemblerLayout.Theme;
        var active = TeamAssemblerData.Instance.GetActiveSynergies();

        if (TeamAssemblerData.Instance.team.Count == 0)
        {
            AddSynergyRow(Localize("combat.teamassembler.no_synergies", "No synergies active"),
                "", false);
            return;
        }

        foreach (var synergy in active)
        {
            string name = LocalizeSynergyName(synergy);
            string effect = LocalizeSynergyDescription(synergy);
            AddSynergyRow(name, effect, true);
        }

        // Then what is still missing, so the panel gives advice rather than just a status.
        foreach (var hint in BuildSynergyHints(active))
            AddSynergyRow(hint, "", false);
    }

    private void AddSynergyRow(string title, string effect, bool isActive)
    {
        var theme = TeamAssemblerLayout.Theme;

        var row = TeamAssemblerLayout.CreateRect(isActive ? "Synergy" : "SynergyHint",
            synergyListContent);
        var rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = string.IsNullOrEmpty(effect) ? 22f : 40f;
        rowLayout.minHeight = rowLayout.preferredHeight;

        Color titleColor = isActive
            ? (theme != null ? theme.positive : new Color(0.20f, 0.50f, 0.22f))
            : new Color(0.45f, 0.42f, 0.38f);

        var titleLabel = TeamAssemblerLayout.CreateLabel("Title", row,
            (isActive ? "* " : "○ ") + title,
            theme != null ? theme.fontSizeSmall : 14f, titleColor, TextAlignmentOptions.TopLeft);
        TeamAssemblerLayout.SetSize(titleLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 20f));
        if (isActive) titleLabel.fontStyle = FontStyles.Bold;

        if (!string.IsNullOrEmpty(effect))
        {
            var effectLabel = TeamAssemblerLayout.CreateLabel("Effect", row, effect,
                theme != null ? theme.fontSizeCaption : 12f,
                theme != null ? theme.textDark : new Color(0.18f, 0.16f, 0.15f),
                TextAlignmentOptions.TopLeft);
            TeamAssemblerLayout.SetSize(effectLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(12f, -20f), new Vector2(-12f, 20f));
        }
    }

    /// <summary>
    /// Near-miss hints: which synergies the team does not have, and what it would take.
    /// </summary>
    private List<string> BuildSynergyHints(List<ActiveSynergy> active)
    {
        var hints = new List<string>();
        var team = TeamAssemblerData.Instance.team;
        if (team.Count == 0) return hints;

        var activeTypes = new HashSet<SynergyType>(active.Select(s => s.type));

        if (!activeTypes.Contains(SynergyType.Flock))
        {
            var best = team.Where(pa => pa.animalData != null)
                .GroupBy(pa => pa.animalData.animalFamily)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (best != null)
            {
                int missing = TeamSynergy.FlockMinCount - best.Count();
                if (missing > 0)
                {
                    hints.Add(string.Format(
                        Localize("combat.teamassembler.hint_flock", "{0} more {1} for Flock"),
                        missing, LocalizeFamily(best.Key)));
                }
            }
        }

        if (!activeTypes.Contains(SynergyType.MixedYard))
        {
            int families = team.Where(pa => pa.animalData != null)
                .Select(pa => pa.animalData.animalFamily).Distinct().Count();
            int missing = TeamSynergy.MixedYardMinFamilies - families;
            if (missing > 0)
            {
                hints.Add(string.Format(
                    Localize("combat.teamassembler.hint_mixed", "{0} more families for Mixed Yard"),
                    missing));
            }
        }

        if (!activeTypes.Contains(SynergyType.FrontLine))
        {
            hints.Add(Localize("combat.teamassembler.hint_front",
                "2 Tanks in the front column for Front Line"));
        }

        if (!activeTypes.Contains(SynergyType.WellCared))
        {
            hints.Add(string.Format(
                Localize("combat.teamassembler.hint_cared", "All at {0}+ happiness for Well Cared For"),
                Mathf.RoundToInt(TeamSynergy.WellCaredMinHappiness)));
        }

        if (!activeTypes.Contains(SynergyType.WellFed))
        {
            hints.Add(Localize("combat.teamassembler.hint_fed",
                "Feed each their favourite food for Well Fed"));
        }

        return hints;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ACTIONS
    // ══════════════════════════════════════════════════════════════════════════

    private void OnSearchChanged(string value)
    {
        searchFilter = value ?? "";
        BuildAnimalList();
        RefreshAllCardsAndSlots();
    }

    private bool MatchesSearch(Animal animal)
    {
        if (string.IsNullOrWhiteSpace(searchFilter)) return true;

        string needle = searchFilter.Trim();
        if (animal.GetDisplayName().IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        var data = animal.AnimalData;
        if (data == null) return false;

        return LocalizeFamily(data.animalFamily).IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0
            || (data.combatClass ?? "").IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void OnCombatModeToggleClicked()
    {
        var data = TeamAssemblerData.Instance;
        data.combatMode = data.combatMode == CombatMode.ActivePause
            ? CombatMode.Auto
            : CombatMode.ActivePause;

        RefreshCombatModeLabel();
    }

    private void RefreshCombatModeLabel()
    {
        if (combatModeLabel == null) return;

        bool activePause = TeamAssemblerData.Instance.combatMode == CombatMode.ActivePause;
        combatModeLabel.text = activePause
            ? Localize("combat.teamassembler.mode_active_pause", "Mode: Active Pause")
            : Localize("combat.teamassembler.mode_auto", "Mode: Auto");
    }

    private void OnProfileClicked(TeamProfile profile)
    {
        var result = TeamProfileManager.Instance.Apply(profile, availableAnimals);

        // Repaint the grid from the restored team.
        foreach (var slot in gridSlots)
        {
            if (slot == null) continue;
            slot.ClearVisualOnly();

            var member = TeamAssemblerData.Instance.GetPositionedAnimalAtPosition(slot.gridPosition);
            if (member == null) continue;

            var animal = availableAnimals.FirstOrDefault(
                a => a != null && TeamAssemblerData.GetAnimalId(a) == member.animalId);
            if (animal != null) slot.AdoptAnimal(animal);
        }

        if (result.missing > 0)
        {
            Debug.LogWarning($"[TeamAssembler] Profile '{profile.profileName}': " +
                $"{result.placed} placed, {result.missing} missing " +
                $"({string.Join(", ", result.missingNames)}).");
        }

        RefreshAllCardsAndSlots();
        RefreshEverything();
    }

    private void OnSaveProfileClicked()
    {
        if (TeamAssemblerData.Instance.team.Count == 0)
        {
            Debug.LogWarning("[TeamAssembler] Nothing to save — place animals on the grid first.");
            return;
        }

        string name = searchField != null && !string.IsNullOrWhiteSpace(searchField.text)
            ? searchField.text.Trim()
            : $"{Localize("combat.teamassembler.team", "Team")} {TeamProfileManager.Instance.Profiles.Count + 1}";

        var saved = TeamProfileManager.Instance.SaveCurrentTeam(name);
        if (saved == null)
        {
            Debug.LogWarning("[TeamAssembler] Could not save profile (limit reached or empty team).");
            return;
        }

        if (searchField != null) searchField.text = "";
        BuildProfileRow();
    }

    /// <summary>
    /// Feed every hungry team member, preferring each animal's favourite food when the
    /// player has it. Partial feeding is allowed — what can be fed is fed, rather than
    /// refusing the whole action because one item is short.
    /// </summary>
    private void OnFeedAllClicked()
    {
        var data = TeamAssemblerData.Instance;
        if (data.team.Count == 0)
        {
            Debug.LogWarning("[TeamAssembler] No animals in team — drag animals to the grid first.");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        var inventory = player != null ? player.GetComponent<Inventory.Inventory>() : null;
        if (inventory == null)
        {
            Debug.LogWarning("[TeamAssembler] Player inventory not found.");
            return;
        }

        int fed = 0;
        foreach (var member in data.team)
        {
            if (member.isFed || member.animalData == null) continue;

            // Favourite food first: it grants the bonus and counts toward Well Fed.
            string preferred = FoodPreference.GetPreferredFood(member.animalData);
            if (TryConsume(inventory, preferred, 1))
            {
                member.isFed = true;
                member.fedPreferredFood = true;
                fed++;
                continue;
            }

            // Otherwise anything the animal accepts keeps it at full strength.
            bool anyFed = false;
            foreach (var requirement in member.animalData.dailyFoodRequirements)
            {
                if (TryConsume(inventory, requirement.itemName, requirement.quantityPerDay))
                {
                    anyFed = true;
                    break;
                }
            }

            if (anyFed)
            {
                member.isFed = true;
                fed++;
            }
        }

        if (fed == 0)
            Debug.LogWarning("[TeamAssembler] No food available for the hungry animals.");

        RefreshAllCardsAndSlots();
        UpdateInfoDisplay();
    }

    /// <summary>Remove quantity of itemName from the inventory if it is all there.</summary>
    private bool TryConsume(Inventory.Inventory inventory, string itemName, int quantity)
    {
        if (string.IsNullOrEmpty(itemName) || quantity <= 0) return false;

        Item item = ItemDatabase.GetItem(itemName);
        if (item == null) return false;

        if (inventory.GetItemCount(item) < quantity) return false;

        inventory.RemoveItem(item, quantity);
        return true;
    }

    private void OnClearGridClicked()
    {
        TeamAssemblerData.Instance.ClearTeam();

        foreach (var slot in gridSlots)
            if (slot != null) slot.ClearSlot();

        RefreshAllCardsAndSlots();
        UpdateInfoDisplay();
    }

    private void OnStartBattleClicked()
    {
        var data = TeamAssemblerData.Instance;
        if (!data.IsTeamValid())
        {
            Debug.LogWarning("[TeamAssembler] Cannot start: no animals in team.");
            return;
        }

        data.SaveToPrefs();

        // The farm scene reloads on return from combat and rebuilds the Inventory, which
        // otherwise comes back empty when no disk save is available (demo builds).
        Inventory.InventorySceneSnapshot.Capture(FindFirstObjectByType<Inventory.Inventory>());

        // Capture every ISaveable (purchased animals included) before the scene unloads.
        if (SaveManager.Instance != null)
            SaveManager.Instance.CaptureRegisteredObjectsIntoCurrentGameData();

        // WorldMap sets timeScale to 0; without this the combat scene loads frozen.
        Time.timeScale = 1f;
        SceneManager.LoadScene(combatSceneName);
    }

    private void OnCancelClicked() => CloseAssembler();

    // ══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    public GridPositionSlot GetSlotAtPosition(Vector2Int position)
        => gridSlots.Find(slot => slot.gridPosition == position);

    private void DisablePlayerMovement()
        => FindFirstObjectByType<PlayerMove>()?.DisableMovement();

    private void EnablePlayerMovement()
        => FindFirstObjectByType<PlayerMove>()?.EnableMovement();

    /// <summary>
    /// Resolve a Combat-table key, falling back to English. Built at call time rather than
    /// held in a serialized field: this screen is constructed in code, so there is no
    /// Inspector to wire LocalizedStrings into.
    /// </summary>
    private static string Localize(string key, string fallback)
    {
        var localized = new LocalizedString("Combat", key);
        string value = localized.SafeGetLocalizedString();
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    /// <summary>Family names are data, not UI strings; localize when a key exists.</summary>
    private static string LocalizeFamily(string family)
    {
        if (string.IsNullOrEmpty(family)) return "";
        string value = new LocalizedString("Animals", $"animals.family.{family.ToLowerInvariant()}")
            .SafeGetLocalizedString();
        return string.IsNullOrEmpty(value) ? family : value;
    }

    private static string LocalizeSynergyName(ActiveSynergy synergy)
    {
        string name = new LocalizedString("Combat", TeamSynergy.NameKey(synergy.type))
            .SafeGetLocalizedString();
        if (string.IsNullOrEmpty(name)) name = TeamSynergy.FallbackName(synergy.type);

        // Flock names the family it applies to; the others are team-wide.
        if (synergy.type == SynergyType.Flock && !string.IsNullOrEmpty(synergy.subject))
            return $"{name}: {LocalizeFamily(synergy.subject)} ×{synergy.count}";

        return name;
    }

    private static string LocalizeSynergyDescription(ActiveSynergy synergy)
    {
        string desc = new LocalizedString("Combat", TeamSynergy.DescriptionKey(synergy.type))
            .SafeGetLocalizedString();
        return string.IsNullOrEmpty(desc) ? TeamSynergy.FallbackDescription(synergy) : desc;
    }
}

} // namespace SowurShield.Combat
