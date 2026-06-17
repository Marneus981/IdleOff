using System;
using System.Collections.Generic;

namespace IdleOff.Profiles
{
    [System.Serializable]
    public sealed class Accuracy : Stat
    {
        public new float Formula(List<float> formulaValuesList)
        /*Standard Formula for Stat Updates, can be changed inside each subclass
        */
        {
            var currentClass = CharacterClass.NormalizeClassName(owner.CharacterClass.GetClassName());
            float flatClass = formulaValuesList[0];
            float percentClass = formulaValuesList[1];
            float flatSign = formulaValuesList[2];
            float percentSign = formulaValuesList[3];
            float flatEquip = formulaValuesList[4];
            float percentEquip = formulaValuesList[5];
            float flatFamily = formulaValuesList[6];
            float percentFamily = formulaValuesList[7];
            float generalPercentile = formulaValuesList[8];
            float mainScalingStatValue;
            switch (currentClass)
            {
                case "wanderingsoul":
                    mainScalingStatValue = owner.GetStatValueByID(1004);//Scaling stat is luck
                    break;
                case "errantknight":
                    mainScalingStatValue = owner.GetStatValueByID(1003);//Scaling stat is wis
                    break;
                case "wildhunter":
                    mainScalingStatValue = owner.GetStatValueByID(1001);//Scaling stat is str
                    break;
                case "voidwhisperer":
                    mainScalingStatValue = owner.GetStatValueByID(1002);//Scaling stat is agi
                    break;
                default:
                    throw new ArgumentException($"Unknown class '{currentClass}'.", nameof(currentClass));
            }
            float result = (defaultValue + (flatClass * (1 + percentClass))
                                         + (flatSign * (1 + percentSign))
                                         + (flatEquip * (1 + percentEquip))
                                         + (flatFamily * (1 + percentFamily))
                                         + (0.5f * mainScalingStatValue)
                                         ) * generalPercentile;
            return result;
        }
    }
}
