using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SowurShield.Core;
using SowurShield.Dialogue;

namespace SowurShield.Tests
{
    /// <summary>
    /// Falar com um NPC nunca pode deixar o jogador congelado.
    ///
    /// Relatado a jogar a build em 2026-09-05: interagir com um NPC "faz um barulho tipo
    /// de erro e trava". A causa: o `NPCDialogueInteractable.StartDialogue` punha
    /// `isDialogueActive = true` e chamava `DisableMovement()` **antes** de pedir a UI
    /// para abrir; a UI recusava em silencio (por outra janela estar aberta, pela arvore
    /// nao validar, ou por nao haver no inicial) e ninguem desfazia nada. O "barulho de
    /// erro" era o `SFXManager.Play("Denied")` do `UIManager.TryOpenWindow`.
    /// </summary>
    public class NPCInteractionPlayModeTests
    {
        private static NPCDialogueInteractable AcharNPC()
        {
            foreach (var n in Object.FindObjectsByType<NPCDialogueInteractable>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (n.CanInteract()) return n;
            return null;
        }

        [UnityTest]
        public IEnumerator ConversaRecusada_NaoDeixaOJogadorCongelado()
        {
            var npc = AcharNPC();
            if (npc == null) Assert.Ignore("Nenhum NPC disponivel na cena aberta.");

            var jogador = Object.FindFirstObjectByType<PlayerMove>();
            if (jogador == null) Assert.Ignore("Sem PlayerMove na cena.");

            var ui = Object.FindFirstObjectByType<DialogueTreeUI>(FindObjectsInactive.Include);
            if (ui == null || UIManager.Instance == null) Assert.Ignore("Sem UI de dialogo.");

            // Ocupar a pilha do UIManager: e o que faz o TryOpenWindow recusar e tocar
            // "Denied". Usamos a propria UI de dialogo como ocupante, que e um IUIWindow.
            Assert.IsTrue(UIManager.Instance.TryOpenWindow(ui),
                "A pilha ja estava ocupada — o teste nao mediria o que quer.");

            yield return null;
            jogador.EnableMovement();

            npc.Interact();
            yield return null;

            Assert.IsTrue(jogador.IsMovementEnabled(),
                "O jogador ficou sem movimento depois de uma conversa RECUSADA — "
                + "e exatamente o 'trava' que o Lucas descreveu.");
            Assert.IsFalse(npc.IsDialogueActive(),
                "O NPC acha que a conversa esta ativa, mas ela nunca abriu.");

            UIManager.Instance.TryCloseWindow(ui);
            jogador.EnableMovement();
        }

        [UnityTest]
        public IEnumerator ConversaAceite_AbreEDevolveOMovimentoAoFechar()
        {
            var npc = AcharNPC();
            if (npc == null) Assert.Ignore("Nenhum NPC disponivel na cena aberta.");

            var jogador = Object.FindFirstObjectByType<PlayerMove>();
            var ui = Object.FindFirstObjectByType<DialogueTreeUI>(FindObjectsInactive.Include);
            if (jogador == null || ui == null) Assert.Ignore("Cena incompleta.");

            npc.Interact();
            yield return null;

            Assert.IsTrue(npc.IsDialogueActive(), "A conversa nao abriu num caso valido.");

            ui.EndDialogue();
            yield return null;

            Assert.IsTrue(jogador.IsMovementEnabled(),
                "O movimento nao voltou depois de fechar a conversa.");
        }

        [UnityTest]
        public IEnumerator MoverUmNPC_MantemOColisorNoMesmoSitio()
        {
            var npc = AcharNPC();
            if (npc == null) Assert.Ignore("Nenhum NPC disponivel na cena aberta.");

            var col = npc.GetComponent<Collider2D>();
            if (col == null) Assert.Ignore("NPC sem Collider2D.");

            var destino = npc.transform.position + new Vector3(5f, 3f, 0f);
            npc.transform.position = destino;

            // Isto e o que o editor de mapa tem de fazer: com o jogo pausado o
            // FixedUpdate nao corre, e sem a sincronizacao o colisor fica para tras --
            // o NPC aparece no sitio novo e nao e alvo de interacao nenhuma.
            Physics2D.SyncTransforms();
            yield return null;

            float desvio = Vector2.Distance(col.bounds.center, npc.transform.position + (Vector3)col.offset);
            Assert.Less(desvio, 0.01f,
                $"O colisor ficou a {desvio:0.00} unidades do NPC depois de o mover — "
                + "interagir com ele nao faria nada, nem som de erro.");
        }
    }
}
