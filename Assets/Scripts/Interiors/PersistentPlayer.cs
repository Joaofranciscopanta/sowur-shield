using UnityEngine;
using UnityEngine.SceneManagement;

namespace SowurShield.Interiors
{

/// <summary>
/// Faz o jogador sobreviver a mudanca de cena.
///
/// Ate aqui o jogador vivia so na SampleScene. Isso bastava porque a unica outra
/// cena de jogo era a de combate, que monta as suas proprias equipas e nao precisa
/// dele. Com interiores, cada porta leva a uma cena nova -- e sem isto o jogador
/// simplesmente NAO EXISTE la dentro: a cena carrega, mostra o quarto, e nao ha
/// ninguem para o percorrer.
///
/// <para>A alternativa seria por uma copia do jogador em cada uma das dez cenas.
/// Mas o inventario, os stats e o dinheiro vivem nos componentes deste objeto: dez
/// copias seriam dez inventarios distintos, e entrar numa loja perderia o que se
/// levava. Um so objeto que atravessa as cenas mantem um so estado.</para>
///
/// <para>Guarda contra duplicados: voltar a SampleScene carrega a cena outra vez, e
/// com ela o SEU jogador. O que ja atravessou fica, o recem-carregado destroi-se --
/// senao ficariam dois, ambos a responder ao teclado.</para>
/// </summary>
[DisallowMultipleComponent]
public class PersistentPlayer : MonoBehaviour
{
    private static PersistentPlayer instancia;

    /// <summary>O jogador que atravessa as cenas, se ja houver um.</summary>
    public static PersistentPlayer Instancia => instancia;

    /// <summary>Limpa o estatico: o Play Mode sem domain reload preserva-o entre sessoes.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Preparar() => instancia = null;

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            // Ja ha um jogador vindo de outra cena. Este e a copia que a cena trouxe
            // consigo; sai de cena antes de duplicar entradas e inventarios.
            //
            // ⚠️ O PlayerInput tem de ser desligado ANTES do Destroy. Destroy so
            // acontece no fim do frame, e ate la os dois PlayerInput existem e tentam
            // emparelhar-se com o mesmo teclado — o Unity recusa o segundo com
            // "Cannot find matching control scheme for Bunny (all control schemes are
            // already paired to matching devices)". O erro aparecia ao voltar de um
            // interior, e o jogador ficava sem responder ao teclado.
            var entrada = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (entrada != null) entrada.enabled = false;

            // SetActive(false) antes do Destroy: Destroy so remove no fim do frame, e
            // ate la o Start dos outros componentes desta copia ainda corria — dois
            // inventarios a registar-se no SaveManager, dois icones no minimapa.
            // Desativar impede isso de imediato.
            //
            // DestroyImmediate aqui NAO serve: destruir um GameObject a meio do proprio
            // Awake deixa o Unity a iterar uma lista que acabou de mudar, e sai um
            // "Assertion failed on expression: m_GameObjects.find(...)" mais um
            // MissingReferenceException nos componentes que ainda iam ser inicializados.
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        instancia = this;

        // O PlayerInput vive DESLIGADO na cena e e ligado aqui, por quem ganhou.
        //
        // Sem isto a copia da cena emparelhava-se com o teclado antes de este Awake
        // sequer correr — a ordem de Awake entre componentes do mesmo GameObject nao e
        // garantida, e o PlayerInput costuma ser primeiro. O Unity recusava entao o
        // segundo emparelhamento com "Cannot find matching control scheme for Bunny",
        // e o jogador ficava sem teclado ao voltar de um interior.
        var meuInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (meuInput != null) meuInput.enabled = true;

        // DontDestroyOnLoad exige um objeto de raiz. O jogador esta na raiz da
        // SampleScene, mas se algum dia for reparentado isto avisa em vez de falhar
        // em silencio.
        if (transform.parent != null)
        {
            Debug.LogWarning("[JogadorPersistente] O jogador tem pai; DontDestroyOnLoad " +
                             "so funciona na raiz. A soltar.", this);
            transform.SetParent(null, true);
        }

        // Marcar UMA so vez, controlado por uma flag propria.
        //
        // Chamar DontDestroyOnLoad sobre um objeto que ja esta marcado faz o Unity
        // tentar registá-lo numa lista onde ja consta, e sai um "Assertion failed on
        // expression: m_GameObjects.find(gameObject.GetEntityId()) == m_GameObjects
        // .end()". Acontecia a cada travessia porque o Awake volta a correr sempre que
        // o objeto e reactivado.
        //
        // Testar gameObject.scene.name != "DontDestroyOnLoad" parece resolver e nao
        // resolve: dentro do proprio Awake a cena do objeto ainda nao foi actualizada,
        // entao a comparacao passava e o DontDestroyOnLoad corria na mesma.
        // Delegado ao PersistentRoot: a regra e a mesma, e a guarda correta e a cena
        // do objeto (buildIndex -1 = ja esta em DontDestroyOnLoad), nao uma flag nossa.
        PersistentRoot.MarcarComoPersistente(gameObject);
    }

    private void OnEnable()
    {
        // O jogador tambem nao pertence a toda a parte: a CombatScene monta as suas
        // proprias equipas, e um Bunny persistente ficaria a andar por cima do
        // combate. Destroi-se ao chegar a uma cena que nao seja de jogo; voltar a
        // vila recarrega-o com a cena.
        SceneManager.sceneLoaded -= AoMudarDeCena;
        SceneManager.sceneLoaded += AoMudarDeCena;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= AoMudarDeCena;
    }

    private void AoMudarDeCena(Scene cena, LoadSceneMode modo)
    {
        if (PersistentRoot.EhCenaDeJogo(cena.name)) return;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }
}

}
