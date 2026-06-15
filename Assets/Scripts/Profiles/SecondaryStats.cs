using System;
using UnityEngine;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class SecondaryStats
    {
        [SerializeField] private string statsJsonPath = "Assets/Tables/DummyStats.json";
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
            accuracy = Stat.CreateFromTable<Accuracy>("accuracy", statsTable);
            mastery = Stat.CreateFromTable<Mastery>("mastery", statsTable);
            weaponPower = Stat.CreateFromTable<WeaponPower>("weaponPower", statsTable);
            unarmedWeaponPower = Stat.CreateFromTable<UnarmedWeaponPower>("unarmedWeaponPower", statsTable);
            defense = Stat.CreateFromTable<Defense>("defense", statsTable);
            hp = Stat.CreateFromTable<Hp>("hp", statsTable);
            maxHp = Stat.CreateFromTable<MaxHp>("maxHp", statsTable);
            mp = Stat.CreateFromTable<Mp>("mp", statsTable);
            maxMp = Stat.CreateFromTable<MaxMp>("maxMp", statsTable);
            damageFlatBonus = Stat.CreateFromTable<DamageFlatBonus>("damageFlatBonus", statsTable);
            damageMultiplier = Stat.CreateFromTable<DamageMultiplier>("damageMultiplier", statsTable);
            critChance = Stat.CreateFromTable<CritChance>("critChance", statsTable);
            critDamage = Stat.CreateFromTable<CritDamage>("critDamage", statsTable);
            bossDamage = Stat.CreateFromTable<BossDamage>("bossDamage", statsTable);
            dropRate = Stat.CreateFromTable<DropRate>("dropRate", statsTable);
            classXPRate = Stat.CreateFromTable<ClassXPRate>("classXPRate", statsTable);
            speed = Stat.CreateFromTable<Speed>("speed", statsTable);
        }

        public float GetSpeed()
        {
            speed ??= Stat.CreateFromTable<Speed>("speed", Stat.LoadStatsTable(statsJsonPath));
            return speed.GetValue();
        }

        public void SetSpeed(float value)
        {
            speed ??= Stat.CreateFromTable<Speed>("speed", Stat.LoadStatsTable(statsJsonPath));
            speed.SetValue(value);
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
