using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SowurShield.Interiors;

namespace SowurShield.Editor
{

/// <summary>
/// Gera as cenas de interior das casas da vila.
///
/// Dez quartos montados a mao divergiriam entre si e seriam impossiveis de
/// reajustar: mudar a espessura das paredes obrigaria a reabrir dez cenas. Aqui a
/// planta e uma receita, e cada casa e uma linha de dados.
///
/// As cenas geradas TEM de ser acrescentadas as Build Settings. Sem isso o
/// SceneManager.LoadScene recarrega a cena atual em silencio -- sem erro, sem
/// aviso -- e a porta parece simplesmente nao funcionar. Este menu ja as regista.
/// </summary>
public static class BuildInteriorScenes
{
    private const string PastaCenas = "Assets/Scenes/Interiors";
    private const string CenaVila = "Assets/Scenes/SampleScene.unity";

    private const string FolhaParedes = "Assets/Art/ThirdParty/main-characters-home-free-top-down-pixel-art-asset/PNG/walls_floor.png";
    private const string FolhaMoveis = "Assets/Art/ThirdParty/main-characters-home-free-top-down-pixel-art-asset/PNG/Interior.png";

    /// <summary>Uma casa: quem vive la, e o que se ve la dentro.</summary>
    private readonly struct Casa
    {
        public readonly string Id;
        public readonly string Dono;
        public readonly string Titulo;
        public readonly int LarguraCelulas;
        public readonly int AlturaCelulas;
        public readonly string Tema;

        public Casa(string id, string dono, string titulo, int w, int h, string tema)
        {
            Id = id; Dono = dono; Titulo = titulo;
            LarguraCelulas = w; AlturaCelulas = h; Tema = tema;
        }
    }

    private static readonly Casa[] Casas =
    {
        new Casa("Jogador", "", "Casa", 9, 7, "casa"),
        new Casa("Isabela", "Isabela", "Despensa", 9, 7, "loja"),
        new Casa("Tomas", "Tomás", "Pomar", 9, 7, "loja"),
        new Casa("Nara", "Nara", "Viveiro", 8, 6, "loja"),
        new Casa("Clara", "Clara", "Ervanaria", 8, 6, "loja"),
        new Casa("Rui", "Rui", "Casa do Pescador", 8, 6, "casa"),
        new Casa("Bento", "Bento", "Carpintaria", 9, 7, "oficina"),
        new Casa("Elias", "Elias", "Cabana", 8, 6, "casa"),
        new Casa("Joana", "Joana", "Casa da Joana", 8, 6, "casa"),
        new Casa("Maren", "Maren", "Casa Grande", 11, 8, "salao"),
    };

    [MenuItem("Sowur Shield/Interiores/Gerar cenas de interior")]
    public static void Gerar()
    {
        if (!System.IO.Directory.Exists(PastaCenas))
            System.IO.Directory.CreateDirectory(PastaCenas);

        var paredes = CarregarFolha(FolhaParedes);
        var moveis = CarregarFolha(FolhaMoveis);
        if (paredes.Count == 0 || moveis.Count == 0)
        {
            Debug.LogError("[Interiores] Nao encontrei a arte de paredes ou de moveis.");
            return;
        }

        var criadas = new List<string>();

        foreach (var casa in Casas)
        {
            string caminho = PastaCenas + "/Interior_" + casa.Id + ".unity";
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            MontarInterior(casa, paredes, moveis);

            EditorSceneManager.SaveScene(cena, caminho);
            criadas.Add(caminho);
        }

        RegistarNasBuildSettings(criadas);
        EditorSceneManager.OpenScene(CenaVila, OpenSceneMode.Single);
        Debug.Log("[Interiores] " + criadas.Count + " cenas geradas e registadas nas Build Settings.");
    }

    private static Dictionary<string, Sprite> CarregarFolha(string caminho)
    {
        var d = new Dictionary<string, Sprite>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(caminho))
        {
            var s = o as Sprite;
            if (s != null) d[s.name] = s;
        }
        return d;
    }

    private static void MontarInterior(Casa casa, Dictionary<string, Sprite> paredes,
                                       Dictionary<string, Sprite> moveis)
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;

