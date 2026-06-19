using IdleOff.Combat;
using IdleOff.Mobs;
using UnityEngine;

namespace IdleOff.Drops
{
    public static class RewardResolver
    {
        public static RewardResult ResolveMobDeath(
            PlayerCombatant player,
            IMobCombatant mob,
            IRandomSource random = null)
        {
            var result = new RewardResult();
            if (player == null || player.Character == null || mob == null)
            {
                return result;
            }

            random ??= UnityRandomSource.Shared;
            var xpRate = Mathf.Max(0f, player.GetStatValueByID(CombatStatIDs.ClassXPRate));
            result.XpReward = Mathf.RoundToInt(mob.RuntimeData.Template.xpReward * (1f + xpRate));
            GrantClassXP(player, result.XpReward);

            var dropRate = Mathf.Max(0f, player.GetStatValueByID(CombatStatIDs.DropRate));
            ResolveItemDrops(mob.RuntimeData.Template, dropRate, random, result);
            ResolveMoneyDrops(mob.RuntimeData.Template, dropRate, random, result);
            return result;
        }

        private static void GrantClassXP(PlayerCombatant player, int xpReward)
        {
            if (xpReward <= 0 || player.Character == null)
            {
                return;
            }

            var characterClass = player.Character.CharacterClass;
            characterClass.AddCurrentXP(xpReward);
            while (characterClass.GetCurrentXP() >= characterClass.GetMaxXP())
            {
                characterClass.LevelUp(player.Character);
            }
        }

        private static void ResolveItemDrops(
            MobTemplate template,
            float dropRate,
            IRandomSource random,
            RewardResult result)
        {
            if (template.itemDrops == null)
            {
                return;
            }

            foreach (var drop in template.itemDrops)
            {
                var quantity = RollQuantity(drop.chance * (1f + dropRate), random);
                for (var i = 0; i < quantity; i++)
                {
                    result.Drops.Add(WorldDropPayload.Item(drop.itemID));
                }
            }
        }

        private static void ResolveMoneyDrops(
            MobTemplate template,
            float dropRate,
            IRandomSource random,
            RewardResult result)
        {
            if (template.moneyDrops == null)
            {
                return;
            }

            foreach (var drop in template.moneyDrops)
            {
                var quantity = RollQuantity(drop.chance * (1f + dropRate), random);
                for (var i = 0; i < quantity; i++)
                {
                    result.Drops.Add(WorldDropPayload.Money(drop.money));
                }
            }
        }

        private static int RollQuantity(float effectiveChance, IRandomSource random)
        {
            if (effectiveChance <= 0f)
            {
                return 0;
            }

            var guaranteed = Mathf.FloorToInt(effectiveChance);
            var fractional = effectiveChance - guaranteed;
            return guaranteed + (random.Value < fractional ? 1 : 0);
        }
    }
}
