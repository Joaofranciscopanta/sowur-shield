using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace SowurShield.Editor
{
    /// <summary>
    /// Unity Editor tools for cleaning up scene structure and fixing common issues.
    /// Senior Designer Review: Automated fixes for scene architecture problems.
    /// </summary>
    public class SceneCleanupTools : EditorWindow
    {
        [MenuItem("Tools/Scene Cleanup/Fix Manager Positions")]
        public static void FixManagerPositions()
        {
            // Find all objects under the Managers parent
            GameObject managersParent = GameObject.Find("Managers");

            if (managersParent == null)
            {
                return;
            }

            int fixedCount = 0;
            Transform[] allChildren = managersParent.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in allChildren)
            {
                // Skip the parent itself
                if (child == managersParent.transform)
                    continue;

                // Only fix direct children of Managers
                if (child.parent != managersParent.transform)
                    continue;

                bool needsFix = false;

                // Check if position is not at origin
                if (child.localPosition != Vector3.zero)
                {
                    Undo.RecordObject(child, "Fix Manager Position");
                    child.localPosition = Vector3.zero;
                    needsFix = true;
                }

                // Check if rotation is not identity
                if (child.localRotation != Quaternion.identity)
                {
                    Undo.RecordObject(child, "Fix Manager Rotation");
                    child.localRotation = Quaternion.identity;
                    needsFix = true;
                }

                // Check if scale is not (1,1,1)
                if (child.localScale != Vector3.one)
                {
                    Undo.RecordObject(child, "Fix Manager Scale");
                    child.localScale = Vector3.one;
                    needsFix = true;
                }

                if (needsFix)
                {
                    fixedCount++;
                }
            }

            if (fixedCount > 0)
            {
                EditorUtility.SetDirty(managersParent);
            }
            else
            {
            }
        }

        [MenuItem("Tools/Scene Cleanup/Fix All Rotation Values")]
        public static void FixRotationValues()
        {
            // Find all GameObjects in the scene
            GameObject[] allObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            int fixedCount = 0;
            float snapAngle = 15f; // Snap to 15-degree increments

            foreach (GameObject root in allObjects)
            {
                Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);

                foreach (Transform t in allTransforms)
                {
                    Vector3 euler = t.localEulerAngles;
                    Vector3 snappedEuler = new Vector3(
                        SnapToIncrement(euler.x, snapAngle),
                        SnapToIncrement(euler.y, snapAngle),
                        SnapToIncrement(euler.z, snapAngle)
                    );

                    // Check if the rotation has weird floating point errors
                    if (Vector3.Distance(euler, snappedEuler) > 0.5f && Vector3.Distance(euler, snappedEuler) < 5f)
                    {
                        Undo.RecordObject(t, "Snap Rotation");
                        t.localEulerAngles = snappedEuler;
                        fixedCount++;
                    }
                }
            }

            if (fixedCount > 0)
            {
            }
            else
            {
            }
        }

        [MenuItem("Tools/Scene Cleanup/Report Scene Health")]
        public static void ReportSceneHealth()
        {

            // Count canvases
            Canvas[] canvases = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas c in canvases)
            {
            }

            // Check managers
            GameObject managersParent = GameObject.Find("Managers");
            if (managersParent != null)
            {
                int badManagers = 0;
                foreach (Transform child in managersParent.transform)
                {
                    if (child.localPosition != Vector3.zero ||
                        child.localRotation != Quaternion.identity ||
                        child.localScale != Vector3.one)
                    {
                        badManagers++;
                    }
                }
            }

            // Count total GameObjects
            GameObject[] allObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            int totalCount = 0;
            foreach (GameObject obj in allObjects)
            {
                totalCount += obj.GetComponentsInChildren<Transform>(true).Length;
            }

            // Check for editor-only objects in scene
            GameObject mapEditor = GameObject.Find("MapEditor");
            if (mapEditor != null)
            {
            }

        }

        private static float SnapToIncrement(float value, float increment)
        {
            return Mathf.Round(value / increment) * increment;
        }

        [MenuItem("Tools/Scene Cleanup/Open Cleanup Window")]
        public static void ShowWindow()
        {
            GetWindow<SceneCleanupTools>("Scene Cleanup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Scene Cleanup Tools", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Fix Manager Positions", GUILayout.Height(30)))
            {
                FixManagerPositions();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Fix Rotation Values", GUILayout.Height(30)))
            {
                FixRotationValues();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Report Scene Health", GUILayout.Height(30)))
            {
                ReportSceneHealth();
            }

            GUILayout.Space(20);
            GUILayout.Label("Quick Fixes:", EditorStyles.boldLabel);

            if (GUILayout.Button("Select All Managers"))
            {
                GameObject managers = GameObject.Find("Managers");
                if (managers != null)
                {
                    Selection.activeGameObject = managers;
                    EditorGUIUtility.PingObject(managers);
                }
            }

            if (GUILayout.Button("Select All Canvases"))
            {
                Selection.objects = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            }
        }
    }
}
