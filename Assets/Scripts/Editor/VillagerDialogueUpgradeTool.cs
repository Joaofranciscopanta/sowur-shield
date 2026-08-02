using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SowurShield.Dialogue;

namespace SowurShield.Editor
{
/// <summary>
/// Regenerates the villagers' default dialogue trees so existing NPCs pick up the expanded
/// hub-and-spoke shape from <see cref="VillagerDialogueFactory"/>.
///
/// Why this exists separately from VillagerPopulationTool: that tool only builds a dialogue
/// tree while *creating* a villager, and skips anyone already in the scene. Every villager
/// already exists, so without this the expanded trees would only ever reach a fresh project.
///
/// Deletes the old asset before rebuilding, because CreateOrLoad is deliberately idempotent —
/// it returns the existing two-node stub untouched if the file is still there.
/// </summary>
public static class VillagerDialogueUpgradeTool
{
    private const string DialogueFolder = "Assets/Resources/Dialogues/Villagers";

    /// <summary>
    /// Villagers whose trees this tool owns. Maren is deliberately excluded: her five
    /// hand-authored trees are real content, not placeholders, and regenerating them would
    /// destroy writing that the factory cannot reproduce.
    /// </summary>
    private static readonly (string id, string displayName)[] Villagers =
    {
        ("tomas",   "Tomás"),
        ("isabela", "Isabela"),
        ("joana",   "Joana"),
        ("elias",   "Elias"),
        ("clara",   "Clara"),
        ("rui",     "Rui"),
        ("nara",    "Nara"),
        ("bento",   "Bento"),
    };

    /// <summary>
    /// No modal dialog on purpose: this menu item is also driven through automation, and
    /// EditorUtility.DisplayDialog blocks the editor until a human clicks it. The Console
    /// summary is the report.
    /// </summary>
    [MenuItem("Tools/NPC/Upgrade Villager Dialogue Trees")]
    public static void UpgradeTrees()
    {
        int rebuilt = 0;

        foreach (var (id, displayName) in Villagers)
        {
            string assetPath = $"{DialogueFolder}/{id}_Default.asset";

            if (AssetDatabase.LoadAssetAtPath<DialogueTree>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            var tree = VillagerDialogueFactory.CreateOrLoad(id, displayName);
            if (tree == null)
            {
                Debug.LogError($"[VillagerDialogueUpgradeTool] Failed to rebuild tree for '{id}'.");
                continue;
            }

            rebuilt++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // The scene's NPCs reference these trees by GUID. Deleting and recreating the asset
        // mints a new GUID, so every reference must be re-pointed or the villager falls silent.
        int relinked = RelinkSceneReferences();

        Debug.Log($"[VillagerDialogueUpgradeTool] Rebuilt {rebuilt} dialogue trees and " +
                  $"relinked {relinked} NPC references. Save the scene (Ctrl+S).");
    }

    /// <summary>
    /// Re-points each villager's defaultDialogue/availableDialogues at the freshly created
    /// asset. Without this the scene keeps a reference to the deleted GUID, which deserializes
    /// as null — the NPC would open an empty dialogue box instead of talking.
    /// </summary>
    private static int RelinkSceneReferences()
    {
        var npcs = Object.FindObjectsByType<NPCDialogueInteractable>(FindObjectsSortMode.None);
        int relinked = 0;

        foreach (var npc in npcs)
        {
            string npcId = npc.GetNPCId();
            if (string.IsNullOrEmpty(npcId)) continue;

            bool owned = false;
            foreach (var (id, _) in Villagers)
            {
                if (id == npcId) { owned = true; break; }
            }
            if (!owned) continue;

            var tree = AssetDatabase.LoadAssetAtPath<DialogueTree>($"{DialogueFolder}/{npcId}_Default.asset");
            if (tree == null) continue;

            var so = new SerializedObject(npc);
            so.FindProperty("defaultDialogue").objectReferenceValue = tree;

            var avail = so.FindProperty("availableDialogues");
            avail.arraySize = 1;
            avail.GetArrayElementAtIndex(0).objectReferenceValue = tree;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(npc);
            relinked++;
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return relinked;
    }
}
} // namespace SowurShield.Editor
