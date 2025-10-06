using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor utility to automatically create enhanced inventory UI
/// Tools > Inventory > Setup Enhanced UI
/// </summary>
public class InventoryUISetupHelper : EditorWindow
{
    private GameObject targetCanvas;
    private Inventory inventoryScript;

    [MenuItem("Tools/Inventory/Setup Enhanced UI")]
    public static void ShowWindow()
    {
        GetWindow<InventoryUISetupHelper>("Inventory UI Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Enhanced Inventory UI Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetCanvas = (GameObject)EditorGUILayout.ObjectField("Target Canvas", targetCanvas, typeof(GameObject), true);
        inventoryScript = (Inventory)EditorGUILayout.ObjectField("Inventory Script", inventoryScript, typeof(Inventory), true);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Enhanced Inventory UI", GUILayout.Height(40)))
        {
            CreateEnhancedInventoryUI();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This will create a complete inventory UI with:\n" +
            "• Main panel with background\n" +
            "• Header with title and capacity\n" +
            "• Tab system for filtering\n" +
            "• Search bar\n" +
            "• Sort buttons\n" +
            "• Styled slot grid",
            MessageType.Info);
    }

    private void CreateEnhancedInventoryUI()
    {
        if (targetCanvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a target Canvas!", "OK");
            return;
        }

        // Create main inventory panel
        GameObject inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(targetCanvas.transform, false);

        RectTransform panelRect = inventoryPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(800, 600);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelBg = inventoryPanel.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Add shadow for depth
        var shadow = inventoryPanel.AddComponent<Shadow>();
        shadow.effectDistance = new Vector2(5, -5);

        // Create header
        CreateHeader(inventoryPanel);

        // Create tab system
        CreateTabSystem(inventoryPanel);

        // Create search and sort section
        CreateSearchSortSection(inventoryPanel);

        // Create slots container
        CreateSlotsContainer(inventoryPanel);

        // Create UI Manager component
        InventoryUIManager uiManager = inventoryPanel.AddComponent<InventoryUIManager>();
        uiManager.inventoryPanel = inventoryPanel;
        uiManager.inventory = inventoryScript;

        Debug.Log("Enhanced Inventory UI created successfully!");
        EditorUtility.DisplayDialog("Success", "Enhanced Inventory UI has been created!", "OK");
    }

    private void CreateHeader(GameObject parent)
    {
        GameObject header = new GameObject("Header");
        header.transform.SetParent(parent.transform, false);

        RectTransform headerRect = header.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.sizeDelta = new Vector2(0, 60);
        headerRect.anchoredPosition = Vector2.zero;

        Image headerBg = header.AddComponent<Image>();
        headerBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Title
        GameObject title = new GameObject("Title");
        title.transform.SetParent(header.transform, false);

        RectTransform titleRect = title.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0);
        titleRect.anchorMax = new Vector2(0, 1);
        titleRect.pivot = new Vector2(0, 0.5f);
        titleRect.sizeDelta = new Vector2(200, 0);
        titleRect.anchoredPosition = new Vector2(20, 0);

        TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
        titleText.text = "INVENTORY";
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.8f, 0.3f);
        titleText.alignment = TextAlignmentOptions.MidlineLeft;

        // Capacity text
        GameObject capacity = new GameObject("Capacity");
        capacity.transform.SetParent(header.transform, false);

        RectTransform capacityRect = capacity.AddComponent<RectTransform>();
        capacityRect.anchorMin = new Vector2(1, 0);
        capacityRect.anchorMax = new Vector2(1, 1);
        capacityRect.pivot = new Vector2(1, 0.5f);
        capacityRect.sizeDelta = new Vector2(150, 0);
        capacityRect.anchoredPosition = new Vector2(-80, 0);

        TextMeshProUGUI capacityText = capacity.AddComponent<TextMeshProUGUI>();
        capacityText.text = "0/36";
        capacityText.fontSize = 24;
        capacityText.color = Color.white;
        capacityText.alignment = TextAlignmentOptions.MidlineRight;

        // Close button
        GameObject closeBtn = new GameObject("CloseButton");
        closeBtn.transform.SetParent(header.transform, false);

