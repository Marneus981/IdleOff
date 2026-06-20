using System.Collections.Generic;
using System.IO;
using IdleOff.Actions;
using IdleOff.Combat;
using IdleOff.Drops;
using IdleOff.Interactables;
using IdleOff.Maps;
using IdleOff.Mobs;
using IdleOff.Profiles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GameAction = IdleOff.Actions.Action;

public sealed class Phase47RuntimeSystemTests
{
    [SetUp]
    public void SetUp()
    {
        LogAssert.ignoreFailingMessages = true;
        GlobalModifierCatalog.LoadGlobalModifiers();
        GlobalItemCatalog.LoadItems();
        ActionCatalog.LoadGlobalActions();
        MobCatalog.LoadMobs();
        MapCatalog.LoadMaps();
        InteractableObjectCatalog.LoadInteractables();
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;
        DestroyObjectsOfType<MeleeActionHitbox>();
        DestroyObjectsNamed("World Drop");
        DestroyObjectsNamed("Money Drop");
        DestroyObjectsNamed("Item Drop");
    }

    [Test]
    public void MobActionController_ReturnsFalse_WhenTargetIsNull()
    {
        var mobObject = CreateMobObject("Null Target Mob", 9901, 10f, 1.25f);
        try
        {
            var controller = mobObject.GetComponent<MobActionController>();

            Assert.IsFalse(controller.IsTargetInRange(null));
            Assert.IsFalse(controller.TryAttack(null));
        }
        finally
        {
            Object.DestroyImmediate(mobObject);
        }
    }

    [Test]
    public void MobActionController_ReturnsFalse_WhenTargetOutOfRange()
    {
        var player = CreatePlayerContext("Out Of Range Target");
        var mobObject = CreateMobObject("Out Of Range Mob", 9902, 10f, 1.25f);
        try
        {
            mobObject.transform.position = Vector3.zero;
            player.PlayerObject.transform.position = new Vector3(10f, 0f, 0f);
            var controller = mobObject.GetComponent<MobActionController>();

            Assert.IsFalse(controller.IsTargetInRange(player.PlayerObject.transform));
            Assert.IsFalse(controller.TryAttack(player.PlayerObject.transform));
        }
        finally
        {
            Object.DestroyImmediate(mobObject);
            DestroyPlayerContext(player);
        }
    }

