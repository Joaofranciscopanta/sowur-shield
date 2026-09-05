using UnityEngine;
using SowurShield.Dialogue;

namespace SowurShield.MapEditor
{
    /// <summary>
    /// Coloca NPCs pela paleta do editor de mapa.
    ///
    /// Ate 2026-09-05 o catalogo da paleta so varria pastas de cenario, entao nao havia
    /// como por gente num mapa: o editor deixava plantar arvores e pedras num mundo
    /// permanentemente vazio. Existia um `NPCPlacer` de 606 linhas com o seu proprio
    /// sistema de `NPCTemplate`, mas nunca esteve em cena nenhuma nem foi instanciado por
    /// ninguem -- codigo morto, e um modelo de dados que nao casa com o do jogo, onde cada
    /// NPC e uma personagem completa e nao um molde a preencher.
    ///
    /// Este placer segue o desenho do <see cref="ObjectPlacer"/>, com uma diferenca que
    /// vem do dominio: <b>um NPC nao se duplica</b>.
    ///
    /// As nove personagens do jogo sao unicas -- `npcId` indexa a memoria de conversa e o
    /// relacionamento -- entao duas Joanas partilhariam o mesmo estado e as duas ficariam
    /// erradas. Por isso colocar uma personagem que ja esta no mapa MOVE a que existe em
    /// vez de criar outra. O NPC generico nao tem `npcId`, entao esse cada clique cria um
    /// novo, que e o que se quer para povoar um mapa.
    /// </summary>
    [RequireComponent(typeof(RuntimeMapEditor))]
    public class NPCPalettePlacer : MonoBehaviour
    {
        private RuntimeMapEditor mapEditor;
        private Camera cam;

        private string caminhoSelecionado;
        private GameObject prefabSelecionado;
        private GameObject fantasma;

        /// <summary>Null quando nao ha NPC escolhido (o clique volta ao pincel).</summary>
        public string CaminhoSelecionado => caminhoSelecionado;
        public bool ModoColocacao => prefabSelecionado != null;

        /// <summary>
        /// Ultima acao feita, para a paleta poder dizer o que aconteceu. "Mover" e
        /// indistinguivel de "nao fez nada" quando o NPC estava fora do ecra.
        /// </summary>
        public string UltimaMensagem { get; private set; }

        /// <summary>A pasta, sob Resources/, onde os prefabs de NPC vivem.</summary>
        public const string PastaDeNPCs = "Prefabs/NPCs";

        /// <summary>O prefab sem personagem, que pode ser colocado quantas vezes se quiser.</summary>
        public const string NomeDoGenerico = "NPC_Novo";

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
            if (!aberto) Selecionar(null);
        }

        /// <summary>
        /// Escolhe o NPC a colocar. Passar null desliga o modo.
        ///
        /// Devolve o clique ao pincel exatamente como o ObjectPlacer faz -- os dois nunca
        /// estao ativos ao mesmo tempo, ver <see cref="ObjectPlacer.ModoColocacao"/>.
        /// </summary>
        public void Selecionar(string caminho)
        {
            caminhoSelecionado = caminho;
            prefabSelecionado = string.IsNullOrEmpty(caminho)
                ? null
                : PrefabCatalog.Resolver(caminho);

            UltimaMensagem = null;
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
            if (Input.GetMouseButtonDown(1)) RemoverEm(destino);
        }

        private Vector3 PosicaoSobOCursor()
        {
            var mundo = cam.ScreenToWorldPoint(Input.mousePosition);
            var dual = mapEditor.DualGrid;
            if (dual != null && dual.placeholderTilemap != null)
            {
                var celula = dual.placeholderTilemap.WorldToCell(mundo);
                return new Vector3(celula.x + 0.5f, celula.y + 0.5f, 0f);
            }
            return new Vector3(Mathf.Floor(mundo.x) + 0.5f, Mathf.Floor(mundo.y) + 0.5f, 0f);
        }

        /// <summary>O npcId gravado num prefab, ou vazio para o generico.</summary>
        public static string IdDoPrefab(GameObject prefab)
        {
            if (prefab == null) return "";
            var npc = prefab.GetComponent<NPCDialogueInteractable>();
            return npc != null ? npc.GetNPCId() : "";
        }

