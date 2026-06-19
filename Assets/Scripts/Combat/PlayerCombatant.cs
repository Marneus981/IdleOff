using System.Collections.Generic;
using IdleOff.Profiles;
using UnityEngine;

namespace IdleOff.Combat
{
    public sealed class PlayerCombatant : MonoBehaviour, ICombatant
    {
        [SerializeField] private CharacterProfile profile;
        [SerializeField] private float currentHp = -1f;

        private static readonly IReadOnlyList<string> EmptyTags = new List<string>();

        public CharacterData Character => profile != null ? profile.ActiveCharacter : null;
        public string DisplayName => Character != null ? Character.CharacterName : "Player";
        public bool IsAlive => CurrentHp > 0f;
        public bool IsPlayerControlled => true;
        public IReadOnlyList<string> Tags => EmptyTags;
        public float CurrentHp => currentHp < 0f ? GetMaxHp() : currentHp;

        public void SetProfile(CharacterProfile characterProfile)
        {
            profile = characterProfile;
            ResetHpToMax();
        }

        public float GetStatValueByID(int statID)
        {
            return Character == null ? 0f : Character.GetStatValueByID(statID);
        }

        public void ReceiveDamage(DamageResult result)
        {
            if (!result.Hit || result.FinalDamage <= 0f)
            {
                return;
            }

            if (currentHp < 0f)
            {
                ResetHpToMax();
            }

            currentHp = Mathf.Max(0f, currentHp - result.FinalDamage);
        }

        public void ResetHpToMax()
        {
            currentHp = GetMaxHp();
        }

        private float GetMaxHp()
        {
            return Mathf.Max(1f, GetStatValueByID(1011));
        }
    }
}
