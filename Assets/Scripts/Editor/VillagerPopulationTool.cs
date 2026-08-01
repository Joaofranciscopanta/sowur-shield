using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SowurShield.Dialogue;

namespace SowurShield.Editor
{
/// <summary>
/// Populates the farm scene with placeholder villagers so the relationship, gift, codex and
/// quest systems can actually be exercised. Before this, the game had exactly one NPC, which
/// made all four systems impossible to evaluate.
///
/// Everything here is explicitly placeholder: all villagers share one generic sprite and their
/// bios/lore are short. What is NOT placeholder is the *shape* of the data — gift preferences,
/// four lore tiers each, and dialogue trees — because that is what needs testing.
///
/// Run via Tools → NPC → Populate Village (Placeholders). Idempotent: villagers that already
/// exist are skipped, so it is safe to re-run after adding a new one to the table below.
/// </summary>
public static class VillagerPopulationTool
{
    private const float INTERACTION_RANGE = 3f;
    private const string PlaceholderSpritePath = "Assets/Art/NPCs/npc_villager_placeholder.png";

    /// <summary>
    /// One villager's full definition. Kept as a plain struct so the whole cast reads as a
    /// single table below — adding a villager should mean adding one entry, nothing else.
    /// </summary>
    private struct Villager
    {
        public string id;
        public string displayName;
        public string bio;
        public Vector2 position;
        public string[] loved;
        public string[] liked;
        public string[] disliked;
        public (float rel, string title, string body)[] lore;
    }

