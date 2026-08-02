using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Localization;
using UnityEditor.Localization;
using SowurShield.Dialogue;

namespace SowurShield.Editor
{
/// <summary>
/// Migrates the NPC Codex text (bio + lore tiers) from raw scene strings into the
/// Dialogue string table, so it follows the selected language like everything else.
///
/// The bug this fixes: npcBio and NpcLoreEntry.title/body were plain strings baked into
/// SampleScene in Portuguese. Switching to English or Spanish changed the dialogue, the
/// menus and the item names, but the Codex kept showing Portuguese, because a raw string
/// has no locale to resolve against.
///
/// This writes en/pt/es entries for every NPC currently in the scene and points the new
/// LocalizedString fields at them by key *id* — a name-only reference leaves the id at 0
/// and silently resolves to nothing, the same trap documented in VillagerDialogueFactory.
/// The legacy raw fields are deliberately left untouched as a fallback.
///
/// Idempotent: re-running overwrites the same keys and re-binds the same references.
/// </summary>
public static class CodexLocalizationTool
{
    private const string TableName = "Dialogue";

    /// <summary>
    /// Translations for the placeholder cast. Keyed by the Portuguese source string so the
    /// table survives NPCs being re-created by VillagerPopulationTool, which is the only
    /// thing that writes these strings today.
    ///
    /// Portuguese is the source of truth here because that is what is already in the scene;
    /// en/es are translations of it rather than the other way round.
    /// </summary>
    private static readonly Dictionary<string, (string en, string es)> Translations =
        new Dictionary<string, (string, string)>
    {
        // ---- Bios ----
        ["O ferreiro do vale. Fala pouco, martela muito. Diz que metal honesto não mente."] =
            ("The valley's blacksmith. Says little, hammers a lot. Claims honest metal never lies.",
             "El herrero del valle. Habla poco, martillea mucho. Dice que el metal honesto no miente."),
        ["Padeira. Acorda às quatro da manhã e jura que o cheiro do pão é melhor que o gosto."] =
            ("Baker. Wakes at four in the morning and swears the smell of bread beats the taste.",
             "Panadera. Se levanta a las cuatro y jura que el olor del pan supera al sabor."),
        ["Pescadora. Passa mais tempo no rio que em casa e considera isso perfeitamente normal."] =
            ("Fisherwoman. Spends more time on the river than at home, and finds that perfectly normal.",
             "Pescadora. Pasa más tiempo en el río que en casa, y lo considera perfectamente normal."),
        ["Pastor. Conversa com as ovelhas e afirma, muito sério, que elas respondem."] =
            ("Shepherd. Talks to his sheep and insists, quite seriously, that they answer.",
             "Pastor. Habla con las ovejas y afirma, muy serio, que le responden."),
        ["Herborista. Sabe o nome de todas as plantas e a serventia de quase todas."] =
            ("Herbalist. Knows the name of every plant and the use of nearly every one.",
             "Herborista. Sabe el nombre de todas las plantas y para qué sirven casi todas."),
        ["Carpinteiro. Mede três vezes, corta uma, e reclama de quem faz diferente."] =
            ("Carpenter. Measures three times, cuts once, and grumbles at anyone who does otherwise.",
             "Carpintero. Mide tres veces, corta una, y se queja de quien lo hace distinto."),
        ["Viajante. Chegou faz pouco tempo e ainda não decidiu se fica."] =
            ("Traveller. Arrived not long ago and still hasn't decided whether to stay.",
             "Viajera. Llegó hace poco y aún no ha decidido si se queda."),
        ["O mais velho do vale. Senta na praça e comenta tudo que passa."] =
            ("The oldest soul in the valley. Sits in the square and comments on all that passes.",
             "El más viejo del valle. Se sienta en la plaza y comenta todo lo que pasa."),
        ["Cuida das sementes e do que nasce delas. Sabe o nome de cada canteiro do vale."] =
            ("Tends the seeds and whatever grows from them. Knows every plot in the valley by name.",
             "Cuida de las semillas y de lo que nace de ellas. Conoce cada bancal del valle por su nombre."),

        // ---- Maren ----
        // Maren predates the villager cast and was authored in English, so her source strings
        // are English rather than Portuguese. SourceIsEnglish() detects this; the pt/es values
        // below are the translations, and the dictionary key doubles as the en value.
        ["Maren arrived in the valley after losing three harvests in a row to a devastating drought. She lost everything she had planted in the eastern valley, but found something different in this land - a fertility that feels almost blessed. She sells seeds, but what she truly sells is hope."] =
            ("Maren chegou ao vale depois de perder três colheitas seguidas para uma seca devastadora. Perdeu tudo o que havia plantado no vale oriental, mas encontrou algo diferente nesta terra — uma fertilidade que parece quase abençoada. Ela vende sementes, mas o que vende de verdade é esperança.",
             "Maren llegó al valle tras perder tres cosechas seguidas por una sequía devastadora. Perdió todo lo que había plantado en el valle oriental, pero encontró algo distinto en esta tierra: una fertilidad que parece casi bendita. Vende semillas, pero lo que vende de verdad es esperanza."),
        ["Who is Maren?"] = ("Quem é Maren?", "¿Quién es Maren?"),
        ["The Valley"] = ("O Vale", "El Valle"),
        ["The Eastern Valley Drought"] = ("A Seca do Vale Oriental", "La Sequía del Valle Oriental"),
        ["Her Father's Seed"] = ("A Semente do Pai", "La Semilla de su Padre"),
        ["Maren is the valley's seed merchant. She wakes before sunrise just to feel the soil between her fingers - there is a magic in that first moment when a seed opens."] =
            ("Maren é a vendedora de sementes do vale. Acorda antes do sol só para sentir a terra entre os dedos — há uma magia naquele primeiro instante em que a semente se abre.",
             "Maren es la vendedora de semillas del valle. Se levanta antes del amanecer solo para sentir la tierra entre los dedos: hay una magia en ese primer instante en que una semilla se abre."),
        ["This valley has history. People come, plant, harvest and leave. Something here keeps you - the land, the rhythm of the seasons. They say a druid blessed the deepest layers of the soil."] =
            ("Este vale tem história. As pessoas chegam, plantam, colhem e vão embora. Algo aqui prende — a terra, o ritmo das estações. Dizem que um druida abençoou as camadas mais fundas do solo.",
             "Este valle tiene historia. La gente llega, planta, cosecha y se va. Algo aquí te retiene: la tierra, el ritmo de las estaciones. Dicen que un druida bendijo las capas más profundas del suelo."),
        ["Maren lost three harvests in a row to a devastating drought in the eastern valley. She arrived with nothing, but found a land that welcomed her differently. Her mother used to say the seed that survives winter blooms stronger."] =
            ("Maren perdeu três colheitas seguidas para uma seca devastadora no vale oriental. Chegou sem nada, mas encontrou uma terra que a acolheu de outro jeito. A mãe dela dizia que a semente que sobrevive ao inverno floresce mais forte.",
             "Maren perdió tres cosechas seguidas por una sequía devastadora en el valle oriental. Llegó sin nada, pero encontró una tierra que la acogió de otro modo. Su madre decía que la semilla que sobrevive al invierno florece más fuerte."),
        ["Maren carries a mysterious seed found in her father's coat pocket the day he died. She never knew what plant it was - she never had the courage to plant it, afraid of losing the last thing she has of him."] =
            ("Maren carrega uma semente misteriosa encontrada no bolso do casaco do pai no dia em que ele morreu. Nunca soube de que planta era — nunca teve coragem de plantá-la, com medo de perder a última coisa que tem dele.",
             "Maren lleva una semilla misteriosa que encontró en el bolsillo del abrigo de su padre el día que murió. Nunca supo de qué planta era; nunca tuvo el valor de plantarla, por miedo a perder lo último que le queda de él."),

        // ---- Lore titles ----
        ["Quem é Tomás?"]   = ("Who is Tomás?",   "¿Quién es Tomás?"),
        ["Quem é Isabela?"] = ("Who is Isabela?", "¿Quién es Isabela?"),
        ["Quem é Joana?"]   = ("Who is Joana?",   "¿Quién es Joana?"),
        ["Quem é Elias?"]   = ("Who is Elias?",   "¿Quién es Elias?"),
        ["Quem é Clara?"]   = ("Who is Clara?",   "¿Quién es Clara?"),
        ["Quem é Rui?"]     = ("Who is Rui?",     "¿Quién es Rui?"),
        ["Quem é Nara?"]    = ("Who is Nara?",    "¿Quién es Nara?"),
        ["Quem é Bento?"]   = ("Who is Bento?",   "¿Quién es Bento?"),
        ["Quem é Maren?"]   = ("Who is Maren?",   "¿Quién es Maren?"),
        ["A Forja"]         = ("The Forge",        "La Forja"),
        ["As Mãos"]         = ("The Hands",        "Las Manos"),
        ["O Sino"]          = ("The Bell",         "La Campana"),
        ["A Receita"]       = ("The Recipe",       "La Receta"),
        ["O Rio"]           = ("The River",        "El Río"),
        ["O Peixe que Escapou"] = ("The Fish That Got Away", "El Pez que Escapó"),
        ["O Barco do Pai"]  = ("Her Father's Boat", "El Barco del Padre"),
        ["A Aprendiz"]      = ("The Apprentice",   "La Aprendiz"),
        ["O Banco"]         = ("The Bench",        "El Banco"),
        ["As Histórias"]    = ("The Stories",      "Las Historias"),
        ["O Que Ele Viu"]   = ("What He Saw",      "Lo Que Vio"),
        ["A Madeira"]       = ("The Timber",       "La Madera"),
        ["A Casa Torta"]    = ("The Crooked House", "La Casa Torcida"),
        ["O Berço"]         = ("The Cradle",       "La Cuna"),
        ["A Estrada"]       = ("The Road",         "El Camino"),
        ["O Motivo"]        = ("The Reason",       "El Motivo"),
        ["A Decisão"]       = ("The Decision",     "La Decisión"),
        ["O Inverno Difícil"] = ("The Hard Winter", "El Invierno Difícil"),
        ["O Caderno"]       = ("The Notebook",     "El Cuaderno"),
        ["O Jardim"]        = ("The Garden",       "El Jardín"),
        ["A Planta que Falta"] = ("The Missing Plant", "La Planta que Falta"),
        ["O Rebanho"]       = ("The Flock",        "El Rebaño"),
        ["A Noite da Tempestade"] = ("The Night of the Storm", "La Noche de la Tormenta"),
        ["O Silêncio"]      = ("The Silence",      "El Silencio"),

        // ---- Lore bodies: Tomás ----
        ["O ferreiro. Aprendeu o ofício com o avô e nunca saiu do vale."] =
            ("The blacksmith. Learned the trade from his grandfather and never left the valley.",
             "El herrero. Aprendió el oficio de su abuelo y nunca salió del valle."),
        ["A forja dele é a mais antiga do vale. O fogo nunca apagou completamente em quarenta anos."] =
            ("His forge is the oldest in the valley. The fire hasn't gone fully out in forty years.",
             "Su forja es la más antigua del valle. El fuego no se ha apagado del todo en cuarenta años."),
        ["As cicatrizes nas mãos dele contam cada erro que cometeu aprendendo. Ele diz que são o diploma dele."] =
            ("The scars on his hands record every mistake he made learning. He calls them his diploma.",
             "Las cicatrices de sus manos cuentan cada error que cometió aprendiendo. Dice que son su diploma."),
        ["Tomás fundiu o sino da praça quando tinha vinte anos. Toda vez que toca, ele para o que está fazendo e escuta."] =
            ("Tomás cast the square's bell at twenty. Every time it rings he stops what he's doing and listens.",
             "Tomás fundió la campana de la plaza a los veinte. Cada vez que suena, deja lo que hace y escucha."),

        // ---- Lore bodies: Bento ----
        ["O mais velho daqui. Viu o vale mudar três vezes."] =
            ("The oldest man here. He's watched the valley change three times over.",
             "El más viejo de aquí. Ha visto el valle cambiar tres veces."),
        ["Conta a mesma história de jeitos diferentes, e todas as versões são boas."] =
            ("He tells the same story different ways, and every version is a good one.",
             "Cuenta la misma historia de formas distintas, y todas las versiones son buenas."),
        ["Estava aqui quando o vale quase secou. Diz que a terra se lembra disso."] =
            ("He was here when the valley nearly dried out. He says the land remembers it.",
             "Estaba aquí cuando el valle casi se secó. Dice que la tierra lo recuerda."),
        ["O banco onde ele senta foi feito pelo pai dele. É o último pedaço que restou da casa antiga."] =
            ("The bench he sits on was built by his father. It's the last piece left of the old house.",
             "El banco donde se sienta lo hizo su padre. Es lo último que queda de la casa vieja."),

        // ---- Lore bodies: Rui ----
        ["O carpinteiro. Construiu metade dos telhados do vale."] =
            ("The carpenter. He built half the roofs in the valley.",
             "El carpintero. Construyó la mitad de los tejados del valle."),
        ["Ele escolhe a madeira batendo nela e ouvindo. Diz que cada árvore soa diferente."] =
            ("He picks timber by knocking on it and listening. Says every tree sounds different.",
             "Elige la madera golpeándola y escuchando. Dice que cada árbol suena distinto."),
        ["A primeira casa que construiu saiu torta. Ela ainda está de pé, e ele ainda tem vergonha."] =
            ("The first house he built came out crooked. It's still standing, and he's still embarrassed.",
             "La primera casa que construyó salió torcida. Sigue en pie, y él sigue avergonzado."),
        ["Guarda um berço que fez para um filho que não chegou a nascer. Nunca conseguiu desmontar."] =
            ("He keeps a cradle he made for a child who was never born. He could never bring himself to take it apart.",
             "Guarda una cuna que hizo para un hijo que no llegó a nacer. Nunca pudo desarmarla."),

        // ---- Lore bodies: Nara ----
        ["Uma viajante. Ninguém sabe exatamente de onde ela veio."] =
            ("A traveller. Nobody knows exactly where she came from.",
             "Una viajera. Nadie sabe exactamente de dónde vino."),
        ["Ela fala de cidades que ninguém no vale nunca viu."] =
            ("She speaks of cities nobody in the valley has ever seen.",
             "Habla de ciudades que nadie en el valle ha visto jamás."),
        ["Não veio para cá por acaso. Está procurando alguém, mas não diz quem."] =
            ("She didn't come here by chance. She's looking for someone, but won't say who.",
             "No vino aquí por casualidad. Busca a alguien, pero no dice a quién."),
        ["Depois de tanto tempo na estrada, começou a considerar parar. Isso a assusta mais que viajar."] =
            ("After so long on the road she's started to consider stopping. That frightens her more than travelling.",
             "Tras tanto tiempo en el camino, ha empezado a pensar en parar. Eso la asusta más que viajar."),

        // ---- Lore bodies: Isabela ----
        ["A padeira do vale. O forno dela acende antes do sol."] =
            ("The valley's baker. Her oven is lit before the sun is.",
             "La panadera del valle. Su horno se enciende antes que el sol."),
        ["Ela usa a receita da avó, mas mudou uma coisa e não conta qual."] =
            ("She uses her grandmother's recipe, but changed one thing and won't say what.",
             "Usa la receta de su abuela, pero cambió algo y no dice qué."),
        ["Num inverno em que faltou farinha, ela assou pão para o vale inteiro com o que tinha guardado para si."] =
            ("One winter when flour ran short, she baked bread for the whole valley from her own stores.",
             "Un invierno en que faltó harina, horneó pan para todo el valle con lo que guardaba para sí."),
        ["Guarda um caderno de receitas que ninguém nunca leu. Diz que metade delas nunca deu certo."] =
            ("She keeps a recipe notebook nobody has ever read. Says half of them never worked.",
             "Guarda un cuaderno de recetas que nadie ha leído. Dice que la mitad nunca funcionaron."),

        // ---- Lore bodies: Joana ----
        ["A pescadora. Conhece cada curva do rio pelo som da água."] =
            ("The fisherwoman. She knows every bend of the river by the sound of the water.",
             "La pescadora. Conoce cada curva del río por el sonido del agua."),
        ["Ela diz que o rio muda de humor com as estações, e que dá para ouvir."] =
            ("She says the river changes mood with the seasons, and that you can hear it.",
             "Dice que el río cambia de humor con las estaciones, y que se puede oír."),
        ["Fala de um peixe enorme que escapou dela há dez anos. Ninguém sabe se é verdade."] =
            ("She talks about an enormous fish that got away ten years ago. Nobody knows if it's true.",
             "Habla de un pez enorme que se le escapó hace diez años. Nadie sabe si es cierto."),
        ["O barco que ela usa era do pai. Já trocou cada tábua, mas insiste que é o mesmo barco."] =
            ("The boat she uses was her father's. She's replaced every plank, but insists it's the same boat.",
             "El barco que usa era de su padre. Ha cambiado cada tabla, pero insiste en que es el mismo barco."),

        // ---- Lore bodies: Clara ----
        ["A herborista. Se você adoecer no vale, é a porta dela que você bate."] =
            ("The herbalist. If you fall ill in the valley, hers is the door you knock on.",
             "La herborista. Si enfermas en el valle, es su puerta la que tocas."),
        ["O jardim dela parece bagunçado, mas cada planta está exatamente onde ela quer."] =
            ("Her garden looks like a mess, but every plant is exactly where she wants it.",
             "Su jardín parece un desorden, pero cada planta está exactamente donde ella quiere."),
        ["Aprendeu com uma velha que morava na floresta e nunca teve nome, só apelido."] =
            ("She learned from an old woman who lived in the forest and never had a name, only a nickname.",
             "Aprendió de una anciana que vivía en el bosque y nunca tuvo nombre, solo apodo."),
        ["Procura há anos uma erva que só viu uma vez, quando criança. Não sabe se existiu mesmo."] =
            ("For years she's searched for a herb she saw only once as a child. She isn't sure it was ever real.",
             "Lleva años buscando una hierba que vio una sola vez de niña. No sabe si existió de verdad."),

        // ---- Lore bodies: Elias ----
        ["O pastor. Some no campo por dias e volta como se nada fosse."] =
            ("The shepherd. He vanishes into the fields for days and comes back as if nothing happened.",
             "El pastor. Desaparece en el campo durante días y vuelve como si nada."),
        ["Ele dá nome a cada animal do rebanho e nunca confunde nenhum."] =
            ("He names every animal in the flock and never mixes one up.",
             "Le pone nombre a cada animal del rebaño y nunca confunde a ninguno."),
        ["Passou uma noite inteira na chuva procurando uma ovelha perdida. Achou."] =
            ("He spent an entire night in the rain looking for a lost sheep. He found it.",
             "Pasó una noche entera bajo la lluvia buscando una oveja perdida. La encontró."),
        ["Elias gosta do silêncio do campo porque, segundo ele, é o único lugar onde consegue pensar direito."] =
            ("Elias likes the silence of the fields because, he says, it's the only place he can think straight.",
             "A Elias le gusta el silencio del campo porque, dice, es el único sitio donde puede pensar bien."),
    };

