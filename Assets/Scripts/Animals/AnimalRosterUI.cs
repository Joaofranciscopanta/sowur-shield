using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization;
using SowurShield.Core;
using SowurShield.UI;

namespace SowurShield.Animals
{

/// <summary>
/// UI panel displaying all registered animals from AnimalRoster.
/// Shows animals grouped by zone, with each card displaying: icon, name,
/// happiness bar (mini), and food status. Clicking a card opens AnimalInfoUI.
///
/// Implements IUIWindow for proper UIManager integration.
/// </summary>
public class AnimalRosterUI : MonoBehaviour, IUIWindow
{
    [Header("Panel")]
    [SerializeField] private GameObject rosterPanel;
    [SerializeField] private Button closeButton;

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI headerText;

    [Header("Content")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject zoneHeaderPrefab;
    [SerializeField] private GameObject animalCardPrefab;

    [Header("Footer / Summary")]
    [SerializeField] private TextMeshProUGUI summaryText;

    [Header("Colors")]
    [SerializeField] private Color happyColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color neutralColor = new Color(0.8f, 0.8f, 0.3f);
    [SerializeField] private Color sadColor = new Color(0.8f, 0.3f, 0.3f);
    [SerializeField] private Color fedColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color hungryTextColor = new Color(0.8f, 0.3f, 0.3f);

    [Header("Theme")]
    [SerializeField] private UITheme theme;

    [Header("Localization")]
    [SerializeField] private LocalizedString groupHeaderText; // table "Animals", key "animals.roster.group_header"
    [SerializeField] private LocalizedString moodHappyText; // table "Animals", key "animals.roster.mood_happy"
    [SerializeField] private LocalizedString moodNeutralText; // table "Animals", key "animals.roster.mood_neutral"
    [SerializeField] private LocalizedString moodSadText; // table "Animals", key "animals.roster.mood_sad"
    [SerializeField] private LocalizedString hungryText; // table "Animals", key "animals.roster.hungry"
    [SerializeField] private LocalizedString fedText; // table "Animals", key "animals.roster.fed"
    [SerializeField] private LocalizedString rowStatusText; // table "Animals", key "animals.roster.row_status"
    [SerializeField] private LocalizedString rosterTitleText; // table "Animals", key "animals.roster.title"
    [SerializeField] private LocalizedString rosterEmptyText; // table "Animals", key "animals.roster.empty"
    [SerializeField] private LocalizedString avgHappinessText; // table "Animals", key "animals.roster.avg_happiness"
    [SerializeField] private LocalizedString unassignedText; // table "Animals", key "animals.market.unassigned"

    private Color cardBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    private bool isOpen = false;
    private List<GameObject> spawnedUIElements = new List<GameObject>();

    // =========================================================================
    // IUIWindow Implementation
    // =========================================================================

    public string WindowName => "AnimalRoster";
    public int WindowPriority => SowurShield.Core.WindowPriority.Inventory; // Same tier as AnimalInfoUI
    public bool IsWindowOpen => isOpen;
    public bool CanCloseWithEsc => true;

    public void OpenWindow()
    {
        if (rosterPanel != null)
            rosterPanel.SetActive(true);
        isOpen = true;

        RefreshRoster();
        DisablePlayerMovement();
    }

    public void CloseWindow()
    {
        if (rosterPanel != null)
            rosterPanel.SetActive(false);
        isOpen = false;

        ClearSpawnedUI();
        EnablePlayerMovement();
    }

    public void OnWindowBlocked(string blockedBy)
    {
        Debug.LogWarning($"[AnimalRosterUI] Cannot open — blocked by '{blockedBy}'");
    }

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        if (theme == null)
            theme = Resources.Load<UITheme>("UI/CozyUITheme");
        ApplyThemeColors();

        if (rosterPanel != null)
            rosterPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseRoster);
    }

    private void ApplyThemeColors()
    {
        if (theme == null) return;

        happyColor = theme.positive;
        neutralColor = theme.warning;
        sadColor = theme.negative;
        fedColor = theme.positive;
        hungryTextColor = theme.negative;
        cardBackgroundColor = new Color(theme.backgroundTan.r, theme.backgroundTan.g, theme.backgroundTan.b, 0.8f);
    }

    private void Start()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.RegisterWindow(this);

