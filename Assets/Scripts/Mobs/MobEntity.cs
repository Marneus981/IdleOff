using System;
using System.Collections.Generic;
using IdleOff.Actions;
using IdleOff.Combat;
using IdleOff.Drops;
using UnityEngine;

namespace IdleOff.Mobs
{
    public sealed class MobEntity : MonoBehaviour, IMobCombatant
    {
        [SerializeField] private int mobID = 6001;
        [SerializeField] private WorldDropSpawner dropSpawner;

        private MobRuntimeData runtimeData;
        private bool deathResolved;

        public event System.Action<MobEntity, ICombatant> Damaged;
        public event System.Action<MobEntity> Died;

        public int MobID => Template.mobID;
        public MobType MobType => Template.mobType;
        public float AC => Template.ac;
        public MobRuntimeData RuntimeData => runtimeData;
        public string DisplayName => Template.name;
        public bool IsAlive => runtimeData != null && runtimeData.IsAlive;
        public bool IsPlayerControlled => false;
        public IReadOnlyList<string> Tags => Template.tags;
        public MobTemplate Template => runtimeData?.Template ?? GetTemplateFromCatalog();

        private void Awake()
        {
            if (runtimeData == null)
            {
                Initialize(mobID);
            }
        }

        public void Initialize(int templateID)
        {
            MobCatalog.EnsureLoaded();
            if (!MobCatalog.Mobs.TryGetValue(templateID, out var template))
            {
                throw new KeyNotFoundException($"Mob ID {templateID} was not found.");
            }

            mobID = templateID;
            Initialize(template);
        }

        public void Initialize(MobTemplate template)
        {
            runtimeData = new MobRuntimeData(template.Clone());
            deathResolved = false;
        }

        public IdleOff.Actions.Action GetBasicAction()
        {
            ActionCatalog.EnsureLoaded();
            if (!ActionCatalog.MobActions.TryGetValue(Template.basicActionID, out var action))
            {
                throw new KeyNotFoundException($"Mob action ID {Template.basicActionID} was not found.");
            }

            return action.Clone();
        }

        public float GetStatValueByID(int statID)
        {
            return statID switch
            {
                CombatStatIDs.Defense => Template.defense,
                CombatStatIDs.Damage => Template.damage,
                _ => 0f
            };
        }

        public void ReceiveDamage(DamageResult result)
        {
            if (runtimeData == null || !result.Hit)
            {
                return;
            }

            runtimeData.ReceiveDamage(result.FinalDamage);
            Damaged?.Invoke(this, result.Attacker);
            if (!runtimeData.IsAlive)
            {
                ResolveDeath(result.Attacker);
            }
        }

        private void ResolveDeath(ICombatant killer)
        {
            if (deathResolved)
            {
                return;
            }

            deathResolved = true;
            if (killer is PlayerCombatant player)
            {
                var reward = RewardResolver.ResolveMobDeath(player, this);
                var spawner = dropSpawner != null ? dropSpawner : WorldDropSpawner.Instance;
                spawner?.SpawnDrops(reward.Drops, transform.position);
            }

            Died?.Invoke(this);
        }

        private MobTemplate GetTemplateFromCatalog()
        {
            MobCatalog.EnsureLoaded();
            if (!MobCatalog.Mobs.TryGetValue(mobID, out var template))
            {
                throw new KeyNotFoundException($"Mob ID {mobID} was not found.");
            }

            return template;
        }
    }
}
