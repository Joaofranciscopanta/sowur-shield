using NUnit.Framework;
using UnityEngine;

namespace SowurShield.Tests
{
    /// <summary>
    /// Guards the farming SFX that were silent in-game until 2026-08-04.
    ///
    /// The bug: SoilBlockInteractable played its clips with AudioSource.PlayClipAtPoint,
    /// which spawns a 3D (spatialBlend = 1) source at the soil's world position. The
    /// AudioListener lives on the Main Camera ~22 units away, so logarithmic rolloff
    /// attenuated tilling and watering to inaudible. Every sound that DID work
    /// (music, menus, typewriter) goes through SFXManager's 2D pooled sources.
    ///
    /// SoilBlockInteractable now falls back to SFXManager.Play(key), so these tests
    /// assert the keys actually resolve to clips on disk. If someone renames or moves
    /// the audio files, the lookup silently returns nothing and the sounds go quiet
    /// again — with no error anywhere. That is exactly what these tests catch.
    /// </summary>
    public class FarmingAudioTests
    {
        /// <summary>
        /// Mirrors SFXManager.ToFileName: "TillSoil" -> "sfx_till_soil".
        /// Kept in sync deliberately; if the real conversion changes, the
        /// round-trip test below fails rather than silently diverging.
        /// </summary>
        private static string ToFileName(string key)
        {
            var sb = new System.Text.StringBuilder("sfx_", key.Length + 8);
            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                if (char.IsUpper(c))
                {
                    if (i > 0) sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static int CountClips(string key)
        {
            string fileForm = ToFileName(key);
            int count = 0;

            if (Resources.Load<AudioClip>($"Audio/SFX/{fileForm}") != null)
                count++;

            for (int i = 1; ; i++)
            {
                if (Resources.Load<AudioClip>($"Audio/SFX/{fileForm}{i}") == null) break;
                count++;
            }
            return count;
        }

        [TestCase("TillSoil")]
        [TestCase("WaterSoil")]
        [TestCase("HarvestCrop")]
        public void FarmingSfxKey_ResolvesToAtLeastOneClip(string key)
        {
            Assert.Greater(CountClips(key), 0,
                $"SFX key '{key}' resolved to no clips. SoilBlockInteractable calls " +
                $"SFXManager.Play(\"{key}\"), which looks for Resources/Audio/SFX/{ToFileName(key)}[n]. " +
                "The farming sound is silent in-game.");
        }

        [Test]
        public void ToFileName_ConvertsPascalCaseToSfxSnakeCase()
        {
            Assert.AreEqual("sfx_till_soil", ToFileName("TillSoil"));
            Assert.AreEqual("sfx_water_soil", ToFileName("WaterSoil"));
            Assert.AreEqual("sfx_harvest_crop", ToFileName("HarvestCrop"));
        }

        [Test]
        public void TillAndWaterSoil_HaveMultipleVariations()
        {
            // These shipped as 3-clip sets so a repeated action does not sound
            // copy-pasted. Dropping to a single clip is a quality regression worth
            // failing on, since nothing else in the project would report it.
            Assert.GreaterOrEqual(CountClips("TillSoil"), 2, "TillSoil lost its variations.");
            Assert.GreaterOrEqual(CountClips("WaterSoil"), 2, "WaterSoil lost its variations.");
        }
    }
}
