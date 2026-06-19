using System;
using IdleOff.Profiles;

namespace IdleOff.Drops
{
    [Serializable]
    public sealed class WorldDropPayload
    {
        public int itemID;
        public int quantity = 1;
        public Money money;
        public bool isMoney;

        public static WorldDropPayload Item(int itemID, int quantity = 1)
        {
            return new WorldDropPayload
            {
                itemID = itemID,
                quantity = Math.Max(1, quantity),
                isMoney = false
            };
        }

        public static WorldDropPayload Money(Money money)
        {
            return new WorldDropPayload
            {
                money = money,
                isMoney = true
            };
        }

        public bool IsEmpty => isMoney ? money.TotalCopper <= 0 : itemID <= 0 || quantity <= 0;
    }
}
