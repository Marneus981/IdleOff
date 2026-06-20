using System.Collections.Generic;
using IdleOff.Actions;
using IdleOff.Combat;
using IdleOff.Drops;
using IdleOff.Mobs;
using IdleOff.Profiles;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using GameAction = IdleOff.Actions.Action;

public sealed class CombatSystemTests
{
    [SetUp]
    public void SetUp()
    {
        LogAssert.ignoreFailingMessages = true;
        GlobalModifierCatalog.LoadGlobalModifiers();
        GlobalItemCatalog.LoadItems();
        ActionCatalog.LoadGlobalActions();
        MobCatalog.LoadMobs();
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;
    }

    [Test]
    public void PlayerHitChance_ComparesAccuracyAgainstMobAC()
    {
        var player = new FakeCombatant { accuracy = 4f };
        var mob = new FakeMobCombatant { ac = 5f };

        Assert.AreEqual(0.8f, CombatResolver.CalculatePlayerHitChance(player, mob));

        player.accuracy = 8f;
        Assert.AreEqual(1f, CombatResolver.CalculatePlayerHitChance(player, mob));
    }

    [Test]
    public void PlayerAction_MissesWhenAccuracyRollFails()
    {
        var player = new FakeCombatant { accuracy = 4f, damage = 100f, mastery = 0.35f };
        var mob = new FakeMobCombatant { ac = 5f };
        var random = new FakeRandomSource(0.9f);

        var result = CombatResolver.ResolvePlayerAction(player, mob, BasicAction(), random);

        Assert.IsFalse(result.Hit);
        Assert.AreEqual(0.8f, result.HitChance);
        Assert.AreEqual(25f, mob.RuntimeData.CurrentHp);
    }

    [Test]
    public void PlayerDamageRange_RespectsMasteryMinimum()
    {
        var player = new FakeCombatant { accuracy = 100f, damage = 100f, mastery = 0.35f };
        var mob = new FakeMobCombatant { ac = 1f };
        var action = BasicAction(2f);
        var random = new FakeRandomSource(new[] { 0f }, 70f);

        var result = CombatResolver.ResolvePlayerAction(player, mob, action, random);

        Assert.IsTrue(result.Hit);
        Assert.AreEqual(70f, result.RawDamage);
        Assert.AreEqual(70f, result.FinalDamage);
    }

    [Test]
    public void CritChanceAboveOneHundredPercent_CanDoubleCrit()
    {
        var player = new FakeCombatant
        {
            accuracy = 100f,
            damage = 100f,
            mastery = 1f,
            critChance = 1.5f,
            critDamage = 0.5f
        };
        var mob = new FakeMobCombatant { ac = 1f };
        var random = new FakeRandomSource(new[] { 0f, 0.25f }, 100f);

        var result = CombatResolver.ResolvePlayerAction(player, mob, BasicAction(), random);

        Assert.AreEqual(2, result.CritCount);
        Assert.AreEqual(225f, result.RawDamage);
        Assert.AreEqual(225f, result.FinalDamage);
    }

    [Test]
    public void BossDamage_OnlyAppliesToBossTaggedOrBossTypeMobs()
    {
        var player = new FakeCombatant
        {
            accuracy = 100f,
            damage = 100f,
            mastery = 1f,
            bossDamage = 0.5f
        };
        var normalMob = new FakeMobCombatant { ac = 1f };
        var bossTaggedMob = new FakeMobCombatant
        {
            ac = 1f,
            template = new MobTemplate { mobID = 2, name = "Tagged Boss", maxHp = 25f, tags = new List<string> { "boss" } }
        };
        var bossMob = new FakeMobCombatant { ac = 1f, mobType = MobType.Boss };

        var normalResult = CombatResolver.ResolvePlayerAction(player, normalMob, BasicAction(), new FakeRandomSource(new[] { 0f }, 100f));
        var taggedResult = CombatResolver.ResolvePlayerAction(player, bossTaggedMob, BasicAction(), new FakeRandomSource(new[] { 0f }, 100f));
        var bossResult = CombatResolver.ResolvePlayerAction(player, bossMob, BasicAction(), new FakeRandomSource(new[] { 0f }, 100f));

        Assert.AreEqual(100f, normalResult.FinalDamage);
        Assert.AreEqual(150f, taggedResult.FinalDamage);
        Assert.AreEqual(150f, bossResult.FinalDamage);
    }

