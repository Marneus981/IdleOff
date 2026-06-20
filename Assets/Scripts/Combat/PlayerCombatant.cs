using System.Collections.Generic;
using IdleOff.Maps;
using IdleOff.Profiles;
using UnityEngine;

namespace IdleOff.Combat
{
    public sealed class PlayerCombatant : MonoBehaviour, ICombatant
    {
        [SerializeField] private CharacterProfile profile;
        [SerializeField] private CombatHealth health = new();
        [SerializeField, Min(0f)] private float healthRegenPercentPerSecond = 0.01f;

        private static readonly IReadOnlyList<string> EmptyTags = new List<string>();

        public CharacterData Character => profile != null ? profile.ActiveCharacter : null;
        public string DisplayName => Character != null ? Character.CharacterName : "Player";
        public bool IsAlive => CurrentHp > 0f;
        public bool IsPlayerControlled => true;
        public IReadOnlyList<string> Tags => EmptyTags;
        public float CurrentHp => health.Max <= 0f ? GetMaxHp() : health.Current;

        public void SetProfile(CharacterProfile characterProfile)
        {
            profile = characterProfile;
            ResetHpToMax();
        }

        private void Update()
        {
            if (health.Max <= 0f)
            {
                ResetHpToMax();
            }

            if (health.IsAlive)
            {
                health.Heal(GetMaxHp() * healthRegenPercentPerSecond * Time.deltaTime);
            }
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

            if (health.Max <= 0f)
            {
                ResetHpToMax();
            }

            health.TakeDamage(result.FinalDamage);
            Debug.Log($"[Combat] {DisplayName} took {result.FinalDamage:0.##} damage from {result.Attacker?.DisplayName ?? "unknown attacker"}. HP {health.Current:0.##}/{health.Max:0.##}.");
            if (!health.IsAlive)
            {
                RespawnAtCurrentMapSpawn();
            }
        }

        public void ResetHpToMax()
        {
            health.Reset(GetMaxHp());
        }

        private float GetMaxHp()
        {
            return Mathf.Max(1f, GetStatValueByID(1011));
        }

        private void RespawnAtCurrentMapSpawn()
        {
            // Temporary death behavior: later this should become a real death/respawn flow with penalties, timers, UI, and save-state handling.
            if (MapManager.Instance != null && MapManager.Instance.TryGetCurrentSpawnPosition(out var spawnPosition))
            {
                transform.position = spawnPosition;
                var body = GetComponent<Rigidbody2D>();
                if (body != null)
                {
                    body.position = spawnPosition;
                    body.linearVelocity = Vector2.zero;
                    body.angularVelocity = 0f;
                }
            }

            ResetHpToMax();
            Debug.Log($"[Combat] {DisplayName} died and respawned at the current map spawn with full HP.");
        }
    }
}
