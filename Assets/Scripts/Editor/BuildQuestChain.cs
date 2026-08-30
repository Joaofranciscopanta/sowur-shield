using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using SowurShield.Dialogue;

namespace SowurShield.Editor
{

/// <summary>
/// Builds a prerequisite-gated quest chain on top of the seven quests the project already had.
///
/// <para>Those seven are all entry-level and unordered: finish any one and nothing opens up.
/// That is why the game has no reason to be played on a second day -- there is no ladder, only
/// a handful of rungs lying on the floor. This adds six quests that each require the one
/// before, escalating in scale and reward, and ending on a stage the player cannot reach until
/// they have actually built a farm.</para>
///
/// <para>The chain deliberately reuses the four auto-tracked objective types rather than
/// inventing mechanics: every step is something the game can already detect, so nothing here
/// depends on new gameplay code.</para>
///
/// <list type="bullet">
/// <item><b>settling_in</b> — harvest 3 carrots. Opens after first_harvest, so it is the first
/// thing a new player sees after the tutorial's single carrot.</item>
/// <item><b>full_pantry</b> — 5 tomatoes and 5 cabbages. Forces the second and third crop
/// types, which the starting quests never touch.</item>
/// <item><b>the_herd</b> — 10 eggs and 5 milk. Scale beyond Isabela's 3-and-2, so the player
/// has to actually keep animals fed rather than collect a few strays.</item>
/// <item><b>market_day</b> — 8 pumpkins, the slowest crop, for the largest farming payout.</item>
/// <item><b>proving_grounds</b> — clear Whispering Woods. The second combat stage, gated
/// behind the whole farming ladder.</item>
/// <item><b>valley_keeper</b> — clear Ancient Forest and talk to every villager. Once the
/// capstone; now the midpoint.</item>
/// </list>
///
/// <para>A later pass added eight more. Four continue the ladder past valley_keeper, which used
/// to be the end: Cave, Mountain and Volcano had no quest pointing at them and not one of the
/// five bosses was ever named, so the back half of the combat content was invisible. Four more
/// hang off the side as optional branches, covering fishing, the axe, and the four villagers
/// who never asked the player for anything.</para>
///
/// <list type="bullet">
/// <item><b>quiet_water</b> → <b>the_one_that_stayed</b> — fishing, for Joana.</item>
/// <item><b>timber</b> — chopping, for Rui.</item>
/// <item><b>a_word_with_everyone</b> — the remaining four villagers.</item>
/// <item><b>first_fang</b> → <b>into_the_dark</b> → <b>above_the_clouds</b> →
/// <b>ash_and_ember</b> — one boss per biome, ending on the final dragon.</item>
/// </list>
///
/// <para>Counts on the side branches are kept low on purpose: CollectItem tracks the item count
/// currently held, not a running total, so selling the fish before turning the quest in walks
/// the progress backwards. Twelve wood is a chore; forty would be a trap.</para>
///
/// Menu: Sowur Shield > Quests > Build Quest Chain
/// </summary>
public static class BuildQuestChain
{
    private const string QuestFolder = "Assets/Resources/Quests";
    private const string Table = "Dialogue";

    private class Step
    {
        public string Id;
        public string Prereq;
        public int Gold;
        public (string en, string pt, string es) Title;
        public (string en, string pt, string es) Desc;
        public List<(QuestObjectiveType type, string target, int count,
                     string en, string pt, string es)> Objectives = new();

        /// <summary>Item payouts, by ItemDatabase name. Empty for a gold-only quest.</summary>
        public List<(string item, int qty)> ItemRewards = new();

        /// <summary>Relationship gains, by NPC id — the reward for quests given by a villager.</summary>
        public List<(string npc, float amount)> RelationshipRewards = new();
    }

