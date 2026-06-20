using System.Collections.Generic;
using System.IO;
using IdleOff.Actions;
using IdleOff.Combat;
using IdleOff.Interactables;
using IdleOff.Maps;
using IdleOff.Mobs;
using IdleOff.Profiles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GameAction = IdleOff.Actions.Action;

public sealed class Phase89ActionPatternTests
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
        BossPatternCatalog.LoadPatterns();
        ActionRuntimeRegistry.ClearAll();
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;
        ActionRuntimeRegistry.ClearAll();
        DestroyObjectsOfType<BossTelegraph>();
        DestroyObjectsNamedPrefix("MapRoot - ");
        DestroyObjectsNamed("Boundary Left Wall");
        DestroyObjectsNamed("Boundary Floor");
        DestroyObjectsNamed("Projectile Target");
        DestroyObjectsNamed("Area Target");
    }

    [Test]
    public void ProjectileActionHitbox_MovesInDirection()
    {
        var projectile = CreateProjectile(TestOwner(), ProjectileAction(), Vector2.zero, Vector2.right);

        projectile.Tick(0.5f);

        Assert.AreEqual(2.5f, projectile.transform.position.x, 0.001f);
    }

    [Test]
    public void ProjectileActionHitbox_DamagesValidTarget()
    {
        var owner = TestOwner();
        var target = CreateMobTarget(new Vector2(1f, 0f), 20f);
        var projectile = CreateProjectile(owner, ProjectileAction(speed: 10f, lifetime: 1f), Vector2.zero, Vector2.right);
        try
        {
            projectile.Tick(0.1f);

            Assert.Less(target.RuntimeData.CurrentHp, 20f);
            Assert.IsTrue(projectile == null);
        }
        finally
        {
            Object.DestroyImmediate(target.gameObject);
        }
    }

    [Test]
    public void ProjectileActionHitbox_IgnoresOwner()
    {
        var ownerObject = new GameObject("Projectile Owner");
        var owner = ownerObject.AddComponent<TestCombatantBehaviour>();
        owner.isPlayerControlled = true;
        ownerObject.AddComponent<BoxCollider2D>();
        var projectile = CreateProjectile(owner, ProjectileAction(speed: 10f), Vector2.zero, Vector2.right);
        try
        {
            ownerObject.transform.position = new Vector3(1f, 0f, 0f);
            Physics2D.SyncTransforms();

            projectile.Tick(0.1f);

            Assert.IsFalse(projectile == null);
            Assert.IsNull(owner.LastDamageResult);
        }
        finally
        {
            if (projectile != null)
            {
                Object.DestroyImmediate(projectile.gameObject);
            }

            Object.DestroyImmediate(ownerObject);
        }
    }

    [Test]
    public void ProjectileActionHitbox_ExpiresAfterLifetime()
    {
        var projectile = CreateProjectile(TestOwner(), ProjectileAction(lifetime: 0.25f), Vector2.zero, Vector2.right);

        projectile.Tick(0.25f);

        Assert.IsTrue(projectile == null);
    }

    [Test]
    public void ProjectileActionHitbox_RespectsPierceBeforeDestroying()
    {
        var first = CreateMobTarget(new Vector2(1f, 0f), 20f);
        var second = CreateMobTarget(new Vector2(2f, 0f), 20f);
        var projectile = CreateProjectile(TestOwner(), ProjectileAction(speed: 10f, pierce: 1), Vector2.zero, Vector2.right);
        try
        {
            projectile.Tick(0.1f);
            Assert.IsFalse(projectile == null);

            projectile.Tick(0.1f);
            Assert.IsTrue(projectile == null);
            Assert.Less(first.RuntimeData.CurrentHp, 20f);
            Assert.Less(second.RuntimeData.CurrentHp, 20f);
        }
        finally
        {
            Object.DestroyImmediate(first.gameObject);
            Object.DestroyImmediate(second.gameObject);
        }
    }

    [Test]
    public void ProjectileActionHitbox_CollidesWithPlatformAndFloorButPassesInvisibleBoundaryWalls()
    {
        var wall = CreateWorldBox("Boundary Left Wall", new Vector2(0.5f, 0f));
        var floor = CreateWorldBox("Boundary Floor", new Vector2(2.5f, 0f));
        var projectileWall = CreateProjectile(TestOwner(), ProjectileAction(speed: 10f, lifetime: 1f), Vector2.zero, Vector2.right);
        var projectileFloor = CreateProjectile(TestOwner(), ProjectileAction(speed: 10f, lifetime: 1f), new Vector2(2.5f, 1f), new Vector2(2.5f, 0f));
        try
        {
            projectileWall.Tick(0.1f);
            projectileFloor.Tick(0.1f);
            Assert.IsFalse(projectileWall == null);
            Assert.IsFalse(projectileFloor == null);

            projectileWall.Tick(0.1f);
            projectileFloor.Tick(0.1f);
            Assert.IsTrue(projectileWall == null);
            Assert.IsTrue(projectileFloor == null);
        }
        finally
        {
            Object.DestroyImmediate(wall);
            Object.DestroyImmediate(floor);
        }
    }

    [Test]
    public void AreaActionHitbox_TelegraphDoesNotDamage()
    {
        var target = CreateMobTarget(Vector2.zero, 20f);
        var area = CreateArea(TestOwner(), AreaAction(delay: 0.5f), Vector2.zero);
        try
        {
            area.Tick(0.49f);

            Assert.AreEqual(20f, target.RuntimeData.CurrentHp);
            Assert.IsFalse(area.IsActive);
        }
        finally
        {
            Object.DestroyImmediate(target.gameObject);
            if (area != null)
            {
                Object.DestroyImmediate(area.gameObject);
            }
        }
    }

    [Test]
    public void AreaActionHitbox_DamagesAfterDelay()
    {
        var target = CreateMobTarget(Vector2.zero, 20f);
        var area = CreateArea(TestOwner(), AreaAction(delay: 0.5f), Vector2.zero);
        try
        {
            area.Tick(0.5f);

            Assert.Less(target.RuntimeData.CurrentHp, 20f);
            Assert.IsTrue(area == null);
        }
        finally
        {
            Object.DestroyImmediate(target.gameObject);
        }
    }

    [Test]
    public void AreaActionHitbox_OneShotOnlyHitsOnce()
    {
        var target = CreateMobTarget(Vector2.zero, 30f);
        var area = CreateArea(TestOwner(), AreaAction(delay: 0f), Vector2.zero);
        try
        {
            area.Tick(0f);
            var afterFirst = target.RuntimeData.CurrentHp;

            Assert.IsTrue(area == null);
            Assert.AreEqual(afterFirst, target.RuntimeData.CurrentHp);
        }
        finally
        {
            Object.DestroyImmediate(target.gameObject);
        }
    }

    [Test]
    public void AreaActionHitbox_RepeatedTicksRespectIntervalAndCanHitSameTargetEachTick()
    {
        var target = CreateMobTarget(Vector2.zero, 50f);
        var area = CreateArea(TestOwner(), AreaAction(delay: 0f, duration: 2f, tickInterval: 0.5f), Vector2.zero);
        try
        {
            area.Tick(0f);
            var afterFirst = target.RuntimeData.CurrentHp;
            area.Tick(0.49f);
            Assert.AreEqual(afterFirst, target.RuntimeData.CurrentHp);

            area.Tick(0.01f);
            Assert.Less(target.RuntimeData.CurrentHp, afterFirst);
        }
        finally
        {
            Object.DestroyImmediate(target.gameObject);
            if (area != null)
            {
                Object.DestroyImmediate(area.gameObject);
            }
        }
    }

    [Test]
    public void MapTransition_ClearsActiveActionHitboxes()
    {
        var context = CreateMapContext();
        try
        {
            context.Manager.LoadMap(1001);
            CreateProjectile(TestOwner(), ProjectileAction(lifetime: 5f), Vector2.zero, Vector2.right);
            Assert.Greater(ActionRuntimeRegistry.ActiveCount, 0);

            context.Manager.LoadMap(1002);

            Assert.AreEqual(0, ActionRuntimeRegistry.ActiveCount);
        }
        finally
        {
            DestroyMapContext(context);
        }
    }

    [Test]
    public void BossPatternCatalog_LoadsDefinitions()
    {
        Assert.IsTrue(BossPatternCatalog.Patterns.ContainsKey(9001));
        Assert.IsTrue(BossPatternCatalog.Patterns.ContainsKey(9002));
        Assert.IsTrue(BossPatternCatalog.Patterns.ContainsKey(9003));
        Assert.AreEqual(6003, BossPatternCatalog.Patterns[9001].bossMobID);
    }

    [Test]
    public void BossPatternExecutor_LoadsPatternsForBossMobID()
    {
        var boss = CreateBossObject();
        try
        {
            var executor = boss.GetComponent<BossPatternExecutor>();

            Assert.AreEqual(3, executor.LoadedPatternCount);
        }
        finally
        {
            Object.DestroyImmediate(boss);
        }
    }

    [Test]
    public void BossPatternStep_SpawnsActionFromBossPosition()
    {
        var boss = CreateBossObject();
        try
        {
            var executor = boss.GetComponent<BossPatternExecutor>();
            var step = new BossPatternStep { actionID = 7002, originMode = BossPatternOriginMode.BossPosition, directionMode = BossPatternDirectionMode.FixedDirection, fixedDirection = Vector2.right };

            Assert.IsTrue(executor.ExecuteStep(step));
            var projectile = Object.FindFirstObjectByType<ProjectileActionHitbox>();

            Assert.IsNotNull(projectile);
            Assert.AreEqual(boss.transform.position.x, projectile.transform.position.x, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(boss);
        }
    }

    [Test]
    public void BossPatternStep_CanSpawnAtTargetPosition()
    {
        var boss = CreateBossObject();
        var target = CreatePlayerContext();
        try
        {
            target.PlayerObject.transform.position = new Vector3(3f, 1f, 0f);
            var executor = boss.GetComponent<BossPatternExecutor>();
            executor.SetTarget(target.PlayerObject.transform);

            Assert.IsTrue(executor.ExecuteStep(new BossPatternStep { actionID = 7003, originMode = BossPatternOriginMode.TargetPosition }));
            var area = Object.FindFirstObjectByType<AreaActionHitbox>();

            Assert.IsNotNull(area);
            Assert.AreEqual(3f, area.transform.position.x, 0.001f);
            Assert.AreEqual(1f, area.transform.position.y, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(boss);
            DestroyPlayerContext(target);
        }
    }

    [Test]
    public void BossPatternStep_CanSpawnAtMapAnchor()
    {
        var context = CreateMapContext();
        var boss = CreateBossObject();
        try
        {
            context.Manager.LoadMap(1001);
            var executor = boss.GetComponent<BossPatternExecutor>();

            Assert.IsTrue(executor.ExecuteStep(new BossPatternStep { actionID = 7003, originMode = BossPatternOriginMode.AnchorID, anchorID = "exit_portal" }));
            var area = Object.FindFirstObjectByType<AreaActionHitbox>();

            Assert.IsNotNull(area);
            Assert.AreEqual(3.75f, area.transform.position.x, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(boss);
            DestroyMapContext(context);
        }
    }

    [Test]
    public void BossTelegraph_AppearsBeforeDamage()
    {
        var boss = CreateBossObject();
        var target = CreatePlayerContext();
        try
        {
            target.PlayerObject.transform.position = new Vector3(1f, 0f, 0f);
            var executor = boss.GetComponent<BossPatternExecutor>();
            executor.SetTarget(target.PlayerObject.transform);

            executor.Tick(0f);

            Assert.IsNotNull(Object.FindFirstObjectByType<BossTelegraph>());
            Assert.AreEqual(0, ActionRuntimeRegistry.ActiveCount);
        }
        finally
        {
            Object.DestroyImmediate(boss);
            DestroyPlayerContext(target);
        }
    }

    [Test]
    public void BossPattern_DamageOccursOnlyAfterTelegraphDelay()
    {
        var boss = CreateBossObject();
        var target = CreatePlayerContext();
        try
        {
            target.PlayerObject.transform.position = new Vector3(1f, 0f, 0f);
            var executor = boss.GetComponent<BossPatternExecutor>();
            executor.SetTarget(target.PlayerObject.transform);

            executor.Tick(0.34f);
            Assert.AreEqual(0, ActionRuntimeRegistry.ActiveCount);

            executor.Tick(0.01f);
            Assert.Greater(ActionRuntimeRegistry.ActiveCount, 0);
        }
        finally
        {
            Object.DestroyImmediate(boss);
            DestroyPlayerContext(target);
        }
    }

    [Test]
    public void BossPattern_ProjectileSpreadCreatesExpectedCount()
    {
        var boss = CreateBossObject();
        var target = CreatePlayerContext();
        try
        {
            target.PlayerObject.transform.position = new Vector3(5f, 0f, 0f);
            var executor = boss.GetComponent<BossPatternExecutor>();
            executor.SetTarget(target.PlayerObject.transform);
            var spread = BossPatternCatalog.Patterns[9002].steps[0];

            Assert.IsTrue(executor.ExecuteStep(spread));

            Assert.AreEqual(5, Object.FindObjectsByType<ProjectileActionHitbox>(FindObjectsSortMode.None).Length);
        }
        finally
        {
            Object.DestroyImmediate(boss);
            DestroyPlayerContext(target);
        }
    }

    [Test]
    public void BossPattern_RepeatedGroundZonesTickAtExpectedInterval()
    {
        var target = CreateMobTarget(Vector2.zero, 80f);
        var area = CreateArea(TestMobOwner(), AreaAction(delay: 0f, duration: 2f, tickInterval: 0.5f), Vector2.zero);
        try
        {
            area.Tick(0f);
            var afterFirst = target.RuntimeData.CurrentHp;
            area.Tick(0.25f);
            Assert.AreEqual(afterFirst, target.RuntimeData.CurrentHp);

            area.Tick(0.25f);
            Assert.Less(target.RuntimeData.CurrentHp, afterFirst);
        }
        finally
        {
            Object.DestroyImmediate(target.gameObject);
            if (area != null)
            {
                Object.DestroyImmediate(area.gameObject);
            }
        }
    }

    [Test]
    public void BossPattern_DoesNotExecuteWhenBossDead()
    {
        var boss = CreateBossObject();
        var target = CreatePlayerContext();
        try
        {
            var mob = boss.GetComponent<MobEntity>();
            mob.ReceiveDamage(new DamageResult(null, mob, true, 1f, 99999f, 99999f, 0));
            var executor = boss.GetComponent<BossPatternExecutor>();
            executor.SetTarget(target.PlayerObject.transform);

            executor.Tick(99f);

            Assert.AreEqual(0, ActionRuntimeRegistry.ActiveCount);
        }
        finally
        {
            if (boss != null)
            {
                Object.DestroyImmediate(boss);
            }

            DestroyPlayerContext(target);
        }
    }

    [Test]
    public void BossPattern_DoesNotCalculateDamageDirectly_HitboxCallsCombatResolver()
    {
        var target = CreateMobTarget(Vector2.zero, 30f);
        var bossOwner = TestMobOwner();
        var area = CreateArea(bossOwner, AreaAction(delay: 0f), Vector2.zero);
        try
        {
            area.Tick(0f);

            Assert.Less(target.RuntimeData.CurrentHp, 30f);
        }
        finally
        {
            Object.DestroyImmediate(target.gameObject);
        }
    }

    [Test]
    public void BossPattern_FinishesStartedPatternButDoesNotStartNewPatternWhenTargetLeavesAggroRange()
    {
        var boss = CreateBossObject();
        var target = CreatePlayerContext();
        try
        {
            target.PlayerObject.transform.position = new Vector3(1f, 0f, 0f);
            var executor = boss.GetComponent<BossPatternExecutor>();
            executor.SetTarget(target.PlayerObject.transform);
            executor.Tick(0f);

            target.PlayerObject.transform.position = new Vector3(100f, 0f, 0f);
            executor.Tick(0.35f);

            Assert.Greater(ActionRuntimeRegistry.ActiveCount, 0);
            ActionRuntimeRegistry.ClearAll();
            executor.Tick(3f);

            Assert.AreEqual(0, ActionRuntimeRegistry.ActiveCount);
        }
        finally
        {
            Object.DestroyImmediate(boss);
            DestroyPlayerContext(target);
        }
    }

    private static ProjectileActionHitbox CreateProjectile(ICombatant owner, GameAction action, Vector2 origin, Vector2 direction)
    {
        var projectileObject = new GameObject("Test Projectile");
        var projectile = projectileObject.AddComponent<ProjectileActionHitbox>();
        projectile.Initialize(new ActionUseRequest(owner, action, origin, direction, ~0));
        return projectile;
    }

    private static AreaActionHitbox CreateArea(ICombatant owner, GameAction action, Vector2 origin)
    {
        var areaObject = new GameObject("Test Area");
        var area = areaObject.AddComponent<AreaActionHitbox>();
        area.Initialize(new ActionUseRequest(owner, action, origin, Vector2.right, ~0));
        return area;
    }

    private static GameAction ProjectileAction(float speed = 5f, float lifetime = 2f, int pierce = 0)
    {
        return new GameAction
        {
            actionID = 8101,
            name = "Test Projectile",
            hitboxType = ActionHitboxType.Projectile,
            projectileSpeed = speed,
            projectileLifetime = lifetime,
            projectilePierceCount = pierce,
            radius = 0.2f,
            attackScaling = 1f
        };
    }

    private static GameAction AreaAction(float delay = 0f, float duration = 0f, float tickInterval = 0f)
    {
        return new GameAction
        {
            actionID = 8102,
            name = "Test Area",
            hitboxType = ActionHitboxType.Area,
            areaDelay = delay,
            telegraphDuration = delay,
            areaDuration = duration,
            areaTickInterval = tickInterval,
            radius = 1f,
            size = Vector2.one * 2f,
            attackScaling = 1f
        };
    }

    private static TestCombatant TestOwner()
    {
        return new TestCombatant { isPlayerControlled = true, accuracy = 100f, damage = 10f, mastery = 1f };
    }

    private static TestMobCombatant TestMobOwner()
    {
        return new TestMobCombatant { damage = 10f };
    }

    private static MobEntity CreateMobTarget(Vector2 position, float hp)
    {
        var targetObject = new GameObject("Projectile Target");
        targetObject.transform.position = position;
        targetObject.AddComponent<BoxCollider2D>();
        var mob = targetObject.AddComponent<MobEntity>();
        mob.Initialize(new MobTemplate { mobID = 99011, name = "Projectile Target", maxHp = hp, ac = 1f, basicActionID = 7001 });
        Physics2D.SyncTransforms();
        return mob;
    }

    private static GameObject CreateWorldBox(string name, Vector2 position)
    {
        var box = new GameObject(name);
        box.transform.position = position;
        box.AddComponent<BoxCollider2D>();
        Physics2D.SyncTransforms();
        return box;
    }

    private static GameObject CreateBossObject()
    {
        var bossObject = new GameObject("Pattern Boss");
        bossObject.transform.position = Vector3.zero;
        var mob = bossObject.AddComponent<MobEntity>();
        mob.Initialize(new MobTemplate
        {
            mobID = 6003,
            name = "Pattern Boss",
            maxHp = 100f,
            damage = 10f,
            ac = 1f,
            attackRange = 8f,
            aggroRange = 12f,
            mobType = MobType.Boss,
            basicActionID = 7002,
            bossPatternIDs = new List<int> { 9001, 9002, 9003 }
        });
        var controller = bossObject.AddComponent<ActionController>();
        var executor = bossObject.AddComponent<BossPatternExecutor>();
        InvokeUnityMessage(controller, "Awake");
        InvokeUnityMessage(executor, "Awake");
        return bossObject;
    }

    private static MapContext CreateMapContext()
    {
        var playerContext = CreatePlayerContext();
        DeleteMapStateFile(playerContext.Character.CharacterID);

        var managerObject = new GameObject("Phase 89 Map Manager");
        var manager = managerObject.AddComponent<MapManager>();
        manager.Configure(playerContext.Profile, 1001, CreateSprite(Color.gray), CreateSprite(Color.yellow), CreateSprite(Color.red), CreateSprite(Color.gray), CreateSprite(Color.green));
        InvokeUnityMessage(manager, "Awake");

        return new MapContext(playerContext, managerObject, manager);
    }

    private static PlayerContext CreatePlayerContext()
    {
        var profile = ScriptableObject.CreateInstance<CharacterProfile>();
        var character = new CharacterData("Phase 89 Tester", CharacterGender.Unspecified, 1);
        Assert.IsTrue(profile.TryAddCharacter(character));

        var playerObject = new GameObject("Phase 89 Player");
        playerObject.AddComponent<BoxCollider2D>();
        var player = playerObject.AddComponent<PlayerCombatant>();
        player.SetProfile(profile);
        return new PlayerContext(profile, character, playerObject, player);
    }

    private static void DestroyMapContext(MapContext context)
    {
        DeleteMapStateFile(context.Character.CharacterID);
        Object.DestroyImmediate(context.ManagerObject);
        DestroyPlayerContext(context.PlayerContext);
        DestroyObjectsNamedPrefix("MapRoot - ");
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

    private static void DeleteMapStateFile(string characterID)
    {
        var path = Path.Combine(Application.persistentDataPath, "IdleOff", "MapStates", characterID + ".json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
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
            if (gameObject != null && gameObject.name == objectName)
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }

    private static void DestroyObjectsNamedPrefix(string prefix)
    {
        foreach (var gameObject in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (gameObject != null && gameObject.name.StartsWith(prefix))
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }

    private class TestCombatant : ICombatant
    {
        public float accuracy;
        public float mastery;
        public float damage;
        public bool isPlayerControlled;
        public DamageResult? LastDamageResult { get; private set; }
        public string DisplayName => "Test Combatant";
        public bool IsAlive { get; set; } = true;
        public bool IsPlayerControlled => isPlayerControlled;
        public IReadOnlyList<string> Tags { get; } = new List<string>();

        public virtual float GetStatValueByID(int statID)
        {
            return statID switch
            {
                CombatStatIDs.Accuracy => accuracy,
                CombatStatIDs.Mastery => mastery,
                CombatStatIDs.Damage => damage,
                _ => 0f
            };
        }

        public void ReceiveDamage(DamageResult result)
        {
            LastDamageResult = result;
        }
    }

    private sealed class TestCombatantBehaviour : MonoBehaviour, ICombatant
    {
        public bool isPlayerControlled;
        public DamageResult? LastDamageResult { get; private set; }
        public string DisplayName => "Test Behaviour Combatant";
        public bool IsAlive { get; set; } = true;
        public bool IsPlayerControlled => isPlayerControlled;
        public IReadOnlyList<string> Tags { get; } = new List<string>();
        public float GetStatValueByID(int statID) => 0f;
        public void ReceiveDamage(DamageResult result) => LastDamageResult = result;
    }

    private sealed class TestMobCombatant : TestCombatant, IMobCombatant
    {
        private readonly MobRuntimeData runtimeData = new(new MobTemplate { mobID = 98001, name = "Test Mob Owner", maxHp = 50f, damage = 10f, ac = 1f, basicActionID = 7001 });
        public int MobID => RuntimeData.Template.mobID;
        public MobType MobType => RuntimeData.Template.mobType;
        public float AC => RuntimeData.Template.ac;
        public MobRuntimeData RuntimeData => runtimeData;
        public MobTemplate Template => runtimeData.Template;

        public override float GetStatValueByID(int statID)
        {
            return statID == CombatStatIDs.Damage ? 10f : 0f;
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
        public MapContext(PlayerContext playerContext, GameObject managerObject, MapManager manager)
        {
            PlayerContext = playerContext;
            ManagerObject = managerObject;
            Manager = manager;
        }

        public PlayerContext PlayerContext { get; }
        public GameObject ManagerObject { get; }
        public MapManager Manager { get; }
        public CharacterData Character => PlayerContext.Character;
    }
}
