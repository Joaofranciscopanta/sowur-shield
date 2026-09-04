using System.Collections.Generic;
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
/// Fixa a ligacao do pincel ao chao do jogo (2026-09-03).
///
/// O BrushTool tinha 539 linhas de brushes (pincel, linha, retangulo, balde, borracha)
/// e nunca desenhou nada: todos os seus caminhos de escrita passavam por um
/// `ExtendedDualGridTilemap`, um sistema paralelo que nao existe em cena nenhuma —
/// ha 0 assets de TileLibrary no projeto. Com o campo null, `PaintTiles` retornava
/// na primeira linha e o clique nao tinha efeito, sem erro nenhum no console.
///
/// Agora o pincel fala com o DualGridTilemap do jogo atraves do RuntimeMapEditor.
/// Estes testes cobrem o que quebraria de novo em silencio.
/// </summary>
public class BrushToolWiringTests
{
    private GameObject host;
    private BrushTool brush;

    private static readonly BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("BrushToolTestHost");
        brush = host.AddComponent<BrushTool>();
    }

    [TearDown]
    public void TearDown()
    {
        if (host != null) Object.DestroyImmediate(host);
    }

    /// <summary>
    /// A regressao central: se alguem reintroduzir uma dependencia de
    /// ExtendedDualGridTilemap, o pincel volta a nao pintar sem avisar.
    /// </summary>
    [Test]
    public void BrushTool_NaoDependeMaisDoSistemaParalelo()
    {
        var campos = typeof(BrushTool).GetFields(Priv | BindingFlags.Public);

        Assert.IsNull(
            campos.FirstOrDefault(f => f.FieldType == typeof(ExtendedDualGridTilemap)),
            "O pincel voltou a depender do ExtendedDualGridTilemap, que nao existe em " +
            "cena nenhuma (0 assets de TileLibrary). Com ele null, PaintTiles retorna " +
            "cedo e o clique nao pinta — sem erro no console.");

        Assert.IsNotNull(
            campos.FirstOrDefault(f => f.FieldType == typeof(DualGridTilemap)),
            "O pincel precisa falar com o DualGridTilemap do jogo.");
    }

    /// <summary>
    /// WorldToTilePosition arredondava na mao com FloorToInt. Hoje o grid e 1x1 na
    /// origem e o resultado coincide, mas qualquer mudanca de cellSize, offset ou
    /// escala faria o pincel pintar uma celula ao lado do cursor — em silencio.
    /// </summary>
    [Test]
    public void WorldToTilePosition_UsaOGridDaCena_NaoArredondaNaMao()
    {
        var grid = new GameObject("Grid");
        var gridComp = grid.AddComponent<Grid>();
        // Um grid deslocado e com celula diferente de 1: aqui FloorToInt erraria.
        gridComp.cellSize = new Vector3(2f, 2f, 0f);
        grid.transform.position = new Vector3(10f, 10f, 0f);

        var tilemapGO = new GameObject("PlaceholderTilemap");
        tilemapGO.transform.SetParent(grid.transform);
        var tilemap = tilemapGO.AddComponent<Tilemap>();

        var dualGO = new GameObject("DualGrid");
        var dual = dualGO.AddComponent<DualGridTilemap>();
        dual.placeholderTilemap = tilemap;

        try
        {
            typeof(BrushTool).GetField("dualGrid", Priv).SetValue(brush, dual);

            var metodo = typeof(BrushTool).GetMethod("WorldToTilePosition", Priv);
            var world = new Vector3(13.5f, 17.2f, 0f);

            var obtido = (Vector3Int)metodo.Invoke(brush, new object[] { world });
            var esperado = tilemap.WorldToCell(world);
            esperado.z = 0;

            Assert.AreEqual(esperado, obtido,
                "A conversao tem que perguntar ao tilemap. Com cellSize 2 e o grid " +
                "deslocado, FloorToInt daria outra celula.");

            var floor = new Vector3Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y), 0);
            Assert.AreNotEqual(floor, obtido,
                "Se este teste falhar aqui, o cenario deixou de distinguir os dois " +
                "metodos e nao esta provando nada.");
        }
        finally
        {
            Object.DestroyImmediate(dualGO);
            Object.DestroyImmediate(grid);
        }
    }

    [Test]
    public void GetBrushArea_TamanhoUm_PintaUmaCelulaSo()
    {
        typeof(BrushTool).GetField("brushSize", Priv).SetValue(brush, 1);
        var area = InvocarGetBrushArea(new Vector3Int(5, 5, 0));

        Assert.AreEqual(1, area.Count);
        Assert.AreEqual(new Vector3Int(5, 5, 0), area[0]);
    }

    /// <summary>
    /// O pincel grande e um losango (filtra por distancia), nao um quadrado.
    /// Fixado porque e facil "corrigir" isso por engano para 9 celulas.
    /// </summary>
    [Test]
    public void GetBrushArea_TamanhoTres_EUmLosangoDeCincoCelulas()
    {
        typeof(BrushTool).GetField("brushSize", Priv).SetValue(brush, 3);
        var area = InvocarGetBrushArea(Vector3Int.zero);

        Assert.AreEqual(5, area.Count, "Losango de raio 1, nao quadrado 3x3.");
        foreach (var esperada in new[]
        {
            Vector3Int.zero,
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
        })
            Assert.Contains(esperada, area);

        Assert.IsFalse(area.Contains(new Vector3Int(1, 1, 0)), "As diagonais ficam de fora.");
    }

    private List<Vector3Int> InvocarGetBrushArea(Vector3Int centro)
    {
        var metodo = typeof(BrushTool).GetMethod("GetBrushArea", Priv);
        return (List<Vector3Int>)metodo.Invoke(brush, new object[] { centro });
    }
}

/// <summary>
/// O CursorController do jogo tem que recuar enquanto o editor de mapa esta aberto.
///
/// EnterEditorMode LIGA o CursorController de proposito (para o cursor ficar visivel),
/// entao ele nao para sozinho: o mesmo clique pintava um tile E usava a ferramenta da
/// mao — arar, regar, cavar — na celula debaixo do cursor.
/// </summary>
public class CursorControllerEditorGuardTests
{
    [Test]
    public void CursorController_ConsultaOEditorDeMapa()
    {
        var fonte = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath,
                "Scripts/DualGridTilemap/CursorController.cs"));

        Assert.IsTrue(fonte.Contains("RuntimeMapEditor.Instance"),
            "O CursorController precisa recuar com o editor aberto, senao o clique " +
            "pinta E usa a ferramenta da mao ao mesmo tempo.");

        int guarda = fonte.IndexOf("RuntimeMapEditor.Instance");
        int inventario = fonte.IndexOf("IsInventoryOpen");
        Assert.Less(guarda, inventario,
            "A guarda do editor tem que vir antes das outras: ela retorna cedo, e as " +
            "de baixo ligam o cursorRenderer de volta.");
    }
}

}
