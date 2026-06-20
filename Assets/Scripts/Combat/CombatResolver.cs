using IdleOff.Actions;
using IdleOff.Mobs;
using UnityEngine;

namespace IdleOff.Combat
{
    public static class CombatResolver
    {
        public static DamageResult ResolvePlayerAction(
            ICombatant player,
            IMobCombatant mob,
            Action action,
            IRandomSource random = null)
        {
            if (player == null || mob == null || action == null || !player.IsAlive || !mob.IsAlive)
            {
                return new DamageResult(player, mob, false, 0f, 0f, 0f, 0);
            }

            random ??= UnityRandomSource.Shared;
            var hitChance = CalculatePlayerHitChance(player, mob);
            if (random.Value > hitChance)
            {
                var miss = new DamageResult(player, mob, false, hitChance, 0f, 0f, 0);
                Debug.Log($"[Combat] {player.DisplayName} used {action.name} on {mob.DisplayName}: miss. Hit chance {hitChance:P0}.");
                return miss;
            }

            var playerDamage = Mathf.Max(0f, player.GetStatValueByID(CombatStatIDs.Damage));
            var baseAttackDamage = playerDamage * action.GetAttackScaling();
            var mastery = Mathf.Max(0f, player.GetStatValueByID(CombatStatIDs.Mastery));
            var minAttackDamage = baseAttackDamage * mastery;
            var lowerDamage = Mathf.Min(minAttackDamage, baseAttackDamage);
            var upperDamage = Mathf.Max(minAttackDamage, baseAttackDamage);
            var rawDamage = random.Range(lowerDamage, upperDamage);

            var critCount = RollCritCount(player.GetStatValueByID(CombatStatIDs.CritChance), random);
            var critDamage = Mathf.Max(0f, player.GetStatValueByID(CombatStatIDs.CritDamage));
            for (var i = 0; i < critCount; i++)
            {
                rawDamage *= 1f + critDamage;
            }

            if (IsBossLike(mob))
            {
                rawDamage *= 1f + Mathf.Max(0f, player.GetStatValueByID(CombatStatIDs.BossDamage));
            }

            var finalDamage = Mathf.Max(1f, rawDamage - Mathf.Max(0f, mob.GetStatValueByID(CombatStatIDs.Defense)));
            var result = new DamageResult(player, mob, true, hitChance, rawDamage, finalDamage, critCount);
            mob.ReceiveDamage(result);
            Debug.Log($"[Combat] {player.DisplayName} used {action.name} on {mob.DisplayName}: hit, dealt {finalDamage:0.##} damage (raw {rawDamage:0.##}, crits {critCount}, hit chance {hitChance:P0}).");
            return result;
        }

        public static DamageResult ResolveMobAction(
            IMobCombatant mob,
            ICombatant target,
            Action action)
        {
            if (mob == null || target == null || action == null || !mob.IsAlive || !target.IsAlive)
            {
                return new DamageResult(mob, target, false, 0f, 0f, 0f, 0);
            }

            var rawDamage = Mathf.Max(0f, mob.GetStatValueByID(CombatStatIDs.Damage)) * action.GetAttackScaling();
            var finalDamage = Mathf.Max(1f, rawDamage - Mathf.Max(0f, target.GetStatValueByID(CombatStatIDs.Defense)));
            var result = new DamageResult(mob, target, true, 1f, rawDamage, finalDamage, 0);
            target.ReceiveDamage(result);
            Debug.Log($"[Combat] {mob.DisplayName} used {action.name} on {target.DisplayName}: hit, dealt {finalDamage:0.##} damage (raw {rawDamage:0.##}).");
            return result;
        }

        public static float CalculatePlayerHitChance(ICombatant player, IMobCombatant mob)
        {
            if (player == null || mob == null)
            {
                return 0f;
            }

            var accuracy = Mathf.Max(0f, player.GetStatValueByID(CombatStatIDs.Accuracy));
            var ac = Mathf.Max(1f, mob.AC);
            return Mathf.Clamp01(accuracy / ac);
        }

        private static int RollCritCount(float critChance, IRandomSource random)
        {
            var cappedCritChance = Mathf.Clamp(critChance, 0f, 2f);
            var critCount = 0;
            if (cappedCritChance >= 1f)
            {
                critCount++;
                if (random.Value < cappedCritChance - 1f)
                {
                    critCount++;
                }
            }
            else if (random.Value < cappedCritChance)
            {
                critCount++;
            }

            return critCount;
        }

        private static bool IsBossLike(IMobCombatant mob)
        {
            if (mob.MobType == MobType.Boss || mob.Tags == null)
            {
                return mob.MobType == MobType.Boss;
            }

            foreach (var tag in mob.Tags)
            {
                if (tag == "boss")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
