using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GroundItem : MonoBehaviour, IInteractable
{
    public Item item;
    public int quantity = 1; // Add quantity support
    private bool playerInRange = false;
    public bool itemPicked = false;

    // Componentes visuais
    private SpriteRenderer spriteRenderer;
    private float initialY;

    // Configurações de efeitos visuais
    [Header("Visual Effects")]
    public bool enableFloating = true;
    public float floatHeight = 0.1f;
    public float floatSpeed = 1.0f;
    public bool enableRotation = true;
    public float rotationSpeed = 30.0f;

    [Header("Feedback Effects")]
    public GameObject pickupEffectPrefab;
    public AudioClip pickupSound;

    [Header("Visuals")]
    public Color highlightColor = new Color(1f, 1f, 0.5f, 1f);
    private Color originalColor;

    [Header("Area Collection")]
    public bool enableAreaCollection = true;
    public float collectionRadius = 1.5f;
    public GameObject areaCollectionEffectPrefab;
    public AudioClip areaSoundEffect;

    [Header("Item Movement")]
    public float moveToPlayerSpeed = 15f;
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float minimumMoveTime = 0.1f;
    public float maximumMoveTime = 0.3f;

    // Referência ao jogador e seu inventário
    private Transform playerTransform;
    private Inventory playerInventory;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisual();
    }

    private void Start()
    {
        initialY = transform.position.y;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        StartCoroutine(SpawnAnimation());
    }

    private void Update()
    {
        if (!itemPicked && enableFloating)
        {
            // Movimento de flutuação suave
            float y = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, initialY + y, transform.position.z);
        }

        if (!itemPicked && enableRotation)
        {
            // Rotação suave para dar dimensão
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
    }

    private IEnumerator SpawnAnimation()
    {
        transform.position += new Vector3(0, 0.3f, 0);
        Vector3 targetPos = new Vector3(transform.position.x, initialY, transform.position.z);

        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, elapsed/duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerTransform = other.transform;

            // Obtém referência ao inventário do jogador
            PlayerMove playerMove = other.GetComponent<PlayerMove>();
            if (playerMove != null)
            {
                playerInventory = playerMove.GetInventory();
            }

            // Destaque visual quando jogador se aproxima
            if (spriteRenderer != null)
            {
                spriteRenderer.color = highlightColor;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            // Remove o destaque quando o jogador se afasta
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    public void Interact()
    {
        if (!itemPicked && playerTransform != null && playerInventory != null)
        {
            if (enableAreaCollection)
            {
                // Colete todos os itens na área
                CollectAllItemsInArea();
            }
            else
            {
                // Coleta apenas este item
                PickupItem();
            }
        }
    }

    private void CollectAllItemsInArea()
    {
        // Encontra todos os GroundItems próximos
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, collectionRadius);
        List<GroundItem> itemsToCollect = new List<GroundItem>();

        // Adiciona este próprio item primeiro (garantindo que seja coletado)
        itemsToCollect.Add(this);

        // Encontra outros itens na área
        foreach (Collider2D col in colliders)
        {
            GroundItem otherItem = col.GetComponent<GroundItem>();
            if (otherItem != null && otherItem != this && !otherItem.itemPicked)
            {
                itemsToCollect.Add(otherItem);
            }
        }

        // Se tem apenas 1 item (este), faz coleta normal
        if (itemsToCollect.Count == 1)
        {
            PickupItem();
            return;
        }

        // Inicia a coleta com movimento em direção ao jogador
        StartCoroutine(CollectItemsWithAnimation(itemsToCollect));
    }

    private IEnumerator CollectItemsWithAnimation(List<GroundItem> itemsToCollect)
    {
        int collectedCount = 0;
        List<GroundItem> successfullyCollected = new List<GroundItem>();

        // Primeiro verifica se todos cabem no inventário
        foreach (GroundItem groundItem in itemsToCollect)
        {
            groundItem.itemPicked = true;

            if (playerInventory != null)
            {
                bool canAdd = playerInventory.CanAdd(groundItem.item, 1);
                if (!canAdd)
                {
                    groundItem.itemPicked = false;
                }
            }
        }

        // Realiza a animação de movimento para os itens que serão coletados
        List<Coroutine> movementCoroutines = new List<Coroutine>();

        foreach (GroundItem groundItem in itemsToCollect)
        {
            if (groundItem.itemPicked)
            {
                // Calcula tempo de movimento baseado na distância
                float distance = Vector2.Distance(groundItem.transform.position, playerTransform.position);
                float moveTime = Mathf.Lerp(minimumMoveTime, maximumMoveTime,
                                           distance / collectionRadius);

                // Inicia a coroutine de movimento e armazena a referência
                Coroutine moveCo = StartCoroutine(groundItem.MoveToPlayer(playerTransform, moveTime));
                movementCoroutines.Add(moveCo);

                // Adiciona à lista de coletados com sucesso
                successfullyCollected.Add(groundItem);
            }
        }

        // Aguarda um pequeno delay para variar o início do movimento dos itens
        foreach (GroundItem groundItem in successfullyCollected)
        {
            yield return new WaitForSeconds(0.05f);

            // Adiciona ao inventário enquanto os itens estão se movendo
            bool added = playerInventory.Add(groundItem.item, 1);
            if (added)
            {
                collectedCount++;
            }
        }

        // Aguarda todas as coroutines de movimento terminarem
        foreach (Coroutine co in movementCoroutines)
        {
            yield return co;
        }

        // Destrói os itens coletados
        foreach (GroundItem groundItem in successfullyCollected)
        {
            groundItem.PlayPickupEffects();
            Destroy(groundItem.gameObject, 0.1f);
        }

        // Se coletou vários itens, mostra efeito especial de área
        if (collectedCount > 1)
        {
            PlayAreaCollectionEffect(collectedCount);
        }
    }

    private IEnumerator MoveToPlayer(Transform target, float duration)
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = target.position;
        float elapsed = 0;

        // Desativa efeitos de flutuação durante o movimento
        enableFloating = false;

        // Aumenta um pouco a velocidade de rotação
        float originalRotationSpeed = rotationSpeed;
        rotationSpeed = originalRotationSpeed * 2;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float curvedT = movementCurve.Evaluate(t);

            // Move em direção ao jogador com a curva de animação
            transform.position = Vector3.Lerp(startPosition, endPosition, curvedT);

            // Diminui o tamanho gradualmente enquanto se aproxima
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.5f, curvedT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Finaliza no centro do jogador
        transform.position = endPosition;
    }

    private void PlayAreaCollectionEffect(int count)
    {
        // Efeito visual de coleta em área
        if (areaCollectionEffectPrefab != null && playerTransform != null)
        {
            GameObject effect = Instantiate(areaCollectionEffectPrefab,
                                           playerTransform.position,
                                           Quaternion.identity);

            // Ajusta a intensidade do efeito baseado na quantidade de itens
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var emission = ps.emission;
                emission.rateOverTime = count * 5;
            }

            Destroy(effect, 2f);
        }

        // Som de coleta em área
        if (areaSoundEffect != null && playerTransform != null)
        {
            AudioSource.PlayClipAtPoint(areaSoundEffect, playerTransform.position);
        }

        // Exibe texto flutuante com a quantidade coletada
        ShowFloatingText($"+{count} itens");
    }

    private void ShowFloatingText(string message)
    {
        // Se você tiver um sistema de texto flutuante, implemente aqui
    }

    private void PickupItem()
    {
        if (playerInventory != null && playerTransform != null)
        {
            bool canAdd = playerInventory.CanAdd(item, 1);
            if (canAdd)
            {
                itemPicked = true;
                StartCoroutine(CollectWithAnimation());
            }
            else
            {
                // Inventory full - item stays on ground
            }
        }
    }

    private IEnumerator CollectWithAnimation()
    {
        // Move em direção ao jogador antes de adicionar ao inventário
        yield return StartCoroutine(MoveToPlayer(playerTransform, minimumMoveTime));

        // Adiciona ao inventário após o movimento
        bool added = playerInventory.Add(item, quantity);
        if (added)
        {
            PlayPickupEffects();
            Destroy(gameObject, 0.1f);
        }
        else
        {
            // Se algo deu errado, reseta
            itemPicked = false;
        }
    }

    public void PlayPickupEffects()
    {
        // Efeito visual de coleta (partículas)
        if (pickupEffectPrefab != null && playerTransform != null)
        {
            GameObject effect = Instantiate(pickupEffectPrefab,
                                           playerTransform.position,
                                           Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Efeito sonoro de coleta
        if (pickupSound != null && playerTransform != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, playerTransform.position);
        }
    }

    private void UpdateVisual()
    {
        if (item != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = item.icon;
        }
    }

    // Método público para definir o item quando spawnar dinamicamente
    public void SetItem(Item newItem)
    {
        item = newItem;
        quantity = 1; // Default quantity
        UpdateVisual();
    }

    // Overloaded method to set item with quantity
    public void SetItem(Item newItem, int newQuantity)
    {
        item = newItem;
        quantity = Mathf.Max(1, newQuantity); // Ensure at least 1
        UpdateVisual();
    }

    // Method to set item from ItemStack
    public void SetItemStack(ItemStack itemStack)
    {
        if (itemStack != null && !itemStack.IsEmpty)
        {
            item = itemStack.item;
            quantity = itemStack.quantity;
            UpdateVisual();
        }
    }

    // Visualização da área de coleta no editor
    private void OnDrawGizmosSelected()
    {
        if (enableAreaCollection)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, collectionRadius);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, collectionRadius);
        }
    }
}
