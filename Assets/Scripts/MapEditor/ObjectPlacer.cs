using UnityEngine;
using SowurShield.Farming;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Coloca e remove objetos do mundo — arvores, decoracao, itens de chao.
    ///
    /// Grava cada objeto no MapData com o CAMINHO do prefab, que e o que o
    /// PrefabCatalog resolve ao carregar o mapa. O NPCPlacer que ja existia gravava
    /// apenas `prefab.name` (com um `// For now, return the name`), e um nome nao
    /// resolve de volta: os objetos eram salvos e nunca reapareciam.
    ///
    /// Vive em paralelo ao pincel: enquanto ha um prefab escolhido, o clique coloca
    /// objeto em vez de pintar chao.
    /// </summary>
    [RequireComponent(typeof(RuntimeMapEditor))]
    public class ObjectPlacer : MonoBehaviour
    {
        private RuntimeMapEditor mapEditor;
        private Camera cam;

        private string caminhoSelecionado;
        private GameObject prefabSelecionado;
        private GameObject fantasma;

        /// <summary>Null quando o modo de colocacao esta desligado (o clique pinta).</summary>
        public string CaminhoSelecionado => caminhoSelecionado;
        public bool ModoColocacao => prefabSelecionado != null;

        // ---------------------------------------------------------------------
        // Tamanho do objeto
        // ---------------------------------------------------------------------

        /// <summary>
        /// Multiplicador aplicado ao objeto colocado. O ObjectSpawnData ja tinha um
        /// campo `scale` e o RecriarObjetos ja o aplicava ao carregar, mas o placer
        /// gravava Vector3.one fixo -- entao o campo existia, era salvo e nunca
        /// significava nada. Agora e ele que o alimenta.
        /// </summary>
        public float Escala { get; private set; } = 1f;

        /// <summary>Limites: abaixo de 0,25 o objeto some no chao, acima de 4 tapa a tela.</summary>
        public const float EscalaMin = 0.25f;
        public const float EscalaMax = 4f;

        /// <summary>Os degraus do botao "-" e "+" da paleta.</summary>
        private static readonly float[] Degraus =
            { 0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f, 3f, 4f };

        // ---------------------------------------------------------------------
        // Rotacao e espelho
        // ---------------------------------------------------------------------

        /// <summary>
        /// Rotacao em Z aplicada ao objeto colocado, em graus.
        ///
        /// Mesma historia da escala: `ObjectSpawnData.rotation` ja existia e o
        /// RecriarObjetos ja fazia `Quaternion.Euler(obj.rotation)` ao carregar --
        /// o placer e que gravava `Vector3.zero` fixo, entao o campo era salvo e
        /// nunca significava nada.
        /// </summary>
        public float Rotacao { get; private set; }

        /// <summary>
        /// Espelhamento horizontal. Vale para arvores e decoracao, onde virar a arte
        /// evita que uma fileira do mesmo prefab pareca copiada.
        /// </summary>
        public bool Espelhado { get; private set; }

        /// <summary>Gira 90 graus por clique: a grade e quadrada, angulos livres so desalinham.</summary>
        public void Girar()
        {
            Rotacao = Mathf.Repeat(Rotacao + 90f, 360f);
            AplicarTransformacaoAoFantasma();
        }

        public void AlternarEspelho()
        {
            Espelhado = !Espelhado;
            AplicarTransformacaoAoFantasma();
        }

        /// <summary>Escala com o sinal do espelho: X negativo vira a arte.</summary>
        private Vector3 EscalaComEspelho()
        {
            return new Vector3(Espelhado ? -Escala : Escala, Escala, Escala);
        }

        private void AplicarTransformacaoAoFantasma()
        {
            if (fantasma == null) return;
            fantasma.transform.localScale = EscalaComEspelho();
            fantasma.transform.rotation = Quaternion.Euler(0f, 0f, Rotacao);
        }

        public void DefinirEscala(float valor)
        {
            Escala = Mathf.Clamp(valor, EscalaMin, EscalaMax);
            // O fantasma tem que mostrar o tamanho real, senao so se descobre que o
            // objeto ficou gigante depois de clicar.
            AplicarTransformacaoAoFantasma();
        }

        /// <summary>Anda um degrau para cima (+1) ou para baixo (-1).</summary>
        public void AjustarEscala(int direcao)
        {
            int i = System.Array.IndexOf(Degraus, Escala);
            if (i < 0)
            {
                // Valor fora da tabela (veio de um mapa antigo): procurar o degrau mais proximo.
                i = 0;
                for (int k = 1; k < Degraus.Length; k++)
                    if (Mathf.Abs(Degraus[k] - Escala) < Mathf.Abs(Degraus[i] - Escala)) i = k;
            }
            DefinirEscala(Degraus[Mathf.Clamp(i + direcao, 0, Degraus.Length - 1)]);
        }

        private void Start()
        {
            mapEditor = GetComponent<RuntimeMapEditor>();
            mapEditor.OnEditorToggled += AoAlternarEditor;
        }

        private void OnDestroy()
        {
            if (mapEditor != null) mapEditor.OnEditorToggled -= AoAlternarEditor;
        }

        private void AoAlternarEditor(bool aberto)
        {
            // Sair do editor com um prefab na mao e voltar depois colocando arvores
            // sem querer seria surpresa ruim.
            if (!aberto) Selecionar(null);
        }

        /// <summary>
        /// Escolhe o que colocar. Passar null desliga o modo e devolve o clique ao
        /// pincel — e o que o botao "Pincel" da paleta faz.
        /// </summary>
        public void Selecionar(string caminho)
        {
            caminhoSelecionado = caminho;
            prefabSelecionado = string.IsNullOrEmpty(caminho)
                ? null
                : PrefabCatalog.Resolver(caminho);

            DestruirFantasma();
            if (prefabSelecionado != null) CriarFantasma();
        }

        private void Update()
        {
            if (mapEditor == null || !mapEditor.IsEditorActive || prefabSelecionado == null)
                return;

            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null) return;
            }

            // Sobre a paleta o clique e da UI, nao do mundo.
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                if (fantasma != null) fantasma.SetActive(false);
                return;
            }

            var destino = PosicaoSobOCursor();
            if (fantasma != null)
            {
                fantasma.SetActive(true);
                fantasma.transform.position = destino;
            }

            if (Input.GetMouseButtonDown(0)) Colocar(destino);
            // Botao direito remove o que estiver ali: e o gesto que todo editor usa,
            // e evita um modo "borracha de objetos" separado.
            if (Input.GetMouseButtonDown(1)) RemoverEm(destino);
        }

        private Vector3 PosicaoSobOCursor()
        {
            var mundo = cam.ScreenToWorldPoint(Input.mousePosition);
            var dual = mapEditor.DualGrid;
            if (dual != null && dual.placeholderTilemap != null)
            {
                // Encaixado na celula, como o pincel: objeto solto entre celulas fica
                // desalinhado do chao pintado.
                var celula = dual.placeholderTilemap.WorldToCell(mundo);
                return new Vector3(celula.x + 0.5f, celula.y + 0.5f, 0f);
            }
            return new Vector3(Mathf.Floor(mundo.x) + 0.5f, Mathf.Floor(mundo.y) + 0.5f, 0f);
        }

        private void Colocar(Vector3 posicao)
        {
            if (prefabSelecionado == null || mapEditor.CurrentMapData == null) return;

            var raiz = ObterRaiz();
            var instancia = Instantiate(prefabSelecionado, posicao,
                                        Quaternion.Euler(0f, 0f, Rotacao), raiz);
            instancia.name = prefabSelecionado.name;
            instancia.transform.localScale = EscalaComEspelho();

            mapEditor.CurrentMapData.objectSpawns.Add(new ObjectSpawnData
            {
                position = posicao,
                objectId = prefabSelecionado.name,
                // O CAMINHO, nao o nome: e o que o PrefabCatalog resolve ao carregar.
                prefabPath = caminhoSelecionado,
                rotation = new Vector3(0f, 0f, Rotacao),
                scale = EscalaComEspelho(),
                isActive = true
            });
        }

        /// <summary>
        /// Remove o objeto que estiver na celula clicada.
        ///
        /// Procura na raiz da cena, e nao numa lista do que foi colocado nesta
        /// sessao: carregar um mapa destroi e recria a raiz inteira, entao uma lista
        /// dessas ficaria cheia de referencias nulas e o botao direito so removeria
        /// o que se acabou de por — nunca o que veio de um mapa salvo.
        /// </summary>
        private void RemoverEm(Vector3 posicao)
        {
            if (mapEditor.CurrentMapData == null) return;

            var raizGO = GameObject.Find(RuntimeMapEditor.RaizDeObjetosDoMapa);
            if (raizGO == null) return;

            // Meia celula de tolerancia: o objeto e encaixado no centro, entao clicar
            // em qualquer ponto da celula tem que pegar.
            const float tolerancia = 0.5f;

            for (int i = raizGO.transform.childCount - 1; i >= 0; i--)
            {
                var filho = raizGO.transform.GetChild(i);
                if (Vector3.Distance(filho.position, posicao) > tolerancia) continue;

                var alvo = filho.position;
                if (Application.isPlaying) Destroy(filho.gameObject);
                else DestroyImmediate(filho.gameObject);

                mapEditor.CurrentMapData.objectSpawns.RemoveAll(
                    o => Vector3.Distance(o.position, alvo) <= tolerancia);
                return;
            }
        }

        private Transform ObterRaiz()
        {
            var existente = GameObject.Find(RuntimeMapEditor.RaizDeObjetosDoMapa);
            if (existente != null) return existente.transform;
            return new GameObject(RuntimeMapEditor.RaizDeObjetosDoMapa).transform;
        }

        /// <summary>
        /// Uma copia translucida sob o cursor, para ver onde o objeto vai cair antes
        /// de clicar — o mesmo principio do preview do pincel.
        /// </summary>
        private void CriarFantasma()
        {
            fantasma = Instantiate(prefabSelecionado);
            fantasma.name = "ObjetoFantasma";

            // Nada de logica rodando num objeto que e so visual: sem isto o fantasma
            // de um NPC comeca a andar e a falar enquanto se escolhe onde por.
            foreach (var c in fantasma.GetComponentsInChildren<MonoBehaviour>()) c.enabled = false;
            foreach (var c in fantasma.GetComponentsInChildren<Collider2D>()) c.enabled = false;

            foreach (var sr in fantasma.GetComponentsInChildren<SpriteRenderer>())
            {
                var cor = sr.color;
                cor.a *= 0.5f;
                sr.color = cor;
            }
            AplicarTransformacaoAoFantasma();
            fantasma.SetActive(false);
        }

        private void DestruirFantasma()
        {
            if (fantasma == null) return;
            if (Application.isPlaying) Destroy(fantasma);
            else DestroyImmediate(fantasma);
            fantasma = null;
        }
    }
}
