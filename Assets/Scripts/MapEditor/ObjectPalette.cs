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
        private const float LarguraPainel = 340f;
        private const float AlturaPainel = 620f;

        private RuntimeMapEditor mapEditor;
        private ObjectPlacer placer;
        private UITheme theme;

        private GameObject painel;
        private readonly Dictionary<string, Button> botoes = new();
        private TextMeshProUGUI rodape;

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

            float y = -MolduraPx;
            var titulo = CriarTexto(painel.transform, "Objetos", theme.fontSizeH2,
                                    theme.headingOnLight, y, 34f);
            titulo.fontStyle = FontStyles.Bold;
            y -= 34f + theme.spacingS;

            var fechar = CriarBotao(painel.transform, "Fechar (voltar ao pincel)", y);
            fechar.onClick.AddListener(Fechar);
            y -= theme.buttonHeightSmall + theme.spacingS;

            float alturaLista = AlturaPainel + y - MolduraPx - 46f;  // 46 = rodape
            ConstruirLista(painel.transform, y, alturaLista);

            rodape = CriarTexto(painel.transform, "Clique para colocar · direito remove",
                                theme.fontSizeCaption, theme.textDark,
                                -(AlturaPainel - MolduraPx - 40f), 40f);
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
