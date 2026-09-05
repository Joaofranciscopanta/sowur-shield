using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SowurShield.Core;

namespace SowurShield.Tests
{

/// <summary>
/// Registo e fecho de janelas de UI (2026-09-05).
///
/// O UIManager guarda duas colecoes: `registeredWindows` (todas as janelas que existem) e
/// `openWindowStack` (as que foram abertas atraves do TryOpenWindow). A auditoria desta data
/// achou os dois lados desalinhados:
///
///   1. Tres janelas implementavam IUIWindow e NUNCA chamavam RegisterWindow, entao nao
///      existiam para o UIManager -- nem para o ESC, nem para o fecho de emergencia.
///   2. O ForceCloseAllWindows percorria so a pilha, entao uma janela aberta por fora do
///      TryOpenWindow ficava orfa: medido, com 4 abertas 3 continuavam abertas depois de
///      chamar o metodo que existe justamente para esse caso.
/// </summary>
public class UIWindowRegistrationTests
{
    private static readonly BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

    private GameObject host;
    private UIManager manager;

    /// <summary>Janela de teste: sem cena, sem canvas, so o contrato do IUIWindow.</summary>
    private class JanelaFalsa : MonoBehaviour, IUIWindow
    {
        public string WindowName => nome;
        public int WindowPriority => 10;
        public bool IsWindowOpen => aberta;
        public bool CanCloseWithEsc => true;

        public string nome = "Falsa";
        public bool aberta;

        public void OpenWindow() { aberta = true; }
        public void CloseWindow() { aberta = false; }
        public void OnWindowBlocked(string blockedBy) { }
    }

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("UIManagerTestHost");
        manager = host.AddComponent<UIManager>();

        // O singleton e atribuido a mao, nao invocando Awake: o Awake do UIManager chama
        // DontDestroyOnLoad, que so existe em Play Mode e lanca num teste de EditMode.
        // `Instance` e uma auto-property com setter privado, entao a escrita vai pelo
        // campo de apoio que o compilador gera.
        DefinirInstancia(manager);
    }

    private static void DefinirInstancia(UIManager valor)
    {
        var prop = typeof(UIManager).GetProperty("Instance");
        var setter = prop != null ? prop.GetSetMethod(nonPublic: true) : null;
        if (setter != null) { setter.Invoke(null, new object[] { valor }); return; }

        var campo = typeof(UIManager).GetField("<Instance>k__BackingField",
                        BindingFlags.NonPublic | BindingFlags.Static);
        if (campo != null) campo.SetValue(null, valor);
    }

    [TearDown]
    public void TearDown()
    {
        if (host != null) Object.DestroyImmediate(host);
        // Sem isto o singleton fica a apontar para um objeto destruido e o teste seguinte
        // (ou o proximo Play Mode) herda uma instancia morta.
        DefinirInstancia(null);
    }

    private JanelaFalsa NovaJanela(string nome)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(host.transform);
        var j = go.AddComponent<JanelaFalsa>();
        j.nome = nome;
        return j;
    }

    private List<IUIWindow> Registradas() =>
        (List<IUIWindow>)typeof(UIManager).GetField("registeredWindows", Priv).GetValue(manager);

    /// <summary>
    /// O contrato central: "fechar todas" fecha TODAS, inclusive a que nunca passou pela
    /// pilha. Este teste falha (a orfa continua aberta) se o metodo voltar a varrer so a pilha.
    /// </summary>
    [Test]
    public void ForceCloseAllWindows_FechaTambemAJanelaAbertaForaDaPilha()
    {
        var empilhada = NovaJanela("Empilhada");
        var orfa = NovaJanela("Orfa");
        manager.RegisterWindow(empilhada);
        manager.RegisterWindow(orfa);

        Assert.IsTrue(manager.TryOpenWindow(empilhada), "A primeira janela devia abrir.");
        orfa.OpenWindow();   // sem passar pelo TryOpenWindow -- e o caso que falhava

        manager.ForceCloseAllWindows();

        Assert.IsFalse(empilhada.IsWindowOpen, "A janela da pilha tem que fechar.");
        Assert.IsFalse(orfa.IsWindowOpen,
            "A janela aberta fora da pilha tambem: varrer so a pilha deixava-a orfa para " +
            "sempre, e este e o metodo de emergencia que existe justamente para esse caso.");
    }

    /// <summary>Fechar tudo esvazia a pilha, senao a proxima janela nunca mais abre.</summary>
    [Test]
    public void ForceCloseAllWindows_DeixaAPilhaVaziaEPermiteAbrirDeNovo()
    {
        var a = NovaJanela("A");
        var b = NovaJanela("B");
        manager.RegisterWindow(a);
        manager.RegisterWindow(b);

        manager.TryOpenWindow(a);
        manager.ForceCloseAllWindows();

        Assert.IsTrue(manager.TryOpenWindow(b),
            "Depois de fechar tudo, a proxima janela tem que conseguir abrir.");
    }

    /// <summary>Uma janela destruida no meio nao pode derrubar o fecho de emergencia.</summary>
    [Test]
    public void ForceCloseAllWindows_ToleraJanelaDestruida()
    {
        var viva = NovaJanela("Viva");
        var morta = NovaJanela("Morta");
        manager.RegisterWindow(viva);
        manager.RegisterWindow(morta);
        morta.OpenWindow();
        Object.DestroyImmediate(morta.gameObject);

        Assert.DoesNotThrow(() => manager.ForceCloseAllWindows(),
            "Uma referencia morta em registeredWindows nao pode lancar.");
    }

    [Test]
    public void RegisterWindow_NaoDuplica()
    {
        var j = NovaJanela("Repetida");
        manager.RegisterWindow(j);
        manager.RegisterWindow(j);

        Assert.AreEqual(1, Registradas().Count(w => ReferenceEquals(w, j)),
            "Registar duas vezes (Awake e Start) nao pode criar duas entradas.");
    }

    /// <summary>
    /// Toda janela que implementa IUIWindow tem que se registar, senao nao existe para o
    /// UIManager. GiftSelectionUI, RelationshipUI e SeedShopUI implementavam a interface e
    /// nunca chamavam RegisterWindow -- medido em runtime: 5 registadas de 12 janelas.
    /// </summary>
    [Test]
    public void TodaJanelaDeUI_ChamaRegisterWindow()
    {
        string raiz = System.IO.Path.Combine(Application.dataPath, "Scripts");
        var faltando = new List<string>();

        foreach (var caminho in System.IO.Directory.GetFiles(raiz, "*.cs",
                                                             System.IO.SearchOption.AllDirectories))
        {
            string nome = System.IO.Path.GetFileNameWithoutExtension(caminho);
            if (nome == "UIManager" || nome == "IUIWindow") continue;

            string fonte = System.IO.File.ReadAllText(caminho);
            // Implementa a interface? (lista de bases, nao uma mencao em comentario)
            if (!System.Text.RegularExpressions.Regex.IsMatch(fonte, @":\s*[^\r\n{]*\bIUIWindow\b"))
                continue;

            // A CHAMADA, nao a palavra: "UnregisterWindow" contem "RegisterWindow", e um
            // comentario que fale do registo tambem -- procurar so a substring daria o
            // teste por passado com o registo apagado.
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    fonte, @"(?<!Un)RegisterWindow\s*\(\s*this\s*\)"))
                faltando.Add(nome);
        }

        Assert.IsEmpty(faltando,
            "Estas janelas implementam IUIWindow e nunca se registam, entao o ESC e o " +
            "ForceCloseAllWindows nao as alcancam: " + string.Join(", ", faltando));
    }
}

}
