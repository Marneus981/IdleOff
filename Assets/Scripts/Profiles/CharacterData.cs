using System;
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
            return characterClass;
        }
        public static Modifier GetModifier(int modifierID)
        {
            ////TO BE IMPLEMENTED
            throw new NotImplementedException($"Modifier lookup is not implemented for modifier ID {modifierID}.");
        }        
        public static void UpdateByStatID(int statID)
        {
            ////TO BE IMPLEMENTED
        }
        private void LoadStats()
        {
            mainStats ??= new MainStats();
            secondaryStats ??= new SecondaryStats();

            if (!mainStats.IsLoaded())
            {
                mainStats.LoadMainStats();
            }

            if (!secondaryStats.IsLoaded())
            {
                secondaryStats.LoadSecondaryStats();
            }
        }
    }
}
