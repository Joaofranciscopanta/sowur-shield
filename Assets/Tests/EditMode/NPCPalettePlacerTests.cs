using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using SowurShield.Farming;
using SowurShield.MapEditor;

namespace SowurShield.Tests
{

/// <summary>
/// Colocar pessoas pela paleta do editor de mapa (2026-09-05).
///
/// A regra que define o desenho: <b>um NPC nao se duplica</b>. As nove personagens do jogo
/// sao unicas e o `npcId` indexa a memoria de conversa e o relacionamento, entao duas
/// Joanas partilhariam estado e as duas ficariam erradas. Colocar uma personagem que ja
/// esta no mapa MOVE a que existe.
///
/// O NPC generico e a excecao deliberada: nao tem `npcId`, entao cada clique cria um novo
/// — e assim que se povoa um mapa com gente nova.
/// </summary>
public class NPCPalettePlacerTests
{
    private static readonly BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

    private GameObject host;
    private GameObject gridHost;
    private RuntimeMapEditor editor;
    private NPCPalettePlacer placer;

    [SetUp]
    public void SetUp()
    {
        gridHost = new GameObject("Grid");
        var placeholderGO = new GameObject("PlaceholderTilemap");
        placeholderGO.transform.SetParent(gridHost.transform);
        placeholderGO.AddComponent<Grid>();
        var placeholder = placeholderGO.AddComponent<Tilemap>();

        var dual = gridHost.AddComponent<DualGridTilemap>();
        dual.placeholderTilemap = placeholder;

        host = new GameObject("NPCPlacerTestHost");
        editor = host.AddComponent<RuntimeMapEditor>();
        placer = host.AddComponent<NPCPalettePlacer>();

        typeof(RuntimeMapEditor).GetField("dualGridTilemap", Priv).SetValue(editor, dual);
        typeof(RuntimeMapEditor).GetField("currentMapData", Priv)
            .SetValue(editor, ScriptableObject.CreateInstance<MapData>());
        typeof(NPCPalettePlacer).GetMethod("Start", Priv).Invoke(placer, null);
    }

    [TearDown]
    public void TearDown()
    {
        var raiz = GameObject.Find(RuntimeMapEditor.RaizDeObjetosDoMapa);
        if (raiz != null) Object.DestroyImmediate(raiz);
        if (host != null) Object.DestroyImmediate(host);
        if (gridHost != null) Object.DestroyImmediate(gridHost);

        // Um NPC solto que sobreviva ao teste faz o SEGUINTE achar que a personagem "ja
        // existe na cena" e mover em vez de criar -- um teste passa e o outro falha
        // conforme a ordem em que correm.
        foreach (var npc in Object.FindObjectsByType<SowurShield.Dialogue.NPCDialogueInteractable>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (npc != null) Object.DestroyImmediate(npc.gameObject);
        }
    }

    private void Colocar(Vector3 posicao) =>
        typeof(NPCPalettePlacer).GetMethod("Colocar", Priv)
            .Invoke(placer, new object[] { posicao });

    /// <summary>Caminho de um NPC com personagem (tem npcId), ou Ignore se nao houver.</summary>
    private static string CaminhoDeUmaPersonagem()
    {
        foreach (var e in PrefabCatalog.Tudo())
        {
            if (!PrefabCatalog.EhNPC(e)) continue;
            var pf = PrefabCatalog.Resolver(e.Caminho);
            if (pf != null && !string.IsNullOrEmpty(NPCPalettePlacer.IdDoPrefab(pf)))
                return e.Caminho;
        }
        Assert.Ignore("Sem prefabs de personagem em Resources/Prefabs/NPCs.");
        return null;
    }

    private static string CaminhoDoGenerico()
    {
        foreach (var e in PrefabCatalog.Tudo())
        {
            if (!PrefabCatalog.EhNPC(e)) continue;
            var pf = PrefabCatalog.Resolver(e.Caminho);
            if (pf != null && string.IsNullOrEmpty(NPCPalettePlacer.IdDoPrefab(pf)))
                return e.Caminho;
        }
        Assert.Ignore("Sem NPC generico (prefab de NPC sem npcId) em Resources/Prefabs/NPCs.");
        return null;
    }

