using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

namespace SowurShield.Editor
{

/// <summary>
/// One-click setup for CombatScene.
/// Menu: Tools > Combat > Setup Combat Scene
/// </summary>
public class CombatSceneSetup : EditorWindow
{
    [MenuItem("Tools/Combat/Setup Combat Scene")]
    public static void SetupScene()
    {
        int fixes = 0;
        string report = "";

        // ── 1. Camera ─────────────────────────────────────────────────────────
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(camGO, "Create Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
            report += "• Created Main Camera\n";
            fixes++;
        }

        cam.orthographic     = true;
        cam.orthographicSize = 3.5f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.42f, 0.48f, 0.57f);
        report += "• Camera: orthographic size=3.5, pos=(0,0,-10)\n";

        // ── 2. Remove ALL existing BattleStatusCanvas objects then recreate ───
        // Removes duplicates (old "New Text" canvas + any extras)
        var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in allCanvases)
        {
            if (c.gameObject.name == "BattleStatusCanvas")
            {
                Undo.DestroyObjectImmediate(c.gameObject);
                report += "• Removed old BattleStatusCanvas\n";
                fixes++;
            }
        }

        // Create fresh BattleStatusCanvas
        var battleStatusGO = new GameObject("BattleStatusCanvas");
        Undo.RegisterCreatedObjectUndo(battleStatusGO, "Create BattleStatusCanvas");

        var bsCanvas = battleStatusGO.AddComponent<Canvas>();
        bsCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        bsCanvas.sortingOrder = 10;

        var bsScaler = battleStatusGO.AddComponent<CanvasScaler>();
        bsScaler.uiScaleMode           = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        bsScaler.referenceResolution   = new Vector2(1280, 720);
        bsScaler.matchWidthOrHeight    = 0.5f;

        battleStatusGO.AddComponent<GraphicRaycaster>();

        var bsUI = battleStatusGO.AddComponent<SowurShield.Combat.BattleStatusUI>();
        report += "• Created fresh BattleStatusCanvas with BattleStatusUI\n";
        fixes++;

        // ── 3. Top bar ────────────────────────────────────────────────────────
        var topBarGO = new GameObject("TopBar");
        Undo.RegisterCreatedObjectUndo(topBarGO, "Create TopBar");
        topBarGO.transform.SetParent(battleStatusGO.transform, false);
        topBarGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        var tbRT = topBarGO.GetComponent<RectTransform>();
        tbRT.anchorMin = new Vector2(0, 1);
        tbRT.anchorMax = new Vector2(1, 1);
        tbRT.offsetMin = new Vector2(0, -42);
        tbRT.offsetMax = Vector2.zero;
        var hlg = topBarGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(12, 12, 5, 5);
        hlg.spacing = 10;

        var turnText   = MakeTMP(topBarGO.transform, "TurnCounterText",  "Turn: 1/30",      14, TextAlignmentOptions.Left);
        var playerText = MakeTMP(topBarGO.transform, "PlayerTeamText",   "Your Team: 0/0",  14, TextAlignmentOptions.Center);
        var enemyText  = MakeTMP(topBarGO.transform, "EnemyTeamText",    "Enemies: 0/0",    14, TextAlignmentOptions.Right);

        // ── 4. Wire TMP refs into BattleStatusUI ──────────────────────────────
        var bsSO = new SerializedObject(bsUI);
        bsSO.FindProperty("turnCounterText").objectReferenceValue = turnText;
        bsSO.FindProperty("playerTeamText").objectReferenceValue  = playerText;
        bsSO.FindProperty("enemyTeamText").objectReferenceValue   = enemyText;
        bsSO.ApplyModifiedProperties();
        report += "• BattleStatusUI texts wired\n";

        // ── 5. Disable CombatTestSpawner (superseded by CombatTeamSpawner) ────
        var testSpawner = Object.FindFirstObjectByType<SowurShield.Combat.CombatTestSpawner>();
        if (testSpawner != null && testSpawner.enabled)
        {
            Undo.RecordObject(testSpawner, "Disable CombatTestSpawner");
            testSpawner.enabled = false;
            report += "• CombatTestSpawner: disabled (CombatTeamSpawner is used instead)\n";
            fixes++;
        }

        // ── 7. GridManager check ──────────────────────────────────────────────
        var gm = Object.FindFirstObjectByType<SowurShield.Combat.GridManager>();
        if (gm == null)
        {
            var gmGO = new GameObject("GridManager");
            Undo.RegisterCreatedObjectUndo(gmGO, "Create GridManager");
            gmGO.AddComponent<SowurShield.Combat.GridManager>();
            report += "• Created GridManager (default 9x5, cellSize=1)\n";
            fixes++;
        }
        else
        {
            report += "• GridManager: already present OK\n";
        }

