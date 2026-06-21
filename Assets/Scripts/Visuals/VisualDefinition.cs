using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Visuals
{
    [Serializable]
    public sealed class VisualDefinition
    {
        public string visualID;
        public string spritePath;
        public string sortingLayer = "Default";
        public int sortingOrder;
        public Vector2 scale = Vector2.one;
        public Vector2 offset = Vector2.zero;
        public string defaultAnimation = "idle";
        public Dictionary<string, VisualAnimationDefinition> animations = new();

        public VisualDefinition Clone()
        {
            var clone = new VisualDefinition
            {
                visualID = visualID,
                spritePath = spritePath,
                sortingLayer = sortingLayer,
                sortingOrder = sortingOrder,
                scale = scale,
                offset = offset,
                defaultAnimation = defaultAnimation,
                animations = new Dictionary<string, VisualAnimationDefinition>()
            };

            if (animations != null)
            {
                foreach (var entry in animations)
                {
                    if (!string.IsNullOrWhiteSpace(entry.Key) && entry.Value != null)
                    {
                        clone.animations[entry.Key] = entry.Value.Clone();
                    }
                }
            }

            return clone;
        }
    }
}
