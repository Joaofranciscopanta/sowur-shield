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
        public static event System.Action OnTablesReady;

        public bool IsFirstBoot => !PlayerPrefs.HasKey(PlayerPrefsKey);

        // GetLocalizedString() lazily loads each StringTable on first use, separately from
        // LocalizationSettings.InitializationOperation (which only sets up locales/selection).
        // Without preloading, the very first read of any given table returns empty while its
        // own load is in flight. Preload every table up front so reads are never empty.
        public static bool AreTablesReady { get; private set; }

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
            StartCoroutine(PreloadAndApplyLanguage());
        }

        private System.Collections.IEnumerator PreloadAndApplyLanguage()
        {
            yield return LocalizationSettings.InitializationOperation;
            yield return LocalizationSettings.StringDatabase.PreloadOperation;

            AreTablesReady = true;
            OnTablesReady?.Invoke();

            if (!IsFirstBoot)
            {
                string code = PlayerPrefs.GetString(PlayerPrefsKey, "en");
                SetLanguage(code, persist: false);
            }
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

            // PreloadBehavior defaults to PreloadSelectedLocale, so switching locales leaves the
            // new locale's tables unloaded — block reads again until they're preloaded too,
            // otherwise the first read after a language switch comes back empty.
            AreTablesReady = false;
            LocalizationSettings.SelectedLocale = locale;
            StartCoroutine(PreloadAfterLocaleSwitch(locale, persist, localeCode));
        }

        private System.Collections.IEnumerator PreloadAfterLocaleSwitch(Locale locale, bool persist, string localeCode)
        {
            yield return LocalizationSettings.StringDatabase.PreloadOperation;

            AreTablesReady = true;

            if (persist)
            {
                PlayerPrefs.SetString(PlayerPrefsKey, localeCode);
                PlayerPrefs.Save();
            }

            OnLanguageChanged?.Invoke(locale);
            OnTablesReady?.Invoke();
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
