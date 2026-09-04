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
        // Medido na propria textura: a madeira vai ate x=55 de 512 (10.7%). Como o
        // Image e Sliced, os 32px do canto nao escalam e o resto da madeira (23px do
        // sprite) e esticado junto com o centro — num painel de 300px isso da 12px,
        // somando ~44px de madeira visivel de cada lado.
        //
        // Inset menor que isso desenha o texto SOBRE a madeira, onde ele fica
        // ilegivel. O rect continua reportando tudo dentro do painel, entao so o
        // screenshot acusa: os botoes pareciam certos (tem arte propria por baixo)
        // e apenas os textos puros ficavam cortados.
        private const float MolduraPx = 48f;

        private GameObject root;
        private TextMeshProUGUI statusText;
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
            float y = -MolduraPx;

            y = AdicionarTitulo(painel.transform, y, "Editor de Mapa");
            y = AdicionarSecao(painel.transform, y, "Terreno");
            y = AdicionarBotoesDeTile(painel.transform, y);
            y = AdicionarSecao(painel.transform, y, "Ferramenta");
            y = AdicionarBotoesDePincel(painel.transform, y);
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
            // 300x500. A altura foi medida, nao estimada: com 470 o rodape de status
            // terminava 18px abaixo da area legivel (painel menos os 34px de moldura
            // de cada lado). O rect dizia que estava tudo dentro; so o screenshot e a
            // medicao contra a MOLDURA acusaram.
            // 320x540: o conteudo mede ~404px e a moldura come 48px em cima e 48
            // embaixo — 404+96 = 500 nao deixava folga para o rodape de status.
            rt.sizeDelta = new Vector2(320f, 540f);

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

        private void AdicionarStatus(Transform pai, float y)
        {
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

            var textoGO = new GameObject("Label", typeof(TextMeshProUGUI));
            textoGO.transform.SetParent(go.transform, false);
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

            return botao;
        }

        private void AtualizarDestaques()
        {
            foreach (var par in tileButtons)
                PintarSelecao(par.Value, par.Key == mapEditor.selectedTileType);

            foreach (var par in brushButtons)
                PintarSelecao(par.Value, par.Key == mapEditor.selectedBrush);

            if (statusText != null)
            {
                int pintados = mapEditor.CurrentMapData != null
                    ? mapEditor.CurrentMapData.tileData.Count
                    : 0;
                statusText.text =
                    $"{NomeDoTipo(mapEditor.selectedTileType)} · {NomeDoPincel(mapEditor.selectedBrush)}\n" +
                    $"{pintados} célula(s) no mapa";
            }
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
