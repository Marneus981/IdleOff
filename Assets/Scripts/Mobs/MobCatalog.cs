using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using IdleOff.Profiles;
using UnityEngine;

namespace IdleOff.Mobs
{
    public static class MobCatalog
    {
        [DataContract]
#pragma warning disable CS0649
        private sealed class MobValues
        {
            [DataMember(IsRequired = false)]
            public string name;
            [DataMember(IsRequired = false)]
            public string description;
            [DataMember(IsRequired = false)]
            public List<string> tags;
            [DataMember(IsRequired = false)]
            public string mobType;
            [DataMember(IsRequired = false)]
            public int level;
            [DataMember(IsRequired = false)]
            public float maxHp;
            [DataMember(IsRequired = false)]
            public float damage;
            [DataMember(IsRequired = false)]
            public float defense;
            [DataMember(IsRequired = false)]
            public float ac;
            [DataMember(IsRequired = false)]
            public float moveSpeed;
            [DataMember(IsRequired = false)]
            public float aggroRange;
            [DataMember(IsRequired = false)]
            public float attackRange;
            [DataMember(IsRequired = false)]
            public int basicActionID;
            [DataMember(IsRequired = false)]
            public List<int> bossPatternIDs;
            [DataMember(IsRequired = false)]
            public int xpReward;
            [DataMember(IsRequired = false)]
            public List<MobItemDropValues> itemDrops;
            [DataMember(IsRequired = false)]
            public List<MobMoneyDropValues> moneyDrops;
        }

        [DataContract]
        private sealed class MobItemDropValues
        {
            [DataMember(IsRequired = false)]
            public int itemID;
            [DataMember(IsRequired = false)]
            public float chance;
        }

        [DataContract]
        private sealed class MobMoneyDropValues
        {
            [DataMember(IsRequired = false)]
            public List<int> money;
            [DataMember(IsRequired = false)]
            public float chance;
        }
#pragma warning restore CS0649

        private const string MobsPath = "Assets/Tables/Mobs.json";

        public static Dictionary<int, MobTemplate> Mobs { get; private set; } = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadMobsBeforeScene()
        {
            LoadMobs();
        }

        public static void EnsureLoaded()
        {
            if (Mobs == null || Mobs.Count == 0)
            {
                LoadMobs();
            }
        }

        public static void LoadMobs()
        {
            var resolvedPath = ResolveMobsPath(MobsPath);
            using var stream = File.OpenRead(resolvedPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, MobValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            var serializedMobs = (Dictionary<string, MobValues>)serializer.ReadObject(stream);

            Mobs = new Dictionary<int, MobTemplate>();
            foreach (var entry in serializedMobs)
            {
                if (!int.TryParse(entry.Key, out var mobID))
                {
                    throw new FormatException($"Mob table key '{entry.Key}' is not a valid mob ID.");
                }

                var mob = CreateMob(mobID, entry.Value);
                if (Mobs.ContainsKey(mobID))
                {
                    throw new InvalidOperationException($"Mob table contains duplicate mob ID {mobID}.");
                }

                Mobs.Add(mobID, mob);
            }
        }

        private static MobTemplate CreateMob(int mobID, MobValues values)
        {
            if (mobID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(mobID), mobID, "Mob ID must be positive.");
            }

            return new MobTemplate
            {
                mobID = mobID,
                name = values.name,
                description = values.description,
                tags = values.tags ?? new List<string>(),
                mobType = ParseMobType(values.mobType),
                level = Mathf.Max(1, values.level),
                maxHp = Mathf.Max(1f, values.maxHp),
                damage = Mathf.Max(0f, values.damage),
                defense = Mathf.Max(0f, values.defense),
                ac = Mathf.Max(1f, values.ac),
                moveSpeed = Mathf.Max(0f, values.moveSpeed),
                aggroRange = Mathf.Max(0f, values.aggroRange),
                attackRange = Mathf.Max(0f, values.attackRange),
                basicActionID = values.basicActionID,
                bossPatternIDs = values.bossPatternIDs ?? new List<int>(),
                xpReward = Mathf.Max(0, values.xpReward),
                itemDrops = CreateItemDrops(values.itemDrops),
                moneyDrops = CreateMoneyDrops(values.moneyDrops)
            };
        }

        private static MobType ParseMobType(string value)
        {
            return string.IsNullOrWhiteSpace(value) || !Enum.TryParse(value, true, out MobType parsed)
                ? MobType.Basic
                : parsed;
        }

        private static List<MobItemDrop> CreateItemDrops(List<MobItemDropValues> values)
        {
            var drops = new List<MobItemDrop>();
            if (values == null)
            {
                return drops;
            }

            foreach (var value in values)
            {
                if (value.itemID <= 0 || value.chance <= 0f)
                {
                    continue;
                }

                drops.Add(new MobItemDrop { itemID = value.itemID, chance = value.chance });
            }

            return drops;
        }

        private static List<MobMoneyDrop> CreateMoneyDrops(List<MobMoneyDropValues> values)
        {
            var drops = new List<MobMoneyDrop>();
            if (values == null)
            {
                return drops;
            }

            foreach (var value in values)
            {
                var money = CreateMoney(value.money);
                if (money.TotalCopper <= 0 || value.chance <= 0f)
                {
                    continue;
                }

                drops.Add(new MobMoneyDrop { money = money, chance = value.chance });
            }

            return drops;
        }

        private static Money CreateMoney(List<int> values)
        {
            if (values == null || values.Count == 0)
            {
                return new Money();
            }

            if (values.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(values), values.Count, "Money drops must contain gold, silver, and copper values.");
            }

            return new Money(values[0], values[1], values[2]);
        }

        private static string ResolveMobsPath(string mobsPath)
        {
            if (Path.IsPathRooted(mobsPath))
            {
                return mobsPath;
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), mobsPath));
        }
    }
}
