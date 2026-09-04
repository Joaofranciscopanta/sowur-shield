using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using SowurShield.Farming;
using SowurShield.MapEditor;

namespace SowurShield.Tests
{

/// <summary>
/// Cobre a ponte entre o editor de mapa e o dual grid do jogo (fase 2, 2026-09-03).
///
/// O editor foi escrito contra um sistema de tiles que o jogo nao tem: um enum de 15
/// valores (Grass, Dirt, Water, Stone...). O dual grid real e BINARIO — o placeholder
/// so distingue duas coisas, e os 16 tiles do display sao derivados dos 4 vizinhos.
/// `DualGridPaintAdapter` e a traducao entre os dois, e estes testes fixam as tres
/// regras que nao sao obvias lendo o codigo:
///
/// 1. A inversao: pintar `grassPlaceholderTile` marca a celula como DIRT, e celula
///    VAZIA conta como GRASS. Nao e bug, e como o jogo sempre funcionou — mas quem
///    mexer no adaptador sem saber disso inverte o mundo inteiro.
/// 2. Tipos sem placeholder (Water, Stone, ...) tem que ser RECUSADOS, nao gravados.
///    Gravar no MapData um tipo que nunca reaparece na tela e perda de dados silenciosa.
/// 3. O round-trip capturar -> limpar -> aplicar tem que devolver o mesmo mundo.
///
/// Todos foram provados vermelhos antes da correcao existir.
/// </summary>
public class DualGridPaintAdapterTests
{
    private GameObject host;
    private DualGridTilemap dualGrid;
    private Tilemap placeholder;
    private Tilemap display;
    private Tile grassPlaceholderTile;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("DualGridTestHost");

        var placeholderGO = new GameObject("PlaceholderTilemap");
        placeholderGO.transform.SetParent(host.transform);
        placeholderGO.AddComponent<Grid>();
        placeholder = placeholderGO.AddComponent<Tilemap>();

        var displayGO = new GameObject("DisplayTilemap");
        displayGO.transform.SetParent(host.transform);
        displayGO.AddComponent<Grid>();
        display = displayGO.AddComponent<Tilemap>();

        dualGrid = host.AddComponent<DualGridTilemap>();
        dualGrid.placeholderTilemap = placeholder;
        dualGrid.displayTilemap = display;

        grassPlaceholderTile = ScriptableObject.CreateInstance<Tile>();
        grassPlaceholderTile.name = "TestGrassPlaceholder";
        dualGrid.grassPlaceholderTile = grassPlaceholderTile;
        dualGrid.dirtPlaceholderTile = ScriptableObject.CreateInstance<Tile>();

        // Os 16 tiles do display. O conteudo nao importa aqui, so a identidade.
        dualGrid.tiles = new Tile[16];
        for (int i = 0; i < 16; i++)
        {
            dualGrid.tiles[i] = ScriptableObject.CreateInstance<Tile>();
            dualGrid.tiles[i].name = "TestTile_" + i;
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (host != null) Object.DestroyImmediate(host);
    }

    /// <summary>
    /// A regressao que custou a primeira tentativa da fase 2: `neighbourTupleToTile` e
    /// static e era preenchido so no Start(), entao SetCell lancava NullReferenceException
    /// fora do Play Mode — que e exatamente onde o editor de mapa (dev-only) roda.
    /// </summary>
    [Test]
    public void Paint_ForaDoPlayMode_NaoLanca()
    {
        Assert.DoesNotThrow(
            () => DualGridPaintAdapter.Paint(dualGrid, new Vector3Int(3, 3, 0), ExtendedTileType.Dirt),
            "SetCell precisa construir as regras sob demanda: o editor roda fora do Play Mode, " +
            "onde Start() nunca correu.");
    }

    [Test]
    public void PaintDirt_EscreveOPlaceholderDeGrama_PorCausaDaInversao()
    {
        var cell = new Vector3Int(2, 2, 0);
        bool ok = DualGridPaintAdapter.Paint(dualGrid, cell, ExtendedTileType.Dirt);

        Assert.IsTrue(ok, "Dirt e pintavel neste tileset.");
        Assert.AreSame(grassPlaceholderTile, placeholder.GetTile(cell),
            "A inversao do jogo: o tile chamado 'grassPlaceholder' e o que marca TERRA.");
    }

    [Test]
    public void PaintGrass_LimpaACelula_PorqueVazioSignificaGrama()
    {
        var cell = new Vector3Int(4, 1, 0);
        DualGridPaintAdapter.Paint(dualGrid, cell, ExtendedTileType.Dirt);
        DualGridPaintAdapter.Paint(dualGrid, cell, ExtendedTileType.Grass);

        Assert.IsNull(placeholder.GetTile(cell),
            "Grama e a ausencia de terra — pintar grama tem que limpar, nao escrever outro tile.");
    }

    [Test]
    public void Read_DevolveOTipoCorreto_NosDoisEstados()
    {
        var terra = new Vector3Int(5, 5, 0);
        var grama = new Vector3Int(6, 5, 0);
        DualGridPaintAdapter.Paint(dualGrid, terra, ExtendedTileType.Dirt);

        Assert.AreEqual(ExtendedTileType.Dirt, DualGridPaintAdapter.Read(dualGrid, terra));
        Assert.AreEqual(ExtendedTileType.Grass, DualGridPaintAdapter.Read(dualGrid, grama),
            "Celula nunca tocada le como grama.");
    }

