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
/// Colocar e remover objetos no editor de mapa (2026-09-04).
///
/// A regra que faz tudo funcionar: o MapData guarda o CAMINHO do prefab, nao o
/// nome. O NPCPlacer que ja existia gravava `prefab.name` com um comentario
/// "For now, return the name" — e um nome nao resolve de volta, entao o objeto era
/// salvo e nunca reaparecia ao carregar o mapa.
/// </summary>
public class ObjectPlacerTests
{
    private static readonly BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

    private GameObject host;
    private GameObject gridHost;
    private RuntimeMapEditor editor;
    private ObjectPlacer placer;

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

        host = new GameObject("ObjectPlacerTestHost");
        editor = host.AddComponent<RuntimeMapEditor>();
        placer = host.AddComponent<ObjectPlacer>();

        typeof(RuntimeMapEditor).GetField("dualGridTilemap", Priv).SetValue(editor, dual);
        typeof(RuntimeMapEditor).GetField("currentMapData", Priv)
            .SetValue(editor, ScriptableObject.CreateInstance<MapData>());
        typeof(ObjectPlacer).GetMethod("Start", Priv).Invoke(placer, null);
    }

    [TearDown]
    public void TearDown()
    {
        var raiz = GameObject.Find(RuntimeMapEditor.RaizDeObjetosDoMapa);
        if (raiz != null) Object.DestroyImmediate(raiz);
        if (host != null) Object.DestroyImmediate(host);
        if (gridHost != null) Object.DestroyImmediate(gridHost);
    }

    /// <summary>
    /// O primeiro prefab de CENARIO do catalogo.
    ///
    /// Desde que a paleta passou a oferecer pessoas (2026-09-05), `Tudo()[0]` e um NPC —
    /// que tem escala natural 1 e por isso fazia o teste de escala auto-ignorar-se em vez
    /// de testar. O ObjectPlacer trata de cenario, entao o helper tem de devolver cenario.
    /// </summary>
    private string PrimeiroPrefab()
    {
        foreach (var e in PrefabCatalog.Tudo())
            if (!PrefabCatalog.EhNPC(e)) return e.Caminho;

        Assert.Ignore("Sem prefabs de cenario no catalogo.");
        return null;
    }

    private void Colocar(Vector3 posicao) =>
        typeof(ObjectPlacer).GetMethod("Colocar", Priv).Invoke(placer, new object[] { posicao });

    private void RemoverEm(Vector3 posicao) =>
        typeof(ObjectPlacer).GetMethod("RemoverEm", Priv).Invoke(placer, new object[] { posicao });

    /// <summary>
    /// O contrato central: o que fica gravado tem que resolver de volta. Se isto
    /// falhar, o mapa salva objetos que nunca reaparecem — sem erro nenhum.
    /// </summary>
    [Test]
    public void Colocar_GravaOCaminhoQueResolveDeVolta()
    {
        placer.Selecionar(PrimeiroPrefab());
        Colocar(new Vector3(3.5f, 4.5f, 0f));

        Assert.AreEqual(1, editor.CurrentMapData.objectSpawns.Count);

        var gravado = editor.CurrentMapData.objectSpawns[0];
        Assert.IsNotNull(PrefabCatalog.Resolver(gravado.prefabPath),
            "O prefabPath gravado tem que resolver para um prefab. Gravar o NOME em " +
            "vez do caminho faz o objeto sumir ao carregar o mapa.");
    }

    [Test]
    public void SelecionarNull_DesligaOModoEDevolveOCliqueAoPincel()
    {
        placer.Selecionar(PrimeiroPrefab());
        Assert.IsTrue(placer.ModoColocacao);

        placer.Selecionar(null);

        Assert.IsFalse(placer.ModoColocacao,
            "Sem objeto escolhido o clique volta a pintar chao.");
        Assert.IsNull(placer.CaminhoSelecionado);
    }

    [Test]
    public void FecharOEditor_LargaOObjetoSelecionado()
    {
        placer.Selecionar(PrimeiroPrefab());
        editor.SetEditorMode(false);

        Assert.IsFalse(placer.ModoColocacao,
            "Voltar ao editor com um prefab ainda na mao colocaria arvores sem querer.");
    }

    /// <summary>
    /// A regressao que o teste manual pegou: uma versao anterior guardava os objetos
    /// numa lista propria. Carregar um mapa destroi e recria a raiz, entao a lista
    /// virava referencias nulas e o botao direito so removia o que se acabou de por.
    /// </summary>
    [Test]
    public void Remover_FuncionaEmObjetoQueVeioDeUmMapaCarregado()
    {
        placer.Selecionar(PrimeiroPrefab());
        Colocar(new Vector3(5.5f, 5.5f, 0f));

        // Simula um load: a raiz e destruida e recriada do MapData.
        typeof(RuntimeMapEditor).GetMethod("RecriarObjetos", Priv).Invoke(editor, null);
        Assert.AreEqual(1, editor.CurrentMapData.objectSpawns.Count);

        RemoverEm(new Vector3(5.5f, 5.5f, 0f));

        Assert.AreEqual(0, editor.CurrentMapData.objectSpawns.Count,
            "O botao direito tem que remover tambem o que veio de um mapa salvo.");
    }

    /// <summary>
    /// O objeto e encaixado no centro da celula, entao clicar em qualquer ponto dela
    /// tem que pegar — exigir precisao de pixel seria frustrante.
    /// </summary>
    [Test]
    public void Remover_ToleraCliqueForaDoCentroExato()
    {
        placer.Selecionar(PrimeiroPrefab());
        Colocar(new Vector3(2.5f, 2.5f, 0f));

        RemoverEm(new Vector3(2.78f, 2.22f, 0f));

        Assert.AreEqual(0, editor.CurrentMapData.objectSpawns.Count);
    }

    [Test]
    public void Remover_ForaDeQualquerObjeto_NaoApagaNada()
    {
        placer.Selecionar(PrimeiroPrefab());
        Colocar(new Vector3(1.5f, 1.5f, 0f));

        RemoverEm(new Vector3(40.5f, 40.5f, 0f));

        Assert.AreEqual(1, editor.CurrentMapData.objectSpawns.Count,
            "Clicar longe nao pode apagar o objeto que esta noutro canto.");
    }

    [Test]
    public void ObjetosColocados_FicamSobUmaRaizUnica()
    {
        placer.Selecionar(PrimeiroPrefab());
        Colocar(new Vector3(1.5f, 1.5f, 0f));
        Colocar(new Vector3(2.5f, 1.5f, 0f));

        var raiz = GameObject.Find(RuntimeMapEditor.RaizDeObjetosDoMapa);
        Assert.IsNotNull(raiz,
            "Sem um pai unico, recarregar o mapa empilha copias soltas pela hierarquia " +
            "e nao ha como limpar as anteriores.");
        Assert.AreEqual(2, raiz.transform.childCount);
    }

    /// <summary>
    /// O pincel e a colocacao disputam o mesmo botao esquerdo. Sem exclusao, um
    /// clique colocaria uma arvore E pintaria terra debaixo dela.
    /// </summary>
    [Test]
    public void BrushTool_RecuaQuandoHaObjetoSelecionado()
    {
        var fonte = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "Scripts/MapEditor/BrushTool.cs"));

        Assert.IsTrue(fonte.Contains("objectPlacer.ModoColocacao"),
            "O pincel precisa consultar o ObjectPlacer antes de pintar.");

        int guarda = fonte.IndexOf("objectPlacer.ModoColocacao");
        int pintura = fonte.IndexOf("switch (currentBrushType)");
        Assert.Less(guarda, pintura, "A guarda vem antes de escolher o brush.");
    }

    /// <summary>
    /// O NPCPlacer gravava `prefab.name`. Um nome nao resolve de volta.
    /// </summary>
    [Test]
    public void NPCPlacer_GravaCaminhoNaoNome()
    {
        var fonte = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "Scripts/MapEditor/NPCPlacer.cs"));

        Assert.IsTrue(fonte.Contains("AssetDatabase.GetAssetPath"),
            "GetPrefabPath tem que devolver o caminho do asset.");
        Assert.IsFalse(fonte.Contains("// For now, return the name"),
            "O atalho que devolvia o nome fazia o NPC sumir ao carregar o mapa.");
    }

    // =====================================================================
    // Escala (2026-09-05)
    //
    // O projeto mistura cinco densidades de pixel (16, 32, 100, 256) e a escala
    // do mundo e PPU 16. Cada prefab da paleta carrega no proprio localScale o
    // fator que o traz para essa escala. O placer ATRIBUIA o multiplicador cru
    // por cima desse fator, entao 30 dos 57 itens nasciam entre 4x e 18x menores
    // do que deviam -- uma flor media 0,11 unidade, um pixel no chao.
    // =====================================================================

    /// <summary>
    /// O contrato: colocar com multiplicador 1 da o tamanho natural do prefab,
    /// nao escala 1. Este teste falha (0,18un em vez de 0,99un) se alguem voltar
    /// a atribuir o multiplicador em vez de multiplicar por ele.
    /// </summary>
    [Test]
    public void Colocar_PreservaAEscalaNaturalDoPrefab()
    {
        var caminho = PrimeiroPrefab();
        var prefab = PrefabCatalog.Resolver(caminho);
        if (prefab == null) Assert.Ignore("Prefab do catalogo nao resolveu.");

        var natural = prefab.transform.localScale;
        if (Mathf.Approximately(natural.x, 1f))
            Assert.Ignore("Este prefab tem escala natural 1; o teste nao distingue nada.");

        placer.Selecionar(caminho);
        placer.DefinirEscala(1f);
        Colocar(new Vector3(2.5f, 2.5f, 0f));

        var gravado = editor.CurrentMapData.objectSpawns[0];
        Assert.AreEqual(natural.x, Mathf.Abs(gravado.scale.x), 0.001f,
            "Com multiplicador 1 a escala gravada tem que ser a natural do prefab. " +
            "Atribuir o multiplicador cru faz o objeto nascer varias vezes menor.");
    }

    /// <summary>O multiplicador continua significando o que diz: 2x e o dobro.</summary>
    [Test]
    public void Colocar_MultiplicaAEscalaNaturalPeloValorEscolhido()
    {
        var caminho = PrimeiroPrefab();
        var prefab = PrefabCatalog.Resolver(caminho);
        if (prefab == null) Assert.Ignore("Prefab do catalogo nao resolveu.");
        var natural = prefab.transform.localScale.x;

        placer.Selecionar(caminho);
        placer.DefinirEscala(2f);
        Colocar(new Vector3(2.5f, 2.5f, 0f));

        Assert.AreEqual(natural * 2f,
            Mathf.Abs(editor.CurrentMapData.objectSpawns[0].scale.x), 0.001f,
            "2x tem que ser o dobro do tamanho natural.");
    }

    /// <summary>O espelho vira a arte sem mudar o tamanho.</summary>
    [Test]
    public void Espelhar_InverteOSinalSemMudarOTamanho()
    {
        var caminho = PrimeiroPrefab();
        var prefab = PrefabCatalog.Resolver(caminho);
        if (prefab == null) Assert.Ignore("Prefab do catalogo nao resolveu.");
        var natural = prefab.transform.localScale.x;

        placer.Selecionar(caminho);
        placer.DefinirEscala(1f);
        placer.AlternarEspelho();
        Colocar(new Vector3(2.5f, 2.5f, 0f));

        var escala = editor.CurrentMapData.objectSpawns[0].scale;
        Assert.Less(escala.x, 0f, "Espelhado grava X negativo.");
        Assert.AreEqual(natural, Mathf.Abs(escala.x), 0.001f,
            "Espelhar vira a arte, nao encolhe.");
        Assert.AreEqual(natural, escala.y, 0.001f, "O Y nunca e espelhado.");
    }

    /// <summary>
    /// Um mapa salvo antes de 2026-09-05 gravou exatamente (1,1,1), de quando todo
    /// prefab tinha escala 1. Usar esse valor hoje reintroduz o defeito vindo do
    /// arquivo, entao o loader cai na escala do proprio prefab.
    /// </summary>
    [Test]
    public void EscalaDe_MapaAntigoComEscalaUm_CaiNaEscalaDoPrefab()
    {
        var prefab = new GameObject("PrefabDeTeste");
        prefab.transform.localScale = new Vector3(5.5f, 5.5f, 1f);

        var antigo = new ObjectSpawnData { scale = Vector3.one };
        Assert.AreEqual(5.5f, MapRuntimeLoader.EscalaDe(antigo, prefab).x, 0.001f,
            "(1,1,1) e a marca de mapa antigo: vale a escala natural do prefab.");

        var zerado = new ObjectSpawnData { scale = Vector3.zero };
        Assert.AreEqual(5.5f, MapRuntimeLoader.EscalaDe(zerado, prefab).x, 0.001f,
            "Escala zero deixaria o objeto invisivel; tambem cai no prefab.");

        var novo = new ObjectSpawnData { scale = new Vector3(11f, 11f, 1f) };
        Assert.AreEqual(11f, MapRuntimeLoader.EscalaDe(novo, prefab).x, 0.001f,
            "Mapa novo ja grava a escala absoluta: vale o que esta no arquivo.");

        Object.DestroyImmediate(prefab);
    }

    /// <summary>
    /// A paleta so serve se os itens dela aparecerem no jogo. Um prefab de escala
    /// natural 1 num tileset PPU 100 mede 0,16 unidade -- invisivel ao lado do
    /// jogador, que tem 0,84.
    /// </summary>
    [Test]
    public void TodoPrefabDaPaleta_TemTamanhoVisivelNoMundo()
    {
        const float MinimoVisivel = 0.35f;
        var pequenos = new System.Collections.Generic.List<string>();

        foreach (var entrada in PrefabCatalog.Tudo())
        {
            var prefab = PrefabCatalog.Resolver(entrada.Caminho);
            if (prefab == null) continue;
            var sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null || sr.sprite == null) continue;

            float maior = Mathf.Max(sr.sprite.rect.width, sr.sprite.rect.height)
                          / sr.sprite.pixelsPerUnit * prefab.transform.localScale.x;
            if (maior < MinimoVisivel)
                pequenos.Add($"{entrada.Nome} ({maior:0.00}un)");
        }

        Assert.IsEmpty(pequenos,
            "Estes prefabs nascem menores que " + MinimoVisivel + " unidade e somem no chao: "
            + string.Join(", ", pequenos));
    }
}

}
