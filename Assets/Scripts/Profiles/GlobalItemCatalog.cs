using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using UnityEngine;

namespace IdleOff.Profiles
{
    public static class GlobalItemCatalog
    {
        [Serializable]
#pragma warning disable CS0649
        private struct ItemValues
        {
            public int itemID;
            public string Name;
            public string Description;
            public string iconPath;
            public List<string> tags;
            public int quantity;
            public int maxStack;
            public List<int> unitPrice;
            public int modifier;
        }
#pragma warning restore CS0649

        private const string ItemsPath = "Assets/Tables/Items.json";

        public static Dictionary<int, Item> Items { get; private set; } = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadItemsBeforeScene()
        {
            LoadItems();
        }

        public static void EnsureLoaded()
        {
            if (Items == null || Items.Count == 0)
            {
                LoadItems();
            }
        }

        public static void LoadItems()
        {
            // Item definitions are global table data; bags clone these templates when storing actual stacks.
            var resolvedPath = ResolveItemsPath(ItemsPath);
            using var stream = File.OpenRead(resolvedPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, ItemValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            var serializedItems = (Dictionary<string, ItemValues>)serializer.ReadObject(stream);

            Items = new Dictionary<int, Item>();
            foreach (var entry in serializedItems)
            {
                if (!int.TryParse(entry.Key, out var itemID))
                {
                    throw new FormatException($"Item table key '{entry.Key}' is not a valid item ID.");
                }

                var item = CreateItem(itemID, entry.Value);
                if (Items.ContainsKey(item.itemID))
                {
                    throw new InvalidOperationException($"Item table contains duplicate item ID {item.itemID}.");
                }

                Items.Add(item.itemID, item);
            }
        }

        private static Item CreateItem(int itemID, ItemValues values)
        {
            if (itemID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemID), itemID, "Item ID must be positive.");
            }

            if (values.itemID != 0 && values.itemID != itemID)
            {
                throw new InvalidOperationException($"Item table key {itemID} does not match itemID field {values.itemID}.");
            }

            return new Item
            {
                itemID = itemID,
                name = values.Name,
                description = values.Description,
                iconPath = string.IsNullOrWhiteSpace(values.iconPath) ? ItemIconResolver.DefaultItemIconPath : values.iconPath,
                tags = values.tags ?? new List<string>(),
                quantity = Math.Max(0, values.quantity),
                maxStack = Math.Max(1, values.maxStack == 0 ? 1 : values.maxStack),
                unitPrice = CreateMoney(values.unitPrice),
                modifier = values.modifier
            };
        }

        private static Money CreateMoney(List<int> values)
        {
            if (values == null || values.Count == 0)
            {
                return new Money();
            }

            if (values.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(values), values.Count, "unitPrice must contain gold, silver, and copper values.");
            }

            return new Money(values[0], values[1], values[2]);
        }

        private static string ResolveItemsPath(string itemsPath)
        {
            if (Path.IsPathRooted(itemsPath))
            {
                return itemsPath;
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), itemsPath));
        }
    }
}
