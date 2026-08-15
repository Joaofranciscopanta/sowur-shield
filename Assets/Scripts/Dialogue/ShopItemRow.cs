using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using SowurShield.Core;
using SowurShield.Inventory;

namespace SowurShield.Dialogue
{

/// <summary>
/// Represents one buyable item in the shop list.
/// Wire itemIcon, itemNameText, priceText, stockText, buyButton in the prefab.
///
/// This lives in its own file on purpose. Unity only creates a MonoScript asset per *file*,
/// named after the file — a MonoBehaviour declared as a second class inside another script has
/// no MonoScript to reference, so adding it to a prefab saves as a missing-script component
/// with no error at build time. It was declared inside ShopUI.cs until 2026-08-15, which is one
/// of the reasons no shop row prefab could ever be authored. BuildingRow (the equivalent that
/// works) has always had its own file.
/// </summary>
public class ShopItemRow : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI stockText;
    [SerializeField] private Button buyButton;

    [SerializeField] private LocalizedString priceLabelText; // table "Dialogue", key "dialogue.shop.price"
    [SerializeField] private LocalizedString unlimitedStockText; // table "Dialogue", key "dialogue.shop.unlimited_stock"
    [SerializeField] private LocalizedString stockCountText; // table "Dialogue", key "dialogue.shop.stock_count"
    [SerializeField] private LocalizedString buyLabelText; // table "Dialogue", key "dialogue.shop.buy"

    /// <summary>
    /// The button's own label. Left as the literal "Buy" from the prefab until 2026-08-15, which
    /// read as English inside an otherwise fully translated Portuguese panel.
    /// </summary>
    [SerializeField] private TextMeshProUGUI buyButtonLabel;

    private ShopItemEntry _entry;

    public void Initialize(Item item, ShopItemEntry entry, int finalPrice,
                           System.Action<ShopItemEntry, Item, int> onBuy)
    {
        _entry = entry;

        if (itemIcon != null && item.icon != null)
            itemIcon.sprite = item.icon;

        if (itemNameText != null)
            itemNameText.text = item.GetDisplayName();

        if (priceText != null)
        {
            priceLabelText.Arguments = new object[] { finalPrice };
            priceText.text = priceLabelText.SafeGetLocalizedString();
        }

        if (buyButtonLabel != null)
        {
            string label = buyLabelText.SafeGetLocalizedString();
            // SafeGetLocalizedString returns empty until the tables preload (the WebGL deadlock
            // guard), so keep whatever the prefab carries rather than blanking the button.
            if (!string.IsNullOrEmpty(label))
                buyButtonLabel.text = label;
        }

        RefreshStock();

        if (buyButton != null)
            buyButton.onClick.AddListener(() => onBuy(entry, item, finalPrice));
    }

    public void RefreshStock()
    {
        if (stockText == null || _entry == null) return;
        if (_entry.IsUnlimited)
        {
            stockText.text = unlimitedStockText.SafeGetLocalizedString();
        }
        else
        {
            stockCountText.Arguments = new object[] { _entry.currentStock };
            stockText.text = stockCountText.SafeGetLocalizedString();
        }
    }
}

} // namespace SowurShield.Dialogue
