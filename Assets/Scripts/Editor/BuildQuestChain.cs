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
/// <item><b>valley_keeper</b> — clear Ancient Forest and talk to every villager. The capstone:
/// it cannot be finished without having engaged with both halves of the game.</item>
/// </list>
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
