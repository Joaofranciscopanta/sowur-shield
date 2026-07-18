using System.Collections.Generic;
using System.IO;

namespace SowurShield.Editor
{

/// <summary>Shared minimal RFC4180 CSV parser used by the Localization setup/import tools.</summary>
public static class LocalizationCsvUtility
{
    public static List<string[]> ParseCsv(string path)
    {
        string text = File.ReadAllText(path);
        var rows = new List<string[]>();
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        int i = 0;

        void EndField()
        {
            fields.Add(current.ToString());
            current.Clear();
        }

        void EndRow()
        {
            EndField();
            rows.Add(fields.ToArray());
            fields.Clear();
        }

        while (i < text.Length)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        current.Append('"');
                        i += 2;
                        continue;
                    }
                    inQuotes = false;
                    i++;
                    continue;
                }
                current.Append(c);
                i++;
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    i++;
                    break;
                case ',':
                    EndField();
                    i++;
                    break;
                case '\r':
                    i++;
                    break;
                case '\n':
                    EndRow();
                    i++;
                    break;
                default:
                    current.Append(c);
                    i++;
                    break;
            }
        }

        if (current.Length > 0 || fields.Count > 0)
            EndRow();

        return rows;
    }
}

} // namespace SowurShield.Editor
