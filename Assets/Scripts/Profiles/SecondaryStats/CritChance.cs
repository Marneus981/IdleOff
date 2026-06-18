using System.Collections.Generic;

namespace IdleOff.Profiles
{
    [System.Serializable]
    public sealed class CritChance : Stat
    {
        public override float Formula(List<float> formulaValuesList)
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
            float mainScalingStatValue = owner.GetStatValueByID(1002);//Scaling stat is agi
            float result = (defaultValue + (flatClass * (1 + percentClass))
                                         + (flatSign * (1 + percentSign))
                                         + (flatEquip * (1 + percentEquip))
                                         + (flatFamily * (1 + percentFamily))
                                         + (0.00075f * mainScalingStatValue)
                                         ) * (1 + generalPercentile);
            return result;
        }
    }
}
