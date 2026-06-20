using System;

namespace IdleOff.Interactables
{
    [Serializable]
    public sealed class InteractEffect
    {
        public InteractEffectType type = InteractEffectType.None;
        public int targetMapID;
    }
}
