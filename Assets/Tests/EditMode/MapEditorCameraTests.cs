using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SowurShield.Core;
using SowurShield.MapEditor;

namespace SowurShield.Tests
{

/// <summary>
/// A camera livre do editor de mapa (2026-09-03): setas para andar, roda ou +/-
/// para zoom, para ver o mundo inteiro em vez do enquadramento do jogo (ortho 2.6).
///
/// O ponto que nao e obvio: nao basta mover a camera. O FollowPlayer a cola no
/// jogador em LateUpdate, TODO frame — mover sem desliga-lo nao teria efeito
/// nenhum, e o defeito pareceria "a camera nao anda" em vez de "algo a puxa de
/// volta".
/// </summary>
public class MapEditorCameraTests
{
    private static readonly BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

    private GameObject host;
    private GameObject cameraGO;
    private RuntimeMapEditor editor;
    private MapEditorCamera controle;
    private Camera cam;
    private FollowPlayer follow;

    [SetUp]
    public void SetUp()
    {
        cameraGO = new GameObject("Main Camera", typeof(Camera));
        cameraGO.tag = "MainCamera";
        cam = cameraGO.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 2.6f;               // o enquadramento do jogo
        cameraGO.transform.position = new Vector3(1f, 2f, -10f);
        follow = cameraGO.AddComponent<FollowPlayer>();

        host = new GameObject("MapEditorTestHost");
        editor = host.AddComponent<RuntimeMapEditor>();
        controle = host.AddComponent<MapEditorCamera>();

        // Start() nao roda sozinho em EditMode; a inscricao no evento e feita la.
        typeof(MapEditorCamera).GetMethod("Start", Priv).Invoke(controle, null);

        // Camera.main devolveria a camera da cena aberta no Editor, nao esta.
        controle.UsarCamera(cam);
    }

    [TearDown]
    public void TearDown()
    {
        if (host != null) Object.DestroyImmediate(host);
        if (cameraGO != null) Object.DestroyImmediate(cameraGO);
    }

    private void Abrir() => editor.SetEditorMode(true);
    private void Fechar() => editor.SetEditorMode(false);

    /// <summary>
    /// A regressao central: sem desligar o FollowPlayer, cada movimento da camera
    /// seria desfeito no LateUpdate seguinte.
    /// </summary>
    [Test]
    public void AoAbrir_DesligaOFollowPlayer()
    {
        Assert.IsTrue(follow.enabled, "Antes de abrir, a camera segue o jogador.");

        Abrir();

        Assert.IsFalse(follow.enabled,
            "Com o FollowPlayer ligado, o LateUpdate cola a camera no jogador todo " +
            "frame e a camera livre nao sai do lugar.");
    }

    [Test]
    public void AoFechar_DevolveACameraAoJogador()
    {
        Abrir();
        Fechar();

        Assert.IsTrue(follow.enabled, "Fechar o editor devolve a camera ao jogador.");
    }

    /// <summary>
    /// Quem constroi nao quer sair do editor e encontrar o jogo com outro zoom ou a
    /// camera noutro canto do mundo.
    /// </summary>
    [Test]
    public void AoFechar_RestauraZoomEPosicao()
    {
        var posicaoOriginal = cameraGO.transform.position;
        const float zoomOriginal = 2.6f;

        Abrir();
        cameraGO.transform.position = new Vector3(40f, 30f, -10f);
        cam.orthographicSize = 18f;
        Fechar();

        Assert.AreEqual(posicaoOriginal, cameraGO.transform.position,
            "A posicao de antes do editor tem que voltar.");
        Assert.AreEqual(zoomOriginal, cam.orthographicSize, 0.001f,
            "O zoom do jogo tem que voltar.");
    }

