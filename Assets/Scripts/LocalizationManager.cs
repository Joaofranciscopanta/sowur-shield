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
                ApplyStoredLanguage();
        }

        private void ApplyStoredLanguage()
        {
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

        public string GetCurrentLanguageCode()
        {
            return LocalizationSettings.SelectedLocale != null
                ? LocalizationSettings.SelectedLocale.Identifier.Code
                : PlayerPrefs.GetString(PlayerPrefsKey, "en");
        }
    }
}