    /// <summary>
    /// Menu entry point. Deliberately does NOT show a modal dialog: this menu item is also
    /// invoked through automation (MCP execute_menu_item), and EditorUtility.DisplayDialog
    /// blocks the editor until a human clicks it, which hangs the calling session. The
    /// Console summary is the report.
    /// </summary>
    [MenuItem("Tools/NPC/Localize Codex Text (bio + lore)")]
    public static void LocalizeCodex()
    {
        LocalizeCodex(showDialogs: false);
    }

    /// <summary>
    /// <paramref name="showDialogs"/> must be false when called from automation:
    /// EditorUtility.DisplayDialog is modal and deadlocks a scripted caller.
    /// </summary>
    public static int LocalizeCodex(bool showDialogs)
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
        if (collection == null)
        {
            string msg = $"String table '{TableName}' not found — cannot localize the Codex.";
            if (showDialogs) EditorUtility.DisplayDialog("Table missing", msg, "OK");
            else Debug.LogError("[CodexLocalizationTool] " + msg);
            return 0;
        }

        var npcs = Object.FindObjectsByType<NPCDialogueInteractable>(FindObjectsSortMode.None);
        if (npcs.Length == 0)
        {
            Debug.LogWarning("[CodexLocalizationTool] No NPCs in the open scene. Open SampleScene first.");
            return 0;
        }

