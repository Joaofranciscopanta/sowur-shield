using NUnit.Framework;
using UnityEngine;
using SowurShield.Inventory;

/// <summary>
/// Edit Mode tests for ItemStack — a pure C# class with no MonoBehaviour dependencies.
/// Creates real Item ScriptableObjects via ScriptableObject.CreateInstance.
/// </summary>
public class ItemStackTests
{
    private Item stackableItem;
    private Item nonStackableItem;

    [SetUp]
    public void SetUp()
    {
        stackableItem = ScriptableObject.CreateInstance<Item>();
        stackableItem.itemName = "Carrot";
        stackableItem.isStackable = true;
        stackableItem.maxStackSize = 10;
        stackableItem.baseValue = 5;

        nonStackableItem = ScriptableObject.CreateInstance<Item>();
        nonStackableItem.itemName = "Hoe";
        nonStackableItem.isStackable = false;
        nonStackableItem.maxStackSize = 1;
        nonStackableItem.baseValue = 50;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(stackableItem);
        Object.DestroyImmediate(nonStackableItem);
    }

    // =========================================================================
    // IS EMPTY
    // =========================================================================

    [Test]
    public void DefaultConstructor_IsEmpty()
    {
        var stack = new ItemStack();
        Assert.IsTrue(stack.IsEmpty);
    }

    [Test]
    public void ItemConstructor_IsNotEmpty_WhenQuantityPositive()
    {
        var stack = new ItemStack(stackableItem, 3);
        Assert.IsFalse(stack.IsEmpty);
    }

    [Test]
    public void IsEmpty_TrueWhenQuantityIsZero()
    {
        var stack = new ItemStack(stackableItem, 0);
        Assert.IsTrue(stack.IsEmpty);
    }

    [Test]
    public void IsEmpty_TrueWhenItemIsNull()
    {
        var stack = new ItemStack(null, 5);
        Assert.IsTrue(stack.IsEmpty);
    }

    // =========================================================================
    // AVAILABLE SPACE
    // =========================================================================

    [Test]
    public void AvailableSpace_CorrectForPartialStack()
    {
        var stack = new ItemStack(stackableItem, 3); // max 10
        Assert.AreEqual(7, stack.AvailableSpace);
    }

    [Test]
    public void AvailableSpace_ZeroForFullStack()
    {
        var stack = new ItemStack(stackableItem, 10);
        Assert.AreEqual(0, stack.AvailableSpace);
    }

    [Test]
    public void AvailableSpace_ZeroForEmptyStack()
    {
        var stack = new ItemStack();
        Assert.AreEqual(0, stack.AvailableSpace);
    }

    // =========================================================================
    // CAN STACK
    // =========================================================================

    [Test]
    public void CanStack_TrueForSameStackableItem_WithSpace()
    {
        var stack = new ItemStack(stackableItem, 3);
        Assert.IsTrue(stack.CanStack(stackableItem));
    }

    [Test]
    public void CanStack_FalseForFullStack()
    {
        var stack = new ItemStack(stackableItem, 10); // max 10
        Assert.IsFalse(stack.CanStack(stackableItem));
    }

    [Test]
    public void CanStack_FalseForNonStackableItem()
    {
        var stack = new ItemStack(nonStackableItem, 0);
        // Even with space, non-stackable items can't stack
        stack.item = nonStackableItem;
        stack.quantity = 0;
        Assert.IsFalse(stack.CanStack(nonStackableItem));
    }

    [Test]
    public void CanStack_FalseForEmptyStack()
    {
        var stack = new ItemStack();
        Assert.IsFalse(stack.CanStack(stackableItem));
    }

    [Test]
    public void CanStack_FalseForDifferentItem()
    {
        var stack = new ItemStack(stackableItem, 3);
        Assert.IsFalse(stack.CanStack(nonStackableItem));
    }

    // =========================================================================
    // ADD QUANTITY
    // =========================================================================

    [Test]
    public void AddQuantity_IncreasesQuantityByAmount()
    {
        var stack = new ItemStack(stackableItem, 3);
        stack.AddQuantity(4);
        Assert.AreEqual(7, stack.quantity);
    }

    [Test]
    public void AddQuantity_ReturnsZeroLeftoverWhenSpaceAvailable()
    {
        var stack = new ItemStack(stackableItem, 3);
        int leftover = stack.AddQuantity(4);
        Assert.AreEqual(0, leftover);
    }

    [Test]
    public void AddQuantity_ClampsAtMaxStackSize()
    {
        var stack = new ItemStack(stackableItem, 8); // 2 spaces left
        stack.AddQuantity(5);
        Assert.AreEqual(10, stack.quantity);
    }

    [Test]
    public void AddQuantity_ReturnsCorrectLeftoverWhenOverflow()
    {
        var stack = new ItemStack(stackableItem, 8); // 2 spaces
        int leftover = stack.AddQuantity(5);
        Assert.AreEqual(3, leftover);
    }

    [Test]
    public void AddQuantity_ReturnsFullAmountForEmptyStack()
    {
        var stack = new ItemStack();
        int leftover = stack.AddQuantity(5);
        Assert.AreEqual(5, leftover); // can't add to empty stack
    }

