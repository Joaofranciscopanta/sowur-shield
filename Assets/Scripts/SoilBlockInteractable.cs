using UnityEngine;
using System.Collections;
using System.Linq;
using SowurShield.Inventory;
using SowurShield.Farming;

namespace SowurShield.Core
{

public class SoilBlockInteractable : MonoBehaviour, IInteractable, ISaveable
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

    [Header("Fertility")]
    [Tooltip("Soil nutrient level (0.5–2.0). Boosts harvest yield; depletes after each harvest.")]
    [SerializeField] [Range(0.5f, 2f)] private float soilFertility = 1.0f;

    [Header("Debug")]

    // Componentes
    private SpriteRenderer soilRenderer;
    private CropGrowthManager cropGrowthManager;
    private SowurShield.Inventory.Inventory playerInventory;
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
        RegisterWithSaveManager();
    }

    private void InitializeComponents()
    {
        soilRenderer = GetComponent<SpriteRenderer>();
        cropGrowthManager = GetComponent<CropGrowthManager>();

        if (cropGrowthManager == null)
            cropGrowthManager = gameObject.AddComponent<CropGrowthManager>();

        // Load custom soil sprites from Resources if not wired in Inspector
        if (regularSprite == null)
            regularSprite = Resources.Load<Sprite>("Sprites/Plain_Dirt");
        if (tilledSprite == null)
            tilledSprite = Resources.Load<Sprite>("Sprites/Tilled_Dirt_Planted");
        if (wateredSprite == null)
            wateredSprite = Resources.Load<Sprite>("Sprites/Tilled_Dirt_Watered");

        // Final fallback: use whatever sprite is already on the renderer
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
            originalColor = soilRenderer.color;
    }

    private void RegisterWithSaveManager()
    {
        // Register with SaveManager for save/load functionality
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
        }
        else
        {
            // Try again in Start() - SaveManager might not be initialized yet
            StartCoroutine(DelayedRegistration());
        }
    }

    private System.Collections.IEnumerator DelayedRegistration()
    {
        // Wait a frame for SaveManager to initialize
        yield return null;
        
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        UnregisterFromCursorController();

        // Unregister from SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
        }
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
        }
    }

    #endregion

    // Inicializa com referência do inventário do jogador
    public void Initialize(SowurShield.Inventory.Inventory inventory)
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
        }
    }

    public string GetInteractionPrompt() => HasCrop && IsReadyForHarvest ? "Harvest" : "Interact";
    public bool CanInteract() => true;
    public float GetInteractionRange() => 2f;

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
        TutorialManager.NotifyStepComplete("till_soil");
    }

    private void WaterSoil()
    {
        if (currentState == SoilState.Tilled)
        {
            currentState = SoilState.Watered;
            UpdateAppearance();
            PlayEffect(waterEffect);
            PlaySound(waterSound);
            TutorialManager.NotifyStepComplete("water_crop");
        }
        else if (currentState == SoilState.WithCrop && cropGrowthManager != null)
        {
            cropGrowthManager.WaterCrop();
            UpdateAppearance(); // Atualiza aparência caso cultivo estivesse morrendo
            PlayEffect(waterEffect);
            PlaySound(waterSound);
        }
    }

    private void PlantSeed(Item seedItem)
    {
        if (playerInventory == null) return;

        // Encontra dados do cultivo para esta semente
        CropData cropData = CropDatabase.GetCropDataForSeed(seedItem);

        if (cropData == null)
        {
            ProvideFeedback(seedItem);
            return;
        }

        // Enforce seasonal restrictions — Greenhouse bypasses them
        bool greenhouseBuilt = SowurShield.Core.FarmBuildingManager.Instance != null
                               && SowurShield.Core.FarmBuildingManager.Instance.HasGreenhouse;
        if (!greenhouseBuilt)
        {
            GameTimeController timeController = GameTimeController.instance;
            if (timeController != null)
            {
                Season currentSeason = (Season)timeController.GetCurrentSeasonIndex();
                if (!cropData.IsValidSeason(currentSeason))
                {
                    ShowWorldFeedback($"{cropData.cropName} can't grow in {currentSeason}!");
                    return;
                }
            }
        }

        // Planta o cultivo
        if (cropGrowthManager.PlantCrop(cropData))
        {
            // Pass current soil fertility to the crop manager so it affects yield at harvest
            cropGrowthManager.Fertility = soilFertility;

            // If soil was watered, transfer that water to the crop
            if (currentState == SoilState.Watered)
            {
                cropGrowthManager.WaterCrop();
            }

            currentState = SoilState.WithCrop;
            UpdateAppearance();
            TutorialManager.NotifyStepComplete("plant_seed");

            // Remove semente do inventário
            playerInventory.Remove(seedItem, 1);

            PlayEffect(plantEffect);
            PlaySound(plantSound);
        }
    }

    private void HarvestCrop()
    {
        if (!HasCrop || !IsReadyForHarvest)
            return;

        StartCoroutine(HarvestWithAnimation());
    }

    private IEnumerator HarvestWithAnimation()
    {
        // IMPORTANT: Store crop data BEFORE calling HarvestCrop()
        // because HarvestCrop() might remove the crop (set currentCrop to null)
        CropData cropToHarvest = CurrentCrop;

        if (cropToHarvest == null)
            yield break;

        // Get yield from crop manager
        int yield = cropGrowthManager.HarvestCrop();

        // Harvesting depletes soil nutrients slightly; rests at 0.5 minimum
        soilFertility = Mathf.Max(0.5f, soilFertility - 0.1f);

        if (yield > 0)
        {
            // Use the stored crop data to get harvest item
            Item harvestItem = cropToHarvest.harvestItem;

            if (harvestItem == null)
                yield break;

            for (int i = 0; i < yield; i++)
            {
                SpawnGroundItem(harvestItem, i, yield);
            }

            // Visual and sound effects
            PlayEffect(harvestEffect);
            PlaySound(harvestSound);

            // Small pause for harvest animation
            yield return new WaitForSeconds(0.5f);
        }

        // Update soil state based on whether crop still exists (regrowth)
        if (!HasCrop)
        {
            currentState = SoilState.Tilled;
        }

        UpdateAppearance();
    }

    private void SpawnGroundItem(Item item, int index, int totalItems)
    {
        if (item == null)
            return;

        // Cria um GameObject para o GroundItem
        GameObject groundItemObj = new GameObject($"GroundItem_{item.itemName}");
        groundItemObj.transform.position = transform.position;

        // Adiciona componentes necessários
        SpriteRenderer sr = groundItemObj.AddComponent<SpriteRenderer>();
        sr.sprite = item.icon;
        sr.sortingOrder = 10; // Garante que fica visível sobre o solo

        // Adiciona collider
        CircleCollider2D collider = groundItemObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.3f;

        // Adiciona o componente GroundItem
        GroundItem groundItem = groundItemObj.AddComponent<GroundItem>();
        groundItem.SetItem(item);

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
        }
    }

    /// <summary>
    /// Called by the weather system on rainy days to automatically water this soil block.
    /// Tilled soil becomes Watered; soil WithCrop has its crop watered.
    /// Does nothing on Regular soil.
    /// </summary>
    public void AutoWater()
    {
        if (currentState == SoilState.Tilled)
        {
            currentState = SoilState.Watered;
            UpdateAppearance();
        }
        else if (currentState == SoilState.WithCrop && cropGrowthManager != null)
        {
            cropGrowthManager.WaterCrop();
            UpdateAppearance();
        }
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
                break;
            case SoilState.Watered:
                break;
            case SoilState.WithCrop:
                break;
        }
    }

    #endregion

    #region Eventos do Cultivo

    private void OnCropGrown(CropGrowthManager manager)
    {
        UpdateAppearance();
    }

    private void OnCropReadyForHarvest(CropGrowthManager manager)
    {
        UpdateAppearance();
    }

    private void OnCropDied(CropGrowthManager manager)
    {
        UpdateAppearance();
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
                return;

            if (IsReadyForHarvest)
            {
                if (!string.IsNullOrEmpty(scytheTag) && selectedItem != null && !HasTag(selectedItem, scytheTag))
                    return;
            }

            // Mostra informações do crescimento do cultivo
            if (cropGrowthManager != null)
            {
                if (CurrentCrop != null && CurrentCrop.requiresWater && !cropGrowthManager.IsWatered)
                {
                    return;
                }
            }
            return;
        }

        // Feedback para estados do solo
        switch (currentState)
        {
            case SoilState.Regular:
                break;
            case SoilState.Tilled:
                break;
            case SoilState.Watered:
                break;
        }

        // Feedback adicional para ferramentas inválidas
        if (selectedItem != null)
        {
            if (selectedItem.itemType == ItemType.Seed && currentState == SoilState.Regular)
            {
                // Feedback for trying to plant on untilled soil
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

    private void ShowWorldFeedback(string message)
    {
        // Floating text above the soil block visible in world space
        GameObject textObj = new GameObject("FeedbackText");
        textObj.transform.position = transform.position + Vector3.up * 0.8f;

        var canvas = textObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        textObj.transform.localScale = Vector3.one * 0.015f;

        var rt = textObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 40);

        var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = message;
        tmp.fontSize = 24;
        tmp.color = new Color(1f, 0.4f, 0.2f);
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontStyle = TMPro.FontStyles.Bold;

        Destroy(textObj, 2f);

        // Float upward
        StartCoroutine(FloatText(textObj.transform));
    }

    private System.Collections.IEnumerator FloatText(Transform t)
    {
        float elapsed = 0f;
        Vector3 start = t.position;
        while (elapsed < 1.5f && t != null)
        {
            elapsed += Time.deltaTime;
            if (t != null)
                t.position = start + Vector3.up * elapsed * 0.4f;
            yield return null;
        }
    }

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

    // ============================================================================
    // ISAVEABLE IMPLEMENTATION
    // ============================================================================

    public void SaveData(GameData gameData)
    {
        if (gameData == null) return;
        Vector2Int position = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
        string positionKey = $"{position.x},{position.y}";

        // Save soil state data
        // Remove existing entry for this position if it exists
        gameData.farmingData.soilStates.RemoveAll(entry => entry.positionKey == positionKey);
        
        // Create new soil state entry
        var soilStateEntry = new FarmingGameData.SoilStateEntry
        {
            positionKey = positionKey,
            soilData = new FarmingGameData.SoilData
            {
                isTilled = currentState != SoilState.Regular,
                isWatered = currentState == SoilState.Watered || (currentState == SoilState.WithCrop && cropGrowthManager != null && cropGrowthManager.IsWatered),
                fertility = soilFertility,
                soilType = "Normal"
            }
        };

        gameData.farmingData.soilStates.Add(soilStateEntry);
    }

    public void LoadData(GameData gameData)
    {
        if (gameData == null) return;
        Vector2Int position = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
        string positionKey = $"{position.x},{position.y}";

        // Load soil state data
        var soilStateEntry = gameData.farmingData.soilStates.FirstOrDefault(entry => entry.positionKey == positionKey);
        if (soilStateEntry != null && soilStateEntry.soilData != null)
        {
            var soilData = soilStateEntry.soilData;
            soilFertility = Mathf.Clamp(soilData.fertility, 0.5f, 2f);
            if (cropGrowthManager != null)
                cropGrowthManager.Fertility = soilFertility;
            if (soilData.isTilled)
            {
                if (soilData.isWatered)
                {
                    // Check if we have a crop at this position
                    bool hasCrop = gameData.farmingData.activeCrops.Any(crop => crop.position == position);
                    if (hasCrop)
                    {
                        currentState = SoilState.WithCrop;
                    }
                    else
                    {
                        currentState = SoilState.Watered;
                    }
                }
                else
                {
                    // Check if we have a crop at this position
                    bool hasCrop = gameData.farmingData.activeCrops.Any(crop => crop.position == position);
                    if (hasCrop)
                    {
                        currentState = SoilState.WithCrop;
                    }
                    else
                    {
                        currentState = SoilState.Tilled;
                    }
                }
            }
            else
            {
                currentState = SoilState.Regular;
            }

            UpdateAppearance();
        }
    }
}

} // namespace SowurShield.Core