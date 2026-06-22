using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using IdleOff.Data;
using UnityEngine;

namespace IdleOff.Profiles
{
    public static class GlobalModifierCatalog
    {
        [Serializable]
#pragma warning disable CS0649
        private struct ModifierValues
        {
            public string name;
            public string description;
            public int level;
            public int maxLevel;
            public List<string> tags;
            public Dictionary<string, Modifier.IndexIncrease> indexIncreaseByStatID;
        }
#pragma warning restore CS0649

        private const string StarSignModifiersPath = "Assets/Tables/ModifiersStarSign.json";
        private const string ItemModifiersPath = "Assets/Tables/ModifiersItem.json";

        public static Dictionary<int, StarSignModifier> StarSignBonuses { get; private set; } = new();
        public static Dictionary<int, ItemModifier> ItemBonuses { get; private set; } = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadGlobalModifiersBeforeScene()
        {
            LoadGlobalModifiers();
        }

        public static void LoadGlobalModifiers()
        {
            // These dictionaries are global table data, so they are loaded once before scene objects can create characters.
            StarSignBonuses = LoadModifiers<StarSignModifier>(StarSignModifiersPath, "star sign");
            ItemBonuses = LoadModifiers<ItemModifier>(ItemModifiersPath, "item");
        }

        public static void EnsureLoaded()
        {
            if (StarSignBonuses == null || StarSignBonuses.Count == 0 || ItemBonuses == null || ItemBonuses.Count == 0)
            {
                LoadGlobalModifiers();
            }
        }

        private static Dictionary<int, TModifier> LoadModifiers<TModifier>(string modifiersPath, string tableName)
            where TModifier : Modifier, new()
        {
            var resolvedPath = ResolveModifiersPath(modifiersPath);
            using var stream = File.OpenRead(resolvedPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, ModifierValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            var serializedModifiers = (Dictionary<string, ModifierValues>)serializer.ReadObject(stream);

            var modifiersByID = new Dictionary<int, TModifier>();
            foreach (var entry in serializedModifiers)
            {
                if (!int.TryParse(entry.Key, out var modifierID))
                {
                    throw new FormatException($"{tableName} modifier table key '{entry.Key}' is not a valid modifier ID.");
                }

                var modifier = CreateModifier<TModifier>(modifierID, entry.Value, tableName);
                if (modifiersByID.ContainsKey(modifierID))
                {
                    throw new InvalidOperationException($"{tableName} modifier table '{modifiersPath}' contains duplicate modifier ID {modifierID}.");
                }

                modifiersByID.Add(modifierID, modifier);
            }

            return modifiersByID;
        }

        private static TModifier CreateModifier<TModifier>(int modifierID, ModifierValues values, string tableName)
            where TModifier : Modifier, new()
        {
            if (modifierID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(modifierID), modifierID, "Modifier ID must be positive.");
            }

            if (values.maxLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(values), values.maxLevel, $"{tableName} modifier ID {modifierID} has an invalid max level.");
            }

            if (values.level < 0 || values.level > values.maxLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(values), values.level, $"{tableName} modifier ID {modifierID} has a level outside 0..maxLevel.");
            }

            var modifier = new TModifier
            {
                modifierID = modifierID,
                name = values.name,
                description = values.description,
                level = values.level,
                maxLevel = values.maxLevel,
                indexIncreaseByStatID = new Dictionary<int, Modifier.IndexIncrease>()
            };
            modifier.SetTags(values.tags);

            if (values.indexIncreaseByStatID == null)
            {
                return modifier;
            }

            foreach (var entry in values.indexIncreaseByStatID)
            {
                if (!int.TryParse(entry.Key, out var statID))
                {
                    throw new FormatException($"{tableName} modifier ID {modifierID} has stat key '{entry.Key}', which is not a valid stat ID.");
                }

                modifier.indexIncreaseByStatID.Add(statID, entry.Value);
            }

            return modifier;
        }

        private static string ResolveModifiersPath(string modifiersPath)
        {
            return TablePathResolver.Resolve(modifiersPath);
        }
    }
}
