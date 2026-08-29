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
/// The panel deliberately copies the pause menu's look — wood frame, cream interior, gold
/// button — because that is the most resolved piece of UI in the game and the tutorial is
/// the first thing a new player sees.
///
/// Placement is bottom-centre, above the hotbar (which occupies y 2..62) and clear of the
/// dialogue box, so it never covers what it is telling the player to click.
///
/// Menu: Sowur Shield > UI > Build Tutorial UI
/// </summary>
public static class TutorialUIBuilder
{
    private const string CanvasName = "TutorialCanvas";

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

        // ── Panel: bottom-centre, clear of the hotbar ────────────────────────────
        var panel = new GameObject("TutorialPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasGO.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot     = new Vector2(0.5f, 0f);
        // 86 + 150 = 236px of vertical frame art alone, so a 200px panel had no interior at
        // all — everything drew on the wood. Height covers the frame plus room for a title,
        // three lines of body text and a button row.
        // 86 top + 44 title + 12 + ~76 body (4 lines at the 14pt auto-size floor) + 16 +
        // 44 buttons + 150 bottom = 428 minimum; 470 leaves room for the longer steps
        // rather than sitting exactly on the limit.
        panelRect.sizeDelta = new Vector2(980f, 470f);
        panelRect.anchoredPosition = new Vector2(0f, 76f); // hotbar tops out at y=62

        var frame = panel.AddComponent<Image>();
        frame.sprite = LoadSprite("Assets/Resources/Sprites/UI/Panels/panel_wood_generic.png");
        frame.type = Image.Type.Sliced;

        // The painted border is far wider than the 32px 9-slice value, and it is not
        // symmetric. Sampling panel_wood_generic's interior gives where the cream actually
        // starts: 82px left, 113px right, 86px top, 150px bottom of a 512px sprite. Sliced
        // keeps borders at fixed pixel size, so these carry over directly.
        //
        // A first pass used a flat 46px and the text visibly ran onto the wood on both
        // sides — the rect was never the problem, the art inside it was.
        // A few px past the measured edge on each side: the sampled value is where the cream
        // begins, and text starting exactly there still reads as touching the wood.
        const float InsetLeft   = 92f;
        const float InsetRight  = 125f;
        const float InsetTop    = 86f;
        const float InsetBottom = 150f;

        var title = CreateText(panel.transform, "StepCountText",
            "Passo 1 de 6 — Are o solo", 22, FontStyles.Bold, textDark,
            TextAlignmentOptions.Top);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot     = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(InsetLeft, -(InsetTop + 44f));
        titleRect.offsetMax = new Vector2(-InsetRight, -InsetTop);

        var body = CreateText(panel.transform, "StepText",
            "Equipe a Enxada e clique com o botão esquerdo em um bloco de terra.",
            18, FontStyles.Normal, textDark, TextAlignmentOptions.Top);
        var bodyRect = body.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(InsetLeft, InsetBottom + 64f);   // clears the button row
        bodyRect.offsetMax = new Vector2(-InsetRight, -(InsetTop + 52f)); // clears the title
        body.enableWordWrapping = true;
        // Descriptions vary a lot in length across the six steps; shrink rather than clip.
        body.enableAutoSizing = true;
        body.fontSizeMin = 14f;
        body.fontSizeMax = 18f;

        var skip = CreateButton(panel.transform, "SkipButton", "Pular tutorial",
            "Assets/Resources/Sprites/UI/Buttons/button_secondary.png", textDark);
        var skipRect = skip.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(0f, 0f);
        skipRect.anchorMax = new Vector2(0f, 0f);
        skipRect.pivot     = new Vector2(0f, 0f);
        skipRect.sizeDelta = new Vector2(210f, 44f);
        skipRect.anchoredPosition = new Vector2(InsetLeft, InsetBottom + 8f);

        var next = CreateButton(panel.transform, "NextButton", "Entendi",
            "Assets/Resources/Sprites/UI/Buttons/button_primary.png", textDark);
        var nextRect = next.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(1f, 0f);
        nextRect.anchorMax = new Vector2(1f, 0f);
        nextRect.pivot     = new Vector2(1f, 0f);
        nextRect.sizeDelta = new Vector2(210f, 44f);
        nextRect.anchoredPosition = new Vector2(-InsetRight, InsetBottom + 8f);

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
