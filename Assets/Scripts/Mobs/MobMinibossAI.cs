using IdleOff.Combat;
using UnityEngine;

namespace IdleOff.Mobs
{
    public sealed class MobMinibossAI : MobBasicAI
    {
        [SerializeField] private LayerMask playerLayerMask = ~0;

        protected override void Update()
        {
            if (mob != null && mob.IsAlive && target == null)
            {
                TryAcquireTargetByAggroRange();
            }

            base.Update();
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
