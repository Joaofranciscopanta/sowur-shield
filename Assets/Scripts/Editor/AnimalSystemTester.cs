#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Reflection;

/// <summary>
/// Editor window for testing the animal production system at runtime.
/// Open via Tools > Sowur Shield > Animal System Tester.
/// All buttons require Play Mode to function.
/// </summary>
public class AnimalSystemTester : EditorWindow
{
    // =========================================================================
    // Window Setup
    // =========================================================================

    [MenuItem("Tools/Sowur Shield/Animal System Tester")]
    public static void Open()
    {
        var window = GetWindow<AnimalSystemTester>("Animal System Tester");
        window.minSize = new Vector2(340f, 480f);
        window.Show();
    }

    // =========================================================================
    // State
    // =========================================================================

    private Animal selectedAnimal;
    private Vector2 scroll;

    // =========================================================================
    // GUI
    // =========================================================================

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawHeader();
        DrawPlayModeGuard();

        if (Application.isPlaying)
        {
            DrawAnimalSelector();
            EditorGUILayout.Space(4);

            if (selectedAnimal != null)
            {
                DrawAnimalStatus();
                EditorGUILayout.Space(6);
                DrawInteractionControls();
                EditorGUILayout.Space(6);
                DrawProductionControls();
                EditorGUILayout.Space(6);
                DrawSaveControls();
            }
            else
            {
                EditorGUILayout.HelpBox("Select an Animal in the scene hierarchy, or pick one from the list above.", MessageType.Info);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(6);
        var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField("🐾  Animal System Tester", style);
        EditorGUILayout.LabelField("Sowur Shield — Runtime Debug Tool", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(6);
        DrawHorizontalLine();
        EditorGUILayout.Space(4);
    }

    private void DrawPlayModeGuard()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use this tool.", MessageType.Warning);
        }
    }

    private void DrawAnimalSelector()
    {
        EditorGUILayout.LabelField("Animal", EditorStyles.boldLabel);

        // Direct object field
        var next = (Animal)EditorGUILayout.ObjectField("Selected Animal", selectedAnimal, typeof(Animal), true);
        if (next != selectedAnimal)
        {
            selectedAnimal = next;
            Repaint();
        }

        // Quick-pick from all animals in scene
        Animal[] allAnimals = FindObjectsByType<Animal>(FindObjectsSortMode.None);
        if (allAnimals.Length == 0)
        {
            EditorGUILayout.HelpBox("No Animal components found in the scene.", MessageType.Warning);
            return;
        }

        string[] names = new string[allAnimals.Length];
        int currentIdx = -1;
        for (int i = 0; i < allAnimals.Length; i++)
        {
            names[i] = allAnimals[i].AnimalData != null
                ? $"{allAnimals[i].AnimalData.animalName} ({allAnimals[i].gameObject.name})"
                : allAnimals[i].gameObject.name;

            if (allAnimals[i] == selectedAnimal) currentIdx = i;
        }

        int newIdx = EditorGUILayout.Popup("Quick Pick", currentIdx, names);
        if (newIdx >= 0 && newIdx < allAnimals.Length && allAnimals[newIdx] != selectedAnimal)
        {
            selectedAnimal = allAnimals[newIdx];
            Selection.activeGameObject = selectedAnimal.gameObject;
            Repaint();
        }
    }

