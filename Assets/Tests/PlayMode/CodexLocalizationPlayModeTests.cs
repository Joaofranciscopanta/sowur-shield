using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Localization.Settings;
using SowurShield.Dialogue;

namespace SowurShield.Tests
{
    /// <summary>
    /// O codex (bio + lore dos NPCs) tem de seguir o idioma escolhido.
    ///
    /// Durante meses nao seguiu: os 9 NPCs tinham as 81 entradas em portugues CRU, com
    /// `keyId = 0` em todas. Como a build abre em `en`, quem jogasse em ingles via o
    /// jogo inteiro traduzido -- menus, itens, dialogos -- e o codex em portugues. Havia
    /// ate um `CodexLocalizationTool` escrito para isto, que **nunca tinha sido corrido**.
    ///
    /// Este teste corre em PlayMode porque trocar de locale e assincrono e precisa de
    /// frames; em EditMode o `SelectedLocale` nao chega a propagar.
    ///
    /// ⚠️ Nao basta afirmar que o campo esta "ligado": um `LocalizedString` mal ligado
    /// devolve a string de erro "No translation found..." em vez de lancar. Por isso o
    /// teste compara o TEXTO entre idiomas, e nao o keyId.
    /// </summary>
    public class CodexLocalizationPlayModeTests
    {
        private static NPCDialogueInteractable Achar(string nome)
        {
            foreach (var n in Object.FindObjectsByType<NPCDialogueInteractable>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (n.gameObject.name == nome) return n;
            return null;
        }

        private static IEnumerator TrocarPara(string codigo)
        {
            foreach (var loc in LocalizationSettings.AvailableLocales.Locales)
            {
                if (loc.Identifier.Code != codigo) continue;
                LocalizationSettings.SelectedLocale = loc;
                yield return null;
                yield break;
            }
            Assert.Fail($"Locale '{codigo}' nao existe no projeto.");
        }

        [UnityTest]
        public IEnumerator ABioDoNPC_MudaComOIdioma()
        {
            var npc = Achar("Rui");
            if (npc == null) Assert.Ignore("Rui nao esta na cena aberta.");

            yield return TrocarPara("en");
            string en = npc.GetBio();

            yield return TrocarPara("pt");
            string pt = npc.GetBio();

            Assert.IsNotEmpty(en, "A bio veio vazia em ingles.");
            StringAssert.DoesNotContain("No translation found", en,
                "A bio esta ligada a uma chave que nao existe na tabela.");
            Assert.AreNotEqual(pt, en,
                "A bio nao muda com o idioma — o campo continua em texto cru, "
                + "que e o defeito que o CodexLocalizationTool existe para corrigir.");
        }

        [UnityTest]
        public IEnumerator OTituloDaLore_MudaComOIdioma()
        {
            var npc = Achar("Joana");
            if (npc == null) Assert.Ignore("Joana nao esta na cena aberta.");

            yield return TrocarPara("en");
            var loreEn = npc.GetUnlockedLore();
            if (loreEn.Length == 0) Assert.Ignore("Nenhuma entrada de lore desbloqueada de inicio.");
            string en = loreEn[0].GetTitle();

            yield return TrocarPara("pt");
            string pt = npc.GetUnlockedLore()[0].GetTitle();

            StringAssert.DoesNotContain("No translation found", en,
                "O titulo da lore aponta para uma chave inexistente.");
            Assert.AreNotEqual(pt, en, "O titulo da lore nao segue o idioma.");
        }

        [UnityTest]
        public IEnumerator NenhumCampoDoCodex_FicouEmTextoCru()
        {
            yield return TrocarPara("en");

            var falhas = new System.Text.StringBuilder();
            foreach (var npc in Object.FindObjectsByType<NPCDialogueInteractable>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // NPCs sem lore nenhuma sao placeholders (generic_npc, chicken): nao
                // tem codex para traduzir, entao nao contam como falha.
                if (npc.GetTotalLoreCount() == 0) continue;

                string bio = npc.GetBio();
                if (string.IsNullOrEmpty(bio) || bio.Contains("No translation found"))
                    falhas.AppendLine($"  {npc.gameObject.name}: bio nao resolve");

                foreach (var e in npc.GetUnlockedLore())
                {
                    if (string.IsNullOrEmpty(e.GetTitle()) || e.GetTitle().Contains("No translation found"))
                        falhas.AppendLine($"  {npc.gameObject.name}: titulo nao resolve");
                    if (string.IsNullOrEmpty(e.GetBody()) || e.GetBody().Contains("No translation found"))
                        falhas.AppendLine($"  {npc.gameObject.name}: corpo nao resolve");
                }
            }

            Assert.IsEmpty(falhas.ToString(), "Campos de codex sem traducao:\n" + falhas);
        }
    }
}