        // O quarto INTEIRO no ecra, e a camera fixa.
        //
        // A vila corre a orthographicSize 2,6 — ve 9,2x5,2 unidades. Aplicado a um
        // quarto de 9x7 isso mostrava um pedaco do chao e deixava as prateleiras e o
        // dono da casa fora do enquadramento; entrar numa loja e nao ver o lojista.
        //
        // Enquadrar o compartimento todo e o que a maioria dos jogos 2D faz em salas
        // pequenas: sem camera a perseguir, le-se a divisao de uma so vez. A margem
        // de 1,6 unidades deixa respirar as paredes, incluindo a fiada extra do topo.
        // 16:9 fixo, e nao cam.aspect: fora do Play Mode o aspect da camera reflete a
        // janela do Editor, e o valor gravado na cena sairia diferente conforme o
        // tamanho da Game View no momento em que este menu correu.
        const float Aspect = 16f / 9f;
        float meiaAltura = (casa.AlturaCelulas + 3.2f) / 2f;
        float meiaLargura = (casa.LarguraCelulas + 3.2f) / 2f / Aspect;
        cam.orthographicSize = Mathf.Max(meiaAltura, meiaLargura);

        cam.backgroundColor = new Color(0.09f, 0.07f, 0.09f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        // Esconder as camadas do minimapa.
        //
        // Uma camera nova nasce com cullingMask = -1 (ve tudo), e o jogador leva consigo
        // um Bunny_MinimapIcon de 5,91 unidades — desenhado a pensar na camera do
        // minimapa, nao nesta. Dentro de casa aparecia como um V verde gigante por cima
        // do quarto. A camera da vila ja exclui estas camadas; a do interior tem de
        // fazer o mesmo.
        int mascara = -1;
        foreach (var nome in new[] { "Minimap", "MinimapTerrain" })
        {
            int camada = LayerMask.NameToLayer(nome);
            if (camada >= 0) mascara &= ~(1 << camada);
        }
        cam.cullingMask = mascara;
        // Centrada no quarto, e nao na origem: a parede norte tem duas fiadas, entao o
        // conteudo estende-se mais para cima do que para baixo.
        camGO.transform.position = new Vector3(0f, 0.5f, -10f);
        camGO.AddComponent<AudioListener>();

        var raiz = new GameObject("Interior_" + casa.Id);

        float w = casa.LarguraCelulas;
        float h = casa.AlturaCelulas;
        float x0 = -w / 2f, x1 = w / 2f;
        float y0 = -h / 2f, y1 = h / 2f;

        DesenharChao(raiz.transform, paredes, x0, y0, x1, y1);
        DesenharParedes(raiz.transform, paredes, x0, y0, x1, y1);
        Mobilar(raiz.transform, moveis, casa, x0, y0, x1, y1);
        ColocarSaidaEChegada(raiz.transform, casa, y0);
        ColocarDono(raiz.transform, casa, x0, y1);
    }

    private static void DesenharChao(Transform pai, Dictionary<string, Sprite> paredes,
                                     float x0, float y0, float x1, float y1)
    {
        // Tiles escolhidos por MEDICAO, nao a olho: percorridos os 99 tiles da folha e
        // guardados os que sao 100% opacos e de cor uniforme. #43/#44/#52/#53 dao
        // rgb(0,55 0,48 0,39) — o castanho quente da tabua de madeira.
        var chao = Achar(paredes, "walls_floor_43", "walls_floor_44", "walls_floor_52");
        if (chao == null) return;

        var grupo = new GameObject("Chao");
        grupo.transform.SetParent(pai, false);

        // Abaixo da parede (-1000), que por sua vez fica abaixo de tudo o que o Y-sort
        // produz. Ver o comentario em DesenharParedes.
        for (float x = x0 + 0.5f; x < x1; x += 1f)
            for (float y = y0 + 0.5f; y < y1; y += 1f)
                Pintar(grupo.transform, chao, new Vector2(x, y), 1.02f, -1100, false);
    }

