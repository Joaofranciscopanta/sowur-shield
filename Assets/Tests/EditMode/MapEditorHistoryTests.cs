using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using SowurShield.Farming;
using SowurShield.MapEditor;

namespace SowurShield.Tests
{

/// <summary>
/// Desfazer e refazer no editor de mapa (2026-09-03).
///
/// A regra que define o desenho: um passo e um GESTO do usuario — um clique, um
/// arrasto inteiro, um balde — e nao cada celula. Desfazer um retangulo de 30
/// celulas tem que voltar as 30 de uma vez; um Ctrl+Z por celula seria inutilizavel.
/// </summary>
public class MapEditorHistoryTests
{
    private static readonly BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

    private GameObject host;
    private GameObject gridHost;
    private RuntimeMapEditor editor;
    private MapEditorHistory historico;
    private DualGridTilemap dual;
    private Tilemap placeholder;
    private Tile tileDeTerra;

    [SetUp]
    public void SetUp()
    {
        gridHost = new GameObject("Grid");
        var placeholderGO = new GameObject("PlaceholderTilemap");
        placeholderGO.transform.SetParent(gridHost.transform);
        placeholderGO.AddComponent<Grid>();
        placeholder = placeholderGO.AddComponent<Tilemap>();

        var displayGO = new GameObject("DisplayTilemap");
        displayGO.transform.SetParent(gridHost.transform);
        displayGO.AddComponent<Grid>();
        var display = displayGO.AddComponent<Tilemap>();

        dual = gridHost.AddComponent<DualGridTilemap>();
        dual.placeholderTilemap = placeholder;
        dual.displayTilemap = display;
        tileDeTerra = ScriptableObject.CreateInstance<Tile>();
        dual.grassPlaceholderTile = tileDeTerra;
        dual.dirtPlaceholderTile = ScriptableObject.CreateInstance<Tile>();
        dual.tiles = new Tile[16];
        for (int i = 0; i < 16; i++) dual.tiles[i] = ScriptableObject.CreateInstance<Tile>();

        host = new GameObject("MapEditorTestHost");
        editor = host.AddComponent<RuntimeMapEditor>();
        historico = host.AddComponent<MapEditorHistory>();

        typeof(RuntimeMapEditor).GetField("dualGridTilemap", Priv).SetValue(editor, dual);
        typeof(RuntimeMapEditor).GetField("currentMapData", Priv)
            .SetValue(editor, ScriptableObject.CreateInstance<MapData>());
        typeof(MapEditorHistory).GetMethod("Start", Priv).Invoke(historico, null);
    }

    [TearDown]
    public void TearDown()
    {
        if (host != null) Object.DestroyImmediate(host);
        if (gridHost != null) Object.DestroyImmediate(gridHost);
    }

    private bool TemTerra(Vector3Int c) => placeholder.GetTile(c) == tileDeTerra;

    private void Gesto(params Vector3Int[] celulas)
    {
        historico.IniciarPasso();
        foreach (var c in celulas) editor.SetTileAtPosition(c, ExtendedTileType.Dirt);
        historico.FinalizarPasso();
    }

    /// <summary>
    /// O comportamento central: um gesto e uma unidade de desfazer.
    /// </summary>
    [Test]
    public void UmGestoDeVariasCelulas_DesfazTudoDeUmaVez()
    {
        var celulas = new[]
        {
            new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0),
            new Vector3Int(2, 0, 0), new Vector3Int(3, 0, 0)
        };
        Gesto(celulas);
        foreach (var c in celulas) Assert.IsTrue(TemTerra(c), "O gesto pintou " + c);

        Assert.AreEqual(1, historico.PassosParaDesfazer,
            "Quatro celulas num gesto so sao UM passo de desfazer.");

        Assert.IsTrue(historico.Desfazer());
        foreach (var c in celulas)
            Assert.IsFalse(TemTerra(c), "Um Ctrl+Z tem que limpar o gesto inteiro.");
    }

    [Test]
    public void Refazer_DevolveOQueFoiDesfeito()
    {
        var celula = new Vector3Int(4, 4, 0);
        Gesto(celula);

        historico.Desfazer();
        Assert.IsFalse(TemTerra(celula));

        Assert.IsTrue(historico.Refazer());
        Assert.IsTrue(TemTerra(celula), "Ctrl+Y tem que repintar.");
    }

