using UnityEngine;
using SowurShield.Farming;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Aplica um <see cref="MapData"/> na cena quando o jogo comeca.
    ///
    /// Ate agora o editor de mapa nao tinha saida: o que se pintava em Play Mode era
    /// perdido ao sair, e o MapData salvo em disco NAO era lido por ninguem -- medido
    /// em 2026-09-04, nenhum arquivo fora de Scripts/MapEditor/ sequer mencionava o
    /// tipo. Construir um mapa e ve-lo no jogo eram duas coisas desligadas.
    ///
    /// Este componente e a ponte. Fica num GameObject da cena, aponta para um mapa em
    /// `Resources/Maps/` e o aplica no Start.
    ///
    /// Nao usa `AssetDatabase` em lugar nenhum, ao contrario do MapSerializer: isto
    /// roda no JOGO, inclusive no build de WebGL, onde `Assets/` nao existe e
    /// `Resources.Load` e o unico caminho.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapRuntimeLoader : MonoBehaviour
    {
        [Header("Mapa")]
        [Tooltip("Nome do mapa em Resources/Maps, sem extensao. Vazio = nao carrega nada.")]
        [SerializeField] private string mapaInicial = "";

        [Tooltip("Referencia direta, se preferir arrastar o asset em vez de digitar o nome. " +
                 "Tem prioridade sobre o nome.")]
        [SerializeField] private MapData mapaDireto;

        [Header("O que aplicar")]
        [SerializeField] private bool aplicarTerreno = true;
        [SerializeField] private bool aplicarObjetos = true;

        [Tooltip("Desligado, o componente fica inerte -- util para testar a cena como " +
                 "ela esta, sem apagar o que ja foi montado a mao.")]
        [SerializeField] private bool carregarAoIniciar = true;

        /// <summary>A pasta dentro de Resources/ onde os mapas vivem.</summary>
        public const string PastaDeMapas = "Maps";

        /// <summary>O mapa que esta aplicado agora, ou null.</summary>
        public MapData MapaAtual { get; private set; }

        private void Start()
        {
            if (!carregarAoIniciar) return;
            Carregar();
        }

        /// <summary>
        /// Aplica o mapa configurado. Devolve false quando nao ha o que carregar --
        /// o chamador decide se isso e um problema.
        /// </summary>
        public bool Carregar()
        {
            var dados = mapaDireto != null ? mapaDireto : CarregarDeResources(mapaInicial);
            if (dados == null) return false;
            return Aplicar(dados);
        }

        /// <summary>Troca o mapa da cena em runtime, pelo nome em Resources/Maps.</summary>
        public bool Carregar(string nome)
        {
            var dados = CarregarDeResources(nome);
            if (dados == null) return false;
            mapaInicial = nome;
            return Aplicar(dados);
        }

        public static MapData CarregarDeResources(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return null;
            return Resources.Load<MapData>(PastaDeMapas + "/" + nome);
        }

        /// <summary>Os mapas que existem em Resources/Maps, para uma UI listar.</summary>
        public static MapData[] TodosOsMapas() => Resources.LoadAll<MapData>(PastaDeMapas);

        private bool Aplicar(MapData dados)
        {
            var dual = FindFirstObjectByType<DualGridTilemap>();
            if (dual == null)
            {
                Debug.LogWarning("[MapRuntimeLoader] Nao ha DualGridTilemap na cena; " +
                                 "o terreno do mapa nao foi aplicado.");
                return false;
            }

            if (aplicarTerreno)
            {
                // A ordem de Start entre este componente e o DualGridTilemap nao
                // importa: RefreshDisplayTilemap chama EnsureRules(), que monta as
                // regras se ainda nao existirem (DualGridTilemap.cs:68).
                DualGridPaintAdapter.Apply(dual, dados);
            }

            if (aplicarObjetos) AplicarObjetos(dados);

            MapaAtual = dados;
            return true;
        }

        /// <summary>
        /// Instancia os objetos do mapa sob uma raiz propria.
        ///
        /// A raiz e limpa antes: carregar dois mapas seguidos empilharia as arvores do
        /// primeiro debaixo das do segundo.
        /// </summary>
        private void AplicarObjetos(MapData dados)
        {
            var raiz = GameObject.Find(RaizDeObjetos);
            if (raiz != null)
            {
                // DestroyImmediate, nao Destroy: o Destroy do Unity so acontece no
                // FIM do frame, entao a raiz nova (criada na linha seguinte, com o
                // mesmo nome) conviveria com a velha ate la -- e um GameObject.Find
                // no meio disso pode devolver a que esta para morrer.
                DestroyImmediate(raiz);
            }
            raiz = new GameObject(RaizDeObjetos);

            int perdidos = 0;
            foreach (var obj in dados.objectSpawns)
            {
                if (!obj.isActive) continue;

                // Resources.Load, nao PrefabCatalog: o catalogo usa AssetDatabase e
                // so existe no Editor. Um prefab referenciado por um caminho de
                // projeto ("Assets/Prefabs/...") nao resolve no jogo -- por isso o
                // caminho e convertido para a chave de Resources.
                var prefab = ResolverPrefab(obj.prefabPath);
                if (prefab == null) { perdidos++; continue; }

                var instancia = Instantiate(prefab, obj.position,
                                            Quaternion.Euler(obj.rotation), raiz.transform);
                instancia.transform.localScale = obj.scale;
                instancia.name = obj.objectId;
            }

            if (perdidos > 0)
            {
                Debug.LogWarning($"[MapRuntimeLoader] {perdidos} objeto(s) do mapa nao " +
                                 "puderam ser carregados: o prefab precisa estar sob " +
                                 "Assets/Resources/ para existir no jogo.");
            }
        }

        public const string RaizDeObjetos = "MapObjects (Runtime)";

        /// <summary>
        /// Converte o caminho gravado pelo editor numa chave de Resources.
        ///
        /// O editor grava o caminho de projeto inteiro ("Assets/Resources/Prefabs/
        /// GroundItems/X.prefab") porque e o que o AssetDatabase resolve. Em runtime
        /// so vale o trecho DEPOIS de "Resources/", e sem a extensao.
        /// </summary>
        public static GameObject ResolverPrefab(string caminho)
        {
            if (string.IsNullOrEmpty(caminho)) return null;

            const string marca = "Resources/";
            int i = caminho.IndexOf(marca);
            if (i < 0) return null;   // fora de Resources: nao existe no build

            string chave = caminho.Substring(i + marca.Length);
            int ponto = chave.LastIndexOf('.');
            if (ponto >= 0) chave = chave.Substring(0, ponto);

            return Resources.Load<GameObject>(chave);
        }
    }
}
