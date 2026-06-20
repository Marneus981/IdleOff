using System;
using IdleOff.Profiles;
using UnityEngine;

namespace IdleOff.Maps
{
    [Serializable]
    public sealed class MapPickupState
    {
        public int itemID;
        public int quantity = 1;
        public Money money;
        public bool isMoney;
        public string anchorID;
        public Vector2 position;
    }
}
