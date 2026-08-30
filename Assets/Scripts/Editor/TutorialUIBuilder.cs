using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.Core;
using SowurShield.UI;

namespace SowurShield.Editor
{

/// <summary>
/// Builds the tutorial panel and wires TutorialManager into the scene.
///
/// TutorialManager is a finished six-step tutorial — till, plant, water, pet, sleep,
/// harvest — with all thirteen strings translated in EN/PT/ES. It had never run once:
/// no GameObject carried the component, so `TutorialManager.Instance` was always null and
/// SaveManager's `TutorialManager.Instance?.StartTutorial()` silently did nothing on every
/// new game. Its first step reads "equip the Hoe from your hotbar", which only became true
/// when new saves started shipping with one.
///
/// The panel is a slim bar above the hotbar, built from the HUD's own topbar_background so
/// it reads as part of the interface rather than a window laid over it.
///
/// It began as a 980x470 wooden window copying the pause menu. That covered 22% of the
/// screen and its band (y 76..546) swallowed the player (y 482..598), so a tutorial whose
/// first instruction is "click a soil block" stood in front of both the soil and the
/// character. The frame art was most of the problem: panel_wood_generic paints 236px of
/// vertical border before any content fits.
///
/// Menu: Sowur Shield > UI > Build Tutorial UI
/// </summary>
public static class TutorialUIBuilder
{
    private const string CanvasName = "TutorialCanvas";

    // A single row, sized against the real strings rather than a placeholder. The longest
    // step is 154 characters; measured in play mode it wraps to two lines needing 70px of
    // text height, so 92px covers that plus padding. The HUD's own bars are 46-50px, so this
    // still reads as the same family rather than as a window.
    private const float BarHeight = 92f;
    private const float StepLabelWidth = 118f;   // "Passo 1 de 6" / "Step 1 of 6" / "Paso 1 de 6"
    private const float NextWidth = 150f;        // 5:1-ish against the 40px height
    private const float SkipWidth = 110f;

    [MenuItem("Sowur Shield/UI/Build Tutorial UI")]
    public static void Build() => Build(showDialogs: true);

