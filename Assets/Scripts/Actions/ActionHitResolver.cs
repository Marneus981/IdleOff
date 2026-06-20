using System.Collections.Generic;
using IdleOff.Combat;
using UnityEngine;

namespace IdleOff.Actions
{
    internal static class ActionHitResolver
    {
        public static bool TryResolveHit(ActionUseRequest request, Collider2D collider, HashSet<ICombatant> hitTargets)
        {
            var target = FindCombatant(collider);
            if (target == null || ReferenceEquals(target, request.Owner) || hitTargets.Contains(target))
            {
                return false;
            }

            hitTargets.Add(target);
            if (request.Owner.IsPlayerControlled && target is IMobCombatant mob)
            {
                CombatResolver.ResolvePlayerAction(request.Owner, mob, request.Action);
                return true;
            }

            if (request.Owner is IMobCombatant attacker)
            {
                CombatResolver.ResolveMobAction(attacker, target, request.Action);
                return true;
            }

            return false;
        }

        public static ICombatant FindCombatant(Collider2D collider)
        {
            var behaviours = collider.GetComponentsInParent<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour is ICombatant combatant)
                {
                    return combatant;
                }
            }

            return null;
        }
    }
}