    [Test]
    public void MobActionController_ReturnsFalse_WhenTargetIsDead()
    {
        var playerObject = new GameObject("Dead Target");
        var deadPlayer = playerObject.AddComponent<PlayerCombatant>();
        var mobObject = CreateMobObject("Dead Target Mob", 9903, 10f, 1.25f);
        try
        {
            mobObject.transform.position = Vector3.zero;
            playerObject.transform.position = new Vector3(0.5f, 0f, 0f);

            Assert.IsFalse(deadPlayer.IsAlive);
            Assert.IsFalse(mobObject.GetComponent<MobActionController>().TryAttack(playerObject.transform));
        }
        finally
        {
            Object.DestroyImmediate(mobObject);
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void MobActionController_UsesBasicAction_WhenTargetInRange()
    {
        var player = CreatePlayerContext("Basic Action Target");
        var mobObject = CreateMobObject("Basic Action Mob", 9904, 10f, 1.25f);
        try
        {
            mobObject.transform.position = Vector3.zero;
            player.PlayerObject.transform.position = new Vector3(0.5f, 0f, 0f);

            var controller = mobObject.GetComponent<MobActionController>();
            var actionController = mobObject.GetComponent<ActionController>();
            var basicAction = mobObject.GetComponent<MobEntity>().GetBasicAction();

            Assert.IsTrue(controller.TryAttack(player.PlayerObject.transform));
            Assert.Greater(actionController.GetCooldownRemaining(basicAction), 0f);
        }
        finally
        {
            Object.DestroyImmediate(mobObject);
            DestroyPlayerContext(player);
        }
    }

    [Test]
    public void MobBasicAI_SetsTarget_WhenDamagedByPlayer()
    {
        var player = CreatePlayerContext("Aggro Player");
        var mobObject = CreateMobObject("Basic AI Mob", 9905, 10f, 1.25f, addBasicAI: false);
        try
        {
            var ai = mobObject.AddComponent<ProbeMobBasicAI>();
            InvokeUnityMessage(ai, "Awake");
            InvokeUnityMessage(ai, "OnEnable");

            mobObject.GetComponent<MobEntity>().ReceiveDamage(new DamageResult(player.Player, mobObject.GetComponent<MobEntity>(), true, 1f, 1f, 1f, 0));

            Assert.AreSame(player.PlayerObject.transform, ai.CurrentTarget);

            InvokeUnityMessage(ai, "OnDisable");
        }
        finally
        {
            Object.DestroyImmediate(mobObject);
            DestroyPlayerContext(player);
        }
    }

    [Test]
    public void MobBossAI_DoesNotMove_WhenTargetAcquired()
    {
        var player = CreatePlayerContext("Boss Target");
        var mobObject = CreateMobObject("Stationary Boss", 9906, 10f, 8f, MobType.Boss, 12f);
        try
        {
            player.PlayerObject.AddComponent<BoxCollider2D>();
            player.PlayerObject.transform.position = new Vector3(1f, 0f, 0f);
            mobObject.transform.position = Vector3.zero;
            Physics2D.SyncTransforms();

            var boss = mobObject.AddComponent<MobBossAI>();
            InvokeUnityMessage(boss, "Awake");
            var start = mobObject.transform.position;

            InvokeUnityMessage(boss, "Update");
            InvokeUnityMessage(boss, "Update");

            Assert.IsTrue(boss.HasTarget);
            Assert.AreEqual(start, mobObject.transform.position);
        }
        finally
        {
            Object.DestroyImmediate(mobObject);
            DestroyPlayerContext(player);
        }
    }

    [Test]
    public void MobAttack_RespectsCooldown()
    {
        var player = CreatePlayerContext("Cooldown Target");
        var mobObject = CreateMobObject("Cooldown Mob", 9907, 10f, 1.25f);
        try
        {
            player.PlayerObject.transform.position = new Vector3(0.5f, 0f, 0f);
            var mobAction = mobObject.GetComponent<MobActionController>();
            var actionController = mobObject.GetComponent<ActionController>();

            Assert.IsTrue(mobAction.TryAttack(player.PlayerObject.transform));
            Assert.IsFalse(mobAction.TryAttack(player.PlayerObject.transform));

            actionController.Tick(1.19f);
            Assert.IsFalse(mobAction.TryAttack(player.PlayerObject.transform));

            actionController.Tick(0.01f);
            Assert.IsTrue(mobAction.TryAttack(player.PlayerObject.transform));
        }
        finally
        {
            Object.DestroyImmediate(mobObject);
            DestroyPlayerContext(player);
        }
    }

    [Test]
    public void CombatHealth_Reset_ClampsMaxToAtLeastOne()
    {
        var health = new CombatHealth();

        health.Reset(0f);

        Assert.AreEqual(1f, health.Max);
        Assert.AreEqual(1f, health.Current);
    }

    [Test]
    public void CombatHealth_TakeDamage_DoesNotGoBelowZero()
    {
        var health = new CombatHealth();
        health.Reset(5f);

        health.TakeDamage(99f);

        Assert.AreEqual(0f, health.Current);
    }

    [Test]
    public void CombatHealth_Heal_DoesNotExceedMax()
    {
        var health = new CombatHealth();
        health.Reset(5f);
        health.TakeDamage(2f);

        health.Heal(99f);

        Assert.AreEqual(5f, health.Current);
    }

    [Test]
    public void CombatHealth_Heal_DoesNothing_WhenDead()
    {
        var health = new CombatHealth();
        health.Reset(5f);
        health.TakeDamage(5f);

        health.Heal(1f);

        Assert.AreEqual(0f, health.Current);
    }

    [Test]
    public void PlayerCombatant_RegeneratesOnePercentPerSecond()
    {
        var context = CreatePlayerContext("Exact Regen Tester");
        try
        {
            var maxHp = context.Player.MaxHp;
            context.Player.ReceiveDamage(new DamageResult(null, context.Player, true, 1f, maxHp * 0.5f, maxHp * 0.5f, 0));

            var before = context.Player.CurrentHp;
            context.Player.TickHealthRegen(1f);

            Assert.AreEqual(before + maxHp * 0.01f, context.Player.CurrentHp, 0.001f);
        }
        finally
        {
            DestroyPlayerContext(context);
        }
    }

    [Test]
    public void PlayerCombatant_RespawnsAtCurrentMapSpawn_WhenKilled()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            Assert.IsTrue(context.Manager.TryGetCurrentSpawnPosition(out var spawn));
            context.PlayerObject.transform.position = new Vector3(8f, 8f, 0f);

            context.Player.ReceiveDamage(new DamageResult(null, context.Player, true, 1f, context.Player.MaxHp * 5f, context.Player.MaxHp * 5f, 0));

            Assert.AreEqual(spawn.x, context.PlayerObject.transform.position.x, 0.001f);
            Assert.AreEqual(spawn.y, context.PlayerObject.transform.position.y, 0.001f);
            Assert.AreEqual(context.Player.MaxHp, context.Player.CurrentHp);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void PlayerCombatant_DoesNotCrash_WhenKilledWithoutMapManager()
    {
        var context = CreatePlayerContext("No Manager Death Tester");
        try
        {
            Assert.DoesNotThrow(() => context.Player.ReceiveDamage(new DamageResult(null, context.Player, true, 1f, context.Player.MaxHp * 5f, context.Player.MaxHp * 5f, 0)));
            Assert.AreEqual(context.Player.MaxHp, context.Player.CurrentHp);
        }
        finally
        {
            DestroyPlayerContext(context);
        }
    }

    [Test]
    public void Phase4_CombatHealth_ClampsMaxDamageAndHealing()
    {
        var health = new CombatHealth();

        health.Reset(0f);
        Assert.AreEqual(1f, health.Max);
        Assert.AreEqual(1f, health.Current);

        health.TakeDamage(0.25f);
        Assert.AreEqual(0.75f, health.Current);

        health.Heal(10f);
        Assert.AreEqual(1f, health.Current);

        health.TakeDamage(50f);
        Assert.AreEqual(0f, health.Current);
        Assert.IsFalse(health.IsAlive);

        health.Heal(1f);
        Assert.AreEqual(0f, health.Current);
    }

    [Test]
    public void Phase4_PlayerCombatant_TickHealthRegenHealsOnePercentPerSecond()
    {
        var context = CreatePlayerContext("Regen Tester");
        try
        {
            var maxHp = context.Player.MaxHp;
            context.Player.ReceiveDamage(new DamageResult(null, context.Player, true, 1f, maxHp * 0.5f, maxHp * 0.5f, 0));

            var damagedHp = context.Player.CurrentHp;
            context.Player.TickHealthRegen(1f);

            Assert.AreEqual(damagedHp + maxHp * 0.01f, context.Player.CurrentHp, 0.001f);
        }
        finally
        {
            DestroyPlayerContext(context);
        }
    }

    [Test]
    public void Phase4_PlayerCombatant_DeathWithoutMapManagerStillRestoresFullHp()
    {
        var context = CreatePlayerContext("Death Tester");
        try
        {
            context.PlayerObject.transform.position = new Vector3(9f, 3f, 0f);

            context.Player.ReceiveDamage(new DamageResult(null, context.Player, true, 1f, context.Player.MaxHp * 5f, context.Player.MaxHp * 5f, 0));

            Assert.AreEqual(context.Player.MaxHp, context.Player.CurrentHp);
            Assert.AreEqual(new Vector3(9f, 3f, 0f), context.PlayerObject.transform.position);
        }
        finally
        {
            DestroyPlayerContext(context);
        }
    }

    [Test]
    public void Phase5_ActionController_CooldownRequiresExplicitTickBeforeReuse()
    {
        var context = CreatePlayerContext("Action Tester");
        try
        {
            var controller = context.PlayerObject.AddComponent<ActionController>();
            InvokeUnityMessage(controller, "Awake");
            var action = TestAction(91001, 2f);

            Assert.IsTrue(controller.TryUseAction(action, Vector2.right));
            Assert.Greater(controller.GetCooldownRemaining(action), 0f);
            Assert.IsFalse(controller.TryUseAction(action, Vector2.right));

            controller.Tick(1.99f);
            Assert.IsFalse(controller.TryUseAction(action, Vector2.right));

            controller.Tick(0.01f);
            Assert.IsTrue(controller.TryUseAction(action, Vector2.right));
        }
        finally
        {
            DestroyPlayerContext(context);
        }
    }

    [Test]
    public void Phase5_MobActionController_RejectsOutOfRangeAndUsesCooldownInRange()
    {
        var player = CreatePlayerContext("Target Tester");
        var mobObject = new GameObject("Mob Action Tester");
        try
        {
            var mob = mobObject.AddComponent<MobEntity>();
            mob.Initialize(new MobTemplate
            {
                mobID = 9901,
                name = "Test Attacker",
                maxHp = 10f,
                damage = 1f,
                attackRange = 1.25f,
                basicActionID = 7001
            });
            var actionController = mobObject.AddComponent<ActionController>();
            var mobActionController = mobObject.AddComponent<MobActionController>();
            InvokeUnityMessage(actionController, "Awake");
            InvokeUnityMessage(mobActionController, "Awake");

            mobObject.transform.position = Vector3.zero;
            player.PlayerObject.transform.position = new Vector3(5f, 0f, 0f);
            Assert.IsFalse(mobActionController.IsTargetInRange(player.PlayerObject.transform));
            Assert.IsFalse(mobActionController.TryAttack(player.PlayerObject.transform));

            player.PlayerObject.transform.position = new Vector3(0.5f, 0f, 0f);
            Assert.IsTrue(mobActionController.IsTargetInRange(player.PlayerObject.transform));
            Assert.IsTrue(mobActionController.TryAttack(player.PlayerObject.transform));
            Assert.IsFalse(mobActionController.TryAttack(player.PlayerObject.transform));
        }
        finally
        {
            Object.DestroyImmediate(mobObject);
            DestroyPlayerContext(player);
        }
    }

    [Test]
    public void MobSpawner_InitializesAndSpawnsMaxActiveMobs()
    {
        var spawnerObject = new GameObject("Max Active Spawner");
        try
        {
            var spawner = spawnerObject.AddComponent<MobSpawner>();
            spawner.Initialize(new MapMobSpawnerDefinition { mobID = 6001, maxActive = 2, respawnSeconds = 5f }, CreateSprite(Color.red));

            Assert.AreEqual(2, spawner.ActiveCount);
        }
        finally
        {
            Object.DestroyImmediate(spawnerObject);
        }
    }

    [Test]
    public void MobSpawner_DoesNotSpawnAboveMaxActive()
    {
        var spawnerObject = new GameObject("No Over Spawn Spawner");
        try
        {
            var spawner = spawnerObject.AddComponent<MobSpawner>();
            spawner.Initialize(new MapMobSpawnerDefinition { mobID = 6001, maxActive = 1, respawnSeconds = 0f }, CreateSprite(Color.red));

            spawner.Tick(999f);

            Assert.AreEqual(1, spawner.ActiveCount);
        }
        finally
        {
            Object.DestroyImmediate(spawnerObject);
        }
    }

    [Test]
    public void MobSpawner_RecordsMobDeathThroughMapManager()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            var spawner = Object.FindFirstObjectByType<MobSpawner>();
            Assert.IsNotNull(spawner);

            spawner.ActiveMobs[0].ReceiveDamage(new DamageResult(null, spawner.ActiveMobs[0], true, 1f, 999f, 999f, 0));

            Assert.AreEqual(1, context.Manager.CurrentRuntimeState.TotalMobKills);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void MobSpawner_RespawnsAfterRespawnTimer()
    {
        var spawnerObject = new GameObject("Respawn Timer Spawner");
        try
        {
            var spawner = spawnerObject.AddComponent<MobSpawner>();
            spawner.Initialize(new MapMobSpawnerDefinition { mobID = 6001, maxActive = 1, respawnSeconds = 2f }, CreateSprite(Color.red));
            spawner.ActiveMobs[0].ReceiveDamage(new DamageResult(null, spawner.ActiveMobs[0], true, 1f, 999f, 999f, 0));

            spawner.Tick(1.99f);
            Assert.AreEqual(0, spawner.ActiveCount);

            spawner.Tick(0.01f);
            Assert.AreEqual(1, spawner.ActiveCount);
        }
        finally
        {
            Object.DestroyImmediate(spawnerObject);
        }
    }

    [Test]
    public void MobSpawner_ThrowsForUnknownMobID()
    {
        var spawnerObject = new GameObject("Unknown Mob Spawner");
        try
        {
            var spawner = spawnerObject.AddComponent<MobSpawner>();

            Assert.Throws<KeyNotFoundException>(() => spawner.Initialize(new MapMobSpawnerDefinition { mobID = 999999, maxActive = 1 }, null));
        }
        finally
        {
            Object.DestroyImmediate(spawnerObject);
        }
    }

    [Test]
    public void MapManager_SpawnsMobSpawnersFromMapDefinition()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);

            var spawners = Object.FindObjectsByType<MobSpawner>(FindObjectsSortMode.None);

            Assert.AreEqual(1, spawners.Length);
            Assert.AreEqual(1, spawners[0].ActiveCount);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void Phase6_MobSpawner_InitializesMaxActiveAndRespawnsAfterDeath()
    {
        var spawnerObject = new GameObject("Mob Spawner Test");
        try
        {
            var spawner = spawnerObject.AddComponent<MobSpawner>();
            spawner.Initialize(new MapMobSpawnerDefinition
            {
                mobID = 6001,
                maxActive = 2,
                respawnSeconds = 5f
            }, CreateSprite(Color.red));

            Assert.AreEqual(2, spawner.ActiveCount);

            var firstMob = spawner.ActiveMobs[0];
            firstMob.ReceiveDamage(new DamageResult(null, firstMob, true, 1f, 9999f, 9999f, 0));

            Assert.AreEqual(1, spawner.ActiveCount);
            spawner.Tick(4.99f);
            Assert.AreEqual(1, spawner.ActiveCount);

            spawner.Tick(0.02f);
            Assert.AreEqual(2, spawner.ActiveCount);
        }
        finally
        {
            Object.DestroyImmediate(spawnerObject);
        }
    }

    [Test]
    public void WorldDrop_ItemPickup_AddsItemAndDestroysDrop()
    {
        var character = CreateCharacter("Item Pickup Tester");
        var dropObject = new GameObject("Item Pickup Drop");
        var drop = dropObject.AddComponent<WorldDrop>();

        drop.Initialize(WorldDropPayload.Item(5019, 3));

        Assert.IsTrue(drop.TryCollect(character));
        Assert.AreEqual(3, character.Inventory.GetItemQuantity(5019));
        Assert.IsTrue(drop == null);
    }

    [Test]
    public void WorldDrop_MoneyPickup_AddsMoneyAndDestroysDrop()
    {
        var character = CreateCharacter("Exact Money Pickup Tester");
        var dropObject = new GameObject("Exact Money Pickup Drop");
        var drop = dropObject.AddComponent<WorldDrop>();

        drop.Initialize(WorldDropPayload.Money(new Money(0, 2, 5)));

        Assert.IsTrue(drop.TryCollect(character));
        Assert.AreEqual(205, character.Inventory.Money.TotalCopper);
        Assert.IsTrue(drop == null);
    }

    [Test]
    public void WorldDrop_ItemPickup_WhenInventoryFull_LeavesDropInWorld()
    {
        var character = CreateCharacter("Full Inventory Tester");
        var dropObject = new GameObject("Full Inventory Drop");
        var drop = dropObject.AddComponent<WorldDrop>();
        try
        {
            Assert.IsTrue(character.AddItem(5019, 1980));
            drop.Initialize(WorldDropPayload.Item(5019, 1));

            Assert.IsFalse(drop.TryCollect(character));
            Assert.AreEqual(1980, character.Inventory.GetItemQuantity(5019));
            Assert.AreEqual(1, drop.Payload.quantity);
            Assert.IsFalse(drop == null);
        }
        finally
        {
            if (dropObject != null)
            {
                Object.DestroyImmediate(dropObject);
            }
        }
    }

    [Test]
    public void WorldDrop_ItemPickup_WhenPartiallyCollected_UpdatesLeftoverQuantity()
    {
        var character = CreateCharacter("Exact Partial Pickup Tester");
        var dropObject = new GameObject("Exact Partial Item Drop");
        var drop = dropObject.AddComponent<WorldDrop>();
        try
        {
            Assert.IsTrue(character.AddItem(5019, 1979));
            drop.Initialize(WorldDropPayload.Item(5019, 5));

            Assert.IsFalse(drop.TryCollect(character));

            Assert.AreEqual(1980, character.Inventory.GetItemQuantity(5019));
            Assert.AreEqual(4, drop.Payload.quantity);
            Assert.IsFalse(drop == null);
        }
        finally
        {
            if (dropObject != null)
            {
                Object.DestroyImmediate(dropObject);
            }
        }
    }

    [Test]
    public void WorldDropSpawner_SpawnDrop_NotifiesMapManager()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);

