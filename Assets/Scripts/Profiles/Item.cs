using System;
using System.Collections.Generic;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class Item
    {
        public int itemID;
        public string name;
        public string description;
        public string iconPath;
        public List<string> tags = new();
        public int quantity;
        public int maxStack = 1;
        public Money unitPrice = new();
        public int modifier;

        public bool HasTag(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) && tags != null && tags.Contains(tag);
        }

        public bool IsEquipment => HasTag("equipment");

        public Item Clone(int quantityOverride = -1)
        {
            return new Item
            {
                itemID = itemID,
                name = name,
                description = description,
                iconPath = iconPath,
                tags = tags == null ? new List<string>() : new List<string>(tags),
                quantity = quantityOverride >= 0 ? quantityOverride : quantity,
                maxStack = maxStack,
                unitPrice = unitPrice,
                modifier = modifier
            };
        }
    }
}
