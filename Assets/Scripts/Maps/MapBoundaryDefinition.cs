using System;

namespace IdleOff.Maps
{
    [Serializable]
    public sealed class MapBoundaryDefinition
    {
        public bool enabled = true;
        public float floorThickness = 1.5f;
        public float wallThickness = 1f;
        public string leftAnchorID;
        public string rightAnchorID;
        public string floorAnchorID;
        public string ceilingAnchorID;
    }
}
