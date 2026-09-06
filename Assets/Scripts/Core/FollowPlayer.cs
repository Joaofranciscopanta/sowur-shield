using UnityEngine;

namespace SowurShield.Core
{

public class FollowPlayer : MonoBehaviour
{
    public Transform player;  // Drag your player here
    public float smoothSpeed = 3f;
    public Vector3 offset = new Vector3(0, 0, -10);

    void LateUpdate()
    {
        // Reencontrar o jogador quando a referencia morre.
        //
        // O jogador atravessa cenas (ver PersistentPlayer), mas cada cena traz a SUA
        // copia, que se destroi ao chegar. A camera de uma cena recem-carregada aponta
        // para essa copia — ou seja, para um objeto ja destruido — e ficaria parada,
        // olhando para o sitio errado, sem erro nenhum na consola.
        //
        // Tambem cobre as cenas de interior, cuja camera nasce sem referencia nenhuma.
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            player = go.transform;
        }

        transform.position = player.position + offset;
    }
}

} // namespace SowurShield.Core
