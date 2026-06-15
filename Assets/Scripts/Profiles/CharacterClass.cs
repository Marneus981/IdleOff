using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class CharacterClass
    {
        [SerializeField] private string className = "Wandering Soul";
        [SerializeField, Min(1)] private int levelNumber = 1;
        [SerializeField, Min(0f)] private float currentXP;
        [SerializeField, Min(0f)] private float maxXP = 100f;
        [SerializeField] private List<Modifier> modifiers = new();
        [SerializeField] private List<Active> actives = new();

        public IReadOnlyList<Modifier> PassiveAbilities => modifiers;
        public IReadOnlyList<Active> ActiveAbilities => actives;

        public CharacterClass()
        {
        }

        private CharacterClass(string className)
        {
            this.className = className;
        }

        public static CharacterClass CreateWanderingSoul()
        {
            return new CharacterClass("Wandering Soul");
        }

        public static CharacterClass CreateErrantKnight()
        {
            return new CharacterClass("Errant Knight");
        }

        public static CharacterClass CreateWildHunter()
        {
            return new CharacterClass("Wild Hunter");
        }

        public static CharacterClass CreateVoidWhisperer()
        {
            return new CharacterClass("Void Whisperer");
        }

        public string GetClassName()
        {
            return className;
        }

        public void SetClassName(string value)
        {
            className = string.IsNullOrWhiteSpace(value) ? "Wandering Soul" : value;
        }

        public int GetLevelNumber()
        {
            return levelNumber;
        }

        public void SetLevelNumber(int value)
        {
            levelNumber = Mathf.Max(1, value);
        }

        public void LevelUp(CharacterData characterData)
        {
        }

        public float GetCurrentXP()
        {
            return currentXP;
        }

        public void SetCurrentXP(float value)
        {
            currentXP = Mathf.Max(0f, value);
        }

        public void AddCurrentXP(float value)
        {
            SetCurrentXP(currentXP + value);
        }

        public float GetMaxXP()
        {
            return maxXP;
        }

        public void SetMaxXP(float value)
        {
            maxXP = Mathf.Max(0f, value);
        }
        private void LoadModifiers()
        {
            ////TO BE IMPLEMENTED
        }
        private void LoadActives()
        {
            ////TO BE IMPLEMENTED
        }
    }

}