    // =========================================================================
    // REMOVE QUANTITY
    // =========================================================================

    [Test]
    public void RemoveQuantity_DecreasesQuantityByAmount()
    {
        var stack = new ItemStack(stackableItem, 8);
        stack.RemoveQuantity(3);
        Assert.AreEqual(5, stack.quantity);
    }

    [Test]
    public void RemoveQuantity_ReturnsAmountActuallyRemoved()
    {
        var stack = new ItemStack(stackableItem, 4);
        int removed = stack.RemoveQuantity(3);
        Assert.AreEqual(3, removed);
    }

    [Test]
    public void RemoveQuantity_ClearsStack_WhenRemovingAll()
    {
        var stack = new ItemStack(stackableItem, 5);
        stack.RemoveQuantity(5);
        Assert.IsTrue(stack.IsEmpty);
    }

    [Test]
    public void RemoveQuantity_RemovesOnlyAvailable_WhenRequestTooLarge()
    {
        var stack = new ItemStack(stackableItem, 3);
        int removed = stack.RemoveQuantity(10);
        Assert.AreEqual(3, removed);
        Assert.IsTrue(stack.IsEmpty);
    }

    // =========================================================================
    // CLEAR
    // =========================================================================

    [Test]
    public void Clear_MakesStackEmpty()
    {
        var stack = new ItemStack(stackableItem, 5);
        stack.Clear();
        Assert.IsTrue(stack.IsEmpty);
        Assert.IsNull(stack.item);
        Assert.AreEqual(0, stack.quantity);
    }

    // =========================================================================
    // CLONE
    // =========================================================================

    [Test]
    public void Clone_CreatesSeparateCopy()
    {
        var original = new ItemStack(stackableItem, 5);
        var clone = original.Clone();

        Assert.AreEqual(original.item, clone.item);
        Assert.AreEqual(original.quantity, clone.quantity);

        // Mutating clone should not affect original
        clone.quantity = 99;
        Assert.AreEqual(5, original.quantity);
    }

    // =========================================================================
    // SPLIT
    // =========================================================================

    [Test]
    public void Split_HalvesTheStack_WhenNoAmountGiven()
    {
        var stack = new ItemStack(stackableItem, 8);
        var split = stack.Split();

        Assert.AreEqual(4, split.quantity);
        Assert.AreEqual(4, stack.quantity);
    }

    [Test]
    public void Split_RemovesSpecifiedAmount()
    {
        var stack = new ItemStack(stackableItem, 8);
        var split = stack.Split(3);

        Assert.AreEqual(3, split.quantity);
        Assert.AreEqual(5, stack.quantity);
    }

    [Test]
    public void Split_ReturnsEmptyStack_WhenQuantityIsOne()
    {
        var stack = new ItemStack(stackableItem, 1);
        var split = stack.Split();
        Assert.IsTrue(split.IsEmpty);
    }

    // =========================================================================
    // TRY MERGE
    // =========================================================================

    [Test]
    public void TryMerge_CombinesStacks_WhenEnoughSpace()
    {
        var target = new ItemStack(stackableItem, 3);
        var source = new ItemStack(stackableItem, 4);

        bool merged = target.TryMerge(source);

        Assert.IsTrue(merged);
        Assert.AreEqual(7, target.quantity);
        Assert.IsTrue(source.IsEmpty);
    }

    [Test]
    public void TryMerge_ReturnsFalse_WhenDifferentItems()
    {
        var target = new ItemStack(stackableItem, 3);
        var source = new ItemStack(nonStackableItem, 1);

        bool merged = target.TryMerge(source);

        Assert.IsFalse(merged);
        Assert.AreEqual(3, target.quantity); // unchanged
    }

    [Test]
    public void TryMerge_HandlesOverflow_LeavingRemainder()
    {
        var target = new ItemStack(stackableItem, 8); // 2 spaces
        var source = new ItemStack(stackableItem, 5);

        target.TryMerge(source);

        Assert.AreEqual(10, target.quantity); // full
        Assert.AreEqual(3, source.quantity);  // leftover
    }

    // =========================================================================
    // IMPLICIT BOOL OPERATOR
    // =========================================================================

    [Test]
    public void ImplicitBool_ReturnsFalse_ForEmptyStack()
    {
        var stack = new ItemStack();
        Assert.IsFalse((bool)stack);
    }

    [Test]
    public void ImplicitBool_ReturnsTrue_ForNonEmptyStack()
    {
        var stack = new ItemStack(stackableItem, 2);
        Assert.IsTrue((bool)stack);
    }

    // =========================================================================
    // TO STRING
    // =========================================================================

    [Test]
    public void ToString_ReturnsEmpty_ForEmptyStack()
    {
        var stack = new ItemStack();
        Assert.AreEqual("Empty", stack.ToString());
    }

    [Test]
    public void ToString_ReturnsNameAndQuantity()
    {
        var stack = new ItemStack(stackableItem, 5);
        Assert.AreEqual("Carrot x5", stack.ToString());
    }
}
