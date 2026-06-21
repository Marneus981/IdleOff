using System;
using System.Collections.Generic;

namespace IdleOff.Visuals
{
    [Serializable]
    public sealed class VisualAnimationDefinition
    {
        public string spriteSheetPath;
        public List<string> framePaths = new();
        public float fps = 8f;
        public bool loop = true;

        public VisualAnimationDefinition Clone()
        {
            return new VisualAnimationDefinition
            {
                spriteSheetPath = spriteSheetPath,
                framePaths = framePaths == null ? new List<string>() : new List<string>(framePaths),
                fps = fps,
                loop = loop
            };
        }
    }
}