    private static void DesenharParedes(Transform pai, Dictionary<string, Sprite> paredes,
                                        float x0, float y0, float x1, float y1)
    {
        // #54/#55/#56: rgb(0,38 0,41 0,46), o cinza-azulado da pedra. Medido, nao
        // adivinhado — a primeira tentativa usou indices a olho e saiu a folha inteira
        // repetida como se fosse um tile.
        var pedra = Achar(paredes, "walls_floor_54", "walls_floor_55", "walls_floor_56");
        if (pedra == null) return;

        var grupo = new GameObject("Paredes");
        grupo.transform.SetParent(pai, false);

        // SEM Y-sort nas paredes.
        //
        // Uma parede nao e um objeto que se contorne: esta sempre no limite do
        // compartimento, e o jogador nunca passa por tras dela.
        //
        // ⚠️ O valor tem de ser MUITO negativo, e nao 1. O YSortSprite converte o Y do
        // mundo em sortingOrder NEGATIVO (quanto mais acima no ecra, mais baixo o
        // numero): as prateleiras encostadas ao fundo ficam em -155, o lojista em -130.
        // Com a parede em 1 — acima de todos eles — ela desenhava-se por cima das
        // prateleiras e da propria Isabela, e a loja parecia vazia. -1000 poe a parede
        // abaixo de qualquer coisa que o Y-sort produza, e acima do chao (0).
        const int OrdemParede = -1000;

        // Norte com duas fiadas: da altura a parede de fundo, senao o quarto le-se
        // como um tabuleiro visto de cima.
        for (float x = x0 + 0.5f; x < x1; x += 1f)
        {
            Pintar(grupo.transform, pedra, new Vector2(x, y1 + 0.5f), 1.02f, OrdemParede, false);
            Pintar(grupo.transform, pedra, new Vector2(x, y1 + 1.5f), 1.02f, OrdemParede, false);
        }
        for (float x = x0 + 0.5f; x < x1; x += 1f)
            Pintar(grupo.transform, pedra, new Vector2(x, y0 - 0.5f), 1.02f, OrdemParede, false);
        for (float y = y0 - 0.5f; y <= y1 + 1.5f; y += 1f)
        {
            Pintar(grupo.transform, pedra, new Vector2(x0 - 0.5f, y), 1.02f, OrdemParede, false);
            Pintar(grupo.transform, pedra, new Vector2(x1 + 0.5f, y), 1.02f, OrdemParede, false);
        }

        // Um colisor por lado, nao um por tile: 4 caixas contra ~40.
        Barreira(grupo.transform, "Muro_N", new Vector2(0f, y1 + 1f), new Vector2(x1 - x0 + 2f, 1f));
        Barreira(grupo.transform, "Muro_S", new Vector2(0f, y0 - 0.5f), new Vector2(x1 - x0 + 2f, 1f));
        Barreira(grupo.transform, "Muro_O", new Vector2(x0 - 0.5f, 0f), new Vector2(1f, y1 - y0 + 3f));
        Barreira(grupo.transform, "Muro_E", new Vector2(x1 + 0.5f, 0f), new Vector2(1f, y1 - y0 + 3f));
    }