    [Test]
    public void ComEditorFechado_NaoMexeNaCamera()
    {
        var posicao = cameraGO.transform.position;
        var zoom = cam.orthographicSize;

        typeof(MapEditorCamera).GetMethod("Update", Priv).Invoke(controle, null);

        Assert.AreEqual(posicao, cameraGO.transform.position);
        Assert.AreEqual(zoom, cam.orthographicSize, 0.001f);
        Assert.IsTrue(follow.enabled);
    }

    /// <summary>
    /// Zoom sem limite deixa passar de perto demais (nada visivel) e de longe demais
    /// (o mundo pintado tem 25x24 celulas).
    /// </summary>
    [Test]
    public void Zoom_TemLimitesUteis()
    {
        float minimo = (float)typeof(MapEditorCamera).GetField("zoomMinimo", Priv).GetValue(controle);
        float maximo = (float)typeof(MapEditorCamera).GetField("zoomMaximo", Priv).GetValue(controle);

        Assert.Greater(minimo, 0f, "Zoom minimo tem que ser positivo.");
        Assert.Less(minimo, 2.6f, "O minimo tem que aproximar mais que o jogo.");
        Assert.Greater(maximo, 10f,
            "O maximo tem que caber o mundo pintado; e para isso que o zoom existe.");
        Assert.Less(maximo, 100f, "Longe demais e so vazio.");
    }

    [Test]
    public void CentrarNoJogador_NaoMudaOZ()
    {
        var jogador = new GameObject("Player", typeof(Rigidbody2D), typeof(Animator));
        jogador.AddComponent<PlayerMove>();
        jogador.transform.position = new Vector3(9f, -4f, 0f);
        try
        {
            Abrir();
            cameraGO.transform.position = new Vector3(50f, 50f, -10f);

            controle.CentrarNoJogador();

            Assert.AreEqual(9f, cameraGO.transform.position.x, 0.001f);
            Assert.AreEqual(-4f, cameraGO.transform.position.y, 0.001f);
            Assert.AreEqual(-10f, cameraGO.transform.position.z, 0.001f,
                "O z da camera e a profundidade — mexer nele tiraria o mundo de vista.");
        }
        finally
        {
            Object.DestroyImmediate(jogador);
        }
    }
}

/// <summary>
/// ESC e M tambem precisam recuar com o editor aberto (pedido do Lucas em
/// 2026-09-03, depois de construir e esbarrar nos menus).
///
/// ESC e o caso interessante: bloquear a tecla prenderia quem constroi, porque ESC
/// e o reflexo de "sair daqui". Ela FECHA o editor em vez de abrir a pausa.
/// </summary>
public class EditorMenuGuardTests
{
    private static string Fonte(string caminho) =>
        System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, caminho));

    [Test]
    public void Esc_FechaOEditorEmVezDeAbrirOMenu()
    {
        var fonte = Fonte("Scripts/Core/UIInput.cs");

        int guarda = fonte.IndexOf("RuntimeMapEditor.Instance");
        Assert.Greater(guarda, -1, "ESC precisa saber do editor de mapa.");

        Assert.IsTrue(fonte.Contains("SetEditorMode(false)"),
            "ESC tem que FECHAR o editor, nao so ser ignorada — bloquear sem saida " +
            "prenderia quem constroi dentro do modo.");

        int menu = fonte.IndexOf("UIManager.Instance.HandleEscapeKey");
        Assert.Less(guarda, menu, "A guarda vem antes de delegar ao menu de pausa.");
    }

    [Test]
    public void M_NaoAbreOMinimapaComOEditorAberto()
    {
        var fonte = Fonte("Scripts/Minimap/MinimapController.cs");

        int guarda = fonte.IndexOf("RuntimeMapEditor.Instance");
        Assert.Greater(guarda, -1,
            "O minimapa fullscreen toma a tela e desabilita o movimento; no editor " +
            "so atrapalha.");

        int toggle = fonte.IndexOf("ToggleMinimapState();", guarda);
        Assert.Greater(toggle, guarda, "A guarda vem antes de alternar o estado.");
    }
}

}
