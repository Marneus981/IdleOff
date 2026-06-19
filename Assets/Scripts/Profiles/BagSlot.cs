using System;
using System.Collections.Generic;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class BagSlot
    {
        public bool general = true;
        public List<string> tags = new();
        public Item item;

        public bool IsEmpty => item == null || item.quantity <= 0;

        public BagSlot()
        {
        }

        public BagSlot(bool general, List<string> tags = null)
        {
            this.general = general;
            this.tags = tags ?? new List<string>();
        }

        public bool Allows(Item candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            if (general)
            {
                return true;
            }

            if (tags == null || candidate.tags == null)
            {
                return false;
            }

            foreach (var tag in tags)
            {
                if (candidate.tags.Contains(tag))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
