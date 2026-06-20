using IdleOff.Actions;
using IdleOff.Combat;
using UnityEngine;

namespace IdleOff.Mobs
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MobEntity), typeof(ActionController))]
    public sealed class MobActionController : MonoBehaviour
    {
        private MobEntity mob;
        private ActionController actionController;

        private void Awake()
        {
            mob = GetComponent<MobEntity>();
            actionController = GetComponent<ActionController>();
        }

        public bool IsTargetInRange(Transform target)
        {
            if (target == null || mob == null)
            {
                return false;
            }

            var range = Mathf.Max(mob.Template.attackRange, mob.GetBasicAction().range);
            return Vector2.Distance(transform.position, target.position) <= range;
        }

        public bool TryAttack(Transform target)
        {
            if (target == null || mob == null || !mob.IsAlive)
            {
                return false;
            }

            var player = target.GetComponentInParent<PlayerCombatant>();
            if (player == null || !player.IsAlive || !IsTargetInRange(target))
            {
                return false;
            }

            var direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
            return actionController.TryUseAction(mob.GetBasicAction(), direction);
        }
    }
}
