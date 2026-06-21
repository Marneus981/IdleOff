using System;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class Inventory : Bag
    {
        public Inventory()
            : base(20, true)
        {
        }
    }
}
