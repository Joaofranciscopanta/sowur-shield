using TMPro;
using UnityEngine;

namespace SowurShield.Core
{

/// <summary>
/// Shows a small "E" badge over whatever the player can currently interact with.
///
/// <para>Nothing in the game told the player they could interact. Each interactable was
/// expected to show its own prompt: nine of the eleven NPCs pointed at an empty GameObject
/// with no renderer on it at all, and the combat trigger zone pointed at nothing, leaving its
/// label parked on a Screen Space canvas scaled to 0.01 -- it rendered at one pixel by four
/// tenths of a pixel, in English, in a Portuguese game. Walking up to a person or the entrance
/// to the whole combat mode looked exactly like walking up to scenery.</para>
///
/// <para>One badge driven off <see cref="InteractionManager"/> replaces eleven per-object
/// prompts that were never built. The manager already tracks the closest interactable and
/// already has the range rules; this just draws what it knows.</para>
///
/// <para>Deliberately just the key, not a sentence. The prompts the interactables return are
/// untranslated English ("Enter Dark Forest", "Press E to Enter"), and a floating "E" says
/// the same thing in every language without needing thirty new strings.</para>
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [Tooltip("How far above the target the badge floats, in world units.")]
    [SerializeField] private float heightAbove = 1.15f;

    [Tooltip("Size of the letter in world units.")]
    [SerializeField] private float fontSize = 3.2f;

    [SerializeField] private Color textColor = new Color(0.15f, 0.13f, 0.11f);
    [SerializeField] private Color badgeColor = new Color(0.98f, 0.84f, 0.35f, 0.95f);

    private InteractionManager manager;
    private TextMeshPro label;
    private SpriteRenderer badge;
    private Transform anchor;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        BuildBadge();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (manager == null)
        {
            manager = InteractionManager.Instance;
            if (manager == null) return;
        }

        IInteractable target = manager.GetCurrentInteractable();
        if (target == null || !manager.CanInteract())
        {
            SetVisible(false);
            return;
        }

        // IInteractable does not expose a transform, so reach for the MonoBehaviour behind it.
        var behaviour = target as MonoBehaviour;
        if (behaviour == null) { SetVisible(false); return; }

        // Sit above the drawn sprite rather than the pivot: the scene mixes feet-pivoted and
        // centre-pivoted art, so a fixed offset from the transform floats at a different
        // height on almost every object.
        var renderer = behaviour.GetComponentInChildren<SpriteRenderer>();
        float top = renderer != null ? renderer.bounds.max.y : behaviour.transform.position.y;

        anchor.position = new Vector3(behaviour.transform.position.x, top + heightAbove * 0.35f,
                                      behaviour.transform.position.z);
        // Reencontrar a camera: cada cena tem a sua, e a da cena anterior foi destruida.
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) anchor.rotation = mainCamera.transform.rotation;

        SetVisible(true);
    }

    private void BuildBadge()
    {
        var root = new GameObject("InteractionPromptBadge");
        root.transform.SetParent(transform, false);
        anchor = root.transform;

        // A solid plate behind the letter so it reads over grass, soil and water alike.
        // slot_selection_outline is hollow by design -- used here it framed empty ground and
        // the letter sat in a box rather than on a badge. A filled sprite tinted to the
        // badge colour is what makes this legible against any background.
        var badgeGO = new GameObject("Plate");
        badgeGO.transform.SetParent(anchor, false);
        badge = badgeGO.AddComponent<SpriteRenderer>();
        badge.sprite = Resources.Load<Sprite>("Sprites/UI/Slots/slot_grid_empty");
        badge.color = badgeColor;
        badge.drawMode = SpriteDrawMode.Sliced;
        badge.size = new Vector2(0.62f, 0.62f);
        // WorldUI is the top sorting layer (value 4). A high order on Default was not
        // enough: ambient decor is not Y-sorted and drew over the badge.
        badge.sortingLayerName = "WorldUI";
        badge.sortingOrder = 10;

        var labelGO = new GameObject("Key");
        labelGO.transform.SetParent(anchor, false);
        label = labelGO.AddComponent<TextMeshPro>();

        // Set the font before touching anything material-backed; a TextMeshPro built from code
        // has none, and TMP throws on the first such property.
        TMP_Text sample = FindFirstObjectByType<TMP_Text>();
        if (sample != null && sample.font != null) label.font = sample.font;

        label.text = "E";
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = textColor;
        label.alignment = TextAlignmentOptions.Center;
        // Both the plate and the label must be centred on the same point, or the glyph sits
        // off to one side of the badge -- which is exactly how it first rendered.
        label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        label.rectTransform.sizeDelta = new Vector2(0.62f, 0.62f);
        label.rectTransform.localPosition = Vector3.zero;
        badge.transform.localPosition = Vector3.zero;
        // TextMeshPro routes sorting through its Renderer, not through properties of its own.
        var labelRenderer = labelGO.GetComponent<Renderer>();
        if (labelRenderer != null)
        {
            labelRenderer.sortingLayerName = "WorldUI";
            labelRenderer.sortingOrder = 11;
        }
    }

    private void SetVisible(bool visible)
    {
        if (anchor != null && anchor.gameObject.activeSelf != visible)
            anchor.gameObject.SetActive(visible);
    }
}

}
