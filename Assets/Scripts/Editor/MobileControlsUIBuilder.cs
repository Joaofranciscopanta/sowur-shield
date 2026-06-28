using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.OnScreen;
using UnityEditor;
using SowurShield.Core;
using SowurShield.UI;

namespace SowurShield.Editor
{

/// <summary>
/// Editor tool to build the MobileControlsCanvas (virtual joystick + action button)
/// from scratch with correct layout and wiring.
/// Menu: Tools > Sowur Shield > Rebuild Mobile Controls UI
/// </summary>
public class MobileControlsUIBuilder : EditorWindow
{
    [MenuItem("Tools/Sowur Shield/Rebuild Mobile Controls UI")]
    public static void RebuildUI()
    {
        var existing = GameObject.Find("MobileControlsCanvas");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Rebuild Mobile Controls UI",
                "This will DELETE the existing MobileControlsCanvas and recreate it.\nContinue?",
                "Yes, rebuild", "Cancel"))
                return;

            Undo.DestroyObjectImmediate(existing);
        }

        // ── Canvas ──────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("MobileControlsCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create MobileControlsCanvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // above gameplay HUD, below modal UI canvases

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Safe-area root ───────────────────────────────────────────────────────
        var safeAreaGO = new GameObject("SafeArea");
        safeAreaGO.transform.SetParent(canvasGO.transform, false);
        var safeAreaRT = safeAreaGO.AddComponent<RectTransform>();
        safeAreaRT.anchorMin = Vector2.zero;
        safeAreaRT.anchorMax = Vector2.one;
        safeAreaRT.offsetMin = Vector2.zero;
        safeAreaRT.offsetMax = Vector2.zero;
        safeAreaGO.AddComponent<SafeAreaFitter>();

        // ── Virtual joystick (bottom-left) ──────────────────────────────────────
        var joystickBase = CreateCircle(safeAreaGO.transform, "VirtualJoystick",
            new Vector2(0f, 0f), new Vector2(0.25f, 0.35f), new Color(1f, 1f, 1f, 0.25f));

        var joystickHandle = CreateCircle(joystickBase.transform, "Handle",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(1f, 1f, 1f, 0.6f));
        var handleRT = joystickHandle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(60, 60);

        var stick = joystickHandle.AddComponent<OnScreenStick>();
        stick.controlPath = "<Gamepad>/leftStick";

        // ── Action button (bottom-right) ────────────────────────────────────────
        var actionButtonGO = CreateCircle(safeAreaGO.transform, "ActionButton",
            new Vector2(0.8f, 0.06f), new Vector2(0.96f, 0.26f), new Color(0.96f, 0.83f, 0.37f, 0.85f));

        var onScreenButton = actionButtonGO.AddComponent<OnScreenButton>();
        onScreenButton.controlPath = "<Gamepad>/buttonSouth";

        // ── Gamepad virtual cursor reticle (for right-stick tool aiming) ───────────
        var cursorVisualGO = CreateCircle(canvasGO.transform, "GamepadCursorReticle",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(1f, 1f, 1f, 0.8f));
        var cursorVisualRT = cursorVisualGO.GetComponent<RectTransform>();
        cursorVisualRT.sizeDelta = new Vector2(24, 24);
        cursorVisualGO.GetComponent<Image>().raycastTarget = false;
        cursorVisualGO.SetActive(false);

        // ── Manager (parent of the canvas, so a single DontDestroyOnLoad covers both) ──
        var managerGO = new GameObject("MobileControlsManager");
        canvasGO.transform.SetParent(managerGO.transform, false);
        var manager = managerGO.AddComponent<MobileControlsManager>();
        var gamepadCursor = managerGO.AddComponent<GamepadVirtualCursor>();

        var so = new SerializedObject(manager);
        so.FindProperty("controlsRoot").objectReferenceValue = canvasGO;
        so.FindProperty("actionButton").objectReferenceValue = onScreenButton;
        so.ApplyModifiedProperties();

        var cursorSO = new SerializedObject(gamepadCursor);
        cursorSO.FindProperty("cursorVisual").objectReferenceValue = cursorVisualRT;
        cursorSO.ApplyModifiedProperties();

        // Start hidden — MobileControlsManager.Awake() decides if/when to show it.
        // Keeping this off by default means its GraphicRaycaster/OnScreenStick/OnScreenButton
        // never compete for input with menu Canvases even for a single frame.
        canvasGO.SetActive(false);

        Debug.Log("[MobileControlsUIBuilder] MobileControlsCanvas + MobileControlsManager + GamepadVirtualCursor created and wired.");

        Selection.activeGameObject = managerGO;
        EditorUtility.DisplayDialog("Done!",
            "MobileControlsCanvas created under 'MobileControlsManager'!\n\n" +
            "1. Drag the 'MobileControlsManager' GameObject into MainMenu.unity (it self-registers with DontDestroyOnLoad, survives every scene change).\n" +
            "2. Run 'Generate C# Class' on PlayerControls.inputactions if you haven't already after the Interact/AimCursor binding changes.\n" +
            "3. Test touch in a WebGL build or the browser's device toolbar (touch emulation), and test Xbox/PS5 controllers by plugging one in and using the right stick to move the on-screen reticle over tools.",
            "OK");
    }

    private static GameObject CreateCircle(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }
}

} // namespace SowurShield.Editor
