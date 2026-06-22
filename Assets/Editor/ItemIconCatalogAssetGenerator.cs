using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using IdleOff.Profiles;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IdleOff.Editor
{
    public sealed class ItemIconCatalogAssetGenerator : IPreprocessBuildWithReport
    {
        private const string ItemsPath = "Assets/Tables/Items.json";
        private const string GeneratedCatalogPath = "Assets/Resources/Generated/ItemIconCatalog.asset";

        public int callbackOrder => -90;

        public void OnPreprocessBuild(BuildReport report)
        {
            Generate();
        }

        [MenuItem("IdleOff/Generate Item Icon Catalog")]
        public static void Generate()
        {
            if (!File.Exists(ItemsPath))
            {
                Debug.LogWarning($"Item table source file was not found: {ItemsPath}");
                return;
            }

            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "Generated");

            var catalog = AssetDatabase.LoadAssetAtPath<ItemIconCatalogAsset>(GeneratedCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ItemIconCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, GeneratedCatalogPath);
            }

            catalog.SetIcons(ReadIconRecords());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static List<ItemIconRecord> ReadIconRecords()
        {
            using var stream = File.OpenRead(ItemsPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, ItemValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            var serializedItems = (Dictionary<string, ItemValues>)serializer.ReadObject(stream);

            var records = new List<ItemIconRecord>
            {
                new ItemIconRecord
                {
                    itemID = 0,
                    iconPath = ItemIconResolver.DefaultItemIconPath,
                    icon = LoadSprite(ItemIconResolver.DefaultItemIconPath)
                }
            };
            foreach (var entry in serializedItems)
            {
                if (!int.TryParse(entry.Key, out var itemID))
                {
                    throw new FormatException($"Item table key '{entry.Key}' is not a valid item ID.");
                }

                var iconPath = string.IsNullOrWhiteSpace(entry.Value.iconPath)
                    ? ItemIconResolver.DefaultItemIconPath
                    : entry.Value.iconPath;
                records.Add(new ItemIconRecord
                {
                    itemID = itemID,
                    iconPath = iconPath,
                    icon = LoadSprite(iconPath)
                });
            }

            records.Sort((left, right) => left.itemID.CompareTo(right.itemID));
            return records;
        }

        private static Sprite LoadSprite(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            var path = parent + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        [DataContract]
#pragma warning disable CS0649
        private sealed class ItemValues
        {
            [DataMember(IsRequired = false)]
            public string iconPath;
        }
#pragma warning restore CS0649
    }
}
