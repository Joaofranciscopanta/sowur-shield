using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SowurShield.Core;

namespace SowurShield.Farming
{
    /// <summary>
    /// Spot de pesca. O jogador interage com a FishingRod equipada,
    /// dispara animacao de lancar, espera um tempo aleatorio e
    /// spawna um peixe (GroundItem) ao lado do jogador.
    /// Feedback visual: bobber na agua, "!" ao morder, texto flutuante de resultado.
    /// </summary>
    public class FishingSpot : MonoBehaviour, IInteractable
    {
        [Header("Fishing Settings")]
        [Tooltip("Tempo minimo de espera antes do peixe morder (segundos)")]
        public float minWaitTime = 2f;
        [Tooltip("Tempo maximo de espera antes do peixe morder (segundos)")]
        public float maxWaitTime = 5f;
        [Tooltip("Chance base de pegar um peixe (0-1)")]
        public float catchChance = 0.75f;
        [Tooltip("Chance de pegar peixe raro em vez de comum (0-1)")]
        public float rareFishChance = 0.15f;

        [Header("Items")]
        [Tooltip("Prefab do GroundItem de peixe comum")]
        public GameObject fishGroundItemPrefab;
        [Tooltip("Prefab do GroundItem de peixe raro")]
        public GameObject rareFishGroundItemPrefab;

        [Header("Visual Feedback")]
        [Tooltip("Sprite do exclamation mark quando peixe morde")]
        public GameObject biteIndicatorPrefab;

        private bool isFishing;
        private SpriteRenderer spriteRenderer;

        // Runtime visual objects
        private GameObject bobberObject;
        private GameObject statusTextObject;
        private LineRenderer fishingLine;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Interact()
        {
            if (isFishing) return;

            // Verificar se o jogador tem FishingRod equipada
            var inventory = FindAnyObjectByType<SowurShield.Inventory.Inventory>();
            if (inventory == null) return;

            var selectedItem = inventory.GetSelectedItem();
            if (selectedItem == null || selectedItem.itemTags == null ||
                !selectedItem.itemTags.Contains("FishingRod"))
            {
                ShowFloatingText(transform.position + Vector3.up * 0.8f,
                    "Equipe a vara!", new Color(1f, 0.8f, 0.3f));
                return;
            }

            // Disparar animacao de pesca no jogador
            var playerMove = FindAnyObjectByType<PlayerMove>();
            if (playerMove != null)
            {
                Vector2 dir = (Vector2)(transform.position - playerMove.transform.position);
                playerMove.FaceDirection(dir);
                playerMove.TriggerActionAnimation("Fish");
                playerMove.DisableMovement();
            }

            StartCoroutine(FishingRoutine(playerMove));
        }

        private IEnumerator FishingRoutine(PlayerMove playerMove)
        {
            isFishing = true;
            SowurShield.Core.SFXManager.Play("FishCast");

            Vector3 playerPos = playerMove != null
                ? playerMove.transform.position
                : transform.position + Vector3.down;

            // --- FASE 1: Lançar linha (0.5s delay para a animação de cast) ---
            yield return new WaitForSeconds(0.5f);

            // Criar bobber (boia) na água
            Vector3 bobberPos = transform.position + new Vector3(
                Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.2f), 0);
            bobberObject = CreateBobber(bobberPos);

            // Criar linha de pesca do jogador até o bobber
            fishingLine = CreateFishingLine(playerPos + Vector3.up * 0.3f, bobberPos);

            // Texto "Pescando..."
            ShowFloatingText(playerPos + Vector3.up * 0.9f,
                "Pescando...", new Color(0.6f, 0.85f, 1f), persistent: true);

            // --- FASE 2: Esperar peixe morder ---
            float waitTime = Random.Range(minWaitTime, maxWaitTime);

