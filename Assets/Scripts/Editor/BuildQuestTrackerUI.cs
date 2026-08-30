using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.Dialogue;

namespace SowurShield.Editor
{

/// <summary>
/// Builds the quest tracker panel and puts QuestTrackerUI in the scene.
///
/// <para>QuestTrackerUI is finished -- it subscribes to QuestManager's started / objective
/// updated / completed events and refreshes itself -- but no GameObject carried the component
/// and its four UI references were never created, so the player's current objective was never
/// shown anywhere. Exactly the shape of the tutorial problem: the feature was written and left
/// unplugged.</para>
///
/// <para>Placed under the stamina readout on the left, which is the only free corner: money
/// and the clock own the top strip, the minimap owns the top right, and the hotbar plus the
/// tutorial bar own the bottom.</para>
///
/// Menu: Sowur Shield > UI > Build Quest Tracker UI
/// </summary>
public static class BuildQuestTrackerUI
{
    private const string PanelName = "QuestTracker";

    // Narrow enough to sit beside the play area rather than over it, tall enough for a title
    // line, an objective line and a progress bar.
    private static readonly Vector2 PanelSize = new Vector2(300f, 86f);

    [MenuItem("Sowur Shield/UI/Build Quest Tracker UI")]
    public static void Build()
    {
        Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                         FindObjectsSortMode.None)
            .FirstOrDefault(c => c.name == "UI" && c.isRootCanvas);

        if (canvas == null)
        {
            Debug.LogError("[BuildQuestTrackerUI] No root canvas named 'UI'.");
            return;
        }

        Transform hud = canvas.transform.Find("HUD") ?? canvas.transform;

        Transform existing = hud.Find(PanelName);
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        // ── Panel ────────────────────────────────────────────────────────────────
        var panelGO = new GameObject(PanelName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(panelGO, "Create quest tracker");
        panelGO.transform.SetParent(hud, false);

        var rect = (RectTransform)panelGO.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = PanelSize;
        // Below the stamina bar, which occupies roughly y -8..-44 from the top-left.
        rect.anchoredPosition = new Vector2(20f, -58f);

        var bg = panelGO.AddComponent<Image>();
        // The HUD's own bar art, so this reads as part of the interface rather than a window.
        bg.sprite = LoadSprite("Assets/Resources/Sprites/UI/Bars/topbar_background.png");
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = false;

        const float pad = 12f;
        Color cream = new Color(0.969f, 0.949f, 0.910f);

        var title = CreateLabel(panelGO.transform, "QuestTitleText", "Primeira Colheita",
                                17f, FontStyles.Bold, cream);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(pad, -30f);
        titleRect.offsetMax = new Vector2(-pad, -6f);

        var objective = CreateLabel(panelGO.transform, "ObjectiveText", "Colha 1 plantação",
                                    14f, FontStyles.Normal, cream);
        var objRect = objective.rectTransform;
        objRect.anchorMin = new Vector2(0f, 1f);
        objRect.anchorMax = new Vector2(1f, 1f);
        objRect.pivot = new Vector2(0.5f, 1f);
        objRect.offsetMin = new Vector2(pad, -56f);
        objRect.offsetMax = new Vector2(-pad, -32f);
        objective.enableAutoSizing = true;
        objective.fontSizeMin = 11f;
        objective.fontSizeMax = 14f;

        // ── Progress bar ─────────────────────────────────────────────────────────
        var trackGO = new GameObject("ProgressTrack", typeof(RectTransform));
        trackGO.transform.SetParent(panelGO.transform, false);
        var trackRect = (RectTransform)trackGO.transform;
        trackRect.anchorMin = new Vector2(0f, 0f);
        trackRect.anchorMax = new Vector2(1f, 0f);
        trackRect.pivot = new Vector2(0.5f, 0f);
        trackRect.offsetMin = new Vector2(pad, 10f);
        trackRect.offsetMax = new Vector2(-pad, 20f);

        var trackImg = trackGO.AddComponent<Image>();
        trackImg.color = new Color(0f, 0f, 0f, 0.35f);
        trackImg.raycastTarget = false;

        var fillGO = new GameObject("ProgressBar", typeof(RectTransform));
        fillGO.transform.SetParent(trackGO.transform, false);
        var fillRect = (RectTransform)fillGO.transform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        var fill = fillGO.AddComponent<Image>();
        fill.color = new Color(0.45f, 0.80f, 0.35f);
        fill.raycastTarget = false;
        // A Filled image with no sprite ignores fillAmount entirely and paints the whole
        // rect, so an empty bar rendered as a full green one. Any opaque sprite works as the
        // fill source; the tint does the rest.
        fill.sprite = LoadSprite("Assets/Resources/Sprites/UI/Slots/slot_inventory.png");
        // QuestTrackerUI drives this through fillAmount, which needs a Filled image.
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;

        // ── Component ────────────────────────────────────────────────────────────
        var host = canvas.gameObject.GetComponent<QuestTrackerUI>()
                   ?? Undo.AddComponent<QuestTrackerUI>(canvas.gameObject);

        var so = new SerializedObject(host);
        so.FindProperty("trackerPanel").objectReferenceValue    = panelGO;
        so.FindProperty("questTitleText").objectReferenceValue  = title;
        so.FindProperty("objectiveText").objectReferenceValue   = objective;
        so.FindProperty("progressBar").objectReferenceValue     = fill;
        so.ApplyModifiedProperties();

        // Start() hides it; it shows itself when a quest starts.
        panelGO.SetActive(false);

        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log($"[BuildQuestTrackerUI] '{PanelName}' built under '{hud.name}' and wired. " +
                  "Save the scene.");
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
                                               float size, FontStyles style, Color colour)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var label = go.AddComponent<TextMeshProUGUI>();

        // Font first: a TextMeshProUGUI built from code has none, and TMP throws on the first
        // material-backed property. Nunito is also the only atlas carrying accents.
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Nunito SDF.asset");
        if (font != null) label.font = font;

        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = colour;
        label.alignment = TextAlignmentOptions.Left;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static Sprite LoadSprite(string path)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite sprite) return sprite;
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}

}
