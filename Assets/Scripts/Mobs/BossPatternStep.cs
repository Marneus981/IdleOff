using System;
using UnityEngine;

namespace IdleOff.Mobs
{
    [Serializable]
    public sealed class BossPatternStep
    {
        public float delay;
        public int actionID;
        public BossPatternOriginMode originMode = BossPatternOriginMode.BossPosition;
        public BossPatternDirectionMode directionMode = BossPatternDirectionMode.TowardTarget;
        public string anchorID;
        public Vector2 offset;
        public Vector2 fixedDirection = Vector2.right;
        public int count = 1;
        public float spreadAngle;
        public float spacing;
        public int repeatCount = 1;
        public float repeatInterval;
        public float telegraphDurationOverride = -1f;

        public BossPatternStep Clone()
        {
            return new BossPatternStep
            {
                delay = delay,
                actionID = actionID,
                originMode = originMode,
                directionMode = directionMode,
                anchorID = anchorID,
                offset = offset,
                fixedDirection = fixedDirection,
                count = count,
                spreadAngle = spreadAngle,
                spacing = spacing,
                repeatCount = repeatCount,
                repeatInterval = repeatInterval,
                telegraphDurationOverride = telegraphDurationOverride
            };
        }
    }
}
