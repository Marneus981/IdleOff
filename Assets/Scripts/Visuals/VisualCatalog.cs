using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using UnityEngine;

namespace IdleOff.Visuals
{
    public static class VisualCatalog
    {
        [DataContract]
#pragma warning disable CS0649
        private sealed class VisualValues
        {
            [DataMember(IsRequired = false)]
            public string spritePath;
            [DataMember(IsRequired = false)]
            public string sortingLayer;
            [DataMember(IsRequired = false)]
            public int sortingOrder;
            [DataMember(IsRequired = false)]
            public List<float> scale;
            [DataMember(IsRequired = false)]
            public List<float> offset;
            [DataMember(IsRequired = false)]
            public string defaultAnimation;
            [DataMember(IsRequired = false)]
            public Dictionary<string, AnimationValues> animations;
        }

        [DataContract]
        private sealed class AnimationValues
        {
            [DataMember(IsRequired = false)]
            public string spriteSheetPath;
            [DataMember(IsRequired = false)]
            public List<string> framePaths;
            [DataMember(IsRequired = false)]
            public float fps;
            [DataMember(IsRequired = false)]
            public bool loop;
        }
#pragma warning restore CS0649

        private const string VisualsPath = "Assets/Tables/Visuals.json";

        public static Dictionary<string, VisualDefinition> Visuals { get; private set; } = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadBeforeScene()
        {
            LoadVisuals();
        }

        public static void EnsureLoaded()
        {
            if (Visuals == null || Visuals.Count == 0)
            {
                LoadVisuals();
            }
        }

        public static void LoadVisuals()
        {
            var resolvedPath = ResolvePath(VisualsPath);
            if (!File.Exists(resolvedPath))
            {
                Visuals = new Dictionary<string, VisualDefinition>();
                return;
            }

            using var stream = File.OpenRead(resolvedPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, VisualValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            var serializedVisuals = (Dictionary<string, VisualValues>)serializer.ReadObject(stream);

            Visuals = new Dictionary<string, VisualDefinition>();
            foreach (var entry in serializedVisuals)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    throw new FormatException("Visual table contains an empty visual ID.");
                }

                Visuals[entry.Key] = CreateVisual(entry.Key, entry.Value);
            }
        }

        public static bool TryGet(string visualID, out VisualDefinition definition)
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(visualID) && Visuals.TryGetValue(visualID, out definition))
            {
                definition = definition.Clone();
                return true;
            }

            definition = null;
            return false;
        }

        public static string GetPlayerClassVisualID(string className)
        {
            var normalized = string.IsNullOrWhiteSpace(className)
                ? "wanderingsoul"
                : className.Replace(" ", string.Empty).ToLowerInvariant();
            return "player_" + normalized;
        }

        private static VisualDefinition CreateVisual(string visualID, VisualValues values)
        {
            return new VisualDefinition
            {
                visualID = visualID,
                spritePath = values.spritePath,
                sortingLayer = string.IsNullOrWhiteSpace(values.sortingLayer) ? "Default" : values.sortingLayer,
                sortingOrder = values.sortingOrder,
                scale = CreateVector2(values.scale, Vector2.one),
                offset = CreateVector2(values.offset, Vector2.zero),
                defaultAnimation = string.IsNullOrWhiteSpace(values.defaultAnimation) ? "idle" : values.defaultAnimation,
                animations = CreateAnimations(values.animations)
            };
        }

        private static Dictionary<string, VisualAnimationDefinition> CreateAnimations(Dictionary<string, AnimationValues> values)
        {
            var animations = new Dictionary<string, VisualAnimationDefinition>();
            if (values == null)
            {
                return animations;
            }

            foreach (var entry in values)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value == null)
                {
                    continue;
                }

                animations[entry.Key] = new VisualAnimationDefinition
                {
                    spriteSheetPath = entry.Value.spriteSheetPath,
                    framePaths = entry.Value.framePaths ?? new List<string>(),
                    fps = entry.Value.fps <= 0f ? 8f : entry.Value.fps,
                    loop = entry.Value.loop
                };
            }

            return animations;
        }

        private static Vector2 CreateVector2(List<float> values, Vector2 fallback)
        {
            return values == null || values.Count < 2
                ? fallback
                : new Vector2(values[0], values[1]);
        }

        private static string ResolvePath(string path)
        {
            return Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }
    }
}
