using NUnit.Framework;
using UnityEngine;
using SowurShield.MapEditor;

namespace SowurShield.Tests
{
    /// <summary>
    /// A ponte do codex escreve na StringTable, nunca em texto cru.
    ///
    /// Essa distincao e o ponto todo: os campos legados (`npcBio`, `title`, `body`) sao
    /// strings simples que NAO traduzem, e foi assim que as 81 entradas do codex ficaram
    /// meses so em portugues sem dar erro nenhum. Um painel que gravasse texto cru
    /// reproduziria o defeito a cada entrada nova.
    ///
    /// A implementacao vive num assembly Editor-only, entao estes testes verificam o
    /// CONTRATO -- que e o que o painel de runtime consegue ver.
    /// </summary>
    public class CodexBridgeTests
    {
        // O runner abre uma cena vazia, entao sem isto os testes que precisam de NPCs
        // pulavam sempre -- e um teste que pula sempre nao testa nada. Carregamos a
        // SampleScene sem a marcar suja; no fim nao gravamos.
        private static bool cenaCarregada;

        [OneTimeSetUp]
        public void CarregarCena()
        {
            if (Object.FindObjectsByType<SowurShield.Dialogue.NPCDialogueInteractable>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
                return;

            var caminho = "Assets/Scenes/SampleScene.unity";
            if (!System.IO.File.Exists(caminho)) return;

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                caminho, UnityEditor.SceneManagement.OpenSceneMode.Additive);
            cenaCarregada = true;
        }

        [OneTimeTearDown]
        public void DescarregarCena()
        {
            if (!cenaCarregada) return;
            var cena = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(
                "Assets/Scenes/SampleScene.unity");
            if (cena.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(cena, true);
            cenaCarregada = false;
        }

        [Test]
        public void APonte_EstaRegistradaNoEditor()
        {
            // O registro e por [InitializeOnLoad]: se alguem partir isso, o painel abre
            // vazio e sem erro, que e o modo de falhar mais caro de diagnosticar.
            Assert.IsTrue(CodexBridge.Disponivel,
                "CodexBridge.Atual e null — o CodexRuntimeBridge nao se registrou. "
                + "O painel do codex abre sem dados e sem mensagem de erro.");
        }

        [Test]
        public void APonte_EnumeraOsTresIdiomas()
        {
            if (!CodexBridge.Disponivel) Assert.Ignore("Ponte nao registrada.");

            var idiomas = CodexBridge.Atual.Idiomas();
            CollectionAssert.Contains(idiomas, "pt");
            CollectionAssert.Contains(idiomas, "en");
            CollectionAssert.Contains(idiomas, "es");
        }

        [Test]
        public void APonte_SoListaPersonagensComCodex()
        {
            if (!CodexBridge.Disponivel) Assert.Ignore("Ponte nao registrada.");

            foreach (var npc in CodexBridge.Atual.Personagens())
            {
                Assert.Greater(CodexBridge.Atual.QuantasEntradas(npc), 0,
                    $"'{npc.gameObject.name}' foi listado sem ter lore nenhuma — "
                    + "placeholders (generic_npc, chicken) so poluem a lista.");
            }
        }

        [Test]
        public void OLimiar_FicaEntreMenos100E100()
        {
            if (!CodexBridge.Disponivel) Assert.Ignore("Ponte nao registrada.");

            var pessoas = CodexBridge.Atual.Personagens();
            if (pessoas.Count == 0) Assert.Ignore("Nenhum NPC com codex na cena aberta.");

            var npc = pessoas[0];
            float original = CodexBridge.Atual.LerLimiar(npc, 0);

            // Fora do intervalo tem de ser preso: um limiar de 500 torna a entrada
            // impossivel de desbloquear, e nada avisaria.
            CodexBridge.Atual.EscreverLimiar(npc, 0, 500f);
            Assert.LessOrEqual(CodexBridge.Atual.LerLimiar(npc, 0), 100f,
                "Um limiar acima de 100 nunca desbloqueia e nao da erro.");

            CodexBridge.Atual.EscreverLimiar(npc, 0, -500f);
            Assert.GreaterOrEqual(CodexBridge.Atual.LerLimiar(npc, 0), -100f);

            CodexBridge.Atual.EscreverLimiar(npc, 0, original);
        }

        [Test]
        public void OTextoGravado_VoltaPelaTabelaNoMesmoIdioma()
        {
            if (!CodexBridge.Disponivel) Assert.Ignore("Ponte nao registrada.");

            var pessoas = CodexBridge.Atual.Personagens();
            if (pessoas.Count == 0) Assert.Ignore("Nenhum NPC com codex na cena aberta.");

            var npc = pessoas[0];
            string original = CodexBridge.Atual.LerBio(npc, "pt");
            const string sonda = "SONDA DE TESTE";

            Assert.IsTrue(CodexBridge.Atual.EscreverBio(npc, "pt", sonda),
                "EscreverBio devolveu false — nao ha onde gravar.");
            Assert.AreEqual(sonda, CodexBridge.Atual.LerBio(npc, "pt"));

            // E o ingles NAO pode ter mudado: cada idioma e a sua propria entrada.
            Assert.AreNotEqual(sonda, CodexBridge.Atual.LerBio(npc, "en"),
                "Escrever em pt sobrescreveu o texto em ingles — as tabelas colidiram.");

            CodexBridge.Atual.EscreverBio(npc, "pt", original);
            Assert.AreEqual(original, CodexBridge.Atual.LerBio(npc, "pt"),
                "A restauracao falhou — o teste deixou o projeto sujo.");
        }
    }
}
