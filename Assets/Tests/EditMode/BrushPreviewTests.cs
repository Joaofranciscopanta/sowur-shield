using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SowurShield.MapEditor;

namespace SowurShield.Tests
{

/// <summary>
/// Cobre o preview do pincel (2026-09-03): o realce da celula sob o cursor e a
/// previsao da forma antes do clique.
///
/// A regra que da sentido a tudo isto: o preview e o pincel tem que concordar. Um
/// preview que mostra uma forma e pinta outra e pior que nao ter preview nenhum,
/// porque o usuario confia nele. Foi exatamente assim que apareceu um erro de
/// off-by-one que existia ha meses no GetLinePoints do BrushTool.
/// </summary>
public class BrushPreviewTests
{
    private static readonly BindingFlags Priv =
        BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    private GameObject host;
    private BrushPreview preview;
    private BrushTool brush;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("BrushPreviewTestHost");
        host.AddComponent<RuntimeMapEditor>();   // exigido por [RequireComponent]
        brush = host.AddComponent<BrushTool>();
        preview = host.AddComponent<BrushPreview>();
    }

    [TearDown]
    public void TearDown()
    {
        if (host != null) Object.DestroyImmediate(host);
    }

    private List<Vector3Int> Linha(Vector3Int a, Vector3Int b) =>
        (List<Vector3Int>)typeof(BrushPreview)
            .GetMethod("CelulasDaLinha", Priv).Invoke(preview, new object[] { a, b });

    private List<Vector3Int> Retangulo(Vector3Int a, Vector3Int b) =>
        (List<Vector3Int>)typeof(BrushPreview)
            .GetMethod("CelulasDoRetangulo", Priv).Invoke(preview, new object[] { a, b });

    private List<Vector3Int> LinhaDoPincel(Vector3Int a, Vector3Int b) =>
        (List<Vector3Int>)typeof(BrushTool)
            .GetMethod("GetLinePoints", Priv).Invoke(brush, new object[] { a, b });

    /// <summary>
    /// A regressao mais valiosa desta leva. `GetLinePoints` iterava `i < dx + dy`,
    /// mas uma linha de (0,0) a (0,5) tem SEIS celulas. O pincel parava um passo
    /// antes e deixava de pintar a celula onde o usuario soltava o botao — defeito
    /// que ninguem tinha notado porque o pincel nunca chegou a desenhar nada ate
    /// hoje. Foi o preview, ao discordar, que o revelou.
    /// </summary>
    [Test]
    public void LinhaVertical_PintaAteACelulaFinal()
    {
        var pontos = LinhaDoPincel(new Vector3Int(0, 0, 0), new Vector3Int(0, 5, 0));

        Assert.AreEqual(6, pontos.Count,
            "Uma linha de (0,0) a (0,5) cobre seis celulas, nao cinco.");
        Assert.AreEqual(new Vector3Int(0, 5, 0), pontos[pontos.Count - 1],
            "A ultima celula — onde o usuario solta o botao — tem que ser pintada.");
    }

    [Test]
    public void LinhaHorizontal_PintaAteACelulaFinal()
    {
        var pontos = LinhaDoPincel(new Vector3Int(0, 0, 0), new Vector3Int(5, 0, 0));

        Assert.AreEqual(6, pontos.Count);
        Assert.AreEqual(new Vector3Int(5, 0, 0), pontos[pontos.Count - 1]);
    }

    /// <summary>
    /// O contrato central: o que o preview promete e o que o pincel entrega.
    /// </summary>
    [Test]
    public void PreviewDaLinha_ConcordaComOPincel()
    {
        var casos = new[]
        {
            (new Vector3Int(0, 0, 0),   new Vector3Int(4, 2, 0)),
            (new Vector3Int(3, 7, 0),   new Vector3Int(-2, 1, 0)),
            (new Vector3Int(0, 0, 0),   new Vector3Int(0, 5, 0)),   // vertical
            (new Vector3Int(0, 0, 0),   new Vector3Int(5, 0, 0)),   // horizontal
            (new Vector3Int(2, 2, 0),   new Vector3Int(2, 2, 0)),   // ponto
            (new Vector3Int(-3, -3, 0), new Vector3Int(3, 3, 0))    // diagonal
        };

        foreach (var (inicio, fim) in casos)
        {
            var doPreview = Linha(inicio, fim);
            var doPincel = LinhaDoPincel(inicio, fim);

            CollectionAssert.AreEqual(doPincel, doPreview,
                $"O preview de {inicio}->{fim} tem que ser exatamente o que o pincel pinta. " +
                "Se divergirem, o preview mente para o usuario.");
        }
    }

    [Test]
    public void Retangulo_CobreAAreaInteira()
    {
        var celulas = Retangulo(new Vector3Int(0, 0, 0), new Vector3Int(2, 3, 0));
        Assert.AreEqual(12, celulas.Count, "3 colunas x 4 linhas.");
        Assert.Contains(new Vector3Int(0, 0, 0), celulas);
        Assert.Contains(new Vector3Int(2, 3, 0), celulas);
        Assert.Contains(new Vector3Int(1, 2, 0), celulas);
    }

