using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Visuals
{
    [Serializable]
    public sealed class VisualAnimationDefinition
    {
        public string spriteSheetPath;
        public List<string> framePaths = new();
        public List<Sprite> frames = new();
        public float fps = 8f;
        public bool loop = true;

        public VisualAnimationDefinition Clone()
        {
            return new VisualAnimationDefinition
            {
                spriteSheetPath = spriteSheetPath,
                framePaths = framePaths == null ? new List<string>() : new List<string>(framePaths),
                frames = frames == null ? new List<Sprite>() : new List<Sprite>(frames),
                fps = fps,
                loop = loop
            };
        }
    }
}
