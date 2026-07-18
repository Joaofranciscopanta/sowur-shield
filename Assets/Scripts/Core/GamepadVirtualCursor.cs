using UnityEngine;
using UnityEngine.InputSystem;

namespace SowurShield.Core
{
    /// <summary>
    /// Drives an on-screen virtual cursor from the gamepad right stick, so farm tools
    /// (which read mouse position via CursorController) can be used with Xbox/PS5 controllers.
    /// Falls back to the real mouse whenever the player touches it, so mouse+gamepad can be mixed freely.
    /// </summary>
    public class GamepadVirtualCursor : MonoBehaviour
    {
        [SerializeField] private float cursorSpeed = 1200f; // pixels per second at full stick deflection
        [SerializeField] private RectTransform cursorVisual;

        public static GamepadVirtualCursor Instance { get; private set; }

        private Vector2 virtualPosition;
        private bool isActive;

        /// <summary>Non-null while a gamepad is actively driving the cursor; CursorController should prefer this over Mouse.current.</summary>
        public static Vector2? OverridePosition => Instance != null && Instance.isActive ? Instance.virtualPosition : (Vector2?)null;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            virtualPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                isActive = false;
                return;
            }

            Vector2 stick = gamepad.rightStick.ReadValue();

            // Any real mouse movement hands control back to the mouse immediately.
            if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f)
                isActive = false;

            if (stick.sqrMagnitude < 0.01f)
            {
                if (!isActive)
                    return;
            }
            else
            {
                isActive = true;
            }

            virtualPosition += stick * cursorSpeed * Time.deltaTime;
            virtualPosition.x = Mathf.Clamp(virtualPosition.x, 0, Screen.width);
            virtualPosition.y = Mathf.Clamp(virtualPosition.y, 0, Screen.height);

            if (cursorVisual != null)
            {
                cursorVisual.gameObject.SetActive(isActive);
                cursorVisual.position = virtualPosition;
            }
        }

        /// <summary>True if the gamepad's south button (A/Cross) was pressed this frame — used as a virtual left-click.</summary>
        public static bool WasClickPressedThisFrame()
        {
            Gamepad gamepad = Gamepad.current;
            return gamepad != null && OverridePosition.HasValue && gamepad.buttonSouth.wasPressedThisFrame;
        }
    }
}
