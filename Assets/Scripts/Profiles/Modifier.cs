//System for Passive effects (Talents in IdleOn) + others (might include food, etc.)
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

namespace IdleOff.Profiles
{
    public sealed class Modifier
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

        public  (int,float) AppliedIncrease(int statID)
        {
            //These Modifiers have linear scaling thus applied increse is a simple multiplication

            var index = indexIncreaseByStatID[statID].index;
            var increase = indexIncreaseByStatID[statID].increase;
            return (index,increase*(float)level);

        }
        public int ModifierLevelChange(int change)
        {
            //Returns -1 on failed level change
            if(level + change <= maxLevel && level + change >= 0)
            {
                level = level + change;
                foreach(var indexIncr in indexIncreaseByStatID)
                {
                    CharacterData.UpdateByStatID(indexIncr.Key);////TBA///CharacterData.UpdateByStatID calls update method corresponding to the stat ID in question
                }
                return 0;
            }
            return -1;
        }

    }

}