    private static void Barreira(Transform pai, string nome, Vector2 centro, Vector2 tamanho)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        go.transform.position = centro;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = tamanho;
    }

    private static void Mobilar(Transform pai, Dictionary<string, Sprite> moveis, Casa casa,
                                float x0, float y0, float x1, float y1)
    {
        var grupo = new GameObject("Moveis");
        grupo.transform.SetParent(pai, false);

        // Encostado a parede do fundo, mas DENTRO do chao: a 0,9 do limite os moveis
        // altos entravam por baixo da fiada de parede.
        float topo = y1 - 1.2f;

        switch (casa.Tema)
        {
            // Indices conferidos contra a folha: #0 cama grande, #10 prateleira com
            // mercadoria, #11 e #12 estantes vazias, #13 estante de livros, #15 mesa
            // redonda com cadeiras, #23 mesa quadrada, #27 e #36 tapetes, #7 armario,
            // #22 suporte de armas, #38 mesa comprida.
            case "loja":
                // Prateleira cheia ao fundo e balcao ao centro: le-se como loja sem
                // precisar de texto nenhum.
                PorMovel(grupo.transform, moveis, "Interior_10", new Vector2(-1.4f, topo), 1.5f);
                PorMovel(grupo.transform, moveis, "Interior_11", new Vector2(1.4f, topo), 1.4f);
                PorMovel(grupo.transform, moveis, "Interior_13", new Vector2(3.4f, topo), 1.3f);
                PorMovel(grupo.transform, moveis, "Interior_23", new Vector2(0f, y0 + 1.7f), 1.1f);
                PorMovel(grupo.transform, moveis, "Interior_4", new Vector2(x1 - 1.2f, y0 + 1.5f), 0.7f);
                break;

            case "oficina":
                PorMovel(grupo.transform, moveis, "Interior_11", new Vector2(-2.2f, topo), 1.4f);
                PorMovel(grupo.transform, moveis, "Interior_22", new Vector2(1.2f, topo), 1.0f);
                PorMovel(grupo.transform, moveis, "Interior_7", new Vector2(3.2f, topo), 1.2f);
                PorMovel(grupo.transform, moveis, "Interior_23", new Vector2(0f, y0 + 1.7f), 1.1f);
                PorMovel(grupo.transform, moveis, "Interior_4", new Vector2(x0 + 1.2f, y0 + 1.5f), 0.7f);
                break;

            case "salao":
                // A casa maior da vila: tapete e mesa ao centro, estantes ao fundo.
                PorMovel(grupo.transform, moveis, "Interior_36", new Vector2(0f, -0.2f), 2.4f);
                PorMovel(grupo.transform, moveis, "Interior_15", new Vector2(0f, 0.3f), 1.7f);
                PorMovel(grupo.transform, moveis, "Interior_13", new Vector2(-3.2f, topo), 1.3f);
                PorMovel(grupo.transform, moveis, "Interior_11", new Vector2(3.2f, topo), 1.4f);
                PorMovel(grupo.transform, moveis, "Interior_0", new Vector2(-4.2f, y0 + 2.0f), 1.6f);
                break;

            default:
                PorMovel(grupo.transform, moveis, "Interior_0", new Vector2(x0 + 1.4f, topo - 0.4f), 1.6f);
                PorMovel(grupo.transform, moveis, "Interior_23", new Vector2(1.8f, y0 + 1.9f), 1.1f);
                PorMovel(grupo.transform, moveis, "Interior_11", new Vector2(2.8f, topo), 1.4f);
                PorMovel(grupo.transform, moveis, "Interior_40", new Vector2(x1 - 1.0f, y0 + 1.3f), 0.5f);
                break;
        }
    }

    private static void PorMovel(Transform pai, Dictionary<string, Sprite> moveis, string chave,
                                 Vector2 pos, float alturaAlvo)
    {
        Sprite sp;
        if (!moveis.TryGetValue(chave, out sp)) return;
        Pintar(pai, sp, pos, alturaAlvo, 2, true);
    }

    private static void ColocarSaidaEChegada(Transform pai, Casa casa, float y0)
    {
        var saida = new GameObject("Porta_Saida");
        saida.transform.SetParent(pai, false);
        saida.transform.position = new Vector2(0f, y0 - 0.2f);

        var col = saida.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.4f, 0.8f);

        // Uma porta VISIVEL na parede sul.
        //
        // O Lucas jogou a build e nao percebeu como sair: a saida era um colisor
        // invisivel no chao. O emblema "E" do InteractionPromptUI so aparece quando ja
        // se esta em cima dela — nao diz onde ela esta. Uma porta desenhada na parede
        // le-se de longe, e e o que qualquer jogo 2D faz.
        var props = CarregarFolha("Assets/Texture/Extra/TX Props with Shadow.png");
        if (props.TryGetValue("TX Props with Shadow_9", out var spPorta))
        {
            var arte = new GameObject("Arte_Saida");
            arte.transform.SetParent(saida.transform, false);
            var sr = arte.AddComponent<SpriteRenderer>();
            sr.sprite = spPorta;
            sr.sortingLayerName = "Default";
            // Acima da parede (-1000), abaixo de tudo o que anda.
            sr.sortingOrder = -900;
            float k = 1.1f / (spPorta.rect.height / spPorta.pixelsPerUnit);
            arte.transform.localScale = new Vector3(k, k, 1f);
            // Na parede, nao no chao: a fiada sul comeca meia celula abaixo de y0.
            arte.transform.localPosition = new Vector3(0f, -0.35f, 0f);
        }

        var porta = saida.AddComponent<DoorInteractable>();
        var so = new SerializedObject(porta);
        so.FindProperty("cenaDestino").stringValue = "SampleScene";
        so.FindProperty("pontoDeChegada").stringValue = "Porta_" + casa.Id;
        so.FindProperty("nomeDoLocal").stringValue = "Sair";
        so.ApplyModifiedProperties();

        var chegada = new GameObject("Chegada_Entrada");
        chegada.transform.SetParent(pai, false);
        chegada.transform.position = new Vector2(0f, y0 + 0.9f);
        var ponto = chegada.AddComponent<PlayerSpawnPoint>();
        var so2 = new SerializedObject(ponto);
        so2.FindProperty("nome").stringValue = "Entrada";
        so2.FindProperty("ehPadrao").boolValue = true;
        so2.ApplyModifiedProperties();
    }

    private static void ColocarDono(Transform pai, Casa casa, float x0, float y1)
    {
        if (string.IsNullOrEmpty(casa.Dono)) return;

        var prefab = Resources.Load<GameObject>("Prefabs/NPCs/" + casa.Dono);
        if (prefab == null)
        {
            Debug.LogWarning("[Interiores] Nao achei o prefab do NPC '" + casa.Dono + "'.");
            return;
        }

        var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pai);
        npc.name = casa.Dono;
        npc.transform.position = new Vector3(0f, y1 - 2.2f, 0f);
    }

    private static Sprite Achar(Dictionary<string, Sprite> folha, params string[] chaves)
    {
        foreach (var k in chaves)
        {
            Sprite s;
            if (folha.TryGetValue(k, out s)) return s;
        }
        // Sem recurso silencioso.
        //
        // A versao anterior devolvia "o primeiro sprite que houvesse" quando nenhuma
        // chave batia. Como walls_floor.png nao estava fatiado, esse primeiro sprite
        // era a FOLHA INTEIRA — e os interiores sairam com a imagem toda repetida
        // celula a celula, um mosaico de paredes e portas. Nao houve erro nenhum:
        // pareceu arte mal escolhida, e nao um sprite em falta.
        Debug.LogError("[Interiores] Nenhuma destas chaves existe na folha: " +
                       string.Join(", ", chaves) + ". A folha esta fatiada?");
        return null;
    }

    private static GameObject Pintar(Transform pai, Sprite sp, Vector2 pos, float alturaAlvo,
                                     int ordem, bool ySort)
    {
        var go = new GameObject(sp.name);
        go.transform.SetParent(pai, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = ordem;

        float baseAltura = sp.rect.height / sp.pixelsPerUnit;
        float k = alturaAlvo / baseAltura;
        go.transform.localScale = new Vector3(k, k, 1f);
        go.transform.position = pos;

        if (ySort)
        {
            var ys = go.AddComponent<SowurShield.Core.YSortSprite>();
            // Cenario estatico: ordenar uma vez chega. Dez cenas cheias de moveis com
            // LateUpdate por frame seria desperdicio puro.
            var so = new SerializedObject(ys);
            so.FindProperty("continuous").boolValue = false;
            so.ApplyModifiedProperties();
        }

        return go;
    }

    private static void RegistarNasBuildSettings(List<string> caminhos)
    {
        var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        var jaLa = new HashSet<string>();
        foreach (var s in lista) jaLa.Add(s.path);

        int novas = 0;
        foreach (var c in caminhos)
        {
            if (jaLa.Contains(c)) continue;
            lista.Add(new EditorBuildSettingsScene(c, true));
            novas++;
        }

        EditorBuildSettings.scenes = lista.ToArray();
        Debug.Log("[Interiores] " + novas + " cenas acrescentadas as Build Settings.");
    }
}

}
