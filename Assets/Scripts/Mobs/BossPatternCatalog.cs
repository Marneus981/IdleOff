using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using IdleOff.Data;
using UnityEngine;

namespace IdleOff.Mobs
{
    public static class BossPatternCatalog
    {
        [DataContract]
#pragma warning disable CS0649
        private sealed class PatternValues
        {
            [DataMember(IsRequired = false)]
            public string name;
            [DataMember(IsRequired = false)]
            public int bossMobID;
            [DataMember(IsRequired = false)]
            public float cooldown;
            [DataMember(IsRequired = false)]
            public bool loop;
            [DataMember(IsRequired = false)]
            public List<StepValues> steps;
        }

        [DataContract]
        private sealed class StepValues
        {
            [DataMember(IsRequired = false)]
            public float delay;
            [DataMember(IsRequired = false)]
            public int actionID;
            [DataMember(IsRequired = false)]
            public string originMode;
            [DataMember(IsRequired = false)]
            public string directionMode;
            [DataMember(IsRequired = false)]
            public string anchorID;
            [DataMember(IsRequired = false)]
            public float offsetX;
            [DataMember(IsRequired = false)]
            public float offsetY;
            [DataMember(IsRequired = false)]
            public float directionX;
            [DataMember(IsRequired = false)]
            public float directionY;
            [DataMember(IsRequired = false)]
            public int count;
            [DataMember(IsRequired = false)]
            public float spreadAngle;
            [DataMember(IsRequired = false)]
            public float spacing;
            [DataMember(IsRequired = false)]
            public int repeatCount;
            [DataMember(IsRequired = false)]
            public float repeatInterval;
            [DataMember(IsRequired = false)]
            public float telegraphDurationOverride;
        }
#pragma warning restore CS0649

        private const string PatternsPath = "Assets/Tables/BossPatterns.json";

        public static Dictionary<int, BossActionPattern> Patterns { get; private set; } = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadPatternsBeforeScene()
        {
            LoadPatterns();
        }

        public static void EnsureLoaded()
        {
            if (Patterns == null || Patterns.Count == 0)
            {
                LoadPatterns();
            }
        }

        public static void LoadPatterns()
        {
            Patterns = LoadPatterns(PatternsPath);
        }

        public static Dictionary<int, BossActionPattern> LoadPatterns(string patternsPath)
        {
            var resolvedPath = ResolvePath(patternsPath);
            if (!File.Exists(resolvedPath))
            {
                return new Dictionary<int, BossActionPattern>();
            }

            using var stream = File.OpenRead(resolvedPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, PatternValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            var serializedPatterns = (Dictionary<string, PatternValues>)serializer.ReadObject(stream);

            var patterns = new Dictionary<int, BossActionPattern>();
            foreach (var entry in serializedPatterns)
            {
                if (!int.TryParse(entry.Key, out var patternID))
                {
                    throw new FormatException($"Boss pattern table key '{entry.Key}' is not a valid pattern ID.");
                }

                patterns.Add(patternID, CreatePattern(patternID, entry.Value));
            }

            return patterns;
        }

        private static BossActionPattern CreatePattern(int patternID, PatternValues values)
        {
            var pattern = new BossActionPattern
            {
                patternID = patternID,
                name = values.name,
                bossMobID = values.bossMobID,
                cooldown = Mathf.Max(0f, values.cooldown),
                loop = values.loop,
                steps = new List<BossPatternStep>()
            };

            if (values.steps != null)
            {
                foreach (var step in values.steps)
                {
                    pattern.steps.Add(CreateStep(step));
                }
            }

            return pattern;
        }

        private static BossPatternStep CreateStep(StepValues values)
        {
            return new BossPatternStep
            {
                delay = Mathf.Max(0f, values.delay),
                actionID = values.actionID,
                originMode = ParseEnum(values.originMode, BossPatternOriginMode.BossPosition),
                directionMode = ParseEnum(values.directionMode, BossPatternDirectionMode.TowardTarget),
                anchorID = values.anchorID,
                offset = new Vector2(values.offsetX, values.offsetY),
                fixedDirection = new Vector2(values.directionX, values.directionY) == Vector2.zero
                    ? Vector2.right
                    : new Vector2(values.directionX, values.directionY).normalized,
                count = Mathf.Max(1, values.count),
                spreadAngle = values.spreadAngle,
                spacing = values.spacing,
                repeatCount = Mathf.Max(1, values.repeatCount),
                repeatInterval = Mathf.Max(0f, values.repeatInterval),
                telegraphDurationOverride = values.telegraphDurationOverride
            };
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum defaultValue)
            where TEnum : struct
        {
            return string.IsNullOrWhiteSpace(value) || !Enum.TryParse(value, true, out TEnum parsed)
                ? defaultValue
                : parsed;
        }

        private static string ResolvePath(string path)
        {
            return TablePathResolver.Resolve(path);
        }
    }
}
