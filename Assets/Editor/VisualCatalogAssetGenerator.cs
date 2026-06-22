using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using IdleOff.Visuals;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IdleOff.Editor
{
    public sealed class VisualCatalogAssetGenerator : IPreprocessBuildWithReport
    {
        private const string VisualsPath = "Assets/Tables/Visuals.json";
        private const string GeneratedCatalogPath = "Assets/Resources/Generated/VisualCatalog.asset";

        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            Generate();
        }

        [MenuItem("IdleOff/Generate Visual Catalog")]
        public static void Generate()
        {
            if (!File.Exists(VisualsPath))
            {
                Debug.LogWarning($"Visual catalog source file was not found: {VisualsPath}");
                return;
            }

            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "Generated");

            var catalog = AssetDatabase.LoadAssetAtPath<VisualCatalogAsset>(GeneratedCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<VisualCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, GeneratedCatalogPath);
            }

            catalog.SetVisuals(ReadVisualRecords());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static List<VisualDefinitionRecord> ReadVisualRecords()
        {
            using var stream = File.OpenRead(VisualsPath);
            var serializer = new DataContractJsonSerializer(
                typeof(Dictionary<string, VisualValues>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            var serializedVisuals = (Dictionary<string, VisualValues>)serializer.ReadObject(stream);

            var records = new List<VisualDefinitionRecord>();
            foreach (var entry in serializedVisuals)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    throw new FormatException("Visual table contains an empty visual ID.");
                }

                records.Add(CreateRecord(entry.Key, entry.Value));
            }

            records.Sort((left, right) => string.Compare(left.visualID, right.visualID, StringComparison.Ordinal));
            return records;
        }

        private static VisualDefinitionRecord CreateRecord(string visualID, VisualValues values)
        {
            return new VisualDefinitionRecord
            {
                visualID = visualID,
                spritePath = values.spritePath,
                sprite = LoadSprite(values.spritePath),
                sortingLayer = string.IsNullOrWhiteSpace(values.sortingLayer) ? "Default" : values.sortingLayer,
                sortingOrder = values.sortingOrder,
                scale = CreateVector2(values.scale, Vector2.one),
                offset = CreateVector2(values.offset, Vector2.zero),
                defaultAnimation = string.IsNullOrWhiteSpace(values.defaultAnimation) ? "idle" : values.defaultAnimation,
                animations = CreateAnimationRecords(values.animations)
            };
        }

        private static List<VisualAnimationRecord> CreateAnimationRecords(Dictionary<string, AnimationValues> values)
        {
            var records = new List<VisualAnimationRecord>();
            if (values == null)
            {
                return records;
            }

            foreach (var entry in values)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value == null)
                {
                    continue;
                }

                records.Add(new VisualAnimationRecord
                {
                    animationID = entry.Key,
                    spriteSheetPath = entry.Value.spriteSheetPath,
                    framePaths = entry.Value.framePaths ?? new List<string>(),
                    frames = LoadAnimationFrames(entry.Value),
                    fps = entry.Value.fps <= 0f ? 8f : entry.Value.fps,
                    loop = entry.Value.loop
                });
            }

            records.Sort((left, right) => string.Compare(left.animationID, right.animationID, StringComparison.Ordinal));
            return records;
        }

        private static List<Sprite> LoadAnimationFrames(AnimationValues values)
        {
            var frames = new List<Sprite>();
            if (values.framePaths != null && values.framePaths.Count > 0)
            {
                foreach (var framePath in values.framePaths)
                {
                    var frame = LoadSprite(framePath);
                    if (frame != null)
                    {
                        frames.Add(frame);
                    }
                }

                return frames;
            }

            if (!string.IsNullOrWhiteSpace(values.spriteSheetPath))
            {
                frames.AddRange(LoadSpritesAtPath(values.spriteSheetPath));
            }

            return frames;
        }

        private static Sprite LoadSprite(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static List<Sprite> LoadSpritesAtPath(string path)
        {
            var sprites = new List<Sprite>();
            var main = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (main != null)
            {
                sprites.Add(main);
            }

            foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            sprites.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
            return sprites;
        }

        private static Vector2 CreateVector2(List<float> values, Vector2 fallback)
        {
            return values == null || values.Count < 2
                ? fallback
                : new Vector2(values[0], values[1]);
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
    }
}
