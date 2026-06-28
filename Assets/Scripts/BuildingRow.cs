using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;

namespace SowurShield.Core
{

/// <summary>
/// Attach to the building row prefab. Wire all fields in the Inspector.
/// BuildingShopUI calls Populate() and SetBuyAction() after instantiation.
///
/// PREFAB STRUCTURE (suggested):
///   BuildingRow (BuildingRow component)
///   ├── IconImage        (Image)
///   ├── NameText         (TextMeshProUGUI)
///   ├── EffectText       (TextMeshProUGUI)
///   ├── CostText         (TextMeshProUGUI)
///   ├── MaterialText     (TextMeshProUGUI)  ← "You have: X / Y"
///   ├── StatusText       (TextMeshProUGUI)  ← "✓ Built" or "Cannot afford"
///   └── BuyButton        (Button)
///
/// Kept in its own file (not alongside BuildingShopUI) so the Editor builder that creates
/// this prefab can reference it with the standard fileID 11500000 — a secondary class
/// sharing a .cs file with another MonoBehaviour gets a different, non-obvious fileID that
/// can't safely be hand-written into prefab YAML.
/// </summary>
public class BuildingRow : MonoBehaviour
{
    [Header("Wire these in the Prefab Inspector")]
    [SerializeField] public Image              iconImage;
    [SerializeField] public TextMeshProUGUI    nameText;
    [SerializeField] public TextMeshProUGUI    effectText;
    [SerializeField] public TextMeshProUGUI    costText;
    [SerializeField] public TextMeshProUGUI    materialText;   // "You have: X / Y"
    [SerializeField] public TextMeshProUGUI    statusText;     // built / can't afford
    [SerializeField] public Button             buyButton;

    [Header("Localized Strings")]
    [SerializeField] private LocalizedString costProgressText; // table "Farming", key "farming.building.cost_progress"
    [SerializeField] private LocalizedString builtText; // table "Farming", key "farming.building.built"
    [SerializeField] private LocalizedString cannotAffordText; // table "Farming", key "farming.building.cannot_afford"

    // Colour palette
    private static readonly Color COLOR_AFFORDABLE   = new Color(0.2f, 0.75f, 0.3f);
    private static readonly Color COLOR_UNAFFORDABLE = new Color(0.85f, 0.35f, 0.25f);
    private static readonly Color COLOR_BUILT        = new Color(0.5f, 0.5f, 0.5f);

    public void Populate(FarmBuildingData data, bool alreadyBuilt, bool canAfford, int playerMaterialCount)
    {
        if (nameText   != null) nameText.text   = data.buildingName;
        if (effectText != null) effectText.text = data.effectDescription;

        // Cost line
        if (costText != null)
        {
            costText.text = alreadyBuilt
                ? ""
                : $"{data.goldCost}g";
        }

        // Material count
        if (materialText != null)
        {
            if (!alreadyBuilt && !string.IsNullOrEmpty(data.materialItemName) && data.materialQuantity > 0)
            {
                materialText.gameObject.SetActive(true);
                costProgressText.Arguments = new object[] { data.materialItemName, playerMaterialCount, data.materialQuantity };
                materialText.text  = costProgressText.SafeGetLocalizedString();
                materialText.color = playerMaterialCount >= data.materialQuantity
                    ? COLOR_AFFORDABLE
                    : COLOR_UNAFFORDABLE;
            }
            else
            {
                materialText.gameObject.SetActive(false);
            }
        }

        // Status
        if (statusText != null)
        {
            if (alreadyBuilt)
            {
                statusText.gameObject.SetActive(true);
                statusText.text  = builtText.SafeGetLocalizedString();
                statusText.color = COLOR_BUILT;
            }
            else if (!canAfford)
            {
                statusText.gameObject.SetActive(true);
                statusText.text  = cannotAffordText.SafeGetLocalizedString();
                statusText.color = COLOR_UNAFFORDABLE;
            }
            else
            {
                statusText.gameObject.SetActive(false);
            }
        }

        // Icon
        if (iconImage != null)
        {
            iconImage.sprite  = data.icon;
            iconImage.enabled = data.icon != null;
        }

        // Buy button
        if (buyButton != null)
        {
            buyButton.interactable = !alreadyBuilt && canAfford;
            ColorBlock cb = buyButton.colors;
            cb.normalColor = alreadyBuilt ? COLOR_BUILT
                           : canAfford    ? COLOR_AFFORDABLE
                           :               COLOR_UNAFFORDABLE;
            buyButton.colors = cb;
        }
    }

    public void SetBuyAction(System.Action onBuy)
    {
        if (buyButton == null) return;
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => onBuy?.Invoke());
    }

    /// <summary>
    /// Called when the BuildingRow component is added at runtime (no designer-wired prefab).
    /// Searches child components by index as a best-effort fallback.
    /// Designers should wire the prefab instead.
    /// </summary>
    public void AutoWire()
    {
        var texts  = GetComponentsInChildren<TextMeshProUGUI>();
        var images = GetComponentsInChildren<Image>();

        if (texts.Length > 0) nameText   = texts[0];
        if (texts.Length > 1) effectText = texts[1];
        if (texts.Length > 2) costText   = texts[2];
        if (texts.Length > 3) statusText = texts[3];
        if (images.Length > 1) iconImage = images[1]; // [0] is usually the row background

        buyButton = GetComponentInChildren<Button>();
    }
}

} // namespace SowurShield.Core
