using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using SowurShield.Core;
using SowurShield.UI;

namespace SowurShield.Editor
{

/// <summary>
/// Builds the missing language UI pieces directly inside the currently open MainMenu scene:
/// a "Language" dropdown inside the existing settingsPanel, and a first-boot LanguageSelectPanel
/// with EN/PT/ES buttons. Wires both into the MainMenuUI component's serialized fields.
/// Menu: Tools > Sowur Shield > Build Language UI (in open MainMenu scene)
/// </summary>
public static class LanguageUIBuilder
{
    [MenuItem("Tools/Sowur Shield/Reset First-Boot Language Flag")]
    public static void ResetFirstBootLanguageFlag()
    {
        PlayerPrefs.DeleteKey(SowurShield.Core.LocalizationManager.PlayerPrefsKey);
        PlayerPrefs.Save();
        EditorUtility.DisplayDialog("Reset First-Boot Language Flag",
            "Deleted the saved 'Language' PlayerPrefs key.\nNext Play will show the first-boot language select panel again.",
            "OK");
    }

    [MenuItem("Tools/Sowur Shield/Build Language UI (in open MainMenu scene)")]
    public static void BuildLanguageUI()
    {
        var menuUI = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        if (menuUI == null)
        {
            EditorUtility.DisplayDialog("Build Language UI",
                "No MainMenuUI component found in any open scene.\nOpen Assets/Scenes/MainMenu.unity first.", "OK");
            return;
        }

        var so = new SerializedObject(menuUI);
        UITheme theme = Resources.Load<UITheme>("UI/CozyUITheme");

        GameObject settingsPanel = (so.FindProperty("settingsPanel").objectReferenceValue as GameObject);
        GameObject mainPanel = (so.FindProperty("mainPanel").objectReferenceValue as GameObject);

        if (settingsPanel == null || mainPanel == null)
        {
            EditorUtility.DisplayDialog("Build Language UI",
                "MainMenuUI is missing settingsPanel or mainPanel references — wire those first.", "OK");
            return;
        }

        bool dropdownBuilt = BuildLanguageDropdown(so, settingsPanel, "languageDropdown", theme);
        bool promptBuilt = BuildLanguageSelectPanel(so, mainPanel, theme);

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(menuUI.gameObject.scene);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Build Language UI — Done",
            $"Language dropdown created in settingsPanel: {dropdownBuilt}\n" +
            $"First-boot language panel created next to mainPanel: {promptBuilt}\n\n" +
            "Both are now wired into MainMenuUI's serialized fields. Save the scene (Ctrl+S).",
            "OK");
    }

