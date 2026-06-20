using System;

namespace IdleOff.Interactables
{
    [Serializable]
    public sealed class InteractableObjectDefinition
    {
        public int interactableID;
        public string name;
        public string description;
        public InteractableObjectType type = InteractableObjectType.Portal;
        public string closedSpritePath;
        public string openSpritePath;
        public InteractCondition condition = new();
        public InteractEffect effect = new();

        public InteractableObjectDefinition Clone()
        {
            return new InteractableObjectDefinition
            {
                interactableID = interactableID,
                name = name,
                description = description,
                type = type,
                closedSpritePath = closedSpritePath,
                openSpritePath = openSpritePath,
                condition = new InteractCondition
                {
                    type = condition?.type ?? InteractConditionType.None,
                    requiredAmount = condition?.requiredAmount ?? 0,
                    itemID = condition?.itemID ?? 0,
                    bossMobID = condition?.bossMobID ?? 0,
                    consumeItem = condition?.consumeItem ?? false
                },
                effect = new InteractEffect
                {
                    type = effect?.type ?? InteractEffectType.None,
                    targetMapID = effect?.targetMapID ?? 0
                }
            };
        }
    }
}
