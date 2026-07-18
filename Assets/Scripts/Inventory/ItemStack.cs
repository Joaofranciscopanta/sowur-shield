using UnityEngine;
using System;

namespace SowurShield.Inventory
{
    [System.Serializable]
    public class ItemStack
    {
        public Item item;
        public int quantity;

        public ItemStack()
        {
            item = null;
            quantity = 0;
        }

        public ItemStack(Item item, int quantity = 1)
        {
            this.item = item;
            this.quantity = Mathf.Max(0, quantity);
        }

        public bool IsEmpty => item == null || quantity <= 0;

        public int MaxStackSize => item != null ? item.maxStackSize : 0;

        public int AvailableSpace => item != null ? Mathf.Max(0, item.maxStackSize - quantity) : 0;

        public bool CanStack(Item otherItem)
        {
            if (IsEmpty || otherItem == null) return false;
            if (!item.isStackable) return false;
            return item == otherItem && quantity < item.maxStackSize;
        }

        public int AddQuantity(int amount)
        {
            if (IsEmpty || item == null || amount <= 0) return amount;

            int canAdd = Mathf.Min(amount, AvailableSpace);
            quantity += canAdd;
            return amount - canAdd; // Return leftover
        }

        public int RemoveQuantity(int amount)
        {
            if (IsEmpty || amount <= 0) return 0;

            int toRemove = Mathf.Min(amount, quantity);
            quantity -= toRemove;

            if (quantity <= 0)
            {
                Clear();
            }

            return toRemove;
        }

        public void Clear()
        {
            item = null;
            quantity = 0;
        }

        public ItemStack Clone()
        {
            return new ItemStack(item, quantity);
        }

        public ItemStack Split(int splitAmount = -1)
        {
            if (IsEmpty || quantity <= 1) return new ItemStack();

            if (splitAmount <= 0)
                splitAmount = quantity / 2;

            splitAmount = Mathf.Min(splitAmount, quantity - 1);

            if (splitAmount <= 0) return new ItemStack();

            ItemStack split = new ItemStack(item, splitAmount);
            quantity -= splitAmount;
            return split;
        }

        public bool TryMerge(ItemStack other)
        {
            if (other == null || other.IsEmpty) return false;
            if (!CanStack(other.item)) return false;

            int leftover = AddQuantity(other.quantity);
            other.quantity = leftover;

            if (leftover <= 0)
            {
                other.Clear();
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            if (IsEmpty) return "Empty";
            return $"{item.itemName} x{quantity}";
        }

        public override bool Equals(object obj)
        {
            if (obj is ItemStack other)
            {
                return item == other.item && quantity == other.quantity;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(item, quantity);
        }

        public static implicit operator bool(ItemStack stack)
        {
            return !stack.IsEmpty;
        }
    }
} // namespace SowurShield.Inventory
