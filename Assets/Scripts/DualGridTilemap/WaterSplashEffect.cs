using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SowurShield.Farming
{
    /// <summary>
    /// Efeito visual de respingo d'agua ao regar. Gera um GameObject temporario
    /// no tile regado e anima os frames do regador, destruindo-se ao terminar.
    /// Funciona em qualquer direcao pois aparece sobre o tile-alvo.
    /// </summary>
    public class WaterSplashEffect : MonoBehaviour
    {
        public float fps = 12f;

        private static Sprite[] _frames;

        /// <summary>Cria o respingo na posicao de mundo informada.</summary>
        public static void Spawn(Vector3 worldPos)
        {
            Sprite[] frames = LoadFrames();
            if (frames == null || frames.Length == 0)
                return;

            GameObject go = new GameObject("WaterSplash");
            go.transform.position = worldPos;
            go.transform.localScale = Vector3.one * 4f; // respingo ~300% maior

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 9999;
            sr.sprite = frames[0];

            WaterSplashEffect fx = go.AddComponent<WaterSplashEffect>();
            fx.StartCoroutine(fx.Play(sr, frames));
        }

        private static Sprite[] LoadFrames()
        {
            if (_frames != null)
                return _frames;

            Sprite[] all = Resources.LoadAll<Sprite>("Effects/WaterCanFrames");
            List<Sprite> list = new List<Sprite>();
            foreach (Sprite s in all)
            {
                // Ignora os frames grandes quase vazios; mantem so as gotas
                if (s.rect.width <= 40f && s.rect.height <= 40f)
                    list.Add(s);
            }
            list.Sort((a, b) => FrameIndex(a.name).CompareTo(FrameIndex(b.name)));
            _frames = list.ToArray();
            return _frames;
        }

        private static int FrameIndex(string spriteName)
        {
            int u = spriteName.LastIndexOf('_');
            if (u >= 0 && int.TryParse(spriteName.Substring(u + 1), out int n))
                return n;
            return 0;
        }

        private IEnumerator Play(SpriteRenderer sr, Sprite[] frames)
        {
            float dt = 1f / Mathf.Max(1f, fps);
            for (int i = 0; i < frames.Length; i++)
            {
                sr.sprite = frames[i];
                yield return new WaitForSeconds(dt);
            }
            Destroy(gameObject);
        }
    }
}