            // Bobber faz pequenas oscilações enquanto espera
            float elapsed = 0f;
            while (elapsed < waitTime)
            {
                if (bobberObject != null)
                {
                    float bobY = Mathf.Sin(elapsed * 3f) * 0.03f;
                    bobberObject.transform.position = bobberPos + new Vector3(0, bobY, 0);
                }
                // Atualizar linha
                if (fishingLine != null && playerMove != null)
                {
                    fishingLine.SetPosition(0, playerMove.transform.position + Vector3.up * 0.3f);
                    if (bobberObject != null)
                        fishingLine.SetPosition(1, bobberObject.transform.position);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Limpar texto "Pescando..."
            DestroyStatusText();

            // --- FASE 3: Peixe mordeu! ---
            SowurShield.Core.SFXManager.Play("FishBite");
            // Bobber afunda rapidamente
            if (bobberObject != null)
            {
                StartCoroutine(BobberSplash(bobberObject.transform.position));
                // Sacudir o bobber
                for (int i = 0; i < 6; i++)
                {
                    if (bobberObject != null)
                    {
                        bobberObject.transform.position = bobberPos +
                            new Vector3(Random.Range(-0.06f, 0.06f), -0.05f * i, 0);
                    }
                    yield return new WaitForSeconds(0.05f);
                }
            }

            // Indicador "!" acima do bobber
            GameObject indicator = CreateBiteIndicator(bobberPos + Vector3.up * 0.5f);

            yield return new WaitForSeconds(0.6f);

            if (indicator != null) Destroy(indicator);

            // --- FASE 4: Resultado ---
            // Limpar bobber e linha
            CleanupVisuals();

            bool caught = Random.value <= catchChance;
            if (caught)
            {
                SowurShield.Core.SFXManager.Play("FishCatch");
                bool isRare = Random.value <= rareFishChance;
                SpawnFish(playerMove, isRare);

                string msg = isRare ? "Peixe Raro!" : "Pegou um peixe!";
                Color color = isRare ? new Color(1f, 0.85f, 0.2f) : new Color(0.3f, 1f, 0.5f);
                ShowFloatingText(playerPos + Vector3.up * 0.9f, msg, color);
            }
            else
            {
                // The fish getting away is a small denial, so it shares the denied sting
                // rather than needing a clip of its own.
                SowurShield.Core.SFXManager.Play("Denied");
                ShowFloatingText(playerPos + Vector3.up * 0.9f,
                    "Escapou...", new Color(0.8f, 0.4f, 0.4f));
            }

            // Reabilitar movimento
            if (playerMove != null)
                playerMove.EnableMovement();

            isFishing = false;
        }

        // ====== Visual Helpers ======

        private GameObject CreateBobber(Vector3 pos)
        {
            var go = new GameObject("FishingBobber");
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;

            // Criar sprite procedural: circulo vermelho/branco (boia)
            int size = 8;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f - 0.5f, size / 2f - 0.5f));
                    if (dist < size / 2f - 0.5f)
                        pixels[y * size + x] = y >= size / 2 ? Color.red : Color.white;
                    else
                        pixels[y * size + x] = Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 16f);