            WorldDropSpawner.Instance.SpawnDrop(WorldDropPayload.Item(5019, 1), Vector3.zero);

            Assert.AreEqual(1, context.Manager.CurrentRuntimeState.presentPickups.Count);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void MapManager_TracksSpawnedDropsInCurrentMapState()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);

            WorldDropSpawner.Instance.SpawnDrop(WorldDropPayload.Money(new Money(0, 0, 7)), new Vector3(1f, 0f, 0f));

            Assert.AreEqual(1, context.Manager.CurrentRuntimeState.presentPickups.Count);
            Assert.IsTrue(context.Manager.CurrentRuntimeState.presentPickups[0].isMoney);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void MapManager_RemovesCollectedDropFromCurrentMapState()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            var drop = WorldDropSpawner.Instance.SpawnDrop(WorldDropPayload.Item(5019, 1), Vector3.zero);

            Assert.IsTrue(drop.TryCollect(context.Character));

            Assert.AreEqual(0, context.Manager.CurrentRuntimeState.presentPickups.Count);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void MapManager_LoadsPersistedPickups()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            WorldDropSpawner.Instance.SpawnDrop(WorldDropPayload.Item(5019, 2), Vector3.zero);
            context.Manager.SaveCurrentMapState();

            context.Manager.LoadMap(1001);

            Assert.AreEqual(1, context.Manager.CurrentRuntimeState.presentPickups.Count);
            Assert.AreEqual(1, Object.FindObjectsByType<WorldDrop>(FindObjectsSortMode.None).Length);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void WorldDropSpawner_SpawnDrop_WithNotifyFalse_DoesNotDuplicateSavedPickup()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            WorldDropSpawner.Instance.SpawnDrop(WorldDropPayload.Item(5019, 1), Vector3.zero);
            context.Manager.SaveCurrentMapState();

            context.Manager.LoadMap(1001);

            Assert.AreEqual(1, context.Manager.CurrentRuntimeState.presentPickups.Count);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void Phase7_WorldDrop_PartialItemPickupLeavesRemainderOnGround()
    {
        var character = CreateCharacter("Partial Pickup Tester");
        var dropObject = new GameObject("Partial Item Drop");
        var drop = dropObject.AddComponent<WorldDrop>();
        try
        {
            Assert.IsTrue(character.AddItem(5019, 1979));
            Assert.AreEqual(1979, character.Inventory.GetItemQuantity(5019));

            drop.Initialize(WorldDropPayload.Item(5019, 5));

            Assert.IsFalse(drop.TryCollect(character));
            Assert.AreEqual(1980, character.Inventory.GetItemQuantity(5019));
            Assert.AreEqual(4, drop.Payload.quantity);
            Assert.IsFalse(drop == null);
        }
        finally
        {
            if (dropObject != null)
            {
                Object.DestroyImmediate(dropObject);
            }
        }
    }

    [Test]
    public void Phase7_WorldDrop_MoneyPickupAddsMoneyAndDestroysDrop()
    {
        var character = CreateCharacter("Money Pickup Tester");
        var dropObject = new GameObject("Money Pickup Drop");
        var drop = dropObject.AddComponent<WorldDrop>();

        drop.Initialize(WorldDropPayload.Money(new Money(0, 1, 25)));

        Assert.IsTrue(drop.TryCollect(character));
        Assert.AreEqual(125, character.Inventory.Money.TotalCopper);
        Assert.IsTrue(drop == null);
    }

    [Test]
    public void Phase7_WorldDropSpawner_DestroyClearsStaticInstance()
    {
        var spawnerObject = new GameObject("Drop Spawner Instance Test");
        var spawner = spawnerObject.AddComponent<WorldDropSpawner>();
        InvokeUnityMessage(spawner, "Awake");

        Assert.AreSame(spawner, WorldDropSpawner.Instance);

        Object.DestroyImmediate(spawnerObject);

        Assert.IsTrue(WorldDropSpawner.Instance == null);
    }

    private static GameAction TestAction(int actionID, float cooldown)
    {
        return new GameAction
        {
            actionID = actionID,
            name = "Test Action",
            cooldown = cooldown,
            range = 0.25f,
            attackScaling = 1f,
            hitboxType = ActionHitboxType.Box
        };
    }

    private static GameObject CreateMobObject(
        string name,
        int mobID,
        float maxHp,
        float attackRange,
        MobType mobType = MobType.Basic,
        float aggroRange = 0f,
        bool addBasicAI = true)
    {
        var mobObject = new GameObject(name);
        var mob = mobObject.AddComponent<MobEntity>();
        mob.Initialize(new MobTemplate
        {
            mobID = mobID,
            name = name,
            maxHp = maxHp,
            damage = 1f,
            attackRange = attackRange,
            aggroRange = aggroRange,
            mobType = mobType,
            basicActionID = mobType == MobType.Boss ? 7002 : 7001
        });
        var actionController = mobObject.AddComponent<ActionController>();
        var mobActionController = mobObject.AddComponent<MobActionController>();
        InvokeUnityMessage(actionController, "Awake");
        InvokeUnityMessage(mobActionController, "Awake");
        if (addBasicAI)
        {
            mobObject.AddComponent<Rigidbody2D>();
        }

        return mobObject;
    }

    private static MapContext CreateMapContext()
    {
        var playerContext = CreatePlayerContext("Phase Map Tester");
        DeleteMapStateFile(playerContext.Character.CharacterID);

        var dropSpawnerObject = new GameObject("Phase Test Drop Spawner");
        var dropSpawner = dropSpawnerObject.AddComponent<WorldDropSpawner>();
        dropSpawner.Configure(CreateSprite(Color.blue), CreateSprite(Color.yellow));
        InvokeUnityMessage(dropSpawner, "Awake");

        var managerObject = new GameObject("Phase Test Map Manager");
        var manager = managerObject.AddComponent<MapManager>();
        manager.Configure(playerContext.Profile, 1001, CreateSprite(Color.gray), CreateSprite(Color.yellow), CreateSprite(Color.red), CreateSprite(Color.gray), CreateSprite(Color.green));
        InvokeUnityMessage(manager, "Awake");

        return new MapContext(playerContext, managerObject, manager, dropSpawnerObject);
    }

    private static PlayerContext CreatePlayerContext(string name)
    {
        var profile = ScriptableObject.CreateInstance<CharacterProfile>();
        var character = new CharacterData(name, CharacterGender.Unspecified, 1);
        Assert.IsTrue(profile.TryAddCharacter(character));

        var playerObject = new GameObject(name);
        var player = playerObject.AddComponent<PlayerCombatant>();
        player.SetProfile(profile);

        return new PlayerContext(profile, character, playerObject, player);
    }

    private static void DestroyMapContext(MapContext context)
    {
        DeleteMapStateFile(context.Character.CharacterID);
        Object.DestroyImmediate(context.ManagerObject);
        Object.DestroyImmediate(context.DropSpawnerObject);
        DestroyPlayerContext(context.PlayerContext);
        DestroyObjectsNamedPrefix("MapRoot - ");
        DestroyObjectsNamed("World Drop");
        DestroyObjectsNamed("Money Drop");
        DestroyObjectsNamed("Item Drop");
    }

    private static CharacterData CreateCharacter(string name)
    {
        var profile = ScriptableObject.CreateInstance<CharacterProfile>();
        var character = new CharacterData(name, CharacterGender.Unspecified, 1);
        Assert.IsTrue(profile.TryAddCharacter(character));
        Object.DestroyImmediate(profile);
        return character;
    }

    private static void DestroyPlayerContext(PlayerContext context)
    {
        Object.DestroyImmediate(context.Profile);
        Object.DestroyImmediate(context.PlayerObject);
    }

    private static Sprite CreateSprite(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private static void InvokeUnityMessage(Object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method?.Invoke(target, null);
    }

    private static void DestroyObjectsOfType<T>() where T : Object
    {
        foreach (var target in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            if (target is Component component)
            {
                Object.DestroyImmediate(component.gameObject);
                continue;
            }

            Object.DestroyImmediate(target);
        }
    }

    private static void DestroyObjectsNamed(string objectName)
    {
        foreach (var gameObject in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (gameObject == null)
            {
                continue;
            }

            if (gameObject.name == objectName)
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }

    private static void DestroyObjectsNamedPrefix(string prefix)
    {
        foreach (var gameObject in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (gameObject == null)
            {
                continue;
            }

            if (gameObject.name.StartsWith(prefix))
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }

    private static void DeleteMapStateFile(string characterID)
    {
        var path = Path.Combine(Application.persistentDataPath, "IdleOff", "MapStates", characterID + ".json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private sealed class ProbeMobBasicAI : MobBasicAI
    {
        public Transform CurrentTarget => target;
    }

    private readonly struct PlayerContext
    {
        public PlayerContext(CharacterProfile profile, CharacterData character, GameObject playerObject, PlayerCombatant player)
        {
            Profile = profile;
            Character = character;
            PlayerObject = playerObject;
            Player = player;
        }

        public CharacterProfile Profile { get; }
        public CharacterData Character { get; }
        public GameObject PlayerObject { get; }
        public PlayerCombatant Player { get; }
    }

    private readonly struct MapContext
    {
        public MapContext(PlayerContext playerContext, GameObject managerObject, MapManager manager, GameObject dropSpawnerObject)
        {
            PlayerContext = playerContext;
            ManagerObject = managerObject;
            Manager = manager;
            DropSpawnerObject = dropSpawnerObject;
        }

        public PlayerContext PlayerContext { get; }
        public GameObject ManagerObject { get; }
        public MapManager Manager { get; }
        public GameObject DropSpawnerObject { get; }
        public CharacterData Character => PlayerContext.Character;
        public GameObject PlayerObject => PlayerContext.PlayerObject;
        public PlayerCombatant Player => PlayerContext.Player;
    }
}
