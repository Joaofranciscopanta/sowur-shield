using System.Collections.Generic;
using UnityEngine;

namespace SowurShield.UI
{
    /// <summary>
    /// Decide ONDE cada janela aberta se encaixa na tela, para que duas nunca se tapem.
    ///
    /// Isto nasceu de um relato de jogo: abrir a caixa de venda (ou o comedouro) e carregar
    /// em Tab punha os dois inventarios exatamente por cima um do outro. Medido, os tres
    /// paineis ocupam a mesma faixa central -- o do jogador em x 412..1508 e y 224..753, a
    /// caixa em 730..1190 x 305..775, o comedouro em 695..1225 x 344..736.
    ///
    /// A correcao podia ser dar a cada painel uma posicao fixa que nao colidisse, mas isso
    /// obriga a reservar um lugar a mao sempre que nascer uma janela nova. Aqui a posicao e
    /// CALCULADA a partir de quantas janelas estao abertas: sozinha fica centrada; duas
    /// dividem a vertical; tres ou mais empilham com o mesmo passo. Uma janela nova so
    /// precisa de se registar.
    ///
    /// O dock nao abre nem fecha nada -- so posiciona quem se anuncia.
    /// </summary>
    public class WindowDock : MonoBehaviour
    {
        public static WindowDock Instance { get; private set; }

        /// <summary>Uma janela ancorada, na ordem em que foi aberta.</summary>
        private readonly List<RectTransform> abertas = new List<RectTransform>();

        /// <summary>Onde cada janela estava antes de o dock lhe tocar.</summary>
        private readonly Dictionary<RectTransform, Vector2> posicaoOriginal =
            new Dictionary<RectTransform, Vector2>();

        /// <summary>Folga entre duas janelas empilhadas, em pixels de referencia.</summary>
        private const float Respiro = 24f;

        /// <summary>Altura do ecra de referencia do CanvasScaler do projeto.</summary>
        private const float AlturaDeReferencia = 1080f;

        /// <summary>Margem que a pilha nunca invade, em cima e em baixo.</summary>
        private const float MargemDoEcra = 40f;

        /// <summary>
        /// Cria o dock sozinho ao carregar a cena.
        ///
        /// Assim nao e preciso arrastar um GameObject para cada cena que tenha janelas --
        /// e o mesmo padrao do LocalizationManager. Sem dock registado os paineis nao
        /// quebram: cada Anunciar sai por um null-check e a janela fica onde estava.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Instalar()
        {
            if (FindFirstObjectByType<WindowDock>(FindObjectsInactive.Include) != null) return;

            var go = new GameObject(nameof(WindowDock));
            go.AddComponent<WindowDock>();
            // Sobrevive a troca de cena. `AfterSceneLoad` dispara uma unica vez, na
            // PRIMEIRA cena -- que no jogo montado e o MainMenu, nao a SampleScene. Sem
            // isto o dock morria ao carregar a quinta, e o jogo ficava sem dock nenhum:
            // medido na build com um selfcheck, "[DOCK] instalado=False". Em Play Mode o
            // engano nao aparecia, porque ali a primeira cena JA e a do jogo.
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            // Tambem para o caso de alguem por o componente numa cena a mao.
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Anuncia que uma janela abriu. O dock recoloca esta e as que ja estavam.
        /// </summary>
        public void Registrar(RectTransform janela)
        {
            if (janela == null || abertas.Contains(janela)) return;

            // Guardar a posicao de origem na PRIMEIRA vez: e para onde ela volta quando
            // ficar sozinha, e e a posicao que o designer escolheu no editor.
            if (!posicaoOriginal.ContainsKey(janela))
                posicaoOriginal[janela] = janela.anchoredPosition;

            abertas.Add(janela);
            Reorganizar();
        }

        /// <summary>Anuncia que uma janela fechou.</summary>
        public void Remover(RectTransform janela)
        {
            if (janela == null) return;
            if (!abertas.Remove(janela)) return;

            // Devolve a janela ao lugar de origem, senao ela reabria deslocada.
            if (posicaoOriginal.TryGetValue(janela, out var origem))
                janela.anchoredPosition = origem;

            Reorganizar();
        }

        /// <summary>
        /// Recalcula a posicao de todas as janelas abertas.
        ///
        /// Uma janela: fica onde o designer a poe. Duas ou mais: empilham centradas na
        /// vertical, na ordem de abertura, com o mesmo passo entre elas.
        /// </summary>
        private void Reorganizar()
        {
            // Referencias mortas acumulam se uma janela for destruida sem fechar.
            abertas.RemoveAll(j => j == null);

            if (abertas.Count == 0) return;

            if (abertas.Count == 1)
            {
                var unica = abertas[0];
                if (posicaoOriginal.TryGetValue(unica, out var origem))
                    unica.anchoredPosition = origem;
                return;
            }

            // Empilhamento pela ALTURA REAL de cada janela, nao por um passo fixo.
            //
            // Com um passo de 330 o comedouro (392 de altura) e o inventario (530) ainda se
            // sobrepunham em 131px -- medido. Um passo fixo so funciona se todas as janelas
            // tiverem o mesmo tamanho, e nao tem: a soma das metades e que decide.
            var alturas = new List<float>(abertas.Count);
            float somaAlturas = 0f;
            foreach (var j in abertas)
            {
                float h = Mathf.Max(j.rect.height * Mathf.Abs(j.lossyScale.y), 1f);
                alturas.Add(h);
                somaAlturas += h;
            }

            float respiro = Respiro;
            float alturaTotal = somaAlturas + respiro * (abertas.Count - 1);

            // Se a pilha nao cabe no ecra, o respiro encolhe antes de deixar sobrepor.
            float disponivel = AlturaDeReferencia - 2f * MargemDoEcra;
            if (alturaTotal > disponivel && abertas.Count > 1)
            {
                respiro = Mathf.Max(0f, (disponivel - somaAlturas) / (abertas.Count - 1));
                alturaTotal = somaAlturas + respiro * (abertas.Count - 1);
            }

            // Comeca no topo da pilha e desce, dando a cada janela a sua propria altura.
            float cursor = alturaTotal * 0.5f;
            for (int i = 0; i < abertas.Count; i++)
            {
                var janela = abertas[i];
                Vector2 origem = posicaoOriginal.TryGetValue(janela, out var o)
                    ? o : janela.anchoredPosition;

                float centro = cursor - alturas[i] * 0.5f;
                cursor -= alturas[i] + respiro;

                // So o Y e reorganizado. Mexer no X moveria paineis que o designer alinhou
                // de proposito com outra coisa (a hotbar, por exemplo).
                janela.anchoredPosition = new Vector2(origem.x, centro);
            }
        }

        /// <summary>Quantas janelas o dock esta a posicionar agora.</summary>
        public int Contagem => abertas.Count;
    }
}
