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

    private string PrimeiroPrefab()
    {
        var tudo = PrefabCatalog.Tudo();
        if (tudo.Count == 0) Assert.Ignore("Sem prefabs no catalogo.");
        return tudo[0].Caminho;
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
}

}
