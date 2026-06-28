using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using SowurShield.Core;
using SowurShield.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SowurShield.Combat
{

/// <summary>
/// Editor utility script to automatically organize and setup Team Assembler UI.
///
/// USAGE:
/// 1. Attach this script to your TeamAssemblerUI GameObject in the scene
/// 2. In the Inspector, click the "Auto-Setup UI" button
/// 3. This script will find all UI elements and organize them properly
/// 4. After setup is complete, you can remove this script
/// </summary>
public class TeamAssemblerUISetup : MonoBehaviour
{
    [Header("References to Find")]
    [Tooltip("The TeamAssemblerUI script to configure")]
    public TeamAssemblerUI assemblerUI;

    [Header("Localization")]
    [SerializeField] private LocalizedString availableAnimalsText_Localized; // table "Combat", key "combat.teamassemblersetup.available_animals"

    [Header("Status")]
#pragma warning disable CS0414
    [SerializeField] private string statusMessage = "Right-click on this component → Auto-Setup UI (or use context menu)";
#pragma warning restore CS0414

#if UNITY_EDITOR
    /// <summary>
    /// Auto-setup button in Inspector
    /// </summary>
    [ContextMenu("Auto-Setup UI")]
    private void AutoSetupUI()
    {

        if (assemblerUI == null)
        {
            assemblerUI = GetComponent<TeamAssemblerUI>();
        }

        if (assemblerUI == null)
        {
            statusMessage = "ERROR: No TeamAssemblerUI found!";
            return;
        }

        // Find or create all UI elements
        bool success = SetupMainPanels();
        success &= SetupGridSystem();
        success &= SetupAnimalSelection();
        success &= SetupInfoPanel();
        success &= SetupButtons();
        success &= AssignReferences();

        // Set AssemblerPanel to inactive now that setup is complete
        Transform assemblerPanel = transform.Find("AssemblerPanel");
        if (assemblerPanel != null)
        {
            assemblerPanel.gameObject.SetActive(false);
        }

        // Ensure EventSystem exists for drag-and-drop to work
        EnsureEventSystem();

        if (success)
        {
            statusMessage = "✓ Setup complete! You can now remove this script.";
            EditorUtility.SetDirty(assemblerUI);
            EditorUtility.SetDirty(gameObject);
        }
        else
        {
            statusMessage = "⚠ Setup had errors - check Console";
        }
    }

    private bool SetupMainPanels()
    {

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return false;
        }

        // Find or create Assembler Panel
        Transform assemblerPanel = transform.Find("AssemblerPanel");
        if (assemblerPanel == null)
        {
            GameObject panelObj = new GameObject("AssemblerPanel");
            panelObj.transform.SetParent(transform, false);
            assemblerPanel = panelObj.transform;

            // Add components
            RectTransform rect = panelObj.AddComponent<RectTransform>();
            Image img = panelObj.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Fullscreen anchoring
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            // Keep active during setup - we'll set it inactive at the end
            panelObj.SetActive(true);

        }
        else
        {
            // If it already exists, make sure it's active for setup
            assemblerPanel.gameObject.SetActive(true);
        }

        // Find or create Animal Selection Panel (LEFT SIDE)
        Transform animalSelectionPanel = assemblerPanel.Find("AnimalSelectionPanel");
        if (animalSelectionPanel == null)
        {
            GameObject panelObj = new GameObject("AnimalSelectionPanel");
            panelObj.transform.SetParent(assemblerPanel, false);
            animalSelectionPanel = panelObj.transform;

            RectTransform rect = panelObj.AddComponent<RectTransform>();
            Image img = panelObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Left side positioning - wider for bigger cards (360px)
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0.35f, 1); // Increased from 0.3 to 0.35 (35% width)
            rect.offsetMin = new Vector2(20, 20);
            rect.offsetMax = new Vector2(-10, -20);

        }

        // Find or create Grid Panel (CENTER)
        Transform gridPanel = assemblerPanel.Find("GridPanel");
        if (gridPanel == null)
        {
            GameObject panelObj = new GameObject("GridPanel");
            panelObj.transform.SetParent(assemblerPanel, false);
            gridPanel = panelObj.transform;

            RectTransform rect = panelObj.AddComponent<RectTransform>();
            Image img = panelObj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Fixed size for grid panel - centers it and prevents expansion
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 40); // Slightly above center
            rect.sizeDelta = new Vector2(350, 550); // Fixed size

        }

        // Find or create Info Panel (RIGHT SIDE)
        Transform infoPanel = assemblerPanel.Find("InfoPanel");
        if (infoPanel == null)
        {
            GameObject panelObj = new GameObject("InfoPanel");
            panelObj.transform.SetParent(assemblerPanel, false);
            infoPanel = panelObj.transform;

            RectTransform rect = panelObj.AddComponent<RectTransform>();
            Image img = panelObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Right side positioning - adjusted for wider animal panel
            rect.anchorMin = new Vector2(0.65f, 0); // Moved from 0.7 to 0.65
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(10, 20);
            rect.offsetMax = new Vector2(-20, -20);

        }

        return true;
    }

    private bool SetupGridSystem()
    {

        Transform gridPanel = transform.Find("AssemblerPanel/GridPanel");
        if (gridPanel == null)
        {
            return false;
        }

        // Find or create Grid Container
        Transform gridContainer = gridPanel.Find("GridContainer");
        if (gridContainer == null)
        {
            GameObject containerObj = new GameObject("GridContainer");
            containerObj.transform.SetParent(gridPanel, false);
            gridContainer = containerObj.transform;

            RectTransform rect = containerObj.AddComponent<RectTransform>();

            // Center positioning - FIXED SIZE to prevent expansion
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(305, 505); // Fixed size: 3 cols x 5 rows

            // Add Grid Layout Group for 3x5 grid
            GridLayoutGroup grid = containerObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(95, 95);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3; // 3 columns
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(5, 5, 5, 5);

            // CRITICAL: Add ContentSizeFitter to PREVENT infinite expansion
            ContentSizeFitter fitter = containerObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained; // Don't auto-fit!

        }

        return true;
    }

    private bool SetupAnimalSelection()
    {

        Transform animalPanel = transform.Find("AssemblerPanel/AnimalSelectionPanel");
        if (animalPanel == null)
        {
            return false;
        }

        // Create complete Scroll View hierarchy in one go - avoid using Find() after creation
        Transform scrollView = animalPanel.Find("Scroll View");

        if (scrollView == null)
        {
            // Create everything at once with direct references (no Find() mid-creation)
            GameObject scrollObj = new GameObject("Scroll View");
            scrollObj.transform.SetParent(animalPanel, false);

            RectTransform rect = scrollObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10, 50); // Bottom: 50px from bottom
            rect.offsetMax = new Vector2(-10, -70); // Top: 70px from top (more space from "Available Animals")

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            Image img = scrollObj.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            // Create Viewport with direct parent reference (OPTION B: Proper ScrollRect)
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);

            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            // CRITICAL: Make viewport fill parent completely for Option B
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero; // Fill parent
            viewportRect.anchoredPosition = Vector2.zero;

            // Use RectMask2D instead of Mask (better performance, no stencil buffer)
            UnityEngine.UI.RectMask2D rectMask = viewportObj.AddComponent<UnityEngine.UI.RectMask2D>();
            rectMask.enabled = true; // Ensure masking is active


            // Create Content with direct parent reference
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 500);
            contentRect.anchoredPosition = Vector2.zero;

            // Add Vertical Layout Group to Content
            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15; // More spacing between cards
            vlg.padding = new RectOffset(15, 15, 20, 15); // More padding (left, right, top, bottom)
            vlg.childControlWidth = true; // Control width - make cards fill container
            vlg.childControlHeight = false; // Don't control height - let cards keep their size
            vlg.childForceExpandWidth = true; // Expand cards to fill horizontal space
            vlg.childForceExpandHeight = false; // Don't expand cards vertically
            vlg.childAlignment = TextAnchor.UpperCenter;

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Setup ScrollRect with direct references
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

        }
        else
        {

            // Validate and fix Viewport configuration
            Transform viewport = scrollView.Find("Viewport");
            if (viewport != null)
            {
                RectTransform viewportRect = viewport.GetComponent<RectTransform>();

                // Check if viewport fills parent (Option B requirement)
                bool needsFix = false;
                if (viewportRect.anchorMin != Vector2.zero || viewportRect.anchorMax != Vector2.one || viewportRect.sizeDelta != Vector2.zero)
                {
                    viewportRect.anchorMin = Vector2.zero;
                    viewportRect.anchorMax = Vector2.one;
                    viewportRect.sizeDelta = Vector2.zero;
                    viewportRect.anchoredPosition = Vector2.zero;
                    needsFix = true;
                }

                // Check if using RectMask2D (Option B requirement)
                UnityEngine.UI.RectMask2D rectMask = viewport.GetComponent<UnityEngine.UI.RectMask2D>();
                Mask oldMask = viewport.GetComponent<Mask>();

                if (oldMask != null && rectMask == null)
                {
                    DestroyImmediate(oldMask);
                    rectMask = viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                    rectMask.enabled = true;
                    needsFix = true;
                }
                else if (rectMask == null)
                {
                    rectMask = viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                    rectMask.enabled = true;
                    needsFix = true;
                }
                else if (!rectMask.enabled)
                {
                    rectMask.enabled = true;
                    needsFix = true;
                }

                if (needsFix)
                {
                }
                else
                {
                }
            }
            else
            {
            }
        }


        // Add title text if missing
        Transform titleText = animalPanel.Find("TitleText");
        if (titleText == null)
        {
            GameObject textObj = new GameObject("TitleText");
            textObj.transform.SetParent(animalPanel, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(0, -35); // Lower position (was -25)
            rect.sizeDelta = new Vector2(-20, 50); // Taller text area

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = availableAnimalsText_Localized.SafeGetLocalizedString();
            text.fontSize = 20;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

        }

        return true;
    }

    private bool SetupInfoPanel()
    {

        Transform infoPanel = transform.Find("AssemblerPanel/InfoPanel");
        if (infoPanel == null)
        {
            return false;
        }

        // Add Vertical Layout Group to organize info texts
        if (infoPanel.GetComponent<VerticalLayoutGroup>() == null)
        {
            VerticalLayoutGroup vlg = infoPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        // Create info text fields
        CreateInfoText(infoPanel, "ZoneNameText", "Zone: Unknown", 24, FontStyles.Bold);
        CreateInfoText(infoPanel, "TeamSizeText", "Team: 0/15", 18, FontStyles.Normal);
        CreateInfoText(infoPanel, "FoodRequirementsText", "Food: None", 16, FontStyles.Normal);
        CreateInfoText(infoPanel, "SynergiesText", "Synergies: TBD", 16, FontStyles.Normal);

        return true;
    }

    private void CreateInfoText(Transform parent, string name, string text, float fontSize, FontStyles style)
    {
        Transform existing = parent.Find(name);
        if (existing == null)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 50);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.color = Color.white;
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            LayoutElement le = textObj.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            le.flexibleHeight = 1;

        }
    }

    private bool SetupButtons()
    {

        Transform assemblerPanel = transform.Find("AssemblerPanel");
        if (assemblerPanel == null)
        {
            return false;
        }

        // Create button container at bottom
        Transform buttonContainer = assemblerPanel.Find("ButtonContainer");
        if (buttonContainer == null)
        {
            GameObject containerObj = new GameObject("ButtonContainer");
            containerObj.transform.SetParent(assemblerPanel, false);

            RectTransform rect = containerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(0, 60);
            rect.sizeDelta = new Vector2(-40, 80);

            HorizontalLayoutGroup hlg = containerObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            buttonContainer = containerObj.transform;
        }

        // Verify container is valid before creating buttons
        if (buttonContainer == null)
        {
            return false;
        }

        // Create buttons
        CreateButton(buttonContainer, "FeedAllButton", "Feed All");
        CreateButton(buttonContainer, "ClearGridButton", "Clear Grid");
        CreateButton(buttonContainer, "StartBattleButton", "Start Battle");
        CreateButton(buttonContainer, "CancelButton", "Cancel");

        return true;
    }

    private void CreateButton(Transform parent, string name, string text)
    {
        if (parent == null)
        {
            return;
        }

        Transform existing = parent.Find(name);
        if (existing == null)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 50);

            Image img = buttonObj.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Button btn = buttonObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

        }
    }

    private bool AssignReferences()
    {

        // Use SerializedObject to properly modify the component
        SerializedObject serializedObject = new SerializedObject(assemblerUI);

        // Assign panels
        AssignReference(serializedObject, "assemblerPanel", transform.Find("AssemblerPanel"));
        AssignReference(serializedObject, "animalSelectionPanel", transform.Find("AssemblerPanel/AnimalSelectionPanel"));
        AssignReference(serializedObject, "gridPanel", transform.Find("AssemblerPanel/GridPanel"));

        // Assign containers - try to find Content with detailed debugging
        Transform animalContainer = transform.Find("AssemblerPanel/AnimalSelectionPanel/Scroll View/Viewport/Content");
        if (animalContainer == null)
        {

            // Debug the hierarchy
            Transform scrollView = transform.Find("AssemblerPanel/AnimalSelectionPanel/Scroll View");
            if (scrollView != null)
            {
                for (int i = 0; i < scrollView.childCount; i++)
                {
                    Transform child = scrollView.GetChild(i);

                    if (child.name == "Viewport")
                    {
                        for (int j = 0; j < child.childCount; j++)
                        {
                        }

                        // Try to get Content directly
                        Transform content = child.Find("Content");
                        if (content != null)
                        {
                            animalContainer = content;
                        }
                    }
                }
            }
        }
        AssignReference(serializedObject, "animalCardContainer", animalContainer);
        AssignReference(serializedObject, "gridContainer", transform.Find("AssemblerPanel/GridPanel/GridContainer"));

        // Assign prefabs (load from Assets/Prefabs/Combat/)
        AssignPrefabReference(serializedObject, "gridSlotPrefab", "Assets/Prefabs/Combat/GridSlotPrefab.prefab");
        AssignPrefabReference(serializedObject, "animalCardPrefab", "Assets/Prefabs/Combat/AnimalCardPrefab.prefab");

        // Assign info texts
        AssignReference(serializedObject, "zoneNameText", transform.Find("AssemblerPanel/InfoPanel/ZoneNameText"));
        AssignReference(serializedObject, "teamSizeText", transform.Find("AssemblerPanel/InfoPanel/TeamSizeText"));
        AssignReference(serializedObject, "foodRequirementsText", transform.Find("AssemblerPanel/InfoPanel/FoodRequirementsText"));
        AssignReference(serializedObject, "synergiesText", transform.Find("AssemblerPanel/InfoPanel/SynergiesText"));

        // Assign buttons
        AssignReference(serializedObject, "feedAllButton", transform.Find("AssemblerPanel/ButtonContainer/FeedAllButton"));
        AssignReference(serializedObject, "clearGridButton", transform.Find("AssemblerPanel/ButtonContainer/ClearGridButton"));
        AssignReference(serializedObject, "startBattleButton", transform.Find("AssemblerPanel/ButtonContainer/StartBattleButton"));
        AssignReference(serializedObject, "cancelButton", transform.Find("AssemblerPanel/ButtonContainer/CancelButton"));

        serializedObject.ApplyModifiedProperties();

        return true;
    }

    private void EnsureEventSystem()
    {
        // Check if EventSystem exists in scene
        UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();

        if (eventSystem == null)
        {

            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        }
        else
        {
        }

        // Ensure Canvas has GraphicRaycaster
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            UnityEngine.UI.GraphicRaycaster raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
                canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            else
            {
            }
        }
    }

    private void AssignReference(SerializedObject serializedObject, string propertyName, Transform target)
    {
        if (target == null)
        {
            return;
        }

        SerializedProperty prop = serializedObject.FindProperty(propertyName);
        if (prop != null)
        {
            // Determine which component to assign based on what the field expects
            if (propertyName.Contains("Text"))
            {
                TextMeshProUGUI textComp = target.GetComponent<TextMeshProUGUI>();
                if (textComp != null)
                {
                    prop.objectReferenceValue = textComp;
                    return;
                }
            }

            if (propertyName.Contains("Button"))
            {
                Button btnComp = target.GetComponent<Button>();
                if (btnComp != null)
                {
                    prop.objectReferenceValue = btnComp;
                    return;
                }
            }

            if (propertyName.Contains("Panel"))
            {
                prop.objectReferenceValue = target.gameObject;
            }
            else if (propertyName.Contains("Container"))
            {
                prop.objectReferenceValue = target;
            }
            else
            {
                prop.objectReferenceValue = target;
            }
        }
        else
        {
        }
    }

    private void AssignPrefabReference(SerializedObject serializedObject, string propertyName, string assetPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {

            // Try to create the prefab
            if (propertyName == "animalCardPrefab")
            {
                prefab = CreateAnimalCardPrefab(assetPath);
            }
            else if (propertyName == "gridSlotPrefab")
            {
                prefab = CreateGridSlotPrefab(assetPath);
            }

            if (prefab == null)
            {
                return;
            }
        }

        SerializedProperty prop = serializedObject.FindProperty(propertyName);
        if (prop != null)
        {
            prop.objectReferenceValue = prefab;
        }
        else
        {
        }
    }

    /// <summary>
    /// Create AnimalCardPrefab with all required UI components
    /// </summary>
    private GameObject CreateAnimalCardPrefab(string assetPath)
    {
        UITheme theme = Resources.Load<UITheme>("UI/CozyUITheme");

        // Create root card object - will expand to fill container width
        GameObject card = new GameObject("AnimalCardPrefab");
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(100, 140); // Min width, will expand horizontally

        // Add background image with nice color
        Image cardBackground = card.AddComponent<Image>();
        cardBackground.color = theme != null ? theme.backgroundCream : new Color(0.95f, 0.95f, 0.95f, 1f);
        cardBackground.raycastTarget = true; // Allow clicking

        // Add shadow for depth
        UnityEngine.UI.Shadow shadow = card.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.3f); // Semi-transparent black shadow
        shadow.effectDistance = new Vector2(4, -4);

        // Add outline for crisp edges
        UnityEngine.UI.Outline outline = card.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        outline.effectDistance = new Vector2(2, -2);

        // Add LayoutElement for proper sizing in VerticalLayoutGroup
        LayoutElement layoutElement = card.AddComponent<LayoutElement>();
        layoutElement.minWidth = 300; // Minimum width
        layoutElement.preferredHeight = 140; // Fixed height
        layoutElement.minHeight = 140;
        layoutElement.flexibleWidth = 1; // Allow horizontal expansion to fill space
        layoutElement.flexibleHeight = 0; // Don't expand vertically (keep height fixed)

        // Add AnimalSelectionCard component
        AnimalSelectionCard cardScript = card.AddComponent<AnimalSelectionCard>();

        // Create portrait image (LEFT SIDE - HIGH RESOLUTION with scale-down)
        GameObject portraitObj = new GameObject("Portrait");
        portraitObj.transform.SetParent(card.transform, false);
        RectTransform portraitRect = portraitObj.AddComponent<RectTransform>();

        // Make portrait take more space (40% width instead of 35%)
        portraitRect.anchorMin = new Vector2(0, 0);
        portraitRect.anchorMax = new Vector2(0.4f, 1); // Increased from 0.35 to 0.4
        portraitRect.offsetMin = new Vector2(10, 10);
        portraitRect.offsetMax = new Vector2(-5, -10);

        // Scale down to 0.9 for better resolution (image is 2x size, displayed at 0.9x = sharper)
        portraitRect.localScale = new Vector3(0.9f, 0.9f, 1f);

        Image portrait = portraitObj.AddComponent<Image>();
        portrait.color = theme != null ? theme.woodLight : new Color(0.7f, 0.7f, 0.7f, 1f);
        portrait.preserveAspect = true; // Maintain aspect ratio for animal sprites

        // Add border to portrait for definition
        UnityEngine.UI.Outline portraitOutline = portraitObj.AddComponent<UnityEngine.UI.Outline>();
        portraitOutline.effectColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        portraitOutline.effectDistance = new Vector2(2, -2);

        // Create name text (TOP RIGHT - adjusted for 40% portrait width)
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(card.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.4f, 0.65f); // Adjusted from 0.35 to 0.4
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.offsetMin = new Vector2(10, 0);
        nameRect.offsetMax = new Vector2(-25, -20); // 25px from right (better padding)
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 18; // Bigger font
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.TopLeft;
        nameText.color = theme != null ? theme.textDark : new Color(0.15f, 0.15f, 0.15f, 1f);

        // Create happiness text (MIDDLE RIGHT - adjusted for 40% portrait width)
        GameObject happinessObj = new GameObject("HappinessText");
        happinessObj.transform.SetParent(card.transform, false);
        RectTransform happinessRect = happinessObj.AddComponent<RectTransform>();
        happinessRect.anchorMin = new Vector2(0.4f, 0.35f); // Adjusted from 0.35 to 0.4
        happinessRect.anchorMax = new Vector2(1, 0.65f);
        happinessRect.offsetMin = new Vector2(10, 0);
        happinessRect.offsetMax = new Vector2(-25, -5); // 25px from right (better padding)
        TextMeshProUGUI happinessText = happinessObj.AddComponent<TextMeshProUGUI>();
        happinessText.fontSize = 15; // Slightly bigger
        happinessText.alignment = TextAlignmentOptions.TopLeft;
        happinessText.color = theme != null ? theme.negative : new Color(0.8f, 0.3f, 0.3f, 1f);

        // Create food status text (BOTTOM - full width, 20px from bottom)
        GameObject foodObj = new GameObject("FoodStatusText");
        foodObj.transform.SetParent(card.transform, false);
        RectTransform foodRect = foodObj.AddComponent<RectTransform>();
        foodRect.anchorMin = new Vector2(0, 0);
        foodRect.anchorMax = new Vector2(1, 0.35f);
        foodRect.offsetMin = new Vector2(10, 20); // 20px from bottom
        foodRect.offsetMax = new Vector2(-25, 0); // 25px from right (better padding)
        TextMeshProUGUI foodText = foodObj.AddComponent<TextMeshProUGUI>();
        foodText.fontSize = 13;
        foodText.alignment = TextAlignmentOptions.TopLeft;
        foodText.color = theme != null ? theme.positive : new Color(0.2f, 0.4f, 0.2f, 1f);

        // Assign references via SerializedObject
        SerializedObject so = new SerializedObject(cardScript);
        so.FindProperty("animalPortrait").objectReferenceValue = portrait;
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("happinessText").objectReferenceValue = happinessText;
        so.FindProperty("foodStatusText").objectReferenceValue = foodText;
        so.FindProperty("cardBackground").objectReferenceValue = cardBackground;

        // Set colors to prevent yellow hover default
        so.FindProperty("normalColor").colorValue = theme != null ? theme.backgroundCream : new Color(0.95f, 0.95f, 0.95f, 1f);
        so.FindProperty("hoverColor").colorValue = theme != null ? theme.highlightGold : new Color(1f, 1f, 0.9f, 1f);
        so.FindProperty("inTeamColor").colorValue = theme != null ? theme.positive : new Color(0.8f, 1f, 0.8f, 1f);

        so.ApplyModifiedProperties();

        // Ensure prefab folder exists
        string folderPath = System.IO.Path.GetDirectoryName(assetPath);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string[] folders = folderPath.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }

        // Save as prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(card, assetPath);
        DestroyImmediate(card); // Remove from scene

        return prefab;
    }

    /// <summary>
    /// Create GridSlotPrefab
    /// </summary>
    private GameObject CreateGridSlotPrefab(string assetPath)
    {

        // Create root slot object
        GameObject slot = new GameObject("GridSlotPrefab");
        RectTransform slotRect = slot.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(95, 95);

        // Add background image
        Image slotBackground = slot.AddComponent<Image>();
        slotBackground.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Light gray
        slotBackground.raycastTarget = true; // CRITICAL: Allow drop detection

        // Add outline for visibility
        UnityEngine.UI.Outline slotOutline = slot.AddComponent<UnityEngine.UI.Outline>();
        slotOutline.effectColor = new Color(0.3f, 0.3f, 0.3f);
        slotOutline.effectDistance = new Vector2(1, -1);

        // Add GridPositionSlot component
        GridPositionSlot slotScript = slot.AddComponent<GridPositionSlot>();

        // Create animal icon (child image)
        GameObject iconObj = new GameObject("AnimalIcon");
        iconObj.transform.SetParent(slot.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(5, 5);
        iconRect.offsetMax = new Vector2(-5, -5);
        Image animalIcon = iconObj.AddComponent<Image>();
        animalIcon.color = new Color(1, 1, 1, 0); // Start invisible (will be made visible by UpdateVisuals)
        animalIcon.raycastTarget = false; // Don't block clicks
        animalIcon.preserveAspect = true; // Maintain animal sprite proportions

        // Assign references via SerializedObject
        SerializedObject so = new SerializedObject(slotScript);
        so.FindProperty("slotBackground").objectReferenceValue = slotBackground;
        so.FindProperty("animalIcon").objectReferenceValue = animalIcon;
        so.ApplyModifiedProperties();

        // Ensure prefab folder exists
        string folderPath = System.IO.Path.GetDirectoryName(assetPath);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string[] folders = folderPath.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }

        // Save as prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(slot, assetPath);
        DestroyImmediate(slot); // Remove from scene

        return prefab;
    }
#endif
}

} // namespace SowurShield.Combat
