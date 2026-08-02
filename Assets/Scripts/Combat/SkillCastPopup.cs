using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SowurShield.Animals;

namespace SowurShield.Combat
{

/// <summary>
/// Shows a skill's icon and name over the caster for a moment when the skill
/// fires. Self-spawning like BattleHudOverlay, so it needs no scene wiring.
///
/// This is where the skill icons actually get seen: the Skill button renders one
/// at ~40px beside its caption, which is too small to read the art. Here it is
/// shown large, briefly, at the moment it is relevant.
/// </summary>
public class SkillCastPopup : MonoBehaviour
{
    private const float IconPixels = 96f;
    private const float HoldSeconds = 0.85f;
    private const float RiseWorldUnits = 0.6f;

    private static SkillCastPopup instance;

    private Canvas canvas;
    private RectTransform root;
    private Image iconImage;
    private TextMeshProUGUI label;

    private TurnManager boundManager;
    private Camera cam;

    private Vector3 worldAnchor;
    private float shownAt = -999f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawn()
    {
        // Only in the combat scene — TurnManager is the marker for that.
        if (Object.FindFirstObjectByType<TurnManager>() == null) return;
        if (instance != null) return;

        var go = new GameObject("SkillCastPopup");
        instance = go.AddComponent<SkillCastPopup>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        cam = Camera.main;
        BuildUI();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (boundManager != null) boundManager.OnTelegraph -= OnTelegraph;
    }

    private void Update()
    {
        // TurnManager is spawned on a delay, so bind lazily rather than in Awake.
        if (boundManager == null)
        {
            boundManager = Object.FindFirstObjectByType<TurnManager>();
            if (boundManager != null) boundManager.OnTelegraph += OnTelegraph;
            return;
        }

        if (!root.gameObject.activeSelf) return;

        float age = Time.unscaledTime - shownAt;
        if (age >= HoldSeconds) { SetVisible(false); return; }

        // Drift upward and fade out over its lifetime.
        float t = age / HoldSeconds;
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            Vector3 world = worldAnchor + Vector3.up * (RiseWorldUnits * t);
            root.position = cam.WorldToScreenPoint(world);
        }

        var group = root.GetComponent<CanvasGroup>();
        if (group != null) group.alpha = t < 0.75f ? 1f : Mathf.InverseLerp(1f, 0.75f, t);
    }

    private void OnTelegraph(TurnManager.TelegraphInfo info)
    {
        // Basic attacks broadcast with a null skill; nothing to show for those.
        if (info.skill == null || info.actor == null) return;

        iconImage.sprite  = info.skill.skillIcon;
        iconImage.enabled = info.skill.skillIcon != null;
        label.text = info.skill.skillName;

        worldAnchor = info.actor.transform.position + Vector3.up * 0.5f;
        shownAt = Time.unscaledTime;

        var group = root.GetComponent<CanvasGroup>();
        if (group != null) group.alpha = 1f;
        SetVisible(true);
    }

    private void SetVisible(bool on)
    {
        if (root != null) root.gameObject.SetActive(on);
    }

    private void BuildUI()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Above the grid and units, below the command panel (120) so it never
        // covers the buttons the player is reading.
        canvas.sortingOrder = 115;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var holder = new GameObject("Popup");
        holder.transform.SetParent(transform, false);
        root = holder.AddComponent<RectTransform>();
        root.sizeDelta = new Vector2(IconPixels + 40f, IconPixels + 46f);
        holder.AddComponent<CanvasGroup>();

        var vlg = holder.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2f;

        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(holder.transform, false);
        iconObj.AddComponent<RectTransform>();
        iconImage = iconObj.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        var iconLE = iconObj.AddComponent<LayoutElement>();
        iconLE.preferredHeight = IconPixels;
        iconLE.preferredWidth  = IconPixels;

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(holder.transform, false);
        labelObj.AddComponent<RectTransform>();
        label = labelObj.AddComponent<TextMeshProUGUI>();
        label.fontSize = 22;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        label.enableWordWrapping = false;
        // Dark outline so the name stays readable over both the red and green
        // halves of the grid.
        label.outlineWidth = 0.25f;
        label.outlineColor = new Color32(0, 0, 0, 255);
        labelObj.AddComponent<LayoutElement>().preferredHeight = 26f;
    }
}

} // namespace SowurShield.Combat
