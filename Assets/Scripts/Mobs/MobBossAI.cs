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
        private BossPatternExecutor patternExecutor;
        private Transform target;

        public Transform Target => target;
        public bool HasTarget => target != null;

        private void Awake()
        {
            mob = GetComponent<MobEntity>();
            actionController = GetComponent<MobActionController>();
            patternExecutor = GetComponent<BossPatternExecutor>();
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

            if (patternExecutor != null && patternExecutor.LoadedPatternCount > 0)
            {
                patternExecutor.SetTarget(target);
                if (patternExecutor.IsExecuting || IsTargetInAggroRange())
                {
                    return;
                }
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
                    if (patternExecutor != null)
                    {
                        patternExecutor.SetTarget(target);
                    }
                    return;
                }
            }
        }

        private bool IsTargetInAggroRange()
        {
            return target != null
                && (mob.Template.aggroRange <= 0f || Vector2.Distance(transform.position, target.position) <= mob.Template.aggroRange);
        }
    }
}
