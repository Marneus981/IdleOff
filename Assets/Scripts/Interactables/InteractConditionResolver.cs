using IdleOff.Combat;
using IdleOff.Maps;

namespace IdleOff.Interactables
{
    public static class InteractConditionResolver
    {
        public static bool IsMet(InteractCondition condition, PlayerCombatant player, MapRuntimeState mapState)
        {
            if (condition == null || condition.type == InteractConditionType.None)
            {
                return true;
            }

            return condition.type switch
            {
                InteractConditionType.MobKills => mapState != null && mapState.TotalMobKills >= condition.requiredAmount,
                InteractConditionType.HasItem => player?.Character != null && player.Character.Inventory.GetItemQuantity(condition.itemID) > 0,
                InteractConditionType.BossDefeated => mapState != null && mapState.HasBossDefeated(condition.bossMobID),
                _ => false
            };
        }

        public static bool TryConsumeCost(InteractCondition condition, PlayerCombatant player)
        {
            if (condition == null || condition.type != InteractConditionType.HasItem || !condition.consumeItem)
            {
                return true;
            }

            return player?.Character != null
                && player.Character.Inventory.TryRemoveItem(condition.itemID, 1, out _);
        }
    }
}
