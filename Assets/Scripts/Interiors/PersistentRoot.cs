using UnityEngine;
using UnityEngine.SceneManagement;

namespace SowurShield.Interiors
{

/// <summary>
/// Faz uma raiz da cena sobreviver a mudanca de cena, sem duplicar.
///
/// A UI do jogo (HUD, inventario, dialogo, minimapa) e os gestores de cena vivem
/// em duas raizes da SampleScene: "UI" e "Managers". Nenhuma delas era
/// DontDestroyOnLoad, porque durante muito tempo a unica outra cena de jogo era a
/// de combate, que tem a sua propria interface.
///
/// <para>Com interiores isso partiu tudo de uma vez. Entrar numa casa destruia:</para>
/// <list type="bullet">
///   <item>o <b>EventSystem</b> — e sem ele NENHUM botao da Unity UI recebe clique.
///         Era este o "todos os botoes travam"; o editor de mapa continuava a
///         funcionar so porque vive numa raiz propria que ja persistia.</item>
///   <item>o <b>DialogueTreeUI</b> — falar com um NPC nao mostrava nada.</item>
///   <item>o <b>HUD e o minimapa</b> — e o indicador do rato ficava preso.</item>
///   <item>as referencias do <b>menu de pausa</b>: o GameMenuManager sobrevivia mas
///         3 das suas 5 ligacoes apontavam para paineis destruidos, entao o ESC
///         nao abria nada.</item>
/// </list>
///
/// <para>Guarda contra duplicados pelo NOME da raiz: voltar a SampleScene carrega-a
/// outra vez, com a sua propria copia de "UI" e "Managers". A que ja atravessou
/// fica; a recem-carregada destroi-se — senao ficariam dois EventSystem, e dois
/// EventSystem em simultaneo tambem partem os cliques.</para>
/// </summary>
[DisallowMultipleComponent]
public class PersistentRoot : MonoBehaviour
{
    private static readonly System.Collections.Generic.Dictionary<string, PersistentRoot> Vivas
        = new System.Collections.Generic.Dictionary<string, PersistentRoot>();

    /// <summary>Limpa o registo: o Play Mode sem domain reload preserva estaticos.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Preparar()
    {
        Vivas.Clear();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= AoCarregarCena;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += AoCarregarCena;
    }

    /// <summary>
    /// Desliga o EventSystem duplicado assim que a cena acaba de carregar.
    ///
    /// O Awake nao chega. O EventSystem regista-se a si proprio no SEU Awake, e a
    /// ordem entre objetos de raizes diferentes nao e garantida — quando o Awake
    /// desta raiz corre, o EventSystem da copia ja se registou, e o Unity queixa-se
    /// "There can be only one active Event System" com os cliques a irem para o
    /// sitio errado durante esse frame.
    ///
    /// sceneLoaded corre depois de todos os Awake da cena nova, e e aqui que se pode
    /// garantir que fica exatamente um ativo.
    /// </summary>
    private static void AoCarregarCena(UnityEngine.SceneManagement.Scene cena,
                                       UnityEngine.SceneManagement.LoadSceneMode modo)
    {
        var todos = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (todos.Length == 0) return;

        // Numa cena que NAO e de jogo (combate, menu), o EventSystem persistente esta
        // de saida — as raizes da quinta destroem-se ao chegar aqui. Preferir o dela.
        //
        // ⚠️ Preferir sempre o persistente partia o combate: este callback corre ANTES
        // de o Destroy acontecer, entao havia dois, o da CombatScene era desligado, e o
        // persistente morria logo a seguir — a cena ficava com ZERO EventSystem ativos e
        // nenhum botao do turno respondia. Medido: enabled=False, currentInputModule=NULL.
        bool cenaDeJogo = EhCenaDeJogo(cena.name);

        UnityEngine.EventSystems.EventSystem escolhido = null;
        foreach (var ev in todos)
        {
            bool persistente = ev.gameObject.scene.buildIndex == -1;
            bool serve = cenaDeJogo ? persistente : !persistente;
            if (serve) { escolhido = ev; break; }
        }

        // Nao ha nenhum do tipo preferido: fica o primeiro que houver, seja qual for.
        if (escolhido == null) escolhido = todos[0];

        foreach (var ev in todos)
            ev.enabled = (ev == escolhido);
    }

    /// <summary>
    /// Cenas onde esta UI faz sentido: a vila e os interiores das casas.
    ///
    /// A UI da quinta nao pertence a toda a parte. Marcar o TeamAssemblerCanvas como
    /// persistente fez com que ele sobrevivesse a ida para a CombatScene e ficasse
    /// desenhado POR CIMA do combate — que tem a sua propria interface. O mesmo valeria
    /// para o menu principal.
    ///
    /// Testar pelo NOME da cena, e nao por uma lista de excecoes: uma cena nova de
    /// combate ou de menu nao precisa de ser acrescentada a lado nenhum para se
    /// comportar bem.
    /// </summary>
    internal static bool EhCenaDeJogo(string nomeDaCena)
    {
        if (string.IsNullOrEmpty(nomeDaCena)) return false;
        return nomeDaCena == "SampleScene" || nomeDaCena.StartsWith("Interior_");
    }

