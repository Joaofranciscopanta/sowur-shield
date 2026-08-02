using System.Collections.Generic;
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
    /// Per-villager flavour for the two topic branches. Keeping the trade-specific lines here
    /// means every villager exercises the same tree shape while still sounding like themselves —
    /// a placeholder that reads as written content rather than as lorem ipsum.
    ///
    /// Falls back to <see cref="GenericFlavour"/> for any id not listed, so adding a villager
    /// never produces a blank speech bubble.
    /// </summary>
    private static readonly Dictionary<string, Flavour> TradeFlavour = new Dictionary<string, Flavour>
    {
        ["tomas"] = new Flavour(
            workEn: "The forge keeps me honest. Metal tells you straight away when you've rushed it.",
            workPt: "A forja me mantém honesto. O metal avisa na hora quando você teve pressa.",
            workEs: "La forja me mantiene honesto. El metal te avisa enseguida cuando te apuras."),
        ["isabela"] = new Flavour(
            workEn: "Four in the morning, every morning. The oven doesn't care how I slept.",
            workPt: "Quatro da manhã, toda manhã. O forno não quer saber como eu dormi.",
            workEs: "Cuatro de la mañana, cada mañana. Al horno no le importa cómo dormí."),
        ["joana"] = new Flavour(
            workEn: "The river's been generous this week. Ask me again after the rains.",
            workPt: "O rio andou generoso essa semana. Me pergunte de novo depois das chuvas.",
            workEs: "El río ha sido generoso esta semana. Pregúntame otra vez tras las lluvias."),
        ["elias"] = new Flavour(
            workEn: "The flock's well. They complain less than people do, and they mean it more.",
            workPt: "O rebanho vai bem. Reclamam menos que gente, e com mais razão.",
            workEs: "El rebaño está bien. Se quejan menos que la gente, y con más razón."),
        ["clara"] = new Flavour(
            workEn: "Everything growing has a use. Most people only learn that when they need it.",
            workPt: "Tudo que cresce tem serventia. A maioria só descobre isso quando precisa.",
            workEs: "Todo lo que crece sirve para algo. Casi todos lo aprenden solo cuando lo necesitan."),
        ["rui"] = new Flavour(
            workEn: "Measure three times, cut once. I've yet to regret the extra measuring.",
            workPt: "Medir três vezes, cortar uma. Nunca me arrependi de medir a mais.",
            workEs: "Medir tres veces, cortar una. Nunca me he arrepentido de medir de más."),
        ["nara"] = new Flavour(
            workEn: "Still finding my footing here. Ask me next season and I might have an answer.",
            workPt: "Ainda me achando por aqui. Me pergunte na próxima estação, talvez eu saiba.",
            workEs: "Todavía me estoy ubicando aquí. Pregúntame la próxima estación, quizá lo sepa."),
        ["bento"] = new Flavour(
            workEn: "My work is watching, and business is good. Plenty passes through this square.",
            workPt: "Meu trabalho é observar, e anda rendendo. Passa muita coisa por esta praça.",
            workEs: "Mi trabajo es mirar, y va bien. Por esta plaza pasa de todo."),
    };

    private static readonly Flavour GenericFlavour = new Flavour(
        workEn: "Same as always. Work doesn't finish, it just pauses for the night.",
        workPt: "Como sempre. O trabalho não acaba, só para para dormir.",
        workEs: "Como siempre. El trabajo no termina, solo se detiene por la noche.");

    /// <summary>One villager's trade-specific answer to "how's the work going?".</summary>
    private readonly struct Flavour
    {
        public readonly string WorkEn, WorkPt, WorkEs;

        public Flavour(string workEn, string workPt, string workEs)
        {
            WorkEn = workEn; WorkPt = workPt; WorkEs = workEs;
        }
    }

    /// <summary>
    /// Returns the villager's dialogue tree, creating it (and its localization entries) on
    /// first call. Idempotent — re-running the population tool reuses the existing asset.
    ///
    /// Shape: a greeting hub with three choices (work / village / farewell), each topic
    /// looping back to the hub so the player can read both before leaving. This exercises the
    /// choice UI, the hub-and-spoke navigation real conversations use, and the gift/relationship
    /// choices NPCDialogueInteractable injects — none of which a linear two-node stub reaches.
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

        Flavour flavour = TradeFlavour.TryGetValue(npcId, out var f) ? f : GenericFlavour;

        string K(string suffix) => $"dialogue.{npcId}.default.{suffix}";

        // Bind before building the nodes: BindLine creates the table entries and returns a
        // LocalizedString carrying the real key id.
        var greetLine = BindLine(K("start"),
            en: "Hello there. Good to see a friendly face around here.",
            pt: "Olá. É bom ver um rosto amigo por aqui.",
            es: "Hola. Da gusto ver una cara amiga por aquí.");

        var workLine = BindLine(K("work"),
            en: flavour.WorkEn, pt: flavour.WorkPt, es: flavour.WorkEs);

        var villageLine = BindLine(K("village"),
            en: "The valley looks after its own. Slow place, but nobody's a stranger for long.",
            pt: "O vale cuida dos seus. Lugar devagar, mas ninguém fica estranho por muito tempo.",
            es: "El valle cuida de los suyos. Sitio tranquilo, pero nadie es forastero mucho tiempo.");

        var byeLine = BindLine(K("bye"),
            en: "Take care out there.",
            pt: "Se cuida por aí.",
            es: "Cuídate por ahí.");

        var askWork = BindLine(K("choice.work"),
            en: "How's the work going?",
            pt: "Como vai o trabalho?",
            es: "¿Cómo va el trabajo?");

        var askVillage = BindLine(K("choice.village"),
            en: "Tell me about the village.",
            pt: "Fale-me da vila.",
            es: "Háblame del pueblo.");

        var askBye = BindLine(K("choice.bye"),
            en: "I should get going.",
            pt: "Preciso ir andando.",
            es: "Debería irme.");

        var backLine = BindLine(K("choice.back"),
            en: "Something else, then.",
            pt: "Outra coisa, então.",
            es: "Otra cosa, entonces.");

        // Hub: the only Choice node. Topic nodes loop back here via a "back" choice, so the
        // player can visit both without re-triggering the greeting.
        var start = new DialogueNode
        {
            nodeId = "start",
            speakerName = displayName,
            nodeType = NodeType.Choice,
            dialogueText = greetLine,
            choices = new[]
            {
                new DialogueChoice { choiceText = askWork,    nextNodeId = "work" },
                new DialogueChoice { choiceText = askVillage, nextNodeId = "village" },
                new DialogueChoice { choiceText = askBye,     nextNodeId = "bye" },
            }
        };

        var work = new DialogueNode
        {
            nodeId = "work",
            speakerName = displayName,
            nodeType = NodeType.Choice,
            dialogueText = workLine,
            choices = new[]
            {
                new DialogueChoice { choiceText = backLine, nextNodeId = "start" },
                new DialogueChoice { choiceText = askBye,   nextNodeId = "bye" },
            }
        };

        var village = new DialogueNode
        {
            nodeId = "village",
            speakerName = displayName,
            nodeType = NodeType.Choice,
            dialogueText = villageLine,
            choices = new[]
            {
                new DialogueChoice { choiceText = backLine, nextNodeId = "start" },
                new DialogueChoice { choiceText = askBye,   nextNodeId = "bye" },
            }
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

        tree.nodes = new[] { start, work, village, bye };

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