        int keysWritten = 0;

        foreach (var npc in npcs)
        {
            string npcId = npc.GetNPCId();
            if (string.IsNullOrEmpty(npcId)) continue;

            var so = new SerializedObject(npc);

            // ---- Bio ----
            var bioRaw = so.FindProperty("npcBio");
            if (bioRaw != null && !string.IsNullOrEmpty(bioRaw.stringValue))
            {
                string key = $"npc.{npcId}.bio";
                if (WriteEntry(collection, key, bioRaw.stringValue, out long id))
                {
                    BindLocalizedString(so.FindProperty("npcBioLocalized"), key, id);
                    keysWritten++;
                }
            }

            // ---- Lore tiers ----
            var loreProp = so.FindProperty("loreEntries");
            if (loreProp != null)
            {
                for (int i = 0; i < loreProp.arraySize; i++)
                {
                    var element = loreProp.GetArrayElementAtIndex(i);

                    var titleRaw = element.FindPropertyRelative("title");
                    if (titleRaw != null && !string.IsNullOrEmpty(titleRaw.stringValue))
                    {
                        string key = $"npc.{npcId}.lore{i}.title";
                        if (WriteEntry(collection, key, titleRaw.stringValue, out long id))
                        {
                            BindLocalizedString(element.FindPropertyRelative("titleLocalized"), key, id);
                            keysWritten++;
                        }
                    }

                    var bodyRaw = element.FindPropertyRelative("body");
                    if (bodyRaw != null && !string.IsNullOrEmpty(bodyRaw.stringValue))
                    {
                        string key = $"npc.{npcId}.lore{i}.body";
                        if (WriteEntry(collection, key, bodyRaw.stringValue, out long id))
                        {
                            BindLocalizedString(element.FindPropertyRelative("bodyLocalized"), key, id);
                            keysWritten++;
                        }
                    }
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(npc);
        }

        EditorUtility.SetDirty(collection.SharedData);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[CodexLocalizationTool] Wrote {keysWritten} keys across en/pt/es for " +
                  $"{npcs.Length} NPCs. Save the scene (Ctrl+S).");

        if (showDialogs)
        {
            EditorUtility.DisplayDialog("Codex localized",
                $"{keysWritten} keys written for {npcs.Length} NPCs.\n\nSave the scene (Ctrl+S).", "OK");
        }

        return keysWritten;
    }