    private void DrawAnimalStatus()
    {
        if (selectedAnimal == null || selectedAnimal.AnimalData == null) return;

        DrawHorizontalLine();
        EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Toggle("Petted Today", selectedAnimal.HasBeenPetToday);
            EditorGUILayout.IntField("Food Eaten Today", selectedAnimal.FoodEatenToday);
            EditorGUILayout.Toggle("Needs Feeding", selectedAnimal.NeedsFeeding);
            EditorGUILayout.FloatField("Food %", selectedAnimal.GetFoodPercentage() * 100f);
            EditorGUILayout.IntField("Current Day", selectedAnimal.CurrentDay);
            EditorGUILayout.IntField("Last Production Day", selectedAnimal.LastProductionDay);

            AnimalAI ai = selectedAnimal.GetComponent<AnimalAI>();
            if (ai != null)
                EditorGUILayout.TextField("AI State", ai.GetCurrentState());
        }
    }

    private void DrawInteractionControls()
    {
        DrawHorizontalLine();
        EditorGUILayout.LabelField("Interaction", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Force Pet"))
        {
            InvokePrivate(selectedAnimal, "PetAnimal");
            Debug.Log($"[AnimalTester] Force-petted {selectedAnimal.AnimalData?.animalName}");
        }

        if (GUILayout.Button("Reset Pet"))
        {
            SetPrivateField(selectedAnimal, "hasBeenPetToday", false);
            SetPrivateField(selectedAnimal, "lastPetTime", -999f);
            Debug.Log($"[AnimalTester] Reset pet state on {selectedAnimal.AnimalData?.animalName}");
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Mark Fed (full)"))
        {
            if (selectedAnimal.AnimalData != null)
            {
                int totalRequired = 0;
                foreach (var req in selectedAnimal.AnimalData.dailyFoodRequirements)
                    totalRequired += req.quantityPerDay;

                SetPrivateField(selectedAnimal, "foodEatenToday", totalRequired);
                SetPrivateField(selectedAnimal, "needsFeeding", false);
            }
        }

        if (GUILayout.Button("Reset Food"))
        {
            SetPrivateField(selectedAnimal, "foodEatenToday", 0);
            SetPrivateField(selectedAnimal, "needsFeeding", true);
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Trigger Eating Animation (AnimalAI)"))
        {
            AnimalAI ai = selectedAnimal.GetComponent<AnimalAI>();
            if (ai != null)
            {
                ai.TriggerEating();
                Debug.Log($"[AnimalTester] TriggerEating on {selectedAnimal.AnimalData?.animalName}");
            }
            else
            {
                Debug.LogWarning("[AnimalTester] No AnimalAI component found!");
            }
        }
    }

    private void DrawProductionControls()
    {
        DrawHorizontalLine();
        EditorGUILayout.LabelField("Production", EditorStyles.boldLabel);

        if (selectedAnimal.AnimalData != null && !selectedAnimal.AnimalData.canProduce)
        {
            EditorGUILayout.HelpBox($"canProduce = false on '{selectedAnimal.AnimalData.animalName}'.\nEnable it in the AnimalData asset to test production.", MessageType.Info);
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Force Check Production"))
        {
            InvokePrivate(selectedAnimal, "CheckProduction");
            Debug.Log($"[AnimalTester] CheckProduction() on {selectedAnimal.AnimalData?.animalName}");
        }

        if (GUILayout.Button("Reset Production Day"))
        {
            SetPrivateField(selectedAnimal, "lastProductionDay", -1);
            Debug.Log($"[AnimalTester] Reset lastProductionDay on {selectedAnimal.AnimalData?.animalName}");
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Simulate Day Change"))
        {
            SimulateDayChange(selectedAnimal);
        }
    }

    private void DrawSaveControls()
    {
        DrawHorizontalLine();
        EditorGUILayout.LabelField("Save / Load", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save Animal State"))
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
                Debug.Log("[AnimalTester] Triggered SaveManager.SaveGame()");
            }
            else
            {
                Debug.LogWarning("[AnimalTester] SaveManager.Instance is null.");
            }
        }

        if (GUILayout.Button("Load Animal State"))
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadGame();
                Debug.Log("[AnimalTester] Triggered SaveManager.LoadGame()");
            }
            else
            {
                Debug.LogWarning("[AnimalTester] SaveManager.Instance is null.");
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    // =========================================================================
    // Auto-refresh
    // =========================================================================

    private void OnInspectorUpdate()
    {
        if (Application.isPlaying)
            Repaint();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static void DrawHorizontalLine()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1f);
        EditorGUI.DrawRect(rect, new Color(0.4f, 0.4f, 0.4f, 1f));
        EditorGUILayout.Space(2);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        if (target == null) return;

        var method = target.GetType().GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (method != null)
            method.Invoke(target, null);
        else
            Debug.LogWarning($"[AnimalTester] Method '{methodName}' not found on {target.GetType().Name}");
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null) return;

        var field = target.GetType().GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (field != null)
            field.SetValue(target, value);
        else
            Debug.LogWarning($"[AnimalTester] Field '{fieldName}' not found on {target.GetType().Name}");
    }

    private static void SimulateDayChange(Animal animal)
    {
        if (animal == null) return;

        // Advance the internal currentDay counter then invoke OnDayChanged
        int currentDay = (int)(animal.GetType()
            .GetField("currentDay", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(animal) ?? 0);

        animal.GetType()
            .GetField("currentDay", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(animal, currentDay + 1);

        InvokePrivate(animal, "OnDayChanged");

        Debug.Log($"[AnimalTester] Simulated day change on {animal.AnimalData?.animalName}. New day: {currentDay + 1}");
    }
}
#endif
