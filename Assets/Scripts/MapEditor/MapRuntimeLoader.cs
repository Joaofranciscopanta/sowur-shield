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

        [Tooltip("As pessoas colocadas no editor. Uma personagem que a cena ja traz e " +
                 "movida para a posicao do mapa, nunca duplicada.")]
        [SerializeField] private bool aplicarNPCs = true;

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
            if (aplicarNPCs) AplicarNPCs(dados);

            MapaAtual = dados;
            return true;
        }

        /// <summary>
        /// Instancia as pessoas do mapa.
        ///
        /// Ate 2026-09-05 este loader ignorava `npcSpawns` por completo: o campo existia no
        /// MapData desde sempre, o editor gravava-o, e nada o lia no jogo -- o mesmo defeito
        /// que a opcao B corrigiu para os objetos, so que para gente.
        ///
        /// Uma personagem que a CENA ja traz nao e duplicada: `npcId` indexa a memoria de
        /// conversa e o relacionamento, entao duas Joanas partilhariam estado e as duas
        /// ficariam erradas. Nesse caso a que ja existe e MOVIDA para a posicao do mapa,
        /// que e a mesma regra que a paleta aplica ao colocar.
        /// </summary>
        private void AplicarNPCs(MapData dados)
        {
            if (dados.npcSpawns == null || dados.npcSpawns.Count == 0) return;

            var raiz = GameObject.Find(RaizDeNPCs);
            if (raiz != null) DestroyImmediate(raiz);
            raiz = new GameObject(RaizDeNPCs);

            // Uma unica varredura da cena, reusada por todos os spawns: FindObjectsByType
            // por NPC seria O(n*m) num metodo que corre no Start.
            var naCena = FindObjectsByType<SowurShield.Dialogue.NPCDialogueInteractable>(
                             FindObjectsSortMode.None);

            int perdidos = 0, movidos = 0, criados = 0;
            foreach (var npc in dados.npcSpawns)
            {
                if (!npc.isActive) continue;

                if (!string.IsNullOrEmpty(npc.npcId))
                {
                    var existente = System.Array.Find(naCena, n => n.GetNPCId() == npc.npcId);
                    if (existente != null)
                    {
                        existente.transform.position = npc.position;
                        movidos++;
                        continue;
                    }
                }

                var prefab = ResolverPrefab(npc.npcPrefabPath);
                if (prefab == null) { perdidos++; continue; }

                var instancia = Instantiate(prefab, npc.position,
                                            Quaternion.Euler(0f, 0f, npc.rotation), raiz.transform);
                if (!string.IsNullOrEmpty(npc.npcName)) instancia.name = npc.npcName;
                criados++;
            }

            if (perdidos > 0)
            {
                Debug.LogWarning($"[MapRuntimeLoader] {perdidos} NPC(s) do mapa nao " +
                                 "puderam ser carregados: o prefab precisa estar sob " +
                                 "Assets/Resources/ para existir no jogo.");
            }
        }

        public const string RaizDeNPCs = "MapNPCs (Runtime)";

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
                instancia.transform.localScale = EscalaDe(obj, prefab);
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
        /// A escala com que instanciar um objeto do mapa.
        ///
        /// Desde 2026-09-05 cada prefab da paleta carrega no proprio localScale o fator
        /// que o traz para a escala do mundo (PPU 16) -- 5,5 para a arte a PPU 100, 2
        /// para as arvores, e assim por diante -- e o ObjectPlacer grava em
        /// `scale` o produto desse fator pelo multiplicador escolhido. Para esses
        /// mapas o valor gravado ja e absoluto e vai direto.
        ///
        /// Os mapas salvos ANTES disso gravaram exatamente (1,1,1), de quando o placer
        /// escrevia o multiplicador cru e todo prefab tinha escala 1. Usar esse valor
        /// hoje faria a flor nascer com 1/5,5 do tamanho -- o mesmo defeito de antes,
        /// so que vindo do arquivo. Nesse caso caimos na escala do proprio prefab.
        ///
        /// (1,1,1) e seguro como sentinela: nenhum prefab da paleta tem escala natural
        /// 1, entao um mapa novo nunca grava esse valor por coincidencia.
        /// </summary>
        public static Vector3 EscalaDe(ObjectSpawnData obj, GameObject prefab)
        {
            bool escalaDeMapaAntigo = obj.scale == Vector3.one;
            if (escalaDeMapaAntigo && prefab != null)
                return prefab.transform.localScale;

            // Um mapa gravado com escala zerada (campo nunca preenchido) tambem cai
            // no prefab: um objeto de escala 0 e invisivel e parece nao ter carregado.
            if (obj.scale == Vector3.zero)
                return prefab != null ? prefab.transform.localScale : Vector3.one;

            return obj.scale;
        }

        /// <summary>
        /// Converte o caminho gravado pelo editor numa chave de Resources.
        ///
        /// O editor grava o caminho de projeto inteiro ("Assets/Resources/Prefabs/
        /// GroundItems/X.prefab") porque e o que o AssetDatabase resolve. Em runtime
        /// so vale o trecho DEPOIS de "Resources/", e sem a extensao.
        ///
        /// Tem um segundo caminho para os mapas antigos -- ver o comentario dentro.
        /// </summary>
        public static GameObject ResolverPrefab(string caminho)
        {
            if (string.IsNullOrEmpty(caminho)) return null;

            const string marca = "Resources/";
            int i = caminho.IndexOf(marca);
            if (i >= 0)
            {
                string chave = SemExtensao(caminho.Substring(i + marca.Length));
                var direto = Resources.Load<GameObject>(chave);
                if (direto != null) return direto;
            }

            // Mapas salvos ANTES de 2026-09-04 gravaram o caminho antigo
            // ("Assets/Prefabs/Decorations/X.prefab"), de quando esses prefabs ainda
            // nao viviam sob Resources/. Sem este segundo caminho, todo mapa antigo
            // abriria com o chao certo e NENHUM objeto -- medido: os 7 objetos do
            // unico mapa salvo falhavam todos.
            //
            // O nome do arquivo e unico entre os prefabs da paleta, entao procurar
            // por ele nas pastas conhecidas recupera o objeto sem precisar reescrever
            // os mapas.
            string nome = SemExtensao(System.IO.Path.GetFileName(caminho));
            if (string.IsNullOrEmpty(nome)) return null;

            foreach (var pasta in PastasDePrefabs)
            {
                var achado = Resources.Load<GameObject>(pasta + "/" + nome);
                if (achado != null) return achado;
            }
            return null;
        }

        /// <summary>As pastas de prefab sob Resources/ que a paleta oferece.</summary>
        private static readonly string[] PastasDePrefabs =
        {
            "Prefabs/NPCs",
            "Prefabs/Decorations",
            "Prefabs/FruitTrees",
            "Prefabs/Fruits",
            "Prefabs/GroundItems"
        };

        private static string SemExtensao(string s)
        {
            int ponto = s.LastIndexOf('.');
            return ponto >= 0 ? s.Substring(0, ponto) : s;
        }
    }
}
