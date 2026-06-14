using System;
using UnityEngine;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class SecondaryStats
    {
        [SerializeField, Min(0f)] private float accuracy = 2f;
        [SerializeField, Min(0f)] private float mastery = 0.35f;
        [SerializeField, Min(0f)] private float weaponPower;
        [SerializeField, Min(0f)] private float unarmedWeaponPower = 5f;
        [SerializeField, Min(0f)] private float defense;
        [SerializeField, Min(0f)] private float hp = 10f;
        [SerializeField, Min(0f)] private float mp = 5f;
        [SerializeField, Min(0f)] private float damageFlatBonus;
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;
        [SerializeField, Min(0f)] private float critChance = 0.025f;
        [SerializeField, Min(0f)] private float critDamage;
        [SerializeField, Min(0f)] private float bossDamage;
        [SerializeField, Min(0f)] private float dropRate;
        [SerializeField, Min(0f)] private float classXPRate;
        [SerializeField, Min(0f)] private float speed = 5f;

        public float GetAccuracy()
        {
            return accuracy;
        }

        public void SetAccuracy(float value)
        {
            accuracy = Mathf.Max(0f, value);
        }

        public void AddAccuracy(float value)
        {
            SetAccuracy(accuracy + value);
        }

        public void UpdateAccuracy()
        {
        }

        public float GetMastery()
        {
            return mastery;
        }

        public void SetMastery(float value)
        {
            mastery = Mathf.Max(0f, value);
        }

        public void AddMastery(float value)
        {
            SetMastery(mastery + value);
        }

        public void UpdateMastery()
        {
        }

        public float GetWeaponPower()
        {
            return weaponPower;
        }

        public void SetWeaponPower(float value)
        {
            weaponPower = Mathf.Max(0f, value);
        }

        public void AddWeaponPower(float value)
        {
            SetWeaponPower(weaponPower + value);
        }

        public void UpdateWeaponPower()
        {
        }

        public float GetUnarmedWeaponPower()
        {
            return unarmedWeaponPower;
        }

        public void SetUnarmedWeaponPower(float value)
        {
            unarmedWeaponPower = Mathf.Max(0f, value);
        }

        public void AddUnarmedWeaponPower(float value)
        {
            SetUnarmedWeaponPower(unarmedWeaponPower + value);
        }

        public void UpdateUnarmedWeaponPower()
        {
        }

        public float GetDefense()
        {
            return defense;
        }

        public void SetDefense(float value)
        {
            defense = Mathf.Max(0f, value);
        }

        public void AddDefense(float value)
        {
            SetDefense(defense + value);
        }

        public void UpdateDefense()
        {
        }

        public float GetHP()
        {
            return hp;
        }

        public void SetHP(float value)
        {
            hp = Mathf.Max(0f, value);
        }

        public void AddHP(float value)
        {
            SetHP(hp + value);
        }

        public void UpdateHP()
        {
        }

        public float GetMP()
        {
            return mp;
        }

        public void SetMP(float value)
        {
            mp = Mathf.Max(0f, value);
        }

        public void AddMP(float value)
        {
            SetMP(mp + value);
        }

        public void UpdateMP()
        {
        }

        public float GetDamageFlatBonus()
        {
            return damageFlatBonus;
        }

        public void SetDamageFlatBonus(float value)
        {
            damageFlatBonus = Mathf.Max(0f, value);
        }

        public void AddDamageFlatBonus(float value)
        {
            SetDamageFlatBonus(damageFlatBonus + value);
        }

        public void UpdateDamageFlatBonus()
        {
        }

        public float GetDamageMultiplier()
        {
            return damageMultiplier;
        }

        public void SetDamageMultiplier(float value)
        {
            damageMultiplier = Mathf.Max(0f, value);
        }

        public void AddDamageMultiplier(float value)
        {
            SetDamageMultiplier(damageMultiplier + value);
        }

        public void UpdateDamageMultiplier()
        {
        }

        public float GetCritChance()
        {
            return critChance;
        }

        public void SetCritChance(float value)
        {
            critChance = Mathf.Max(0f, value);
        }

        public void AddCritChance(float value)
        {
            SetCritChance(critChance + value);
        }

        public void UpdateCritChance()
        {
        }

        public float GetCritDamage()
        {
            return critDamage;
        }

        public void SetCritDamage(float value)
        {
            critDamage = Mathf.Max(0f, value);
        }

        public void AddCritDamage(float value)
        {
            SetCritDamage(critDamage + value);
        }

        public void UpdateCritDamage()
        {
        }

        public float GetBossDamage()
        {
            return bossDamage;
        }

        public void SetBossDamage(float value)
        {
            bossDamage = Mathf.Max(0f, value);
        }

        public void AddBossDamage(float value)
        {
            SetBossDamage(bossDamage + value);
        }

        public void UpdateBossDamage()
        {
        }

        public float GetDropRate()
        {
            return dropRate;
        }

        public void SetDropRate(float value)
        {
            dropRate = Mathf.Max(0f, value);
        }

        public void AddDropRate(float value)
        {
            SetDropRate(dropRate + value);
        }

        public void UpdateDropRate()
        {
        }

        public float GetClassXPRate()
        {
            return classXPRate;
        }

        public void SetClassXPRate(float value)
        {
            classXPRate = Mathf.Max(0f, value);
        }

        public void AddClassXPRate(float value)
        {
            SetClassXPRate(classXPRate + value);
        }

        public void UpdateClassXPRate()
        {
        }

        public float GetSpeed()
        {
            return speed;
        }

        public void SetSpeed(float value)
        {
            speed = Mathf.Max(0f, value);
        }

        public void AddSpeed(float value)
        {
            SetSpeed(speed + value);
        }

        public void UpdateSpeed()
        {
        }

        public void Update()
        {
            UpdateAccuracy();
            UpdateMastery();
            UpdateWeaponPower();
            UpdateUnarmedWeaponPower();
            UpdateDefense();
            UpdateHP();
            UpdateMP();
            UpdateDamageFlatBonus();
            UpdateDamageMultiplier();
            UpdateCritChance();
            UpdateCritDamage();
            UpdateBossDamage();
            UpdateDropRate();
            UpdateClassXPRate();
            UpdateSpeed();
        }
    }
}
