using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class CharacterData
    {
        [SerializeField] private string characterName = "New Character";
        [SerializeField] private CharacterGender gender = CharacterGender.Unspecified;
        [SerializeField] private CharacterClass characterClass = CharacterClass.CreateWanderingSoul();
        [SerializeField] private MainStats mainStats = new();
        [SerializeField] private SecondaryStats secondaryStats = new();

        public string CharacterName => characterName;
        public CharacterGender Gender => gender;
        public CharacterClass CharacterClass => GetCharacterClass();
        public int Level => GetCharacterClass().GetLevelNumber();
        public MainStats MainStats
        {
            get
            {
                LoadStats();
                return mainStats;
            }
        }

        public SecondaryStats SecondaryStats
        {
            get
            {
                LoadStats();
                return secondaryStats;
            }
        }

        public Speed Speed
        {
            get
            {
                LoadStats();
                return secondaryStats.Speed;
            }
        }

        public CharacterData()
        {
            LoadStats();
        }

        public CharacterData(string characterName, CharacterGender gender, int level)
        {
            LoadStats();
            this.characterName = characterName;
            this.gender = gender;
            GetCharacterClass().SetLevelNumber(level);
        }

        public CharacterData(string characterName, CharacterGender gender, CharacterClass characterClass)
        {
            LoadStats();
            this.characterName = characterName;
            this.gender = gender;
            this.characterClass = characterClass ?? CharacterClass.CreateWanderingSoul();
        }

        public void UpdateStats()
        {
            LoadStats(); //may reset saved values for the character
            mainStats.Update();
            secondaryStats.Update();
        }

        private CharacterClass GetCharacterClass()
        {
            characterClass ??= CharacterClass.CreateWanderingSoul();
            characterClass.SetOwner(this);
            return characterClass;
        }

        public Modifier GetModifier(int modifierID)
        {
            return GetCharacterClass().GetModifier(modifierID);
        }        

        public void UpdateByStatID(int statID)
        {
            if (statID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(statID), statID, "Stat ID must be positive.");
            }

            LoadStats();
            if (mainStats.UpdateByStatID(statID) || secondaryStats.UpdateByStatID(statID))
            {
                return;
            }

            throw new KeyNotFoundException($"Stat ID {statID} was not found in main or secondary stats.");
        }

        private void LoadStats()
        {
            mainStats ??= new MainStats();
            secondaryStats ??= new SecondaryStats();
            GetCharacterClass().SetOwner(this);
            mainStats.SetOwner(this);
            secondaryStats.SetOwner(this);

            if (!mainStats.IsLoaded())
            {
                mainStats.LoadMainStats(this);
            }

            if (!secondaryStats.IsLoaded())
            {
                secondaryStats.LoadSecondaryStats(this);
            }
        }
        public float GetStatValueByID(int statID)
        {
            if (statID < 1005)
            {
                return MainStats.GetStatValueByID(statID);
            }
            return SecondaryStats.GetStatValueByID(statID);
        }
    }
}
