using System.Collections.Generic;
using IdleOff.Combat;
using IdleOff.Game;
using UnityEngine;

namespace IdleOff.Actions
{
    internal static class ActionHitResolver
    {
        public static bool TryResolveHit(ActionUseRequest request, Collider2D collider, HashSet<ICombatant> hitTargets)
        {
            return TryResolveHit(request, collider, hitTargets, ResolveImpactPoint(collider));
        }

        public static bool TryResolveHit(ActionUseRequest request, Collider2D collider, HashSet<ICombatant> hitTargets, Vector2 impactPoint)
        {
            var target = FindCombatant(collider);
            if (target == null || ReferenceEquals(target, request.Owner) || hitTargets.Contains(target))
            {
                return false;
            }

            hitTargets.Add(target);
            if (request.Owner.IsPlayerControlled && target is IMobCombatant mob)
            {
                var result = CombatResolver.ResolvePlayerAction(request.Owner, mob, request.Action);
                FloatingFeedbackService.ShowCombatResult(result, impactPoint);
                return true;
            }

            if (request.Owner is IMobCombatant attacker)
            {
                var result = CombatResolver.ResolveMobAction(attacker, target, request.Action);
                FloatingFeedbackService.ShowCombatResult(result, impactPoint);
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

        private static Vector2 ResolveImpactPoint(Collider2D collider)
        {
            return collider != null ? collider.bounds.center : Vector2.zero;
        }
    }
}
