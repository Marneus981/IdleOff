using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Visuals
{
    [CreateAssetMenu(menuName = "IdleOff/Visual Catalog")]
    public sealed class VisualCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<VisualDefinitionRecord> visuals = new();

        public IReadOnlyList<VisualDefinitionRecord> Visuals => visuals;

        public void SetVisuals(List<VisualDefinitionRecord> records)
        {
            visuals = records ?? new List<VisualDefinitionRecord>();
        }
    }

    [Serializable]
    public sealed class VisualDefinitionRecord
    {
        public string visualID;
        public string spritePath;
        public Sprite sprite;
        public string sortingLayer = "Default";
        public int sortingOrder;
        public Vector2 scale = Vector2.one;
        public Vector2 offset = Vector2.zero;
        public string defaultAnimation = "idle";
        public List<VisualAnimationRecord> animations = new();

        public VisualDefinition ToRuntimeDefinition()
        {
            var definition = new VisualDefinition
            {
                visualID = visualID,
                spritePath = spritePath,
                sprite = sprite,
                sortingLayer = string.IsNullOrWhiteSpace(sortingLayer) ? "Default" : sortingLayer,
                sortingOrder = sortingOrder,
                scale = scale == Vector2.zero ? Vector2.one : scale,
                offset = offset,
                defaultAnimation = string.IsNullOrWhiteSpace(defaultAnimation) ? "idle" : defaultAnimation,
                animations = new Dictionary<string, VisualAnimationDefinition>()
            };

            if (animations == null)
            {
                return definition;
            }

            foreach (var animation in animations)
            {
                if (animation == null || string.IsNullOrWhiteSpace(animation.animationID))
                {
                    continue;
                }

                definition.animations[animation.animationID] = animation.ToRuntimeDefinition();
            }

            return definition;
        }
    }

    [Serializable]
    public sealed class VisualAnimationRecord
    {
        public string animationID;
        public string spriteSheetPath;
        public List<string> framePaths = new();
        public List<Sprite> frames = new();
        public float fps = 8f;
        public bool loop = true;

        public VisualAnimationDefinition ToRuntimeDefinition()
        {
            return new VisualAnimationDefinition
            {
                spriteSheetPath = spriteSheetPath,
                framePaths = framePaths == null ? new List<string>() : new List<string>(framePaths),
                frames = frames == null ? new List<Sprite>() : new List<Sprite>(frames),
                fps = fps <= 0f ? 8f : fps,
                loop = loop
            };
        }
    }
}