    [Test]
    public void GestosSeguidos_DesfazemNaOrdemInversa()
    {
        var primeira = new Vector3Int(1, 1, 0);
        var segunda = new Vector3Int(2, 2, 0);
        Gesto(primeira);
        Gesto(segunda);

        historico.Desfazer();
        Assert.IsFalse(TemTerra(segunda), "O ultimo gesto sai primeiro.");
        Assert.IsTrue(TemTerra(primeira), "O anterior fica.");

        historico.Desfazer();
        Assert.IsFalse(TemTerra(primeira));
    }

    /// <summary>
    /// Pintar terra sobre terra nao muda nada. Sem este filtro, um arrasto sobre
    /// area ja pintada encheria o historico de passos que nao desfazem nada — e o
    /// Ctrl+Z pareceria travado.
    /// </summary>
    [Test]
    public void PintarSobreOMesmoTipo_NaoGeraPasso()
    {
        var celula = new Vector3Int(5, 5, 0);
        Gesto(celula);
        Assert.AreEqual(1, historico.PassosParaDesfazer);

        Gesto(celula);   // de novo, mesmo tipo
        Assert.AreEqual(1, historico.PassosParaDesfazer,
            "Repintar o mesmo tipo nao muda nada, entao nao e um passo.");
    }

    /// <summary>
    /// Comportamento padrao de qualquer editor: agir depois de desfazer abandona o
    /// caminho de refazer. Manter os dois daria um estado impossivel.
    /// </summary>
    [Test]
    public void AcaoNova_InvalidaORefazer()
    {
        Gesto(new Vector3Int(1, 0, 0));
        historico.Desfazer();
        Assert.AreEqual(1, historico.PassosParaRefazer);

        Gesto(new Vector3Int(9, 9, 0));

        Assert.AreEqual(0, historico.PassosParaRefazer,
            "Uma acao nova apaga o futuro que o refazer guardava.");
    }

    [Test]
    public void SemNada_DesfazerERefazerNaoLancam()
    {
        Assert.IsFalse(historico.Desfazer(), "Nada a desfazer.");
        Assert.IsFalse(historico.Refazer(), "Nada a refazer.");
    }

    /// <summary>
    /// O desfazer nao pode virar historico novo, senao o proximo Ctrl+Z desfaria o
    /// proprio desfazer e o usuario ficaria preso alternando entre dois estados.
    /// </summary>
    [Test]
    public void Desfazer_NaoSeRegistraComoPassoNovo()
    {
        Gesto(new Vector3Int(3, 3, 0));
        historico.Desfazer();

        Assert.AreEqual(0, historico.PassosParaDesfazer,
            "Aplicar um desfazer nao pode empilhar um passo novo.");
    }

    [Test]
    public void FecharOEditor_LimpaOHistorico()
    {
        Gesto(new Vector3Int(2, 2, 0));
        Assert.AreEqual(1, historico.PassosParaDesfazer);

        editor.SetEditorMode(false);

        Assert.AreEqual(0, historico.PassosParaDesfazer,
            "O historico e da sessao: Ctrl+Z nao pode desfazer algo de uma sessao " +
            "anterior, que o usuario nem lembra de ter feito.");
    }
}

/// <summary>
/// O catalogo que resolve `prefabPath` de volta para um prefab — a peca que faltava
/// para os objetos gravados num mapa reaparecerem ao carregar.
/// </summary>
public class PrefabCatalogTests
{
    [Test]
    public void Catalogo_EncontraPrefabsDoProjeto()
    {
        var tudo = PrefabCatalog.Tudo();
        Assert.Greater(tudo.Count, 0,
            "O projeto tem 34 prefabs so em Assets/Prefabs/Decorations; um catalogo " +
            "vazio significa que as pastas varridas estao erradas.");
    }

    [Test]
    public void Resolver_DevolveOPrefabDoCaminhoGravado()
    {
        var tudo = PrefabCatalog.Tudo();
        if (tudo.Count == 0) Assert.Ignore("Sem prefabs para testar.");

        var primeiro = tudo[0];
        var prefab = PrefabCatalog.Resolver(primeiro.Caminho);

        Assert.IsNotNull(prefab, "O caminho do catalogo tem que resolver.");
        Assert.AreEqual(primeiro.Nome, prefab.name);
    }

    /// <summary>
    /// Um prefab movido ou apagado desde que o mapa foi salvo devolve null — e
    /// quem chama tem que poder detectar isso em vez de instanciar nada.
    /// </summary>
    [Test]
    public void Resolver_CaminhoInvalido_DevolveNullSemLancar()
    {
        Assert.IsNull(PrefabCatalog.Resolver("Assets/Prefabs/NaoExiste_xyz.prefab"));
        Assert.IsNull(PrefabCatalog.Resolver(""));
        Assert.IsNull(PrefabCatalog.Resolver(null));
    }
}

}