    public static void Build(bool showDialogs)
    {
        var existing = GameObject.Find(CanvasName);
        if (existing != null)
        {
            // EditorUtility.DisplayDialog is modal and hard-hangs an MCP session, so the
            // caller decides whether prompting is safe.
            if (showDialogs && !EditorUtility.DisplayDialog("Rebuild Tutorial UI",
                    $"This will DELETE the existing {CanvasName} and recreate it.\nContinue?",
                    "Yes, rebuild", "Cancel"))
                return;

            Undo.DestroyObjectImmediate(existing);
        }

        UITheme theme = Resources.Load<UITheme>("UI/CozyUITheme");
        Color cream    = theme != null ? theme.backgroundCream : new Color(0.969f, 0.949f, 0.910f);
        Color textDark = theme != null ? theme.textDark        : new Color(0.176f, 0.165f, 0.149f);
        Color woodDark = theme != null ? theme.woodDark        : new Color(0.420f, 0.267f, 0.137f);

        // ── Canvas ───────────────────────────────────────────────────────────────
        var canvasGO = new GameObject(CanvasName);
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create TutorialCanvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Above the HUD, below the pause menu and dialogue so it never traps the player.
        canvas.sortingOrder = 60;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Panel: a slim bar above the hotbar ───────────────────────────────────
        //
        // This replaces a 980x470 wooden window. That panel covered 22% of the screen and,
        // measured in play mode, its band (y 76..546) swallowed the player (y 482..598) --
        // so a tutorial whose first step is "click a soil block" stood in front of the soil
        // and the character. Lucas reported it as "can't see the player, it's weird".
        //
        // Two things forced the old size, and both are avoided here rather than tuned:
        // panel_wood_generic paints 236px of vertical frame (86 top + 150 bottom) before any
        // content fits, and the title, body and two buttons were stacked vertically.
        // topbar_background is the HUD's own bar art -- 99% interior, almost no frame -- and
        // the row is laid out horizontally, so the whole thing fits in 72px.
        var panel = new GameObject("TutorialPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasGO.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot     = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(1120f, BarHeight);
        // The hotbar occupies y 6..54, so this clears it with a small gap.
        panelRect.anchoredPosition = new Vector2(0f, 68f);

        var frame = panel.AddComponent<Image>();
        frame.sprite = LoadSprite("Assets/Resources/Sprites/UI/Bars/topbar_background.png");
        frame.type = Image.Type.Sliced;

        // The bar art has essentially no painted frame (measured: 99% interior), unlike
        // panel_wood_generic. A small uniform breathing margin is all that is needed.
        const float PadX = 22f;
        const float PadY = 10f;

        // Title and body share one line: "Passo 1 de 6" as a lead-in, then the instruction.
        // Stacking them was part of what made the old panel tall.
        var title = CreateText(panel.transform, "StepCountText",
            "Passo 1 de 6", 17, FontStyles.Bold, cream, TextAlignmentOptions.Left);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot     = new Vector2(0f, 0.5f);
        titleRect.offsetMin = new Vector2(PadX, PadY);
        titleRect.offsetMax = new Vector2(PadX + StepLabelWidth, -PadY);
        title.textWrappingMode = TextWrappingModes.NoWrap;

        var body = CreateText(panel.transform, "StepText",
            "Equipe a Enxada e clique com o botão esquerdo em um bloco de terra.",
            17, FontStyles.Normal, cream, TextAlignmentOptions.Left);
        var bodyRect = body.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(PadX + StepLabelWidth + 14f, PadY);
        bodyRect.offsetMax = new Vector2(-(PadX + NextWidth + SkipWidth + 24f), -PadY);
        body.textWrappingMode = TextWrappingModes.Normal;
        // Two lines at most in a 52px interior; the longest step still has to fit.
        body.enableAutoSizing = true;
        body.fontSizeMin = 13f;
        body.fontSizeMax = 17f;

        // Buttons sit on the right of the same row, smaller than the old 210x44 pair.
        var next = CreateButton(panel.transform, "NextButton", "Entendi",
            "Assets/Resources/Sprites/UI/Buttons/button_primary.png", textDark);
        var nextRect = next.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(1f, 0.5f);
        nextRect.anchorMax = new Vector2(1f, 0.5f);
        nextRect.pivot     = new Vector2(1f, 0.5f);
        nextRect.sizeDelta = new Vector2(NextWidth, 40f);
        nextRect.anchoredPosition = new Vector2(-PadX, 0f);

        var skip = CreateButton(panel.transform, "SkipButton", "Pular",
            "Assets/Resources/Sprites/UI/Buttons/button_secondary.png", textDark);
        var skipRect = skip.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1f, 0.5f);
        skipRect.anchorMax = new Vector2(1f, 0.5f);
        skipRect.pivot     = new Vector2(1f, 0.5f);
        skipRect.sizeDelta = new Vector2(SkipWidth, 40f);
        skipRect.anchoredPosition = new Vector2(-(PadX + NextWidth + 10f), 0f);

        // TutorialManager rewrites the step text and the counter every step but never the
        // button captions, so without this they stay in whatever language is baked in here.
        // The old panel had the same gap; it is worth closing while the buttons are moving.
        LocalizeLabel(next, "ui_common.got_it");
        LocalizeLabel(skip, "ui_common.skip");

        panel.SetActive(false); // StartTutorial turns it on

        // ── Manager ──────────────────────────────────────────────────────────────
        // On the canvas itself so one object carries the whole feature; TutorialManager
        // handles its own singleton and persistence.
        var manager = canvasGO.AddComponent<TutorialManager>();

        var so = new SerializedObject(manager);
        so.FindProperty("tutorialPanel").objectReferenceValue  = panel;
        so.FindProperty("stepText").objectReferenceValue       = body;
        so.FindProperty("stepCountText").objectReferenceValue  = title;
        so.FindProperty("skipButton").objectReferenceValue     = skip.GetComponent<Button>();
        so.FindProperty("nextButton").objectReferenceValue     = next.GetComponent<Button>();
        WireLocalizedString(so, "tillSoilTitle_Localized",        "tutorial.till_soil.title");
        WireLocalizedString(so, "tillSoilDescription_Localized",  "tutorial.till_soil.description");
        WireLocalizedString(so, "plantSeedTitle_Localized",       "tutorial.plant_seed.title");
        WireLocalizedString(so, "plantSeedDescription_Localized", "tutorial.plant_seed.description");
        WireLocalizedString(so, "waterCropTitle_Localized",       "tutorial.water_crop.title");
        WireLocalizedString(so, "waterCropDescription_Localized", "tutorial.water_crop.description");
        WireLocalizedString(so, "petAnimalTitle_Localized",       "tutorial.pet_animal.title");
        WireLocalizedString(so, "petAnimalDescription_Localized", "tutorial.pet_animal.description");
        WireLocalizedString(so, "sleepTitle_Localized",           "tutorial.sleep.title");
        WireLocalizedString(so, "sleepDescription_Localized",     "tutorial.sleep.description");
        WireLocalizedString(so, "harvestTitle_Localized",         "tutorial.harvest.title");
        WireLocalizedString(so, "harvestDescription_Localized",   "tutorial.harvest.description");
        WireLocalizedString(so, "stepProgressText_Localized",     "tutorial.step_progress");
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        Debug.Log($"[TutorialUIBuilder] Built {CanvasName} with TutorialManager wired. " +
                  "Save the scene to keep it.");
    }

