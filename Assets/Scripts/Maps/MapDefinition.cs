using System;
using System.Collections.Generic;

namespace IdleOff.Maps
{
    [Serializable]
    public sealed class MapDefinition
    {
        public int mapID;
        public string name;
        public string sceneName;
        public string playerSpawnAnchor;
        public MapLayoutDefinition layout = new();
        public List<MapInteractableSpawn> interactables = new();
        public List<MapMobSpawnerDefinition> mobSpawners = new();
        public List<MapPickupState> pickups = new();
    }
}
