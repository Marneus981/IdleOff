using System;
using UnityEngine;

namespace IdleOff.Maps
{
    [Serializable]
    public sealed class MapPlatformDefinition
    {
        public string anchorID;
        public Vector2 position;
        public Vector2 size = Vector2.one;
    }
}