        SowurShield.Core.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        if (isOpen)
            RefreshRoster();
    }

    private void OnDestroy()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UnregisterWindow(this);

        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    // =========================================================================
    // Open / Close
    // =========================================================================

    /// <summary>Toggle the roster panel open/closed.</summary>
    public void ToggleRoster()
    {
        if (isOpen)
            CloseRoster();
        else
            OpenRoster();
    }

    public void OpenRoster()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TryOpenWindow(this);
        else
            OpenWindow();
    }

    public void CloseRoster()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TryCloseWindow(this);
        else
            CloseWindow();
    }

    // =========================================================================
    // Roster Population
    // =========================================================================

    /// <summary>Rebuild the entire roster UI from AnimalRoster data.</summary>
    public void RefreshRoster()
    {
        ClearSpawnedUI();

        if (AnimalRoster.Instance == null)
        {
            UpdateHeader(0);
            UpdateSummary(0, 0, 0);
            return;
        }

        List<Animal> allAnimals = AnimalRoster.Instance.GetAllAnimals();
        UpdateHeader(allAnimals.Count);

        if (allAnimals.Count == 0)
        {
            UpdateSummary(0, 0, 0);
            return;
        }

        // Group animals by zone
        var grouped = GroupAnimalsByZone(allAnimals);

        // Build UI for each zone group
        foreach (var group in grouped)
        {
            CreateZoneHeader(group.Key, group.Value.Count);

            foreach (Animal animal in group.Value)
            {
                CreateAnimalCard(animal);
            }
        }

        // Update summary
        float avgHappiness = AnimalRoster.Instance.GetAverageHappiness();
        int hungryCount = AnimalRoster.Instance.GetHungryAnimalCount();
        UpdateSummary(allAnimals.Count, avgHappiness, hungryCount);
    }

    private Dictionary<string, List<Animal>> GroupAnimalsByZone(List<Animal> animals)
    {
        var groups = new Dictionary<string, List<Animal>>();

        foreach (Animal animal in animals)
        {
            if (animal == null) continue;

            string zoneName = animal.AssignedZone != null
                ? animal.AssignedZone.gameObject.name
                : unassignedText.SafeGetLocalizedString();

            if (!groups.ContainsKey(zoneName))
                groups[zoneName] = new List<Animal>();

            groups[zoneName].Add(animal);
        }

        return groups;
    }

    // =========================================================================
    // UI Element Creation
    // =========================================================================

    private void CreateZoneHeader(string zoneName, int animalCount)
    {
        if (contentParent == null) return;

        if (zoneHeaderPrefab != null)
        {
            // Use prefab if available
            GameObject header = Instantiate(zoneHeaderPrefab, contentParent);
            TextMeshProUGUI text = header.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                groupHeaderText.Arguments = new object[] { zoneName, animalCount };
                text.text = groupHeaderText.SafeGetLocalizedString();
            }
            spawnedUIElements.Add(header);
        }
        else
        {
            // Create simple header dynamically
            GameObject header = new GameObject($"ZoneHeader_{zoneName}");
            header.transform.SetParent(contentParent, false);

            TextMeshProUGUI text = header.AddComponent<TextMeshProUGUI>();
            groupHeaderText.Arguments = new object[] { zoneName, animalCount };
            text.text = groupHeaderText.SafeGetLocalizedString();
            text.fontSize = 16;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Left;
            text.margin = new Vector4(10, 5, 0, 5);

            // Set layout size
            LayoutElement layout = header.AddComponent<LayoutElement>();
            layout.preferredHeight = 30;
            layout.flexibleWidth = 1;

            spawnedUIElements.Add(header);
        }
    }

    private void CreateAnimalCard(Animal animal)
    {
        if (contentParent == null || animal == null) return;

        if (animalCardPrefab != null)
        {
            // Use prefab if available
            GameObject card = Instantiate(animalCardPrefab, contentParent);
            SetupCardFromPrefab(card, animal);
            spawnedUIElements.Add(card);
        }
        else
        {
            // Create card dynamically
            GameObject card = CreateDynamicCard(animal);
            spawnedUIElements.Add(card);
        }
    }

    private void SetupCardFromPrefab(GameObject card, Animal animal)
    {
        // Try to find and populate known child elements by name
        AnimalData data = animal.AnimalData;

        // Icon
        Image icon = FindChildComponent<Image>(card, "Icon");
        if (icon != null && data != null && data.idleSprite != null)
            icon.sprite = data.idleSprite;

        // Name
        TextMeshProUGUI nameText = FindChildComponent<TextMeshProUGUI>(card, "Name");
        if (nameText != null)
            nameText.text = animal.GetDisplayName();

        // Happiness bar
        Image happinessBar = FindChildComponent<Image>(card, "HappinessBar");
        if (happinessBar != null)
        {
            float h = animal.GetHappiness();
            happinessBar.fillAmount = h / 100f;
            happinessBar.color = h >= 70f ? happyColor : (h >= 40f ? neutralColor : sadColor);
        }

        // Food status
        TextMeshProUGUI foodStatus = FindChildComponent<TextMeshProUGUI>(card, "FoodStatus");
        if (foodStatus != null)
        {
            foodStatus.text = animal.NeedsFeeding ? hungryText.SafeGetLocalizedString() : fedText.SafeGetLocalizedString();
            foodStatus.color = animal.NeedsFeeding ? hungryTextColor : fedColor;
        }

        // Click handler
        Button btn = card.GetComponent<Button>();
        if (btn == null) btn = card.AddComponent<Button>();
        btn.onClick.AddListener(() => OnAnimalCardClicked(animal));
    }

    private GameObject CreateDynamicCard(Animal animal)
    {
        AnimalData data = animal.AnimalData;

        GameObject card = new GameObject($"Card_{animal.GetDisplayName()}");
        card.transform.SetParent(contentParent, false);

        // Background
        Image bg = card.AddComponent<Image>();
        bg.color = cardBackgroundColor;

        // Layout
        HorizontalLayoutGroup hlg = card.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.padding = new RectOffset(8, 8, 4, 4);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;

        LayoutElement cardLayout = card.AddComponent<LayoutElement>();
        cardLayout.preferredHeight = 50;
        cardLayout.flexibleWidth = 1;

        // Icon
        if (data != null && data.idleSprite != null)
        {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(card.transform, false);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = data.idleSprite;
            iconImg.preserveAspect = true;
            LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 40;
            iconLayout.preferredHeight = 40;
        }

        // Info column
        GameObject infoCol = new GameObject("Info");
        infoCol.transform.SetParent(card.transform, false);
        VerticalLayoutGroup vlg = infoCol.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childForceExpandHeight = false;
        LayoutElement infoLayout = infoCol.AddComponent<LayoutElement>();
        infoLayout.flexibleWidth = 1;

        // Name text
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(infoCol.transform, false);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = animal.GetDisplayName();
        nameText.fontSize = 14;
        nameText.fontStyle = FontStyles.Bold;
        LayoutElement nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 20;

        // Status text (happiness + food)
        GameObject statusObj = new GameObject("Status");
        statusObj.transform.SetParent(infoCol.transform, false);
        TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();

        float happiness = animal.GetHappiness();
        string moodEmoji = happiness >= 70f ? moodHappyText.SafeGetLocalizedString() : (happiness >= 40f ? moodNeutralText.SafeGetLocalizedString() : moodSadText.SafeGetLocalizedString());
        string fedStatus = animal.NeedsFeeding ? hungryText.SafeGetLocalizedString() : fedText.SafeGetLocalizedString();
        Color fedCol = animal.NeedsFeeding ? hungryTextColor : fedColor;

        rowStatusText.Arguments = new object[] { moodEmoji, happiness, fedStatus };
        statusText.text = rowStatusText.SafeGetLocalizedString();
        statusText.fontSize = 11;
        statusText.color = happiness >= 70f ? happyColor : (happiness >= 40f ? neutralColor : sadColor);
        LayoutElement statusLayout = statusObj.AddComponent<LayoutElement>();
        statusLayout.preferredHeight = 16;

        // Click handler
        Button btn = card.AddComponent<Button>();
        btn.onClick.AddListener(() => OnAnimalCardClicked(animal));

        return card;
    }

    // =========================================================================
    // Interaction
    // =========================================================================

    private void OnAnimalCardClicked(Animal animal)
    {
        if (animal == null) return;

        // Close roster first
        CloseRoster();

        // Open AnimalInfoUI for this animal
        AnimalInfoUI infoUI = Object.FindFirstObjectByType<AnimalInfoUI>();
        if (infoUI != null)
        {
            infoUI.ShowAnimalInfo(animal);
        }
    }

    // =========================================================================
    // UI Updates
    // =========================================================================

    private void UpdateHeader(int totalCount)
    {
        if (headerText != null)
        {
            rosterTitleText.Arguments = new object[] { totalCount, totalCount != 1 ? "s" : "" };
            headerText.text = rosterTitleText.SafeGetLocalizedString();
        }
    }

    private void UpdateSummary(int total, float avgHappiness, int hungryCount)
    {
        if (summaryText == null) return;

        if (total == 0)
        {
            summaryText.text = rosterEmptyText.SafeGetLocalizedString();
            return;
        }

        avgHappinessText.Arguments = new object[] { avgHappiness, hungryCount, total };
        summaryText.text = avgHappinessText.SafeGetLocalizedString();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private void ClearSpawnedUI()
    {
        foreach (GameObject obj in spawnedUIElements)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedUIElements.Clear();
    }

    private T FindChildComponent<T>(GameObject parent, string childName) where T : Component
    {
        Transform child = parent.transform.Find(childName);
        if (child != null)
            return child.GetComponent<T>();
        return null;
    }

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
}

} // namespace SowurShield.Animals
