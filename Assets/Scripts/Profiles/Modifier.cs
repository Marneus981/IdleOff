//System for Passive effects (Talents in IdleOn) + others (might include food, etc.)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Profiles
{
    [Serializable]
    public class Modifier
    {
        
        public struct IndexIncrease
        {
            public int index;
            public float increase;
            public IndexIncrease(int defIndex = 0, float defIncrease = 0f)
            {
                index = defIndex;
                increase = defIncrease;
            }
        }
        public int modifierID;
        public Dictionary<int, IndexIncrease> indexIncreaseByStatID = new Dictionary<int, IndexIncrease>();
        public string name;
        public string description;
        public int level;
        public int maxLevel;
        [SerializeField] protected List<string> tags = new List<string>();
        [NonSerialized] protected CharacterData owner;

        public IReadOnlyList<string> GetTags()
        {
            return tags;
        }

        public bool HasTag(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) && tags != null && tags.Contains(tag);
        }

        internal void SetTags(List<string> tags)
        {
            // Modifier table loaders assign tags through this method while keeping the serialized field protected.
            this.tags = tags ?? new List<string>();
        }

        public void SetOwner(CharacterData owner)
        {
            this.owner = owner;
        }

        public virtual (int,float) AppliedIncrease(int statID)
        {
            //These Modifiers have linear scaling thus applied increse is a simple multiplication
            if (indexIncreaseByStatID == null || !indexIncreaseByStatID.TryGetValue(statID, out var indexIncrease))
            {
                throw new KeyNotFoundException($"Modifier ID {modifierID} does not define an increase for stat ID {statID}.");
            }

            var index = indexIncrease.index;
            var increase = indexIncrease.increase;
            return (index,increase*(float)level);

        }
        public int ModifierLevelChange(int change)
        {
            //Returns -1 on failed level change
            var newLevel = level + change;
            if(newLevel <= maxLevel && newLevel >= 0)
            {
                level = newLevel;
                if (owner == null)
                {
                    throw new InvalidOperationException($"Modifier ID {modifierID} is not bound to a character and cannot update stats.");
                }

                if (indexIncreaseByStatID == null)
                {
                    throw new InvalidOperationException($"Modifier ID {modifierID} has no stat mappings to update.");
                }

                foreach(var indexIncr in indexIncreaseByStatID)
                {
                    owner.UpdateByStatID(indexIncr.Key);
                }
                return 0;
            }
            return -1;
        }

    }

}
