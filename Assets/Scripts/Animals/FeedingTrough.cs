using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization;
using SowurShield.Core;
using SowurShield.Inventory;
using SowurShield.Inventory.Policies;
using SowurShield.UI;

namespace SowurShield.Animals
{

/// <summary>
/// A placeable world object that stores food and auto-feeds animals in its linked AnimalZone.
/// Modeled after SellBox — implements IInteractable (E key), IUIWindow (panel management),
/// and ISaveable (persistence). Uses InventoryContainer for internal food storage.
///
/// Auto-feeding occurs on day change: for each animal in the zone, the trough removes
/// matching food items and calls Animal.AutoFeed().
/// </summary>
public class FeedingTrough : MonoBehaviour, IInteractable, IUIWindow, ISaveable
{
    [Header("Zone Link")]
    [SerializeField] private AnimalZone linkedZone;

    [Header("Storage")]
    [SerializeField] private int slotCount = 12;

    [Header("Visual Sprites")]
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite partialSprite;
    [SerializeField] private Sprite fullSprite;

    [Header("UI References")]
    [SerializeField] private GameObject troughPanel;
    [SerializeField] private Transform slotParent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button closeButton;

    [Header("Interaction")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private string interactionPrompt = "Open Feeding Trough";

    [Header("Localization")]
    [SerializeField] private LocalizedString troughTitleText; // table "Animals", key "animals.trough.title"
    [SerializeField] private LocalizedString troughStatusText; // table "Animals", key "animals.trough.status"

    // Internal storage
    private InventoryContainer container;
    private SpriteRenderer spriteRenderer;
    private bool isOpen = false;

    // Slot UI is owned by ContainerView (Etapa 3 of review/04_CONTAINER_REFACTOR_PLAN.md).
    // The component is added at runtime rather than wired in the Inspector so this migration
    // needs no scene changes — the trough already holds slotParent/slotPrefab and hands them over.
    private ContainerView view;
    private FeedingTroughPolicy policy;

    // =========================================================================
    // IUIWindow Implementation
    // =========================================================================

    public string WindowName => "FeedingTrough";
    public int WindowPriority => SowurShield.Core.WindowPriority.SellBox; // Same tier as SellBox (20)
    public bool IsWindowOpen => isOpen;

    // ESC is reserved for the pause menu; close this window with E (Interact) instead.
    public bool CanCloseWithEsc => false;

    public void OpenWindow()
    {
        if (troughPanel != null)
            troughPanel.SetActive(true);
        isOpen = true;

        // Re-tint the slot wells on every open. Doing it once in SetupUI is not enough: each
        // InventorySlot repaints its own Background when it initialises, which happens after
        // SetupUI has run, so the styling applied there was immediately overwritten.
        StyleSlotBackgrounds(UIThemeStyler.LoadTheme());

        // O empilhamento tem de correr com o painel ATIVO. ApplyTheme roda no SetupUI, e
        // ali o painel ainda esta desligado: o Unity nao faz passe de layout em objeto
        // inativo, entao StackBetween lia rect.height/width desatualizados e punha o
        // status fora do painel e o titulo por cima da grade. Medido: status em y
        // 848..880 num painel que acaba em 736.
        // O texto ANTES do layout: StackBetween posiciona o status a partir da altura do
        // proprio rect, e com o texto do frame anterior essa altura esta errada.
        UpdateStatusText();
        LayoutPanel();

        DisablePlayerMovement();
    }

    /// <summary>
    /// Recoloca titulo, grade, status e botao dentro da moldura.
    ///
    /// Separado do ApplyTheme (que trata de cores e sprites) porque so pode correr com o
    /// painel ativo — ver o comentario no OpenWindow.
    /// </summary>
    private void LayoutPanel()
    {
        var panelRect = troughPanel != null ? troughPanel.GetComponent<RectTransform>() : null;
        if (panelRect == null) return;

        // Mesmo recuo que o ApplyTheme usa, para os dois nao discordarem.
        float inset = Mathf.Round(panelRect.rect.width * 0.125f) + 12f;

        InsetFromFrame(titleText != null ? titleText.rectTransform : null, inset, fromTop: true);
        InsetFromFrame(statusText != null ? statusText.rectTransform : null, inset, fromTop: false);
        InsetFromFrame(closeButton != null ? closeButton.GetComponent<RectTransform>() : null,
                       inset, fromTop: false);
        FitSlotGrid();
        StackBetween(inset);
    }

    public void CloseWindow()
    {
        if (troughPanel != null)
            troughPanel.SetActive(false);
        isOpen = false;

        EnablePlayerMovement();
    }

    public void OnWindowBlocked(string blockedBy) { }

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        container = new InventoryContainer(slotCount, $"FeedingTrough_{gameObject.name}");

        // RejectNonFood stays false: the trough accepts anything today and simply never consumes
        // what no animal eats. Changing that is a gameplay decision, not part of this refactor.
        policy = new FeedingTroughPolicy(GetAcceptedFood);

        // The view handles the slot UI. This handler covers what is the trough's own business,
        // and stays subscribed directly to the container because it must also run before the
        // slot UI exists — LoadData writes slots during Start, ahead of SetupUI.
        container.OnSlotChanged += HandleContainerChanged;

        if (troughPanel != null)
            troughPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseTrough);
    }

