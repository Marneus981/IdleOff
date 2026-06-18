using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Profiles
{
    [CreateAssetMenu(fileName = "CharacterProfile", menuName = "IdleOff/Profile/Character Profile")]
    public sealed class CharacterProfile : ScriptableObject
    {
        public const int MaxCharacters = 10;

        [SerializeField, Min(0)] private int activeCharacterIndex;
        [SerializeField] private List<CharacterData> characters = new();

        public IReadOnlyList<CharacterData> Characters => characters;
        public int ActiveCharacterIndex => Mathf.Clamp(activeCharacterIndex, 0, Mathf.Max(0, characters.Count - 1));
        public CharacterData ActiveCharacter => characters.Count == 0 ? null : characters[ActiveCharacterIndex];

        public bool HasRoom => characters.Count < MaxCharacters;

        public bool TryAddCharacter(CharacterData character)
        {
            if (character == null || !HasRoom)
            {
                return false;
            }

            characters.Add(character);
            character.SetParentProfile(this);
            character.UpdateStats();
            return true;
        }

        public void SetActiveCharacterIndex(int index)
        {
            activeCharacterIndex = Mathf.Clamp(index, 0, Mathf.Max(0, characters.Count - 1));
        }

        private void OnValidate()
        {
            while (characters.Count > MaxCharacters)
            {
                characters.RemoveAt(characters.Count - 1);
            }

            activeCharacterIndex = Mathf.Clamp(activeCharacterIndex, 0, Mathf.Max(0, characters.Count - 1));
            BindCharactersToProfile();
        }

        private void OnEnable()
        {
            BindCharactersToProfile();
        }

        private void BindCharactersToProfile()
        {
            // Serialized CharacterData instances need their runtime parent profile restored after load/domain reload.
            foreach (var character in characters)
            {
                character?.SetParentProfile(this);
            }
        }
    }
}