    private static List<Step> Chain() => new()
    {
        new Step {
            Id = "settling_in", Prereq = "first_harvest", Gold = 75,
            Title = ("Settling In", "Se Ajeitando", "Acomodándose"),
            Desc  = ("One carrot is a start. Three is a habit.",
                     "Uma cenoura é um começo. Três é um hábito.",
                     "Una zanahoria es un comienzo. Tres es un hábito."),
            Objectives = { (QuestObjectiveType.HarvestCrop, "Carrot", 3,
                            "Harvest 3 carrots", "Colha 3 cenouras", "Cosecha 3 zanahorias") }
        },
        new Step {
            Id = "full_pantry", Prereq = "settling_in", Gold = 140,
            Title = ("A Full Pantry", "Despensa Cheia", "Una Despensa Llena"),
            Desc  = ("Carrots alone make a dull winter. Try something else.",
                     "Só de cenoura ninguém passa o inverno. Plante outra coisa.",
                     "Solo con zanahorias no se pasa el invierno. Planta otra cosa."),
            Objectives = {
                (QuestObjectiveType.CollectItem, "Tomato", 5,
                 "Gather 5 tomatoes", "Junte 5 tomates", "Reúne 5 tomates"),
                (QuestObjectiveType.CollectItem, "Cabbage", 5,
                 "Gather 5 cabbages", "Junte 5 repolhos", "Reúne 5 repollos"),
            }
        },
        new Step {
            Id = "the_herd", Prereq = "full_pantry", Gold = 200,
            Title = ("The Herd", "O Rebanho", "El Rebaño"),
            Desc  = ("Animals that are fed give back. Animals that are not, do not.",
                     "Animal alimentado retribui. Animal esquecido, não.",
                     "El animal alimentado retribuye. El olvidado, no."),
            Objectives = {
                (QuestObjectiveType.CollectItem, "Egg", 10,
                 "Collect 10 eggs", "Colete 10 ovos", "Recoge 10 huevos"),
                (QuestObjectiveType.CollectItem, "Milk", 5,
                 "Collect 5 milk", "Colete 5 leites", "Recoge 5 leches"),
            }
        },
        new Step {
            Id = "market_day", Prereq = "the_herd", Gold = 320,
            Title = ("Market Day", "Dia de Feira", "Día de Mercado"),
            Desc  = ("Pumpkins take their time. So does a reputation.",
                     "Abóbora leva tempo. Reputação também.",
                     "La calabaza toma su tiempo. La reputación también."),
            Objectives = { (QuestObjectiveType.CollectItem, "Pumpkin", 8,
                            "Gather 8 pumpkins", "Junte 8 abóboras", "Reúne 8 calabazas") }
        },
        new Step {
            Id = "proving_grounds", Prereq = "market_day", Gold = 260,
            Title = ("Proving Grounds", "Campo de Provas", "Campo de Pruebas"),
            Desc  = ("The woods have been restless. Your animals are ready.",
                     "A mata anda inquieta. Seus animais estão prontos.",
                     "El bosque está inquieto. Tus animales están listos."),
            Objectives = { (QuestObjectiveType.CompleteBattle, "Whispering Woods", 1,
                            "Clear Whispering Woods", "Limpe o Bosque Sussurrante",
                            "Despeja el Bosque Susurrante") }
        },
        new Step {
            Id = "valley_keeper", Prereq = "proving_grounds", Gold = 500,
            Title = ("Keeper of the Valley", "Guardião do Vale", "Guardián del Valle"),
            Desc  = ("The valley knows your name now. Go and earn it.",
                     "O vale já sabe seu nome. Vá merecê-lo.",
                     "El valle ya sabe tu nombre. Ve a merecerlo."),
            Objectives = {
                (QuestObjectiveType.CompleteBattle, "Ancient Forest", 1,
                 "Clear Ancient Forest", "Limpe a Floresta Ancestral",
                 "Despeja el Bosque Ancestral"),
                (QuestObjectiveType.TalkToNPC, "maren_default", 1,
                 "Tell Maren", "Conte para a Maren", "Cuéntaselo a Maren"),
            }
        },

        // ── Side branches ─────────────────────────────────────────────
        // These hang off the main ladder rather than extending it, so a player who wants a
        // break from farming has somewhere to go. Each is built on a system that had no quest
        // at all before: fishing, the axe, and the four villagers who never asked for anything.

        new Step {
            Id = "quiet_water", Prereq = "settling_in", Gold = 90,
            Title = ("Quiet Water", "Água Parada", "Agua Quieta"),
            Desc  = ("Joana says the river gives to those who wait. Go and wait.",
                     "A Joana diz que o rio dá a quem sabe esperar. Vá esperar.",
                     "Joana dice que el río da a quien sabe esperar. Ve a esperar."),
            Objectives = {
                (QuestObjectiveType.CollectItem, "Fish", 4,
                 "Catch 4 fish", "Pesque 4 peixes", "Pesca 4 peces"),
                (QuestObjectiveType.TalkToNPC, "joana_default", 1,
                 "Show Joana", "Mostre para a Joana", "Muéstraselo a Joana"),
            },
            RelationshipRewards = { ("joana", 10f) }
        },
        new Step {
            Id = "the_one_that_stayed", Prereq = "quiet_water", Gold = 220,
            Title = ("The One That Stayed", "O Que Não Escapou", "El Que No Escapó"),
            Desc  = ("Every fisher has a story about the big one. Joana wants proof.",
                     "Todo pescador tem a história do peixe grande. A Joana quer prova.",
                     "Todo pescador tiene la historia del pez grande. Joana quiere pruebas."),
            Objectives = { (QuestObjectiveType.CollectItem, "RareFish", 1,
                            "Catch a rare fish", "Pesque um peixe raro", "Pesca un pez raro") },
            RelationshipRewards = { ("joana", 15f) }
        },
        new Step {
            Id = "timber", Prereq = "settling_in", Gold = 110,
            Title = ("Timber", "Madeira", "Madera"),
            Desc  = ("Rui needs planks and has no axe arm left. You have both.",
                     "O Rui precisa de tábuas e não tem mais braço pra machado. Você tem os dois.",
                     "Rui necesita tablas y ya no tiene brazo para el hacha. Tú tienes ambos."),
            Objectives = {
                (QuestObjectiveType.CollectItem, "Wood", 12,
                 "Chop 12 wood", "Corte 12 madeiras", "Corta 12 maderas"),
                (QuestObjectiveType.TalkToNPC, "rui_default", 1,
                 "Deliver to Rui", "Entregue ao Rui", "Entrégaselo a Rui"),
            },
            RelationshipRewards = { ("rui", 10f) }
        },
        new Step {
            Id = "a_word_with_everyone", Prereq = "meet_the_village", Gold = 180,
            Title = ("A Word With Everyone", "Uma Palavra com Cada Um", "Una Palabra con Cada Uno"),
            Desc  = ("You have met three. There are more, and they have noticed.",
                     "Você conheceu três. Tem mais gente, e eles repararam.",
                     "Has conocido a tres. Hay más, y se han dado cuenta."),
            Objectives = {
                (QuestObjectiveType.TalkToNPC, "clara_default", 1,
                 "Talk to Clara", "Fale com a Clara", "Habla con Clara"),
                (QuestObjectiveType.TalkToNPC, "elias_default", 1,
                 "Talk to Elias", "Fale com o Elias", "Habla con Elías"),
                (QuestObjectiveType.TalkToNPC, "nara_default", 1,
                 "Talk to Nara", "Fale com a Nara", "Habla con Nara"),
                (QuestObjectiveType.TalkToNPC, "rui_default", 1,
                 "Talk to Rui", "Fale com o Rui", "Habla con Rui"),
            },
            RelationshipRewards = { ("clara", 5f), ("elias", 5f), ("nara", 5f), ("rui", 5f) }
        },

        // ── The ladder continues past valley_keeper ────────────────────────
        // The old chain ended there, leaving three whole biomes -- Cave, Mountain and Volcano --
        // with no quest pointing at them, and not one of the five bosses ever named. These four
        // carry the player through them and finish on the final boss.

        new Step {
            Id = "first_fang", Prereq = "valley_keeper", Gold = 400,
            Title = ("First Fang", "Primeira Presa", "Primer Colmillo"),
            Desc  = ("Something big has been circling the pasture. End it.",
                     "Algo grande anda rondando o pasto. Acabe com isso.",
                     "Algo grande ronda el pastizal. Acaba con ello."),
            Objectives = { (QuestObjectiveType.CompleteBattle, "Peaceful Pasture — Boss", 1,
                            "Defeat the Meadow Wolf", "Derrote o Lobo da Pradaria",
                            "Derrota al Lobo de la Pradera") },
            ItemRewards = { ("Medicine", 2) }
        },
        new Step {
            Id = "into_the_dark", Prereq = "first_fang", Gold = 480,
            Title = ("Into the Dark", "Rumo ao Escuro", "Hacia la Oscuridad"),
            Desc  = ("The caves under the ridge were sealed for a reason. That reason is awake.",
                     "As cavernas sob a serra foram seladas por um motivo. O motivo acordou.",
                     "Las cuevas bajo la sierra fueron selladas por algo. Ese algo despertó."),
            Objectives = {
                (QuestObjectiveType.CompleteBattle, "Crystal Cavern", 1,
                 "Clear Crystal Cavern", "Limpe a Caverna de Cristal",
                 "Despeja la Caverna de Cristal"),
                (QuestObjectiveType.CompleteBattle, "Stalactite Hall — Boss", 1,
                 "Defeat the Cave Troll", "Derrote o Troll da Caverna",
                 "Derrota al Troll de la Cueva"),
            },
            ItemRewards = { ("Medicine", 3) }
        },
        new Step {
            Id = "above_the_clouds", Prereq = "into_the_dark", Gold = 600,
            Title = ("Above the Clouds", "Acima das Nuvens", "Sobre las Nubes"),
            Desc  = ("The mountain does not care that you have come this far.",
                     "A montanha não está nem aí pra onde você já chegou.",
                     "A la montaña no le importa lo lejos que hayas llegado."),
            Objectives = {
                (QuestObjectiveType.CompleteBattle, "Cloud Ridge", 1,
                 "Clear Cloud Ridge", "Limpe a Crista das Nuvens",
                 "Despeja la Cresta de las Nubes"),
                (QuestObjectiveType.CompleteBattle, "Frozen Summit — Boss", 1,
                 "Defeat the Mountain King", "Derrote o Rei da Montanha",
                 "Derrota al Rey de la Montaña"),
            },
            ItemRewards = { ("Medicine", 3) }
        },
        new Step {
            Id = "ash_and_ember", Prereq = "above_the_clouds", Gold = 1000,
            Title = ("Ash and Ember", "Cinza e Brasa", "Ceniza y Brasa"),
            Desc  = ("It began with one carrot. It ends in the mouth of a volcano.",
                     "Começou com uma cenoura. Termina na boca de um vulcão.",
                     "Empezó con una zanahoria. Termina en la boca de un volcán."),
            Objectives = {
                (QuestObjectiveType.CompleteBattle, "Magma Core — Final Boss", 1,
                 "Defeat the Inferno Dragon", "Derrote o Dragão do Inferno",
                 "Derrota al Dragón del Infierno"),
                (QuestObjectiveType.TalkToNPC, "maren_default", 1,
                 "Go home to Maren", "Volte para a Maren", "Vuelve con Maren"),
            },
            ItemRewards = { ("Medicine", 5) },
            RelationshipRewards = { ("maren", 20f) }
        },
    };

