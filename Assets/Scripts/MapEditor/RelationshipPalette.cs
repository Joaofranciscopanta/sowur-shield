using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.Dialogue;
using SowurShield.UI;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Edita as preferencias de presente de cada personagem, com o jogo rodando.
    ///
    /// Ate agora isto so existia no Inspector: um array de strings por NPC, com os nomes
    /// digitados a mao. A reacao e casada por `item.itemName` em
    /// <see cref="NPCDialogueInteractable.GetReactionTo"/> -- o id interno, nunca o nome
    /// traduzido -- entao um nome mal escrito produz uma preferencia que **nunca dispara
    /// e nao da erro nenhum**. So se descobre testando presente por presente.
    ///
    /// Por isso o painel nao tem campo de texto: mostra os itens que existem e alterna
    /// entre Ama / Gosta / Odeia / Neutro ao clicar. E impossivel gravar um nome invalido.
    ///
    /// Fala com os assets pela interface <see cref="IRelationshipBridge"/>, pelo mesmo
    /// motivo que o painel de dialogo: gravar num prefab exige UnityEditor, que o
    /// assembly de runtime nao pode ver.
    ///
    /// O LORE (as 4 entradas do codex por personagem) ainda nao e editavel aqui: usa
    /// LocalizedString, entao gravar exige a mecanica de StringTable do
    /// DialogueRuntimeBridge -- e trabalho a parte, nao um esquecimento.
    /// </summary>
    [RequireComponent(typeof(RuntimeMapEditor))]
    public class RelationshipPalette : MonoBehaviour
    {
        // Medidas NO SCREENSHOT deste painel, nao na textura nem copiadas do de dialogo:
        // com Image.Sliced so a borda do 9-slice fica fixa, todo o resto da arte pertence
        // a faixa central, que e ESTICADA -- entao a moldura depende da largura do painel.
        //
        // Herdei 48px do DialoguePalette e o conteudo vazou: lendo a linha do meio do
        // painel ja desenhado (1200..1660), o creme comeca em 1259 e acaba em 1577. A
        // moldura e ASSIMETRICA -- 59 de um lado, 83 do outro -- e um valor unico deixava
        // os nomes cortados a esquerda e os botoes a passar por cima da madeira a direita.
        private const float MolduraEsqPx = 59f;
        private const float MolduraDirPx = 83f;
        private const float MolduraTopoPx = 130f;

        private const float Largura = 460f;
        private const float Altura = 900f;

        private RuntimeMapEditor mapEditor;
        private UITheme theme;

        private GameObject painel;
        private Transform listaPersonagens;
        private Transform listaItens;
        private TextMeshProUGUI rodape;
        private TextMeshProUGUI resumo;

        private readonly Dictionary<string, Button> botoesPersonagem = new();
        private readonly Dictionary<string, Button> botoesItem = new();

        private NPCDialogueInteractable selecionada;
        private readonly List<string> amados = new();
        private readonly List<string> gosta = new();
        private readonly List<string> odeia = new();

        private string mensagem;
        private float mensagemAte;

        public bool Aberta => painel != null && painel.activeSelf;

        /// <summary>Os quatro estados por que um item passa ao ser clicado.</summary>
        private enum Reacao { Neutro, Ama, Gosta, Odeia }

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
            if (Aberta) { Fechar(); return; }

            // O painel do codex ocupa a mesma faixa da tela (1200..1660 de 1920): abrir
            // um por cima do outro deixa os nomes de um a aparecer por tras dos botoes
            // do outro. O reciproco esta no CodexPalette.Alternar.
            GetComponent<CodexPalette>()?.Fechar();

            painel.SetActive(true);
            PreencherPersonagens();
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

        // ------------------------------------------------------------------
        // Construcao
        // ------------------------------------------------------------------

        private void Construir()
        {
            var canvasGO = new GameObject("RelationshipPaletteCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Acima da paleta de dialogo (520), que e a que este painel substitui na tela.
            canvas.sortingOrder = 530;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            painel = new GameObject("RelationshipPanel", typeof(Image));
            painel.transform.SetParent(canvasGO.transform, false);
            var rt = painel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            // A direita da paleta de dialogo (720 + 460 + margem).
            rt.anchoredPosition = new Vector2(1200f, -theme.spacingL);
            rt.sizeDelta = new Vector2(Largura, Altura);
            UIThemeStyler.StylePanel(painel, theme);

            float y = -MolduraTopoPx;

            var titulo = CriarTexto(painel.transform, "Presentes", theme.fontSizeH2,
                                    theme.headingOnLight, y, 34f);
            titulo.fontStyle = FontStyles.Bold;
            y -= 34f + theme.spacingS;

            var fechar = CriarBotao(painel.transform, "Fechar", y);
            fechar.onClick.AddListener(Fechar);
            y -= theme.buttonHeightSmall + theme.spacingS;

            CriarTexto(painel.transform, "Personagem", theme.fontSizeSmall,
                       theme.headingOnLight, y, 22f);
            y -= 22f + theme.spacingXS;
            listaPersonagens = CriarLista(painel.transform, y, 150f);
            y -= 150f + theme.spacingS;

            resumo = CriarTexto(painel.transform, "", theme.fontSizeCaption,
                                theme.textDark, y, 40f);
            resumo.alignment = TextAlignmentOptions.TopLeft;
            y -= 40f + theme.spacingXS;

            CriarTexto(painel.transform, "Itens - clique para alternar", theme.fontSizeSmall,
                       theme.headingOnLight, y, 22f);
            y -= 22f + theme.spacingXS;
            listaItens = CriarLista(painel.transform, y, 300f);
            y -= 300f + theme.spacingS;

            rodape = CriarTexto(painel.transform, "", theme.fontSizeCaption,
                                theme.textDark, y, 44f);
            rodape.alignment = TextAlignmentOptions.TopLeft;
            AtualizarRodape();
        }

        // ------------------------------------------------------------------
        // Listas
        // ------------------------------------------------------------------

        private void PreencherPersonagens()
        {
            LimparFilhos(listaPersonagens);
            botoesPersonagem.Clear();

            if (!RelationshipBridge.Disponivel)
            {
                AtualizarRodape();
                return;
            }

            foreach (var npc in RelationshipBridge.Atual.Personagens())
            {
                var capturado = npc;
                var botao = CriarLinha(listaPersonagens, npc.GetNPCDisplayName());
                botao.onClick.AddListener(() => Selecionar(capturado));
                botoesPersonagem[npc.GetNPCId()] = botao;
            }

            AtualizarDestaques();
            AtualizarRodape();
        }

        private void Selecionar(NPCDialogueInteractable npc)
        {
            selecionada = npc;

            amados.Clear();
            gosta.Clear();
            odeia.Clear();
            LerPreferencias(npc, amados, gosta, odeia);

            PreencherItens();
            AtualizarDestaques();
            AtualizarResumo();
            AtualizarRodape();
        }

        /// <summary>
        /// Le os tres arrays privados de uma personagem.
        ///
        /// Por reflexao porque os campos sao `[SerializeField] private` e nao ha getter --
        /// o unico acesso publico e o <see cref="NPCDialogueInteractable.GetReactionTo"/>,
        /// que responde por item e nao devolve as listas.
        /// </summary>
        private static void LerPreferencias(NPCDialogueInteractable npc,
                                            List<string> amados, List<string> gosta,
                                            List<string> odeia)
        {
            if (npc == null) return;

            const System.Reflection.BindingFlags priv =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var t = typeof(NPCDialogueInteractable);

            amados.AddRange((t.GetField("lovedGifts", priv)?.GetValue(npc) as string[])
                            ?? new string[0]);
            gosta.AddRange((t.GetField("likedGifts", priv)?.GetValue(npc) as string[])
                           ?? new string[0]);
            odeia.AddRange((t.GetField("dislikedGifts", priv)?.GetValue(npc) as string[])
                           ?? new string[0]);
        }

        private void PreencherItens()
        {
            LimparFilhos(listaItens);
            botoesItem.Clear();

            if (!RelationshipBridge.Disponivel || selecionada == null) return;

            foreach (var item in RelationshipBridge.Atual.Itens())
            {
                var nome = item.itemName;
                var botao = CriarLinha(listaItens, RotuloDoItem(nome));
                botao.onClick.AddListener(() => Alternar(nome));
                botoesItem[nome] = botao;
            }
        }

        /// <summary>O rotulo de um item na lista, com a marca da reacao atual.</summary>
        private string RotuloDoItem(string nome)
        {
            switch (ReacaoDe(nome))
            {
                case Reacao.Ama:   return "++  " + nome;
                case Reacao.Gosta: return "+  " + nome;
                case Reacao.Odeia: return "--  " + nome;
                default:           return "    " + nome;
            }
        }

        private Reacao ReacaoDe(string nome)
        {
            if (amados.Contains(nome)) return Reacao.Ama;
            if (gosta.Contains(nome)) return Reacao.Gosta;
            if (odeia.Contains(nome)) return Reacao.Odeia;
            return Reacao.Neutro;
        }

        /// <summary>
        /// Roda um item pelos quatro estados: Neutro, Ama, Gosta, Odeia, e volta a Neutro.
        ///
        /// Um clique so, em vez de tres botoes por linha: com 28 itens, tres botoes cada
        /// seriam 84 alvos numa lista de 300px.
        /// </summary>
        private void Alternar(string nome)
        {
            if (selecionada == null)
            {
                Avisar("Escolha uma personagem primeiro.");
                return;
            }

            var atual = ReacaoDe(nome);
            amados.Remove(nome);
            gosta.Remove(nome);
            odeia.Remove(nome);

            switch (atual)
            {
                case Reacao.Neutro: amados.Add(nome); break;
                case Reacao.Ama:    gosta.Add(nome);  break;
                case Reacao.Gosta:  odeia.Add(nome);  break;
                // Odeia volta a Neutro: nao entra em lista nenhuma.
            }

            Gravar();
            AtualizarRotuloDoItem(nome);
            AtualizarResumo();
        }

        private void Gravar()
        {
            if (!RelationshipBridge.Disponivel || selecionada == null) return;

            bool ok = RelationshipBridge.Atual.GravarPresentes(selecionada, amados, gosta, odeia);
            Avisar(ok
                ? $"Gravado em {selecionada.GetNPCDisplayName()}."
                : "Não foi possível gravar.");
        }

        private void AtualizarRotuloDoItem(string nome)
        {
            if (!botoesItem.TryGetValue(nome, out var botao) || botao == null) return;
            var rotulo = botao.GetComponentInChildren<TextMeshProUGUI>(true);
            if (rotulo != null) rotulo.text = RotuloDoItem(nome);
        }

        private void AtualizarResumo()
        {
            if (resumo == null) return;

            if (selecionada == null)
            {
                resumo.text = "";
                return;
            }

            // Marcas em ASCII, nao simbolos: o atlas da Nunito e Static e nao tem "♥" nem
            // "×", entao cada uso enchia o LiberationSans SDF - Fallback com glifos
            // dinamicos -- um asset do projeto a mudar sozinho a cada Play Mode.
            resumo.text =
                $"++ ama:   {string.Join(", ", amados)}\n" +
                $"+  gosta: {string.Join(", ", gosta)}\n" +
                $"-- odeia: {string.Join(", ", odeia)}";
        }

        private void AtualizarDestaques()
        {
            string id = selecionada != null ? selecionada.GetNPCId() : null;
            foreach (var par in botoesPersonagem)
                Pintar(par.Value, par.Key == id);
        }

        private void Pintar(Button botao, bool selecionado)
        {
            var img = botao != null ? botao.GetComponent<Image>() : null;
            if (img != null)
                img.color = selecionado ? theme.highlightGold : Color.white;
        }

        private void Avisar(string texto)
        {
            mensagem = texto;
            mensagemAte = Time.unscaledTime + 3f;
            AtualizarRodape();
        }

        private void AtualizarRodape()
        {
            if (rodape == null) return;

            if (mensagem != null) { rodape.text = mensagem; return; }

            if (!RelationshipBridge.Disponivel)
            {
                rodape.text = "Edição indisponível: a ponte de Editor não está registada.";
                return;
            }

            rodape.text = selecionada == null
                ? "Escolha uma personagem."
                : "Clique num item: ++ ama, + gosta, -- odeia, vazio neutro.";
        }

        // ------------------------------------------------------------------
        // Construcao de UI -- mesmos helpers do DialoguePalette
        // ------------------------------------------------------------------

        private static void LimparFilhos(Transform pai)
        {
            if (pai == null) return;
            // DestroyImmediate: Destroy e adiado para o fim do frame, e a lista seria
            // repovoada com os filhos antigos ainda presentes.
            for (int i = pai.childCount - 1; i >= 0; i--)
                DestroyImmediate(pai.GetChild(i).gameObject);
        }

        private Transform CriarLista(Transform pai, float y, float altura)
        {
            var viewport = new GameObject("Viewport", typeof(Image), typeof(Mask), typeof(ScrollRect));
            viewport.transform.SetParent(pai, false);
            var vrt = viewport.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0f, 1f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.pivot = new Vector2(0.5f, 1f);
            // ⚠️ A ORDEM importa: anchoredPosition e sizeDelta sao a MESMA informacao que
            // offsetMin/offsetMax, vista de outro angulo. Escrever os offsets primeiro e
            // depois tocar em anchoredPosition/sizeDelta recalcula os offsets a partir do
            // centro -- e uma moldura assimetrica (59 / 83) volta a ficar simetrica (71 /
            // 71). Medido: o conteudo saiu 12px a direita dos dois lados.
            //
            // Entao a posicao vertical vem primeiro, e os offsets horizontais por ultimo.
            vrt.anchoredPosition = new Vector2(0f, y);
            vrt.sizeDelta = new Vector2(vrt.sizeDelta.x, altura);
            vrt.offsetMin = new Vector2(MolduraEsqPx, vrt.offsetMin.y);
            vrt.offsetMax = new Vector2(-MolduraDirPx, vrt.offsetMax.y);

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
            // codigo nasce com (100,100), e com ancoras esticadas esse 100 e SOMADO a
            // largura do pai -- as linhas vazariam pela moldura.
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
            // Centrado, como as linhas do ObjectPalette. Com MidlineLeft o texto comeca na
            // borda do rect, e a placa dourada comeca mais para dentro: o nome ficava a
            // ESQUERDA da placa, sobre o creme, parecendo cortado. Medido: o glifo de
            // "Bento" tinha o centro em -131 do rect enquanto a placa comecava em -159.
            AdicionarRotulo(go.transform, rotulo, TextAlignmentOptions.Center);
            return b;
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
            // Posicao antes dos offsets -- ver o comentario em CriarLista.
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, altura);
            rt.offsetMin = new Vector2(MolduraEsqPx, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-MolduraDirPx, rt.offsetMax.y);

            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = conteudo;
            t.fontSize = tamanho;
            t.color = cor;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            if (theme != null && theme.fontPrimary != null) t.font = theme.fontPrimary;
            return t;
        }

        private Button CriarBotao(Transform pai, string rotulo, float y)
        {
            var go = new GameObject("Button", typeof(Image), typeof(Button));
            go.transform.SetParent(pai, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            // Posicao antes dos offsets -- ver o comentario em CriarLista.
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, theme.buttonHeightSmall);
            rt.offsetMin = new Vector2(MolduraEsqPx, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-MolduraDirPx, rt.offsetMax.y);

            var b = go.GetComponent<Button>();
            UIThemeStyler.StyleButton(b, theme);
            AdicionarRotulo(go.transform, rotulo, TextAlignmentOptions.Center);
            return b;
        }

        private void AdicionarRotulo(Transform pai, string rotulo,
                                     TextAlignmentOptions alinhamento)
        {
            var go = new GameObject("Label", typeof(TextMeshProUGUI));
            go.transform.SetParent(pai, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            // Inset contra a ARTE pintada, nao contra o rect: a arte do botao cobre
            // ~71% da largura, entao um rotulo colado ao rect vaza para fora da placa.
            rt.offsetMin = new Vector2(12f, 2f);
            rt.offsetMax = new Vector2(-12f, -2f);

            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = rotulo;
            t.fontSize = theme.fontSizeButton;
            t.color = theme.textDark;
            t.alignment = alinhamento;
            // Auto-size como no ObjectPalette: um nome longo ("WateringCan" com a marca da
            // reacao a frente) encolhe em vez de ser cortado pela placa.
            t.enableAutoSizing = true;
            t.fontSizeMin = theme.fontSizeCaption;
            t.fontSizeMax = theme.fontSizeButton;
            if (theme.fontPrimary != null) t.font = theme.fontPrimary;
        }
    }
}
