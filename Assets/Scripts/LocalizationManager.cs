using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace SowurShield.Core
{
    /// <summary>
    /// Runtime entry point for language selection: persists the chosen locale,
    /// applies it via Unity Localization, and exposes a first-boot flag for
    /// the language-select prompt shown before the main menu.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        public const string PlayerPrefsKey = "Language";

        public static LocalizationManager Instance { get; private set; }

        public static event System.Action<Locale> OnLanguageChanged;

        public bool IsFirstBoot => !PlayerPrefs.HasKey(PlayerPrefsKey);

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
        }

        private void Start()
        {
            if (!IsFirstBoot)
                StartCoroutine(ApplyStoredLanguageWhenReady());
        }

        // AvailableLocales / SelectedLocale internally call WaitForCompletion() on the
        // Addressables operation backing the Localization system. WaitForCompletion is
        // unsupported on WebGL (no threads to block) and raises a native exception there,
        // so wait for InitializationOperation to finish on its own before touching them.
        private System.Collections.IEnumerator ApplyStoredLanguageWhenReady()
        {
            yield return LocalizationSettings.InitializationOperation;

            string code = PlayerPrefs.GetString(PlayerPrefsKey, "en");
            SetLanguage(code, persist: false);
        }

        /// <summary>Sets the active locale by code ("en", "pt", "es") and persists the choice.</summary>
        public void SetLanguage(string localeCode)
        {
            SetLanguage(localeCode, persist: true);
        }

        private void SetLanguage(string localeCode, bool persist)
        {
            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                StartCoroutine(SetLanguageWhenReady(localeCode, persist));
                return;
            }

            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
            if (locale == null)
            {
                Debug.LogWarning($"[LocalizationManager] No locale found for code '{localeCode}'.");
                return;
            }

            LocalizationSettings.SelectedLocale = locale;

            if (persist)
            {
                PlayerPrefs.SetString(PlayerPrefsKey, localeCode);
                PlayerPrefs.Save();
            }

            OnLanguageChanged?.Invoke(locale);
        }

        private System.Collections.IEnumerator SetLanguageWhenReady(string localeCode, bool persist)
        {
            yield return LocalizationSettings.InitializationOperation;
            SetLanguage(localeCode, persist);
        }

        public string GetCurrentLanguageCode()
        {
            if (!LocalizationSettings.InitializationOperation.IsDone)
                return PlayerPrefs.GetString(PlayerPrefsKey, "en");

            return LocalizationSettings.SelectedLocale != null
                ? LocalizationSettings.SelectedLocale.Identifier.Code
                : PlayerPrefs.GetString(PlayerPrefsKey, "en");
        }
    }
}
