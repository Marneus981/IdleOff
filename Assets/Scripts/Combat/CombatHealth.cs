using System;
using UnityEngine;

namespace IdleOff.Combat
{
    [Serializable]
    public sealed class CombatHealth
    {
        [SerializeField] private float current;
        [SerializeField] private float max = 1f;

        public float Current => current;
        public float Max => max;
        public bool IsAlive => current > 0f;

        public void Reset(float maxValue)
        {
            max = Mathf.Max(1f, maxValue);
            current = max;
        }

        public void SetCurrent(float value)
        {
            current = Mathf.Clamp(value, 0f, max);
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || !IsAlive)
            {
                return;
            }

            current = Mathf.Max(0f, current - amount);
        }
    }
}
