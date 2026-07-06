using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SowurShield.Core
{
    /// <summary>
    /// Detects whether the game is running on a touch/mobile device.
    /// In WebGL builds this queries the browser via JS interop (user agent + touch points),
    /// since Application.isMobilePlatform is always false for WebGL.
    /// </summary>
    public static class MobileDetector
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int IsMobileBrowser();
#endif

        private static bool? cachedResult;

        /// <summary>Editor-only override to force touch controls on/off without a real device.</summary>
        public static bool ForceTouchInEditor = false;

        public static event System.Action<bool> OnTouchAvailabilityChanged;

        public static bool IsTouchDevice
        {
            get
            {
                if (cachedResult.HasValue)
                    return cachedResult.Value;

                cachedResult = ComputeIsTouchDevice();
                return cachedResult.Value;
            }
        }

        private static bool ComputeIsTouchDevice()
        {
#if UNITY_EDITOR
            return ForceTouchInEditor;
#elif UNITY_WEBGL
            return IsMobileBrowser() == 1;
#else
            return Application.isMobilePlatform;
#endif
        }

        /// <summary>Call once at startup to listen for a touchscreen appearing after the initial check.</summary>
        public static void Initialize()
        {
            InputSystem.onDeviceChange += HandleDeviceChange;
        }

        public static void Shutdown()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;
        }

        private static void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is not Touchscreen)
                return;

            if (change != InputDeviceChange.Added)
                return;

            if (cachedResult == true)
                return;

            cachedResult = true;
            OnTouchAvailabilityChanged?.Invoke(true);
        }
    }
}