    [Test]
    public void Defense_ReducesFinalDamageButNotBelowOne()
    {
        var player = new FakeCombatant { accuracy = 100f, damage = 10f, mastery = 1f };
        var mob = new FakeMobCombatant { ac = 1f, defense = 100f };

        var result = CombatResolver.ResolvePlayerAction(player, mob, BasicAction(), new FakeRandomSource(new[] { 0f }, 10f));

        Assert.AreEqual(1f, result.FinalDamage);
    }

    [Test]
    public void MobDamageFormula_IgnoresAccuracy()
    {
        var mob = new FakeMobCombatant { damage = 20f };
        var target = new FakeCombatant { accuracy = 0f, defense = 5f };

        var result = CombatResolver.ResolveMobAction(mob, target, BasicAction(2f));

        Assert.IsTrue(result.Hit);
        Assert.AreEqual(40f, result.RawDamage);
        Assert.AreEqual(35f, result.FinalDamage);
    }

    [Test]
    public void ClassXPRate_IncreasesXpReward()
    {
        var context = CreatePlayerContext(3004);
        try
        {
            var mob = new FakeMobCombatant { template = new MobTemplate { mobID = 9001, name = "XP Mob", maxHp = 1f, xpReward = 50 } };

            var reward = RewardResolver.ResolveMobDeath(context.Player, mob, new FakeRandomSource(0f));
            var expectedXp = Mathf.RoundToInt(mob.RuntimeData.Template.xpReward * (1f + context.Character.GetStatValueByID(CombatStatIDs.ClassXPRate)));

            Assert.AreEqual(expectedXp, reward.XpReward);
            Assert.AreEqual(expectedXp, context.Character.CharacterClass.GetCurrentXP());
        }
        finally
        {
            Object.DestroyImmediate(context.Profile);
            Object.DestroyImmediate(context.GameObject);
        }
    }

    [Test]
    public void DropRateOverOneHundredPercent_CanProduceMultipleDrops()
    {
        var context = CreatePlayerContext(3005);
        try
        {
            var template = new MobTemplate
            {
                mobID = 9002,
                name = "Drop Mob",
                maxHp = 1f,
                itemDrops = new List<MobItemDrop> { new() { itemID = 5019, chance = 1f } }
            };
            var mob = new FakeMobCombatant { template = template };

            var reward = RewardResolver.ResolveMobDeath(context.Player, mob, new FakeRandomSource(0.05f));

            Assert.AreEqual(2, reward.Drops.Count);
            Assert.IsTrue(reward.Drops.TrueForAll(drop => !drop.isMoney && drop.itemID == 5019));
        }
        finally
        {
            Object.DestroyImmediate(context.Profile);
            Object.DestroyImmediate(context.GameObject);
        }
    }

    [Test]
    public void MobCatalog_LoadsTemplatesCorrectly()
    {
        Assert.IsTrue(MobCatalog.Mobs.ContainsKey(6001));
        Assert.IsTrue(MobCatalog.Mobs.ContainsKey(6002));
        Assert.IsTrue(MobCatalog.Mobs.ContainsKey(6003));

        var boss = MobCatalog.Mobs[6003];
        Assert.AreEqual(MobType.Boss, boss.mobType);
        Assert.AreEqual(7002, boss.basicActionID);
        Assert.Contains("boss", boss.tags);
    }

    [Test]
    public void ActionCatalog_LoadsDefinitionsCorrectly()
    {
        Assert.IsTrue(ActionCatalog.MobActions.ContainsKey(7001));
        Assert.IsTrue(ActionCatalog.MobActions.ContainsKey(7002));

        var classActions = ActionCatalog.LoadActions("Assets/Tables/ActionsWanderingSoul.json", "test action");
        Assert.IsTrue(classActions.ContainsKey(2001));
        Assert.AreEqual(ActionOwnerType.Player, classActions[2001].ownerType);
        Assert.AreEqual(ActionHitboxType.Box, classActions[2001].hitboxType);
    }

    [Test]
    public void MoneyDrops_ResolveCorrectly()
    {
        var context = CreatePlayerContext(3005);
        try
        {
            var template = new MobTemplate
            {
                mobID = 9003,
                name = "Money Mob",
                maxHp = 1f,
                moneyDrops = new List<MobMoneyDrop> { new() { money = new Money(0, 1, 25), chance = 1f } }
            };
            var mob = new FakeMobCombatant { template = template };

            var reward = RewardResolver.ResolveMobDeath(context.Player, mob, new FakeRandomSource(0.5f));

            Assert.AreEqual(1, reward.Drops.Count);
            Assert.IsTrue(reward.Drops[0].isMoney);
            Assert.AreEqual(new Money(0, 1, 25).TotalCopper, reward.Drops[0].money.TotalCopper);
        }
        finally
        {
            Object.DestroyImmediate(context.Profile);
            Object.DestroyImmediate(context.GameObject);
        }
    }

