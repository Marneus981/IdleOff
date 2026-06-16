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
        [NonSerialized] private CharacterData owner;

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
            LoadSecondaryStats(owner);
        }

        public void LoadSecondaryStats(CharacterData owner)
        {
            var statsTable = Stat.LoadStatsTable(statsJsonPath);
            this.owner = owner;
            accuracy = Stat.CreateFromTable<Accuracy>(1005, statsTable, owner);
            mastery = Stat.CreateFromTable<Mastery>(1006, statsTable, owner);
            weaponPower = Stat.CreateFromTable<WeaponPower>(1007, statsTable, owner);
            unarmedWeaponPower = Stat.CreateFromTable<UnarmedWeaponPower>(1008, statsTable, owner);
            defense = Stat.CreateFromTable<Defense>(1009, statsTable, owner);
            hp = Stat.CreateFromTable<Hp>(1010, statsTable, owner);
            maxHp = Stat.CreateFromTable<MaxHp>(1011, statsTable, owner);
            mp = Stat.CreateFromTable<Mp>(1012, statsTable, owner);
            maxMp = Stat.CreateFromTable<MaxMp>(1013, statsTable, owner);
            damageFlatBonus = Stat.CreateFromTable<DamageFlatBonus>(1014, statsTable, owner);
            damageMultiplier = Stat.CreateFromTable<DamageMultiplier>(1015, statsTable, owner);
            critChance = Stat.CreateFromTable<CritChance>(1016, statsTable, owner);
            critDamage = Stat.CreateFromTable<CritDamage>(1017, statsTable, owner);
            bossDamage = Stat.CreateFromTable<BossDamage>(1018, statsTable, owner);
            dropRate = Stat.CreateFromTable<DropRate>(1019, statsTable, owner);
            classXPRate = Stat.CreateFromTable<ClassXPRate>(1020, statsTable, owner);
            speed = Stat.CreateFromTable<Speed>(1021, statsTable, owner);
        }

        public void SetOwner(CharacterData owner)
        {
            this.owner = owner;
            accuracy?.SetOwner(owner);
            mastery?.SetOwner(owner);
            weaponPower?.SetOwner(owner);
            unarmedWeaponPower?.SetOwner(owner);
            defense?.SetOwner(owner);
            hp?.SetOwner(owner);
            maxHp?.SetOwner(owner);
            mp?.SetOwner(owner);
            maxMp?.SetOwner(owner);
            damageFlatBonus?.SetOwner(owner);
            damageMultiplier?.SetOwner(owner);
            critChance?.SetOwner(owner);
            critDamage?.SetOwner(owner);
            bossDamage?.SetOwner(owner);
            dropRate?.SetOwner(owner);
            classXPRate?.SetOwner(owner);
            speed?.SetOwner(owner);
        }

        public bool UpdateByStatID(int statID)
        {
            if (!IsLoaded())
            {
                LoadSecondaryStats(owner);
            }

            switch (statID)
            {
                case 1005:
                    accuracy.UpdateStat();
                    return true;
                case 1006:
                    mastery.UpdateStat();
                    return true;
                case 1007:
                    weaponPower.UpdateStat();
                    return true;
                case 1008:
                    unarmedWeaponPower.UpdateStat();
                    return true;
                case 1009:
                    defense.UpdateStat();
                    return true;
                case 1010:
                    hp.UpdateStat();
                    return true;
                case 1011:
                    maxHp.UpdateStat();
                    return true;
                case 1012:
                    mp.UpdateStat();
                    return true;
                case 1013:
                    maxMp.UpdateStat();
                    return true;
                case 1014:
                    damageFlatBonus.UpdateStat();
                    return true;
                case 1015:
                    damageMultiplier.UpdateStat();
                    return true;
                case 1016:
                    critChance.UpdateStat();
                    return true;
                case 1017:
                    critDamage.UpdateStat();
                    return true;
                case 1018:
                    bossDamage.UpdateStat();
                    return true;
                case 1019:
                    dropRate.UpdateStat();
                    return true;
                case 1020:
                    classXPRate.UpdateStat();
                    return true;
                case 1021:
                    speed.UpdateStat();
                    return true;
                default:
                    return false;
            }
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
