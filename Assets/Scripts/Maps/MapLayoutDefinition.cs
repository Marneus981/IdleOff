using System;
using System.Collections.Generic;

namespace IdleOff.Maps
{
    [Serializable]
    public sealed class MapLayoutDefinition
    {
        public List<MapPlatformDefinition> platforms = new();
        public List<MapLadderDefinition> ladders = new();
        public List<MapAnchorDefinition> anchors = new();
        public MapBoundaryDefinition boundaries = new();
    }
}