    private static GameAction BasicAction(float scaling = 1f)
    {
        return new GameAction
        {
            actionID = 1,
            name = "Test Action",
            level = 1,
            maxLevel = 1,
            attackScaling = scaling
        };
    }

    private static PlayerContext CreatePlayerContext(int starSignModifierID)
    {
        var profile = ScriptableObject.CreateInstance<CharacterProfile>();
        var character = new CharacterData("Tester", CharacterGender.Unspecified, 1, starSignModifierID);
        Assert.IsTrue(profile.TryAddCharacter(character));

        var gameObject = new GameObject("Test Player");
        var player = gameObject.AddComponent<PlayerCombatant>();
        player.SetProfile(profile);

        return new PlayerContext(profile, character, gameObject, player);
    }

    private readonly struct PlayerContext
    {
        public PlayerContext(CharacterProfile profile, CharacterData character, GameObject gameObject, PlayerCombatant player)
        {
            Profile = profile;
            Character = character;
            GameObject = gameObject;
            Player = player;
        }

        public CharacterProfile Profile { get; }
        public CharacterData Character { get; }
        public GameObject GameObject { get; }
        public PlayerCombatant Player { get; }
    }

    private sealed class FakeRandomSource : IRandomSource
    {
        private readonly Queue<float> values = new();
        private readonly float rangeValue;

        public FakeRandomSource(float value)
            : this(new[] { value }, value)
        {
        }

        public FakeRandomSource(IEnumerable<float> values, float rangeValue = 0f)
        {
            foreach (var value in values)
            {
                this.values.Enqueue(value);
            }

            this.rangeValue = rangeValue;
        }

        public float Value => values.Count > 0 ? values.Dequeue() : 0f;

        public float Range(float minInclusive, float maxInclusive)
        {
            return Mathf.Clamp(rangeValue, minInclusive, maxInclusive);
        }
    }

    private class FakeCombatant : ICombatant
    {
        public float accuracy;
        public float mastery;
        public float defense;
        public float damage;
        public float critChance;
        public float critDamage;
        public float bossDamage;

        public string DisplayName => "Fake Combatant";
        public bool IsAlive { get; set; } = true;
        public bool IsPlayerControlled { get; set; } = true;
        public virtual IReadOnlyList<string> Tags { get; set; } = new List<string>();
        public DamageResult LastDamageResult { get; private set; }

        public virtual float GetStatValueByID(int statID)
        {
            return statID switch
            {
                CombatStatIDs.Accuracy => accuracy,
                CombatStatIDs.Mastery => mastery,
                CombatStatIDs.Defense => defense,
                CombatStatIDs.Damage => damage,
                CombatStatIDs.CritChance => critChance,
                CombatStatIDs.CritDamage => critDamage,
                CombatStatIDs.BossDamage => bossDamage,
                _ => 0f
            };
        }

        public virtual void ReceiveDamage(DamageResult result)
        {
            LastDamageResult = result;
        }
    }

    private sealed class FakeMobCombatant : FakeCombatant, IMobCombatant
    {
        public float ac = 1f;
        public MobType mobType = MobType.Basic;
        public MobTemplate template;
        private MobRuntimeData runtimeData;

        public int MobID => Template.mobID;
        public MobType MobType => Template.mobType;
        public float AC => ac;
        public MobRuntimeData RuntimeData => runtimeData ??= new MobRuntimeData(Template);
        public override IReadOnlyList<string> Tags
        {
            get => Template.tags;
            set => Template.tags = value == null ? new List<string>() : new List<string>(value);
        }

        public MobTemplate Template => template ??= new MobTemplate
        {
            mobID = 1,
            name = "Fake Mob",
            tags = new List<string>(),
            mobType = mobType,
            maxHp = 25f,
            damage = damage,
            defense = defense,
            ac = ac
        };

        public override float GetStatValueByID(int statID)
        {
            return statID switch
            {
                CombatStatIDs.Defense => defense,
                CombatStatIDs.Damage => damage,
                _ => base.GetStatValueByID(statID)
            };
        }

        public override void ReceiveDamage(DamageResult result)
        {
            base.ReceiveDamage(result);
            RuntimeData.ReceiveDamage(result.FinalDamage);
        }
    }
}