        // ── 8. CombatTeamSpawner — ensure spawnPlayerTeam=true ───────────────
        var spawner = Object.FindFirstObjectByType<SowurShield.Combat.CombatTeamSpawner>();
        if (spawner != null)
        {
            var spSO = new SerializedObject(spawner);

            var spawnPlayerProp = spSO.FindProperty("spawnPlayerTeam");
            if (spawnPlayerProp != null)
            {
                spawnPlayerProp.boolValue = true;
                fixes++;
            }

            spSO.ApplyModifiedProperties();
            report += "• CombatTeamSpawner: spawnPlayerTeam=true (no prefab needed)\n";
        }
        else
        {
            // Create a fresh CombatTeamSpawner if missing
            var spawnerGO = new GameObject("CombatTeamSpawner");
            Undo.RegisterCreatedObjectUndo(spawnerGO, "Create CombatTeamSpawner");
            spawnerGO.AddComponent<SowurShield.Combat.CombatTeamSpawner>();
            report += "• Created CombatTeamSpawner\n";
            fixes++;
        }

        // ── 9. EnemySpawner — create if missing ──────────────────────────────
        var enemySpawner = Object.FindFirstObjectByType<SowurShield.Combat.EnemySpawner>();
        if (enemySpawner == null)
        {
            var esGO = new GameObject("EnemySpawner");
            Undo.RegisterCreatedObjectUndo(esGO, "Create EnemySpawner");
            esGO.AddComponent<SowurShield.Combat.EnemySpawner>();
            report += "• Created EnemySpawner (spawns at 0.6s from StageData or fallback)\n";
            fixes++;
        }
        else
        {
            report += "• EnemySpawner: already present OK\n";
        }

        // ── 10. TurnManager check ─────────────────────────────────────────────
        var tm = Object.FindFirstObjectByType<SowurShield.Combat.TurnManager>();
        if (tm == null)
        {
            var tmGO = new GameObject("TurnManager");
            Undo.RegisterCreatedObjectUndo(tmGO, "Create TurnManager");
            tmGO.AddComponent<SowurShield.Combat.TurnManager>();
            report += "• Created TurnManager\n";
            fixes++;
        }
        else
        {
            report += "• TurnManager: already present OK\n";
        }

        // ── 11. GridManager healthBarPrefab warning ───────────────────────────
        var gmCheck = Object.FindFirstObjectByType<SowurShield.Combat.GridManager>();
        if (gmCheck != null)
        {
            var gmSO = new SerializedObject(gmCheck);
            var hbProp = gmSO.FindProperty("healthBarPrefab");
            if (hbProp != null && hbProp.objectReferenceValue == null)
            {
                report += "• WARNING: GridManager.healthBarPrefab is not assigned — health bars won't appear!\n";
            }
            else
            {
                report += "• GridManager.healthBarPrefab: assigned OK\n";
            }
        }

        // ── 12. BattleResultsUI scene name check ──────────────────────────────
        var bru = Object.FindFirstObjectByType<SowurShield.Combat.BattleResultsUI>();
        if (bru != null)
        {
            var bruSO = new SerializedObject(bru);
            var farmProp   = bruSO.FindProperty("farmSceneName");
            var combatProp = bruSO.FindProperty("combatSceneName");
            bool changed = false;
            if (farmProp != null && farmProp.stringValue != "SampleScene")
            {
                farmProp.stringValue = "SampleScene";
                changed = true;
            }
            if (combatProp != null && combatProp.stringValue != "CombatScene")
            {
                combatProp.stringValue = "CombatScene";
                changed = true;
            }
            if (changed)
            {
                bruSO.ApplyModifiedProperties();
                report += "• BattleResultsUI: scene names corrected (SampleScene / CombatScene)\n";
                fixes++;
            }
            else
            {
                report += "• BattleResultsUI: scene names OK\n";
            }
        }

        // Save scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Combat Scene Setup Complete",
            report +
            "\nSave the scene (Ctrl+S) then test:\n" +
            "Farm scene → TeamAssembler → place galinha → Start Battle",
            "OK");

        Selection.activeGameObject = battleStatusGO;
        Debug.Log($"[CombatSceneSetup] Done. {fixes} items configured.");
    }

    private static TextMeshProUGUI MakeTMP(Transform parent, string goName, string text,
        float fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(goName);
        Undo.RegisterCreatedObjectUndo(go, $"Create {goName}");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.color     = Color.white;
        t.alignment = alignment;
        t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        return t;
    }
}

} // namespace SowurShield.Editor
