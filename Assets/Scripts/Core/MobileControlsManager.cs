using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;

namespace SowurShield.Core
{
    /// <summary>
    /// Owns the on-screen touch controls canvas (virtual joystick + action button).
    /// Shows/hides it based on MobileDetector and the active scene, and keeps the
    /// action button's bound control in sync with the current gameplay context.
    /// </summary>
    public class MobileControlsManager : MonoBehaviour
    {
        [SerializeField] private GameObject controlsRoot;
        [SerializeField] private OnScreenButton actionButton;
        [SerializeField] private string[] gameplaySceneNames = { "SampleScene", "CombatScene" };

        public static MobileControlsManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Decide visibility before the controls Canvas ever renders a frame —
            // otherwise the OnScreenStick/OnScreenButton GraphicRaycaster can briefly
            // intercept clicks meant for the menu underneath.
            RefreshVisibility();

            MobileDetector.Initialize();
            MobileDetector.OnTouchAvailabilityChanged += HandleTouchAvailabilityChanged;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            MobileDetector.OnTouchAvailabilityChanged -= HandleTouchAvailabilityChanged;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            MobileDetector.Shutdown();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshVisibility();
        }

        private bool hiddenForPanel;

        private void Update()
        {
            if (controlsRoot == null)
                return;

            bool panelOpen = UIManager.Instance != null && UIManager.Instance.IsAnyPanelOpen();

            if (panelOpen && controlsRoot.activeSelf)
            {
                controlsRoot.SetActive(false);
                hiddenForPanel = true;
            }
            else if (!panelOpen && hiddenForPanel)
            {
                hiddenForPanel = false;
                RefreshVisibility();
            }
        }

        private void HandleTouchAvailabilityChanged(bool isTouch)
        {
            RefreshVisibility();
        }

        /// <summary>Switches the action button to control a different action (e.g. Interact vs Attack).</summary>
        public void SetActionButtonTarget(string controlPath)
        {
            if (actionButton == null)
                return;

            actionButton.controlPath = controlPath;
        }

        private void RefreshVisibility()
        {
            if (controlsRoot == null)
                return;

            bool isGameplayScene = false;
            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            foreach (string name in gameplaySceneNames)
            {
                if (activeScene == name)
                {
                    isGameplayScene = true;
                    break;
                }
            }

            controlsRoot.SetActive(MobileDetector.IsTouchDevice && isGameplayScene);
        }
    }
}
