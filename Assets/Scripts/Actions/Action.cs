// System for Actions

using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Actions
{
    [Serializable]
    public sealed class Action
    {
        public int actionID;
        public string name;
        public string description;
        public List<string> tags = new();
        public int level;
        public int maxLevel = 1;
        public float cooldown;
        public float range;
        public float attackScaling = 1f;
        public float attackScalingPerLevel;
        public ActionOwnerType ownerType = ActionOwnerType.Any;
        public ActionTargetingType targetingType = ActionTargetingType.ForwardMelee;
        public ActionHitboxType hitboxType = ActionHitboxType.Box;

        public bool HasTag(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) && tags != null && tags.Contains(tag);
        }

        public float GetAttackScaling()
        {
            return Mathf.Max(0f, attackScaling + attackScalingPerLevel * Mathf.Max(0, level - 1));
        }

        public Action Clone(int levelOverride = -1)
        {
            return new Action
            {
                actionID = actionID,
                name = name,
                description = description,
                tags = tags == null ? new List<string>() : new List<string>(tags),
                level = levelOverride >= 0 ? levelOverride : level,
                maxLevel = maxLevel,
                cooldown = cooldown,
                range = range,
                attackScaling = attackScaling,
                attackScalingPerLevel = attackScalingPerLevel,
                ownerType = ownerType,
                targetingType = targetingType,
                hitboxType = hitboxType
            };
        }
    }
}
