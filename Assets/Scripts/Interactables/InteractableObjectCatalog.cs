using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using UnityEngine;

namespace IdleOff.Interactables
{
    public static class InteractableObjectCatalog
    {
        [Serializable]
#pragma warning disable CS0649
        private struct InteractableValues
        {
            public string name;
            public string description;
            public string type;
            public string closedSpritePath;
            public string openSpritePath;
            public ConditionValues condition;
            public EffectValues effect;
        }

        [Serializable]
        private struct ConditionValues
        {
            public string type;
            public int requiredAmount;
            public int itemID;
            public int bossMobID;
            public bool consumeItem;
        }

        [Serializable]
        private struct EffectValues
        {
            public string type;
            public int targetMapID;
        }
#pragma warning restore CS0649

        private const string InteractablesPath = "Assets/Tables/InteractableObjects.json";

        public static Dictionary<int, InteractableObjectDefinition> Interactables { get; private set; } = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadBeforeScene()
        {
            LoadInteractables();
        }

        public static void EnsureLoaded()
        {
            if (Interactables == null || Interactables.Count == 0)
            {
                LoadInteractables();
            }
        }

        public static void LoadInteractables()
        {
            var resolvedPath = ResolvePath(InteractablesPath);
            using var stream = File.OpenRead(resolvedPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, InteractableValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            var serializedInteractables = (Dictionary<string, InteractableValues>)serializer.ReadObject(stream);

            Interactables = new Dictionary<int, InteractableObjectDefinition>();
            foreach (var entry in serializedInteractables)
            {
                if (!int.TryParse(entry.Key, out var interactableID))
                {
                    throw new FormatException($"Interactable table key '{entry.Key}' is not a valid interactable ID.");
                }

                Interactables.Add(interactableID, CreateDefinition(interactableID, entry.Value));
            }
        }

        private static InteractableObjectDefinition CreateDefinition(int interactableID, InteractableValues values)
        {
            return new InteractableObjectDefinition
            {
                interactableID = interactableID,
                name = values.name,
                description = values.description,
                type = ParseEnum(values.type, InteractableObjectType.Portal),
                closedSpritePath = values.closedSpritePath,
                openSpritePath = values.openSpritePath,
                condition = new InteractCondition
                {
                    type = ParseEnum(values.condition.type, InteractConditionType.None),
                    requiredAmount = Mathf.Max(0, values.condition.requiredAmount),
                    itemID = values.condition.itemID,
                    bossMobID = values.condition.bossMobID,
                    consumeItem = values.condition.consumeItem
                },
                effect = new InteractEffect
                {
                    type = ParseEnum(values.effect.type, InteractEffectType.None),
                    targetMapID = values.effect.targetMapID
                }
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
            return Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }
    }
}
