using System.IO;
using IdleOff.Actions;
using IdleOff.Combat;
using IdleOff.Drops;
using IdleOff.Interactables;
using IdleOff.Maps;
using IdleOff.Mobs;
using IdleOff.Player;
using IdleOff.Profiles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CrossSystemRuntimeTests
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
        DestroyObjectsNamedPrefix("MapRoot - ");
        DestroyObjectsOfType<MeleeActionHitbox>();
    }

    [Test]
    public void PlayerKillsMob_RecordsKill_GeneratesDrops_GrantsXp()
    {
        var context = CreateMapContext();
        var mobObject = new GameObject("Guaranteed Reward Mob");
        try
        {
            context.Manager.LoadMap(1001);
            var mob = mobObject.AddComponent<MobEntity>();
            mob.Initialize(new MobTemplate
            {
                mobID = 99001,
                name = "Guaranteed Reward Mob",
                maxHp = 5f,
                xpReward = 25,
                basicActionID = 7001,
                itemDrops = new()
                {
                    new MobItemDrop { itemID = 5019, chance = 1f }
                },
                moneyDrops = new()
                {
                    new MobMoneyDrop { money = new Money(0, 0, 9), chance = 1f }
                }
            });
            mob.Died += context.Manager.RecordMobKilled;
            var startingXp = context.Character.CharacterClass.GetCurrentXP();

            mob.ReceiveDamage(new DamageResult(context.Player, mob, true, 1f, 999f, 999f, 0));

            Assert.AreEqual(1, context.Manager.CurrentRuntimeState.TotalMobKills);
            Assert.Greater(context.Character.CharacterClass.GetCurrentXP(), startingXp);
            Assert.AreEqual(2, context.Manager.CurrentRuntimeState.presentPickups.Count);
            Assert.AreEqual(2, Object.FindObjectsByType<WorldDrop>(FindObjectsSortMode.None).Length);
        }
        finally
        {
            if (mobObject != null)
            {
                Object.DestroyImmediate(mobObject);
            }

            DestroyMapContext(context);
        }
    }

    [Test]
    public void PortalOpens_AfterSpawnerMobKilled()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            var portal = Object.FindFirstObjectByType<InteractableObjectEntity>();
            var spawner = Object.FindFirstObjectByType<MobSpawner>();
            Assert.IsNotNull(portal);
            Assert.IsNotNull(spawner);
            Assert.IsFalse(portal.CanInteract(context.Player));

            spawner.ActiveMobs[0].ReceiveDamage(new DamageResult(null, spawner.ActiveMobs[0], true, 1f, 999f, 999f, 0));
            Assert.IsTrue(InteractConditionResolver.IsMet(portal.Definition.condition, context.Player, context.Manager.CurrentRuntimeState));
            context.PlayerObject.transform.position = portal.transform.position;
            Assert.IsTrue(portal.CanInteract(context.Player));
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void RespawnedMob_DoesNotDoubleCountPreviousDeath()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            var spawner = Object.FindFirstObjectByType<MobSpawner>();
            Assert.IsNotNull(spawner);

            spawner.ActiveMobs[0].ReceiveDamage(new DamageResult(null, spawner.ActiveMobs[0], true, 1f, 999f, 999f, 0));
            Assert.AreEqual(1, context.Manager.CurrentRuntimeState.TotalMobKills);

            spawner.Tick(5f);
            Assert.AreEqual(1, context.Manager.CurrentRuntimeState.TotalMobKills);

            spawner.ActiveMobs[0].ReceiveDamage(new DamageResult(null, spawner.ActiveMobs[0], true, 1f, 999f, 999f, 0));
            Assert.AreEqual(2, context.Manager.CurrentRuntimeState.TotalMobKills);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void MapTransition_ClearsPreviousMapSpawnersAndDrops()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            var oldSpawner = Object.FindFirstObjectByType<MobSpawner>();
            var drop = WorldDropSpawner.Instance.SpawnDrop(WorldDropPayload.Item(5019, 1), Vector3.zero);
            Assert.IsNotNull(oldSpawner);
            Assert.IsNotNull(drop);

            context.Manager.LoadMap(1002);

            Assert.IsTrue(oldSpawner == null);
            Assert.AreEqual(1002, context.Manager.CurrentMap.mapID);
            Assert.IsNotNull(Object.FindFirstObjectByType<InteractableObjectEntity>());
            Assert.AreEqual(0, Object.FindObjectsByType<WorldDrop>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(Object.FindFirstObjectByType<MobSpawner>());
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void SavedMapState_PreservesKillsAndPickupsAcrossReload()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            context.Manager.CurrentRuntimeState.RecordMobKilled(new MobTemplate { mobID = 6001, name = "Training Mob", maxHp = 1f, mobType = MobType.Basic });
            WorldDropSpawner.Instance.SpawnDrop(WorldDropPayload.Item(5019, 2), Vector3.zero);
            context.Manager.SaveCurrentMapState();

            context.Manager.LoadMap(1002);
            context.Manager.LoadMap(1001);

            Assert.AreEqual(1, context.Manager.CurrentRuntimeState.TotalMobKills);
            Assert.AreEqual(1, context.Manager.CurrentRuntimeState.presentPickups.Count);
            Assert.AreEqual(1, Object.FindObjectsByType<WorldDrop>(FindObjectsSortMode.None).Length);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void MapLoad_UsesBoundaryAnchorsForVoidThresholdAndBoundaryObjects()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);

            Assert.IsTrue(context.Manager.TryGetAnchor("boundary_floor", out var floorAnchor));
            Assert.AreEqual(-7.5f, floorAnchor.y);
            Assert.AreEqual(-10f, context.Manager.CurrentVoidRespawnY.Value, 0.001f);

            var floor = GameObject.Find("Boundary Floor");
            var leftWall = GameObject.Find("Boundary Left Wall");
            var rightWall = GameObject.Find("Boundary Right Wall");
            var ceiling = GameObject.Find("Boundary Ceiling");

            Assert.IsNotNull(floor);
            Assert.IsNotNull(leftWall);
            Assert.IsNotNull(rightWall);
            Assert.IsNotNull(ceiling);
            Assert.AreEqual(-7.5f, floor.transform.position.y, 0.001f);
            Assert.IsNull(leftWall.GetComponent<SpriteRenderer>());
            Assert.IsNotNull(leftWall.GetComponent<BoxCollider2D>());
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void SpawnedWorldDrop_IsTrackedByCurrentMapStateAndRemovedOnPickup()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            Assert.AreEqual(0, context.Manager.CurrentRuntimeState.presentPickups.Count);

            var drop = WorldDropSpawner.Instance.SpawnDrop(WorldDropPayload.Item(5019, 1), Vector3.zero);

            Assert.AreEqual(1, context.Manager.CurrentRuntimeState.presentPickups.Count);

            Assert.IsTrue(drop.TryCollect(context.Character));

            Assert.AreEqual(0, context.Manager.CurrentRuntimeState.presentPickups.Count);
            Assert.AreEqual(1, context.Character.Inventory.GetItemQuantity(5019));
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void SpawnedWorldDrop_WithNotifyFalseDoesNotEnterMapRuntimeState()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);

            var drop = WorldDropSpawner.Instance.SpawnDrop(WorldDropPayload.Item(5019, 1), Vector3.zero, false);

            Assert.AreEqual(0, context.Manager.CurrentRuntimeState.presentPickups.Count);
            Object.DestroyImmediate(drop.gameObject);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void MobDeath_FromLoadedSpawnerRecordsKillAndRespawnsLater()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            var spawner = Object.FindFirstObjectByType<MobSpawner>();
            Assert.IsNotNull(spawner);
            Assert.AreEqual(5, spawner.ActiveCount);

            var mob = spawner.ActiveMobs[0];
            mob.ReceiveDamage(new DamageResult(context.Player, mob, true, 1f, 9999f, 9999f, 0));

            Assert.AreEqual(1, context.Manager.CurrentRuntimeState.TotalMobKills);
            Assert.AreEqual(4, spawner.ActiveCount);

            spawner.Tick(10f);

            Assert.AreEqual(5, spawner.ActiveCount);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void PortalUnlock_PersistsAcrossReloadAndAllowsTravel()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            context.Manager.CurrentRuntimeState.RecordMobKilled(new MobTemplate { mobID = 6001, name = "Training Mob", maxHp = 1f, mobType = MobType.Basic });
            context.Manager.SaveCurrentMapState();

            context.Manager.LoadMap(1001);
            var portal = Object.FindFirstObjectByType<InteractableObjectEntity>();
            Assert.IsNotNull(portal);

            context.PlayerObject.transform.position = portal.transform.position;

            Assert.IsTrue(portal.CanInteract(context.Player));
            Assert.IsTrue(portal.TryInteract(context.Player));
            Assert.AreEqual(1002, context.Manager.CurrentMap.mapID);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void PortalTravel_SpawnsAtReversePortal_WhenDestinationLinksBackToSource()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1002);

            context.Manager.LoadMapFromPortal(1001);

            Assert.AreEqual(1001, context.Manager.CurrentMap.mapID);
            Assert.IsTrue(context.Manager.TryGetAnchor("exit_portal", out var reversePortalAnchor));
            Assert.AreEqual(reversePortalAnchor.x, context.PlayerObject.transform.position.x, 0.001f);
            Assert.AreEqual(reversePortalAnchor.y, context.PlayerObject.transform.position.y, 0.001f);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void PlayerDeath_AfterMapTravelRespawnsAtHubSpawnByDefault()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1002);
            context.PlayerObject.transform.position = new Vector3(8f, 8f, 0f);
            context.Player.ReceiveDamage(new DamageResult(null, context.Player, true, 1f, context.Player.MaxHp * 5f, context.Player.MaxHp * 5f, 0));

            Assert.AreEqual(MapManager.HubMapID, context.Manager.CurrentMap.mapID);
            Assert.IsTrue(context.Manager.TryGetCurrentSpawnPosition(out var spawn));
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
    public void MapManager_RemembersLastMapAndPositionAcrossStoreReload()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1002);
            context.PlayerObject.transform.position = new Vector3(2.25f, -1.35f, 0f);
            context.Manager.SaveCurrentMapState();

            var store = new CharacterMapStateStore(context.Character);

            Assert.IsTrue(store.TryGetLastLocation(out var mapID, out var position));
            Assert.AreEqual(1002, mapID);
            Assert.AreEqual(2.25f, position.x, 0.001f);
            Assert.AreEqual(-1.35f, position.y, 0.001f);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void PlayerInteractDriverAndPortal_UseCurrentMapStateCondition()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            var portal = Object.FindFirstObjectByType<InteractableObjectEntity>();
            Assert.IsNotNull(portal);

            context.PlayerObject.transform.position = portal.transform.position;
            Assert.IsTrue(portal.CanInteract(context.Player));

            context.Manager.CurrentRuntimeState.RecordMobKilled(new MobTemplate { mobID = 6001, name = "Training Mob", maxHp = 1f, mobType = MobType.Basic });

            Assert.IsTrue(portal.TryInteract(context.Player));
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    private static MapContext CreateMapContext()
    {
        var playerContext = CreatePlayerContext();
        DeleteMapStateFile(playerContext.Character.CharacterID);

        var dropSpawnerObject = new GameObject("Cross Test Drop Spawner");
        var dropSpawner = dropSpawnerObject.AddComponent<WorldDropSpawner>();
        dropSpawner.Configure(CreateSprite(Color.blue), CreateSprite(Color.yellow));
        InvokeUnityMessage(dropSpawner, "Awake");

        var managerObject = new GameObject("Cross Test Map Manager");
        var manager = managerObject.AddComponent<MapManager>();
        manager.Configure(playerContext.Profile, 1001, CreateSprite(Color.gray), CreateSprite(Color.yellow), CreateSprite(Color.red), CreateSprite(Color.gray), CreateSprite(Color.green));
        InvokeUnityMessage(manager, "Awake");

        return new MapContext(playerContext, managerObject, manager, dropSpawnerObject);
    }

    private static PlayerContext CreatePlayerContext()
    {
        var profile = ScriptableObject.CreateInstance<CharacterProfile>();
        var character = new CharacterData("Cross Tester", CharacterGender.Unspecified, 1);
        Assert.IsTrue(profile.TryAddCharacter(character));

        var playerObject = new GameObject("Cross Test Player");
        var body = playerObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        var player = playerObject.AddComponent<PlayerCombatant>();
        player.SetProfile(profile);
        playerObject.AddComponent<PlayerInteractDriver>();

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

    private static void DestroyPlayerContext(PlayerContext context)
    {
        Object.DestroyImmediate(context.Profile);
        Object.DestroyImmediate(context.PlayerObject);
    }

    private static void DeleteMapStateFile(string characterID)
    {
        var path = Path.Combine(Application.persistentDataPath, "IdleOff", "MapStates", characterID + ".json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
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