    /// <summary>
    /// Arrastar da direita para a esquerda tem que dar o mesmo retangulo: quem
    /// desenha nao pensa em "canto inicial".
    /// </summary>
    [Test]
    public void Retangulo_NaoImportaADirecaoDoArrasto()
    {
        var normal = Retangulo(new Vector3Int(0, 0, 0), new Vector3Int(2, 3, 0));
        var invertido = Retangulo(new Vector3Int(2, 3, 0), new Vector3Int(0, 0, 0));

        Assert.AreEqual(normal.Count, invertido.Count);
        foreach (var c in normal) Assert.Contains(c, invertido);
    }

    [Test]
    public void Retangulo_DeUmaCelulaSo()
    {
        var celulas = Retangulo(new Vector3Int(4, 4, 0), new Vector3Int(4, 4, 0));
        Assert.AreEqual(1, celulas.Count);
    }

    /// <summary>
    /// O preview NAO deve marcar a celula sob o cursor: quem faz isso e o indicador
    /// que o jogo ja tem, emprestado ao editor. Um segundo marcador competiria com
    /// o primeiro na mesma celula.
    ///
    /// (Uma versao anterior reposicionava pool[0] para isso, o que punha dois
    /// quadrados na mesma celula e sumia com uma celula legitima da area.)
    /// </summary>
    [Test]
    public void Preview_NaoDesenhaMarcadorProprioDeCursor()
    {
        var campos = typeof(BrushPreview)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNull(campos.FirstOrDefault(f => f.Name == "marcadorCursor"),
            "O realce da celula sob o mouse e do CursorController do jogo. Um " +
            "marcador proprio aqui seria um segundo indicador na mesma celula.");
    }

    /// <summary>
    /// O preview desenha em WorldUI, a sorting layer mais alta. Em Default ele
    /// ficaria por baixo do tilemap do chao — o mesmo enterro que ja escondeu
    /// dezenas de sprites do jogo antes.
    /// </summary>
    [Test]
    public void Preview_DesenhaNaLayerMaisAlta()
    {
        var fonte = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "Scripts/MapEditor/BrushPreview.cs"));

        Assert.IsTrue(fonte.Contains("sortingLayerName = \"WorldUI\""),
            "Sem uma sorting layer alta o preview fica enterrado sob o tilemap do chao.");
    }
}

/// <summary>
/// Enquanto o editor de mapa esta aberto, nada do JOGO pode responder ao input:
/// o Lucas reportou (2026-09-03) que construir esbarrava em NPC, cama e menus.
///
/// Sao dois caminhos independentes, e cobrir so um deixa o defeito de pe:
///  - clique esquerdo -> CursorController (ferramenta, SellBox, NPC por raycast);
///  - tecla E         -> PlayerMove.DetectAndInteract -> InteractionManager.
/// A guarda do cursor ja existia; a de E nao, e era por ali que o dialogo abria.
/// </summary>
public class EditorInputIsolationTests
{
    private static string Fonte(string caminhoRelativo) =>
        System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, caminhoRelativo));

    [Test]
    public void TeclaE_NaoInterageComOEditorAberto()
    {
        var fonte = Fonte("Scripts/Core/PlayerMove.cs");

        int guarda = fonte.IndexOf("RuntimeMapEditor.Instance");
        Assert.Greater(guarda, -1,
            "DetectAndInteract precisa recuar com o editor aberto. Sem isto E abre " +
            "dialogo de NPC e a cama no meio da construcao — o cursor ser neutro " +
            "nao basta, porque E segue outro caminho.");

        int deteccao = fonte.IndexOf("InteractionManager.Instance");
        Assert.Less(guarda, deteccao,
            "A guarda tem que vir antes de consultar o InteractionManager.");
    }

    [Test]
    public void Cursor_ContinuaVisivelMasInerteNoEditor()
    {
        var fonte = Fonte("Scripts/DualGridTilemap/CursorController.cs");

        Assert.IsTrue(fonte.Contains("AcompanharMouseSemInteragir"),
            "O editor usa o indicador do proprio jogo; ele segue o mouse sem interagir.");

        int guarda = fonte.IndexOf("RuntimeMapEditor.Instance");
        int interacao = fonte.IndexOf("ProcessHexInteraction(activeTilePos)");
        Assert.Less(guarda, interacao,
            "A guarda do editor tem que vir antes do processamento de clique.");
    }

    /// <summary>
    /// O editor sobe o cursor para WorldUI para ele nao ficar sob o tilemap. Se o
    /// valor original nao for devolvido, o cursor do jogo fica com o sorting do
    /// editor depois de fechar — um defeito que so apareceria muito depois.
    /// </summary>
    [Test]
    public void Cursor_RecuperaOSortingOriginalAoFechar()
    {
        var fonte = Fonte("Scripts/DualGridTilemap/CursorController.cs");

        Assert.IsTrue(fonte.Contains("sortingLayerOriginal"),
            "O sorting original precisa ser guardado.");
        Assert.IsTrue(fonte.Contains("cursorRenderer.sortingLayerName = sortingLayerOriginal"),
            "E devolvido ao sair do modo editor.");
    }
}

}
