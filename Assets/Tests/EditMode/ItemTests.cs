using NUnit.Framework;
using UnityEngine;
using SowurShield.Inventory;
using SowurShield.Core;

/// <summary>
/// Edit Mode tests for Item ScriptableObject and related helpers.
/// </summary>
public class ItemTests
{
    private Item item;

    [SetUp]
    public void SetUp()
    {
        item = ScriptableObject.CreateInstance<Item>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(item);
    }

    // =========================================================================
    // DEFAULT VALUES
    // =========================================================================

    [Test]
    public void Item_DefaultName_IsNewItem()
    {
        Assert.AreEqual("New Item", item.itemName);
    }

    [Test]
    public void Item_DefaultIsStackable_IsTrue()
    {
        Assert.IsTrue(item.isStackable);
    }

    [Test]
    public void Item_DefaultMaxStackSize_Is99()
    {
        Assert.AreEqual(99, item.maxStackSize);
    }

    [Test]
    public void Item_DefaultBaseValue_IsOne()
    {
        Assert.AreEqual(1, item.baseValue);
    }

    [Test]
    public void Item_DefaultCanBeSold_IsTrue()
    {
        Assert.IsTrue(item.canBeSold);
    }

    [Test]
    public void Item_DefaultDurability_IsMinusOne()
    {
        Assert.AreEqual(-1, item.durability); // -1 = infinite
    }

    // =========================================================================
    // RARITY COLORS
    // =========================================================================

    [Test]
    public void GetRarityColor_Common_ReturnsWhite()
    {
        item.rarity = ItemRarity.Common;
        Assert.AreEqual(Color.white, item.GetRarityColor());
    }

    [Test]
    public void GetRarityColor_Uncommon_ReturnsGreen()
    {
        item.rarity = ItemRarity.Uncommon;
        Assert.AreEqual(Color.green, item.GetRarityColor());
    }

    [Test]
    public void GetRarityColor_Rare_ReturnsBlue()
    {
        item.rarity = ItemRarity.Rare;
        Assert.AreEqual(Color.blue, item.GetRarityColor());
    }

    [Test]
    public void GetRarityColor_Epic_ReturnsPurple()
    {
        item.rarity = ItemRarity.Epic;
        var expected = new Color(0.6f, 0f, 1f);
        Assert.AreEqual(expected, item.GetRarityColor());
    }

    [Test]
    public void GetRarityColor_Legendary_ReturnsOrange()
    {
        item.rarity = ItemRarity.Legendary;
        var expected = new Color(1f, 0.5f, 0f);
        Assert.AreEqual(expected, item.GetRarityColor());
    }

    // =========================================================================
    // ITEM BOX SPRITE MATCHING
    // =========================================================================

    [Test]
    public void ItemBoxSprite_MatchesItem_True_ForExactItemMatch()
    {
        var mapping = new ItemBoxSprite { item = item };
        Assert.IsTrue(mapping.MatchesItem(item));
    }

    [Test]
    public void ItemBoxSprite_MatchesItem_False_ForNull()
    {
        var mapping = new ItemBoxSprite { item = item };
        Assert.IsFalse(mapping.MatchesItem(null));
    }

    [Test]
    public void ItemBoxSprite_MatchesItem_True_ForTagMatch()
    {
        item.itemTags.Add("Vegetable");
        var mapping = new ItemBoxSprite { item = null, itemTag = "Vegetable" };
        Assert.IsTrue(mapping.MatchesItem(item));
    }

    [Test]
    public void ItemBoxSprite_MatchesItem_False_WhenTagDoesNotMatch()
    {
        item.itemTags.Add("Vegetable");
        var mapping = new ItemBoxSprite { item = null, itemTag = "Fruit" };
        Assert.IsFalse(mapping.MatchesItem(item));
    }

    [Test]
    public void ItemBoxSprite_MatchesItem_True_ForItemTypeMatch()
    {
        item.itemType = ItemType.Food;
        var mapping = new ItemBoxSprite { item = null, itemTag = "", itemType = ItemType.Food };
        Assert.IsTrue(mapping.MatchesItem(item));
    }

    [Test]
    public void ItemBoxSprite_DirectItemMatch_TakesPriorityOverTag()
    {
        var otherItem = ScriptableObject.CreateInstance<Item>();
        otherItem.itemTags.Add("Vegetable");

        // Mapping matches by direct item reference, not by tag
        var mapping = new ItemBoxSprite { item = item, itemTag = "Vegetable" };

        Assert.IsTrue(mapping.MatchesItem(item));
        Assert.IsFalse(mapping.MatchesItem(otherItem)); // different Item, even though tag matches

        Object.DestroyImmediate(otherItem);
    }
}
