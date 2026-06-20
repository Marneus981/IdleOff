using System;
using System.Collections.Generic;

namespace IdleOff.Mobs
{
    [Serializable]
    public sealed class BossActionPattern
    {
        public int patternID;
        public string name;
        public int bossMobID;
        public float cooldown;
        public bool loop;
        public List<BossPatternStep> steps = new();

        public BossActionPattern Clone()
        {
            var clone = new BossActionPattern
            {
                patternID = patternID,
                name = name,
                bossMobID = bossMobID,
                cooldown = cooldown,
                loop = loop,
                steps = new List<BossPatternStep>()
            };

            if (steps != null)
            {
                foreach (var step in steps)
                {
                    clone.steps.Add(step?.Clone());
                }
            }

            return clone;
        }
    }
}