        private void Colocar(Vector3 posicao)
        {
            if (prefabSelecionado == null || mapEditor.CurrentMapData == null) return;

            string id = IdDoPrefab(prefabSelecionado);

            // Personagem unica que ja existe: MOVER, nao duplicar. Duas copias com o mesmo
            // npcId partilhariam memoria de conversa e relacionamento.
            //
            // Olhar o MapData NAO CHEGA. As nove personagens do jogo estao soltas na
            // SampleScene desde sempre e nao no `npcSpawns`, entao num mapa novo (lista
            // vazia) o primeiro clique instanciava uma segunda Joana ao lado da que o
            // jogador ja via -- medido rodando: 2 Joanas na cena com npcSpawns a dizer 1.
            // A cena e a fonte de verdade sobre quem existe; o MapData so sobre onde fica.
            if (!string.IsNullOrEmpty(id))
            {
                var naCena = InstanciaNaCena(id);
                var gravado = mapEditor.CurrentMapData.npcSpawns.Find(n => n.npcId == id);

                if (naCena != null || gravado != null)
                {
                    Vector3 antes = gravado != null
                        ? gravado.position
                        : (naCena != null ? naCena.transform.position : posicao);

                    if (gravado != null) gravado.position = posicao;
                    else mapEditor.CurrentMapData.npcSpawns.Add(new NPCSpawnData
                    {
                        position = posicao,
                        npcId = id,
                        npcName = prefabSelecionado.name,
                        dialogueIds = new string[0],
                        rotation = 0f,
                        isActive = true,
                        npcPrefabPath = caminhoSelecionado
                    });

                    if (naCena != null)
                    {
                        naCena.transform.position = posicao;

                        // O COLISOR nao acompanha o transform sozinho.
                        //
                        // A fisica 2D so sincroniza no FixedUpdate, e o editor de mapa
                        // corre com o jogo pausado (timeScale 0) -- entao o FixedUpdate
                        // nunca chega a correr e o colisor fica na posicao ANTIGA para
                        // sempre. Medido: personagem movida para 1 unidade do jogador com
                        // o colisor ainda a 9,9 de distancia.
                        //
                        // Sintoma a jogar: a personagem aparece no sitio novo e interagir
                        // com ela nao faz absolutamente nada -- nem som de erro, porque
                        // nem chega a ser considerada um alvo. Relatado pelo Lucas.
                        Physics2D.SyncTransforms();
                    }

                    UltimaMensagem = $"{NomeVisivel(prefabSelecionado)} movido de " +
                                     $"({antes.x:0},{antes.y:0}) para ({posicao.x:0},{posicao.y:0}).";
                    return;
                }
            }

            var raiz = ObterRaiz();

            // O generico precisa de um NOME unico, nao so de um id vazio: quando o npcId
            // esta em branco, o proprio jogo gera um a partir do nome do GameObject
            // (NPCDialogueInteractable.InitializeNPC), e dois "NPC_Novo" na cena viravam
            // dois `npc_NPC_Novo` -- o mesmo id partilhado que a regra de nao-duplicar
            // existe para evitar, so que entrando pela porta dos fundos.
            string nome = string.IsNullOrEmpty(id)
                ? NomeUnicoParaGenerico()
                : prefabSelecionado.name;

            var instancia = Instantiate(prefabSelecionado, posicao, Quaternion.identity, raiz);
            instancia.name = nome;

            // Mesmo motivo do ramo de mover: com o jogo pausado o FixedUpdate nao corre,
            // e sem isto a personagem nova nasce com o colisor fora do sitio.
            Physics2D.SyncTransforms();

            mapEditor.CurrentMapData.npcSpawns.Add(new NPCSpawnData
            {
                position = posicao,
                // Vazio para o generico: e a marca de "cada clique cria um novo".
                npcId = id,
                npcName = nome,
                dialogueIds = new string[0],
                rotation = 0f,
                isActive = true,
                // O CAMINHO, nao o nome -- e o que o catalogo resolve ao carregar.
                npcPrefabPath = caminhoSelecionado
            });

            UltimaMensagem = $"{NomeVisivel(prefabSelecionado)} colocado.";
        }

