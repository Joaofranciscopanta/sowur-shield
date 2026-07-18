using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using SowurShield.Core;

namespace SowurShield.Animals
{

/// <summary>
/// Attach to the buy-row prefab. Wire all fields in the Inspector.
/// Kept in its own file (not alongside AnimalMarketUI) so the Editor builder that creates
/// this prefab can reference it with the standard fileID 11500000.
/// </summary>
public class AnimalMarketBuyRow : MonoBehaviour
{
    [Header("Wire these in the Prefab Inspector")]
    [SerializeField] public Image iconImage;
    [SerializeField] public TextMeshProUGUI nameText;
    [SerializeField] public TextMeshProUGUI priceText;
    [SerializeField] public TextMeshProUGUI stockText;
    [SerializeField] public Button buyButton;

    [Header("Localization")]
    [SerializeField] private LocalizedString buyPriceText; // table "Animals", key "animals.market.buy_price"

    public void Initialize(AnimalMarketEntry entry, int finalPrice, System.Action<AnimalMarketEntry, int> onBuy)
    {
        if (iconImage != null && entry.animalData.idleSprite != null)
        {
            iconImage.sprite = entry.animalData.idleSprite;
            iconImage.enabled = true;
        }

        if (nameText != null) nameText.text = entry.animalData.GetDisplayName();
        if (priceText != null)
        {
            buyPriceText.Arguments = new object[] { finalPrice };
            priceText.text = buyPriceText.SafeGetLocalizedString();
        }
        if (stockText != null) stockText.text = entry.IsUnlimited ? "" : $"x{entry.maxStock - entry.purchasedCount}";

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onBuy(entry, finalPrice));
        }
    }
}

} // namespace SowurShield.Animals
