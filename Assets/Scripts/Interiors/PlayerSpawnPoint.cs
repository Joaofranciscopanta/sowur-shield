using UnityEngine;

namespace SowurShield.Interiors
{

/// <summary>
/// Onde o jogador aparece ao chegar a esta cena por uma porta.
///
/// Cada cena pode ter varios: entrar pela porta da frente e sair da cave devem
/// deixar o jogador em sitios diferentes. A porta diz por qual pergunta
/// (<see cref="DoorInteractable.PontoDeChegadaPendente"/>), e este componente
/// responde se o nome bater.
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    [Tooltip("Nome que as portas usam para pedir este ponto.")]
    [SerializeField] private string nome = "Entrada";

    [Tooltip("Usado quando a cena e aberta sem vir de porta nenhuma — teste no Editor, " +
             "ou um save carregado. So um ponto por cena deve ter isto ligado.")]
    [SerializeField] private bool ehPadrao = false;

    public string Nome => nome;
    public bool EhPadrao => ehPadrao;

    private void Start()
    {
        // Start, e nao Awake: o jogador pode ainda nao existir em Awake se for
        // instanciado pela propria cena.
        var pedido = DoorInteractable.PontoDeChegadaPendente;

        bool souEu = !string.IsNullOrEmpty(pedido)
            ? string.Equals(pedido, nome, System.StringComparison.OrdinalIgnoreCase)
            : ehPadrao;

        if (!souEu) return;

        var jogador = GameObject.FindGameObjectWithTag("Player");
        if (jogador == null)
        {
            Debug.LogWarning($"[PontoDeChegada] '{nome}': nao ha nenhum objeto com a tag Player.", this);
            return;
        }

        var p = transform.position;
        p.z = jogador.transform.position.z;   // preservar a profundidade do jogador
        jogador.transform.position = p;

        // A fisica 2D so sincroniza no FixedUpdate: sem isto o colisor fica onde
        // o jogador estava antes, e ele atravessa paredes ate ao proximo passo.
        Physics2D.SyncTransforms();

        // Consumido: se ficasse pendente, voltar a esta cena por outra porta
        // continuaria a usar o ponto antigo.
        DoorInteractable.LimparChegada();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = ehPadrao ? Color.green : new Color(1f, 0.85f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.7f);
    }
#endif
}

}
