using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.UI;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// A lista de objetos que se pode colocar no mundo.
    ///
    /// Painel proprio, com rolagem, aberto por um botao da paleta principal: sao 57
    /// prefabs (34 decoracoes, 17 itens de chao, 4 arvores, 2 frutas) e uma lista
    /// fixa desse tamanho nao cabe na tela nem faz sentido misturada aos pinceis.
    ///
    /// Construida por codigo, como o resto da UI do editor.
    /// </summary>
    [RequireComponent(typeof(RuntimeMapEditor))]
    public class ObjectPalette : MonoBehaviour
    {
        // A mesma moldura medida na paleta principal: a madeira do panel_wood_generic
        // cobre ~44px de cada lado num painel desta largura, e inset menor desenha o
        // texto sobre ela.
        private const float MolduraPx = 48f;

        // Medido no SCREENSHOT renderizado, nao na textura: com Image.Sliced so os
        // 32px da borda do 9-slice ficam fixos -- todo o resto da arte pertence a
        // faixa central, que e ESTICADA. Entao os 81px que a textura mostra nao
        // sao 81px na tela. Lendo a coluna do meio do painel ja desenhado, a
        // moldura vai ate ~103px e o creme estavel comeca a 126. Dai os 130.
        private const float MolduraTopoPx = 130f;

        // A base da arte e MAIS espessa que o topo: medindo a mesma coluna do
        // screenshot de baixo para cima, o creme estavel comeca a 165px (contra 126
        // no topo) -- o painel tem um pe decorativo. Assumir simetria com o topo
        // deixava o rodape por baixo dele.
        private const float MolduraBasePx = 168f;
        private const float LarguraPainel = 340f;
        // 620 quando o conteudo comecava a 48px do topo e nao havia linha de tamanho.
        // Passou a comecar a 84 (moldura medida na textura) e ganhou a linha
        // "Tamanho", e as duas molduras medidas na TELA (130 no topo, 168 na base)
        // em vez dos 48 estimados.
        private const float AlturaPainel = 906f;

        private RuntimeMapEditor mapEditor;
        private ObjectPlacer placer;
        private UITheme theme;

        private GameObject painel;
        private readonly Dictionary<string, Button> botoes = new();
        private TextMeshProUGUI rodape;
        private TextMeshProUGUI rotuloTamanho;
        private TextMeshProUGUI rotuloGiro;

        public bool Aberta => painel != null && painel.activeSelf;

        private void Start()
        {
            mapEditor = GetComponent<RuntimeMapEditor>();
            placer = GetComponent<ObjectPlacer>();
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

        private void Update()
        {
            if (Aberta) AtualizarDestaques();
        }

        public void Alternar()
        {
            if (Aberta) Fechar();
            else painel.SetActive(true);
        }

        public void Fechar()
        {
            if (painel != null) painel.SetActive(false);
            // Fechar a lista tambem larga o objeto: deixar o modo de colocacao ligado
            // com a lista escondida faria o clique colocar arvores sem nada na tela
            // explicando por que o pincel parou de pintar.
            placer?.Selecionar(null);
        }

        private void Construir()
        {
            var canvasGO = new GameObject("ObjectPaletteCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Acima da paleta principal (500), para nunca ficar por baixo dela.
            canvas.sortingOrder = 510;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            painel = new GameObject("ObjectPanel", typeof(Image));
            painel.transform.SetParent(canvasGO.transform, false);
            var rt = painel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            // A direita da paleta principal (320 de largura + margens), para as duas
            // ficarem visiveis ao mesmo tempo.
            rt.anchoredPosition = new Vector2(360f, -theme.spacingL);
            rt.sizeDelta = new Vector2(LarguraPainel, AlturaPainel);
            UIThemeStyler.StylePanel(painel, theme);

            float y = -MolduraTopoPx;
            var titulo = CriarTexto(painel.transform, "Objetos", theme.fontSizeH2,
                                    theme.headingOnLight, y, 34f);
            titulo.fontStyle = FontStyles.Bold;
            y -= 34f + theme.spacingS;

            // "Fechar (voltar ao pincel)" nao cabia na largura pintada do botao: a arte
            // pinta so parte do rect e o rotulo vazava por cima da borda.
            var fechar = CriarBotao(painel.transform, "Voltar ao pincel", y);
            fechar.onClick.AddListener(Fechar);
            y -= theme.buttonHeightSmall + theme.spacingS;

            // Tamanho do objeto: por BOTAO, nunca por atalho -- no Play Mode a Game
            // View disputa foco com o Editor e a tecla vai para a janela errada.
            ConstruirLinhaDeTamanho(painel.transform, y);
            y -= theme.buttonHeightSmall + theme.spacingS;

            ConstruirLinhaDeGiro(painel.transform, y);
            y -= theme.buttonHeightSmall + theme.spacingS;

            float alturaLista = AlturaPainel + y - MolduraBasePx - 46f;  // 46 = rodape
            ConstruirLista(painel.transform, y, alturaLista);

            rodape = CriarTexto(painel.transform, "Clique para colocar · direito remove",
                                theme.fontSizeCaption, theme.textDark,
                                -(AlturaPainel - MolduraBasePx - 40f), 40f);
        }

        /// <summary>
        /// A linha "Tamanho: [-] 1,0x [+]".
        ///
        /// O ObjectSpawnData sempre teve um campo `scale` e o carregador sempre o
        /// aplicou; faltava alguem para escolher o valor. Fica acima da lista porque
        /// vale para o proximo objeto colocado, seja ele qual for.
        /// </summary>
        private void ConstruirLinhaDeTamanho(Transform pai, float y)
        {
            var linha = new GameObject("LinhaTamanho", typeof(RectTransform));
            linha.transform.SetParent(pai, false);
            var rt = linha.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(MolduraPx, 0f);
            rt.offsetMax = new Vector2(-MolduraPx, 0f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, theme.buttonHeightSmall);

            float largura = LarguraPainel - MolduraPx * 2f;
            float ladoBotao = theme.buttonHeightSmall + 12f;

            var menos = CriarBotaoEm(linha.transform, "-", 0f, ladoBotao);
            menos.onClick.AddListener(() => { placer?.AjustarEscala(-1); AtualizarRotuloTamanho(); });

            var mais = CriarBotaoEm(linha.transform, "+", largura - ladoBotao, ladoBotao);
            mais.onClick.AddListener(() => { placer?.AjustarEscala(+1); AtualizarRotuloTamanho(); });

            var alvo = new GameObject("Tamanho", typeof(TextMeshProUGUI));
            alvo.transform.SetParent(linha.transform, false);
            var art = alvo.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0f, 0f);
            art.anchorMax = new Vector2(0f, 1f);
            art.pivot = new Vector2(0f, 0.5f);
            art.anchoredPosition = new Vector2(ladoBotao + theme.spacingXS, 0f);
            art.sizeDelta = new Vector2(largura - (ladoBotao + theme.spacingXS) * 2f, 0f);

            rotuloTamanho = alvo.GetComponent<TextMeshProUGUI>();
            rotuloTamanho.fontSize = theme.fontSizeButton;
            rotuloTamanho.color = theme.headingOnLight;
            rotuloTamanho.alignment = TextAlignmentOptions.Center;
            if (theme.fontPrimary != null) rotuloTamanho.font = theme.fontPrimary;
            AtualizarRotuloTamanho();
        }

        /// <summary>
        /// A linha "[Girar 90] 0 [Espelhar]".
        ///
        /// `ObjectSpawnData.rotation` ja existia e o carregador ja o aplicava; era o
        /// placer que gravava zero fixo. Girar de 90 em 90 porque a grade e quadrada:
        /// angulo livre so desalinha do chao pintado.
        /// </summary>
        private void ConstruirLinhaDeGiro(Transform pai, float y)
        {
            var linha = new GameObject("LinhaGiro", typeof(RectTransform));
            linha.transform.SetParent(pai, false);
            var rt = linha.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(MolduraPx, 0f);
            rt.offsetMax = new Vector2(-MolduraPx, 0f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, theme.buttonHeightSmall);

            float largura = LarguraPainel - MolduraPx * 2f;
            float ladoBotao = (largura - 52f) * 0.5f;   // 52 = espaco do rotulo do angulo

            // Rotulos curtos de proposito: a arte do botao e 5:1 e pinta so parte do
            // rect, entao "Espelhar" por extenso vaza da placa pintada num botao
            // desta largura. O rotulo do meio ja diz o angulo e o estado do espelho.
            var girar = CriarBotaoEm(linha.transform, "Girar", 0f, ladoBotao);
            girar.onClick.AddListener(() => { placer?.Girar(); AtualizarRotuloGiro(); });

            var espelhar = CriarBotaoEm(linha.transform, "Virar", largura - ladoBotao, ladoBotao);
            espelhar.onClick.AddListener(() => { placer?.AlternarEspelho(); AtualizarRotuloGiro(); });

            var alvo = new GameObject("Angulo", typeof(TextMeshProUGUI));
            alvo.transform.SetParent(linha.transform, false);
            var art = alvo.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0f, 0f);
            art.anchorMax = new Vector2(0f, 1f);
            art.pivot = new Vector2(0f, 0.5f);
            art.anchoredPosition = new Vector2(ladoBotao, 0f);
            art.sizeDelta = new Vector2(52f, 0f);

            rotuloGiro = alvo.GetComponent<TextMeshProUGUI>();
            rotuloGiro.fontSize = theme.fontSizeCaption;
            rotuloGiro.color = theme.headingOnLight;
            rotuloGiro.alignment = TextAlignmentOptions.Center;
            if (theme.fontPrimary != null) rotuloGiro.font = theme.fontPrimary;
            AtualizarRotuloGiro();
        }

        private void AtualizarRotuloGiro()
        {
            if (rotuloGiro == null) return;
            float g = placer != null ? placer.Rotacao : 0f;
            bool esp = placer != null && placer.Espelhado;
            rotuloGiro.text = g.ToString("0") + "°" + (esp ? " V" : "");
        }

        private void AtualizarRotuloTamanho()
        {
            if (rotuloTamanho == null) return;
            float e = placer != null ? placer.Escala : 1f;
            rotuloTamanho.text = "Tamanho: " + e.ToString("0.##") + "x";
        }

        /// <summary>Botao numa posicao X fixa dentro da linha, para o "-" e o "+".</summary>
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

        /// <summary>
        /// A lista rolavel. 57 itens nao cabem sem rolagem, e um ScrollRect com
        /// VerticalLayoutGroup e o caminho padrao do Unity para isso.
        /// </summary>
        private void ConstruirLista(Transform pai, float y, float altura)
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

            // O Mask precisa de um Image para recortar, mas ele nao deve aparecer.
            var fundo = viewport.GetComponent<Image>();
            fundo.color = new Color(0f, 0f, 0f, 0.04f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var conteudo = new GameObject("Content", typeof(VerticalLayoutGroup),
                                          typeof(ContentSizeFitter));
            conteudo.transform.SetParent(viewport.transform, false);
            var crt = conteudo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero;
            // sizeDelta.x = 0 significa "a largura das ancoras", ou seja a do viewport.
            // Um RectTransform criado por codigo nasce com sizeDelta (100, 100), e com
            // ancoras esticadas esse 100 e SOMADO a largura do pai — as linhas ficavam
            // 344px num painel de 340 e vazavam pela moldura.
            crt.sizeDelta = new Vector2(0f, crt.sizeDelta.y);

            var layout = conteudo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = theme.spacingXS;
            layout.childForceExpandHeight = false;
            // childControlWidth precisa estar ligado, senao childForceExpandWidth
            // nao faz nada e as linhas nascem com largura zero.
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
            scroll.scrollSensitivity = 24f;

            string categoriaAtual = null;
            foreach (var entrada in PrefabCatalog.Tudo())
            {
                // Cabecalho por categoria: 57 nomes numa lista corrida sao dificeis
                // de varrer com o olho.
                if (entrada.Categoria != categoriaAtual)
                {
                    categoriaAtual = entrada.Categoria;
                    var cab = CriarLinhaDeTexto(conteudo.transform, NomeDaCategoria(categoriaAtual));
                    cab.fontStyle = FontStyles.Bold;
                }

                var caminho = entrada.Caminho;
                var botao = CriarLinhaDeBotao(conteudo.transform, entrada.Nome);
                botao.onClick.AddListener(() => placer?.Selecionar(caminho));
                botoes[caminho] = botao;
            }
        }

        private void AtualizarDestaques()
        {
            var selecionado = placer != null ? placer.CaminhoSelecionado : null;
            foreach (var par in botoes)
            {
                var img = par.Value.GetComponent<Image>();
                if (img != null)
                    img.color = par.Key == selecionado ? theme.highlightGold : Color.white;
            }

            if (rodape != null)
            {
                rodape.text = string.IsNullOrEmpty(selecionado)
                    ? "Escolha um objeto para colocar"
                    : "Clique para colocar · direito remove";
            }

            // O tamanho pode mudar sem passar pelos botoes (mapa carregado, por
            // exemplo), entao o rotulo se mantem em dia aqui tambem.
            if (rotuloTamanho != null)
            {
                AtualizarRotuloTamanho();
                AtualizarRotuloGiro();
            }
        }

        private static string NomeDaCategoria(string pasta) => pasta switch
        {
            "Decorations" => "Decoração",
            "FruitTrees"  => "Árvores frutíferas",
            "Fruits"      => "Frutas",
            "GroundItems" => "Itens de chão",
            _ => pasta
        };

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
            AdicionarRotulo(go.transform, rotulo);
            return b;
        }

        /// <summary>Linha da lista: altura vem do LayoutElement, nao de ancoras.</summary>
        private Button CriarLinhaDeBotao(Transform pai, string rotulo)
        {
            var go = new GameObject("Item_" + rotulo, typeof(Image), typeof(Button),
                                    typeof(LayoutElement));
            go.transform.SetParent(pai, false);
            go.GetComponent<LayoutElement>().preferredHeight = theme.buttonHeightSmall;

            var b = go.GetComponent<Button>();
            UIThemeStyler.StyleButton(b, theme);
            AdicionarRotulo(go.transform, rotulo);
            return b;
        }

        private TextMeshProUGUI CriarLinhaDeTexto(Transform pai, string texto)
        {
            var go = new GameObject("Header_" + texto,
                                    typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(pai, false);
            go.GetComponent<LayoutElement>().preferredHeight = 24f;

            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = texto;
            t.fontSize = theme.fontSizeSmall;
            t.color = theme.headingOnLight;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            if (theme.fontPrimary != null) t.font = theme.fontPrimary;
            return t;
        }

        private void AdicionarRotulo(Transform pai, string rotulo)
        {
            var go = new GameObject("Label", typeof(TextMeshProUGUI));
            go.transform.SetParent(pai, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            // A arte do botao pinta so parte do rect; a legenda entra por dentro.
            rt.offsetMin = new Vector2(theme.spacingM, 0f);
            rt.offsetMax = new Vector2(-theme.spacingM, 0f);

            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = rotulo;
            t.fontSize = theme.fontSizeButton;
            t.color = theme.textDark;
            t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = true;
            t.fontSizeMin = theme.fontSizeCaption;
            t.fontSizeMax = theme.fontSizeButton;
            if (theme.fontPrimary != null) t.font = theme.fontPrimary;
        }
    }
}