    /// <summary>
    /// O catalogo tem de VER as pessoas. Ate 2026-09-05 so varria pastas de cenario, entao
    /// nao havia como por gente num mapa — a paleta oferecia arvores e pedras para um
    /// mundo permanentemente vazio.
    /// </summary>
    [Test]
    public void OCatalogo_OfereceAsPessoas()
    {
        var pessoas = PrefabCatalog.Tudo().Where(PrefabCatalog.EhNPC).ToList();
        Assert.IsNotEmpty(pessoas,
            "A paleta tem que oferecer NPCs; sem isto o editor so monta cenario vazio.");
    }

    /// <summary>As pessoas vem antes do cenario: sao o que se procura primeiro.</summary>
    [Test]
    public void AsPessoas_VemAntesDoCenarioNaLista()
    {
        var tudo = PrefabCatalog.Tudo();
        int primeiraPessoa = -1, primeiroCenario = -1;
        for (int i = 0; i < tudo.Count; i++)
        {
            bool pessoa = PrefabCatalog.EhNPC(tudo[i]);
            if (pessoa && primeiraPessoa < 0) primeiraPessoa = i;
            if (!pessoa && primeiroCenario < 0) primeiroCenario = i;
        }
        if (primeiraPessoa < 0 || primeiroCenario < 0) Assert.Ignore("Falta uma das categorias.");

        Assert.Less(primeiraPessoa, primeiroCenario,
            "Ordenar por nome de categoria poria Decorations antes de NPCs e desfazia a " +
            "ordem escolhida na lista de pastas.");
    }

    /// <summary>
    /// O contrato central. Colocar a mesma personagem duas vezes tem que MOVER, nunca
    /// duplicar: duas copias partilhariam npcId, e com ele a memoria de conversa e o
    /// relacionamento.
    /// </summary>
    [Test]
    public void ColocarAMesmaPersonagemDuasVezes_MoveEmVezDeDuplicar()
    {
        placer.Selecionar(CaminhoDeUmaPersonagem());

        Colocar(new Vector3(2.5f, 2.5f, 0f));
        Assert.AreEqual(1, editor.CurrentMapData.npcSpawns.Count);

        Colocar(new Vector3(8.5f, 6.5f, 0f));

        Assert.AreEqual(1, editor.CurrentMapData.npcSpawns.Count,
            "A segunda colocacao tem que mover a personagem, nao criar outra.");
        Assert.AreEqual(new Vector3(8.5f, 6.5f, 0f), editor.CurrentMapData.npcSpawns[0].position,
            "E a posicao gravada tem que ser a nova.");
    }

    /// <summary>
    /// A cena, e nao so o MapData, decide quem ja existe.
    ///
    /// As nove personagens estao soltas na SampleScene e nao no `npcSpawns`, entao num
    /// mapa novo (lista vazia) olhar so o MapData dava "nao existe" e o placer instanciava
    /// uma segunda Joana ao lado da que o jogador ja via. Achado rodando, nao pelos
    /// testes: o npcSpawns dizia 1 enquanto a cena tinha 2.
    /// </summary>
    [Test]
    public void PersonagemJaNaCena_EMovidaEmVezDeInstanciada()
    {
        string caminho = CaminhoDeUmaPersonagem();
        var prefab = PrefabCatalog.Resolver(caminho);
        string id = NPCPalettePlacer.IdDoPrefab(prefab);

        // Uma instancia solta na cena, como as nove do jogo -- fora da raiz do mapa.
        var jaExiste = Object.Instantiate(prefab, new Vector3(7f, -3f, 0f), Quaternion.identity);
        jaExiste.name = "PersonagemDaCena";

        try
        {
            placer.Selecionar(caminho);
            Colocar(new Vector3(1.5f, 1.5f, 0f));

            int quantas = Object.FindObjectsByType<SowurShield.Dialogue.NPCDialogueInteractable>(
                              FindObjectsSortMode.None)
                          .Count(n => n.GetNPCId() == id);

            Assert.AreEqual(1, quantas,
                "So pode haver uma copia da personagem: a que a cena ja trazia tem que ser " +
                "movida, nao acompanhada de uma segunda.");
            Assert.AreEqual(new Vector3(1.5f, 1.5f, 0f), jaExiste.transform.position,
                "E a que existia e que tem que se mover.");
        }
        finally
        {
            if (jaExiste != null) Object.DestroyImmediate(jaExiste);
        }
    }