    /// <summary>
    /// Points a LocalizedString field at the "Tutorial" table by key. Assigning through
    /// SerializedObject rather than code keeps it identical to an Inspector-set reference.
    /// </summary>
    /// <summary>
    /// Binds a button's caption to the UI_Common table, matching how the rest of the
    /// project's static text is localized.
    /// </summary>
    private static void LocalizeLabel(GameObject button, string key)
    {
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;

        var evt = label.gameObject
            .AddComponent<UnityEngine.Localization.Components.LocalizeStringEvent>();

        var so = new SerializedObject(evt);
        SerializedProperty reference = so.FindProperty("m_StringReference");
        reference.FindPropertyRelative("m_TableReference")
                 .FindPropertyRelative("m_TableCollectionName").stringValue = "UI_Common";
        reference.FindPropertyRelative("m_TableEntryReference")
                 .FindPropertyRelative("m_Key").stringValue = key;
        so.ApplyModifiedProperties();

        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            evt.OnUpdateString, new UnityEngine.Events.UnityAction<string>(label.SetText));
    }

    private static void WireLocalizedString(SerializedObject so, string fieldName, string key)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"[TutorialUIBuilder] No field '{fieldName}' on TutorialManager.");
            return;
        }

        SerializedProperty table = prop.FindPropertyRelative("m_TableReference")
                                      ?.FindPropertyRelative("m_TableCollectionName");
        SerializedProperty entry = prop.FindPropertyRelative("m_TableEntryReference")
                                      ?.FindPropertyRelative("m_Key");

        if (table == null || entry == null)
        {
            Debug.LogWarning($"[TutorialUIBuilder] '{fieldName}' does not look like a " +
                             "LocalizedString; leaving it unset.");
            return;
        }

        table.stringValue = "Tutorial";
        entry.stringValue = key;
    }

    private static Sprite LoadSprite(string path)
    {
        // These import as spriteMode Multiple, so LoadAssetAtPath<Sprite> returns null.
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite sprite)
                return sprite;

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text,
                                              float size, FontStyles style, Color colour,
                                              TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = colour;
        tmp.alignment = align;
        tmp.raycastTarget = false;

        return tmp;
    }

    private static GameObject CreateButton(Transform parent, string name, string label,
                                           string spritePath, Color labelColour)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.sprite = LoadSprite(spritePath);
        image.type = Image.Type.Sliced;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        // The painted pill fills roughly 65% of a button rect, so the label is inset rather
        // than stretched edge to edge — a label sized to the rect overflows the art.
        var text = CreateText(go.transform, "Label", label, 16, FontStyles.Normal,
                              labelColour, TextAlignmentOptions.Center);
        var rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(18f, 6f);
        rect.offsetMax = new Vector2(-18f, -6f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 16f;

        return go;
    }
}

}