    // Gift preferences are drawn from the 16 items that currently have a giftAffinityValue.
    // Each villager loves something thematically tied to their trade, so the preference
    // system is testable by reasoning rather than by memorising a table.
    private static Villager[] BuildCast() => new Villager[]
    {
        new Villager {
            id = "tomas", displayName = "Tomás",
            bio = "O ferreiro do vale. Fala pouco, martela muito. Diz que metal honesto não mente.",
            position = new Vector2(-4f, 5f),
            loved = new[] { "Wood", "RareFish" },
            liked = new[] { "Bread", "Milk" },
            disliked = new[] { "Feather" },
            lore = new (float, string, string)[] {
                (  0f, "Quem é Tomás?", "O ferreiro. Aprendeu o ofício com o avô e nunca saiu do vale."),
                ( 10f, "A Forja", "A forja dele é a mais antiga do vale. O fogo nunca apagou completamente em quarenta anos."),
                ( 40f, "As Mãos", "As cicatrizes nas mãos dele contam cada erro que cometeu aprendendo. Ele diz que são o diploma dele."),
                ( 75f, "O Sino", "Tomás fundiu o sino da praça quando tinha vinte anos. Toda vez que toca, ele para o que está fazendo e escuta."),
            }
        },
        new Villager {
            id = "isabela", displayName = "Isabela",
            bio = "Padeira. Acorda às quatro da manhã e jura que o cheiro do pão é melhor que o gosto.",
            position = new Vector2(-6f, 2f),
            loved = new[] { "Egg", "Milk" },
            liked = new[] { "Pumpkin", "Carrot", "Apple" },
            disliked = new[] { "Rabbit Fur" },
            lore = new (float, string, string)[] {
                (  0f, "Quem é Isabela?", "A padeira do vale. O forno dela acende antes do sol."),
                ( 10f, "A Receita", "Ela usa a receita da avó, mas mudou uma coisa e não conta qual."),
                ( 40f, "O Inverno Difícil", "Num inverno em que faltou farinha, ela assou pão para o vale inteiro com o que tinha guardado para si."),
                ( 75f, "O Caderno", "Guarda um caderno de receitas que ninguém nunca leu. Diz que metade delas nunca deu certo."),
            }
        },
        new Villager {
            id = "joana", displayName = "Joana",
            bio = "Pescadora. Passa mais tempo no rio que em casa e considera isso perfeitamente normal.",
            position = new Vector2(7f, -3f),
            loved = new[] { "RareFish", "Fish" },
            liked = new[] { "Bread", "Radish" },
            disliked = new[] { "Wood" },
            lore = new (float, string, string)[] {
                (  0f, "Quem é Joana?", "A pescadora. Conhece cada curva do rio pelo som da água."),
                ( 10f, "O Rio", "Ela diz que o rio muda de humor com as estações, e que dá para ouvir."),
                ( 40f, "O Peixe que Escapou", "Fala de um peixe enorme que escapou dela há dez anos. Ninguém sabe se é verdade."),
                ( 75f, "O Barco do Pai", "O barco que ela usa era do pai. Já trocou cada tábua, mas insiste que é o mesmo barco."),
            }
        },
        new Villager {
            id = "elias", displayName = "Elias",
            bio = "Pastor. Conversa com as ovelhas e afirma, muito sério, que elas respondem.",
            position = new Vector2(-8f, -4f),
            loved = new[] { "Cabbage", "Milk" },
            liked = new[] { "Carrot", "Tomato" },
            disliked = new[] { "Fish" },
            lore = new (float, string, string)[] {
                (  0f, "Quem é Elias?", "O pastor. Some no campo por dias e volta como se nada fosse."),
                ( 10f, "O Rebanho", "Ele dá nome a cada animal do rebanho e nunca confunde nenhum."),
                ( 40f, "A Noite da Tempestade", "Passou uma noite inteira na chuva procurando uma ovelha perdida. Achou."),
                ( 75f, "O Silêncio", "Elias gosta do silêncio do campo porque, segundo ele, é o único lugar onde consegue pensar direito."),
            }
        },
        new Villager {
            id = "clara", displayName = "Clara",
            bio = "Herborista. Sabe o nome de todas as plantas e a serventia de quase todas.",
            position = new Vector2(5f, 6f),
            loved = new[] { "MysterySeed", "Medicine" },
            liked = new[] { "Radish", "Cabbage", "Tomato" },
            disliked = new[] { "Feather" },
            lore = new (float, string, string)[] {
                (  0f, "Quem é Clara?", "A herborista. Se você adoecer no vale, é a porta dela que você bate."),
                ( 10f, "O Jardim", "O jardim dela parece bagunçado, mas cada planta está exatamente onde ela quer."),
                ( 40f, "A Aprendiz", "Aprendeu com uma velha que morava na floresta e nunca teve nome, só apelido."),
                ( 75f, "A Planta que Falta", "Procura há anos uma erva que só viu uma vez, quando criança. Não sabe se existiu mesmo."),
            }
        },
        new Villager {
            id = "rui", displayName = "Rui",
            bio = "Carpinteiro. Mede três vezes, corta uma, e reclama de quem faz diferente.",
            position = new Vector2(-2f, -6f),
            loved = new[] { "Wood" },
            liked = new[] { "Bread", "Apple", "Banana" },
            disliked = new[] { "Rabbit Fur" },
            lore = new (float, string, string)[] {
                (  0f, "Quem é Rui?", "O carpinteiro. Construiu metade dos telhados do vale."),
                ( 10f, "A Madeira", "Ele escolhe a madeira batendo nela e ouvindo. Diz que cada árvore soa diferente."),
                ( 40f, "A Casa Torta", "A primeira casa que construiu saiu torta. Ela ainda está de pé, e ele ainda tem vergonha."),
                ( 75f, "O Berço", "Guarda um berço que fez para um filho que não chegou a nascer. Nunca conseguiu desmontar."),
            }
        },
        new Villager {
            id = "nara", displayName = "Nara",
            bio = "Viajante. Chegou faz pouco tempo e ainda não decidiu se fica.",
            position = new Vector2(9f, 3f),
            loved = new[] { "MysterySeed", "RareFish" },
            liked = new[] { "Apple", "Banana", "Egg" },
            disliked = new[] { "Wood" },
            lore = new (float, string, string)[] {
                (  0f, "Quem é Nara?", "Uma viajante. Ninguém sabe exatamente de onde ela veio."),
                ( 10f, "A Estrada", "Ela fala de cidades que ninguém no vale nunca viu."),
                ( 40f, "O Motivo", "Não veio para cá por acaso. Está procurando alguém, mas não diz quem."),
                ( 75f, "A Decisão", "Depois de tanto tempo na estrada, começou a considerar parar. Isso a assusta mais que viajar."),
            }
        },
        new Villager {
            id = "bento", displayName = "Bento",
            bio = "O mais velho do vale. Senta na praça e comenta tudo que passa.",
            position = new Vector2(2f, -8f),
            loved = new[] { "Bread", "Milk" },
            liked = new[] { "Apple", "Pumpkin", "Egg" },
            disliked = new[] { "MysterySeed" },
            lore = new (float, string, string)[] {
                (  0f, "Quem é Bento?", "O mais velho daqui. Viu o vale mudar três vezes."),
                ( 10f, "As Histórias", "Conta a mesma história de jeitos diferentes, e todas as versões são boas."),
                ( 40f, "O Que Ele Viu", "Estava aqui quando o vale quase secou. Diz que a terra se lembra disso."),
                ( 75f, "O Banco", "O banco onde ele senta foi feito pelo pai dele. É o último pedaço que restou da casa antiga."),
            }
        },
    };