    /// <summary>
    /// Source strings authored in English rather than Portuguese. Maren predates the villager
    /// cast and was written in English, so for her rows the dictionary key IS the en value and
    /// the tuple holds (pt, es) instead of (en, es).
    /// </summary>
    private static readonly HashSet<string> EnglishSourceKeys = new HashSet<string>
    {
        "Maren arrived in the valley after losing three harvests in a row to a devastating drought. She lost everything she had planted in the eastern valley, but found something different in this land - a fertility that feels almost blessed. She sells seeds, but what she truly sells is hope.",
        "Who is Maren?",
        "The Valley",
        "The Eastern Valley Drought",
        "Her Father's Seed",
        "Maren is the valley's seed merchant. She wakes before sunrise just to feel the soil between her fingers - there is a magic in that first moment when a seed opens.",
        "This valley has history. People come, plant, harvest and leave. Something here keeps you - the land, the rhythm of the seasons. They say a druid blessed the deepest layers of the soil.",
        "Maren lost three harvests in a row to a devastating drought in the eastern valley. She arrived with nothing, but found a land that welcomed her differently. Her mother used to say the seed that survives winter blooms stronger.",
        "Maren carries a mysterious seed found in her father's coat pocket the day he died. She never knew what plant it was - she never had the courage to plant it, afraid of losing the last thing she has of him.",
    };