            return go;
        }

        private LineRenderer CreateFishingLine(Vector3 from, Vector3 to)
        {
            var go = new GameObject("FishingLine");
            var lr = go.AddComponent<LineRenderer>();
            lr.startWidth = 0.02f;
            lr.endWidth = 0.015f;
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(0.55f, 0.45f, 0.35f);
            lr.endColor = new Color(0.55f, 0.45f, 0.35f, 0.7f);
            lr.sortingOrder = 9;
            lr.useWorldSpace = true;
            return lr;
        }

        private GameObject CreateBiteIndicator(Vector3 pos)
        {
            // Se tiver prefab customizado, usar ele
            if (biteIndicatorPrefab != null)
                return Instantiate(biteIndicatorPrefab, pos, Quaternion.identity);

            // Senao, criar "!" procedural
            var go = new GameObject("BiteIndicator");
            go.transform.position = pos;

            var tm = go.AddComponent<TextMesh>();
            tm.text = "!";
            tm.fontSize = 48;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.yellow;
            tm.fontStyle = FontStyle.Bold;

            // Sorting
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 100;
            }

            // Escalar para ficar visível
            StartCoroutine(PulseEffect(go.transform, 0.6f));

            return go;
        }

        private IEnumerator PulseEffect(Transform t, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration && t != null)
            {
                float scale = 1f + Mathf.Sin(elapsed * 12f) * 0.3f;
                t.localScale = Vector3.one * scale;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator BobberSplash(Vector3 pos)
        {
            // Criar particulas de splash simples (circulos azuis que se espalham)
            for (int i = 0; i < 5; i++)
            {
                var splash = new GameObject("Splash");
                splash.transform.position = pos;
                var sr = splash.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 11;
                sr.color = new Color(0.5f, 0.7f, 1f, 0.8f);

                int size = 4;
                var tex = new Texture2D(size, size);
                tex.filterMode = FilterMode.Point;
                Color[] pixels = new Color[size * size];
                for (int p = 0; p < pixels.Length; p++)
                {
                    float x = p % size - size / 2f + 0.5f;
                    float y = p / size - size / 2f + 0.5f;
                    pixels[p] = (x * x + y * y < size * 0.6f) ?
                        new Color(0.6f, 0.8f, 1f, 0.9f) : Color.clear;
                }
                tex.SetPixels(pixels);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), 16f);

                Vector2 dir = Random.insideUnitCircle.normalized;
                StartCoroutine(MoveSplashParticle(splash, dir));
            }

            yield return null;
        }

        private IEnumerator MoveSplashParticle(GameObject particle, Vector2 dir)
        {
            float speed = Random.Range(0.8f, 1.5f);
            float life = Random.Range(0.3f, 0.5f);
            float elapsed = 0;

            while (elapsed < life && particle != null)
            {
                particle.transform.position += (Vector3)(dir * speed * Time.deltaTime);
                var sr = particle.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b,
                        1f - (elapsed / life));
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (particle != null) Destroy(particle);
        }

        private void ShowFloatingText(Vector3 pos, string text, Color color, bool persistent = false)
        {
            // Limpar texto anterior se persistente
            if (persistent) DestroyStatusText();

            var go = new GameObject("FishingText");
            go.transform.position = pos;

            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 36;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.fontStyle = FontStyle.Bold;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.sortingOrder = 100;

            if (persistent)
            {
                statusTextObject = go;
                StartCoroutine(FloatAnimation(go.transform));
            }
            else
            {
                StartCoroutine(FloatAndFade(go));
            }
        }

        private IEnumerator FloatAnimation(Transform t)
        {
            Vector3 basePos = t.position;
            float time = 0;
            while (t != null)
            {
                t.position = basePos + Vector3.up * Mathf.Sin(time * 2f) * 0.05f;
                time += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator FloatAndFade(GameObject go)
        {
            float duration = 1.5f;
            float elapsed = 0;
            Vector3 startPos = go.transform.position;
            var tm = go.GetComponent<TextMesh>();
            Color startColor = tm.color;

            while (elapsed < duration && go != null)
            {
                go.transform.position = startPos + Vector3.up * (elapsed * 0.4f);
                float alpha = 1f - (elapsed / duration);
                tm.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (go != null) Destroy(go);
        }

        private void DestroyStatusText()
        {
            if (statusTextObject != null)
            {
                Destroy(statusTextObject);
                statusTextObject = null;
            }
        }

        private void CleanupVisuals()
        {
            if (bobberObject != null) { Destroy(bobberObject); bobberObject = null; }
            if (fishingLine != null) { Destroy(fishingLine.gameObject); fishingLine = null; }
            DestroyStatusText();
        }

        // ====== Original Methods ======

        private void SpawnFish(PlayerMove playerMove, bool rare)
        {
            GameObject prefab = rare ? rareFishGroundItemPrefab : fishGroundItemPrefab;
            if (prefab == null)
            {
                string path = rare
                    ? "Prefabs/GroundItems/RareFish_GroundItem"
                    : "Prefabs/GroundItems/Fish_GroundItem";
                prefab = Resources.Load<GameObject>(path);
            }

            if (prefab == null)
            {
                Debug.LogWarning("FishingSpot: prefab de peixe nao encontrado!");
                return;
            }

            Vector3 spawnPos = playerMove != null
                ? playerMove.transform.position + (Vector3)(Random.insideUnitCircle.normalized * 0.5f)
                : transform.position + Vector3.up;

            string uniqueName = prefab.name + "_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            GameObject fish = Instantiate(prefab, spawnPos, Quaternion.identity);
            fish.name = uniqueName;
        }

        public void OnPlayerEnterRange()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(0.8f, 0.95f, 1f, 1f);
        }

        public void OnPlayerExitRange()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;
        }

        public string GetInteractionPrompt()
        {
            return "Pescar";
        }

        public bool CanInteract()
        {
            return !isFishing;
        }

        public float GetInteractionRange()
        {
            return 1.5f;
        }

        private void OnDisable()
        {
            CleanupVisuals();
        }
    }
}
