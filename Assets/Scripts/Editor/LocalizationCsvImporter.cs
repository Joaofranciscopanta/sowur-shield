using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace SowurShield.Editor
{

/// <summary>
/// Imports Assets/Localization/translations.csv into the project's String Table Collections.
/// The CSV has a "Table" column identifying which collection each row belongs to, plus
/// "Key", "en", "pt", "es" columns. String Table Collections (one per "Table" value) and
/// their Locales must already exist in the project (created once via the Localization
/// Tables window) before running this.
/// Menu: Tools > Sowur Shield > Import Localization CSV
/// </summary>
public static class LocalizationCsvImporter
{
    private const string CsvPath = "Assets/Localization/translations.csv";
    private const string CollectionsFolder = "Assets/Localization/StringTables";

    [MenuItem("Tools/Sowur Shield/Import Localization CSV")]
    public static void Import()
    {
        if (!File.Exists(CsvPath))
        {
            EditorUtility.DisplayDialog("Import Localization CSV",
                $"CSV not found at {CsvPath}.", "OK");
            return;
        }

        var rows = LocalizationCsvUtility.ParseCsv(CsvPath);
        if (rows.Count == 0)
        {
            EditorUtility.DisplayDialog("Import Localization CSV", "CSV has no data rows.", "OK");
            return;
        }

        string[] header = rows[0];
        var dataRows = rows.Skip(1).ToList();

        int tableCol = System.Array.IndexOf(header, "Table");
        int keyCol = System.Array.IndexOf(header, "Key");
        if (tableCol < 0 || keyCol < 0)
        {
            EditorUtility.DisplayDialog("Import Localization CSV",
                "CSV must have 'Table' and 'Key' columns.", "OK");
            return;
        }

        // Locale columns are every column except Table/Key.
        var localeColumns = new List<(int index, string code)>();
        for (int i = 0; i < header.Length; i++)
        {
            if (i == tableCol || i == keyCol)
                continue;
            localeColumns.Add((i, header[i]));
        }

        var byTable = dataRows.GroupBy(r => r[tableCol]);

        int tablesUpdated = 0;
        int entriesWritten = 0;

        foreach (var group in byTable)
        {
            string tableName = group.Key;
            var collection = FindCollection(tableName);
            if (collection == null)
            {
                Debug.LogWarning($"[LocalizationCsvImporter] No StringTableCollection named '{tableName}' found under {CollectionsFolder}. Skipping {group.Count()} rows. Create it first via Window > Asset Management > Localization Tables.");
                continue;
            }

            foreach (var row in group)
            {
                string key = row[keyCol];

                foreach (var (colIndex, localeCode) in localeColumns)
                {
                    StringTable table = collection.StringTables.FirstOrDefault(t =>
                        t.LocaleIdentifier.Code == localeCode);

                    if (table == null)
                    {
                        Debug.LogWarning($"[LocalizationCsvImporter] Collection '{tableName}' has no locale '{localeCode}'. Add the locale in Localization Settings first.");
                        continue;
                    }

                    string value = colIndex < row.Length ? row[colIndex] : string.Empty;
                    var entry = table.AddEntry(key, value);
                    entriesWritten++;
                }
            }

            EditorUtility.SetDirty(collection.SharedData);
            foreach (var t in collection.StringTables)
                EditorUtility.SetDirty(t);

            tablesUpdated++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[LocalizationCsvImporter] Updated {tablesUpdated} table collection(s), wrote {entriesWritten} entries.");
        EditorUtility.DisplayDialog("Import Localization CSV",
            $"Done.\n\nTables updated: {tablesUpdated}\nEntries written: {entriesWritten}\n\n" +
            "If a table was skipped, create the matching StringTableCollection first (Window > Asset Management > Localization Tables > New Table Collection), then re-run this.",
            "OK");
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

}

} // namespace SowurShield.Editor
