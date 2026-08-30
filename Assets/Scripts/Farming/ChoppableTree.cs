using UnityEngine;
using System.Collections;
using SowurShield.Core;
using SowurShield.Inventory;

namespace SowurShield.Farming
{
    /// <summary>
    /// Uma arvore que pode ser cortada com o machado. A cada golpe balanca e solta
    /// lenha; ao atingir o numero de golpes cai (animacao) e vira toco.
    /// Implementa IInteractable: responde ao clique (via CursorController) e ao E.
    /// </summary>
    public class ChoppableTree : MonoBehaviour, IInteractable
    {
        [Header("Chopping")]
        [SerializeField] private int hitsToFell = 3;
        [SerializeField] private string axeTag = "Axe";
        [SerializeField] private GameObject woodDropPrefab;
        [SerializeField] private int woodPerHit = 1;
        [SerializeField] private int woodOnFell = 2;
        [SerializeField] private float interactionRange = 1.2f;

        private int hitsTaken = 0;
        private bool felled = false;
        private Animator animator;
        private Collider2D bodyCollider;
        private SowurShield.Inventory.Inventory playerInventory;
        private PlayerMove playerMove;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            bodyCollider = GetComponent<Collider2D>();
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerInventory = player.GetComponent<SowurShield.Inventory.Inventory>();
                playerMove = player.GetComponent<PlayerMove>();
            }

            if (InteractionManager.Instance != null)
                InteractionManager.Instance.RegisterInteractable(this);
        }

        private void OnDestroy()
        {
            if (InteractionManager.Instance != null)
                InteractionManager.Instance.UnregisterInteractable(this);
        }

        #region IInteractable

        public bool CanInteract() => !felled;

        public float GetInteractionRange() => interactionRange;

        public string GetInteractionPrompt()
        {
            if (felled) return "";
            return HoldingAxe() ? "Chop tree" : "Tree (needs an axe)";
        }

        public void Interact()
        {
            if (felled || !HoldingAxe()) return;

            // Vira o Bunny para a arvore e toca a animacao de machado
            if (playerMove != null)
            {
                playerMove.FaceDirection((Vector2)(transform.position - playerMove.transform.position));
                playerMove.TriggerActionAnimation("Axe");
            }

            hitsTaken++;
            SowurShield.Core.SFXManager.Play("AxeChop");
            StopAllCoroutines();
            StartCoroutine(ShakeThenDrop());

            if (hitsTaken >= hitsToFell)
                Fell();
        }

        #endregion

        private bool HoldingAxe()
        {
            if (playerInventory == null) return false;
            Item selected = playerInventory.GetSelectedItem();
            return selected != null && selected.itemTags != null && selected.itemTags.Contains(axeTag);
        }

        private IEnumerator ShakeThenDrop()
        {
            float t = 0f;
            const float dur = 0.2f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                float angle = Mathf.Sin(k * Mathf.PI * 3f) * 5f * (1f - k);
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            transform.rotation = Quaternion.identity;

            if (!felled)
                DropWood(woodPerHit);
        }

        private void Fell()
        {
            felled = true;
            SowurShield.Core.SFXManager.Play("TreeFall");

            if (bodyCollider != null)
                bodyCollider.enabled = false;

            if (animator != null)
                animator.SetTrigger("Fall");

            DropWood(woodOnFell);

            if (InteractionManager.Instance != null)
                InteractionManager.Instance.UnregisterInteractable(this);
        }

        private void DropWood(int amount)
        {
            if (woodDropPrefab == null || amount <= 0) return;

            for (int i = 0; i < amount; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.3f, 0.1f), 0f);
                Instantiate(woodDropPrefab, transform.position + offset, Quaternion.identity);
            }
        }
    }
}