        /// <summary>
        /// Um nome que ainda nao existe na cena, para o proximo NPC generico.
        ///
        /// Conta a partir do que ja esta la em vez de usar um contador de campo: dois
        /// carregamentos do mesmo mapa, ou um undo pelo meio, poriam o contador fora de
        /// sincronia e o nome repetido voltaria.
        /// </summary>
        private static string NomeUnicoParaGenerico()
        {
            for (int i = 1; i < 1000; i++)
            {
                string tentativa = $"NPC {i}";
                if (GameObject.Find(tentativa) == null) return tentativa;
            }
            // Praticamente inalcancavel; ainda assim melhor que devolver um nome repetido.
            return "NPC " + System.Guid.NewGuid().ToString("N").Substring(0, 6);
        }

        /// <summary>
        /// A personagem com este id que ja existe na cena, ou null.
        ///
        /// Procura a cena INTEIRA, e nao so a raiz do mapa: as nove personagens do jogo
        /// estao soltas na SampleScene desde sempre, entao olhar so dentro da raiz que o
        /// editor cria nao encontraria a Joana que o jogador ve -- e o placer poria uma
        /// segunda ao lado dela.
        /// </summary>
        private static NPCDialogueInteractable InstanciaNaCena(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (var npc in FindObjectsByType<NPCDialogueInteractable>(FindObjectsSortMode.None))
                if (npc.GetNPCId() == id) return npc;

            return null;
        }

        /// <summary>
        /// Remove o NPC que estiver na celula clicada.
        ///
        /// So mexe no que esta sob a raiz do mapa: apagar com o botao direito uma das
        /// personagens que a cena ja trazia seria destruir conteudo do jogo por engano.
        /// </summary>
        private void RemoverEm(Vector3 posicao)
        {
            if (mapEditor.CurrentMapData == null) return;

            var raizGO = GameObject.Find(RuntimeMapEditor.RaizDeObjetosDoMapa);
            if (raizGO == null) return;

            const float tolerancia = 0.5f;

            for (int i = raizGO.transform.childCount - 1; i >= 0; i--)
            {
                var filho = raizGO.transform.GetChild(i);
                if (filho.GetComponent<NPCDialogueInteractable>() == null) continue;
                if (Vector3.Distance(filho.position, posicao) > tolerancia) continue;

                var alvo = filho.position;
                string nome = filho.name;
                if (Application.isPlaying) Destroy(filho.gameObject);
                else DestroyImmediate(filho.gameObject);

                mapEditor.CurrentMapData.npcSpawns.RemoveAll(
                    n => Vector3.Distance(n.position, alvo) <= tolerancia);
                UltimaMensagem = $"{nome} removido.";
                return;
            }
        }

        private Transform ObterRaiz()
        {
            var existente = GameObject.Find(RuntimeMapEditor.RaizDeObjetosDoMapa);
            if (existente != null) return existente.transform;
            return new GameObject(RuntimeMapEditor.RaizDeObjetosDoMapa).transform;
        }

        /// <summary>O nome que a paleta mostra: o nome de exibicao, com recuo para o do asset.</summary>
        public static string NomeVisivel(GameObject prefab)
        {
            if (prefab == null) return "";
            var npc = prefab.GetComponent<NPCDialogueInteractable>();
            string nome = npc != null ? npc.GetNPCDisplayName() : null;
            return string.IsNullOrWhiteSpace(nome) ? prefab.name : nome;
        }

        private void CriarFantasma()
        {
            fantasma = Instantiate(prefabSelecionado);
            fantasma.name = "NPCFantasma";

            // Um NPC fantasma com a logica ligada comeca a andar e a falar enquanto se
            // escolhe onde o por -- e o collider roubaria o clique do proprio placer.
            foreach (var c in fantasma.GetComponentsInChildren<MonoBehaviour>()) c.enabled = false;
            foreach (var c in fantasma.GetComponentsInChildren<Collider2D>()) c.enabled = false;

            foreach (var sr in fantasma.GetComponentsInChildren<SpriteRenderer>())
            {
                var cor = sr.color;
                cor.a *= 0.5f;
                sr.color = cor;
            }
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