    [MenuItem("Tools/Sowur Shield/Build Language UI (in open gameplay scene's pause menu)")]
    public static void BuildPauseMenuLanguageUI()
    {
        var menuUI = Object.FindFirstObjectByType<GameMenuUI>(FindObjectsInactive.Include);
        if (menuUI == null)
        {
            EditorUtility.DisplayDialog("Build Pause Menu Language UI",
                "No GameMenuUI component found in any open scene.\nOpen the gameplay scene (e.g. Assets/Scenes/SampleScene.unity) first.", "OK");
            return;
        }

        var so = new SerializedObject(menuUI);
        UITheme theme = Resources.Load<UITheme>("UI/CozyUITheme");

        GameObject settingsPanel = (so.FindProperty("settingsPanel").objectReferenceValue as GameObject);

        if (settingsPanel == null)
        {
            EditorUtility.DisplayDialog("Build Pause Menu Language UI",
                "GameMenuUI is missing its settingsPanel reference — wire that first.", "OK");
            return;
        }

        bool dropdownBuilt = BuildLanguageDropdown(so, settingsPanel, "languageDropdown", theme);

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(menuUI.gameObject.scene);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Build Pause Menu Language UI — Done",
            $"Language dropdown created in pause menu's settingsPanel: {dropdownBuilt}\n\n" +
            "Wired into GameMenuUI's languageDropdown field. Save the scene (Ctrl+S).",
            "OK");
    }

    private static bool BuildLanguageDropdown(SerializedObject so, GameObject settingsPanel, string dropdownPropName, UITheme theme)
    {
        // Always tear down and rebuild rather than trusting an existing wired reference. A non-null
        // `template` only proves the dropdown can open — it says nothing about correct parenting,
        // anchors, or layout, which is exactly what earlier rounds got wrong while still leaving a
        // "valid" (non-null template) dropdown behind. Treating that as "already wired, skip" is why
        // re-running this tool after a code fix silently no-opped on the stale, still-broken object.
        SerializedProperty dropdownProp = so.FindProperty(dropdownPropName);
        var wiredDropdown = dropdownProp.objectReferenceValue as TMP_Dropdown;
        if (wiredDropdown != null)
        {
            dropdownProp.objectReferenceValue = null;
            Transform wiredRow = wiredDropdown.transform.parent;
            if (wiredRow != null && wiredRow.name == "LanguageRow")
                Object.DestroyImmediate(wiredRow.gameObject);
            else
                Object.DestroyImmediate(wiredDropdown.gameObject);
        }

        // Find a known-good TMP_Dropdown already in this panel/component to clone — this avoids
        // hand-building TMP_Dropdown's template/Viewport/Content/Item hierarchy, which is fragile
        // and previously produced a dropdown with no usable template (crashed on click) and a
        // caption Text that rendered one character per line.
        SerializedProperty templateSourceProp = so.FindProperty("resolutionDropdown");
        TMP_Dropdown templateSource = templateSourceProp?.objectReferenceValue as TMP_Dropdown;
        if (templateSource == null)
        {
            EditorUtility.DisplayDialog("Build Language Dropdown",
                "Could not find an existing resolutionDropdown to clone from. Wire resolutionDropdown first, or build the language dropdown manually.", "OK");
            return false;
        }

        // Two different scenes turned out to have two different structures: in MainMenu.unity the
        // resolution dropdown sits inside a dedicated row wrapper (e.g. "VideoSection"), one level
        // below settingsPanel; in SampleScene.unity (pause menu) the dropdown is parented *directly*
        // under settingsPanel with no row wrapper at all. Always insert LanguageRow as a sibling of
        // the dropdown itself (same parent as the dropdown, not the dropdown's parent's parent) — this
        // works for both layouts and never escapes one level above settingsPanel by mistake.
        Transform resolutionDropdownTransform = templateSource.transform;
        Transform languageRowParent = resolutionDropdownTransform.parent != null
            ? resolutionDropdownTransform.parent
            : settingsPanel.transform;

        Transform existingRow = languageRowParent.Find("LanguageRow");
        if (existingRow != null)
            Object.DestroyImmediate(existingRow.gameObject);

        // Earlier broken build rounds could have left a "LanguageRow"/"LanguageDropdown" orphan
        // parented directly under settingsPanel (the old, incorrect parenting) that is no longer
        // referenced by dropdownProp and wouldn't be found by the lookups above. Sweep the whole
        // settingsPanel subtree and remove any stray copies before building a fresh one. Snapshot
        // to an array first since destroying a parent invalidates its children mid-enumeration.
        var strays = settingsPanel.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in strays)
        {
            if (t == null || t.gameObject == null) continue;
            if ((t.name == "LanguageRow" || t.name == "LanguageDropdown") && t != existingRow)
                Object.DestroyImmediate(t.gameObject);
        }

        Color textDark = theme != null ? theme.textDark : new Color(0.18f, 0.17f, 0.15f);

        // languageRowParent has no LayoutGroup of its own in either scene — existing rows/controls are
        // hand-placed with explicit anchors/anchoredPosition/sizeDelta, not flow-laid-out. A row meant
        // to be driven by a parent LayoutGroup (anchorMin/Max = 0.5,0.5 with size coming from outside)
        // has zero effective width here, which is what made the cloned dropdown's caption wrap one
        // letter per line. Give the row an explicit rect instead, copying the resolution dropdown's
        // own anchors/width and offsetting downward by its own height + spacing — this places it
        // immediately below the dropdown regardless of whether that dropdown lives in a row wrapper
        // or directly under settingsPanel.
        var rowGO = new GameObject("LanguageRow");
        rowGO.transform.SetParent(languageRowParent, false);
        rowGO.transform.SetSiblingIndex(resolutionDropdownTransform.GetSiblingIndex() + 1);
        var rowRT = rowGO.AddComponent<RectTransform>();
        var dropdownSourceRT = resolutionDropdownTransform as RectTransform;
        float rowWidth = dropdownSourceRT != null && dropdownSourceRT.sizeDelta.x > 0 ? dropdownSourceRT.sizeDelta.x : 200;
        float rowHeight = dropdownSourceRT != null && dropdownSourceRT.sizeDelta.y > 0 ? dropdownSourceRT.sizeDelta.y : 36;
        Vector2 sourceAnchorMin = dropdownSourceRT != null ? dropdownSourceRT.anchorMin : new Vector2(0.5f, 0.5f);
        Vector2 sourceAnchorMax = dropdownSourceRT != null ? dropdownSourceRT.anchorMax : new Vector2(0.5f, 0.5f);
        Vector2 sourcePos = dropdownSourceRT != null ? dropdownSourceRT.anchoredPosition : Vector2.zero;
        rowRT.anchorMin = sourceAnchorMin;
        rowRT.anchorMax = sourceAnchorMax;
        rowRT.pivot = dropdownSourceRT != null ? dropdownSourceRT.pivot : new Vector2(0.5f, 0.5f);
        float insertedSpace = rowHeight + 12;
        rowRT.anchoredPosition = new Vector2(sourcePos.x, sourcePos.y - insertedSpace);
        rowRT.sizeDelta = new Vector2(rowWidth + 140, rowHeight);

        // languageRowParent (e.g. "VideoSection") is itself one hand-placed block among several
        // sibling blocks (VideoSection, AudioSection, ...) stacked under a shared grandparent with no
        // LayoutGroup. Growing languageRowParent by inserting this row pushes its visual bottom edge
        // down by insertedSpace, which would now overlap whichever sibling block was positioned right
        // below it. Shift every sibling block that sits below languageRowParent down by that same
        // amount so the whole stack re-flows instead of colliding. Marked with a visible sentinel
        // GameObject (not hidden — easier to spot/delete by hand if this ever needs undoing) so
        // re-running this tool, which always destroys/rebuilds LanguageRow itself, does not push the
        // same siblings down again on every run.
        Transform languageRowParentParent = languageRowParent.parent;
        var languageRowParentRT = languageRowParent as RectTransform;
        const string shiftMarkerName = "LanguageUIBuilder_SiblingsAlreadyShifted";
        if (languageRowParentParent != null && languageRowParentRT != null
            && languageRowParentParent.Find(shiftMarkerName) == null)
        {
            float thisBlockY = languageRowParentRT.anchoredPosition.y;
            foreach (Transform sibling in languageRowParentParent)
            {
                if (sibling == languageRowParent) continue;
                var siblingRT = sibling as RectTransform;
                if (siblingRT == null) continue;
                if (siblingRT.anchoredPosition.y < thisBlockY)
                    siblingRT.anchoredPosition = new Vector2(siblingRT.anchoredPosition.x, siblingRT.anchoredPosition.y - insertedSpace);
            }
            new GameObject(shiftMarkerName).transform.SetParent(languageRowParentParent, false);
        }

        var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(rowGO.transform, false);
        var labelLE = labelGO.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 140;
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text = "Language";
        labelTMP.fontSize = 16;
        labelTMP.color = textDark;
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Clone the existing, working resolution dropdown wholesale — same RectTransform setup,
        // same Template/Viewport/Content/Item hierarchy, same fonts/materials.
        GameObject dropdownGO = Object.Instantiate(templateSource.gameObject, rowGO.transform);
        dropdownGO.name = "LanguageDropdown";

        // The clone inherits the source dropdown's anchors/sizeDelta, which were tuned for its
        // original row's layout context. Reset to a plain stretch rect so the new HorizontalLayoutGroup
        // (which drives size via LayoutElement, not anchors) has full control — otherwise the cloned
        // children (caption Label, Template) can end up with a stale/near-zero effective width and
        // TMP word-wraps the caption text one character per line.
        var dropdownRT = dropdownGO.GetComponent<RectTransform>();
        dropdownRT.anchorMin = new Vector2(0, 0.5f);
        dropdownRT.anchorMax = new Vector2(0, 0.5f);
        dropdownRT.pivot = new Vector2(0, 0.5f);
        dropdownRT.sizeDelta = new Vector2(200, 32);
        dropdownRT.anchoredPosition = Vector2.zero;

        var dropdownLE = dropdownGO.GetComponent<LayoutElement>();
        if (dropdownLE == null) dropdownLE = dropdownGO.AddComponent<LayoutElement>();
        dropdownLE.flexibleWidth = 1;
        dropdownLE.preferredHeight = 32;

        var dropdown = dropdownGO.GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent(); // strip cloned resolution listeners

        dropdownProp.objectReferenceValue = dropdown;

        // Edit-time structural changes (reparenting, adding LayoutElements) don't trigger Unity's
        // layout pass automatically — without forcing a rebuild here, the cloned caption Label and
        // Template can retain stale sizes from before the clone until something else (e.g. entering
        // Play Mode) happens to trigger one, which is what produced the vertical letter-by-letter
        // caption text and misaligned dropdown box seen in testing.
        LayoutRebuilder.ForceRebuildLayoutImmediate(rowRT);
        var settingsPanelRT = settingsPanel.GetComponent<RectTransform>();
        if (settingsPanelRT != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(settingsPanelRT);

        return true;
    }

    private static bool BuildLanguageSelectPanel(SerializedObject so, GameObject mainPanel, UITheme theme)
    {
        SerializedProperty panelProp = so.FindProperty("languageSelectPanel");
        if (panelProp.objectReferenceValue != null)
            return false; // already wired, don't duplicate

        Transform existing = mainPanel.transform.parent != null ? mainPanel.transform.parent.Find("LanguageSelectPanel") : null;
        if (existing != null)
        {
            WireLanguagePanel(so, existing.gameObject);
            return true;
        }

        Color backgroundCream = theme != null ? theme.backgroundCream : new Color(0.97f, 0.95f, 0.91f);
        Color textDark = theme != null ? theme.textDark : new Color(0.18f, 0.17f, 0.15f);
        Color highlightGold = theme != null ? theme.highlightGold : new Color(0.96f, 0.83f, 0.37f);

        var panelGO = new GameObject("LanguageSelectPanel");
        panelGO.transform.SetParent(mainPanel.transform.parent, false);

        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(420, 260);

        panelGO.AddComponent<Image>().color = backgroundCream;

        var vlg = panelGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(24, 24, 24, 24);
        vlg.spacing = 14;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        titleGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 36);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Select your language / Selecione seu idioma / Selecciona tu idioma";
        titleTMP.fontSize = 16;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = textDark;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.textWrappingMode = TextWrappingModes.Normal;

        Button englishButton = CreateLanguageButton(panelGO.transform, "EnglishButton", "English", highlightGold);
        Button portugueseButton = CreateLanguageButton(panelGO.transform, "PortugueseButton", "Português", highlightGold);
        Button spanishButton = CreateLanguageButton(panelGO.transform, "SpanishButton", "Español", highlightGold);

        panelGO.SetActive(false);

        panelProp.objectReferenceValue = panelGO;
        so.FindProperty("languageSelectEnglishButton").objectReferenceValue = englishButton;
        so.FindProperty("languageSelectPortugueseButton").objectReferenceValue = portugueseButton;
        so.FindProperty("languageSelectSpanishButton").objectReferenceValue = spanishButton;

        return true;
    }

    private static void WireLanguagePanel(SerializedObject so, GameObject panelGO)
    {
        so.FindProperty("languageSelectPanel").objectReferenceValue = panelGO;
        so.FindProperty("languageSelectEnglishButton").objectReferenceValue = panelGO.transform.Find("EnglishButton")?.GetComponent<Button>();
        so.FindProperty("languageSelectPortugueseButton").objectReferenceValue = panelGO.transform.Find("PortugueseButton")?.GetComponent<Button>();
        so.FindProperty("languageSelectSpanishButton").objectReferenceValue = panelGO.transform.Find("SpanishButton")?.GetComponent<Button>();
    }

    private static Button CreateLanguageButton(Transform parent, string name, string label, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 44);
        go.AddComponent<Image>().color = bgColor;
        var btn = go.AddComponent<Button>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }
}

} // namespace SowurShield.Editor
