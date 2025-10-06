using UnityEngine;
using System.Collections;
using System.Linq;

public class SoilBlockInteractable : MonoBehaviour, IInteractable, ISaveable
{
    public enum SoilState
    {
        Regular,
        Tilled,
        Watered,
        WithCrop
    }

    [Header("Soil")]
    public SoilState currentState = SoilState.Regular;
    public Sprite regularSprite;
    public Sprite tilledSprite;
    public Sprite wateredSprite;

    [Header("Tool Tags")]
    public string hoeTag = "Hoe";
    public string wateringCanTag = "WateringCan";
    public string shovelTag = "Shovel";
    public string scytheTag = "Scythe";

    [Header("Effects")]
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

    [Header("Visual Feedback")]
    public Color highlightColor = new Color(1f, 1f, 0.5f, 1f);
    public bool enableHighlightOnHover = true;
    private Color originalColor;
    private bool playerInRange = false;

    [Header("Debug")]

    private SpriteRenderer soilRenderer;
    private CropGrowthManager cropGrowthManager;
    private Inventory playerInventory;
    private Transform playerTransform;
    private Vector3Int gridPosition;

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

        if (regularSprite == null && soilRenderer != null)
            regularSprite = soilRenderer.sprite;
    }

    private void SetupGridPosition()
    {
        gridPosition = CursorController.GetWorldPosTile(transform.position);
    }

    private void EnsureCollider()
    {
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.9f, 0.9f);
            collider.isTrigger = true;
        }
    }

    private void SubscribeToCropEvents()
    {
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
        if (soilRenderer != null)
            originalColor = soilRenderer.color;
    }

    private void RegisterWithSaveManager()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
        }
        else
        {
            StartCoroutine(DelayedRegistration());
        }
    }

    private System.Collections.IEnumerator DelayedRegistration()
    {
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
        CursorController cursorController = FindFirstObjectByType<CursorController>();
        if (cursorController != null)
        {
            cursorController.UnregisterSoilBlock(gridPosition);
        }
    }

    #region Player Proximity Detection

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerTransform = other.transform;

            PlayerMove playerMove = other.GetComponent<PlayerMove>();
            if (playerMove != null)
            {
                playerInventory = playerMove.GetInventory();
            }

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

            if (enableHighlightOnHover && soilRenderer != null)
            {
                soilRenderer.color = originalColor;
            }
        }
    }

    #endregion

    public void Initialize(Inventory inventory)
    {
        playerInventory = inventory;
    }

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
        if (currentState == SoilState.WithCrop && IsReadyForHarvest)
        {
            if (string.IsNullOrEmpty(scytheTag) || HasTag(selectedItem, scytheTag))
            {
                HarvestCrop();
                return;
            }
        }

        if (HasTag(selectedItem, shovelTag) && currentState != SoilState.Regular)
        {
            ResetSoil();
            return;
        }

        if (currentState == SoilState.Regular && HasTag(selectedItem, hoeTag))
        {
            TillSoil();
            return;
        }

        if ((currentState == SoilState.Tilled || currentState == SoilState.WithCrop) &&
            HasTag(selectedItem, wateringCanTag))
        {
            WaterSoil();
            return;
        }

        if ((currentState == SoilState.Tilled || currentState == SoilState.Watered) &&
            selectedItem.itemType == ItemType.Seed)
        {
            PlantSeed(selectedItem);
            return;
        }

        ProvideFeedback(selectedItem);
    }

    private bool HasTag(Item item, string tag)
    {
        if (string.IsNullOrEmpty(tag) || item == null || item.itemTags == null)
            return false;

        return item.itemTags.Contains(tag);
    }

    #region Soil Actions

    private void TillSoil()
    {
        if (currentState != SoilState.Regular)
            return;

        currentState = SoilState.Tilled;
        UpdateAppearance();
        PlayEffect(tillEffect);
        PlaySound(tillSound);
    }

    private void WaterSoil()
    {
        if (currentState == SoilState.Tilled)
        {
            currentState = SoilState.Watered;
            UpdateAppearance();
            PlayEffect(waterEffect);
            PlaySound(waterSound);
        }
        else if (currentState == SoilState.WithCrop && cropGrowthManager != null)
        {
            cropGrowthManager.WaterCrop();
            UpdateAppearance();
            PlayEffect(waterEffect);
            PlaySound(waterSound);
        }
    }

    private void PlantSeed(Item seedItem)
    {
        if (playerInventory == null) return;

        CropData cropData = CropDatabase.GetCropDataForSeed(seedItem);

        if (cropData == null)
        {
            ProvideFeedback(seedItem);
            return;
        }

        GameTimeController timeController = GameTimeController.instance;
        if (timeController != null)
        {
            // Season validation can be implemented here if needed
        }

        if (cropGrowthManager.PlantCrop(cropData))
        {
            if (currentState == SoilState.Watered)
            {
                cropGrowthManager.WaterCrop();
            }

            currentState = SoilState.WithCrop;
            UpdateAppearance();

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
        CropData cropToHarvest = CurrentCrop;

        if (cropToHarvest == null)
            yield break;

        int yield = cropGrowthManager.HarvestCrop();

        if (yield > 0)
        {
            Item harvestItem = cropToHarvest.harvestItem;

            if (harvestItem == null)
                yield break;

            for (int i = 0; i < yield; i++)
            {
                SpawnGroundItem(harvestItem, i, yield);
            }

            PlayEffect(harvestEffect);
            PlaySound(harvestSound);

            yield return new WaitForSeconds(0.5f);
        }

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

        GameObject groundItemObj = new GameObject($"GroundItem_{item.itemName}");
        groundItemObj.transform.position = transform.position;

        SpriteRenderer sr = groundItemObj.AddComponent<SpriteRenderer>();
        sr.sprite = item.icon;
        sr.sortingOrder = 10;

        CircleCollider2D collider = groundItemObj.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.3f;

        GroundItem groundItem = groundItemObj.AddComponent<GroundItem>();
        groundItem.SetItem(item);

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
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(-0.3f, 0.3f),
                0
            );
            groundItemObj.transform.position += randomOffset;
        }
    }

    private void ResetSoil()
    {
        SoilState previousState = currentState;

        if (HasCrop)
        {
            cropGrowthManager.RemoveCrop();
        }

        currentState = SoilState.Regular;
        UpdateAppearance();
        PlayEffect(shovelEffect);
        PlaySound(shovelSound);

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

    #region Crop Events

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
        UpdateAppearance();
    }

    #endregion

    #region Feedback and Interface

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

            if (cropGrowthManager != null)
            {
                if (CurrentCrop != null && CurrentCrop.requiresWater && !cropGrowthManager.IsWatered)
                {
                    return;
                }
            }
            return;
        }

        switch (currentState)
        {
            case SoilState.Regular:
                break;
            case SoilState.Tilled:
                break;
            case SoilState.Watered:
                break;
        }

        if (selectedItem != null)
        {
            if (selectedItem.itemType == ItemType.Seed && currentState == SoilState.Regular)
            {
            }
        }
    }

    public void UpdateAppearance()
    {
        if (soilRenderer == null) return;

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
                bool showWatered = cropGrowthManager != null && cropGrowthManager.IsWatered;
                soilRenderer.sprite = (showWatered && wateredSprite != null) ? wateredSprite : tilledSprite;
                break;
        }

        soilRenderer.color = colorToUse;
    }

    #endregion

    #region Audio and Effects

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

    #region Public Getters

    public string GetStatusText()
    {
        if (HasCrop && cropGrowthManager != null)
        {
            return cropGrowthManager.GetCropInfo();
        }

        return currentState switch
        {
            SoilState.Regular => "Untilled soil",
            SoilState.Tilled => "Tilled soil - ready for planting",
            SoilState.Watered => "Watered soil - ready for planting",
            _ => "Unknown soil state"
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

    #region ISaveable Implementation

    public void SaveData(GameData gameData)
    {
        Vector2Int position = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
        string positionKey = $"{position.x},{position.y}";

        gameData.farmingData.soilStates.RemoveAll(entry => entry.positionKey == positionKey);

        var soilStateEntry = new FarmingGameData.SoilStateEntry
        {
            positionKey = positionKey,
            soilData = new FarmingGameData.SoilData
            {
                isTilled = currentState != SoilState.Regular,
                isWatered = currentState == SoilState.Watered || (currentState == SoilState.WithCrop && cropGrowthManager != null && cropGrowthManager.IsWatered),
                fertility = 1.0f,
                soilType = "Normal"
            }
        };

        gameData.farmingData.soilStates.Add(soilStateEntry);
    }

    public void LoadData(GameData gameData)
    {
        Vector2Int position = new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
        string positionKey = $"{position.x},{position.y}";

        var soilStateEntry = gameData.farmingData.soilStates.FirstOrDefault(entry => entry.positionKey == positionKey);
        if (soilStateEntry != null && soilStateEntry.soilData != null)
        {
            var soilData = soilStateEntry.soilData;
            if (soilData.isTilled)
            {
                if (soilData.isWatered)
                {
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

    #endregion
}