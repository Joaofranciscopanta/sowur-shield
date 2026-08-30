using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SowurShield.Editor
{

/// <summary>
/// Narrows the dialogue box and gives it a framed portrait on the left.
///
/// <para>The box ran the full width of the screen: 1891px of 1920, with the text spanning
/// 1010px at 18pt. That works out to roughly 112 characters per line, against the 45-75 that
/// reads comfortably -- long enough that the eye loses its place coming back to the left
/// margin. Lucas chose the narrow-with-portrait layout over widening the font.</para>
///
/// <para>The portrait is the villager's own world sprite, framed head-and-shoulders. The nine
/// files in Resources/Portraits are 64x80 five-colour silhouettes with no face on them (only
/// Maren's is real art), while every villager has ~450x900 drawn art standing in the scene --
/// see NPCDialogueInteractable.GetPortrait. The frame crops to the top of the sprite rather
/// than centring it, because centring a full-body sprite in a square shows the character's
/// waist.</para>
///
/// Menu: Sowur Shield > UI > Restyle Dialogue Box
/// </summary>
public static class RestyleDialogueBox
{
    // 820px of a 1920 screen. Measured at 900px the text still ran 77 characters per line,
    // just over the 45-75 band; trimming the panel brings it to about 68.
    private static readonly Vector2 PanelSize = new Vector2(820f, 210f);

    private const float PortraitSize = 150f;
    private const float Pad = 18f;

    [MenuItem("Sowur Shield/UI/Restyle Dialogue Box")]
    public static void Restyle()
    {
        RectTransform panel = Object.FindObjectsByType<RectTransform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(r => r.name == "DialoguePanel");

        if (panel == null)
        {
            Debug.LogError("[RestyleDialogueBox] No DialoguePanel in the open scene.");
            return;
        }

        // Bottom-centre, a fixed width rather than stretched edge to edge.
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.sizeDelta = PanelSize;
        panel.anchoredPosition = new Vector2(0f, 24f);

        // ── Portrait: left, framed, cropped to the head ──────────────────────────
        RectTransform left = panel.Find("LeftPortrait") as RectTransform;
        if (left != null)
        {
            // EnsureHeadCrop owns the portrait's rect from here: it wraps it in a masked
            // frame and scales it up so the head fills the square.
            EnsureHeadCrop(left);
        }

        // The right-hand portrait has no place in a narrow box; the speaker is always on the
        // left now. It is disabled rather than deleted so a two-speaker scene can restore it.
        RectTransform right = panel.Find("RightPortrait") as RectTransform;
        if (right != null) right.gameObject.SetActive(false);

        // ── Name and body text, to the right of the portrait ─────────────────────
        float textLeft = Pad + 18f + PortraitSize + Pad;

        RectTransform name = panel.Find("SpeakerNameText") as RectTransform;
        if (name != null)
        {
            name.anchorMin = new Vector2(0f, 1f);
            name.anchorMax = new Vector2(1f, 1f);
            name.pivot = new Vector2(0.5f, 1f);
            name.offsetMin = new Vector2(textLeft, -46f);
            name.offsetMax = new Vector2(-Pad, -14f);

            var t = name.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.alignment = TextAlignmentOptions.TopLeft;
                t.fontSize = 20f;
                t.fontStyle = FontStyles.Bold;
            }
        }

        RectTransform body = panel.Find("DialogueText") as RectTransform;
        if (body != null)
        {
            body.anchorMin = new Vector2(0f, 0f);
            body.anchorMax = new Vector2(1f, 1f);
            body.pivot = new Vector2(0.5f, 0.5f);
            body.offsetMin = new Vector2(textLeft, Pad + 20f);   // clears the continue arrow
            body.offsetMax = new Vector2(-Pad, -50f);            // clears the name line

            var t = body.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.alignment = TextAlignmentOptions.TopLeft;
                t.fontSize = 18f;
                t.textWrappingMode = TextWrappingModes.Normal;
                // Long nodes should shrink rather than overflow a box that no longer has the
                // whole screen to spread into.
                t.enableAutoSizing = true;
                t.fontSizeMin = 14f;
                t.fontSizeMax = 18f;
            }
        }

        EditorUtility.SetDirty(panel.gameObject);
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);

        Debug.Log($"[RestyleDialogueBox] Panel {PanelSize.x}x{PanelSize.y}, portrait " +
                  $"{PortraitSize}px. Save the scene.");
    }

    /// <summary>
    /// Frames the portrait so a full-body sprite shows its head instead of being squashed
    /// into a square.
    /// </summary>
    /// <remarks>
    /// The Image stays on LeftPortrait itself rather than moving to a child: PortraitManager
    /// holds a serialized reference to that exact Image and writes the sprite straight into
    /// it, so a child would end up holding the frame while the sprite went somewhere
    /// invisible. Instead the mask goes on a parent created around it.
    /// </remarks>
    private static void EnsureHeadCrop(RectTransform portrait)
    {
        Transform parent = portrait.parent;

        // A frame object wraps the portrait and does the clipping.
        RectTransform frame = parent.Find("PortraitFrame") as RectTransform;
        if (frame == null)
        {
            var go = new GameObject("PortraitFrame", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create portrait frame");
            go.transform.SetParent(parent, false);
            frame = (RectTransform)go.transform;
            go.AddComponent<RectMask2D>();
        }

        frame.anchorMin = new Vector2(0f, 0.5f);
        frame.anchorMax = new Vector2(0f, 0.5f);
        frame.pivot = new Vector2(0f, 0.5f);
        frame.sizeDelta = new Vector2(PortraitSize, PortraitSize);
        // The panel art paints a wooden border; +18 keeps the portrait off the woodwork
        // instead of flush against it.
        frame.anchoredPosition = new Vector2(Pad + 18f, 0f);
        frame.SetSiblingIndex(0);

        // Re-parent the portrait under the frame and blow it up so the head fills the square.
        Undo.SetTransformParent(portrait, frame, "Frame portrait");
        portrait.anchorMin = new Vector2(0.5f, 1f);
        portrait.anchorMax = new Vector2(0.5f, 1f);
        portrait.pivot = new Vector2(0.5f, 1f);
        portrait.sizeDelta = new Vector2(PortraitSize * 1.9f, PortraitSize * 1.9f);
        // 0.30 clipped the top of Joana's hat; 0.22 leaves headroom for hats and hair
        // while still framing the head rather than the waist.
        portrait.anchoredPosition = new Vector2(0f, PortraitSize * 0.22f);

        var img = portrait.GetComponent<Image>();
        if (img != null)
        {
            // Keep the character's proportions; the mask handles the cropping.
            img.preserveAspect = true;
            img.type = Image.Type.Simple;
        }
    }
}

}