    private void Start()
    {
        // Register with InteractionManager
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.RegisterInteractable(this);

        // Register with UIManager
        if (UIManager.Instance != null)
            UIManager.Instance.RegisterWindow(this);

        // Register with SaveManager
        if (SaveManager.Instance != null)
            SaveManager.Instance.RegisterSaveable(this);

        // Subscribe to day changes for auto-feeding
        if (GameTimeController.instance != null)
            GameTimeController.instance.OnDayChanged += OnDayChanged;

        SetupUI();
        UpdateTroughSprite();

        SowurShield.Core.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        if (titleText != null)
            titleText.text = troughTitleText.SafeGetLocalizedString();
        UpdateStatusText();
    }

    /// <summary>Reacts to the container changing. Slot UI is the view's job; this is ours.</summary>
    private void HandleContainerChanged(int index, ItemStack stack)
    {
        UpdateTroughSprite();
        UpdateStatusText();
    }

    private void SetupUI()
    {
        if (view != null && view.IsBuilt) return;

        if (slotParent == null || slotPrefab == null)
        {
            Debug.LogWarning("[FeedingTrough] SlotParent or SlotPrefab not assigned — slots won't be created.");
            return;
        }

        if (view == null)
            view = gameObject.AddComponent<ContainerView>();

        view.Configure(slotParent, slotPrefab, "TroughSlot");
        // No per-slot configuration needed: the slot's OwnerView is what identifies it now.
        view.Bind(container, policy);

        if (titleText != null)
            titleText.text = troughTitleText.SafeGetLocalizedString();

        ApplyTheme();
        UpdateStatusText();
    }

    /// <summary>
    /// Give the trough the same wooden frame every other window in the game wears.
    ///
    /// It was the odd one out: a "Dialogue Box" sprite tinted near-black brown and drawn
    /// Simple, so the painted frame in that art was flattened into a plain rectangle. Beside
    /// the inventory and the codex it read as a debug placeholder, which is what the audit
    /// flagged. Restyling here rather than in the scene keeps it working if the panel is ever
    /// rebuilt, and matches how SellBox and the battle UI adopt the theme.
    /// </summary>
    private void ApplyTheme()
    {
        if (troughPanel == null) return;

        var theme = UIThemeStyler.LoadTheme();
        UIThemeStyler.StylePanel(troughPanel, theme);

        // panel_wood_generic paints a frame over roughly an eighth of its width per side, and
        // that band SCALES with the panel. Content laid out to the rect edge ends up sitting on
        // the wood — the codex defect from earlier today.
        //
        // There is no layout group here to hold padding: every child is anchored by hand, so
        // the inset has to be applied to the children themselves. The panel also grows, because
        // insetting a 530px panel by 66 a side would leave the eight-wide slot row no room.
        var panelRect = troughPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            const float TargetWidth = 720f;
            const float TargetHeight = 560f;
            if (panelRect.rect.width < TargetWidth)
                panelRect.sizeDelta = new Vector2(TargetWidth, TargetHeight);

            // Um primeiro posicionamento aqui deixa o painel apresentavel se algo o
            // mostrar sem passar pelo OpenWindow. O que vale e a chamada de la, com o
            // painel ja ativo: com ele desligado o Unity nao faz passe de layout e as
            // medidas lidas aqui sao as do editor, nao as reais.
            LayoutPanel();
        }

