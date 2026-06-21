using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using UnityEngine;

namespace IdleOff.Actions
{
    public static class ActionCatalog
    {
        [DataContract]
#pragma warning disable CS0649
        private sealed class ActionValues
        {
            [DataMember(IsRequired = false)]
            public string name;
            [DataMember(IsRequired = false)]
            public string description;
            [DataMember(IsRequired = false)]
            public List<string> tags;
            [DataMember(IsRequired = false)]
            public int level;
            [DataMember(IsRequired = false)]
            public int maxLevel;
            [DataMember(IsRequired = false)]
            public float cooldown;
            [DataMember(IsRequired = false)]
            public float range;
            [DataMember(IsRequired = false)]
            public float attackScaling;
            [DataMember(IsRequired = false)]
            public float attackScalingPerLevel;
            [DataMember(IsRequired = false)]
            public float? projectileSpeed;
            [DataMember(IsRequired = false)]
            public float? projectileLifetime;
            [DataMember(IsRequired = false)]
            public int? projectilePierceCount;
            [DataMember(IsRequired = false)]
            public float? areaDelay;
            [DataMember(IsRequired = false)]
            public float? areaDuration;
            [DataMember(IsRequired = false)]
            public float? areaTickInterval;
            [DataMember(IsRequired = false)]
            public float? telegraphDuration;
            [DataMember(IsRequired = false)]
            public float? radius;
            [DataMember(IsRequired = false)]
            public float? width;
            [DataMember(IsRequired = false)]
            public float? height;
            [DataMember(IsRequired = false)]
            public string ownerType;
            [DataMember(IsRequired = false)]
            public string targetingType;
            [DataMember(IsRequired = false)]
            public string hitboxType;
            [DataMember(IsRequired = false)]
            public string projectileVisualID;
            [DataMember(IsRequired = false)]
            public string areaVisualID;
            [DataMember(IsRequired = false)]
            public string telegraphVisualID;
        }
#pragma warning restore CS0649

        private const string MobActionsPath = "Assets/Tables/MobActions.json";

        public static Dictionary<int, Action> MobActions { get; private set; } = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadGlobalActionsBeforeScene()
        {
            LoadGlobalActions();
        }

        public static void LoadGlobalActions()
        {
            MobActions = LoadActions(MobActionsPath, "mob action");
        }

        public static void EnsureLoaded()
        {
            if (MobActions == null || MobActions.Count == 0)
            {
                LoadGlobalActions();
            }
        }

        public static Dictionary<int, Action> LoadActions(string actionsPath, string tableName)
        {
            var resolvedPath = ResolveActionsPath(actionsPath);
            using var stream = File.OpenRead(resolvedPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, ActionValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            var serializedActions = (Dictionary<string, ActionValues>)serializer.ReadObject(stream);

            var actionsByID = new Dictionary<int, Action>();
            foreach (var entry in serializedActions)
            {
                if (!int.TryParse(entry.Key, out var actionID))
                {
                    throw new FormatException($"{tableName} table key '{entry.Key}' is not a valid action ID.");
                }

                var action = CreateAction(actionID, entry.Value, tableName);
                if (actionsByID.ContainsKey(actionID))
                {
                    throw new InvalidOperationException($"{tableName} table '{actionsPath}' contains duplicate action ID {actionID}.");
                }

                actionsByID.Add(actionID, action);
            }

            return actionsByID;
        }

        private static Action CreateAction(int actionID, ActionValues values, string tableName)
        {
            if (actionID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actionID), actionID, "Action ID must be positive.");
            }

            if (values.maxLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(values), values.maxLevel, $"{tableName} ID {actionID} has an invalid max level.");
            }

            if (values.level < 0 || values.level > values.maxLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(values), values.level, $"{tableName} ID {actionID} has a level outside 0..maxLevel.");
            }

            return new Action
            {
                actionID = actionID,
                name = values.name,
                description = values.description,
                tags = values.tags ?? new List<string>(),
                level = values.level,
                maxLevel = values.maxLevel == 0 ? 1 : values.maxLevel,
                cooldown = Mathf.Max(0f, values.cooldown),
                range = Mathf.Max(0f, values.range),
                attackScaling = values.attackScaling == 0f ? 1f : values.attackScaling,
                attackScalingPerLevel = values.attackScalingPerLevel,
                projectileSpeed = values.projectileSpeed.GetValueOrDefault() <= 0f ? 8f : values.projectileSpeed.Value,
                projectileLifetime = values.projectileLifetime.GetValueOrDefault() <= 0f ? 2f : values.projectileLifetime.Value,
                projectilePierceCount = Mathf.Max(0, values.projectilePierceCount.GetValueOrDefault()),
                areaDelay = Mathf.Max(0f, values.areaDelay.GetValueOrDefault()),
                areaDuration = Mathf.Max(0f, values.areaDuration.GetValueOrDefault()),
                areaTickInterval = Mathf.Max(0f, values.areaTickInterval.GetValueOrDefault()),
                telegraphDuration = Mathf.Max(0f, values.telegraphDuration.GetValueOrDefault()),
                radius = Mathf.Max(0f, values.radius.GetValueOrDefault()),
                size = new Vector2(Mathf.Max(0f, values.width.GetValueOrDefault()), Mathf.Max(0f, values.height.GetValueOrDefault())),
                ownerType = ParseEnum(values.ownerType, ActionOwnerType.Any),
                targetingType = ParseEnum(values.targetingType, ActionTargetingType.ForwardMelee),
                hitboxType = ParseEnum(values.hitboxType, ActionHitboxType.Box),
                projectileVisualID = values.projectileVisualID,
                areaVisualID = values.areaVisualID,
                telegraphVisualID = values.telegraphVisualID
            };
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum defaultValue)
            where TEnum : struct
        {
            return string.IsNullOrWhiteSpace(value) || !Enum.TryParse(value, true, out TEnum parsed)
                ? defaultValue
                : parsed;
        }

        private static string ResolveActionsPath(string actionsPath)
        {
            if (Path.IsPathRooted(actionsPath))
            {
                return actionsPath;
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), actionsPath));
        }
    }
}
