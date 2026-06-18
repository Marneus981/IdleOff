using System;
using System.Collections.Generic;

namespace IdleOff.Profiles
{
    [System.Serializable]
    public sealed class Damage : Stat
    {
        public override float Formula(List<float> formulaValuesList)
        {
            var currentClass = CharacterClass.NormalizeClassName(owner.CharacterClass.GetClassName());
            float WP;
            if (owner.GetStatValueByID(1007) > 0f)
            {
                WP = owner.GetStatValueByID(1007); //Use Weapon Power if weapon equipped; might change for rubostness if needed
            }
            else
            {
                WP = owner.GetStatValueByID(1008);
            }
            float damageMultiplier = owner.GetStatValueByID(1015);//Get damageMultiplier
            float damageFlatBonus = owner.GetStatValueByID(1014);
            float WPScaling = formulaValuesList[0];
            float flatClass = formulaValuesList[1];
            float percentClass = formulaValuesList[2];
            float flatSign = formulaValuesList[3];
            float percentSign = formulaValuesList[4];
            float flatEquip = formulaValuesList[5];
            float percentEquip = formulaValuesList[6];
            float flatFamily = formulaValuesList[7];
            float percentFamily = formulaValuesList[8];
            float mainScalingStatMultiplier = formulaValuesList[9];
            float mainScalingStatValue;
            switch (currentClass)
            {
                case "wanderingsoul":
                    mainScalingStatValue = owner.GetStatValueByID(1004);//Scaling stat is luck
                    break;
                case "errantknight":
                    mainScalingStatValue = owner.GetStatValueByID(1001);//Scaling stat is str
                    break;
                case "wildhunter":
                    mainScalingStatValue = owner.GetStatValueByID(1002);//Scaling stat is agi
                    break;
                case "voidwhisperer":
                    mainScalingStatValue = owner.GetStatValueByID(1003);//Scaling stat is wis
                    break;
                default:
                    throw new ArgumentException($"Unknown class '{currentClass}'.", nameof(currentClass));
            }
            float result = (defaultValue + damageFlatBonus
                                         + (1f /9f) * (WP * (1+ WPScaling) * WP * (1 + WPScaling))
                                         + (mainScalingStatMultiplier * (2f * mainScalingStatValue))
                                         + (flatClass * (1 + percentClass))
                                         + (flatSign * (1 + percentSign))
                                         + (flatEquip * (1 + percentEquip))
                                         + (flatFamily * (1 + percentFamily))
                                         ) * (1 + damageMultiplier);
            return result;
        }
    }
}
