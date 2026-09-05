using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.UI;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// A paleta do editor de mapa: constroi a propria interface em runtime.
    ///
    /// Existe um MapEditorUI de 531 linhas no projeto, mas ele espera ~40 referencias
    /// ligadas a mao num prefab que nunca foi criado (e usa `Dropdown` legado em vez
    /// de TMP). Montar esse prefab a mao seria mais trabalho — e mais fragil, porque
    /// um unico campo esquecido vira NullReference em runtime — do que construir a UI
    /// por codigo, que e o padrao que o resto deste projeto ja usa.
    ///
    /// Mostra apenas o que este tileset sabe desenhar. O enum ExtendedTileType tem 15
    /// valores, mas o dual grid do jogo e binario: oferecer Water ou Stone so ensinaria
    /// o usuario a clicar sem efeito, entao a paleta pergunta ao DualGridPaintAdapter.
    /// </summary>
    [RequireComponent(typeof(RuntimeMapEditor))]
    public class MapEditorPalette : MonoBehaviour
    {
        private RuntimeMapEditor mapEditor;
        private BrushTool brushTool;
        private UITheme theme;

        // A moldura VISIVEL do panel_wood_generic nao e a borda de 32px do 9-slice.
        // Medido no SCREENSHOT renderizado, nao na textura: com Image.Sliced so os
        // 32px da borda do 9-slice ficam fixos -- todo o resto da arte pertence a
        // faixa central, que e ESTICADA. Entao os 81px que a textura mostra nao
        // sao 81px na tela. Lendo a coluna do meio do painel ja desenhado, a
        // moldura vai ate ~103px e o creme estavel comeca a 126. Dai os 130.
        private const float MolduraPx = 48f;

        // Medido na textura do panel_wood_generic (512x512, borda 9-slice de 32): o
        // o interior creme estavel (F7E7CE) so comeca a 81px do topo. A madeira PINTADA passa muito da
        // borda do 9-slice, e o rect nao acusa nada -- com os 48 de MolduraPx o
        // titulo saia desenhado por cima da moldura, cortado ao meio.
        private const float MolduraTopoPx = 130f;

        /// <summary>O pe do painel e mais espesso que o topo: 165px medidos na tela.</summary>
        private const float MolduraBasePx = 168f;

        private GameObject root;
        private TextMeshProUGUI statusText;
        private Button botaoObjetos;
        private ObjectPalette objectPalette;
        private Button botaoDialogo;
        private DialoguePalette dialoguePalette;
        private Button botaoPresentes;
        private RelationshipPalette relationshipPalette;
        private Button botaoDesfazer;
        private TextMeshProUGUI rotuloPincel;
        private GameObject painelDeMapas;
        private Button botaoRefazer;
        private string mensagem;
        private float mensagemAte;
        private readonly Dictionary<ExtendedTileType, Button> tileButtons = new();
        private readonly Dictionary<BrushType, Button> brushButtons = new();

        // Os tipos que o dual grid desenha de verdade, na ordem em que aparecem.
        // As teclas 1 e 2 do RuntimeMapEditor seguem esta mesma ordem.
        private static readonly ExtendedTileType[] TiposPintaveis =
        {
            ExtendedTileType.Grass,
            ExtendedTileType.Dirt
        };

        private static readonly (BrushType tipo, string rotulo)[] Pinceis =
        {
            (BrushType.Paint,     "Pincel"),
            (BrushType.Line,      "Linha"),
            (BrushType.Rectangle, "Retângulo"),
            (BrushType.Fill,      "Balde"),
            (BrushType.Eraser,    "Borracha")
        };

        private void Start()
        {
            mapEditor = GetComponent<RuntimeMapEditor>();
            brushTool = GetComponent<BrushTool>();
            objectPalette = GetComponent<ObjectPalette>();
            dialoguePalette = GetComponent<DialoguePalette>();
            // Adicionado se faltar: assim o editor funciona numa cena montada antes de
            // este componente existir, sem exigir religacao a mao no Inspector.
            relationshipPalette = GetComponent<RelationshipPalette>();
            if (relationshipPalette == null)
                relationshipPalette = gameObject.AddComponent<RelationshipPalette>();
            theme = UIThemeStyler.LoadTheme();

            ConstruirUI();

            mapEditor.OnEditorToggled += AoAlternarEditor;
            root.SetActive(mapEditor.IsEditorActive);
        }

        private void OnDestroy()
        {
            if (mapEditor != null) mapEditor.OnEditorToggled -= AoAlternarEditor;
        }

        private void Update()
        {
            // As teclas 1 e 2 tambem trocam o tipo, entao a paleta nao pode confiar
            // apenas nos proprios cliques para saber o que esta selecionado.
            if (root != null && root.activeSelf) AtualizarDestaques();
        }

        private void AoAlternarEditor(bool aberto)
        {
            if (root != null) root.SetActive(aberto);
        }

        private void ConstruirUI()
        {
            var canvasGO = new GameObject("MapEditorPaletteCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Acima da HUD do jogo, que fica na faixa baixa de sortingOrder.
            canvas.sortingOrder = 500;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Sem isto o scaler fica no default 800x600 e desenha ~1.8x maior em 1080p.
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            root = canvasGO;

            var painel = CriarPainel(canvasGO.transform);
            // A margem de topo tem que ser a mesma moldura: o titulo estava sendo
            // desenhado sobre a madeira superior.
            float y = -MolduraTopoPx;

            y = AdicionarTitulo(painel.transform, y, "Editor de Mapa");
            y = AdicionarSecao(painel.transform, y, "Terreno");
            y = AdicionarBotoesDeTile(painel.transform, y);
            y = AdicionarSecao(painel.transform, y, "Ferramenta");
            y = AdicionarBotoesDePincel(painel.transform, y);
            y = AdicionarTamanhoDoPincel(painel.transform, y);
            y = AdicionarSecao(painel.transform, y, "Objetos");
            y = AdicionarBotaoDeObjetos(painel.transform, y);
            y = AdicionarSecao(painel.transform, y, "Ações");
            y = AdicionarBotoesDeAcao(painel.transform, y);
            AdicionarStatus(painel.transform, y);

            AtualizarDestaques();
        }

        private GameObject CriarPainel(Transform pai)
        {
            var painel = new GameObject("PalettePanel", typeof(Image));
            painel.transform.SetParent(pai, false);

            var rt = painel.GetComponent<RectTransform>();
            // Ancorado no canto superior esquerdo, longe da HUD do jogo.
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(theme.spacingL, -theme.spacingL);
            // A altura e medida, nao estimada: o conteudo tem que caber DENTRO da
            // area legivel, que e o painel menos MolduraPx em cima e embaixo. Uma
            // versao com 470 punha o rodape 18px para fora — e o rect reportava tudo
            // dentro do painel, entao so o screenshot acusava.
            //
            // 1056 cobre titulo + 2 tiles + 5 pinceis + 2 (objetos/dialogo) + 3 acoes
            // + area do pincel + carregar + status, com as duas faixas de moldura.
            // Medido em Play Mode: o conteudo termina 892px abaixo do topo. (Eram 810 quando o conteudo
            // comecava a 48px do topo; medida no screenshot, a moldura desenhada e de
            // 130 no topo e 168 na base -- o pe e mais espesso que o topo.)
            // Ao acrescentar, medir de novo.
            rt.sizeDelta = new Vector2(320f, 1056f);

            UIThemeStyler.StylePanel(painel, theme);
            return painel;
        }

        private float AdicionarTitulo(Transform pai, float y, string texto)
        {
            var t = CriarTexto(pai, texto, theme.fontSizeH2, theme.headingOnLight, y, 34f);
            t.fontStyle = FontStyles.Bold;
            return y - 34f - theme.spacingM;
        }

        private float AdicionarSecao(Transform pai, float y, string texto)
        {
            CriarTexto(pai, texto, theme.fontSizeSmall, theme.headingOnLight, y, 22f);
            return y - 22f - theme.spacingXS;
        }

        private float AdicionarBotoesDeTile(Transform pai, float y)
        {
            foreach (var tipo in TiposPintaveis)
            {
                // Defesa em profundidade: se alguem acrescentar um tipo aqui que o
                // tileset nao desenha, ele nao chega a virar botao.
                if (!DualGridPaintAdapter.IsPaintable(tipo)) continue;

                int indice = System.Array.IndexOf(TiposPintaveis, tipo) + 1;
                var botao = CriarBotao(pai, $"{indice}. {NomeDoTipo(tipo)}", y);
                var capturado = tipo;
                botao.onClick.AddListener(() => mapEditor.selectedTileType = capturado);
                tileButtons[tipo] = botao;
                y -= theme.buttonHeightSmall + theme.spacingXS;
            }
            return y - theme.spacingS;
        }

        private float AdicionarBotoesDePincel(Transform pai, float y)
        {
            foreach (var (tipo, rotulo) in Pinceis)
            {
                var botao = CriarBotao(pai, rotulo, y);
                var capturado = tipo;
                botao.onClick.AddListener(() =>
                {
                    mapEditor.selectedBrush = capturado;
                    if (brushTool != null) brushTool.SetBrushType(capturado);
                });
                brushButtons[tipo] = botao;
                y -= theme.buttonHeightSmall + theme.spacingXS;
            }
            return y - theme.spacingS;
        }

        /// <summary>
        /// Salvar, desfazer e refazer como BOTOES, nao atalhos.
        ///
        /// Ctrl+S / Ctrl+Z existiam so no teclado e foram REMOVIDOS: no Play Mode do
        /// Unity a Game View disputa o foco com o resto do Editor, e um atalho com
        /// Ctrl as vezes vai parar na janela errada — o usuario aperta e nada
        /// acontece. Botao sempre funciona.
        /// </summary>
        /// <summary>
        /// Abre a lista de objetos (painel proprio, com rolagem): sao 57 prefabs e
        /// eles nao cabem nesta paleta junto com os pinceis.
        /// </summary>
        /// <summary>
        /// A linha "Area: [-] 1 [+]".
        ///
        /// `BrushTool.SetBrushSize` (1 a 10) e `AreaDoPincel` ja existiam e o preview
        /// ja desenhava a area certa -- so a UI antiga (MapEditorUI, que nao esta em
        /// cena nenhuma) expunha isso. Sem o botao, pintar um mapa inteiro era celula
        /// a celula.
        /// </summary>
        private float AdicionarTamanhoDoPincel(Transform pai, float y)
        {
            var linha = new GameObject("LinhaAreaPincel", typeof(RectTransform));
            linha.transform.SetParent(pai, false);
            var rt = linha.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(MolduraPx, 0f);
            rt.offsetMax = new Vector2(-MolduraPx, 0f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, theme.buttonHeightSmall);

            float largura = 320f - MolduraPx * 2f;
            float lado = theme.buttonHeightSmall + 12f;

            var menos = CriarBotaoEm(linha.transform, "-", 0f, lado);
            menos.onClick.AddListener(() => AjustarPincel(-1));

            var mais = CriarBotaoEm(linha.transform, "+", largura - lado, lado);
            mais.onClick.AddListener(() => AjustarPincel(+1));

            var alvo = new GameObject("AreaPincel", typeof(TextMeshProUGUI));
            alvo.transform.SetParent(linha.transform, false);
            var art = alvo.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0f, 0f);
            art.anchorMax = new Vector2(0f, 1f);
            art.pivot = new Vector2(0f, 0.5f);
            art.anchoredPosition = new Vector2(lado + theme.spacingXS, 0f);
            art.sizeDelta = new Vector2(largura - (lado + theme.spacingXS) * 2f, 0f);

            rotuloPincel = alvo.GetComponent<TextMeshProUGUI>();
            rotuloPincel.fontSize = theme.fontSizeButton;
            rotuloPincel.color = theme.headingOnLight;
            rotuloPincel.alignment = TextAlignmentOptions.Center;
            if (theme.fontPrimary != null) rotuloPincel.font = theme.fontPrimary;
            AtualizarRotuloPincel();

            return y - theme.buttonHeightSmall - theme.spacingXS - theme.spacingS;
        }

        /// <summary>
        /// Degraus que mudam a area de verdade.
        ///
        /// O GetBrushArea faz `raio = brushSize / 2` em divisao INTEIRA, entao 2 e 3
        /// dao o mesmo raio 1 (5 celulas), 4 e 5 o mesmo raio 2 (13 celulas), e assim
        /// por diante. Andar de 1 em 1 faria metade dos cliques nao mudar nada na
        /// tela -- parece botao quebrado. Estes sao os valores que produzem areas
        /// distintas.
        /// </summary>
        private static readonly int[] DegrausDePincel = { 1, 2, 4, 6, 8, 10 };

        private void AjustarPincel(int passo)
        {
            if (brushTool == null) return;

            int atual = brushTool.BrushSize;
            int i = System.Array.IndexOf(DegrausDePincel, atual);
            if (i < 0)
            {
                // Valor fora da tabela: cair no degrau mais proximo antes de andar.
                i = 0;
                for (int k = 1; k < DegrausDePincel.Length; k++)
                    if (Mathf.Abs(DegrausDePincel[k] - atual) < Mathf.Abs(DegrausDePincel[i] - atual))
                        i = k;
            }

            int destino = DegrausDePincel[Mathf.Clamp(i + passo, 0, DegrausDePincel.Length - 1)];
            // O proprio SetBrushSize ja limita a 1..10; o rotulo le o valor de volta
            // para nunca mostrar um numero que a ferramenta nao aceitou.
            brushTool.SetBrushSize(destino);
            AtualizarRotuloPincel();
        }

        /// <summary>
        /// Mostra quantas celulas o clique pinta, nao o `brushSize` cru: o numero
        /// interno nao diz nada ao usuario, e por causa da divisao inteira ele nem
        /// corresponde ao tamanho ("3" pintaria 5 celulas, igual ao "2").
        /// </summary>
        private void AtualizarRotuloPincel()
        {
            if (rotuloPincel == null) return;
            if (brushTool == null) { rotuloPincel.text = "Área: 1 célula"; return; }

            int celulas = brushTool.AreaDoPincel(Vector3Int.zero).Count;
            rotuloPincel.text = "Área: " + celulas + (celulas == 1 ? " célula" : " células");
        }

        /// <summary>Botao numa posicao X fixa dentro de uma linha, para o "-" e o "+".</summary>
        private Button CriarBotaoEm(Transform pai, string rotulo, float x, float largura)
        {
            var go = new GameObject("Btn_" + rotulo, typeof(Image), typeof(Button));
            go.transform.SetParent(pai, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(largura, 0f);

            var b = go.GetComponent<Button>();
            UIThemeStyler.StyleButton(b, theme);
            AdicionarRotulo(go.transform, rotulo);
            return b;
        }

        private float AdicionarBotaoDeObjetos(Transform pai, float y)
        {
            botaoObjetos = CriarBotao(pai, "Colocar objeto...", y);
            botaoObjetos.onClick.AddListener(() => objectPalette?.Alternar());
            y -= theme.buttonHeightSmall + theme.spacingXS;

            botaoDialogo = CriarBotao(pai, "Editar diálogo...", y);
            botaoDialogo.onClick.AddListener(() => dialoguePalette?.Alternar());
            y -= theme.buttonHeightSmall + theme.spacingXS;

            botaoPresentes = CriarBotao(pai, "Editar presentes...", y);
            botaoPresentes.onClick.AddListener(() => relationshipPalette?.Alternar());
            return y - theme.buttonHeightSmall - theme.spacingXS - theme.spacingS;
        }

        private float AdicionarBotoesDeAcao(Transform pai, float y)
        {
            botaoDesfazer = CriarBotao(pai, "Desfazer", y);
            botaoDesfazer.onClick.AddListener(() => mapEditor.History?.Desfazer());
            y -= theme.buttonHeightSmall + theme.spacingXS;

            botaoRefazer = CriarBotao(pai, "Refazer", y);
            botaoRefazer.onClick.AddListener(() => mapEditor.History?.Refazer());
            y -= theme.buttonHeightSmall + theme.spacingXS;

            var salvar = CriarBotao(pai, "Salvar mapa", y);
            salvar.onClick.AddListener(Salvar);
            y -= theme.buttonHeightSmall + theme.spacingXS;

            // Havia "Salvar mapa" e nenhum jeito de reabrir: o mapa ia para o disco e
            // ficava inacessivel pelo proprio editor.
            var carregar = CriarBotao(pai, "Carregar mapa...", y);
            carregar.onClick.AddListener(AlternarListaDeMapas);
            y -= theme.buttonHeightSmall + theme.spacingXS;

            return y - theme.spacingS;
        }

        // =====================================================================
        // Carregar mapa
        // =====================================================================

        /// <summary>
        /// Abre a lista de mapas salvos. E reconstruida a cada abertura, e nao uma vez
        /// no Start: salvar um mapa novo com a paleta aberta tem que aparecer aqui sem
        /// reiniciar o jogo.
        /// </summary>
        private void AlternarListaDeMapas()
        {
            if (painelDeMapas != null)
            {
                Destroy(painelDeMapas);
                painelDeMapas = null;
                return;
            }
            ConstruirListaDeMapas();
        }

        private void ConstruirListaDeMapas()
        {
            var todos = mapEditor.MapasDisponiveis();

            // Os autosaves dominam a lista -- ja sao 5 de 7 arquivos depois de uma
            // tarde de uso, e so tendem a crescer. Ficam no fim, depois dos mapas
            // que o usuario salvou de proposito, que sao o que ele procura aqui.
            var mapas = new System.Collections.Generic.List<string>();
            var autosaves = new System.Collections.Generic.List<string>();
            foreach (var nome in todos)
            {
                if (nome.Contains("_autosave_")) autosaves.Add(nome);
                else mapas.Add(nome);
            }
            mapas.AddRange(autosaves);

            var canvasGO = new GameObject("MapListCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Acima das outras paletas (500/510), para nunca abrir por baixo delas.
            canvas.sortingOrder = 520;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            painelDeMapas = canvasGO;

            const float larguraLista = 560f;
            float alturaLista = MolduraTopoPx + 44f
                              + Mathf.Max(1, mapas.Count) * (theme.buttonHeightSmall + theme.spacingXS)
                              + theme.buttonHeightSmall + MolduraBasePx;
            // Nunca mais alta que a tela: com muitos mapas salvos o painel sairia por
            // baixo e os ultimos ficariam inalcancaveis.
            alturaLista = Mathf.Min(alturaLista, 1000f);

            var painel = new GameObject("MapListPanel", typeof(Image));
            painel.transform.SetParent(canvasGO.transform, false);
            var prt = painel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(larguraLista, alturaLista);
            UIThemeStyler.StylePanel(painel, theme);

            float y = -MolduraTopoPx;
            var titulo = CriarTextoEm(painel.transform, "Carregar mapa", theme.fontSizeH2,
                                      theme.headingOnLight, y, 34f, larguraLista);
            titulo.fontStyle = FontStyles.Bold;
            y -= 34f + theme.spacingS;

            if (mapas.Count == 0)
            {
                CriarTextoEm(painel.transform, "Nenhum mapa salvo ainda.",
                             theme.fontSizeSmall, theme.textDark, y, 30f, larguraLista);
                y -= 30f + theme.spacingXS;
            }
            else
            {
                foreach (var nome in mapas)
                {
                    var capturado = nome;
                    var b = CriarBotaoLargo(painel.transform, NomeCurto(nome), y, larguraLista);
                    b.onClick.AddListener(() => CarregarMapa(capturado));
                    y -= theme.buttonHeightSmall + theme.spacingXS;
                }
            }

            var fechar = CriarBotaoLargo(painel.transform, "Fechar", y, larguraLista);
            fechar.onClick.AddListener(AlternarListaDeMapas);
        }

        /// <summary>
        /// O nome como cabe na placa do botao.
        ///
        /// "New Map 2026-09-04_17-00_autosave_17-03" tem 39 caracteres e vaza pelos
        /// dois lados da arte, que pinta so parte do rect. O prefixo "New Map " nao
        /// distingue nada (todos tem) e a data ja esta no resto do nome; o sufixo de
        /// autosave vira um marcador curto. O arquivo no disco continua com o nome
        /// inteiro -- isto e so o rotulo.
        /// </summary>
        private static string NomeCurto(string nome)
        {
            string curto = nome.StartsWith("New Map ") ? nome.Substring(8) : nome;

            int i = curto.IndexOf("_autosave_");
            if (i >= 0)
            {
                // Fica "2026-09-04_17-00 (auto 17-03)": qual mapa, e de quando e a copia.
                string hora = curto.Substring(i + "_autosave_".Length);
                curto = curto.Substring(0, i) + " (auto " + hora + ")";
            }
            return curto;
        }

        private void CarregarMapa(string nome)
        {
            bool ok = mapEditor.CarregarMapaDoDisco(nome);
            mensagem = ok ? "Mapa carregado: " + nome
                          : "Nao consegui ler o mapa: " + nome;
            mensagemAte = Time.unscaledTime + 3f;

            AlternarListaDeMapas();   // fecha a lista
            AtualizarRotuloPincel();
        }

        /// <summary>Texto ancorado no topo de um painel de largura conhecida.</summary>
        private TextMeshProUGUI CriarTextoEm(Transform pai, string conteudo, float tamanho,
                                             Color cor, float y, float altura, float largura)
        {
            var go = new GameObject("Text", typeof(TextMeshProUGUI));
            go.transform.SetParent(pai, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(MolduraPx, 0f);
            rt.offsetMax = new Vector2(-MolduraPx, 0f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, altura);

            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = conteudo;
            t.fontSize = tamanho;
            t.color = cor;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            if (theme.fontPrimary != null) t.font = theme.fontPrimary;
            return t;
        }

        /// <summary>Botao que ocupa a largura util de um painel proprio.</summary>
        private Button CriarBotaoLargo(Transform pai, string rotulo, float y, float largura)
        {
            var go = new GameObject("Button_" + rotulo, typeof(Image), typeof(Button));
            go.transform.SetParent(pai, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(MolduraPx, 0f);
            rt.offsetMax = new Vector2(-MolduraPx, 0f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, theme.buttonHeightSmall);

            var b = go.GetComponent<Button>();
            UIThemeStyler.StyleButton(b, theme);
            AdicionarRotulo(go.transform, rotulo);
            return b;
        }

        private void Salvar()
        {
            mapEditor.SaveCurrentMap();
            // Sem retorno visivel o usuario clica de novo sem saber se funcionou.
            mensagem = "Mapa salvo em Assets/Maps";
            mensagemAte = Time.unscaledTime + 3f;
        }

        private void AdicionarStatus(Transform pai, float y)
        {
            // O status tem 2 linhas ("Grama - Pincel" + "N celula(s) no mapa"), entao
            // sao 40px que precisam caber ACIMA do pe do painel. Sem este empurrao a
            // segunda linha fica desenhada sobre a moldura de baixo.
            y += theme.spacingS;
            statusText = CriarTexto(pai, "", theme.fontSizeCaption, theme.textDark, y, 40f);
            statusText.alignment = TextAlignmentOptions.TopLeft;
        }

        private TextMeshProUGUI CriarTexto(Transform pai, string conteudo, float tamanho,
                                           Color cor, float y, float altura)
        {
            var go = new GameObject("Text", typeof(TextMeshProUGUI));
            go.transform.SetParent(pai, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            // A moldura de madeira pinta uma faixa larga; o texto precisa entrar por
            // dentro dela ou fica desenhado sobre a madeira e some.
            rt.offsetMin = new Vector2(MolduraPx, 0f);
            rt.offsetMax = new Vector2(-MolduraPx, 0f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, altura);

            var texto = go.GetComponent<TextMeshProUGUI>();
            texto.text = conteudo;
            texto.fontSize = tamanho;
            texto.color = cor;
            texto.alignment = TextAlignmentOptions.MidlineLeft;
            if (theme.fontPrimary != null) texto.font = theme.fontPrimary;
            return texto;
        }

        private Button CriarBotao(Transform pai, string rotulo, float y)
        {
            var go = new GameObject("Button_" + rotulo, typeof(Image), typeof(Button));
            go.transform.SetParent(pai, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(MolduraPx, 0f);
            rt.offsetMax = new Vector2(-MolduraPx, 0f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, theme.buttonHeightSmall);

            var botao = go.GetComponent<Button>();
            UIThemeStyler.StyleButton(botao, theme);
            AdicionarRotulo(go.transform, rotulo);
            return botao;
        }

        /// <summary>A legenda de um botao, com o inset da arte e auto-size.</summary>
        private void AdicionarRotulo(Transform pai, string rotulo)
        {
            var textoGO = new GameObject("Label", typeof(TextMeshProUGUI));
            textoGO.transform.SetParent(pai, false);
            var trt = textoGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            // A arte do botao pinta so parte do rect; a legenda entra por dentro.
            trt.offsetMin = new Vector2(theme.spacingM, 0f);
            trt.offsetMax = new Vector2(-theme.spacingM, 0f);

            var texto = textoGO.GetComponent<TextMeshProUGUI>();
            texto.text = rotulo;
            texto.fontSize = theme.fontSizeButton;
            texto.color = theme.textDark;
            texto.alignment = TextAlignmentOptions.Center;
            texto.enableAutoSizing = true;
            texto.fontSizeMin = theme.fontSizeCaption;
            texto.fontSizeMax = theme.fontSizeButton;
            if (theme.fontPrimary != null) texto.font = theme.fontPrimary;
        }

        private void AtualizarDestaques()
        {
            foreach (var par in tileButtons)
                PintarSelecao(par.Value, par.Key == mapEditor.selectedTileType);

            foreach (var par in brushButtons)
                PintarSelecao(par.Value, par.Key == mapEditor.selectedBrush);

            // Um botao que nao faz nada e pior que um botao ausente: o usuario clica,
            // nada acontece, e ele nao sabe se e o botao ou o editor que quebrou.
            var historico = mapEditor.History;
            if (botaoDesfazer != null)
                DefinirDisponibilidade(botaoDesfazer, historico != null && historico.PassosParaDesfazer > 0);
            if (botaoRefazer != null)
                DefinirDisponibilidade(botaoRefazer, historico != null && historico.PassosParaRefazer > 0);

            if (statusText != null)
            {
                int pintados = mapEditor.CurrentMapData != null
                    ? mapEditor.CurrentMapData.tileData.Count
                    : 0;

                // A confirmacao do save toma o lugar do status por alguns segundos.
                string linha = (mensagem != null && Time.unscaledTime < mensagemAte)
                    ? mensagem
                    : $"{pintados} célula(s) no mapa";

                statusText.text =
                    $"{NomeDoTipo(mapEditor.selectedTileType)} · {NomeDoPincel(mapEditor.selectedBrush)}\n" +
                    linha;
            }
        }

        /// <summary>
        /// Acinzenta em vez de esconder: um botao que some faz o painel saltar e o
        /// usuario perde a posicao dos outros.
        /// </summary>
        private void DefinirDisponibilidade(Button botao, bool disponivel)
        {
            botao.interactable = disponivel;
            var rotulo = botao.GetComponentInChildren<TextMeshProUGUI>();
            if (rotulo != null)
                rotulo.color = disponivel
                    ? theme.textDark
                    : new Color(theme.textDark.r, theme.textDark.g, theme.textDark.b, 0.4f);
        }

        private void PintarSelecao(Button botao, bool selecionado)
        {
            var img = botao.GetComponent<Image>();
            if (img == null) return;
            // highlightGold contra a madeira escura do botao; o branco e o estado normal.
            img.color = selecionado ? theme.highlightGold : Color.white;
        }

        private static string NomeDoTipo(ExtendedTileType tipo) => tipo switch
        {
            ExtendedTileType.Grass => "Grama",
            ExtendedTileType.Dirt  => "Terra",
            _ => tipo.ToString()
        };

        private static string NomeDoPincel(BrushType tipo) => tipo switch
        {
            BrushType.Paint     => "Pincel",
            BrushType.Line      => "Linha",
            BrushType.Rectangle => "Retângulo",
            BrushType.Fill      => "Balde",
            BrushType.Eraser    => "Borracha",
            BrushType.Circle    => "Círculo",
            _ => tipo.ToString()
        };
    }
}
