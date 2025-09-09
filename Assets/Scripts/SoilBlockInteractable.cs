using UnityEngine;
using System.Collections;

public class SoilBlockInteractable : MonoBehaviour, IInteractable
{
    public enum SoilState
    {
        Regular,
        Tilled,
        Watered,
        WithCrop
    }

    [Header("Solo")]
    public SoilState currentState = SoilState.Regular;
    public Sprite regularSprite;
    public Sprite tilledSprite;
    public Sprite wateredSprite;

    [Header("Tags de Ferramentas")]
    public string hoeTag = "Hoe";
    public string wateringCanTag = "WateringCan";
    public string shovelTag = "Shovel";
    public string scytheTag = "Scythe";

    [Header("Efeitos")]
    public GameObject tillEffect;
    public GameObject waterEffect;
    public GameObject plantEffect;
    public GameObject shovelEffect;
    public GameObject harvestEffect;

    [Header("Audio")]
    public AudioClip tillSound;
    public AudioClip waterSound;
    public AudioClip plantSound;
    public AudioClip shovelSound;
    public AudioClip harvestSound;

    [Header("Feedback Visual")]
    public Color highlightColor = new Color(1f, 1f, 0.5f, 1f);
    public bool enableHighlightOnHover = true;
    private Color originalColor;
    private bool playerInRange = false;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // Componentes
    private SpriteRenderer soilRenderer;
    private CropGrowthManager cropGrowthManager;
    private Inventory playerInventory;
    private Transform playerTransform;
    private Vector3Int gridPosition;

    // Propriedades para acesso externo
    public SoilState CurrentState => currentState;
    public bool HasCrop => cropGrowthManager != null && cropGrowthManager.HasCrop;
    public bool IsReadyForHarvest => cropGrowthManager != null && cropGrowthManager.IsReadyForHarvest;
    public bool IsCropDead => cropGrowthManager != null && cropGrowthManager.IsDead;
    public CropData CurrentCrop => cropGrowthManager?.CurrentCrop;
    public bool PlayerInRange => playerInRange;

    private void Awake()
    {
        InitializeComponents();
        SetupGridPosition();
        EnsureCollider();
        SubscribeToCropEvents();
        StoreOriginalColor();
    }

    private void InitializeComponents()
    {
        // Obtém componentes necessários
        soilRenderer = GetComponent<SpriteRenderer>();
        cropGrowthManager = GetComponent<CropGrowthManager>();

        // Adiciona CropGrowthManager se não estiver presente
        if (cropGrowthManager == null)
        {
            cropGrowthManager = gameObject.AddComponent<CropGrowthManager>();
        }

        // Define sprite padrão
        if (regularSprite == null && soilRenderer != null)
            regularSprite = soilRenderer.sprite;
    }

    private void SetupGridPosition()
    {
        // Calcula posição no grid para rastreamento do cursor controller
        gridPosition = CursorController.GetWorldPosTile(transform.position);
    }

