using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SowurShield.Editor
{

/// <summary>
/// Reads Assets/Localization/field_map.json (class/field/table/key tuples extracted from the
/// "// table "X", key "y.z"" comments in code) and force-assigns every matching LocalizedString
/// field, via SerializedObject/SerializedProperty (the same path the Inspector picker writes
/// through, so it persists correctly to scene/prefab files), on every MonoBehaviour instance
/// found in every loaded scene and every prefab in the project.
///
/// This replaces manually wiring ~220 Inspector fields by hand.
/// Menu: Tools > Sowur Shield > Auto-Wire Localized Fields
/// </summary>
public static class LocalizedFieldAutoWirer
{
    [Serializable]
    private class FieldMapEntry
    {
        public string @class;
        public string field;
        public string table;
        public string key;
        public string file;
    }

    [Serializable]
    private class Wrapper
    {
        public FieldMapEntry[] items;
    }

    private const string MapPath = "Assets/Localization/field_map.json";

    [MenuItem("Tools/Sowur Shield/Auto-Wire Localized Fields")]
    public static void RunAutoWire()
    {
        if (!File.Exists(MapPath))
        {
            EditorUtility.DisplayDialog("Auto-Wire Localized Fields", $"Map file not found at {MapPath}.", "OK");
            return;
        }

        string json = File.ReadAllText(MapPath);
        FieldMapEntry[] entries = JsonUtility.FromJson<Wrapper>("{\"items\":" + json + "}").items;

        var byClass = entries.GroupBy(e => e.@class).ToDictionary(g => g.Key, g => g.ToList());

        int objectsTouched = 0;
        int fieldsSet = 0;
        int prefabsTouched = 0;
        var missingTypes = new List<string>();
        var missingFields = new List<string>();
        var typeCache = new Dictionary<string, Type>();

        Type ResolveType(string className)
        {
            if (typeCache.TryGetValue(className, out Type cached))
                return cached;

            Type found = FindType(className);
            typeCache[className] = found;
            return found;
        }

        // ── Scene objects (every currently loaded scene) ────────────────────────
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            bool sceneModified = false;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (var className in byClass.Keys)
                {
                    Type type = ResolveType(className);
                    if (type == null)
                    {
                        if (!missingTypes.Contains(className))
                            missingTypes.Add(className);
                        continue;
                    }

                    foreach (var component in root.GetComponentsInChildren(type, true))
                    {
                        int set = ApplyFields(component, byClass[className], missingFields);
                        if (set > 0)
                        {
                            objectsTouched++;
                            fieldsSet += set;
                            sceneModified = true;
                        }
                    }
                }
            }

            if (sceneModified)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        // ── Prefabs in the project (covers self-spawning UI and prefab-based rows) ──
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabRoot == null)
                continue;

            bool prefabModified = false;

            foreach (var className in byClass.Keys)
            {
                Type type = ResolveType(className);
                if (type == null)
                    continue;

                foreach (var component in prefabRoot.GetComponentsInChildren(type, true))
                {
                    int set = ApplyFields(component, byClass[className], missingFields);
                    if (set > 0)
                    {
                        prefabModified = true;
                        fieldsSet += set;
                    }
                }
            }

            if (prefabModified)
            {
                EditorUtility.SetDirty(prefabRoot);
                prefabsTouched++;
            }
        }

        AssetDatabase.SaveAssets();

        string report =
            $"Scene objects updated: {objectsTouched}\n" +
            $"Prefabs updated: {prefabsTouched}\n" +
            $"Field assignments made: {fieldsSet}\n" +
            (missingTypes.Count > 0 ? $"\nClasses not found in any open scene/prefab (skipped): {string.Join(", ", missingTypes)}\n" : "") +
            (missingFields.Count > 0 ? $"\nFields not found on instances (skipped, first 10): {string.Join(", ", missingFields.Distinct().Take(10))}\n" : "");

        Debug.Log("[LocalizedFieldAutoWirer] " + report.Replace("\n", " | "));
        EditorUtility.DisplayDialog("Auto-Wire Localized Fields — Done", report, "OK");
    }

    /// <summary>
    /// Sets TableReference/TableEntryReference on every matching LocalizedString field of this
    /// component via SerializedObject, so the write persists to the scene/prefab file exactly
    /// like the Inspector picker would. Returns how many fields were set.
    /// </summary>
    private static int ApplyFields(Component component, List<FieldMapEntry> fields, List<string> missingFieldsLog)
    {
        if (component == null)
            return 0;

        var so = new SerializedObject(component);
        int count = 0;

        foreach (var entry in fields)
        {
            SerializedProperty fieldProp = so.FindProperty(entry.field);
            if (fieldProp == null)
            {
                missingFieldsLog.Add($"{entry.@class}.{entry.field}");
                continue;
            }

            SerializedProperty tableNameProp = fieldProp.FindPropertyRelative("m_TableReference.m_TableCollectionName");
            SerializedProperty keyProp = fieldProp.FindPropertyRelative("m_TableEntryReference.m_Key");

            if (tableNameProp == null || keyProp == null)
            {
                missingFieldsLog.Add($"{entry.@class}.{entry.field} (no nested table/key property)");
                continue;
            }

            tableNameProp.stringValue = entry.table;
            keyProp.stringValue = entry.key;
            count++;
        }

        if (count > 0)
            so.ApplyModifiedPropertiesWithoutUndo();

        return count;
    }

    private static Type FindType(string className)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }

            Type match = types.FirstOrDefault(t => t.Name == className && typeof(Component).IsAssignableFrom(t));
            if (match != null)
                return match;
        }
        return null;
    }
}

} // namespace SowurShield.Editor
