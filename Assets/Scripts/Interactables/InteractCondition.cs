using System;

namespace IdleOff.Interactables
{
    [Serializable]
    public sealed class InteractCondition
    {
        public InteractConditionType type = InteractConditionType.None;
        public int requiredAmount;
        public int itemID;
        public int bossMobID;
        public bool consumeItem;
    }
}
