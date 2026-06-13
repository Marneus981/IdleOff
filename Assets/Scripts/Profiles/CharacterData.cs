using System;
using UnityEngine;

namespace IdleOff.Profiles
{
    [Serializable]
    public sealed class CharacterData
    {
        [SerializeField] private string characterName = "New Character";
        [SerializeField] private CharacterGender gender = CharacterGender.Unspecified;
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0f)] private float speed = 5f;

        public string CharacterName => characterName;
        public CharacterGender Gender => gender;
        public int Level => level;
        public float Speed => speed;

        public CharacterData()
        {
        }

        public CharacterData(string characterName, CharacterGender gender, int level, float speed)
        {
            this.characterName = characterName;
            this.gender = gender;
            this.level = Mathf.Max(1, level);
            this.speed = Mathf.Max(0f, speed);
        }
    }
}
