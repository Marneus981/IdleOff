using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Profiles
{
    [Serializable]
    public class Bag
    {
        [SerializeField] protected int maxSlots = 20;
        [SerializeField] protected bool canHoldMoney;
        [SerializeField] protected List<BagSlot> slots = new();
        [SerializeField] protected Money money = new();
        protected Dictionary<int, BagSlot> slotsByItemID = new();

        public IReadOnlyList<BagSlot> Slots => slots;
        public Money Money => money;
        public int MaxSlots => maxSlots;
        public bool CanHoldMoney => canHoldMoney;

        public Bag()
            : this(20, false)
        {
        }

        protected Bag(int maxSlots, bool canHoldMoney)
        {
            this.maxSlots = Mathf.Max(0, maxSlots);
            this.canHoldMoney = canHoldMoney;
            InitializeGeneralSlots(this.maxSlots);
        }

        protected void InitializeGeneralSlots(int count)
        {
            slots = new List<BagSlot>();
            for (var i = 0; i < count; i++)
            {
                slots.Add(new BagSlot(true));
            }

            RebuildLookup();
        }

        public virtual bool AddItem(int itemID, int quantity)
        {
            if (quantity <= 0)
            {
                return false;
            }

            GlobalItemCatalog.EnsureLoaded();
            if (!GlobalItemCatalog.Items.TryGetValue(itemID, out var template))
            {
                throw new KeyNotFoundException($"Item ID {itemID} was not found.");
            }

            return AddItem(template, quantity);
        }

        public virtual bool AddItem(Item template, int quantity)
        {
            if (template == null || quantity <= 0)
            {
                return false;
            }

            var remaining = quantity;
            foreach (var slot in slots)
            {
                if (slot.IsEmpty || slot.item.itemID != template.itemID || slot.item.quantity >= slot.item.maxStack)
                {
                    continue;
                }

                var addAmount = Mathf.Min(remaining, slot.item.maxStack - slot.item.quantity);
                slot.item.quantity += addAmount;
                remaining -= addAmount;
                if (remaining == 0)
                {
                    RebuildLookup();
                    return true;
                }
            }

            foreach (var slot in slots)
            {
                if (!slot.IsEmpty || !slot.Allows(template))
                {
                    continue;
                }

                var stackAmount = Mathf.Min(remaining, Mathf.Max(1, template.maxStack));
                slot.item = template.Clone(stackAmount);
                remaining -= stackAmount;
                if (remaining == 0)
                {
                    RebuildLookup();
                    return true;
                }
            }

            RebuildLookup();
            return false;
        }

        public bool TryRemoveItem(int itemID, int quantity, out Item removedItem)
        {
            removedItem = null;
            if (quantity <= 0)
            {
                return false;
            }

            var available = GetItemQuantity(itemID);
            if (available < quantity)
            {
                return false;
            }

            var remaining = quantity;
            foreach (var slot in slots)
            {
                if (slot.IsEmpty || slot.item.itemID != itemID)
                {
                    continue;
                }

                removedItem ??= slot.item.Clone(0);
                var removeAmount = Mathf.Min(remaining, slot.item.quantity);
                removedItem.quantity += removeAmount;
                slot.item.quantity -= removeAmount;
                remaining -= removeAmount;
                if (slot.item.quantity <= 0)
                {
                    slot.item = null;
                }

                if (remaining == 0)
                {
                    RebuildLookup();
                    return true;
                }
            }

            RebuildLookup();
            return false;
        }

        public int GetItemQuantity(int itemID)
        {
            var quantity = 0;
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.item.itemID == itemID)
                {
                    quantity += slot.item.quantity;
                }
            }

            return quantity;
        }

        public bool AddMoney(Money value)
        {
            if (!canHoldMoney)
            {
                return false;
            }

            money.Add(value);
            return true;
        }

        public bool TryRemoveMoney(Money value)
        {
            return canHoldMoney && money.TrySubtract(value);
        }

        protected void RebuildLookup()
        {
            slotsByItemID = new Dictionary<int, BagSlot>();
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && !slotsByItemID.ContainsKey(slot.item.itemID))
                {
                    slotsByItemID.Add(slot.item.itemID, slot);
                }
            }
        }
    }
}
