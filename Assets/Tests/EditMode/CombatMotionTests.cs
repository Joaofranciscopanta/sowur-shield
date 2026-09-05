using NUnit.Framework;
using UnityEngine;
using SowurShield.Combat;

namespace SowurShield.Tests
{
    /// <summary>
    /// O CombatMotion mexe no transform de unidades cuja escala NAO e 1 e cujo X pode ser
    /// negativo (o NormalizeSpriteSize ajusta a altura para 0,8 e as unidades do jogador
    /// levam flip em X para olhar para a direita).
    ///
    /// O que estes testes protegem e exatamente isso: uma animacao que ATRIBUA escala em
    /// vez de multiplicar pela base desfaz o flip e o tamanho, e a unidade fica virada ao
    /// contrario ou do tamanho errado quando a animacao acaba. E a mesma armadilha da
    /// paleta do editor, noutro sitio.
    /// </summary>
    public class CombatMotionTests
    {
        private GameObject go;

        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        private CombatMotion Criar(Vector3 escala)
        {
            go = new GameObject("Unidade");
            go.transform.localScale = escala;
            var m = go.AddComponent<CombatMotion>();
            m.Guardar();
            return m;
        }

        [Test]
        public void Repousar_DevolveAEscalaComFlip_SemAEndireitar()
        {
            // Unidade do jogador: X negativo de proposito.
            var escala = new Vector3(-0.34f, 0.34f, 1f);
            var m = Criar(escala);

            m.Morrer();                 // mexe na escala
            go.transform.localScale = new Vector3(99f, 99f, 99f);  // suja
            m.Repousar();

            Assert.AreEqual(escala.x, go.transform.localScale.x, 1e-4f,
                "O flip em X foi perdido — a unidade do jogador passa a olhar para o lado errado.");
            Assert.AreEqual(escala.y, go.transform.localScale.y, 1e-4f,
                "A escala normalizada foi perdida — a unidade muda de tamanho apos a animacao.");
        }

        [Test]
        public void Guardar_DepoisDeNormalizar_UsaAEscalaFinalENaoADoAwake()
        {
            // O Awake corre ANTES do NormalizeSpriteSize. Se o CombatMotion so guardasse
            // no Awake, guardaria escala 1 e devolveria a unidade ao tamanho errado.
            go = new GameObject("Unidade");
            var m = go.AddComponent<CombatMotion>();   // Awake ve escala 1

            go.transform.localScale = Vector3.one * 0.34f;   // normalizacao acontece depois
            m.Guardar();                                     // CombatUnit chama isto

            go.transform.localScale = Vector3.one * 99f;
            m.Repousar();

            Assert.AreEqual(0.34f, go.transform.localScale.y, 1e-4f,
                "Guardou a escala do Awake em vez da escala final normalizada.");
        }

        [Test]
        public void Repousar_LimpaARotacaoDaMorte()
        {
            var m = Criar(Vector3.one * 0.5f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, -75f);

            m.Repousar();

            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, go.transform.localRotation), 0.01f,
                "A unidade fica tombada ao reviver.");
        }

        [Test]
        public void SemSpriteRenderer_NaoLanca()
        {
            // Inimigos sem arte caem na esfera cinza e nao tem SpriteRenderer nenhum.
            var m = Criar(Vector3.one);

            Assert.DoesNotThrow(() => { m.Atacar(); m.Levar(true); m.Morrer(); m.Estado(Color.green); },
                "Unidade sem SpriteRenderer (esfera placeholder) rebenta ao animar.");
        }

        [Test]
        public void Guardar_NaoEChamadoDuasVezesComPoseSuja()
        {
            // Reentrar em combate nao pode acumular deslocamento: se o Guardar corresse
            // com a unidade ja deslocada, a pose de repouso passava a ser a deslocada.
            var m = Criar(Vector3.one);
            var poseOriginal = go.transform.localPosition;

            m.Atacar();
            m.Repousar();

            Assert.AreEqual(poseOriginal, go.transform.localPosition,
                "A pose de repouso derivou depois de uma animacao.");
        }
    }
}
