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
        [SerializeField] private CombatHealth mana = new();
        [SerializeField, Min(0f)] private float healthRegenPercentPerSecond = 0.01f;
        [SerializeField, Min(0f)] private float manaRegenPercentPerSecond = 0.01f;
        [SerializeField] private PlayerDeathRespawnMode deathRespawnMode = PlayerDeathRespawnMode.HubSpawn;

        private static readonly IReadOnlyList<string> EmptyTags = new List<string>();

        public CharacterData Character => profile != null ? profile.ActiveCharacter : null;
        public string DisplayName => Character != null ? Character.CharacterName : "Player";
        public bool IsAlive => CurrentHp > 0f;
        public bool IsPlayerControlled => true;
        public IReadOnlyList<string> Tags => EmptyTags;
        public float CurrentHp => health.Max <= 0f ? GetMaxHp() : health.Current;
        public float MaxHp => health.Max <= 0f ? GetMaxHp() : health.Max;
        public float CurrentMp => mana.Max <= 0f ? GetMaxMp() : mana.Current;
        public float MaxMp => mana.Max <= 0f ? GetMaxMp() : mana.Max;
        public PlayerDeathRespawnMode DeathRespawnMode
        {
            get => deathRespawnMode;
            set => deathRespawnMode = value;
        }

        public void SetProfile(CharacterProfile characterProfile)
        {
            profile = characterProfile;
            ResetHpToMax();
            ResetMpToMax();
        }

        private void Update()
        {
            TickHealthRegen(Time.deltaTime);
            TickMpRegen(Time.deltaTime);
        }

        public void TickHealthRegen(float deltaTime)
        {
            if (health.Max <= 0f)
            {
                ResetHpToMax();
            }

            if (health.IsAlive)
            {
                health.Heal(GetMaxHp() * healthRegenPercentPerSecond * Mathf.Max(0f, deltaTime));
            }
        }

        public void TickMpRegen(float deltaTime)
        {
            if (mana.Max <= 0f)
            {
                ResetMpToMax();
            }

            if (health.IsAlive)
            {
                mana.Heal(GetMaxMp() * manaRegenPercentPerSecond * Mathf.Max(0f, deltaTime));
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
                RespawnAfterDeath();
            }
        }

        public void ResetHpToMax()
        {
            health.Reset(GetMaxHp());
        }

        public void ResetMpToMax()
        {
            mana.Reset(GetMaxMp());
        }

        private float GetMaxHp()
        {
            return Mathf.Max(1f, GetStatValueByID(1011));
        }

        private float GetMaxMp()
        {
            return Mathf.Max(1f, GetStatValueByID(1013));
        }

        private void RespawnAfterDeath()
        {
            // Temporary death behavior: later this should become a real death/respawn flow with penalties, timers, UI, and save-state handling.
            if (MapManager.Instance != null)
            {
                if (deathRespawnMode == PlayerDeathRespawnMode.HubSpawn)
                {
                    MapManager.Instance.LoadMap(MapManager.HubMapID);
                }
                else if (MapManager.Instance.TryGetCurrentSpawnPosition(out var spawnPosition))
                {
                    MoveToRespawnPosition(spawnPosition);
                }
            }

            ResetHpToMax();
            ResetMpToMax();
            Debug.Log($"[Combat] {DisplayName} died and respawned with full HP and MP.");
        }

        private void MoveToRespawnPosition(Vector2 spawnPosition)
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
    }
}
