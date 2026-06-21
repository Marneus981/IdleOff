using IdleOff.Combat;
using IdleOff.Game;
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
            var levelReward = GrantClassXP(player, result.XpReward);
            FloatingFeedbackService.ShowXpGain(player, result.XpReward);
            for (var i = 0; i < levelReward.LevelsGained; i++)
            {
                FloatingFeedbackService.ShowLevelUp(player);
            }

            FloatingFeedbackService.ShowBaseTalentPoints(player, levelReward.BaseTalentPointsGained);
            FloatingFeedbackService.ShowClassTalentPoints(player, levelReward.ClassTalentPointsGained);
            Debug.Log($"[Reward] {player.DisplayName} gained {result.XpReward} class XP from {mob.DisplayName} (base {mob.RuntimeData.Template.xpReward}, rate +{xpRate:P0}).");

            var dropRate = Mathf.Max(0f, player.GetStatValueByID(CombatStatIDs.DropRate));
            ResolveItemDrops(mob.RuntimeData.Template, dropRate, random, result);
            ResolveMoneyDrops(mob.RuntimeData.Template, dropRate, random, result);
            foreach (var drop in result.Drops)
            {
                FloatingFeedbackService.ShowDropName(player, drop);
            }

            return result;
        }

        private static LevelRewardFeedback GrantClassXP(PlayerCombatant player, int xpReward)
        {
            if (xpReward <= 0 || player.Character == null)
            {
                return default;
            }

            var characterClass = player.Character.CharacterClass;
            var previousBasePoints = characterClass.GetBaseTalentPoints();
            var previousClassPoints = characterClass.GetClassTalentPoints();
            var previousLevel = player.Character.Level;
            characterClass.AddCurrentXP(xpReward);
            while (characterClass.GetCurrentXP() >= characterClass.GetMaxXP())
            {
                characterClass.LevelUp(player.Character);
            }

            GameSession.SaveActiveProfile();
            return new LevelRewardFeedback(
                Mathf.Max(0, player.Character.Level - previousLevel),
                Mathf.Max(0, characterClass.GetBaseTalentPoints() - previousBasePoints),
                Mathf.Max(0, characterClass.GetClassTalentPoints() - previousClassPoints));
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
                    Debug.Log($"[Drop] Rolled item drop ID {drop.itemID} from {template.name}.");
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
                    Debug.Log($"[Drop] Rolled money drop {drop.money.goldP}g {drop.money.silverP}s {drop.money.copperP}c from {template.name}.");
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

        private readonly struct LevelRewardFeedback
        {
            public LevelRewardFeedback(int levelsGained, int baseTalentPointsGained, int classTalentPointsGained)
            {
                LevelsGained = levelsGained;
                BaseTalentPointsGained = baseTalentPointsGained;
                ClassTalentPointsGained = classTalentPointsGained;
            }

            public int LevelsGained { get; }
            public int BaseTalentPointsGained { get; }
            public int ClassTalentPointsGained { get; }
        }
    }
}
