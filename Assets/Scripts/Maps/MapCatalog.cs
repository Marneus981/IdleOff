using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using IdleOff.Data;
using IdleOff.Profiles;
using UnityEngine;

namespace IdleOff.Maps
{
    public static class MapCatalog
    {
        [Serializable]
#pragma warning disable CS0649
        private struct MapValues
        {
            public string name;
            public string sceneName;
            public string playerSpawnAnchor;
            public LayoutValues layout;
            public List<InteractableSpawnValues> interactables;
            public List<MobSpawnerValues> mobSpawners;
            public List<PickupValues> pickups;
        }

        [Serializable]
        private struct LayoutValues
        {
            public List<RectObjectValues> platforms;
            public List<RectObjectValues> ladders;
            public List<AnchorValues> anchors;
            public BoundaryValues boundaries;
        }

        [Serializable]
        private struct BoundaryValues
        {
            public bool enabled;
            public float floorThickness;
            public float wallThickness;
            public string leftAnchorID;
            public string rightAnchorID;
            public string floorAnchorID;
            public string ceilingAnchorID;
        }

        [Serializable]
        private struct RectObjectValues
        {
            public string anchorID;
            public float x;
            public float y;
            public float width;
            public float height;
        }

        [Serializable]
        private struct AnchorValues
        {
            public string anchorID;
            public float x;
            public float y;
        }

        [Serializable]
        private struct InteractableSpawnValues
        {
            public string instanceID;
            public int interactableID;
            public string anchorID;
        }

        [Serializable]
        private struct MobSpawnerValues
        {
            public string spawnerID;
            public int mobID;
            public string anchorID;
            public int maxActive;
            public float respawnSeconds;
        }

        [Serializable]
        private struct PickupValues
        {
            public int itemID;
            public int quantity;
            public List<int> money;
            public bool isMoney;
            public string anchorID;
            public float x;
            public float y;
        }
#pragma warning restore CS0649

        private const string MapsPath = "Assets/Tables/Maps.json";

        public static Dictionary<int, MapDefinition> Maps { get; private set; } = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadBeforeScene()
        {
            LoadMaps();
        }

        public static void EnsureLoaded()
        {
            if (Maps == null || Maps.Count == 0)
            {
                LoadMaps();
            }
        }

        public static void LoadMaps()
        {
            var resolvedPath = ResolvePath(MapsPath);
            using var stream = File.OpenRead(resolvedPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, MapValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            var serializedMaps = (Dictionary<string, MapValues>)serializer.ReadObject(stream);

            Maps = new Dictionary<int, MapDefinition>();
            foreach (var entry in serializedMaps)
            {
                if (!int.TryParse(entry.Key, out var mapID))
                {
                    throw new FormatException($"Map table key '{entry.Key}' is not a valid map ID.");
                }

                Maps.Add(mapID, CreateMap(mapID, entry.Value));
            }
        }

        private static MapDefinition CreateMap(int mapID, MapValues values)
        {
            return new MapDefinition
            {
                mapID = mapID,
                name = values.name,
                sceneName = values.sceneName,
                playerSpawnAnchor = values.playerSpawnAnchor,
                layout = CreateLayout(values.layout),
                interactables = CreateInteractables(values.interactables),
                mobSpawners = CreateMobSpawners(values.mobSpawners),
                pickups = CreatePickups(values.pickups)
            };
        }

        private static MapLayoutDefinition CreateLayout(LayoutValues values)
        {
            var layout = new MapLayoutDefinition();
            if (values.platforms != null)
            {
                foreach (var platform in values.platforms)
                {
                    layout.platforms.Add(new MapPlatformDefinition
                    {
                        anchorID = platform.anchorID,
                        position = new Vector2(platform.x, platform.y),
                        size = new Vector2(Mathf.Max(0.01f, platform.width), Mathf.Max(0.01f, platform.height))
                    });
                }
            }

            if (values.ladders != null)
            {
                foreach (var ladder in values.ladders)
                {
                    layout.ladders.Add(new MapLadderDefinition
                    {
                        anchorID = ladder.anchorID,
                        position = new Vector2(ladder.x, ladder.y),
                        size = new Vector2(Mathf.Max(0.01f, ladder.width), Mathf.Max(0.01f, ladder.height))
                    });
                }
            }

            if (values.anchors != null)
            {
                foreach (var anchor in values.anchors)
                {
                    layout.anchors.Add(new MapAnchorDefinition
                    {
                        anchorID = anchor.anchorID,
                        position = new Vector2(anchor.x, anchor.y)
                    });
                }
            }

            layout.boundaries = new MapBoundaryDefinition
            {
                enabled = values.boundaries.enabled,
                floorThickness = values.boundaries.floorThickness <= 0f ? 1.5f : values.boundaries.floorThickness,
                wallThickness = values.boundaries.wallThickness <= 0f ? 1f : values.boundaries.wallThickness,
                leftAnchorID = values.boundaries.leftAnchorID,
                rightAnchorID = values.boundaries.rightAnchorID,
                floorAnchorID = values.boundaries.floorAnchorID,
                ceilingAnchorID = values.boundaries.ceilingAnchorID
            };

            return layout;
        }

        private static List<MapInteractableSpawn> CreateInteractables(List<InteractableSpawnValues> values)
        {
            var spawns = new List<MapInteractableSpawn>();
            if (values == null)
            {
                return spawns;
            }

            foreach (var value in values)
            {
                spawns.Add(new MapInteractableSpawn { instanceID = value.instanceID, interactableID = value.interactableID, anchorID = value.anchorID });
            }

            return spawns;
        }

        private static List<MapMobSpawnerDefinition> CreateMobSpawners(List<MobSpawnerValues> values)
        {
            var spawners = new List<MapMobSpawnerDefinition>();
            if (values == null)
            {
                return spawners;
            }

            foreach (var value in values)
            {
                spawners.Add(new MapMobSpawnerDefinition
                {
                    spawnerID = value.spawnerID,
                    mobID = value.mobID,
                    anchorID = value.anchorID,
                    maxActive = Mathf.Max(1, value.maxActive),
                    respawnSeconds = Mathf.Max(0f, value.respawnSeconds)
                });
            }

            return spawners;
        }

        private static List<MapPickupState> CreatePickups(List<PickupValues> values)
        {
            var pickups = new List<MapPickupState>();
            if (values == null)
            {
                return pickups;
            }

            foreach (var value in values)
            {
                pickups.Add(new MapPickupState
                {
                    itemID = value.itemID,
                    quantity = Mathf.Max(1, value.quantity),
                    money = CreateMoney(value.money),
                    isMoney = value.isMoney,
                    anchorID = value.anchorID,
                    position = new Vector2(value.x, value.y)
                });
            }

            return pickups;
        }

        private static Money CreateMoney(List<int> values)
        {
            if (values == null || values.Count == 0)
            {
                return new Money();
            }

            if (values.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(values), values.Count, "Map pickup money values must contain gold, silver, and copper.");
            }

            return new Money(values[0], values[1], values[2]);
        }

        private static string ResolvePath(string path)
        {
            return TablePathResolver.Resolve(path);
        }
    }
}
