using IdleOff.Combat;
using UnityEngine;

namespace IdleOff.Mobs
{
    [RequireComponent(typeof(MobEntity))]
    public sealed class MobBossAI : MonoBehaviour
    {
        [SerializeField] private LayerMask playerLayerMask = ~0;

        private MobEntity mob;
        private MobActionController actionController;
        private Transform target;

        public Transform Target => target;
        public bool HasTarget => target != null;

        private void Awake()
        {
            mob = GetComponent<MobEntity>();
            actionController = GetComponent<MobActionController>();
        }

        private void Update()
        {
            if (mob == null || !mob.IsAlive)
            {
                return;
            }

            if (target == null)
            {
                TryAcquireTargetByAggroRange();
                return;
            }

            if (actionController != null)
            {
                actionController.TryAttack(target);
            }
        }

        private void TryAcquireTargetByAggroRange()
        {
            var aggroRange = mob.Template.aggroRange;
            if (aggroRange <= 0f)
            {
                return;
            }

            var hits = Physics2D.OverlapCircleAll(transform.position, aggroRange, playerLayerMask);
            foreach (var hit in hits)
            {
                var player = hit.GetComponentInParent<PlayerCombatant>();
                if (player != null && player.IsAlive)
                {
                    target = player.transform;
                    return;
                }
            }
        }
    }
}
