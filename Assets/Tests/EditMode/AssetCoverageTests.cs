using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using SowurShield.Animals;
using SowurShield.Combat;

namespace SowurShield.Tests
{
    /// <summary>
    /// Guarda as lacunas de asset que foram fechadas em 2026-09-05, para nao reabrirem
    /// em silencio.
    ///
    /// Estas lacunas viveram meses sem ninguem dar por elas porque **nao produzem erro**:
    /// um `sprite` nulo faz o CombatUnit cair numa esfera cinza (com warning, que se
    /// perde no meio do log) e um `animatorController` nulo simplesmente nao anima. O
    /// jogo corre na mesma.
    ///
    /// ⚠️ A documentacao errou a dimensao das tres lacunas, sempre para MAIS: dizia 14
    /// inimigos sem sprite (eram 2) e 19 skills sem icone (eram 2). Contar por teste
    /// e mais barato do que voltar a confiar num numero escrito a mao.
    /// </summary>
    public class AssetCoverageTests
    {
        [Test]
        public void TodoInimigo_TemSprite()
        {
            var sem = Resources.LoadAll<EnemyData>("Enemies")
                               .Where(e => e != null && e.sprite == null)
                               .Select(e => e.name)
                               .ToList();

            Assert.IsEmpty(sem,
                "Inimigo sem sprite renderiza como esfera cinza via CreateSphereVisual(): "
                + string.Join(", ", sem));
        }

        [Test]
        public void TodoAnimal_TemSpriteEAnimator()
        {
            var sb = new StringBuilder();

            foreach (var a in Resources.LoadAll<AnimalData>("Animals"))
            {
                if (a == null) continue;
                if (a.idleSprite == null) sb.AppendLine($"  {a.name}: sem idleSprite");
                if (a.animatorController == null) sb.AppendLine($"  {a.name}: sem animatorController");
            }

            Assert.IsEmpty(sb.ToString(),
                "Animal sem sprite ou sem animator fica parado/invisivel:\n" + sb);
        }

        [Test]
        public void TodaSkill_TemIcone()
        {
            var sem = Resources.LoadAll<AnimalSkill>("AnimalSkills")
                               .Where(s => s != null && s.skillIcon == null)
                               .Select(s => s.name)
                               .ToList();

            Assert.IsEmpty(sem, "Skill sem icone deixa buraco na UI: " + string.Join(", ", sem));
        }
    }
}
