using System;
using System.Collections.Generic;

namespace IdleOff.Mobs
{
    [Serializable]
    public sealed class MobTemplate
    {
        public int mobID;
        public string name;
        public string description;
        public List<string> tags = new();
        public MobType mobType = MobType.Basic;
        public int level = 1;
        public float maxHp = 1f;
        public float damage;
        public float defense;
        public float ac = 1f;
        public float moveSpeed = 1f;
        public float aggroRange;
        public float attackRange = 1f;
        public int basicActionID;
        public int xpReward;
        public List<MobItemDrop> itemDrops = new();
        public List<MobMoneyDrop> moneyDrops = new();

        public bool HasTag(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) && tags != null && tags.Contains(tag);
        }

        public MobTemplate Clone()
        {
            return new MobTemplate
            {
                mobID = mobID,
                name = name,
                description = description,
                tags = tags == null ? new List<string>() : new List<string>(tags),
                mobType = mobType,
                level = level,
                maxHp = maxHp,
                damage = damage,
                defense = defense,
                ac = ac,
                moveSpeed = moveSpeed,
                aggroRange = aggroRange,
                attackRange = attackRange,
                basicActionID = basicActionID,
                xpReward = xpReward,
                itemDrops = CloneItemDrops(),
                moneyDrops = CloneMoneyDrops()
            };
        }

        private List<MobItemDrop> CloneItemDrops()
        {
            var clones = new List<MobItemDrop>();
            if (itemDrops == null)
            {
                return clones;
            }

            foreach (var drop in itemDrops)
            {
                if (drop == null)
                {
                    continue;
                }

                clones.Add(new MobItemDrop { itemID = drop.itemID, chance = drop.chance });
            }

            return clones;
        }

        private List<MobMoneyDrop> CloneMoneyDrops()
        {
            var clones = new List<MobMoneyDrop>();
            if (moneyDrops == null)
            {
                return clones;
            }

            foreach (var drop in moneyDrops)
            {
                if (drop == null)
                {
                    continue;
                }

                clones.Add(new MobMoneyDrop { money = drop.money, chance = drop.chance });
            }

            return clones;
        }
    }
}
