using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SowurShield.Combat;

namespace SowurShield.Tests
{
    /// <summary>
    /// O CombatMotion so pode ser julgado com frames a correr: as rotinas sao
    /// corrotinas que interpolam ao longo do tempo, e em EditMode nenhuma avanca.
    ///
    /// O que se testa aqui e a sobreposicao entre animacoes, que e onde uma animacao
    /// por transformacao costuma partir: a unidade morre a MEIO de um ataque, com o
    /// transform ja deslocado, e o corpo tem de assentar na casa dela -- nao no sitio
    /// para onde tinha avancado.
    /// </summary>
    public class CombatMotionPlayModeTests
    {
        private GameObject go;

        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.Destroy(go);
        }

        private CombatMotion Criar(Vector3 pose, Vector3 escala)
        {
            go = new GameObject("Unidade");
            go.transform.localPosition = pose;
            go.transform.localScale = escala;
            var m = go.AddComponent<CombatMotion>();
            m.DefinirLado(true);
            m.Guardar();
            return m;
        }

        [UnityTest]
        public IEnumerator MorrerAMeioDoAtaque_DeixaOCorpoNaCasaDele()
        {
            var pose = new Vector3(3f, 1f, 0f);
            var m = Criar(pose, new Vector3(-0.34f, 0.34f, 1f));

            m.Atacar();
            yield return new WaitForSeconds(0.06f);   // a meio da investida

            var deslocado = go.transform.localPosition;
            Assert.AreNotEqual(pose.x, deslocado.x,
                "A sonda nao apanhou o ataque a meio — o teste nao provaria nada.");

            m.Morrer();
            yield return new WaitForSeconds(0.7f);    // morte inteira (0,5s) + folga

            // Y desce de proposito ao tombar; X e que nao pode ter derivado.
            Assert.AreEqual(pose.x, go.transform.localPosition.x, 0.01f,
                "O corpo assentou no ponto da investida, nao na casa da unidade.");
        }

        [UnityTest]
        public IEnumerator LevarDanoDuranteOAtaque_NaoAcumulaDeslocamento()
        {
            var pose = new Vector3(-2f, 0.5f, 0f);
            var m = Criar(pose, Vector3.one * 0.5f);

            m.Atacar();
            yield return new WaitForSeconds(0.05f);
            m.Levar(false);                            // interrompe a investida
            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(pose.x, go.transform.localPosition.x, 0.01f,
                "Duas animacoes sobrepostas deixaram a unidade fora do lugar.");
            Assert.AreEqual(pose.y, go.transform.localPosition.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator OAtaqueVoltaSempreAPoseInicial()
        {
            var pose = new Vector3(1.5f, -0.5f, 0f);
            var m = Criar(pose, Vector3.one * 0.34f);

            for (int i = 0; i < 3; i++)
            {
                m.Atacar();
                yield return new WaitForSeconds(0.4f);
            }

            Assert.AreEqual(pose.x, go.transform.localPosition.x, 0.01f,
                "Ataques repetidos fizeram a unidade derivar pelo tabuleiro.");
        }
    }
}
