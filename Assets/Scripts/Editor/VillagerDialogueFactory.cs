using UnityEngine;
using UnityEditor;
using UnityEngine.Localization;
using UnityEditor.Localization;
using SowurShield.Dialogue;

namespace SowurShield.Editor
{
/// <summary>
/// Builds a small placeholder dialogue tree for a villager, plus the en/pt/es localization
/// entries it needs. Split out from <see cref="VillagerPopulationTool"/> because dialogue
/// nodes carry <see cref="LocalizedString"/> fields, and creating one without also creating
/// its table entries produces a villager who talks in blank speech bubbles — the documented
/// SafeGetLocalizedString behaviour, which reads as a bug rather than as missing content.
///
/// The tree shape is deliberately minimal (greeting → farewell) but real: it exercises the
/// dialogue UI, the "give a gift" / "view relationship" choices injected by
/// NPCDialogueInteractable, and the daily-talk affinity award.
/// </summary>
public static class VillagerDialogueFactory
{
    private const string DialogueFolder = "Assets/Resources/Dialogues/Villagers";
    private const string TableName = "Dialogue";

    /// <summary>
    /// Returns the villager's dialogue tree, creating it (and its localization entries) on
    /// first call. Idempotent — re-running the population tool reuses the existing asset.
    /// </summary>
    public static DialogueTree CreateOrLoad(string npcId, string displayName)
    {
        string assetPath = $"{DialogueFolder}/{npcId}_Default.asset";

        var existing = AssetDatabase.LoadAssetAtPath<DialogueTree>(assetPath);
        if (existing != null) return existing;

        EnsureFolder();

        var tree = ScriptableObject.CreateInstance<DialogueTree>();
        tree.conversationId = $"{npcId}_default";
        tree.conversationDescription = $"Conversa padrão de {displayName} (placeholder)";
        tree.startNodeId = "start";
        tree.isRepeatable = true;

        string greetKey = $"dialogue.{npcId}.default.start";
        string byeKey   = $"dialogue.{npcId}.default.bye";

        // Bind before building the nodes: BindLine creates the table entries and returns a
        // LocalizedString carrying the real key id.
        var greetLine = BindLine(greetKey,
            en: "Hello there. Good to see a friendly face around here.",
            pt: "Olá. É bom ver um rosto amigo por aqui.",
            es: "Hola. Da gusto ver una cara amiga por aquí.");

        var byeLine = BindLine(byeKey,
            en: "Take care out there.",
            pt: "Se cuida por aí.",
            es: "Cuídate por ahí.");

        var start = new DialogueNode
        {
            nodeId = "start",
            speakerName = displayName,
            nodeType = NodeType.Dialogue,
            dialogueText = greetLine,
            nextNodeId = "bye"
        };

        // There is no NodeType.End: a conversation ends at a Dialogue node with no
        // nextNodeId and no choices.
        var bye = new DialogueNode
        {
            nodeId = "bye",
            speakerName = displayName,
            nodeType = NodeType.Dialogue,
            dialogueText = byeLine,
            nextNodeId = ""
        };

        tree.nodes = new[] { start, bye };

        AssetDatabase.CreateAsset(tree, assetPath);
        AssetDatabase.SaveAssets();
        return tree;
    }

    private static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(DialogueFolder)) return;

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Dialogues"))
            AssetDatabase.CreateFolder("Assets/Resources", "Dialogues");

        AssetDatabase.CreateFolder("Assets/Resources/Dialogues", "Villagers");
    }

    /// <summary>
    /// Writes one key into all three locales of the Dialogue table. Missing entries are the
    /// difference between a villager who speaks and one who opens an empty box.
    /// </summary>
    private static void AddLocalizedLine(string key, string en, string pt, string es)
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
        if (collection == null)
        {
            Debug.LogWarning($"[VillagerDialogueFactory] String table '{TableName}' not found — " +
                             $"'{key}' will render blank.");
            return;
        }

        var shared = collection.SharedData;
        var sharedEntry = shared.GetEntry(key) ?? shared.AddKey(key);

        foreach (var table in collection.StringTables)
        {
            string code = table.LocaleIdentifier.Code;
            string value = code.StartsWith("pt") ? pt : (code.StartsWith("es") ? es : en);
            table.AddEntry(key, value);
            EditorUtility.SetDirty(table);
        }
        EditorUtility.SetDirty(shared);

        // The LocalizedString on the node stores a key *id*, not the string. Nodes built here
        // are constructed with the name only, which leaves m_KeyId at 0 — a reference that
        // resolves to nothing at runtime and renders an empty speech bubble. Stamp the real id
        // so the table lookup succeeds.
        _lastKeyId = sharedEntry.Id;
    }

    private static long _lastKeyId;

    /// <summary>
    /// Builds a LocalizedString bound by key id rather than by name alone. A name-only
    /// reference leaves the id at 0 and silently resolves to nothing.
    /// </summary>
    private static LocalizedString BindLine(string key, string en, string pt, string es)
    {
        AddLocalizedLine(key, en, pt, es);

        var collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
        var localized = new LocalizedString(TableName, key);
        if (collection != null && _lastKeyId != 0)
            localized.TableEntryReference = _lastKeyId;

        return localized;
    }
}
} // namespace SowurShield.Editor
