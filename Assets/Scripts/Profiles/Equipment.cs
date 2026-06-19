using System;
using System.Collections.Generic;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class Equipment : Bag
    {
        public Equipment()
        {
            maxSlots = 8;
            canHoldMoney = false;
            slots = new List<BagSlot>
            {
                new(false, new List<string> { "hat" }),
                new(false, new List<string> { "top" }),
                new(false, new List<string> { "bot" }),
                new(false, new List<string> { "shoes" }),
                new(false, new List<string> { "mainHand" }),
                new(false, new List<string> { "neck" }),
                new(false, new List<string> { "ringR" }),
                new(false, new List<string> { "ringL" })
            };
            RebuildLookup();
        }

        public BagSlot FindSlotFor(Item item)
        {
            foreach (var slot in slots)
            {
                if (slot.Allows(item))
                {
                    return slot;
                }
            }

            return null;
        }
    }
}
