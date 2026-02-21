using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor helper for quickly setting up animals in the scene
/// </summary>
public class AnimalSetupHelper : EditorWindow
{
    private AnimalData animalData;
    private AnimalZone targetZone;
    private Sprite animalSprite;
    private Vector2 spawnPosition = Vector2.zero;

    [MenuItem("Tools/Animal System/Setup Helper")]
    public static void ShowWindow()
    {
        GetWindow<AnimalSetupHelper>("Animal Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Quick Animal Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Animal Data
        animalData = (AnimalData)EditorGUILayout.ObjectField("Animal Data", animalData, typeof(AnimalData), false);

        // Zone
        targetZone = (AnimalZone)EditorGUILayout.ObjectField("Target Zone", targetZone, typeof(AnimalZone), true);

        // Sprite
        animalSprite = (Sprite)EditorGUILayout.ObjectField("Animal Sprite", animalSprite, typeof(Sprite), false);

        // Position
        spawnPosition = EditorGUILayout.Vector2Field("Spawn Position", spawnPosition);

        EditorGUILayout.Space();

        // Create button
        GUI.enabled = animalData != null;
        if (GUILayout.Button("Create Animal", GUILayout.Height(30)))
        {
            CreateAnimal();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This will create a complete animal GameObject with all required components configured.", MessageType.Info);

        EditorGUILayout.Space();
        GUILayout.Label("Quick Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Animal Zone"))
        {
            CreateAnimalZone();
        }

        if (GUILayout.Button("Create Animal Data Asset"))
        {
            CreateAnimalDataAsset();
        }
    }

    private void CreateAnimal()
    {
        // Create GameObject
        GameObject animalObj = new GameObject(animalData.animalName);
        animalObj.transform.position = spawnPosition;

        // Add SpriteRenderer
        SpriteRenderer sr = animalObj.AddComponent<SpriteRenderer>();
        if (animalSprite != null)
        {
            sr.sprite = animalSprite;
        }
        else if (animalData.idleSprite != null)
        {
            sr.sprite = animalData.idleSprite;
        }
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 5;

        // Add TWO Colliders - one for interaction (trigger), one for physics (non-trigger)
        // Interaction collider (larger, trigger)
        CircleCollider2D interactionCol = animalObj.AddComponent<CircleCollider2D>();
        interactionCol.isTrigger = true;
        interactionCol.radius = 1f;

        // Physics collider (smaller, solid)
        CircleCollider2D physicsCol = animalObj.AddComponent<CircleCollider2D>();
        physicsCol.isTrigger = false;
        physicsCol.radius = 0.3f;

        // Add Rigidbody2D
        Rigidbody2D rb = animalObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Add Animal component
        Animal animal = animalObj.AddComponent<Animal>();
        SerializedObject serializedAnimal = new SerializedObject(animal);
        serializedAnimal.FindProperty("animalData").objectReferenceValue = animalData;
        if (targetZone != null)
        {
            serializedAnimal.FindProperty("assignedZone").objectReferenceValue = targetZone;
        }
        serializedAnimal.ApplyModifiedProperties();

        // Add AnimalAI component
        animalObj.AddComponent<AnimalAI>();

        // Add Animator if controller exists
        if (animalData.animatorController != null)
        {
            Animator animator = animalObj.AddComponent<Animator>();
            animator.runtimeAnimatorController = animalData.animatorController;
        }

        // Select the new object
        Selection.activeGameObject = animalObj;
        EditorGUIUtility.PingObject(animalObj);

    }

    private void CreateAnimalZone()
    {
        GameObject zoneObj = new GameObject("AnimalZone_New");
        zoneObj.transform.position = Vector3.zero;

        // Add BoxCollider2D
        BoxCollider2D col = zoneObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(10f, 10f);

        // Add AnimalZone component
        zoneObj.AddComponent<AnimalZone>();

        Selection.activeGameObject = zoneObj;
        EditorGUIUtility.PingObject(zoneObj);

    }

    private void CreateAnimalDataAsset()
    {
        // Create asset
        AnimalData asset = ScriptableObject.CreateInstance<AnimalData>();

        // Save asset
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Animal Data",
            "NewAnimalData",
            "asset",
            "Create a new Animal Data asset"
        );

        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

        }
    }
}

/// <summary>
/// Custom inspector for Animal component to make setup easier
/// </summary>
[CustomEditor(typeof(Animal))]
public class AnimalEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Animal animal = (Animal)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);

        if (Application.isPlaying && animal.AnimalData != null)
        {
            EditorGUILayout.LabelField("Has Been Pet Today:", animal.HasBeenPetToday.ToString());
            EditorGUILayout.LabelField("Food Eaten Today:", animal.FoodEatenToday.ToString());
            EditorGUILayout.LabelField("Needs Feeding:", animal.NeedsFeeding.ToString());
            EditorGUILayout.LabelField("Food Percentage:", $"{animal.GetFoodPercentage() * 100f:F0}%");
        }
        else
        {
            EditorGUILayout.HelpBox("Runtime info available during play mode", MessageType.Info);
        }

        EditorGUILayout.Space();

        // Quick setup checks
        bool hasRigidbody = animal.GetComponent<Rigidbody2D>() != null;
        bool hasCollider = animal.GetComponent<Collider2D>() != null;
        bool hasAI = animal.GetComponent<AnimalAI>() != null;

        if (!hasRigidbody || !hasCollider || !hasAI)
        {
            EditorGUILayout.HelpBox("Missing required components!", MessageType.Warning);

            if (!hasRigidbody && GUILayout.Button("Add Rigidbody2D"))
            {
                Rigidbody2D rb = animal.gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            if (!hasCollider && GUILayout.Button("Add Colliders (Interaction + Physics)"))
            {
                // Interaction collider
                CircleCollider2D interactionCol = animal.gameObject.AddComponent<CircleCollider2D>();
                interactionCol.isTrigger = true;
                interactionCol.radius = 1f;

                // Physics collider
                CircleCollider2D physicsCol = animal.gameObject.AddComponent<CircleCollider2D>();
                physicsCol.isTrigger = false;
                physicsCol.radius = 0.3f;
            }

            if (!hasAI && GUILayout.Button("Add AnimalAI"))
            {
                animal.gameObject.AddComponent<AnimalAI>();
            }
        }
    }
}
