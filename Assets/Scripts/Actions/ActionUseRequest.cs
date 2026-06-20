using IdleOff.Combat;
using UnityEngine;

namespace IdleOff.Actions
{
    public readonly struct ActionUseRequest
    {
        public ActionUseRequest(ICombatant owner, Action action, Vector2 direction, LayerMask targetLayerMask)
            : this(owner, action, Vector2.zero, direction, targetLayerMask)
        {
        }

        public ActionUseRequest(ICombatant owner, Action action, Vector2 origin, Vector2 direction, LayerMask targetLayerMask)
        {
            Owner = owner;
            Action = action;
            Origin = origin;
            Direction = direction == Vector2.zero ? Vector2.right : direction.normalized;
            TargetLayerMask = targetLayerMask;
        }

        public ICombatant Owner { get; }
        public Action Action { get; }
        public Vector2 Origin { get; }
        public Vector2 Direction { get; }
        public LayerMask TargetLayerMask { get; }
    }
}