    [MenuItem("Tools/NPC/Populate Village (Placeholders)")]
    public static void PopulateVillage()
    {
        PopulateVillage(showDialogs: true);
    }

    /// <summary>
    /// Population run. <paramref name="showDialogs"/> must be false when called from
    /// automation: EditorUtility.DisplayDialog is modal and blocks the editor until a human
    /// clicks it, which deadlocks any scripted caller.
    /// </summary>
    public static int PopulateVillage(bool showDialogs)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
        if (sprite == null)
        {
            string msg = $"Não achei {PlaceholderSpritePath}. Gere o placeholder antes de rodar isto.";
            if (showDialogs) EditorUtility.DisplayDialog("Sprite não encontrado", msg, "OK");
            else Debug.LogError("[VillagerPopulationTool] " + msg);
            return 0;
        }

        var existing = Object.FindObjectsByType<NPCDialogueInteractable>(FindObjectsSortMode.None);
        var cast = BuildCast();

        int created = 0, skipped = 0;
        foreach (var villager in cast)
        {
            bool alreadyThere = false;
            foreach (var npc in existing)
            {
                if (npc.GetNPCId() == villager.id) { alreadyThere = true; break; }
            }
            if (alreadyThere) { skipped++; continue; }

            CreateVillager(villager, sprite);
            created++;
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[VillagerPopulationTool] {created} aldeões criados, {skipped} já existiam. " +
                  "Salve a cena (Ctrl+S).");

        if (showDialogs)
        {
            EditorUtility.DisplayDialog("Aldeia populada",
                $"{created} aldeões criados ({skipped} já existiam).\n\n" +
                "Todos usam o mesmo sprite placeholder e têm:\n" +
                "• 4 níveis de codex cada\n" +
                "• preferências de presente (ama/gosta/não gosta)\n" +
                "• diálogo padrão\n\n" +
                "Salve a cena para persistir.", "OK");
        }

        return created;
    }

    private static void CreateVillager(Villager v, Sprite sprite)
    {
        var root = new GameObject(v.displayName);
        Undo.RegisterCreatedObjectUndo(root, "Spawn Villager");
        root.transform.position = new Vector3(v.position.x, v.position.y, 0f);

        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1;

        var col = root.AddComponent<CapsuleCollider2D>();
        col.isTrigger = true;
        col.size   = new Vector2(0.8f, 1.4f);
        col.offset = new Vector2(0f, 0.5f);

        var promptGO = new GameObject("InteractionPrompt");
        promptGO.transform.SetParent(root.transform, false);
        promptGO.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        promptGO.SetActive(false);

        var interactable = root.AddComponent<NPCDialogueInteractable>();
        var so = new SerializedObject(interactable);

        so.FindProperty("npcId").stringValue           = v.id;
        so.FindProperty("npcDisplayName").stringValue  = v.displayName;
        so.FindProperty("npcBio").stringValue          = v.bio;
        so.FindProperty("interactionRange").floatValue = INTERACTION_RANGE;

        so.FindProperty("allowRepeatedConversations").boolValue   = true;
        so.FindProperty("cooldownBetweenInteractions").floatValue = 1f;
        so.FindProperty("disableMovementDuringDialogue").boolValue = true;
        so.FindProperty("enableGifting").boolValue  = true;
        so.FindProperty("enableSeedShop").boolValue = false;

        so.FindProperty("interactionPrompt").objectReferenceValue = promptGO;

        SetStringArray(so, "lovedGifts",    v.loved);
        SetStringArray(so, "likedGifts",    v.liked);
        SetStringArray(so, "dislikedGifts", v.disliked);

        var loreProp = so.FindProperty("loreEntries");
        loreProp.arraySize = v.lore.Length;
        for (int i = 0; i < v.lore.Length; i++)
        {
            var elem = loreProp.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("requiredRelationship").floatValue = v.lore[i].rel;
            elem.FindPropertyRelative("title").stringValue               = v.lore[i].title;
            elem.FindPropertyRelative("body").stringValue                = v.lore[i].body;
        }

        // Dialogue tree is generated to match, so the villager is talkable immediately.
        var tree = VillagerDialogueFactory.CreateOrLoad(v.id, v.displayName);
        if (tree != null)
        {
            so.FindProperty("defaultDialogue").objectReferenceValue = tree;
            var avail = so.FindProperty("availableDialogues");
            avail.arraySize = 1;
            avail.GetArrayElementAtIndex(0).objectReferenceValue = tree;
        }

        so.ApplyModifiedProperties();
    }

    private static void SetStringArray(SerializedObject so, string propertyName, string[] values)
    {
        var prop = so.FindProperty(propertyName);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).stringValue = values[i];
    }
}
} // namespace SowurShield.Editor