    private void EnsureCollider()
    {
        // Garante que existe um collider para interação
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.9f, 0.9f);
            collider.isTrigger = true;
        }
    }

    private void SubscribeToCropEvents()
    {
        // Inscreve-se nos eventos do gerenciador de cultivo
        if (cropGrowthManager != null)
        {
            cropGrowthManager.OnCropGrown += OnCropGrown;
            cropGrowthManager.OnCropReadyForHarvest += OnCropReadyForHarvest;
            cropGrowthManager.OnCropDied += OnCropDied;
            cropGrowthManager.OnCropHarvested += OnCropHarvested;
        }
    }

    private void StoreOriginalColor()
    {
        // Armazena cor original para destacamento visual
        if (soilRenderer != null)
        {
            originalColor = soilRenderer.color;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        UnregisterFromCursorController();
    }

    private void UnsubscribeFromEvents()
    {
        // Desinscreve-se dos eventos
        if (cropGrowthManager != null)
        {
            cropGrowthManager.OnCropGrown -= OnCropGrown;
            cropGrowthManager.OnCropReadyForHarvest -= OnCropReadyForHarvest;
            cropGrowthManager.OnCropDied -= OnCropDied;
            cropGrowthManager.OnCropHarvested -= OnCropHarvested;
        }
    }

    private void UnregisterFromCursorController()
    {
        // Remove do rastreamento do cursor controller
        CursorController cursorController = FindFirstObjectByType<CursorController>();
        if (cursorController != null)
        {
            cursorController.UnregisterSoilBlock(gridPosition);
        }
    }

    #region Detecção de Proximidade do Jogador (similar ao GroundItem)

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerTransform = other.transform;

            // Obtém referências do jogador
            PlayerMove playerMove = other.GetComponent<PlayerMove>();
            if (playerMove != null)
            {
                playerInventory = playerMove.GetInventory();
            }

            // Aplica destaque visual se habilitado
            if (enableHighlightOnHover && soilRenderer != null)
            {
                soilRenderer.color = highlightColor;
            }

            if (showDebugInfo)
                Debug.Log($"Player entrou no range do solo: {GetStatusText()}");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            // Remove destaque visual
            if (enableHighlightOnHover && soilRenderer != null)
            {
                soilRenderer.color = originalColor;
            }

            if (showDebugInfo)
                Debug.Log("Player saiu do range do solo");
        }
    }

    #endregion

    // Inicializa com referência do inventário do jogador
    public void Initialize(Inventory inventory)
    {
        playerInventory = inventory;
    }

    // Chamado quando criado com enxada (já arado)
    public void TillSoilDirectly()
    {
        if (currentState == SoilState.Regular)
        {
            currentState = SoilState.Tilled;
            UpdateAppearance();
            PlayEffect(tillEffect);
            PlaySound(tillSound);

            if (showDebugInfo)
                Debug.Log("Solo arado diretamente");
        }
    }

    public void Interact()
    {
        EnsurePlayerInventory();

        Item selectedItem = playerInventory?.GetSelectedItem();

        if (selectedItem == null)
        {
            HandleEmptyHandInteraction();
        }
        else
        {
            HandleItemInteraction(selectedItem);
        }
    }

    private void EnsurePlayerInventory()
    {
        // Garante que temos referência do inventário do jogador
        if (playerInventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerMove playerMove = player.GetComponent<PlayerMove>();
                if (playerMove != null)
                {
                    playerInventory = playerMove.GetInventory();
                }
            }

            if (playerInventory == null)
            {
                Debug.LogError("Não foi possível encontrar o inventário do jogador.");
            }
        }
    }

    private void HandleEmptyHandInteraction()
    {
        // Pode colher com mãos vazias
        if (currentState == SoilState.WithCrop && IsReadyForHarvest)
        {
            HarvestCrop();
        }
        else
        {
            ProvideFeedback(null);
        }
    }

    private void HandleItemInteraction(Item selectedItem)
    {
        // Ordem de prioridade: Colher > Remover > Arar > Regar > Plantar

        // 1. Colher com foice (ou qualquer ferramenta se foice não for obrigatória)
        if (currentState == SoilState.WithCrop && IsReadyForHarvest)
        {
            if (string.IsNullOrEmpty(scytheTag) || HasTag(selectedItem, scytheTag))
            {
                HarvestCrop();
                return;
            }
        }

        // 2. Remover cultivo/resetar solo com pá
        if (HasTag(selectedItem, shovelTag) && currentState != SoilState.Regular)
        {
            ResetSoil();
            return;
        }

        // 3. Arar solo com enxada
        if (currentState == SoilState.Regular && HasTag(selectedItem, hoeTag))
        {
            TillSoil();
            return;
        }

        // 4. Regar solo/cultivo com regador
        if ((currentState == SoilState.Tilled || currentState == SoilState.WithCrop) &&
            HasTag(selectedItem, wateringCanTag))
        {
            WaterSoil();
            return;
        }

        // 5. Plantar sementes
        if ((currentState == SoilState.Tilled || currentState == SoilState.Watered) &&
            selectedItem.itemType == ItemType.Seed)
        {
            PlantSeed(selectedItem);
            return;
        }

        // Se não há interação válida, fornece feedback
        ProvideFeedback(selectedItem);
    }

    private bool HasTag(Item item, string tag)
    {
        if (string.IsNullOrEmpty(tag) || item == null || item.itemTags == null)
            return false;

        return item.itemTags.Contains(tag);
    }

    #region Ações do Solo

    private void TillSoil()
    {
        if (currentState != SoilState.Regular)
            return;

        currentState = SoilState.Tilled;
        UpdateAppearance();
        PlayEffect(tillEffect);
        PlaySound(tillSound);

        Debug.Log("Solo arado com sucesso!");
    }

    private void WaterSoil()
    {
        if (currentState == SoilState.Tilled)
        {
            currentState = SoilState.Watered;
            UpdateAppearance();
            PlayEffect(waterEffect);
            PlaySound(waterSound);

            Debug.Log("Solo regado!");
        }
        else if (currentState == SoilState.WithCrop && cropGrowthManager != null)
        {
            cropGrowthManager.WaterCrop();
            UpdateAppearance(); // Atualiza aparência caso cultivo estivesse morrendo
            PlayEffect(waterEffect);
            PlaySound(waterSound);

            Debug.Log("Planta regada!");
        }
    }

    private void PlantSeed(Item seedItem)
    {
        if (playerInventory == null) return;

        // Encontra dados do cultivo para esta semente
        CropData cropData = CropDatabase.GetCropDataForSeed(seedItem);

        if (cropData == null)
        {
            Debug.Log("Esta semente não pode ser plantada aqui ou dados do cultivo não foram encontrados.");
            ProvideFeedback(seedItem);
            return;
        }

        // Verifica requisitos sazonais (se sistema de estações estiver implementado)
        GameTimeController timeController = GameTimeController.instance;
        if (timeController != null)
        {
            // Aqui você pode implementar verificação de estação se necessário
            // Por exemplo: if (!cropData.IsValidSeason(GetCurrentSeason())) return;
        }

        // Planta o cultivo
        if (cropGrowthManager.PlantCrop(cropData))
        {
            currentState = SoilState.WithCrop;
            UpdateAppearance();

            // Remove semente do inventário
            playerInventory.Remove(seedItem, 1);

            PlayEffect(plantEffect);
            PlaySound(plantSound);

            Debug.Log($"Plantou {seedItem.itemName}!");
        }
        else
        {
            Debug.Log("Falha ao plantar semente - solo pode já ter um cultivo");
        }
    }

    private void HarvestCrop()
    {
        if (showDebugInfo)
            Debug.Log($"HarvestCrop called - HasCrop: {HasCrop}, IsReadyForHarvest: {IsReadyForHarvest}");

        if (!HasCrop || !IsReadyForHarvest)
        {
            if (showDebugInfo)
                Debug.Log("Cannot harvest - crop not ready or doesn't exist");
            return;
        }

        StartCoroutine(HarvestWithAnimation());
    }

    private IEnumerator HarvestWithAnimation()
{
    if (showDebugInfo)
        Debug.Log($"Starting harvest animation for crop: {CurrentCrop?.cropName}");

    // IMPORTANT: Store crop data BEFORE calling HarvestCrop()
    // because HarvestCrop() might remove the crop (set currentCrop to null)
    CropData cropToHarvest = CurrentCrop;

    if (cropToHarvest == null)
    {
        if (showDebugInfo)
            Debug.Log("Cannot harvest - no crop data available");
        yield break;
    }

    // Get yield from crop manager
    int yield = cropGrowthManager.HarvestCrop();

    if (showDebugInfo)
        Debug.Log($"Crop manager returned yield: {yield}");

    if (yield > 0)
    {
        // Use the stored crop data to get harvest item
        Item harvestItem = cropToHarvest.harvestItem;

        if (harvestItem == null)
        {
            Debug.LogError($"Harvest item is null for crop: {cropToHarvest.cropName}");
            yield break;
        }

        if (showDebugInfo)
            Debug.Log($"Spawning {yield} ground items of {harvestItem.itemName}");

        for (int i = 0; i < yield; i++)
        {
            SpawnGroundItem(harvestItem, i, yield);
        }

        // Visual and sound effects
        PlayEffect(harvestEffect);
        PlaySound(harvestSound);

        // Small pause for harvest animation
        yield return new WaitForSeconds(0.5f);

        Debug.Log($"Harvested {yield}x {harvestItem.itemName}!");
    }
    else
    {
        if (showDebugInfo)
            Debug.Log($"No yield returned from crop manager");
    }

    // Update soil state based on whether crop still exists (regrowth)
    if (!HasCrop)
    {
        currentState = SoilState.Tilled;
        if (showDebugInfo)
            Debug.Log("No crop remaining - setting soil to tilled");
    }

    UpdateAppearance();
}

    private void SpawnGroundItem(Item item, int index, int totalItems)
    {
        if (showDebugInfo)
            Debug.Log($"SpawnGroundItem called for {item?.itemName}, index: {index}");

        if (item == null)
        {
            Debug.LogError("Cannot spawn ground item - item is null");
            return;
        }

        // Cria um GameObject para o GroundItem
        GameObject groundItemObj = new GameObject($"GroundItem_{item.itemName}");
        groundItemObj.transform.position = transform.position;

        if (showDebugInfo)
            Debug.Log($"Created GameObject at position: {groundItemObj.transform.position}");

        // Adiciona componentes necessários
        SpriteRenderer sr = groundItemObj.AddComponent<SpriteRenderer>();
        sr.sprite = item.icon;
        sr.sortingOrder = 10; // Garante que fica visível sobre o solo

        if (showDebugInfo)
            Debug.Log($"Added SpriteRenderer with sprite: {item.icon?.name}");

        // Adiciona collider
        CircleCollider2D collider = groundItemObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.3f;

        // Adiciona o componente GroundItem
        GroundItem groundItem = groundItemObj.AddComponent<GroundItem>();
        groundItem.SetItem(item);

        if (showDebugInfo)
            Debug.Log($"Added GroundItem component and set item");

        // Posiciona os itens em um pequeno círculo ao redor da planta
        if (totalItems > 1)
        {
            float angle = (360f / totalItems) * index;
            float radius = 0.5f;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                0
            );
            groundItemObj.transform.position += offset;

            if (showDebugInfo)
                Debug.Log($"Positioned item {index} at angle {angle} with offset {offset}");
        }
        else
        {
            // Para um único item, adiciona um pequeno offset aleatório
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(-0.3f, 0.3f),
                0
            );
            groundItemObj.transform.position += randomOffset;

            if (showDebugInfo)
                Debug.Log($"Single item positioned with random offset: {randomOffset}");
        }

        if (showDebugInfo)
            Debug.Log($"Final ground item position: {groundItemObj.transform.position}");
    }

    private void ResetSoil()
    {
        SoilState previousState = currentState;

        // Remove cultivo se presente
        if (HasCrop)
        {
            cropGrowthManager.RemoveCrop();
        }

        currentState = SoilState.Regular;
        UpdateAppearance();
        PlayEffect(shovelEffect);
        PlaySound(shovelSound);

        // Fornece feedback baseado no que foi removido
        switch (previousState)
        {
            case SoilState.Tilled:
                Debug.Log("Solo arado foi nivelado.");
                break;
            case SoilState.Watered:
                Debug.Log("Solo molhado foi nivelado.");
                break;
            case SoilState.WithCrop:
                Debug.Log("Planta removida e solo nivelado.");
                break;
        }
    }

    #endregion

    #region Eventos do Cultivo

    private void OnCropGrown(CropGrowthManager manager)
    {
        UpdateAppearance();
        if (showDebugInfo)
            Debug.Log($"Cultivo cresceu para estágio {manager.CurrentGrowthStage + 1}");
    }

    private void OnCropReadyForHarvest(CropGrowthManager manager)
    {
        UpdateAppearance();
        if (showDebugInfo)
            Debug.Log($"{manager.CurrentCrop.cropName} está pronto para colheita!");
    }

    private void OnCropDied(CropGrowthManager manager)
    {
        UpdateAppearance();
        if (showDebugInfo)
            Debug.Log($"{manager.CurrentCrop.cropName} morreu!");
    }

    private void OnCropHarvested(CropGrowthManager manager)
    {
        // Tratado no método HarvestCrop
        UpdateAppearance();
    }

    #endregion

    #region Feedback e Interface

    private void ProvideFeedback(Item selectedItem)
    {
        if (currentState == SoilState.WithCrop)
        {
            if (IsCropDead)
            {
                Debug.Log("Esta planta está morta. Use uma pá para removê-la.");
                return;
            }

            if (IsReadyForHarvest)
            {
                Debug.Log("Este cultivo está pronto para colheita!");
                if (!string.IsNullOrEmpty(scytheTag) && selectedItem != null && !HasTag(selectedItem, scytheTag))
                    Debug.Log($"Use uma ferramenta com tag '{scytheTag}' para colheita mais eficiente.");
                return;
            }

            // Mostra informações do crescimento do cultivo
            if (cropGrowthManager != null)
            {
                Debug.Log(cropGrowthManager.GetCropInfo());
                if (CurrentCrop != null && CurrentCrop.requiresWater && !cropGrowthManager.IsWatered)
                    Debug.Log("Este cultivo precisa de água! Use um regador.");
            }
            return;
        }

        // Feedback para estados do solo
        switch (currentState)
        {
            case SoilState.Regular:
                Debug.Log("Use uma enxada para arar este solo antes de plantar.");
                break;
            case SoilState.Tilled:
                Debug.Log("Este solo está pronto para plantio. Você pode plantar sementes ou regá-lo primeiro.");
                break;
            case SoilState.Watered:
                Debug.Log("Este solo está molhado e pronto para plantio de sementes.");
                break;
        }

        // Feedback adicional para ferramentas inválidas
        if (selectedItem != null)
        {
            if (selectedItem.itemType == ItemType.Seed && currentState == SoilState.Regular)
            {
                Debug.Log("Are o solo com uma enxada antes de plantar sementes.");
            }
        }
    }

    public void UpdateAppearance()
    {
        if (soilRenderer == null) return;

        // Mantém cor original ou destaque baseado na proximidade do jogador
        Color colorToUse = (enableHighlightOnHover && playerInRange) ? highlightColor : originalColor;

        switch (currentState)
        {
            case SoilState.Regular:
                soilRenderer.sprite = regularSprite;
                break;
            case SoilState.Tilled:
                soilRenderer.sprite = tilledSprite;
                break;
            case SoilState.Watered:
                soilRenderer.sprite = wateredSprite ?? tilledSprite;
                break;
            case SoilState.WithCrop:
                // Mostra sprite molhado se cultivo foi regado recentemente, senão arado
                bool showWatered = cropGrowthManager != null && cropGrowthManager.IsWatered;
                soilRenderer.sprite = (showWatered && wateredSprite != null) ? wateredSprite : tilledSprite;
                break;
        }

        soilRenderer.color = colorToUse;
    }

    #endregion

    #region Audio e Efeitos

    private void PlayEffect(GameObject effectPrefab)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }

    #endregion

    #region Getters Públicos (para sistemas externos)

    public string GetStatusText()
    {
        if (HasCrop && cropGrowthManager != null)
        {
            return cropGrowthManager.GetCropInfo();
        }

        return currentState switch
        {
            SoilState.Regular => "Solo não arado",
            SoilState.Tilled => "Solo arado - pronto para plantio",
            SoilState.Watered => "Solo molhado - pronto para plantio",
            _ => "Estado do solo desconhecido"
        };
    }

    public float GetCropGrowthProgress()
    {
        return cropGrowthManager?.GrowthProgress ?? 0f;
    }

    public SoilState GetSoilState()
    {
        return currentState;
    }

    public CropData GetCurrentCrop()
    {
        return CurrentCrop;
    }

    public bool IsCropReadyForHarvest()
    {
        return IsReadyForHarvest;
    }
    #endregion
}
