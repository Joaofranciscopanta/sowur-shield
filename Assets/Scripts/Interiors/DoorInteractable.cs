using UnityEngine;
using UnityEngine.SceneManagement;
using SowurShield.Core;

namespace SowurShield.Interiors
{

/// <summary>
/// Uma porta: leva o jogador para dentro de uma casa, ou de volta para a rua.
///
/// Cada interior e uma CENA propria. A alternativa — montar os quartos algures
/// no mesmo mapa e teletransportar — evita o carregamento, mas mistura o interior
/// com o exterior na mesma cena, no mesmo minimapa e na mesma medicao de mundo.
///
/// ⚠️ Passa <c>showLoadingScreen: false</c> de proposito. O
/// <see cref="SceneTransitionManager"/> tem um minimo de 2 segundos de ecra de
/// carregamento com dicas, desenhado para ir para a batalha; aplicado a cada porta
/// tornava entrar numa loja uma espera. Com o fade apenas, a passagem e imediata.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Destino")]
    [Tooltip("Cena a carregar. Tem de estar nas Build Settings, senao o Unity recarrega " +
             "a cena atual sem erro nenhum.")]
    [SerializeField] private string cenaDestino;

    [Tooltip("Onde o jogador aparece na cena de destino. Lido pelo PlayerSpawnPoint de la.")]
    [SerializeField] private string pontoDeChegada = "Entrada";

    [Header("Aparencia")]
    [SerializeField] private string nomeDoLocal = "";
    [SerializeField] private float alcance = 1.6f;

    public string CenaDestino => cenaDestino;
    public string PontoDeChegada => pontoDeChegada;

    /// <summary>
    /// De onde o jogador veio, para a porta de saida saber onde o por de volta.
    ///
    /// Estatico e nao guardado no save: e um dado de travessia, valido entre o clique
    /// numa porta e o Start da cena seguinte. Guarda-lo no save faria o jogo restaurar
    /// uma partida a meio de uma transicao que ja terminou.
    /// </summary>
    public static string PontoDeChegadaPendente { get; private set; }

    /// <summary>
    /// Em que frame uma porta foi usada. -1 = nunca.
    ///
    /// Separado de <see cref="PontoDeChegadaPendente"/> de proposito, porque os dois
    /// respondem a perguntas diferentes e sao consumidos por gente diferente:
    ///
    /// - o PONTO diz "onde por o jogador", e o PlayerSpawnPoint limpa-o assim que o
    ///   usa, para nao valer outra vez na proxima cena;
    /// - o FRAME diz "esta chegada veio de uma porta", e e isso que o PlayerDataManager
    ///   precisa de saber para NAO restaurar a posicao gravada por cima.
    ///
    /// Com uma so variavel a ordem de execucao decidia o resultado: se o spawn point
    /// corresse primeiro (e corre), limpava a flag, e o LoadData que vinha a seguir ja
    /// nao via chegada nenhuma — atirando o jogador de volta para a posicao do save.
    /// Medido: aterrava a 5,3 unidades da porta por onde tinha acabado de sair.
    /// </summary>
    public static int FrameDaUltimaChegada { get; private set; } = -1;

    /// <summary>Se o jogador acabou de chegar por uma porta, neste frame ou no anterior.</summary>
    public static bool ChegouAgoraPorPorta =>
        FrameDaUltimaChegada >= 0 && Time.frameCount - FrameDaUltimaChegada <= 2;

    public static void LimparChegada() => PontoDeChegadaPendente = null;

    public void Interact()
    {
        if (string.IsNullOrEmpty(cenaDestino))
        {
            Debug.LogWarning($"[Porta] '{name}' nao tem cena de destino.", this);
            return;
        }

        PontoDeChegadaPendente = pontoDeChegada;
        // Marcado no carregamento, e nao aqui: o fade demora varios frames, e a janela
        // de ChegouAgoraPorPorta tem de cobrir o Start da cena NOVA.
        SceneManager.sceneLoaded -= MarcarChegada;
        SceneManager.sceneLoaded += MarcarChegada;

        var transicao = SceneTransitionManager.Instance;
        if (transicao != null)
        {
            // Sem ecra de carregamento: ver o comentario da classe.
            transicao.LoadScene(cenaDestino, showLoadingScreen: false, fadeTransition: true);
        }
        else
        {
            // Sem o gestor (uma cena aberta directamente no Editor), carregar na mesma:
            // a porta tem de funcionar em teste isolado.
            SceneManager.LoadScene(cenaDestino);
        }
    }

    /// <summary>
    /// Regista o frame em que a cena de destino ficou pronta.
    ///
    /// Estatico e desligado de si proprio a seguir: a porta que despoletou a viagem
    /// pertence a cena ANTIGA e ja foi destruida quando isto corre.
    /// </summary>
    private static void MarcarChegada(Scene cena, LoadSceneMode modo)
    {
        FrameDaUltimaChegada = Time.frameCount;
        SceneManager.sceneLoaded -= MarcarChegada;
    }

    public string GetInteractionPrompt()
    {
        return string.IsNullOrEmpty(nomeDoLocal) ? "Entrar" : $"Entrar: {nomeDoLocal}";
    }

    public bool CanInteract()
    {
        var transicao = SceneTransitionManager.Instance;
        // Clicar duas vezes durante o fade encadeava dois carregamentos.
        return transicao == null || !transicao.IsTransitioning;
    }

    public float GetInteractionRange() => alcance;

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, alcance);
    }
#endif
}

}
