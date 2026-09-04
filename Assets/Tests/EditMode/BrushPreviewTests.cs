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
    /// Uma versao anterior reposicionava pool[0] para marcar o cursor, o que fazia
    /// dois quadrados cairem na mesma celula e uma celula legitima sumir do preview.
    /// O marcador tem que ser um objeto separado.
    /// </summary>
    [Test]
    public void MarcadorDoCursor_NaoConsomeUmaCelulaDaArea()
    {
        var campos = typeof(BrushPreview)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(campos.FirstOrDefault(f => f.Name == "marcadorCursor"),
            "O realce do cursor tem que ser um SpriteRenderer proprio. Reutilizar um " +
            "quadrado do pool apaga uma celula da area prevista.");
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

}
