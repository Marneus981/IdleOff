using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace IdleOff.Profiles
{
    [System.Serializable]
    public sealed class Wis : Stat
    {
        public override void UpdateStat()
        {
            //Update gets recalculated on every stat change, if this gets heavy I will implement a better more surgical update system
            var formulaValuesList = new List<float>();
            for (int i = 0; i < formulaParts; i++) formulaValuesList.Add(0f);

            foreach (var id in validModifierIDs)
            {
                if (id <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(validModifierIDs), id, $"Stat ID {statID} contains an invalid modifier ID.");
                }

                if (owner == null)
                {
                    throw new InvalidOperationException($"Stat ID {statID} is not bound to a character and cannot resolve modifier ID {id}.");
                }

                var tuple = owner.GetModifier(id).AppliedIncrease(statID);
                if (tuple.Item1 < 0 || tuple.Item1 >= formulaValuesList.Count)
                {
                    throw new IndexOutOfRangeException($"Modifier ID {id} returned formula index {tuple.Item1} for stat ID {statID}, but this stat has {formulaParts} formula parts.");
                }

                formulaValuesList[tuple.Item1] = formulaValuesList[tuple.Item1] + tuple.Item2;
            }
            SetValue(Formula(formulaValuesList));
            owner.UpdateByStatID(1013); //Update maxMp
            owner.UpdateByStatID(1018); //Update bossDamage
            var currentClass = CharacterClass.NormalizeClassName(owner.CharacterClass.GetClassName());
            switch (currentClass)
            {
                case "wanderingsoul":
                    break;
                case "errantknight":
                    owner.UpdateByStatID(1005); //Update accuracy
                    break;
                case "wildhunter":
                    break;
                case "voidwhisperer":
                    owner.UpdateByStatID(1022); //Update damage
                    break;
                default:
                    throw new ArgumentException($"Unknown class '{currentClass}'.", nameof(currentClass));
            }

        }
    }
}
