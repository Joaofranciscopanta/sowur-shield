using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SowurShield.Core;

namespace SowurShield.Tests
{
    /// <summary>
    /// Dormir tem de deixar a pilha de janelas LIMPA.
    ///
    /// Relatado a jogar a build em 2026-09-05: "coloquei as sementes no lugar para os
    /// animais comerem, quando fui dormir no outro dia tudo estava dando Denied em todos
    /// os botoes."
    ///
    /// A causa: a cama mostra o painel de confirmacao a mao, sem passar pelo UIManager, e
    /// ninguem fechava o que ja estava aberto. O comedouro ficava na pilha, e uma janela
    /// na pilha faz `TryOpenWindow` recusar TODAS as outras -- tocando "Denied" a cada
    /// clique. Durante o fade de sono o painel some de vista, entao parecia fechado.
    ///
    /// Reproduzido antes do conserto: dia 4 -> 5 com FeedingTrough ainda na pilha.
    /// </summary>
    public class SleepWindowStackPlayModeTests
    {
        private static System.Collections.Generic.Stack<IUIWindow> Pilha()
        {
            var f = typeof(UIManager).GetField("openWindowStack",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return f?.GetValue(UIManager.Instance) as System.Collections.Generic.Stack<IUIWindow>;
        }

        [UnityTest]
        public IEnumerator DormirComUmaJanelaAberta_NaoBloqueiaTudoNoDiaSeguinte()
        {
            if (UIManager.Instance == null) Assert.Ignore("Sem UIManager na cena.");

            var cama = Object.FindFirstObjectByType<BedInteractable>(FindObjectsInactive.Include);
            if (cama == null) Assert.Ignore("Sem cama na cena aberta.");

            // Qualquer IUIWindow serve como a janela que fica esquecida aberta.
            IUIWindow vitima = null;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (mb is IUIWindow j && !(mb is BedInteractable)) { vitima = j; break; }
            if (vitima == null) Assert.Ignore("Nenhuma janela para o teste.");

            UIManager.Instance.ForceCloseAllWindows();
            yield return null;

            Assert.IsTrue(UIManager.Instance.TryOpenWindow(vitima),
                "Nao consegui abrir a janela — o teste nao mediria nada.");
            Assert.AreEqual(1, Pilha().Count, "A janela nao entrou na pilha.");

            // Dormir SEM fechar a janela: exatamente o que o Lucas fez.
            var mi = typeof(BedInteractable).GetMethod("SleepSequence",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (mi == null) Assert.Ignore("SleepSequence nao encontrado.");

            var rotina = mi.Invoke(cama, null) as IEnumerator;
            // Correr so o inicio da sequencia: e ali que a limpeza tem de acontecer,
            // antes do fade. Nao esperamos o dia inteiro avancar.
            if (rotina != null) rotina.MoveNext();
            yield return null;

            Assert.AreEqual(0, Pilha().Count,
                "A janela ficou presa na pilha depois de dormir — no dia seguinte TODOS "
                + "os botoes respondem com o som 'Denied'.");

            // E a prova final: outra janela consegue abrir?
            Assert.IsTrue(UIManager.Instance.TryOpenWindow(vitima),
                "Abrir uma janela foi recusado depois de dormir.");
            UIManager.Instance.ForceCloseAllWindows();
        }

        [UnityTest]
        public IEnumerator ForceCloseAllWindows_LimpaPilhaEOrfas()
        {
            if (UIManager.Instance == null) Assert.Ignore("Sem UIManager na cena.");

            int abertas = 0;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!(mb is IUIWindow j)) continue;
                if (UIManager.Instance.TryOpenWindow(j)) abertas++;
            }
            yield return null;

            UIManager.Instance.ForceCloseAllWindows();
            yield return null;

            Assert.AreEqual(0, Pilha().Count, "A pilha nao ficou vazia.");

            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (mb is IUIWindow j)
                    Assert.IsFalse(j.IsWindowOpen,
                        $"'{j.WindowName}' continuou aberta apos ForceCloseAllWindows.");
        }
    }
}