    [MenuItem("Sowur Shield/Quests/Build Quest Chain")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(QuestFolder))
        {
            string parent = System.IO.Path.GetDirectoryName(QuestFolder).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(QuestFolder));
        }

        StringTableCollection collection =
            LocalizationEditorSettings.GetStringTableCollection(Table);
        if (collection == null)
        {
            Debug.LogError($"[BuildQuestChain] No '{Table}' string table collection.");
            return;
        }

        int made = 0;
        foreach (Step step in Chain())
        {
            WriteStrings(collection, step);
            if (WriteAsset(step)) made++;
        }

        EditorUtility.SetDirty(collection.SharedData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BuildQuestChain] {made} quest(s) written to {QuestFolder}. " +
                  "QuestManager loads them from Resources on Awake.");
    }

    private static bool WriteAsset(Step step)
    {
        string path = $"{QuestFolder}/Quest_{ToPascal(step.Id)}.asset";

        QuestData quest = AssetDatabase.LoadAssetAtPath<QuestData>(path);
        bool isNew = quest == null;
        if (isNew)
        {
            quest = ScriptableObject.CreateInstance<QuestData>();
            AssetDatabase.CreateAsset(quest, path);
        }

        quest.questId = step.Id;
        quest.questTitle = new LocalizedString(Table, $"quest.{step.Id}.title");
        quest.questDescription = new LocalizedString(Table, $"quest.{step.Id}.description");
        quest.rewardGold = step.Gold;

        quest.prerequisiteQuestIds = string.IsNullOrEmpty(step.Prereq)
            ? new List<string>()
            : new List<string> { step.Prereq };

        quest.objectives = new List<QuestObjective>();
        for (int i = 0; i < step.Objectives.Count; i++)
        {
            var o = step.Objectives[i];
            quest.objectives.Add(new QuestObjective
            {
                description = new LocalizedString(Table, $"quest.{step.Id}.objective{i}"),
                type = o.type,
                targetId = o.target,
                requiredCount = o.count,
            });
        }

        quest.rewardItems = step.ItemRewards
            .Select(r => new QuestItemReward { itemName = r.item, quantity = r.qty })
            .ToList();

        quest.rewardRelationships = step.RelationshipRewards
            .Select(r => new QuestRelationshipReward { npcId = r.npc, amount = r.amount })
            .ToList();

        EditorUtility.SetDirty(quest);
        return isNew;
    }

    private static void WriteStrings(StringTableCollection collection, Step step)
    {
        Set(collection, $"quest.{step.Id}.title", step.Title);
        Set(collection, $"quest.{step.Id}.description", step.Desc);

        for (int i = 0; i < step.Objectives.Count; i++)
        {
            var o = step.Objectives[i];
            Set(collection, $"quest.{step.Id}.objective{i}", (o.en, o.pt, o.es));
        }
    }

    private static void Set(StringTableCollection collection, string key,
                            (string en, string pt, string es) values)
    {
        if (!collection.SharedData.Contains(key)) collection.SharedData.AddKey(key);
        var entry = collection.SharedData.GetEntry(key);

        foreach (StringTable table in collection.StringTables)
        {
            string code = table.LocaleIdentifier.Code.Split('-')[0].ToLowerInvariant();
            string value = code switch
            {
                "en" => values.en,
                "pt" => values.pt,
                "es" => values.es,
                _ => null,
            };
            if (value == null) continue;

            table.AddEntry(entry.Id, value);
            EditorUtility.SetDirty(table);
        }
    }

    private static string ToPascal(string snake) =>
        string.Concat(snake.Split('_').Select(p =>
            p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p.Substring(1)));
}

}
