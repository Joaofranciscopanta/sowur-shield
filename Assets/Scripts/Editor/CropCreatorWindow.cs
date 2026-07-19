using UnityEngine;
using UnityEditor;
using SowurShield.Core;

namespace SowurShield.Editor
{
    /// <summary>
    /// Editor window for creating CropData ScriptableObject assets.
    /// Provides one-click creation of pre-configured crop assets for Tomato (Summer)
    /// and Winter Wheat (Winter). Sprite references are left null and must be assigned
    /// in the Inspector after creation.
    ///
    /// Access via: Tools > Sowur Shield > Create Crop Assets
    /// </summary>
    public class CropCreatorWindow : EditorWindow
    {
        private const string CROPS_FOLDER = "Assets/Resources/Crops";

        [MenuItem("Tools/Sowur Shield/Create Crop Assets")]
        public static void ShowWindow()
        {
            CropCreatorWindow window = GetWindow<CropCreatorWindow>("Crop Creator");
            window.minSize = new Vector2(320f, 160f);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(10f);
            GUILayout.Label("Crop Asset Creator", EditorStyles.boldLabel);
            GUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "Creates CropData assets in Assets/Resources/Crops/.\n" +
                "Sprite references are left unassigned — assign them in the Inspector.",
                MessageType.Info);

            GUILayout.Space(12f);

            if (GUILayout.Button("Create Tomato Crop (Summer)", GUILayout.Height(36f)))
            {
                CreateTomatoCrop();
            }

            GUILayout.Space(6f);

            if (GUILayout.Button("Create Winter Wheat Crop (Winter)", GUILayout.Height(36f)))
            {
                CreateWinterWheatCrop();
            }

            GUILayout.Space(10f);
        }

        // ──────────────────────────────────────────────────────────
        // Tomato
        // ──────────────────────────────────────────────────────────

        private static void CreateTomatoCrop()
        {
            CropData crop = ScriptableObject.CreateInstance<CropData>();

            crop.cropName       = "Tomato";
            crop.description    = "A juicy summer tomato. Grows in warm weather and produces multiple harvests.";

            // Season restrictions — Summer only
            crop.growsInAllSeasons = false;
            crop.validSeasons      = new Season[] { Season.Summer };
            crop.canGrowInWinter   = false;

            // Growth: 3 stages × 2 days each = 6 days total
            // growthStages array length determines TotalStages; daysPerStage is the per-stage duration.
            crop.daysPerStage  = 2;
            crop.growthStages  = new Sprite[3]; // 3 null sprite slots; assign in Inspector

            // Visual — left null for Inspector assignment
            crop.deadCropSprite  = null;
            crop.seedlingSprite  = null;

            // Water requirements
            crop.requiresWater      = true;
            crop.maxDaysWithoutWater = 3;

            // Harvest
            crop.minYield = 1;
            crop.maxYield = 3;

            // Regrowth
            crop.regrowsAfterHarvest = true;
            crop.regrowthDays        = 2;
            crop.maxRegrowths        = 2;

            // Special properties
            crop.needsTrellis    = false;
            crop.spreadsToCells  = false;

            // Value
            crop.baseValue          = 60;
            crop.qualityMultiplier  = 1.0f;

            // Item references — assign in Inspector
            crop.seedItem    = null;
            crop.harvestItem = null;

            SaveAsset(crop, "TomatoCropData");
        }

        // ──────────────────────────────────────────────────────────
        // Winter Wheat
        // ──────────────────────────────────────────────────────────

        private static void CreateWinterWheatCrop()
        {
            CropData crop = ScriptableObject.CreateInstance<CropData>();

            crop.cropName    = "Winter Wheat";
            crop.description = "Hardy wheat that thrives in cold winters. Yields a generous bounty but does not regrow.";

            // Season restrictions — Winter only
            crop.growsInAllSeasons = false;
            crop.validSeasons      = new Season[] { Season.Winter };
            crop.canGrowInWinter   = true;

            // Growth: 2 stages × 3 days each = 6 days total
            crop.daysPerStage = 3;
            crop.growthStages = new Sprite[2]; // 2 null sprite slots; assign in Inspector

            // Visual — left null for Inspector assignment
            crop.deadCropSprite = null;
            crop.seedlingSprite = null;

            // Water requirements
            crop.requiresWater       = true;
            crop.maxDaysWithoutWater = 3;

            // Harvest
            crop.minYield = 2;
            crop.maxYield = 5;

            // No regrowth
            crop.regrowsAfterHarvest = false;
            crop.regrowthDays        = 0;
            crop.maxRegrowths        = 0;

            // Special properties
            crop.needsTrellis   = false;
            crop.spreadsToCells = false;

            // Value
            crop.baseValue         = 40;
            crop.qualityMultiplier = 1.0f;

            // Item references — assign in Inspector
            crop.seedItem    = null;
            crop.harvestItem = null;

            SaveAsset(crop, "WinterWheatCropData");
        }

        // ──────────────────────────────────────────────────────────
        // Shared save helper
        // ──────────────────────────────────────────────────────────

        private static void SaveAsset(CropData crop, string fileName)
        {
            EnsureFolderExists(CROPS_FOLDER);

            string assetPath = $"{CROPS_FOLDER}/{fileName}.asset";

            // Overwrite check — ask the user rather than silently clobbering
            if (AssetDatabase.LoadAssetAtPath<CropData>(assetPath) != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Asset Already Exists",
                    $"'{fileName}.asset' already exists at {CROPS_FOLDER}.\n\nOverwrite it?",
                    "Overwrite",
                    "Cancel");

                if (!overwrite)
                {
                    Object.DestroyImmediate(crop);
                    return;
                }

                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.CreateAsset(crop, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Ping the new asset in the Project window
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = crop;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            // Build missing folders one level at a time
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