    /// <summary>
    /// O enum oferece 15 tipos; este tileset desenha 2. Pintar Water tem que falhar de
    /// forma visivel, senao o MapData guarda um dado que nunca reaparece na tela.
    /// </summary>
    [Test]
    public void TiposSemPlaceholder_SaoRecusados_ENaoTocamOTilemap()
    {
        var cell = new Vector3Int(7, 7, 0);

        foreach (var tipo in new[] { ExtendedTileType.Water, ExtendedTileType.Stone,
                                     ExtendedTileType.Sand, ExtendedTileType.Lava })
        {
            bool ok = DualGridPaintAdapter.Paint(dualGrid, cell, tipo);
            Assert.IsFalse(ok, tipo + " nao existe neste tileset e tem que ser recusado.");
        }

        Assert.IsNull(placeholder.GetTile(cell),
            "Um tipo recusado nao pode ter deixado nada escrito no tilemap.");
    }

    [Test]
    public void IsPaintable_SoAceitaOQueODualGridDesenha()
    {
        Assert.IsTrue(DualGridPaintAdapter.IsPaintable(ExtendedTileType.Dirt));
        Assert.IsTrue(DualGridPaintAdapter.IsPaintable(ExtendedTileType.Grass));
        Assert.IsTrue(DualGridPaintAdapter.IsPaintable(ExtendedTileType.None));
        Assert.IsFalse(DualGridPaintAdapter.IsPaintable(ExtendedTileType.Water));
        Assert.IsFalse(DualGridPaintAdapter.IsPaintable(ExtendedTileType.Custom1));
    }

    /// <summary>
    /// O `// TODO: Scan current tilemap state` fazia SaveCurrentMap gravar um MapData
    /// VAZIO por cima do mundo. Este teste e a prova de que capturar e aplicar fecham.
    /// </summary>
    [Test]
    public void RoundTrip_CapturarLimparAplicar_DevolveOMesmoMundo()
    {
        var pintadas = new[]
        {
            new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(5, 5, 0), new Vector3Int(-3, -2, 0)
        };
        foreach (var c in pintadas)
            DualGridPaintAdapter.Paint(dualGrid, c, ExtendedTileType.Dirt);

        var mapData = ScriptableObject.CreateInstance<MapData>();
        try
        {
            int captured = DualGridPaintAdapter.CaptureInto(dualGrid, mapData);
            Assert.AreEqual(pintadas.Length, captured,
                "CaptureInto tem que achar exatamente as celulas de terra pintadas.");

            placeholder.ClearAllTiles();
            foreach (var c in pintadas)
                Assert.IsNull(placeholder.GetTile(c), "O mundo tinha que estar limpo antes de aplicar.");

            int applied = DualGridPaintAdapter.Apply(dualGrid, mapData);
            Assert.AreEqual(pintadas.Length, applied);

            foreach (var c in pintadas)
                Assert.AreSame(grassPlaceholderTile, placeholder.GetTile(c),
                    "A celula " + c + " tinha que voltar depois do round-trip.");
        }
        finally
        {
            Object.DestroyImmediate(mapData);
        }
    }

    /// <summary>
    /// Grama e o estado padrao de ~10.000 celulas. Grava-las faria o asset explodir sem
    /// ganho nenhum, entao so a terra vai para o MapData.
    /// </summary>
    [Test]
    public void CaptureInto_NaoGravaGrama()
    {
        DualGridPaintAdapter.Paint(dualGrid, new Vector3Int(1, 1, 0), ExtendedTileType.Dirt);
        DualGridPaintAdapter.Paint(dualGrid, new Vector3Int(2, 2, 0), ExtendedTileType.Grass);

        var mapData = ScriptableObject.CreateInstance<MapData>();
        try
        {
            int captured = DualGridPaintAdapter.CaptureInto(dualGrid, mapData);
            Assert.AreEqual(1, captured, "So a celula de terra deve ser gravada.");
        }
        finally
        {
            Object.DestroyImmediate(mapData);
        }
    }

    /// <summary>
    /// Aplicar um mapa novo tem que apagar o mundo antigo: sem isso, terra de fora do
    /// novo MapData sobrevivia e os dois mapas se misturavam.
    /// </summary>
    [Test]
    public void Apply_LimpaOMundoAntigo()
    {
        var sobrevivente = new Vector3Int(20, 20, 0);
        DualGridPaintAdapter.Paint(dualGrid, sobrevivente, ExtendedTileType.Dirt);

        var mapData = ScriptableObject.CreateInstance<MapData>();
        try
        {
            mapData.SetTileAt(new Vector3Int(1, 1, 0), ExtendedTileType.Dirt);
            DualGridPaintAdapter.Apply(dualGrid, mapData);

            Assert.IsNull(placeholder.GetTile(sobrevivente),
                "Terra fora do MapData aplicado nao pode sobreviver.");
        }
        finally
        {
            Object.DestroyImmediate(mapData);
        }
    }

    [Test]
    public void Adaptador_ToleraReferenciasNulas()
    {
        Assert.DoesNotThrow(() => DualGridPaintAdapter.Paint(null, Vector3Int.zero, ExtendedTileType.Dirt));
        Assert.AreEqual(ExtendedTileType.None, DualGridPaintAdapter.Read(null, Vector3Int.zero));
        Assert.AreEqual(0, DualGridPaintAdapter.CaptureInto(dualGrid, null));
        Assert.AreEqual(0, DualGridPaintAdapter.Apply(dualGrid, null));
    }
}

}
