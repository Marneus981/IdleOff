using System.Collections.Generic;
using IdleOff.Combat;
using IdleOff.Drops;
using IdleOff.Interactables;
using IdleOff.Mobs;
using IdleOff.Player;
using IdleOff.Profiles;
using IdleOff.World;
using UnityEngine;

namespace IdleOff.Maps
{
    [DisallowMultipleComponent]
    public sealed class MapManager : MonoBehaviour
    {
        [SerializeField] private CharacterProfile profile;
        [SerializeField] private int initialMapID = 1001;
        [SerializeField] private Sprite platformSprite;
        [SerializeField] private Sprite ladderSprite;
        [SerializeField] private Sprite mobSprite;
        [SerializeField] private Sprite portalClosedSprite;
        [SerializeField] private Sprite portalOpenSprite;

        private readonly Dictionary<string, Vector2> anchorsByID = new();
        private CharacterMapStateStore stateStore;
        private GameObject mapRoot;
        private GameObject layoutRoot;
        private GameObject interactablesRoot;
        private GameObject mobsRoot;
        private GameObject pickupsRoot;

        public static MapManager Instance { get; private set; }
        public MapDefinition CurrentMap { get; private set; }
        public MapRuntimeState CurrentRuntimeState { get; private set; }
        public PlayerCombatant Player { get; private set; }

        public void Configure(
            CharacterProfile characterProfile,
            int mapID,
            Sprite platformSprite,
            Sprite ladderSprite,
            Sprite mobSprite,
            Sprite portalClosedSprite,
            Sprite portalOpenSprite)
        {
            profile = characterProfile;
            initialMapID = mapID;
            this.platformSprite = platformSprite;
            this.ladderSprite = ladderSprite;
            this.mobSprite = mobSprite;
            this.portalClosedSprite = portalClosedSprite;
            this.portalOpenSprite = portalOpenSprite;
        }

        public void SetProfile(CharacterProfile characterProfile)
        {
            profile = characterProfile;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            Player = FindFirstObjectByType<PlayerCombatant>();
            if (Player != null && profile == null)
            {
                profile = Player.Character?.ParentProfile;
            }

            if (profile != null && profile.ActiveCharacter != null)
            {
                stateStore = new CharacterMapStateStore(profile.ActiveCharacter);
                LoadMap(initialMapID);
            }
        }

        private void OnApplicationQuit()
        {
            SaveCurrentMapState();
        }

        public void LoadMap(int mapID)
        {
            SaveCurrentMapState();
            MapCatalog.EnsureLoaded();
            InteractableObjectCatalog.EnsureLoaded();
            MobCatalog.EnsureLoaded();

            if (!MapCatalog.Maps.TryGetValue(mapID, out var map))
            {
                throw new KeyNotFoundException($"Map ID {mapID} was not found.");
            }

            if (profile == null || profile.ActiveCharacter == null)
            {
                throw new System.InvalidOperationException("MapManager requires a profile with an active character to load map state.");
            }

            stateStore ??= new CharacterMapStateStore(profile.ActiveCharacter);
            CurrentMap = map;
            CurrentRuntimeState = stateStore.GetOrCreateMapState(map.mapID);
            ClearMapRoot();
            CreateMapRoot(map.name);
            BuildAnchors(map);
            BuildLayout(map);
            MovePlayerToSpawn(map);
            SpawnInteractables(map);
            SpawnMobs(map);
            SpawnPickups(CurrentRuntimeState.presentPickups.Count > 0 ? CurrentRuntimeState.presentPickups : map.pickups);
            SaveCurrentMapState();
        }

        public void SaveCurrentMapState()
        {
            if (CurrentRuntimeState != null)
            {
                CurrentRuntimeState.lastSavedUtcTicks = System.DateTime.UtcNow.Ticks;
            }

            stateStore?.Save();
        }

        public bool TryGetAnchor(string anchorID, out Vector2 position)
        {
            return anchorsByID.TryGetValue(anchorID, out position);
        }

        public void RecordMobKilled(MobEntity mob)
        {
            if (mob == null || CurrentRuntimeState == null)
            {
                return;
            }

            CurrentRuntimeState.RecordMobKilled(mob.Template);
            SaveCurrentMapState();
        }

        private void ClearMapRoot()
        {
            if (mapRoot != null)
            {
                Destroy(mapRoot);
            }

            anchorsByID.Clear();
        }

        private void CreateMapRoot(string mapName)
        {
            mapRoot = new GameObject("MapRoot - " + mapName);
            layoutRoot = new GameObject("Layout");
            interactablesRoot = new GameObject("Interactables");
            mobsRoot = new GameObject("Mobs");
            pickupsRoot = new GameObject("Pickups");
            layoutRoot.transform.SetParent(mapRoot.transform);
            interactablesRoot.transform.SetParent(mapRoot.transform);
            mobsRoot.transform.SetParent(mapRoot.transform);
            pickupsRoot.transform.SetParent(mapRoot.transform);
        }

