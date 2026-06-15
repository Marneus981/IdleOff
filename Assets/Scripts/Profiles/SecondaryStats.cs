using System;
using UnityEngine;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class SecondaryStats
    {
        [SerializeField] private string statsJsonPath = "Assets/Tables/Stats.json";
        [SerializeField] private Accuracy accuracy;
        [SerializeField] private Mastery mastery;
        [SerializeField] private WeaponPower weaponPower;
        [SerializeField] private UnarmedWeaponPower unarmedWeaponPower;
        [SerializeField] private Defense defense;
        [SerializeField] private Hp hp;
        [SerializeField] private MaxHp maxHp;
        [SerializeField] private Mp mp;
        [SerializeField] private MaxMp maxMp;
        [SerializeField] private DamageFlatBonus damageFlatBonus;
        [SerializeField] private DamageMultiplier damageMultiplier;
        [SerializeField] private CritChance critChance;
        [SerializeField] private CritDamage critDamage;
        [SerializeField] private BossDamage bossDamage;
        [SerializeField] private DropRate dropRate;
        [SerializeField] private ClassXPRate classXPRate;
        [SerializeField] private Speed speed;

        public Speed Speed
        {
            get
            {
                if (!IsLoaded())
                {
                    LoadSecondaryStats();
                }

                return speed;
            }
        }

        public void Update()
        {
            if (!IsLoaded())
            {
                LoadSecondaryStats();
            }

            accuracy.UpdateStat();
            mastery.UpdateStat();
            weaponPower.UpdateStat();
            unarmedWeaponPower.UpdateStat();
            defense.UpdateStat();
            hp.UpdateStat();
            maxHp.UpdateStat();
            mp.UpdateStat();
            maxMp.UpdateStat();
            damageFlatBonus.UpdateStat();
            damageMultiplier.UpdateStat();
            critChance.UpdateStat();
            critDamage.UpdateStat();
            bossDamage.UpdateStat();
            dropRate.UpdateStat();
            classXPRate.UpdateStat();
            speed.UpdateStat();
        }
        public void LoadSecondaryStats()
        {
            var statsTable = Stat.LoadStatsTable(statsJsonPath);
            accuracy = Stat.CreateFromTable<Accuracy>(1005, statsTable);
            mastery = Stat.CreateFromTable<Mastery>(1006, statsTable);
            weaponPower = Stat.CreateFromTable<WeaponPower>(1007, statsTable);
            unarmedWeaponPower = Stat.CreateFromTable<UnarmedWeaponPower>(1008, statsTable);
            defense = Stat.CreateFromTable<Defense>(1009, statsTable);
            hp = Stat.CreateFromTable<Hp>(1010, statsTable);
            maxHp = Stat.CreateFromTable<MaxHp>(1011, statsTable);
            mp = Stat.CreateFromTable<Mp>(1012, statsTable);
            maxMp = Stat.CreateFromTable<MaxMp>(1013, statsTable);
            damageFlatBonus = Stat.CreateFromTable<DamageFlatBonus>(1014, statsTable);
            damageMultiplier = Stat.CreateFromTable<DamageMultiplier>(1015, statsTable);
            critChance = Stat.CreateFromTable<CritChance>(1016, statsTable);
            critDamage = Stat.CreateFromTable<CritDamage>(1017, statsTable);
            bossDamage = Stat.CreateFromTable<BossDamage>(1018, statsTable);
            dropRate = Stat.CreateFromTable<DropRate>(1019, statsTable);
            classXPRate = Stat.CreateFromTable<ClassXPRate>(1020, statsTable);
            speed = Stat.CreateFromTable<Speed>(1021, statsTable);
        }

        public bool IsLoaded()
        {
            return accuracy != null
                && mastery != null
                && weaponPower != null
                && unarmedWeaponPower != null
                && defense != null
                && hp != null
                && maxHp != null
                && mp != null
                && maxMp != null
                && damageFlatBonus != null
                && damageMultiplier != null
                && critChance != null
                && critDamage != null
                && bossDamage != null
                && dropRate != null
                && classXPRate != null
                && speed != null;
        }
    }
}