    /// <summary>
    /// Writes one key into all three locales.
    ///
    /// Most source strings are Portuguese (the villager cast), but Maren's are English. The
    /// source string always becomes its own locale's value, and the tuple supplies the other
    /// two. When a string has no translation entry at all, every locale gets the source text —
    /// a missing translation should show *something* rather than a blank codex row.
    /// </summary>
    private static bool WriteEntry(StringTableCollection collection, string key,
                                   string source, out long keyId)
    {
        keyId = 0;

        var shared = collection.SharedData;
        var sharedEntry = shared.GetEntry(key) ?? shared.AddKey(key);
        if (sharedEntry == null) return false;

        keyId = sharedEntry.Id;

        bool hasTranslation = Translations.TryGetValue(source, out var t);
        if (!hasTranslation)
        {
            Debug.LogWarning($"[CodexLocalizationTool] No translation for \"{source}\" " +
                             $"({key}) — falling back to the source text in all locales.");
        }

        bool sourceIsEnglish = EnglishSourceKeys.Contains(source);

        foreach (var table in collection.StringTables)
        {
            string code = table.LocaleIdentifier.Code;
            string value;

            if (!hasTranslation)
            {
                value = source;
            }
            else if (sourceIsEnglish)
            {
                // Tuple is (pt, es); the key itself is the English text.
                if (code.StartsWith("pt"))      value = t.en;
                else if (code.StartsWith("es")) value = t.es;
                else                            value = source;
            }
            else
            {
                // Tuple is (en, es); the key itself is the Portuguese text.
                if (code.StartsWith("pt"))      value = source;
                else if (code.StartsWith("es")) value = t.es;
                else                            value = t.en;
            }

            table.AddEntry(key, value);
            EditorUtility.SetDirty(table);
        }

        return true;
    }

    /// <summary>
    /// Points a serialized LocalizedString at a table entry by key id.
    ///
    /// Binding by name alone leaves m_KeyId at 0, which resolves to nothing at runtime and
    /// renders an empty label — the exact failure VillagerDialogueFactory documents. The
    /// property paths below are LocalizedString's serialized layout.
    /// </summary>
    private static void BindLocalizedString(SerializedProperty localizedProp, string key, long keyId)
    {
        if (localizedProp == null) return;

        var tableName = localizedProp.FindPropertyRelative("m_TableReference.m_TableCollectionName");
        if (tableName != null) tableName.stringValue = TableName;

        var entryId = localizedProp.FindPropertyRelative("m_TableEntryReference.m_KeyId");
        if (entryId != null) entryId.longValue = keyId;

        var entryKey = localizedProp.FindPropertyRelative("m_TableEntryReference.m_Key");
        if (entryKey != null) entryKey.stringValue = string.Empty;
    }
}
} // namespace SowurShield.Editor
