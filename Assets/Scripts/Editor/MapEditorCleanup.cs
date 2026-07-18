using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

namespace SowurShield.Editor
{
    /// <summary>
    /// Handles cleanup of MapEditor code to prevent it from being included in production builds.
    /// Fixes the "MapEditor in Production Scene" issue from senior review.
    /// </summary>
    public class MapEditorCleanup : EditorWindow
    {
        [MenuItem("Tools/MapEditor/Analyze MapEditor Issue")]
        public static void AnalyzeMapEditor()
        {

            GameObject mapEditor = GameObject.Find("MapEditor");

            if (mapEditor == null)
            {
                return;
            }


            // List children
            foreach (Transform child in mapEditor.transform)
            {
            }





        }

        [MenuItem("Tools/MapEditor/Disable MapEditor in Scene")]
        public static void DisableMapEditor()
        {
            GameObject mapEditor = GameObject.Find("MapEditor");

            if (mapEditor == null)
            {
                return;
            }

            Undo.RecordObject(mapEditor, "Disable MapEditor");
            mapEditor.SetActive(false);

        }

        [MenuItem("Tools/MapEditor/Wrap Scripts in #ifdef")]
        public static void WrapScriptsInIfdef()
        {
            string[] mapEditorScripts = new string[]
            {
                "Assets/Scripts/MapEditor/RuntimeMapEditor.cs",
                "Assets/Scripts/MapEditor/BrushTool.cs",
                "Assets/Scripts/MapEditor/NPCPlacer.cs",
                "Assets/Scripts/MapEditor/ExtendedTilemapSystem.cs",
                // Add more as needed
            };

            int wrappedCount = 0;

            foreach (string scriptPath in mapEditorScripts)
            {
                if (!File.Exists(scriptPath))
                {
                    continue;
                }

                string content = File.ReadAllText(scriptPath);

                // Check if already wrapped
                if (content.Contains("#if UNITY_EDITOR") && content.Contains("#endif"))
                {
                    continue;
                }

                // Wrap the entire file
                string wrappedContent = "#if UNITY_EDITOR\n" + content + "\n#endif\n";

                File.WriteAllText(scriptPath, wrappedContent);
                wrappedCount++;

            }

            if (wrappedCount > 0)
            {
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Tools/MapEditor/Create MapEditor Scene (RECOMMENDED)")]
        public static void CreateMapEditorScene()
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Create MapEditor Scene",
                "This will:\n" +
                "1. Create a new scene: MapEditorScene.unity\n" +
                "2. Move the MapEditor object there\n" +
                "3. Set it up for additive loading\n\n" +
                "This is the CLEANEST solution.\n\n" +
                "Continue?",
                "Yes, Do It",
                "Cancel"
            );

            if (!proceed) return;

            // Find MapEditor
            GameObject mapEditor = GameObject.Find("MapEditor");
            if (mapEditor == null)
            {
                EditorUtility.DisplayDialog("Error", "MapEditor not found in current scene!", "OK");
                return;
            }

            // Create new scene
            var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Additive
            );

            // Move MapEditor to new scene
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(mapEditor, newScene);

            // Save the new scene
            string scenePath = "Assets/Scenes/MapEditorScene.unity";
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, scenePath);


            // Create a helper script to load it
            CreateMapEditorLoader();

            EditorUtility.DisplayDialog(
                "Success!",
                "MapEditor scene created!\n\n" +
                "NEXT STEPS:\n" +
                "1. The MapEditor is now in: MapEditorScene.unity\n" +
                "2. Use MapEditorLoader.cs to load it when needed\n" +
                "3. NEVER include MapEditorScene in build settings\n\n" +
                "Check the console for details.",
                "OK"
            );
        }

        private static void CreateMapEditorLoader()
        {
            string loaderCode = @"using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Loads the MapEditor scene additively for level design work.
    /// EDITOR ONLY - won't compile in builds.
    /// </summary>
    public class MapEditorLoader : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem(""Tools/MapEditor/Open MapEditor Scene"")]
        public static void OpenMapEditor()
        {
            // Load the MapEditor scene additively
            Scene mapEditorScene = EditorSceneManager.OpenScene(
                ""Assets/Scenes/MapEditorScene.unity"",
                OpenSceneMode.Additive
            );

        }

        [MenuItem(""Tools/MapEditor/Close MapEditor Scene"")]
        public static void CloseMapEditor()
        {
            Scene mapEditorScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(""MapEditorScene"");

            if (mapEditorScene.isLoaded)
            {
                EditorSceneManager.CloseScene(mapEditorScene, true);
            }
            else
            {
            }
        }

        [MenuItem(""Tools/MapEditor/Toggle MapEditor"")]
        public static void ToggleMapEditor()
        {
            Scene mapEditorScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(""MapEditorScene"");

            if (mapEditorScene.isLoaded)
            {
                CloseMapEditor();
            }
            else
            {
                OpenMapEditor();
            }
        }
#endif
    }
}
";

            string loaderPath = "Assets/Scripts/MapEditor/MapEditorLoader.cs";

            // Ensure directory exists
            string directory = Path.GetDirectoryName(loaderPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(loaderPath, loaderCode);
            AssetDatabase.Refresh();

        }

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
