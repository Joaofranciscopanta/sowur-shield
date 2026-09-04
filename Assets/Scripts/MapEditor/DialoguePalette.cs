using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.Dialogue;
using SowurShield.UI;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Editar dialogo com o jogo rodando: escolher a arvore, o no, e digitar a fala
    /// nos tres idiomas.
    ///
    /// **Por que isto persiste** (nao era obvio, e foi medido antes de construir):
    /// as tabelas de localizacao sao assets, e o Unity restaura assets ao sair do
    /// Play Mode — uma fala digitada e testada se perderia. A ponte chama
    /// `AssetDatabase.SaveAssets()` logo apos cada escrita, e o texto sobrevive ao
    /// stop. Verificado: o valor foi parar dentro do arquivo .asset.
    ///
    /// Fala com os assets pela interface IDialogueBridge. A implementacao vive num
    /// assembly Editor-only, porque escrever nas tabelas exige
    /// UnityEditor.Localization — este painel e runtime e nao pode referenciar
    /// Editor sem quebrar a build de jogador. Sem ponte registrada, o painel avisa
    /// em vez de quebrar.
    /// </summary>
    [RequireComponent(typeof(RuntimeMapEditor))]
    public class DialoguePalette : MonoBehaviour
    {
        // A mesma moldura medida nos outros paineis: a madeira do panel_wood_generic
        // cobre ~44px de cada lado, e inset menor desenha o texto sobre ela.
        private const float MolduraPx = 48f;

        // Medido no SCREENSHOT renderizado, nao na textura: com Image.Sliced so os
        // 32px da borda do 9-slice ficam fixos -- todo o resto da arte pertence a
        // faixa central, que e ESTICADA. Entao os 81px que a textura mostra nao
        // sao 81px na tela. Lendo a coluna do meio do painel ja desenhado, a
        // moldura vai ate ~103px e o creme estavel comeca a 126. Dai os 130.
        private const float MolduraTopoPx = 130f;

        private const float Largura = 460f;
        // 1022: medido, nao estimado. Com 700 o campo de espanhol e o rodape
        // terminavam 76px abaixo da area legivel; com 800 ainda faltavam 6px. Os
        // 820 que resultaram disso valiam enquanto a moldura era tida como 48 de
        // cada lado; medida NA TELA ela e 130 em cima e 168 embaixo, dai +202.
        // O rect reportava tudo dentro em todas as vezes — so a medicao contra a
        // MOLDURA acusou.
        private const float Altura = 1022f;

        private RuntimeMapEditor mapEditor;
        private UITheme theme;

        private GameObject painel;
        private Transform listaArvores;
        private Transform listaNos;
        private readonly Dictionary<DialogueTree, Button> botoesArvore = new();
        private readonly Dictionary<string, Button> botoesNo = new();
        private readonly Dictionary<string, TMP_InputField> campos = new();
        private TextMeshProUGUI rodape;

        private DialogueTree arvoreSelecionada;
        private DialogueNode noSelecionado;
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
            if (Aberta) Fechar();
            else { painel.SetActive(true); PreencherArvores(); }
        }

        public void Fechar()
        {
            if (painel != null) painel.SetActive(false);
        }

        private void Update()
        {
            if (Aberta && rodape != null && mensagem != null && Time.unscaledTime > mensagemAte)
            {
                mensagem = null;
                AtualizarRodape();
            }
        }

        private void Construir()
        {
            var canvasGO = new GameObject("DialoguePaletteCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Acima da paleta principal (500) e da de objetos (510).
            canvas.sortingOrder = 520;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            painel = new GameObject("DialoguePanel", typeof(Image));
            painel.transform.SetParent(canvasGO.transform, false);
            var rt = painel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            // A direita das outras duas paletas (320 + 340 + margens).
            rt.anchoredPosition = new Vector2(720f, -theme.spacingL);
            rt.sizeDelta = new Vector2(Largura, Altura);
            UIThemeStyler.StylePanel(painel, theme);

            float y = -MolduraTopoPx;

            var titulo = CriarTexto(painel.transform, "Diálogo", theme.fontSizeH2,
                                    theme.headingOnLight, y, 34f);
            titulo.fontStyle = FontStyles.Bold;
            y -= 34f + theme.spacingS;

            var fechar = CriarBotao(painel.transform, "Fechar", y);
            fechar.onClick.AddListener(Fechar);
            y -= theme.buttonHeightSmall + theme.spacingS;

            CriarTexto(painel.transform, "Conversa", theme.fontSizeSmall,
                       theme.headingOnLight, y, 22f);
            y -= 22f + theme.spacingXS;
            listaArvores = CriarLista(painel.transform, y, 150f);
            y -= 150f + theme.spacingS;

            CriarTexto(painel.transform, "Fala", theme.fontSizeSmall,
                       theme.headingOnLight, y, 22f);
            y -= 22f + theme.spacingXS;
            listaNos = CriarLista(painel.transform, y, 110f);
            y -= 110f + theme.spacingS;

            y = CriarCamposDeIdioma(painel.transform, y);

            rodape = CriarTexto(painel.transform, "", theme.fontSizeCaption,
                                theme.textDark, y, 44f);
            rodape.alignment = TextAlignmentOptions.TopLeft;
            AtualizarRodape();
        }

        private float CriarCamposDeIdioma(Transform pai, float y)
        {
            if (!DialogueBridge.Disponivel) return y;

            foreach (var idioma in DialogueBridge.Atual.Idiomas())
            {
                CriarTexto(pai, DialogueBridge.Atual.RotuloDoIdioma(idioma),
                           theme.fontSizeCaption, theme.headingOnLight, y, 20f);
                y -= 20f;

                var campo = CriarCampo(pai, y, 58f);
                var capturado = idioma;
                // onEndEdit, nao onValueChanged: gravar a cada tecla salvaria o asset
                // dezenas de vezes por palavra.
                campo.onEndEdit.AddListener(texto => Gravar(capturado, texto));
                campos[idioma] = campo;
                y -= 58f + theme.spacingXS;
            }
            return y - theme.spacingS;
        }

        private void PreencherArvores()
        {
            LimparFilhos(listaArvores);
            botoesArvore.Clear();

            if (!DialogueBridge.Disponivel) { AtualizarRodape(); return; }

            foreach (var arvore in DialogueBridge.Atual.Arvores())
            {
                var capturada = arvore;
                var botao = CriarLinha(listaArvores, arvore.name);
                botao.onClick.AddListener(() => SelecionarArvore(capturada));
                botoesArvore[arvore] = botao;
            }
        }

        private void SelecionarArvore(DialogueTree arvore)
        {
            arvoreSelecionada = arvore;
            noSelecionado = null;

            LimparFilhos(listaNos);
            botoesNo.Clear();

            foreach (var no in arvore.nodes)
            {
                var capturado = no;
                // O rotulo mostra o inicio da fala, nao so o id: "start" e "topic_about"
                // nao dizem nada sobre o que o NPC diz ali.
                var botao = CriarLinha(listaNos, RotuloDoNo(no));
                botao.onClick.AddListener(() => SelecionarNo(capturado));
                botoesNo[no.nodeId] = botao;
            }

            AtualizarDestaques();
            LimparCampos();
            AtualizarRodape();
        }

        private void SelecionarNo(DialogueNode no)
        {
            noSelecionado = no;
            AtualizarDestaques();

            if (!DialogueBridge.Disponivel) return;
            foreach (var par in campos)
                par.Value.SetTextWithoutNotify(DialogueBridge.Atual.LerTexto(no, par.Key));

            AtualizarRodape();
        }

        private void Gravar(string idioma, string texto)
        {
            if (!DialogueBridge.Disponivel || arvoreSelecionada == null || noSelecionado == null)
                return;

            bool ok = DialogueBridge.Atual.EscreverTexto(
                arvoreSelecionada, noSelecionado, idioma, texto);

            mensagem = ok
                ? $"Salvo em {DialogueBridge.Atual.RotuloDoIdioma(idioma)}"
                : "Não foi possível salvar (o nó tem ID?)";
            mensagemAte = Time.unscaledTime + 3f;
            AtualizarRodape();

            // O rotulo do no mostra o inicio da fala; editar o texto muda o rotulo.
            if (ok && botoesNo.TryGetValue(noSelecionado.nodeId, out var botao))
            {
                var rotulo = botao.GetComponentInChildren<TextMeshProUGUI>();
                if (rotulo != null) rotulo.text = RotuloDoNo(noSelecionado);
            }
        }

        private string RotuloDoNo(DialogueNode no)
        {
            string previa = DialogueBridge.Disponivel
                ? DialogueBridge.Atual.LerTexto(no, "pt")
                : "";
            if (string.IsNullOrEmpty(previa)) return no.nodeId;

            if (previa.Length > 28) previa = previa.Substring(0, 28) + "…";
            return $"{no.nodeId}: {previa}";
        }

        private void AtualizarDestaques()
        {
            foreach (var par in botoesArvore)
                Pintar(par.Value, par.Key == arvoreSelecionada);

            foreach (var par in botoesNo)
                Pintar(par.Value, noSelecionado != null && par.Key == noSelecionado.nodeId);
        }

        private void Pintar(Button botao, bool selecionado)
        {
            var img = botao.GetComponent<Image>();
            if (img != null) img.color = selecionado ? theme.highlightGold : Color.white;
        }

        private void AtualizarRodape()
        {
            if (rodape == null) return;

            if (!DialogueBridge.Disponivel)
            {
                rodape.text = "Edição de diálogo indisponível: só funciona no Editor.";
                return;
            }
            if (mensagem != null) { rodape.text = mensagem; return; }
            if (arvoreSelecionada == null) { rodape.text = "Escolha uma conversa."; return; }
            if (noSelecionado == null) { rodape.text = "Escolha uma fala."; return; }

            rodape.text = $"{arvoreSelecionada.name} · {noSelecionado.nodeId}\n" +
                          "O texto é salvo ao sair do campo.";
        }

        private void LimparCampos()
        {
            foreach (var par in campos) par.Value.SetTextWithoutNotify("");
        }

        private static void LimparFilhos(Transform pai)
        {
            if (pai == null) return;
            // DestroyImmediate: Destroy e adiado para o fim do frame, e a lista
            // seria repovoada com os filhos antigos ainda presentes.
            for (int i = pai.childCount - 1; i >= 0; i--)
                DestroyImmediate(pai.GetChild(i).gameObject);
        }

        // ---------- construcao de UI ----------

        private Transform CriarLista(Transform pai, float y, float altura)
        {
            var viewport = new GameObject("Viewport", typeof(Image), typeof(Mask), typeof(ScrollRect));
            viewport.transform.SetParent(pai, false);
            var vrt = viewport.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0f, 1f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.pivot = new Vector2(0.5f, 1f);
            vrt.offsetMin = new Vector2(MolduraPx, 0f);
            vrt.offsetMax = new Vector2(-MolduraPx, 0f);
            vrt.anchoredPosition = new Vector2(0f, y);
            vrt.sizeDelta = new Vector2(vrt.sizeDelta.x, altura);

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
            // sizeDelta.x = 0 = "a largura das ancoras". Um RectTransform criado por
            // codigo nasce com (100,100), e com ancoras esticadas esse 100 e SOMADO
            // a largura do pai — as linhas vazariam pela moldura.
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
            AdicionarRotulo(go.transform, rotulo, TextAlignmentOptions.MidlineLeft);
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
            rt.offsetMin = new Vector2(MolduraPx, 0f);
            rt.offsetMax = new Vector2(-MolduraPx, 0f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, altura);

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

            var b = go.GetComponent<Button>();
            UIThemeStyler.StyleButton(b, theme);
            AdicionarRotulo(go.transform, rotulo, TextAlignmentOptions.Center);
            return b;
        }

        private void AdicionarRotulo(Transform pai, string rotulo, TextAlignmentOptions alinhamento)
        {
            var go = new GameObject("Label", typeof(TextMeshProUGUI));
            go.transform.SetParent(pai, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            // A arte do botao pinta so parte do rect; a legenda entra por dentro.
            rt.offsetMin = new Vector2(theme.spacingS, 0f);
            rt.offsetMax = new Vector2(-theme.spacingS, 0f);

            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = rotulo;
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
