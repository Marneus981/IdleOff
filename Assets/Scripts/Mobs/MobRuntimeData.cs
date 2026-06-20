using System;
using IdleOff.Combat;
using UnityEngine;

namespace IdleOff.Mobs
{
    public sealed class MobRuntimeData
    {
        public MobRuntimeData(MobTemplate template)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
            health.Reset(template.maxHp);
        }

        private readonly CombatHealth health = new();
        public MobTemplate Template { get; }
        public float CurrentHp => health.Current;
        public bool IsAlive => health.IsAlive;

        public void ReceiveDamage(float amount)
        {
            health.TakeDamage(amount);
        }
    }
}
