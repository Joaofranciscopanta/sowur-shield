using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SowurShield.Dialogue;

namespace SowurShield.Editor
{
/// <summary>
/// Editor tool that instantiates and fully configures the Maren NPC in the
/// currently open scene. Run once via Tools → NPC → Spawn Maren (Seed Merchant).
/// </summary>
public static class MarenNPCSpawner
{
    private const string NPC_ID           = "maren";
    private const string NPC_DISPLAY_NAME = "Maren";
    private const string NPC_BIO =
        "Maren chegou ao vale depois de perder três colheitas seguidas numa seca " +
        "devastadora. Perdeu tudo que havia plantado no vale leste, mas encontrou " +
        "nesta terra algo diferente — uma fertilidade que parece quase abençoada. " +
        "Ela vende sementes, mas o que ela realmente vende é esperança.";

    private const float INTERACTION_RANGE    = 3f;
    private const float SPAWN_X              = 3f;
    private const float SPAWN_Y              = 3f;

    [MenuItem("Tools/NPC/Spawn Maren (Seed Merchant)")]
    public static void SpawnMaren()
    {
        // Guard: block if Maren already exists in scene
        var existing = Object.FindFirstObjectByType<NPCDialogueInteractable>();
        if (existing != null)
        {
            var allNpcs = Object.FindObjectsByType<NPCDialogueInteractable>(FindObjectsSortMode.None);
            foreach (var npc in allNpcs)
            {
                if (npc.GetNPCDisplayName() == NPC_DISPLAY_NAME)
                {
                    EditorUtility.DisplayDialog(
                        "Maren já existe",
                        "Um NPC chamado \"Maren\" já está na cena. Remova-o primeiro se quiser recriar.",
                        "OK");
                    Selection.activeGameObject = npc.gameObject;
                    return;
                }
            }
        }

        // Load dialogue trees
        var defaultDlg        = LoadDialogue("Dialogues/Maren/Maren_Default");
        var friendDlg         = LoadDialogue("Dialogues/Maren/Maren_Friend");
        var belovedDlg        = LoadDialogue("Dialogues/Maren/Maren_Beloved");
        var springDlg         = LoadDialogue("Dialogues/Maren/Maren_SeasonalSpring");
        var giftReactionDlg   = LoadDialogue("Dialogues/Maren/Maren_GiftReaction");

        if (defaultDlg == null)
        {
            EditorUtility.DisplayDialog(
                "Assets não encontrados",
                "Não foi possível carregar Maren_Default.asset em Resources/Dialogues/Maren/.\n" +
                "Certifique-se de que os assets de diálogo da Maren existem no projeto.",
                "OK");
            return;
        }

        // ----------------------------------------------------------------
        // Build the NPC hierarchy
        // ----------------------------------------------------------------

        // Root GameObject
        var root = new GameObject("Maren");
        Undo.RegisterCreatedObjectUndo(root, "Spawn Maren NPC");
        root.transform.position = new Vector3(SPAWN_X, SPAWN_Y, 0f);

        // Sprite Renderer (placeholder — designer replaces with real portrait)
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;

        // Collider for InteractionManager / raycast detection
        var col = root.AddComponent<CapsuleCollider2D>();
        col.isTrigger = true;
        col.size   = new Vector2(0.8f, 1.4f);
        col.offset = new Vector2(0f, 0.2f);

        // Optional interaction prompt child (a simple text label the NPC can show)
        var promptGO = new GameObject("InteractionPrompt");
        promptGO.transform.SetParent(root.transform, false);
        promptGO.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        promptGO.SetActive(false); // NPCDialogueInteractable controls visibility

        // ----------------------------------------------------------------
        // Configure NPCDialogueInteractable via SerializedObject so Unity
        // properly tracks the Undo and dirty-marks the scene.
        // ----------------------------------------------------------------

        var interactable = root.AddComponent<NPCDialogueInteractable>();

        var so = new SerializedObject(interactable);

        so.FindProperty("npcId").stringValue          = NPC_ID;
        so.FindProperty("npcDisplayName").stringValue = NPC_DISPLAY_NAME;
        so.FindProperty("npcBio").stringValue         = NPC_BIO;
        so.FindProperty("interactionRange").floatValue = INTERACTION_RANGE;

        so.FindProperty("allowRepeatedConversations").boolValue  = true;
        so.FindProperty("cooldownBetweenInteractions").floatValue = 1f;
        so.FindProperty("disableMovementDuringDialogue").boolValue = true;
        so.FindProperty("enableGifting").boolValue    = true;
        so.FindProperty("enableSeedShop").boolValue   = true;

        so.FindProperty("interactionPrompt").objectReferenceValue = promptGO;

        // Default dialogue
        so.FindProperty("defaultDialogue").objectReferenceValue = defaultDlg;

        // Available dialogues array
        var availProp = so.FindProperty("availableDialogues");
        var trees = new DialogueTree[]
        {
            defaultDlg, friendDlg, belovedDlg, springDlg, giftReactionDlg
        };

        int count = 0;
        foreach (var t in trees) if (t != null) count++;

        availProp.arraySize = count;
        int idx = 0;
        foreach (var t in trees)
        {
            if (t == null) continue;
            availProp.GetArrayElementAtIndex(idx).objectReferenceValue = t;
            idx++;
        }

        so.ApplyModifiedProperties();

        // ----------------------------------------------------------------
        // Select and focus
        // ----------------------------------------------------------------
        Selection.activeGameObject = root;
        SceneView.FrameLastActiveSceneView();

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[MarenNPCSpawner] Maren criada na posição {root.transform.position}. " +
                  "Atribua um sprite no SpriteRenderer e salve a cena.");

        EditorUtility.DisplayDialog(
            "Maren adicionada!",
            "NPC \"Maren\" criada com sucesso na cena.\n\n" +
            "Próximos passos:\n" +
            "1. Atribua um sprite no componente Sprite Renderer\n" +
            "2. Posicione ela no mapa (arraste no Scene view)\n" +
            "3. Salve a cena (Ctrl+S)\n\n" +
            "Os 5 diálogos já estão configurados. Enable Seed Shop: ✓",
            "OK");
    }

    private static DialogueTree LoadDialogue(string resourcePath)
    {
        var asset = Resources.Load<DialogueTree>(resourcePath);
        if (asset == null)
            Debug.LogWarning($"[MarenNPCSpawner] Não encontrou: Resources/{resourcePath}.asset");
        return asset;
    }
}
} // namespace SowurShield.Editor