    private void OnEnable()
    {
        // Sair da vila para uma cena que NAO e de jogo (combate, menu principal) leva
        // esta UI consigo. Destruir-se ao chegar la e o que a mantem invisivel onde
        // nao pertence, sem precisar de a religar depois: voltar a vila recarrega-a
        // com a cena.
        SceneManager.sceneLoaded -= AoMudarDeCena;
        SceneManager.sceneLoaded += AoMudarDeCena;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= AoMudarDeCena;
    }

    private void AoMudarDeCena(Scene cena, LoadSceneMode modo)
    {
        if (EhCenaDeJogo(cena.name)) return;

        // Desligar o EventSystem JA, e nao so no Destroy.
        //
        // Destroy so remove no fim do frame, e ate la este EventSystem — que esta de
        // saida — ainda podia receber os cliques do frame. Desligar aqui garante que
        // quem os recebe e o da cena nova.
        //
        // ⚠️ Isto NAO faz desaparecer o "There can be only one active Event System" da
        // consola: esse erro e escrito pelo OnEnable do EventSystem da cena nova, que
        // corre antes de qualquer callback nosso. Medido — sobra 1 mensagem por ida a
        // uma cena que nao e de jogo, e o estado final fica correto (1 ativo).
        foreach (var ev in GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))
            ev.enabled = false;

        Destroy(gameObject);
    }

    private void Awake()
    {
        string chave = gameObject.name;

        if (Vivas.TryGetValue(chave, out var dona) && dona != null && dona != this)
        {
            // Ja ha uma copia desta raiz vinda de outra cena. Esta e a que a cena
            // trouxe consigo.
            //
            // O EventSystem tem de ser desligado EXPLICITAMENTE, antes de tudo o
            // resto: ele regista-se a si proprio no Awake, que ja correu quando este
            // Awake chega — SetActive(false) no pai nao desfaz esse registo, e o
            // Unity queixa-se "There can be only one active Event System" com os
            // cliques a irem para o sitio errado durante esse frame.
            foreach (var ev in GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))
                ev.enabled = false;

            // Desativar antes de destruir: Destroy so remove no fim do frame, e ate
            // la o Start dos filhos ainda corria — dois HUD, duas janelas a
            // registarem-se no UIManager.
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Vivas[chave] = this;

        if (transform.parent != null)
        {
            Debug.LogWarning($"[RaizPersistente] '{chave}' tem pai; DontDestroyOnLoad " +
                             "so funciona na raiz. A soltar.", this);
            transform.SetParent(null, true);
        }

        MarcarComoPersistente(gameObject);
    }

    /// <summary>
    /// Chama DontDestroyOnLoad so se o objeto ainda nao estiver marcado.
    ///
    /// Um objeto ja marcado vive na cena especial cujo <c>buildIndex</c> e -1 e cujo
    /// nome e "DontDestroyOnLoad". Chamar DontDestroyOnLoad outra vez sobre ele faz o
    /// Unity registá-lo numa lista onde ja consta, e sai um "Assertion failed on
    /// expression: m_GameObjects.find(gameObject.GetEntityId()) == m_GameObjects.end()".
    ///
    /// ⚠️ NAO chega registar quem NOS marcamos. O objeto pode ja ter sido marcado por
    /// OUTRO componente seu: o InteractionManager, o SaveManager e mais uma duzia de
    /// singletons chamam DontDestroyOnLoad no seu proprio Awake. Num GameObject que
    /// tenha ambos, a primeira chamada e deles e nunca passa por aqui — foi exatamente
    /// isso que manteve a assercao a aparecer no arranque, no 'InteractionManager' e no
    /// 'Bunny'. A pergunta certa e "este objeto JA esta em DontDestroyOnLoad?", seja
    /// quem for que o tenha posto la.
    /// </summary>
    internal static void MarcarComoPersistente(GameObject alvo)
    {
        if (alvo.scene.buildIndex == -1) return;   // ja esta em DontDestroyOnLoad

        // Nenhuma guarda feita DENTRO do Awake resolve isto, e vale a pena dizer porque:
        // a cena do objeto so e reatribuida DEPOIS de todos os Awake, entao ate la a
        // leitura acima da sempre a cena antiga, tanto para nos como para eles. E um
        // registo nosso de ids nao ve a chamada do OUTRO componente, que corre primeiro.
        // Medido: a assercao continuava no 'InteractionManager' e no 'Bunny' com as duas
        // guardas em vigor.
        //
        // Silenciar a assercao tambem nao serve: ela e um Assert do motor, nao um
        // Debug.Log nosso.
        //
        // Entao NAO marcar aqui. A unica coisa que este objeto precisa e de estar
        // persistente quando a cena mudar — e isso acontece muito depois do fim deste
        // frame. Ao adiar, a leitura da cena ja e a definitiva e a guarda funciona:
        // se outro singleton ja o marcou, saimos; se ninguem marcou, marcamos nos.
        var adiador = alvo.AddComponent<AdiarPersistencia>();
        adiador.hideFlags = HideFlags.HideInInspector;
    }

    private void OnDestroy()
    {
        if (Vivas.TryGetValue(gameObject.name, out var dona) && dona == this)
            Vivas.Remove(gameObject.name);
    }
}

}
