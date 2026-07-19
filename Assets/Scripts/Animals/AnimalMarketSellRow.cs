using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using SowurShield.Core;

namespace SowurShield.Animals
{

/// <summary>
/// Attach to the sell-row prefab. Wire all fields in the Inspector.
/// Kept in its own file for the same fileID reason as AnimalMarketBuyRow.
/// </summary>
public class AnimalMarketSellRow : MonoBehaviour
{
    [Header("Wire these in the Prefab Inspector")]
    [SerializeField] public Image iconImage;
    [SerializeField] public TextMeshProUGUI nameText;
    [SerializeField] public TextMeshProUGUI priceText;
    [SerializeField] public TextMeshProUGUI zoneText;
    [SerializeField] public Button sellButton;

    [Header("Localization")]
    [SerializeField] private LocalizedString sellPriceText; // table "Animals", key "animals.market.sell_price"
    [SerializeField] private LocalizedString unassignedText; // table "Animals", key "animals.market.unassigned"

    public void Initialize(Animal animal, int sellPrice, System.Action<Animal, int> onSell)
    {
        if (iconImage != null && animal.AnimalData != null && animal.AnimalData.idleSprite != null)
        {
            iconImage.sprite = animal.AnimalData.idleSprite;
            iconImage.enabled = true;
        }

        if (nameText != null) nameText.text = animal.GetDisplayName();
        if (priceText != null)
        {
            sellPriceText.Arguments = new object[] { sellPrice };
            priceText.text = sellPriceText.SafeGetLocalizedString();
        }
        if (zoneText != null) zoneText.text = animal.AssignedZone != null ? animal.AssignedZone.gameObject.name : unassignedText.SafeGetLocalizedString();

        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(() => onSell(animal, sellPrice));
        }
    }
}

} // namespace SowurShield.Animals
