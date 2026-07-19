using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace SowurShield.Editor
{

/// <summary>
/// One-click setup for the entire Localization system: creates the en/pt/es Locales,
/// creates (and activates) the project's LocalizationSettings asset, creates every
/// String Table Collection referenced by the CSV, and imports all translations.
/// Safe to re-run — skips anything that already exists and only adds missing entries.
/// Menu: Tools > Sowur Shield > Setup Localization (Full)
/// </summary>
public static class LocalizationFullSetup
{
    private const string CsvPath = "Assets/Localization/translations.csv";
    private const string LocalesFolder = "Assets/Localization/Locales";
    private const string TablesFolder = "Assets/Localization/StringTables";
    private const string SettingsAssetPath = "Assets/Localization/Default Localization Settings.asset";

    private static readonly string[] LocaleCodes = { "en", "pt", "es" };

    [MenuItem("Tools/Sowur Shield/Setup Localization (Full)")]
    public static void RunFullSetup()
    {
        EnsureFolder("Assets/Localization");
        EnsureFolder(LocalesFolder);
        EnsureFolder(TablesFolder);

        List<Locale> locales = EnsureLocales();
        EnsureLocalizationSettings(locales);

        if (!File.Exists(CsvPath))
        {
            EditorUtility.DisplayDialog("Setup Localization",
                $"Locales and settings are ready, but the CSV was not found at {CsvPath}.\n" +
                "Re-run after restoring it to create tables and import translations.", "OK");
            return;
        }

        var rows = LocalizationCsvUtility.ParseCsv(CsvPath);
        if (rows.Count == 0)
        {
            EditorUtility.DisplayDialog("Setup Localization", "CSV has no data rows.", "OK");
            return;
        }

        string[] header = rows[0];
        var dataRows = rows.Skip(1).ToList();

        int tableCol = System.Array.IndexOf(header, "Table");
        int keyCol = System.Array.IndexOf(header, "Key");
        if (tableCol < 0 || keyCol < 0)
        {
            EditorUtility.DisplayDialog("Setup Localization", "CSV must have 'Table' and 'Key' columns.", "OK");
            return;
        }

        var localeColumns = new List<(int index, string code)>();
        for (int i = 0; i < header.Length; i++)
        {
            if (i == tableCol || i == keyCol)
                continue;
            localeColumns.Add((i, header[i]));
        }

        var byTable = dataRows.GroupBy(r => r[tableCol]);
        int tablesCreated = 0;
        int tablesUpdated = 0;
        int entriesWritten = 0;

        foreach (var group in byTable)
        {
            string tableName = group.Key;
            StringTableCollection collection = FindCollection(tableName);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(tableName, TablesFolder, locales);
                tablesCreated++;
            }

            foreach (var row in group)
            {
                string key = row[keyCol];

                foreach (var (colIndex, localeCode) in localeColumns)
                {
                    StringTable table = collection.StringTables.FirstOrDefault(t => t.LocaleIdentifier.Code == localeCode);
                    if (table == null)
                    {
                        Debug.LogWarning($"[LocalizationFullSetup] Collection '{tableName}' has no locale '{localeCode}'. Skipping.");
                        continue;
                    }

                    string value = colIndex < row.Length ? row[colIndex] : string.Empty;
                    table.AddEntry(key, value);
                    entriesWritten++;
                }
            }

            EditorUtility.SetDirty(collection.SharedData);
            foreach (var t in collection.StringTables)
                EditorUtility.SetDirty(t);

            tablesUpdated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[LocalizationFullSetup] Done. Locales ready: {locales.Count}. Tables created: {tablesCreated}. Tables updated: {tablesUpdated}. Entries written: {entriesWritten}.");
        EditorUtility.DisplayDialog("Setup Localization — Done!",
            $"Locales ready: {locales.Count} (en, pt, es)\n" +
            $"Table collections created: {tablesCreated}\n" +
            $"Table collections updated: {tablesUpdated}\n" +
            $"Entries written: {entriesWritten}\n\n" +
            "Everything is set up. You can now play the game — language selection and all migrated UI text should work.\n\n" +
            "Remaining manual step (if you want static button labels in prefabs/scenes translated too): see step 6+ in MOBILE_LOCALIZATION_SETUP.md.",
            "OK");
    }

    private static List<Locale> EnsureLocales()
    {
        var result = new List<Locale>();
        var existing = LocalizationEditorSettings.GetLocales();

        foreach (string code in LocaleCodes)
        {
            Locale locale = existing.FirstOrDefault(l => l.Identifier.Code == code);
            if (locale == null)
            {
                locale = Locale.CreateLocale(code);
                locale.name = locale.Identifier.CultureInfo != null ? locale.Identifier.CultureInfo.EnglishName : code;

                string path = $"{LocalesFolder}/Locale-{code}.asset";
                AssetDatabase.CreateAsset(locale, path);
                LocalizationEditorSettings.AddLocale(locale);
            }

            result.Add(locale);
        }

        AssetDatabase.SaveAssets();
        return result;
    }

    private static void EnsureLocalizationSettings(List<Locale> locales)
    {
        LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;

        if (settings == null)
        {
            settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsAssetPath);
        }

        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            settings.name = "Default Localization Settings";
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
        }

        LocalizationEditorSettings.ActiveLocalizationSettings = settings;

        Locale englishLocale = locales.FirstOrDefault(l => l.Identifier.Code == "en");
        if (englishLocale != null)
            LocalizationSettings.ProjectLocale = englishLocale;

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    private static StringTableCollection FindCollection(string tableName)
    {
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(StringTableCollection)}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var collection = AssetDatabase.LoadAssetAtPath<StringTableCollection>(path);
            if (collection != null && collection.name == tableName)
                return collection;
        }
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent))
            parent = "Assets";

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }
}

} // namespace SowurShield.Editor
