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
        if (todos.Length <= 1) return;

        // Fica o que ja atravessou cenas (buildIndex -1); os outros desligam-se.
        bool guardado = false;
        foreach (var ev in todos)
        {
            bool persistente = ev.gameObject.scene.buildIndex == -1;
            if (persistente && !guardado) { ev.enabled = true; guardado = true; }
            else ev.enabled = false;
        }

        // Nenhum era persistente (arranque normal): fica o primeiro.
        if (!guardado && todos.Length > 0) todos[0].enabled = true;
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
    /// Uma flag de instancia NAO resolve: o objeto que a cena recem-carregada traz e
    /// uma instancia NOVA, com a flag a false — e o objeto que ja persistiu tem o Awake
    /// a correr outra vez quando e reactivado, tambem com a sua flag ja perdida em
    /// alguns casos. O estado real esta na cena a que o objeto pertence, nao numa
    /// variavel nossa.
    /// </summary>
    internal static void MarcarComoPersistente(GameObject alvo)
    {
        if (alvo.scene.buildIndex == -1) return;   // ja esta em DontDestroyOnLoad
        Object.DontDestroyOnLoad(alvo);
    }

    private void OnDestroy()
    {
        if (Vivas.TryGetValue(gameObject.name, out var dona) && dona == this)
            Vivas.Remove(gameObject.name);
    }
}

}
