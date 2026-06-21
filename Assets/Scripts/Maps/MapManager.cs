using System.Collections.Generic;
using IdleOff.Actions;
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
        public const int HubMapID = 1000;

        [SerializeField] private CharacterProfile profile;
        [SerializeField] private int initialMapID = HubMapID;
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
        private WorldDropSpawner subscribedDropSpawner;
        private string stateStoreCharacterID;
        private Rect currentDropBounds;
        private bool hasCurrentDropBounds;

        public static MapManager Instance { get; private set; }
        public MapDefinition CurrentMap { get; private set; }
        public MapRuntimeState CurrentRuntimeState { get; private set; }
        public PlayerCombatant Player { get; private set; }
        public float? CurrentVoidRespawnY { get; private set; }

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
            ResetStateStoreIfCharacterChanged();
        }

        public void SetProfile(CharacterProfile characterProfile)
        {
            profile = characterProfile;
            ResetStateStoreIfCharacterChanged();
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
            SubscribeToDropSpawner();
            if (Player != null && profile == null)
            {
                profile = Player.Character?.ParentProfile;
            }

            if (profile != null && profile.ActiveCharacter != null && CurrentMap == null)
            {
                stateStore = new CharacterMapStateStore(profile.ActiveCharacter);
                LoadMap(initialMapID);
            }
        }

        private void OnApplicationQuit()
        {
            SaveCurrentMapState();
        }

        private void OnDestroy()
        {
            if (subscribedDropSpawner != null)
            {
                subscribedDropSpawner.DropSpawned -= HandleDropSpawned;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void LoadMap(int mapID)
        {
            LoadMap(mapID, null);
        }

        public void LoadMapFromPortal(int mapID)
        {
            var sourceMapID = CurrentMap?.mapID ?? 0;
            LoadMap(mapID, sourceMapID);
        }

        public void LoadMap(int mapID, Vector2? spawnOverride)
        {
            LoadMap(mapID, spawnOverride, 0);
        }

        private void LoadMap(int mapID, int sourceMapID)
        {
            LoadMap(mapID, null, sourceMapID);
        }

        private void LoadMap(int mapID, Vector2? spawnOverride, int sourceMapID)
        {
            ClearTransientPickupsFromCurrentMapState();
            SaveCurrentMapState();
            SubscribeToDropSpawner();
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

            EnsureStateStore();
            CurrentMap = map;
            CurrentRuntimeState = stateStore.GetOrCreateMapState(map.mapID);
            ClearMapRoot();
            CreateMapRoot(map.name);
            BuildAnchors(map);
            BuildLayout(map);
            BuildBoundaries(map);
            MovePlayerToSpawn(map, ResolvePortalSpawnOverride(map, sourceMapID) ?? spawnOverride);
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

            if (stateStore != null && CurrentMap != null && Player != null)
            {
                stateStore.SetLastLocation(CurrentMap.mapID, Player.transform.position);
            }

            stateStore?.Save();
        }

        public bool TryGetLastSavedLocation(out int mapID, out Vector2 position)
        {
            mapID = default;
            position = default;
            EnsureStateStore();
            return stateStore != null && stateStore.TryGetLastLocation(out mapID, out position);
        }

        private void EnsureStateStore()
        {
            if (profile == null || profile.ActiveCharacter == null)
            {
                return;
            }

            var activeCharacterID = profile.ActiveCharacter.CharacterID;
            if (stateStore == null || stateStoreCharacterID != activeCharacterID)
            {
                stateStore = new CharacterMapStateStore(profile.ActiveCharacter);
                stateStoreCharacterID = activeCharacterID;
            }
        }

        private void ResetStateStoreIfCharacterChanged()
        {
            var activeCharacterID = profile?.ActiveCharacter?.CharacterID;
            if (string.IsNullOrWhiteSpace(activeCharacterID) || stateStoreCharacterID == activeCharacterID)
            {
                return;
            }

            stateStore = null;
            CurrentRuntimeState = null;
            stateStoreCharacterID = null;
        }

        public bool TryGetAnchor(string anchorID, out Vector2 position)
        {
            return anchorsByID.TryGetValue(anchorID, out position);
        }

        public bool TryGetCurrentSpawnPosition(out Vector2 position)
        {
            position = default;
            return CurrentMap != null
                && !string.IsNullOrWhiteSpace(CurrentMap.playerSpawnAnchor)
                && TryGetAnchor(CurrentMap.playerSpawnAnchor, out position);
        }

        public bool IsPositionWithinCurrentDropBounds(Vector2 position)
        {
            return !hasCurrentDropBounds || currentDropBounds.Contains(position);
        }

        private Vector2? ResolvePortalSpawnOverride(MapDefinition destinationMap, int sourceMapID)
        {
            if (destinationMap == null || sourceMapID <= 0 || destinationMap.interactables == null)
            {
                return null;
            }

            foreach (var spawn in destinationMap.interactables)
            {
                if (spawn == null
                    || string.IsNullOrWhiteSpace(spawn.anchorID)
                    || !InteractableObjectCatalog.Interactables.TryGetValue(spawn.interactableID, out var definition)
                    || definition.effect == null
                    || definition.effect.type != InteractEffectType.TravelToMap
                    || definition.effect.targetMapID != sourceMapID
                    || !TryGetAnchor(spawn.anchorID, out var position))
                {
                    continue;
                }

                return position;
            }

            return null;
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
            ActionRuntimeRegistry.ClearAll();
            if (mapRoot != null)
            {
                DestroyMapObject(mapRoot);
            }

            CurrentVoidRespawnY = null;
            hasCurrentDropBounds = false;
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

        private void BuildBoundaries(MapDefinition map)
        {
            if (map.layout?.boundaries == null || !map.layout.boundaries.enabled)
            {
                return;
            }

            var bounds = CalculateLayoutBounds(map);
            var camera = Camera.main;
            var halfHeight = camera != null && camera.orthographic ? camera.orthographicSize : 4.5f;
            var halfWidth = halfHeight * (camera != null ? camera.aspect : 16f / 9f);
            var floorThickness = Mathf.Max(0.1f, map.layout.boundaries.floorThickness);
            var wallThickness = Mathf.Max(0.1f, map.layout.boundaries.wallThickness);
            var useAnchorBounds = TryGetBoundaryAnchorValues(map.layout.boundaries, out var anchorLeftX, out var anchorRightX, out var anchorFloorY, out var anchorCeilingY);

            var leftX = useAnchorBounds ? anchorLeftX : bounds.min.x - halfWidth - wallThickness * 0.5f;
            var rightX = useAnchorBounds ? anchorRightX : bounds.max.x + halfWidth + wallThickness * 0.5f;
            var floorY = useAnchorBounds ? anchorFloorY : bounds.min.y - halfHeight - floorThickness * 0.5f;
            var ceilingY = useAnchorBounds ? anchorCeilingY : bounds.max.y + halfHeight + wallThickness * 0.5f;
            CurrentVoidRespawnY = floorY - floorThickness - 1f;
            currentDropBounds = Rect.MinMaxRect(leftX, floorY, rightX, ceilingY);
            hasCurrentDropBounds = true;
            var centerX = (leftX + rightX) * 0.5f;
            var centerY = (floorY + ceilingY) * 0.5f;
            var horizontalWidth = Mathf.Abs(rightX - leftX) + wallThickness * 2f;
            var verticalHeight = Mathf.Abs(ceilingY - floorY) + floorThickness + wallThickness;

            var floor = CreateBox("Boundary Floor", new Vector2(centerX, floorY), new Vector2(horizontalWidth, floorThickness), platformSprite, new Color32(96, 91, 83, 255), layoutRoot.transform);
            floor.AddComponent<BoxCollider2D>().size = Vector2.one;

            CreateInvisibleBoundary("Boundary Ceiling", new Vector2(centerX, ceilingY), new Vector2(horizontalWidth, wallThickness));
            CreateInvisibleBoundary("Boundary Left Wall", new Vector2(leftX, centerY), new Vector2(wallThickness, verticalHeight));
            CreateInvisibleBoundary("Boundary Right Wall", new Vector2(rightX, centerY), new Vector2(wallThickness, verticalHeight));
        }

        private bool TryGetBoundaryAnchorValues(MapBoundaryDefinition boundaries, out float leftX, out float rightX, out float floorY, out float ceilingY)
        {
            leftX = default;
            rightX = default;
            floorY = default;
            ceilingY = default;

            if (boundaries == null
                || string.IsNullOrWhiteSpace(boundaries.leftAnchorID)
                || string.IsNullOrWhiteSpace(boundaries.rightAnchorID)
                || string.IsNullOrWhiteSpace(boundaries.floorAnchorID)
                || string.IsNullOrWhiteSpace(boundaries.ceilingAnchorID)
                || !TryGetAnchor(boundaries.leftAnchorID, out var left)
                || !TryGetAnchor(boundaries.rightAnchorID, out var right)
                || !TryGetAnchor(boundaries.floorAnchorID, out var floor)
                || !TryGetAnchor(boundaries.ceilingAnchorID, out var ceiling))
            {
                return false;
            }

            leftX = left.x;
            rightX = right.x;
            floorY = floor.y;
            ceilingY = ceiling.y;
            return rightX > leftX && ceilingY > floorY;
        }

        private Bounds CalculateLayoutBounds(MapDefinition map)
        {
            var initialized = false;
            var min = Vector2.zero;
            var max = Vector2.zero;

            void Encapsulate(Vector2 center, Vector2 size)
            {
                var half = size * 0.5f;
                var objectMin = center - half;
                var objectMax = center + half;
                if (!initialized)
                {
                    min = objectMin;
                    max = objectMax;
                    initialized = true;
                    return;
                }

                min = Vector2.Min(min, objectMin);
                max = Vector2.Max(max, objectMax);
            }

            foreach (var platform in map.layout.platforms)
            {
                Encapsulate(platform.position, platform.size);
            }

            foreach (var ladder in map.layout.ladders)
            {
                Encapsulate(ladder.position, ladder.size);
            }

            foreach (var anchor in map.layout.anchors)
            {
                Encapsulate(anchor.position, Vector2.one);
            }

            if (!initialized)
            {
                Encapsulate(Vector2.zero, Vector2.one);
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        private void CreateInvisibleBoundary(string name, Vector2 position, Vector2 size)
        {
            var boundary = new GameObject(name);
            boundary.transform.SetParent(layoutRoot.transform);
            boundary.transform.position = position;
            boundary.transform.localScale = new Vector3(size.x, size.y, 1f);
            boundary.AddComponent<BoxCollider2D>().size = Vector2.one;
        }

        private void MovePlayerToSpawn(MapDefinition map, Vector2? spawnOverride)
        {
            Player ??= FindFirstObjectByType<PlayerCombatant>();
            if (Player == null)
            {
                return;
            }

            Vector2 targetPosition;
            if (spawnOverride.HasValue)
            {
                targetPosition = spawnOverride.Value;
            }
            else if (!string.IsNullOrWhiteSpace(map.playerSpawnAnchor) && TryGetAnchor(map.playerSpawnAnchor, out var spawnPosition))
            {
                targetPosition = spawnPosition;
            }
            else
            {
                return;
            }

            PlayerPlacementUtility.MoveFeetTo(Player.gameObject, targetPosition);
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

                var spawnerObject = new GameObject("Mob Spawner - " + spawner.spawnerID);
                spawnerObject.transform.SetParent(mobsRoot.transform);
                spawnerObject.transform.position = position;
                spawnerObject.AddComponent<MobSpawner>().Initialize(spawner, mobSprite);
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
                var drop = spawner.SpawnDrop(payload, position, false);
                drop.transform.SetParent(pickupsRoot.transform);
                drop.Collected += HandleTrackedDropCollected;
                drop.Expired += HandleTrackedDropExpired;
            }
        }

        private void SubscribeToDropSpawner()
        {
            var spawner = WorldDropSpawner.Instance;
            if (spawner == null || subscribedDropSpawner == spawner)
            {
                return;
            }

            if (subscribedDropSpawner != null)
            {
                subscribedDropSpawner.DropSpawned -= HandleDropSpawned;
            }

            subscribedDropSpawner = spawner;
            subscribedDropSpawner.DropSpawned += HandleDropSpawned;
        }

        private void HandleDropSpawned(WorldDrop drop)
        {
            if (drop == null || drop.Payload == null || CurrentRuntimeState == null)
            {
                return;
            }

            drop.transform.SetParent(pickupsRoot != null ? pickupsRoot.transform : null);
            var state = new MapPickupState
            {
                itemID = drop.Payload.itemID,
                quantity = drop.Payload.quantity,
                money = drop.Payload.money,
                isMoney = drop.Payload.isMoney,
                position = drop.transform.position
            };
            CurrentRuntimeState.presentPickups.Add(state);
            drop.Collected += HandleTrackedDropCollected;
            drop.Expired += HandleTrackedDropExpired;
            SaveCurrentMapState();
        }

        private void HandleTrackedDropCollected(WorldDrop drop)
        {
            RemoveTrackedDrop(drop);
        }

        private void HandleTrackedDropExpired(WorldDrop drop)
        {
            RemoveTrackedDrop(drop);
        }

        private void RemoveTrackedDrop(WorldDrop drop)
        {
            if (drop == null || drop.Payload == null || CurrentRuntimeState == null)
            {
                return;
            }

            var dropPosition = (Vector2)drop.transform.position;
            for (var i = CurrentRuntimeState.presentPickups.Count - 1; i >= 0; i--)
            {
                var pickup = CurrentRuntimeState.presentPickups[i];
                if (pickup == null || pickup.isMoney != drop.Payload.isMoney)
                {
                    continue;
                }

                var samePayload = pickup.isMoney
                    ? pickup.money.TotalCopper == drop.Payload.money.TotalCopper
                    : pickup.itemID == drop.Payload.itemID && pickup.quantity == drop.Payload.quantity;
                if (samePayload && Vector2.Distance(pickup.position, dropPosition) < 0.05f)
                {
                    CurrentRuntimeState.presentPickups.RemoveAt(i);
                    SaveCurrentMapState();
                    return;
                }
            }
        }

        private void ClearTransientPickupsFromCurrentMapState()
        {
            if (CurrentRuntimeState == null || CurrentRuntimeState.presentPickups.Count == 0)
            {
                return;
            }

            CurrentRuntimeState.presentPickups.Clear();
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

        private static void DestroyMapObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
                return;
            }

            DestroyImmediate(target);
        }
    }
}
