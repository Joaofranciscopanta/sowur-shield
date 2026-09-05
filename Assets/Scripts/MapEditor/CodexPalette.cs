using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.Dialogue;
using SowurShield.UI;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Edita o codex -- a bio e as 4 entradas de lore de cada personagem -- com o jogo
    /// rodando, nos tres idiomas.
    ///
    /// Ate agora isto so existia no Inspector, e o resultado esteve meses errado sem
    /// ninguem dar por isso: as 81 entradas (9 bios + 72 campos de lore) estavam em
    /// portugues CRU, com keyId 0. Como a build abre em `en`, quem jogava em ingles via
    /// o jogo todo traduzido e o codex em portugues. O campo legado nao da erro nenhum
    /// -- so nao traduz.
    ///
    /// Por isso este painel **nunca escreve texto cru**: tudo passa pelo
    /// <see cref="ICodexBridge"/>, que grava na StringTable e religa o LocalizedString
    /// pelo id. Uma entrada criada aqui ja nasce traduzivel.
    ///
    /// Botao, nunca atalho com Ctrl: em Play Mode a Game View disputa o foco com o
    /// Editor e o atalho vai parar a janela errada.
    /// </summary>
    [RequireComponent(typeof(RuntimeMapEditor))]
    public class CodexPalette : MonoBehaviour
    {
        // Mesma moldura medida no screenshot da RelationshipPalette: este painel tem a
        // mesma largura e a mesma arte, entao a faixa esticada e a mesma. Assimetrica
        // de proposito -- 59 de um lado, 83 do outro; um valor unico corta os nomes a
        // esquerda e deixa os botoes por cima da madeira a direita.
        private const float MolduraEsqPx = 59f;
        private const float MolduraDirPx = 83f;
        private const float MolduraTopoPx = 130f;

        private const float Largura = 460f;
        private const float Altura = 940f;

        // ⚠️ A base pintada NAO e a base do rect.
        //
        // Com Image.Sliced a faixa central da arte e esticada, entao a madeira de baixo
        // ocupa uma altura que o rect nao reporta: medido no screenshot, o rect vai ate
        // y=124 mas o creme util acaba ~150px acima disso. Duas rondas de "cabe pelo
        // rect" sairam com o espanhol e o rodape por cima da madeira.
        //
        // Este e o mesmo erro documentado em reference_unity_sliced_frame_measure_on_screen,
        // e a razao de o DialoguePalette ter MolduraTopoPx=130 em vez dos 32 do 9-slice.
        private const float MolduraBasePx = 150f;

        private RuntimeMapEditor mapEditor;
        private UITheme theme;

        private GameObject painel;
        private Transform listaPersonagens;
        private Transform listaEntradas;
        private TextMeshProUGUI rodape;
        private TextMeshProUGUI cabecalhoEntrada;

        private readonly Dictionary<string, Button> botoesPersonagem = new();
        private readonly List<Button> botoesEntrada = new();
        private readonly Dictionary<string, Button> botoesIdioma = new();

        private List<string> idiomas = new();
        private string idiomaAtivo = "pt";

        private TMP_InputField campoTitulo;
        private TMP_InputField campoCorpo;
        private TMP_InputField campoLimiar;

        private NPCDialogueInteractable selecionada;

        // -1 = a BIO; 0..n-1 = a entrada de lore correspondente. A bio e uma linha da
        // mesma lista de proposito: para quem edita, e mais um texto do personagem.
        private const int IndiceDaBio = -1;
        private int entradaSelecionada = IndiceDaBio;

        private string mensagem;
        private float mensagemAte;

        public bool Aberta => painel != null && painel.activeSelf;

        private void Start()
        {
            mapEditor = GetComponent<RuntimeMapEditor>();
            theme = UIThemeStyler.LoadTheme();

            Construir();
            painel.SetActive(false);
            mapEditor.OnEditorToggled += AoAlternarEditor;
        }

        private void OnDestroy()
        {
            if (mapEditor != null) mapEditor.OnEditorToggled -= AoAlternarEditor;
        }

        private void AoAlternarEditor(bool aberto)
        {
            if (!aberto) Fechar();
        }

        public void Alternar()
        {
            if (painel == null) return;
            if (painel.activeSelf) { Fechar(); return; }

            // Os dois painéis ocupam a mesma faixa da tela (1200..1660 de 1920), entao
            // nao cabem lado a lado -- abrir este sem fechar o outro deixava os nomes de
            // um a aparecer por tras dos botoes do outro.
            GetComponent<RelationshipPalette>()?.Fechar();

            painel.SetActive(true);
            MarcarIdiomaAtivo();
            PreencherPersonagens();
        }

        public void Fechar()
        {
            if (painel != null) painel.SetActive(false);
        }

        private void Update()
        {
            if (rodape == null) return;

            if (!string.IsNullOrEmpty(mensagem) && Time.unscaledTime > mensagemAte)
            {
                mensagem = null;
                AtualizarRodape();
            }
        }

        // ------------------------------------------------------------------
        // Construcao
        // ------------------------------------------------------------------

        private void Construir()
        {
            var canvasGO = new GameObject("CodexPaletteCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Acima da principal (500), objetos (510), dialogo (520) e presentes (530).
            canvas.sortingOrder = 540;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Sem isto fica no default 800x600 e o painel desenha ~1,8x maior em 1080p.
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            painel = new GameObject("CodexPanel", typeof(Image));
            painel.transform.SetParent(canvasGO.transform, false);
            var rt = painel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            // A paleta de presentes esta em 1200 e tem 460 de largura, entao ocupa ate
            // 1660 -- e 1920-460 = 1460 e o ultimo x onde este painel ainda cabe inteiro.
            // Nao ha faixa livre: os dois nao cabem lado a lado.
            //
            // Por isso o codex abre no MESMO sitio e fecha o de presentes ao abrir, em
            // vez de se sobrepor a ele (que foi o que o primeiro screenshot mostrou:
            // os nomes do painel de baixo a aparecer por tras dos botoes deste).
            rt.anchoredPosition = new Vector2(1200f, -theme.spacingL);
            rt.sizeDelta = new Vector2(Largura, Altura);
            UIThemeStyler.StylePanel(painel, theme);

            float y = -MolduraTopoPx;

            var titulo = CriarTexto(painel.transform, "Codex", theme.fontSizeH2,
                                    theme.headingOnLight, y, 34f);
            titulo.fontStyle = FontStyles.Bold;
            y -= 34f + theme.spacingS;

            var fechar = CriarBotao(painel.transform, "Fechar", y);
            fechar.onClick.AddListener(Fechar);
            y -= theme.buttonHeightSmall + theme.spacingS;

            // 96 em vez de 130: medido, o conteudo pedia 1096px de altura contra os
            // ~1064 que a tela permite. As duas listas rolam, entao encolhe-las custa
            // menos que encolher os campos de texto, que sao o que se vem editar.
            CriarTexto(painel.transform, "Personagem", theme.fontSizeSmall,
                       theme.headingOnLight, y, 22f);
            y -= 22f + theme.spacingXS;
            listaPersonagens = CriarLista(painel.transform, y, 96f);
            y -= 96f + theme.spacingS;

            CriarTexto(painel.transform, "Entrada", theme.fontSizeSmall,
                       theme.headingOnLight, y, 22f);
            y -= 22f + theme.spacingXS;
            listaEntradas = CriarLista(painel.transform, y, 96f);
            y -= 96f + theme.spacingS;

            cabecalhoEntrada = CriarTexto(painel.transform, "", theme.fontSizeCaption,
                                          theme.textDark, y, 18f);
            y -= 18f + theme.spacingXS;

            // Limiar: so faz sentido para lore, entao esconde-se quando a bio esta
            // escolhida (a bio aparece sempre, nao tem desbloqueio).
            // "menos 100" por extenso: o hífen ASCII nao tem glifo neste atlas e sumia,
            // deixando "( 100 a 100)" na tela -- que se le como um intervalo positivo.
            CriarTexto(painel.transform, "Relacionamento mínimo (menos 100 a 100)",
                       theme.fontSizeCaption, theme.headingOnLight, y, 16f);
            y -= 16f + theme.spacingXS;
            campoLimiar = CriarCampo(painel.transform, y, 26f);
            campoLimiar.lineType = TMP_InputField.LineType.SingleLine;
            campoLimiar.contentType = TMP_InputField.ContentType.IntegerNumber;
            // Recebe o TEXTO do evento em vez de reler o campo: o valor do evento e o
            // que o utilizador acabou de escrever, e reler o campo assume que ele ja
            // foi atualizado -- o que nem sempre e verdade.
            campoLimiar.onEndEdit.AddListener(GravarLimiar);
            y -= 26f + theme.spacingS;

            // UM idioma de cada vez, escolhido por botoes.
            //
            // A primeira versao mostrava os tres empilhados e simplesmente nao cabia:
            // tres rondas a encolher campos acabaram com cada corpo a cortar a segunda
            // linha ("...comenta tudo que passa" ficava sem o fim). Havia 3 rotulos, 3
            // titulos e 3 corpos a disputar ~500px.
            //
            // Com um idioma de cada vez sobra espaco para o corpo respirar, e comparar
            // traducoes passa a ser um clique -- que e o que se faz de vez em quando,
            // ao contrario de escrever, que e o que se faz sempre.
            idiomas = CodexBridge.Disponivel
                ? CodexBridge.Atual.Idiomas()
                : new List<string>();

            if (idiomas.Count > 0)
            {
                idiomaAtivo = idiomas[0];

                float larguraBotao = (Largura - MolduraEsqPx - MolduraDirPx
                                      - theme.spacingXS * (idiomas.Count - 1)) / idiomas.Count;
                for (int i = 0; i < idiomas.Count; i++)
                {
                    string id = idiomas[i];
                    // Codigo curto ("PT"), nao o nome por extenso: com tres botoes lado a
                    // lado sobram ~95px cada, e "Português"/"Español" pintavam-se para
                    // fora da placa dourada. O nome completo esta no rodape.
                    var b = CriarBotaoEm(painel.transform,
                                         id.ToUpperInvariant(),
                                         MolduraEsqPx + i * (larguraBotao + theme.spacingXS),
                                         larguraBotao, y);
                    b.onClick.AddListener(() => TrocarIdioma(id));
                    botoesIdioma[id] = b;
                }
                y -= theme.buttonHeightSmall + theme.spacingS;
            }

            CriarTexto(painel.transform, "Título", theme.fontSizeCaption,
                       theme.headingOnLight, y, 16f);
            y -= 16f + theme.spacingXS;
            campoTitulo = CriarCampo(painel.transform, y, 30f);
            campoTitulo.lineType = TMP_InputField.LineType.SingleLine;
            campoTitulo.onEndEdit.AddListener(t => GravarTitulo(idiomaAtivo, t));
            y -= 30f + theme.spacingS;

            CriarTexto(painel.transform, "Texto", theme.fontSizeCaption,
                       theme.headingOnLight, y, 16f);
            y -= 16f + theme.spacingXS;

            // O corpo fica com TODO o resto ate a moldura de baixo, menos o rodape.
            float alturaRodape = 34f;
            float alturaCorpo = Mathf.Max(
                60f, (Altura - MolduraBasePx) + y - alturaRodape - theme.spacingS);

            campoCorpo = CriarCampo(painel.transform, y, alturaCorpo);
            campoCorpo.onEndEdit.AddListener(t => GravarCorpo(idiomaAtivo, t));
            y -= alturaCorpo + theme.spacingS;

            rodape = CriarTexto(painel.transform, "", theme.fontSizeCaption,
                                theme.textDark, y, alturaRodape);
        }

        // ------------------------------------------------------------------
        // Listas
        // ------------------------------------------------------------------

        private void PreencherPersonagens()
        {
            // DestroyImmediate: o Destroy do Unity so ocorre no fim do frame, entao
            // limpar-e-reconstruir no mesmo frame ainda veria os filhos antigos.
            foreach (Transform filho in listaPersonagens)
                DestroyImmediate(filho.gameObject);
            botoesPersonagem.Clear();

            if (!CodexBridge.Disponivel)
            {
                AvisarSemPonte();
                return;
            }

            var todos = CodexBridge.Atual.Personagens();
            foreach (var npc in todos)
            {
                var alvo = npc;
                var botao = CriarLinha(listaPersonagens, npc.GetNPCDisplayName());
                botao.onClick.AddListener(() => Selecionar(alvo));
                botoesPersonagem[npc.gameObject.name] = botao;
            }

            if (todos.Count > 0 && selecionada == null) Selecionar(todos[0]);
            else AtualizarRodape();
        }

        private void Selecionar(NPCDialogueInteractable npc)
        {
            selecionada = npc;
            entradaSelecionada = IndiceDaBio;
            PreencherEntradas();
            CarregarCampos();
            AtualizarRodape();
        }

        private void PreencherEntradas()
        {
            foreach (Transform filho in listaEntradas)
                DestroyImmediate(filho.gameObject);
            botoesEntrada.Clear();

            if (selecionada == null || !CodexBridge.Disponivel) return;

            var bio = CriarLinha(listaEntradas, "Bio");
            bio.onClick.AddListener(() => SelecionarEntrada(IndiceDaBio));
            botoesEntrada.Add(bio);

            int n = CodexBridge.Atual.QuantasEntradas(selecionada);
            for (int i = 0; i < n; i++)
            {
                int indice = i;
                float limiar = CodexBridge.Atual.LerLimiar(selecionada, i);
                var b = CriarLinha(listaEntradas, $"Lore {i + 1}  (≥ {limiar:0})");
                b.onClick.AddListener(() => SelecionarEntrada(indice));
                botoesEntrada.Add(b);
            }
        }

        private void SelecionarEntrada(int indice)
        {
            entradaSelecionada = indice;
            CarregarCampos();
            AtualizarRodape();
        }

        // ------------------------------------------------------------------
        // Campos
        // ------------------------------------------------------------------

        private void CarregarCampos()
        {
            if (selecionada == null || !CodexBridge.Disponivel) return;

            bool ehBio = entradaSelecionada == IndiceDaBio;

            if (cabecalhoEntrada != null)
                cabecalhoEntrada.text = ehBio
                    ? "Bio — aparece sempre"
                    : $"Lore {entradaSelecionada + 1} de {CodexBridge.Atual.QuantasEntradas(selecionada)}";

            // O limiar so existe para lore: desativamos em vez de esconder, para o
            // painel nao mudar de altura ao trocar de entrada.
            if (campoLimiar != null)
            {
                campoLimiar.interactable = !ehBio;
                campoLimiar.text = ehBio
                    ? ""
                    : CodexBridge.Atual.LerLimiar(selecionada, entradaSelecionada).ToString("0");
            }

            if (campoTitulo != null)
            {
                // A bio nao tem titulo proprio; o campo nao lhe serve de nada.
                campoTitulo.interactable = !ehBio;
                campoTitulo.SetTextWithoutNotify(ehBio
                    ? ""
                    : CodexBridge.Atual.LerTitulo(selecionada, entradaSelecionada, idiomaAtivo));
            }

            if (campoCorpo != null)
            {
                campoCorpo.SetTextWithoutNotify(ehBio
                    ? CodexBridge.Atual.LerBio(selecionada, idiomaAtivo)
                    : CodexBridge.Atual.LerCorpo(selecionada, entradaSelecionada, idiomaAtivo));
            }
        }

        /// <summary>Troca o idioma que os campos mostram, sem mudar a entrada escolhida.</summary>
        private void TrocarIdioma(string idioma)
        {
            idiomaAtivo = idioma;
            MarcarIdiomaAtivo();
            CarregarCampos();
            AtualizarRodape();
        }

        /// <summary>
        /// Poe em negrito o idioma escolhido.
        ///
        /// Sem isto os tres botoes ficam iguais e nao ha nada na tela a dizer em que
        /// idioma se esta a escrever -- o que num painel que grava ao sair do campo e
        /// um bom modo de traduzir por cima do idioma errado.
        ///
        /// ⚠️ Negrito, nao cor: o Button usa ColorTint e o normalColor repinta o sprite,
        /// entao mexer na cor aqui apagaria a arte da placa.
        /// </summary>
        private void MarcarIdiomaAtivo()
        {
            foreach (var kv in botoesIdioma)
            {
                var rotulo = kv.Value.GetComponentInChildren<TextMeshProUGUI>();
                if (rotulo == null) continue;
                rotulo.fontStyle = kv.Key == idiomaAtivo ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        private void GravarTitulo(string idioma, string texto)
        {
            if (selecionada == null || entradaSelecionada == IndiceDaBio) return;
            bool ok = CodexBridge.Atual.EscreverTitulo(
                selecionada, entradaSelecionada, idioma, texto);
            Avisar(ok ? $"Título gravado ({idioma})." : "Não foi possível gravar o título.");
            if (ok) PreencherEntradas();   // o rotulo da linha mostra o limiar, nao o titulo
        }

        private void GravarCorpo(string idioma, string texto)
        {
            if (selecionada == null) return;

            bool ok = entradaSelecionada == IndiceDaBio
                ? CodexBridge.Atual.EscreverBio(selecionada, idioma, texto)
                : CodexBridge.Atual.EscreverCorpo(selecionada, entradaSelecionada, idioma, texto);

            Avisar(ok ? $"Texto gravado ({idioma})." : "Não foi possível gravar o texto.");
        }

        private void GravarLimiar(string texto)
        {
            if (selecionada == null || entradaSelecionada == IndiceDaBio) return;
            if (!float.TryParse(texto, out float valor))
            {
                Avisar("Valor inválido — use um número de -100 a 100.");
                CarregarCampos();
                return;
            }

            bool ok = CodexBridge.Atual.EscreverLimiar(selecionada, entradaSelecionada, valor);
            Avisar(ok ? "Relacionamento mínimo gravado." : "Não foi possível gravar.");
            if (ok) { PreencherEntradas(); CarregarCampos(); }
        }

        // ------------------------------------------------------------------
        // Rodape
        // ------------------------------------------------------------------

        private void Avisar(string texto)
        {
            mensagem = texto;
            mensagemAte = Time.unscaledTime + 3f;
            AtualizarRodape();
        }

        private void AvisarSemPonte()
        {
            if (rodape != null)
                rodape.text = "A ponte de Editor não está registrada — "
                            + "o codex não pode ser gravado nesta sessão.";
        }

        private void AtualizarRodape()
        {
            if (rodape == null) return;

            if (!string.IsNullOrEmpty(mensagem)) { rodape.text = mensagem; return; }

            if (selecionada == null) { rodape.text = "Escolha um personagem."; return; }

            rodape.text = $"{selecionada.GetNPCDisplayName()} — editando em "
                        + $"{CodexBridge.Atual.RotuloDoIdioma(idiomaAtivo)}. "
                        + "Grava ao sair do campo.";
        }

        // ------------------------------------------------------------------
        // Construtores de UI (mesmo padrao das outras paletas)
        // ------------------------------------------------------------------

        private Transform CriarLista(Transform pai, float y, float altura)
        {
            var viewport = new GameObject("Viewport", typeof(Image), typeof(Mask), typeof(ScrollRect));
            viewport.transform.SetParent(pai, false);
            var vrt = viewport.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0f, 1f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.pivot = new Vector2(0.5f, 1f);
            // offsetMin/Max e anchoredPosition/sizeDelta sao o MESMO estado: escrever os
            // offsets e depois tocar em anchoredPosition recalcula-os pelo centro e torna
            // a moldura assimetrica em simetrica. Por isso a ordem aqui nao muda.
            vrt.offsetMin = new Vector2(MolduraEsqPx, 0f);
            vrt.offsetMax = new Vector2(-MolduraDirPx, 0f);
            vrt.anchoredPosition = new Vector2(
                (MolduraEsqPx - MolduraDirPx) * 0.5f, y);
            vrt.sizeDelta = new Vector2(-(MolduraEsqPx + MolduraDirPx), altura);

            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.04f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var conteudo = new GameObject("Content", typeof(VerticalLayoutGroup),
                                          typeof(ContentSizeFitter));
            conteudo.transform.SetParent(viewport.transform, false);
            var crt = conteudo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero;
            // sizeDelta.x = 0 significa "a largura das ancoras". Um rect criado por
            // codigo nasce (100,100), e com ancoras esticadas esse 100 seria SOMADO.
            crt.sizeDelta = new Vector2(0f, crt.sizeDelta.y);

            var layout = conteudo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;

            conteudo.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = crt;
            scroll.viewport = vrt;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            return conteudo.transform;
        }

        private Button CriarLinha(Transform pai, string rotulo)
        {
            var go = new GameObject("Item", typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(pai, false);
            go.GetComponent<LayoutElement>().preferredHeight = 28f;

            var b = go.GetComponent<Button>();
            UIThemeStyler.StyleButton(b, theme);
            // CENTRADO, nao a esquerda: a placa pintada nao chega as bordas do rect
            // (medido: rect 1259..1577, mas a arte so preenche o meio), entao um rotulo
            // alinhado a esquerda desenha-se ANTES do ouro comecar e parece estar solto
            // atras do botao. E o mesmo motivo pelo qual a paleta de presentes centra.
            AdicionarRotulo(go.transform, rotulo, TextAlignmentOptions.Center);
            return b;
        }

        private TMP_InputField CriarCampo(Transform pai, float y, float altura)
        {
            var go = new GameObject("Campo", typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(pai, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(MolduraEsqPx, 0f);
            rt.offsetMax = new Vector2(-MolduraDirPx, 0f);
            rt.anchoredPosition = new Vector2((MolduraEsqPx - MolduraDirPx) * 0.5f, y);
            rt.sizeDelta = new Vector2(-(MolduraEsqPx + MolduraDirPx), altura);

            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.85f);

            var area = new GameObject("TextArea", typeof(RectMask2D));
            area.transform.SetParent(go.transform, false);
            var art = area.GetComponent<RectTransform>();
            art.anchorMin = Vector2.zero;
            art.anchorMax = Vector2.one;
            art.offsetMin = new Vector2(6f, 4f);
            art.offsetMax = new Vector2(-6f, -4f);

            var textoGO = new GameObject("Text", typeof(TextMeshProUGUI));
            textoGO.transform.SetParent(area.transform, false);
            var trt = textoGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var texto = textoGO.GetComponent<TextMeshProUGUI>();
            texto.fontSize = theme.fontSizeSmall;
            texto.color = theme.textDark;
            texto.alignment = TextAlignmentOptions.TopLeft;
            if (theme.fontPrimary != null) texto.font = theme.fontPrimary;

            var campo = go.GetComponent<TMP_InputField>();
            campo.textViewport = art;
            campo.textComponent = texto;
            campo.lineType = TMP_InputField.LineType.MultiLineNewline;
            campo.pointSize = theme.fontSizeSmall;
            return campo;
        }

        private TextMeshProUGUI CriarTexto(Transform pai, string conteudo, float tamanho,
                                           Color cor, float y, float altura)
        {
            var go = new GameObject("Texto", typeof(TextMeshProUGUI));
            go.transform.SetParent(pai, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(MolduraEsqPx, 0f);
            rt.offsetMax = new Vector2(-MolduraDirPx, 0f);
            rt.anchoredPosition = new Vector2((MolduraEsqPx - MolduraDirPx) * 0.5f, y);
            rt.sizeDelta = new Vector2(-(MolduraEsqPx + MolduraDirPx), altura);

            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = conteudo;
            t.fontSize = tamanho;
            t.color = cor;
            t.alignment = TextAlignmentOptions.TopLeft;
            if (theme.fontPrimary != null) t.font = theme.fontPrimary;
            return t;
        }

        private Button CriarBotao(Transform pai, string rotulo, float y)
        {
            var go = new GameObject("Botao", typeof(Image), typeof(Button));
            go.transform.SetParent(pai, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(MolduraEsqPx, 0f);
            rt.offsetMax = new Vector2(-MolduraDirPx, 0f);
            rt.anchoredPosition = new Vector2((MolduraEsqPx - MolduraDirPx) * 0.5f, y);
            rt.sizeDelta = new Vector2(-(MolduraEsqPx + MolduraDirPx),
                                       theme.buttonHeightSmall);

            var b = go.GetComponent<Button>();
            UIThemeStyler.StyleButton(b, theme);
            AdicionarRotulo(go.transform, rotulo, TextAlignmentOptions.Center);
            return b;
        }

        /// <summary>
        /// Botao numa posicao X propria, para os tres idiomas ficarem lado a lado.
        /// O CriarBotao normal ocupa a largura toda entre as molduras.
        /// </summary>
        private Button CriarBotaoEm(Transform pai, string rotulo, float x, float largura, float y)
        {
            var go = new GameObject("BotaoIdioma", typeof(Image), typeof(Button));
            go.transform.SetParent(pai, false);
            var rt = go.GetComponent<RectTransform>();
            // Ancora de PONTO (nao esticada): aqui a largura vem do sizeDelta, entao um
            // rect criado por codigo nao herda os 100 de largura por engano.
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(largura, theme.buttonHeightSmall);

            var b = go.GetComponent<Button>();
            UIThemeStyler.StyleButton(b, theme);
            AdicionarRotulo(go.transform, rotulo, TextAlignmentOptions.Center);
            return b;
        }

        private void AdicionarRotulo(Transform pai, string texto, TextAlignmentOptions alinhamento)
        {
            var go = new GameObject("Rotulo", typeof(TextMeshProUGUI));
            go.transform.SetParent(pai, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 0f);
            rt.offsetMax = new Vector2(-8f, 0f);

            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = texto;
            t.fontSize = theme.fontSizeSmall;
            t.color = theme.textDark;
            t.alignment = alinhamento;
            t.enableAutoSizing = true;
            t.fontSizeMin = theme.fontSizeCaption;
            t.fontSizeMax = theme.fontSizeSmall;
            if (theme.fontPrimary != null) t.font = theme.fontPrimary;
        }
    }
}
