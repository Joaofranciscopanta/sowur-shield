using UnityEngine;
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
        [MenuItem("Tools/MapEditor/Open MapEditor Scene")]
        public static void OpenMapEditor()
        {
            // Load the MapEditor scene additively
            Scene mapEditorScene = EditorSceneManager.OpenScene(
                "Assets/Scenes/MapEditorScene.unity",
                OpenSceneMode.Additive
            );

        }

        [MenuItem("Tools/MapEditor/Close MapEditor Scene")]
        public static void CloseMapEditor()
        {
            Scene mapEditorScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("MapEditorScene");

            if (mapEditorScene.isLoaded)
            {
                EditorSceneManager.CloseScene(mapEditorScene, true);
            }
            else
            {
            }
        }

        [MenuItem("Tools/MapEditor/Toggle MapEditor")]
        public static void ToggleMapEditor()
        {
            Scene mapEditorScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("MapEditorScene");

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
