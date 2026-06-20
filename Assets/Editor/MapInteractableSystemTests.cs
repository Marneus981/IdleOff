using System.IO;
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

public sealed class MapInteractableSystemTests
{
    [SetUp]
    public void SetUp()
    {
        LogAssert.ignoreFailingMessages = true;
        GlobalModifierCatalog.LoadGlobalModifiers();
        GlobalItemCatalog.LoadItems();
        MobCatalog.LoadMobs();
        MapCatalog.LoadMaps();
        InteractableObjectCatalog.LoadInteractables();
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;
    }

    [Test]
    public void MapCatalog_LoadsDefinitions()
    {
        Assert.IsTrue(MapCatalog.Maps.ContainsKey(1001));
        Assert.IsTrue(MapCatalog.Maps.ContainsKey(1002));

        var map = MapCatalog.Maps[1001];
        Assert.AreEqual("Training Grounds", map.name);
        Assert.AreEqual("player_start", map.playerSpawnAnchor);
        Assert.AreEqual(2, map.layout.platforms.Count);
        Assert.AreEqual(2, map.layout.ladders.Count);
        Assert.AreEqual(1, map.interactables.Count);
        Assert.AreEqual(1, map.mobSpawners.Count);
    }

    [Test]
    public void LayoutAnchors_ResolveThroughMapManager()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);

            Assert.IsTrue(context.Manager.TryGetAnchor("player_start", out var playerStart));
            Assert.AreEqual(new Vector2(-3f, -1.65f), playerStart);

            Assert.IsTrue(context.Manager.TryGetAnchor("exit_portal", out var portalAnchor));
            Assert.AreEqual(new Vector2(3.75f, -1.75f), portalAnchor);
        }
        finally
        {
            DestroyContext(context);
        }
    }

    [Test]
    public void InteractableObjectCatalog_LoadsPortal()
    {
        Assert.IsTrue(InteractableObjectCatalog.Interactables.ContainsKey(8001));

        var portal = InteractableObjectCatalog.Interactables[8001];
        Assert.AreEqual(InteractableObjectType.Portal, portal.type);
        Assert.AreEqual(InteractConditionType.MobKills, portal.condition.type);
        Assert.AreEqual(1, portal.condition.requiredAmount);
        Assert.AreEqual(InteractEffectType.TravelToMap, portal.effect.type);
        Assert.AreEqual(1002, portal.effect.targetMapID);
    }

    [Test]
    public void MobKillCondition_OpensPortal()
    {
        var condition = new InteractCondition { type = InteractConditionType.MobKills, requiredAmount = 1 };
        var mapState = new MapRuntimeState { mapID = 1001 };
        var playerContext = CreatePlayerContext();
        try
        {
            Assert.IsFalse(InteractConditionResolver.IsMet(condition, playerContext.Player, mapState));

            mapState.RecordMobKilled(new MobTemplate { mobID = 6001, name = "Test Mob", maxHp = 1f, mobType = MobType.Basic });

            Assert.IsTrue(InteractConditionResolver.IsMet(condition, playerContext.Player, mapState));
        }
        finally
        {
            DestroyPlayerContext(playerContext);
        }
    }

    [Test]
    public void BossKeyCondition_ConsumesItem5021()
    {
        var condition = new InteractCondition
        {
            type = InteractConditionType.HasItem,
            itemID = 5021,
            consumeItem = true
        };
        var playerContext = CreatePlayerContext();
        try
        {
            Assert.IsFalse(InteractConditionResolver.IsMet(condition, playerContext.Player, new MapRuntimeState { mapID = 1001 }));

            Assert.IsTrue(playerContext.Character.AddItem(5021, 1));

            Assert.IsTrue(InteractConditionResolver.IsMet(condition, playerContext.Player, new MapRuntimeState { mapID = 1001 }));
            Assert.IsTrue(InteractConditionResolver.TryConsumeCost(condition, playerContext.Player));
            Assert.AreEqual(0, playerContext.Character.Inventory.GetItemQuantity(5021));
        }
        finally
        {
            DestroyPlayerContext(playerContext);
        }
    }

    [Test]
    public void BossDefeatedCondition_ChecksSpecificBossMobID()
    {
        var state = new MapRuntimeState { mapID = 1001 };
        state.RecordMobKilled(new MobTemplate { mobID = 6002, name = "Not The Boss", maxHp = 1f, mobType = MobType.Boss });

        Assert.IsFalse(state.HasBossDefeated(6003));

        state.RecordMobKilled(new MobTemplate { mobID = 6003, name = "The Boss", maxHp = 1f, mobType = MobType.Boss });

        Assert.IsTrue(state.HasBossDefeated(6003));
    }

    [Test]
    public void MapRuntimeState_SavesAndLoadsByCharacterID()
    {
        var character = new CharacterData("Map Save Tester", CharacterGender.Unspecified, 1);
        DeleteMapStateFile(character.CharacterID);
        try
        {
            var store = new CharacterMapStateStore(character);
            var state = store.GetOrCreateMapState(1001);
            state.RecordMobKilled(new MobTemplate { mobID = 6003, name = "Boss", maxHp = 1f, mobType = MobType.Boss });
            state.MarkInteractableUnlocked("training_exit_portal");
            store.Save();

            var loadedStore = new CharacterMapStateStore(character);
            var loadedState = loadedStore.GetOrCreateMapState(1001);

            Assert.AreEqual(1, loadedState.TotalMobKills);
            Assert.IsTrue(loadedState.HasBossDefeated(6003));
            Assert.IsTrue(loadedState.IsInteractableUnlocked("training_exit_portal"));
        }
        finally
        {
            DeleteMapStateFile(character.CharacterID);
        }
    }

    [Test]
    public void PortalEffect_ChangesCurrentMap()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            context.Manager.CurrentRuntimeState.RecordMobKilled(new MobTemplate { mobID = 6001, name = "Test Mob", maxHp = 1f, mobType = MobType.Basic });

            var portal = Object.FindFirstObjectByType<InteractableObjectEntity>();
            Assert.IsNotNull(portal);

            context.PlayerObject.transform.position = portal.transform.position;

            Assert.IsTrue(portal.TryInteract(context.Player));
            Assert.AreEqual(1002, context.Manager.CurrentMap.mapID);
        }
        finally
        {
            DestroyContext(context);
        }
    }

    [Test]
    public void MapManager_RecordMobKilled_PersistsTotalKillsAndBosses()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            var mobObject = new GameObject("Boss Mob");
            var mob = mobObject.AddComponent<MobEntity>();
            mob.Initialize(new MobTemplate { mobID = 6003, name = "Boss", maxHp = 1f, mobType = MobType.Boss });

            context.Manager.RecordMobKilled(mob);

            Assert.AreEqual(1, context.Manager.CurrentRuntimeState.TotalMobKills);
            Assert.IsTrue(context.Manager.CurrentRuntimeState.HasBossDefeated(6003));

            Object.DestroyImmediate(mobObject);
        }
        finally
        {
            DestroyContext(context);
        }
    }

    [Test]
    public void PlayerInteractDriver_SelectsNearestInteractableInRange()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            context.Manager.CurrentRuntimeState.RecordMobKilled(new MobTemplate { mobID = 6001, name = "Test Mob", maxHp = 1f, mobType = MobType.Basic });
            var portal = Object.FindFirstObjectByType<InteractableObjectEntity>();
            Assert.IsNotNull(portal);

            context.PlayerObject.transform.position = portal.transform.position;

            Assert.IsTrue(portal.CanInteract(context.Player));
        }
        finally
        {
            DestroyContext(context);
        }
    }

    private static MapContext CreateMapContext()
    {
        var playerContext = CreatePlayerContext();
        DeleteMapStateFile(playerContext.Character.CharacterID);

        var managerObject = new GameObject("Map Manager Test");
        var manager = managerObject.AddComponent<MapManager>();
        manager.Configure(playerContext.Profile, 1001, CreateSprite(Color.gray), CreateSprite(Color.yellow), CreateSprite(Color.red), CreateSprite(Color.gray), CreateSprite(Color.green));
        InvokeUnityMessage(manager, "Awake");

        var dropSpawnerObject = new GameObject("World Drop Spawner Test");
        dropSpawnerObject.AddComponent<WorldDropSpawner>();
        InvokeUnityMessage(dropSpawnerObject.GetComponent<WorldDropSpawner>(), "Awake");

        return new MapContext(playerContext, managerObject, manager, dropSpawnerObject);
    }

    private static PlayerContext CreatePlayerContext()
    {
        var profile = ScriptableObject.CreateInstance<CharacterProfile>();
        var character = new CharacterData("Map Tester", CharacterGender.Unspecified, 1);
        Assert.IsTrue(profile.TryAddCharacter(character));

        var playerObject = new GameObject("Test Player");
        var player = playerObject.AddComponent<PlayerCombatant>();
        player.SetProfile(profile);
        playerObject.AddComponent<PlayerInteractDriver>();

        return new PlayerContext(profile, character, playerObject, player);
    }

    private static void DestroyContext(MapContext context)
    {
        DeleteMapStateFile(context.Character.CharacterID);
        DestroyPlayerContext(context.PlayerContext);
        Object.DestroyImmediate(context.ManagerObject);
        Object.DestroyImmediate(context.DropSpawnerObject);
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