        // This panel is rendered at half scale, so a theme size set here lands on screen at
        // half its value — 24pt reads as 12px. Divide the scale back out so the trough's text
        // matches the rest of the UI optically rather than numerically.
        float scale = troughPanel.transform.lossyScale.x;
        float sizeFactor = scale > 0.01f ? 1f / scale : 1f;

        if (titleText != null)
        {
            titleText.fontSize = (theme != null ? theme.fontSizeH2 : 24f) * sizeFactor;
            titleText.color = theme != null ? theme.textDark : new Color(0.176f, 0.165f, 0.149f);
        }

        if (statusText != null)
        {
            // Small, not the 24pt it had: this is supporting text under the slots, and the
            // panel interior is cream so it needs dark ink rather than the pale tan it used.
            statusText.fontSize = (theme != null ? theme.fontSizeSmall : 14f) * sizeFactor;
            statusText.color = theme != null ? theme.textDark : new Color(0.176f, 0.165f, 0.149f);
        }

        StyleSlotBackgrounds(theme);

        if (closeButton != null)
        {
            // Was a flat dark-red rectangle, the only one of its kind in the game. StyleButton
            // gives it the shared gold art and darkens the label to suit it.
            UIThemeStyler.StyleButton(closeButton, theme);
            var closeLabel = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (closeLabel != null)
                closeLabel.fontSize = theme != null ? theme.fontSizeButton : 18f;
        }
    }

    /// <summary>
    /// Positions the slot grid and the status line in the gap between the title and the close
    /// button, with a consistent gutter, so nothing overlaps its neighbour.
    /// </summary>
    private void StackBetween(float inset)
    {
        var panelRect = troughPanel.GetComponent<RectTransform>();
        var gridRect = slotParent as RectTransform;
        if (panelRect == null || gridRect == null) return;

        float gutter = 24f;
        float altura = panelRect.rect.height;

        // Converte o anchoredPosition de qualquer filho para o CENTRO do painel. Os filhos
        // aqui nao compartilham ancora (a grade e forcada ao topo, o titulo e o botao vem
        // da cena com as suas), e anchoredPosition e sempre medido a partir da ancora do
        // proprio filho -- somar valores de ancoras diferentes compara coisas distintas.
        System.Func<RectTransform, float> centroDe = r =>
            r.anchoredPosition.y + (r.anchorMin.y - 0.5f) * altura;

        // Title occupies the top band; the grid starts one gutter below it.
        float titleBottom = titleText != null
            ? centroDe(titleText.rectTransform) - titleText.rectTransform.rect.height * 0.5f
            : altura * 0.5f - inset;

        // Height comes from the grid's own contents, not from rect.height: FitSlotGrid has just
        // written sizeDelta and the rect does not reflect it until the next layout pass, so
        // reading rect.height here returns the stale (larger) authored value and the grid ends
        // up mispositioned and oversized.
        float gridHeight = gridRect.sizeDelta.y;

        // A grade cede o espaco de que o resto precisa. Sem isto ela ficava com a altura
        // ideal das suas linhas e o status -- que vem depois -- nao tinha onde caber:
        // sobravam 27px entre a grade e o botao para uma linha de 38 mais dois respiros,
        // o limite inferior vencia e o status subia por cima da grade. Reduzir a grade e
        // preferivel a sobrepor: as celulas encolhem e continuam legiveis.
        if (statusText != null && closeButton != null)
        {
            statusText.ForceMeshUpdate();
            float alturaStatus = Mathf.Max(statusText.preferredHeight, 24f);
            var closeRect = closeButton.GetComponent<RectTransform>();
            float closeTopo = centroDe(closeRect) + closeRect.rect.height * 0.5f;

            // Do fundo do titulo ate o topo do botao, descontando status e os tres respiros.
            float disponivel = (titleBottom - gutter) - (closeTopo + gutter)
                               - alturaStatus - gutter;
            if (disponivel > 0f && gridHeight > disponivel)
            {
                gridHeight = disponivel;
                gridRect.sizeDelta = new Vector2(gridRect.sizeDelta.x, gridHeight);
                ReflowGridCells(gridHeight);
            }
        }

        // A grade passa a viver ancorada ao topo, entao o alvo (calculado no centro) volta
        // a ser expresso no referencial dessa ancora nova.
        gridRect.anchorMin = gridRect.anchorMax = new Vector2(0.5f, 1f);
        float gridCentroAlvo = titleBottom - gutter - gridHeight * 0.5f;
        gridRect.anchoredPosition = new Vector2(
            gridRect.anchoredPosition.x,
            gridCentroAlvo - 0.5f * altura);

        // Status sits between the grid and the close button.
        //
        // It used to be placed purely from the button upwards, which never looked at where the
        // grid had actually ended. With 12 slots the grid reached y 485..610 while the status
        // sat at 470..525 — the grid covered the line, so "Comida armazenada / Pode alimentar"
        // was invisible in game with no error of any kind. Anchoring it below the grid keeps the
        // reading order (title, grid, status, button) whatever the row count turns out to be.
        if (statusText != null)
        {
            var statusRect = statusText.rectTransform;

            // A altura vem do TEXTO, nao do rect da cena. O rect trazia 110px (folga para
            // duas linhas grandes) enquanto as duas linhas reais ocupam ~50: com 110 o
            // status nao cabia entre a grade e o botao, o limite inferior vencia e ele
            // subia por cima da grade -- que e exatamente como a linha ficava invisivel.
            statusText.ForceMeshUpdate();
            float alturaTexto = Mathf.Max(statusText.preferredHeight, 24f);
            statusRect.sizeDelta = new Vector2(statusRect.sizeDelta.x, alturaTexto);

            float gridBottom = centroDe(gridRect) - gridHeight * 0.5f;
            float alvoCentro = gridBottom - gutter - alturaTexto * 0.5f;

            // Nunca abaixo do botao: numa janela apertada e melhor encostar que sobrepor.
            if (closeButton != null)
            {
                var closeRect = closeButton.GetComponent<RectTransform>();
                float closeTopo = centroDe(closeRect) + closeRect.rect.height * 0.5f;
                float minimo = closeTopo + gutter + alturaTexto * 0.5f;
                if (alvoCentro < minimo) alvoCentro = minimo;
            }

            // De volta ao referencial da ancora do proprio status.
            float alvoLocal = alvoCentro - (statusRect.anchorMin.y - 0.5f) * altura;
            statusRect.anchoredPosition = new Vector2(statusRect.anchoredPosition.x, alvoLocal);
        }
    }

    /// <summary>
    /// Darkens the slot wells so they read as slots.
    ///
    /// The slot prefab paints its Background white at 52% alpha, which was fine over the old
    /// near-black panel but disappears on the wooden panel's cream interior — the grid became
    /// twelve invisible squares. A tan well with a solid alpha gives each slot a visible edge
    /// against the cream, the same way the inventory reads.
    /// </summary>
    private void StyleSlotBackgrounds(UITheme theme)
    {
        if (slotParent == null) return;

        Color well = theme != null ? theme.backgroundTan : new Color(0.937f, 0.890f, 0.753f);
        well = new Color(well.r * 0.82f, well.g * 0.80f, well.b * 0.74f, 1f);

        foreach (Transform slot in slotParent)
        {
            var background = slot.Find("Background");
            var image = background != null ? background.GetComponent<Image>() : null;
            if (image != null) image.color = well;
        }
    }

    /// <summary>
    /// Sizes the slot grid to the width it actually has.
    ///
    /// The grid was authored as 8 columns of 112px, which needs 966px — but once the content
    /// is inset past the wooden frame only 772px remain, so the right-hand slots hung outside
    /// the panel. Slots are re-flowed to a column count that fits and squared off so the 12
    /// slots fill whole rows rather than leaving a ragged tail.
    /// </summary>
    private void FitSlotGrid()
    {
        var grid = slotParent != null ? slotParent.GetComponent<GridLayoutGroup>() : null;
        var gridRect = slotParent as RectTransform;
        if (grid == null || gridRect == null) return;

        const int Columns = 6;   // 12 slots = 2 full rows
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;

        // Narrow the grid to the frame-free width first. This is not done through
        // InsetFromFrame because the grid's vertical placement is decided later by
        // StackBetween, and letting both touch anchoredPosition made them fight.
        var panelRect = troughPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            float inset = Mathf.Round(panelRect.rect.width * 0.125f) + 12f;
            float maxWidth = panelRect.rect.width - inset * 2f;

            // Largura ATRIBUIDA, nao apenas limitada. A comparacao ">" sozinha nunca
            // corrigia uma largura errada para MENOS: a cena trazia sizeDelta.x = -296
            // (o rect nasce com ancoras de ponto e offsets de um layout esticado), e um
            // valor negativo passa direto por "> maxWidth" — a grade ficava com os
            // cantos invertidos, x de 1021 a 873, e as celulas caiam fora do painel.
            gridRect.sizeDelta = new Vector2(maxWidth, gridRect.sizeDelta.y);
        }

        // sizeDelta, not rect.width: InsetFromFrame has just narrowed this rect and the change
        // is not visible through rect until the next layout pass.
        float width = gridRect.sizeDelta.x > 0f ? gridRect.sizeDelta.x : gridRect.rect.width;
        float available = width - grid.padding.left - grid.padding.right;
        float cell = Mathf.Floor((available - grid.spacing.x * (Columns - 1)) / Columns);
        if (cell > 0f) grid.cellSize = new Vector2(cell, cell);

        // Height follows from the rows actually needed, so the panel does not reserve space for
        // a third row that never exists.
        int rows = Mathf.CeilToInt(slotCount / (float)Columns);
        float neededHeight = rows * cell + grid.spacing.y * (rows - 1)
                           + grid.padding.top + grid.padding.bottom;
        gridRect.sizeDelta = new Vector2(gridRect.sizeDelta.x, neededHeight);
    }

    /// <summary>
    /// Reencolhe as celulas para caberem numa altura imposta de fora.
    ///
    /// O FitSlotGrid escolhe a celula pela LARGURA e deixa a altura seguir dela. Quando o
    /// StackBetween descobre que essa altura nao cabe entre o titulo e o botao, a grade
    /// tem de encolher — e a celula junto, senao as linhas transbordam do rect.
    /// </summary>
    private void ReflowGridCells(float alturaDisponivel)
    {
        var grid = slotParent != null ? slotParent.GetComponent<GridLayoutGroup>() : null;
        if (grid == null || grid.constraintCount <= 0) return;

        int rows = Mathf.CeilToInt(slotCount / (float)grid.constraintCount);
        if (rows <= 0) return;

        float util = alturaDisponivel - grid.padding.top - grid.padding.bottom
                     - grid.spacing.y * (rows - 1);
        float cell = Mathf.Floor(util / rows);
        if (cell > 0f) grid.cellSize = new Vector2(cell, cell);
    }

    /// <summary>
    /// Pushes one anchored child clear of the panel's painted frame, keeping its distance from
    /// whichever edge it is anchored to. Children here sit on point anchors rather than in a
    /// layout group, so this adjusts each one individually.
    /// </summary>
    private static void InsetFromFrame(RectTransform child, float inset, bool fromTop)
    {
        if (child == null) return;

        // Width: leave the frame clear on both sides. Anything anchored to the horizontal
        // centre keeps its centre; only its width shrinks.
        float panelWidth = ((RectTransform)child.parent).rect.width;
        float maxWidth = panelWidth - inset * 2f;
        if (child.sizeDelta.x > maxWidth)
            child.sizeDelta = new Vector2(maxWidth, child.sizeDelta.y);

        // Vertical: the anchor tells us which edge the offset is measured from, so a top-anchored
        // title moves down and a bottom-anchored button moves up.
        float halfHeight = child.rect.height * 0.5f;
        float limit = inset + halfHeight;
        Vector2 pos = child.anchoredPosition;

        if (fromTop && pos.y > -limit) pos.y = -limit;
        else if (!fromTop && pos.y < limit) pos.y = limit;

        child.anchoredPosition = pos;
    }

    /// <summary>
    /// Every item animals in the linked zone eat. Feeds FeedingTroughPolicy — only consulted
    /// while RejectNonFood is on, which it is not by default.
    /// </summary>
    private IEnumerable<Item> GetAcceptedFood()
    {
        if (linkedZone == null) yield break;

        foreach (Animal animal in linkedZone.GetAnimals())
        {
            AnimalData data = animal != null ? animal.AnimalData : null;
            if (data?.dailyFoodRequirements == null) continue;

            foreach (FoodRequirement req in data.dailyFoodRequirements)
            {
                if (string.IsNullOrEmpty(req.itemName)) continue;

                Item food = ItemDatabase.GetItem(req.itemName);
                if (food != null) yield return food;
            }
        }
    }

    private void OnDestroy()
    {
        if (InteractionManager.Instance != null)
            InteractionManager.Instance.UnregisterInteractable(this);

        if (UIManager.Instance != null)
            UIManager.Instance.UnregisterWindow(this);

        if (SaveManager.Instance != null)
            SaveManager.Instance.UnregisterSaveable(this);

        if (GameTimeController.instance != null)
            GameTimeController.instance.OnDayChanged -= OnDayChanged;

        if (container != null)
            container.OnSlotChanged -= HandleContainerChanged;

        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    // =========================================================================
    // IInteractable Implementation
    // =========================================================================

    public void Interact()
    {
        if (isOpen)
        {
            CloseTrough();
            return;
        }

        OpenTrough();
    }

    public string GetInteractionPrompt() => interactionPrompt;
    public float GetInteractionRange() => interactionRange;
    public bool CanInteract() => !isOpen;

    // =========================================================================
    // Open / Close
    // =========================================================================

    private void OpenTrough()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TryOpenWindow(this);
        }
        else
        {
            OpenWindow();
        }
    }

    public void CloseTrough()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TryCloseWindow(this);
        else
            CloseWindow();
    }

    // =========================================================================
    // Auto-Feeding System
    // =========================================================================

    /// <summary>
    /// Called on each new day. Attempts to auto-feed all animals in the linked zone
    /// using food stored in this trough.
    /// </summary>
    private void OnDayChanged()
    {
        if (linkedZone == null) return;

        List<Animal> animals = linkedZone.GetAnimals();
        if (animals.Count == 0) return;

        int totalFed = 0;

        foreach (Animal animal in animals)
        {
            if (animal == null || animal.AnimalData == null) continue;

            int fedAmount = TryFeedAnimal(animal);
            if (fedAmount > 0)
            {
                animal.AutoFeed(fedAmount);
                totalFed++;
            }
        }

        UpdateTroughSprite();
        UpdateStatusText();
    }

    /// <summary>
    /// Attempt to feed a single animal from the trough's container.
    /// Returns the number of food items consumed (to pass to AutoFeed).
    /// </summary>
    private int TryFeedAnimal(Animal animal)
    {
        AnimalData data = animal.AnimalData;
        if (data.dailyFoodRequirements == null || data.dailyFoodRequirements.Count == 0)
            return 0;

        int totalFed = 0;

        foreach (FoodRequirement req in data.dailyFoodRequirements)
        {
            if (string.IsNullOrEmpty(req.itemName)) continue;

            // Look up the food item
            Item foodItem = ItemDatabase.GetItem(req.itemName);
            if (foodItem == null) continue;

            // Try to remove the required quantity from the trough
            int amountNeeded = req.quantityPerDay;
            int amountAvailable = container.GetItemCount(foodItem);
            int amountToFeed = Mathf.Min(amountNeeded, amountAvailable);

            if (amountToFeed > 0 && container.RemoveItem(foodItem, amountToFeed))
            {
                totalFed += amountToFeed;
            }
        }

        return totalFed;
    }

    /// <summary>
    /// Preview how many animals would be fed with current trough contents.
    /// Used by SleepConfirmationPanel.
    /// </summary>
    public int GetFeedableAnimalCount()
    {
        if (linkedZone == null) return 0;

        List<Animal> animals = linkedZone.GetAnimals();
        int count = 0;

        // Build temp counts using the actual item references from the container
        Dictionary<Item, int> tempCounts = new Dictionary<Item, int>();
        foreach (var stack in container.GetAllItems())
        {
            if (stack != null && !stack.IsEmpty)
            {
                if (tempCounts.ContainsKey(stack.item))
                    tempCounts[stack.item] += stack.quantity;
                else
                    tempCounts[stack.item] = stack.quantity;
            }
        }

        foreach (Animal animal in animals)
        {
            if (animal == null || animal.AnimalData == null) continue;

            AnimalData data = animal.AnimalData;
            if (data.dailyFoodRequirements == null) continue;

            bool canFeed = true;
            foreach (FoodRequirement req in data.dailyFoodRequirements)
            {
                if (string.IsNullOrEmpty(req.itemName)) continue;

                // Resolve via ItemDatabase, same as TryFeedAnimal
                Item foodItem = ItemDatabase.GetItem(req.itemName);
                if (foodItem == null) { canFeed = false; break; }

                int available = tempCounts.ContainsKey(foodItem) ? tempCounts[foodItem] : 0;
                if (available < req.quantityPerDay)
                {
                    canFeed = false;
                    break;
                }
            }

            if (canFeed)
            {
                count++;
                // Deduct from temp counts
                foreach (FoodRequirement req in data.dailyFoodRequirements)
                {
                    if (string.IsNullOrEmpty(req.itemName)) continue;
                    Item foodItem = ItemDatabase.GetItem(req.itemName);
                    if (foodItem != null && tempCounts.ContainsKey(foodItem))
                        tempCounts[foodItem] -= req.quantityPerDay;
                }
            }
        }

        return count;
    }

    /// <summary>Total number of animals in the linked zone.</summary>
    public int GetTotalAnimalCount()
    {
        return linkedZone != null ? linkedZone.GetAnimals().Count : 0;
    }

    // =========================================================================
    // Visual Feedback
    // =========================================================================

    private void UpdateTroughSprite()
    {
        if (spriteRenderer == null) return;

        int totalItems = 0;
        foreach (var stack in container.GetAllItems())
        {
            if (stack != null && !stack.IsEmpty)
                totalItems += stack.quantity;
        }

        int occupiedSlots = 0;
        foreach (var stack in container.GetAllItems())
            if (stack != null && !stack.IsEmpty) occupiedSlots++;

        if (totalItems == 0 && emptySprite != null)
            spriteRenderer.sprite = emptySprite;
        else if (occupiedSlots >= slotCount / 2 && fullSprite != null)
            spriteRenderer.sprite = fullSprite;
        else if (partialSprite != null)
            spriteRenderer.sprite = partialSprite;
    }

    private void UpdateStatusText()
    {
        if (statusText == null) return;

        int totalItems = 0;
        foreach (var stack in container.GetAllItems())
        {
            if (stack != null && !stack.IsEmpty)
                totalItems += stack.quantity;
        }

        int feedable = GetFeedableAnimalCount();
        int total = GetTotalAnimalCount();

        troughStatusText.Arguments = new object[] { totalItems, feedable, total };
        statusText.text = troughStatusText.SafeGetLocalizedString();
    }

    // =========================================================================
    // Player Movement Helpers
    // =========================================================================

    private void DisablePlayerMovement()
    {
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        if (player != null)
            player.DisableMovement();
    }

    private void EnablePlayerMovement()
    {
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        if (player != null)
            player.EnableMovement();
    }

    // =========================================================================
    // ISaveable Implementation
    // =========================================================================

    // Save version 2: the per-slot worldStrings/worldCounters loop this used to carry is gone,
    // replaced by the shared container format. See ContainerPersistence.
    //
    // Behaviour note: the old loader used AddItem, so items landed wherever they fit rather than
    // back in the slot they were saved from. The shared format restores exact indices.

    public void SaveData(GameData gameData)
    {
        ContainerPersistence.Save(gameData, container);
    }

    public void LoadData(GameData gameData)
    {
        ContainerPersistence.Load(gameData, container);

        UpdateTroughSprite();
        UpdateStatusText();
    }

    // =========================================================================
    // Public Accessors
    // =========================================================================

    /// <summary>The internal food container for direct access (used by UI slots).</summary>
    public InventoryContainer Container => container;

    /// <summary>The linked AnimalZone.</summary>
    public AnimalZone LinkedZone => linkedZone;
}

} // namespace SowurShield.Animals
