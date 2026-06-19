using System;
using UnityEngine;

namespace IdleOff.Mobs
{
    public sealed class MobRuntimeData
    {
        public MobRuntimeData(MobTemplate template)
        {
            Template = template ?? throw new ArgumentNullException(nameof(template));
            CurrentHp = Mathf.Max(1f, template.maxHp);
        }

        public MobTemplate Template { get; }
        public float CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0f;

        public void ReceiveDamage(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return;
            }

            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
        }
    }
}
