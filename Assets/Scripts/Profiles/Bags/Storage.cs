using System;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class Storage : Bag
    {
        public Storage()
            : base(50, true)
        {
        }
    }
}
