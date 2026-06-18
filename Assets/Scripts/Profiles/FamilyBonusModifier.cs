using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Profiles
{
    [System.Serializable]
    public sealed class FamilyBonusModifier : Modifier
    {
        public FamilyBonusModifier()
        {
            modifierID = 3001;
            name = "Family Bonus";
            description = "Bonus given for leveling up multiple characters with different classes";
            maxLevel = 1;
            level = 1;
            tags = new List<string> { "family" };
            indexIncreaseByStatID = new Dictionary<int, IndexIncrease>
            {
                { 1001, new IndexIncrease(6, 0f) },
                { 1002, new IndexIncrease(6, 0f) },
                { 1003, new IndexIncrease(6, 0f) },
                { 1004, new IndexIncrease(6, 0f) }
            };
        }

        public override (int, float) AppliedIncrease(int statID)
        {
            if (indexIncreaseByStatID == null || !indexIncreaseByStatID.TryGetValue(statID, out var indexIncrease))
            {
                throw new KeyNotFoundException($"Family bonus modifier does not define an increase for stat ID {statID}.");
            }

            if (owner == null)
            {
                throw new System.InvalidOperationException("Family bonus modifier is not bound to a character.");
            }

            var sourceLevel = GetSourceLevelForStat(statID);
            // Family bonus starts contributing only after the rounded half-source-level value is greater than 2.
            var increase = Mathf.Ceil(sourceLevel / 2f);
            if (increase <= 2f)
            {
                increase = 0f;
            }

            return (indexIncrease.index, increase);
        }

        private int GetSourceLevelForStat(int statID)
        {
            return statID switch
            {
                1001 => GetHighestClassLevel("errantknight"),
                1002 => GetHighestClassLevel("wildhunter"),
                1003 => GetHighestClassLevel("voidwhisperer"),
                1004 => GetHighestOverallLevel(),
                _ => 0
            };
        }

        private int GetHighestClassLevel(string normalizedClassName)
        {
            var characters = owner.ParentProfile?.Characters;
            if (characters == null)
            {
                return 0;
            }

            var highestLevel = 0;
            foreach (var character in characters)
            {
                if (character == null)
                {
                    continue;
                }

                var characterClassName = CharacterClass.NormalizeClassName(character.CharacterClass.GetClassName());
                if (characterClassName == normalizedClassName)
                {
                    highestLevel = Mathf.Max(highestLevel, character.Level);
                }
            }

            return highestLevel;
        }

        private int GetHighestOverallLevel()
        {
            var characters = owner.ParentProfile?.Characters;
            if (characters == null)
            {
                return 0;
            }

            var highestLevel = 0;
            foreach (var character in characters)
            {
                if (character != null)
                {
                    highestLevel = Mathf.Max(highestLevel, character.Level);
                }
            }

            return highestLevel;
        }
    }
}