        RectTransform closeBtnRect = closeBtn.AddComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1, 0.5f);
        closeBtnRect.anchorMax = new Vector2(1, 0.5f);
        closeBtnRect.pivot = new Vector2(1, 0.5f);
        closeBtnRect.sizeDelta = new Vector2(40, 40);
        closeBtnRect.anchoredPosition = new Vector2(-10, 0);

        Button closeButton = closeBtn.AddComponent<Button>();
        Image closeBtnImg = closeBtn.AddComponent<Image>();
        closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        // X text
        GameObject xText = new GameObject("X");
        xText.transform.SetParent(closeBtn.transform, false);
        RectTransform xRect = xText.AddComponent<RectTransform>();
        xRect.anchorMin = Vector2.zero;
        xRect.anchorMax = Vector2.one;
        xRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI xTextComp = xText.AddComponent<TextMeshProUGUI>();
        xTextComp.text = "X";
        xTextComp.fontSize = 24;
        xTextComp.fontStyle = FontStyles.Bold;
        xTextComp.color = Color.white;
        xTextComp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateTabSystem(GameObject parent)
    {
        GameObject tabContainer = new GameObject("TabContainer");
        tabContainer.transform.SetParent(parent.transform, false);

        RectTransform tabRect = tabContainer.AddComponent<RectTransform>();
        tabRect.anchorMin = new Vector2(0, 1);
        tabRect.anchorMax = new Vector2(1, 1);
        tabRect.pivot = new Vector2(0.5f, 1);
        tabRect.sizeDelta = new Vector2(0, 50);
        tabRect.anchoredPosition = new Vector2(0, -60);

        HorizontalLayoutGroup layout = tabContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5;
        layout.padding = new RectOffset(10, 10, 5, 5);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        string[] tabs = { "All", "Tools", "Seeds", "Food", "Resources" };
        foreach (string tabName in tabs)
        {
            CreateTab(tabContainer, tabName);
        }
    }

    private void CreateTab(GameObject parent, string tabName)
    {
        GameObject tab = new GameObject($"{tabName}Tab");
        tab.transform.SetParent(parent.transform, false);

        Button button = tab.AddComponent<Button>();
        Image btnImg = tab.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        GameObject text = new GameObject("Text");
        text.transform.SetParent(tab.transform, false);

        RectTransform textRect = text.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI textComp = text.AddComponent<TextMeshProUGUI>();
        textComp.text = tabName.ToUpper();
        textComp.fontSize = 18;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateSearchSortSection(GameObject parent)
    {
        GameObject section = new GameObject("SearchSortSection");
        section.transform.SetParent(parent.transform, false);

        RectTransform sectionRect = section.AddComponent<RectTransform>();
        sectionRect.anchorMin = new Vector2(0, 1);
        sectionRect.anchorMax = new Vector2(1, 1);
        sectionRect.pivot = new Vector2(0.5f, 1);
        sectionRect.sizeDelta = new Vector2(0, 40);
        sectionRect.anchoredPosition = new Vector2(0, -110);

        // Search bar
        CreateSearchBar(section);

        // Sort buttons
        CreateSortButtons(section);
    }

    private void CreateSearchBar(GameObject parent)
    {
        GameObject searchBar = new GameObject("SearchBar");
        searchBar.transform.SetParent(parent.transform, false);

        RectTransform searchRect = searchBar.AddComponent<RectTransform>();
        searchRect.anchorMin = new Vector2(0, 0);
        searchRect.anchorMax = new Vector2(0.5f, 1);
        searchRect.offsetMin = new Vector2(10, 0);
        searchRect.offsetMax = new Vector2(-5, 0);

        Image searchBg = searchBar.AddComponent<Image>();
        searchBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        TMP_InputField inputField = searchBar.AddComponent<TMP_InputField>();
        inputField.textComponent = CreateInputFieldText(searchBar);

        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(searchBar.transform, false);
        TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
        placeholderText.text = "Search items...";
        placeholderText.fontSize = 16;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholderText.fontStyle = FontStyles.Italic;

        RectTransform phRect = placeholder.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = new Vector2(10, 0);
        phRect.offsetMax = new Vector2(-10, 0);

        inputField.placeholder = placeholderText;
    }

    private TextMeshProUGUI CreateInputFieldText(GameObject parent)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 16;
        text.color = Color.white;

        return text;
    }

    private void CreateSortButtons(GameObject parent)
    {
        GameObject sortContainer = new GameObject("SortButtons");
        sortContainer.transform.SetParent(parent.transform, false);

        RectTransform sortRect = sortContainer.AddComponent<RectTransform>();
        sortRect.anchorMin = new Vector2(0.5f, 0);
        sortRect.anchorMax = new Vector2(1, 1);
        sortRect.offsetMin = new Vector2(5, 0);
        sortRect.offsetMax = new Vector2(-10, 0);

        HorizontalLayoutGroup layout = sortContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        string[] sortButtons = { "Type", "Name", "Value", "Rarity" };
        foreach (string btnName in sortButtons)
        {
            CreateSortButton(sortContainer, btnName);
        }
    }

    private void CreateSortButton(GameObject parent, string buttonName)
    {
        GameObject btn = new GameObject($"Sort{buttonName}Button");
        btn.transform.SetParent(parent.transform, false);

        Button button = btn.AddComponent<Button>();
        Image btnImg = btn.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.25f, 0.35f, 1f);

        GameObject text = new GameObject("Text");
        text.transform.SetParent(btn.transform, false);

        RectTransform textRect = text.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI textComp = text.AddComponent<TextMeshProUGUI>();
        textComp.text = buttonName;
        textComp.fontSize = 14;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateSlotsContainer(GameObject parent)
    {
        GameObject slotsContainer = new GameObject("SlotsContainer");
        slotsContainer.transform.SetParent(parent.transform, false);

        RectTransform slotsRect = slotsContainer.AddComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0, 0);
        slotsRect.anchorMax = new Vector2(1, 1);
        slotsRect.offsetMin = new Vector2(10, 10);
        slotsRect.offsetMax = new Vector2(-10, -160);

        // Scroll view for slots
        ScrollRect scrollRect = slotsContainer.AddComponent<ScrollRect>();
        Image scrollBg = slotsContainer.AddComponent<Image>();
        scrollBg.color = new Color(0.08f, 0.08f, 0.08f, 1f);

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(slotsContainer.transform, false);

        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(70, 70);
        grid.spacing = new Vector2(5, 5);
        grid.padding = new RectOffset(10, 10, 10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 9;

        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        Debug.Log("Slots container created with grid layout!");
    }
}