    /// <summary>O generico e a excecao: sem npcId, cada clique povoa mais um.</summary>
    [Test]
    public void ColocarOGenericoDuasVezes_CriaDois()
    {
        placer.Selecionar(CaminhoDoGenerico());

        Colocar(new Vector3(1.5f, 1.5f, 0f));
        Colocar(new Vector3(4.5f, 4.5f, 0f));

        Assert.AreEqual(2, editor.CurrentMapData.npcSpawns.Count,
            "O NPC generico nao tem npcId, entao nada o identifica como repetido: cada " +
            "clique cria um novo, que e como se povoa um mapa.");
    }

    /// <summary>
    /// Cada generico tem que nascer com um NOME proprio.
    ///
    /// Quando o npcId esta em branco, o jogo gera um a partir do nome do GameObject
    /// (NPCDialogueInteractable.InitializeNPC). Dois genericos chamados "NPC_Novo" viravam
    /// dois `npc_NPC_Novo` -- o mesmo id partilhado que a regra de nao-duplicar existe
    /// para evitar, so que entrando pela porta dos fundos. Achado rodando.
    /// </summary>
    [Test]
    public void CadaGenericoColocado_RecebeUmNomeProprio()
    {
        placer.Selecionar(CaminhoDoGenerico());

        Colocar(new Vector3(1.5f, 1.5f, 0f));
        Colocar(new Vector3(4.5f, 1.5f, 0f));
        Colocar(new Vector3(7.5f, 1.5f, 0f));

        var nomes = editor.CurrentMapData.npcSpawns.Select(n => n.npcName).ToList();
        Assert.AreEqual(nomes.Count, nomes.Distinct().Count(),
            "Nomes repetidos viram ids repetidos: " + string.Join(", ", nomes));

        var naCena = Object.FindObjectsByType<SowurShield.Dialogue.NPCDialogueInteractable>(
                         FindObjectsSortMode.None).Select(n => n.gameObject.name).ToList();
        Assert.AreEqual(naCena.Count, naCena.Distinct().Count(),
            "E na cena tambem: " + string.Join(", ", naCena));
    }

    /// <summary>
    /// O que fica gravado tem que resolver de volta, senao o NPC e salvo e nunca reaparece
    /// — foi exatamente esse o defeito do NPCPlacer antigo, que gravava `prefab.name`.
    /// </summary>
    [Test]
    public void Colocar_GravaOCaminhoQueResolveDeVolta()
    {
        placer.Selecionar(CaminhoDeUmaPersonagem());
        Colocar(new Vector3(3.5f, 3.5f, 0f));

        var gravado = editor.CurrentMapData.npcSpawns[0];
        Assert.IsNotNull(PrefabCatalog.Resolver(gravado.npcPrefabPath),
            "O npcPrefabPath gravado tem que resolver para um prefab.");
        Assert.IsNotEmpty(gravado.npcId,
            "Uma personagem tem que gravar o seu npcId: e ele que evita a duplicacao ao " +
            "carregar o mapa.");
    }

    /// <summary>
    /// Todo prefab de NPC tem que estar sob Resources/, senao existe no Editor e some no
    /// jogo — a consequencia permanente da opcao B (o jogo carrega por Resources.Load).
    /// </summary>
    [Test]
    public void TodoPrefabDeNPC_ResolvePorResources()
    {
        var fora = new System.Collections.Generic.List<string>();
        foreach (var e in PrefabCatalog.Tudo())
        {
            if (!PrefabCatalog.EhNPC(e)) continue;
            if (!e.Caminho.Contains("/Resources/")) fora.Add(e.Nome);
        }

        Assert.IsEmpty(fora,
            "Estes NPCs nao estao sob Resources/ e sumiriam no jogo: " + string.Join(", ", fora));
    }

    /// <summary>Escolher o pincel (null) larga o NPC, senao o clique colocava gente.</summary>
    [Test]
    public void SelecionarNull_DesligaOModo()
    {
        placer.Selecionar(CaminhoDeUmaPersonagem());
        Assert.IsTrue(placer.ModoColocacao);

        placer.Selecionar(null);
        Assert.IsFalse(placer.ModoColocacao);
    }
}

}
