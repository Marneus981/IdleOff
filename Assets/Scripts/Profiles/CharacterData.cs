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
        public MainStats MainStats => mainStats;
        public SecondaryStats SecondaryStats => secondaryStats;
        public float Speed => secondaryStats.GetSpeed();

        public CharacterData()
        {
        }

        public CharacterData(string characterName, CharacterGender gender, int level, float speed)
        {
            this.characterName = characterName;
            this.gender = gender;
            GetCharacterClass().SetLevelNumber(level);
            secondaryStats.SetSpeed(speed);
        }

        public CharacterData(string characterName, CharacterGender gender, CharacterClass characterClass, float speed)
        {
            this.characterName = characterName;
            this.gender = gender;
            this.characterClass = characterClass ?? CharacterClass.CreateWanderingSoul();
            secondaryStats.SetSpeed(speed);
        }

        public void UpdateStats()
        {
            mainStats.Update();
            secondaryStats.Update();
        }

        private CharacterClass GetCharacterClass()
        {
            characterClass ??= CharacterClass.CreateWanderingSoul();
            return characterClass;
        }
    }
}
