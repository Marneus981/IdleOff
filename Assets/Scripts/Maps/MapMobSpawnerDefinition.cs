using System;

namespace IdleOff.Maps
{
    [Serializable]
    public sealed class MapMobSpawnerDefinition
    {
        public string spawnerID;
        public int mobID;
        public string anchorID;
        public int maxActive = 1;
        public float respawnSeconds = 5f;
    }
}