        private void BuildAnchors(MapDefinition map)
        {
            if (map.layout?.anchors != null)
            {
                foreach (var anchor in map.layout.anchors)
                {
                    if (!string.IsNullOrWhiteSpace(anchor.anchorID))
                    {
                        anchorsByID[anchor.anchorID] = anchor.position;
                    }
                }
            }

            if (map.layout?.platforms != null)
            {
                foreach (var platform in map.layout.platforms)
                {
                    if (!string.IsNullOrWhiteSpace(platform.anchorID))
                    {
                        anchorsByID[platform.anchorID] = platform.position;
                    }
                }
            }

            if (map.layout?.ladders != null)
            {
                foreach (var ladder in map.layout.ladders)
                {
                    if (!string.IsNullOrWhiteSpace(ladder.anchorID))
                    {
                        anchorsByID[ladder.anchorID] = ladder.position;
                    }
                }
            }
        }

        private void BuildLayout(MapDefinition map)
        {
            foreach (var platform in map.layout.platforms)
            {
                var platformObject = CreateBox("Platform - " + platform.anchorID, platform.position, platform.size, platformSprite, new Color32(96, 91, 83, 255), layoutRoot.transform);
                platformObject.AddComponent<BoxCollider2D>().size = Vector2.one;
                platformObject.AddComponent<DropThroughPlatform>();
            }

            foreach (var ladder in map.layout.ladders)
            {
                var ladderObject = CreateBox("Ladder - " + ladder.anchorID, ladder.position, ladder.size, ladderSprite, new Color32(201, 142, 69, 180), layoutRoot.transform);
                var collider = ladderObject.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;
                collider.isTrigger = true;
                ladderObject.AddComponent<LadderZone>();
            }
        }

        private void MovePlayerToSpawn(MapDefinition map)
        {
            Player ??= FindFirstObjectByType<PlayerCombatant>();
            if (Player == null || string.IsNullOrWhiteSpace(map.playerSpawnAnchor) || !TryGetAnchor(map.playerSpawnAnchor, out var position))
            {
                return;
            }

            Player.transform.position = position;
            var body = Player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }
        }

        private void SpawnInteractables(MapDefinition map)
        {
            foreach (var spawn in map.interactables)
            {
                if (!TryGetAnchor(spawn.anchorID, out var position)
                    || !InteractableObjectCatalog.Interactables.TryGetValue(spawn.interactableID, out var definition))
                {
                    continue;
                }

                var portal = CreateBox(definition.name, position, new Vector2(0.8f, 1.2f), portalClosedSprite, Color.white, interactablesRoot.transform);
                var entity = portal.AddComponent<InteractableObjectEntity>();
                entity.Initialize(spawn.instanceID, definition, portalClosedSprite, portalOpenSprite);
            }
        }

        private void SpawnMobs(MapDefinition map)
        {
            foreach (var spawner in map.mobSpawners)
            {
                if (!TryGetAnchor(spawner.anchorID, out var position))
                {
                    continue;
                }

                for (var i = 0; i < spawner.maxActive; i++)
                {
                    var mobObject = CreateBox("Mob - " + spawner.mobID, position, new Vector2(0.75f, 0.75f), mobSprite, new Color32(232, 90, 86, 255), mobsRoot.transform);
                    mobObject.AddComponent<BoxCollider2D>();
                    var body = mobObject.AddComponent<Rigidbody2D>();
                    body.gravityScale = 3f;
                    body.freezeRotation = true;
                    var mob = mobObject.AddComponent<MobEntity>();
                    mob.Initialize(spawner.mobID);
                    mob.Died += RecordMobKilled;
                }
            }
        }

        private void SpawnPickups(IReadOnlyList<MapPickupState> pickups)
        {
            var spawner = WorldDropSpawner.Instance;
            if (spawner == null || pickups == null)
            {
                return;
            }

            foreach (var pickup in pickups)
            {
                var position = pickup.position;
                if (!string.IsNullOrWhiteSpace(pickup.anchorID) && TryGetAnchor(pickup.anchorID, out var anchorPosition))
                {
                    position = anchorPosition;
                }

                var payload = pickup.isMoney
                    ? WorldDropPayload.Money(pickup.money)
                    : WorldDropPayload.Item(pickup.itemID, pickup.quantity);
                var drop = spawner.SpawnDrop(payload, position);
                drop.transform.SetParent(pickupsRoot.transform);
            }
        }

        private static GameObject CreateBox(string name, Vector2 position, Vector2 size, Sprite sprite, Color color, Transform parent)
        {
            var box = new GameObject(name);
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = box.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : CreateRuntimeSprite((Color32)color);
            renderer.color = color;
            return box;
        }

        private static Sprite CreateRuntimeSprite(Color32 color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
