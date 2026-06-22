using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IdleOff.Visuals
{
    public static class VisualAssetResolver
    {
        public const string PlayerPlaceholderPath = "Assets/Art/Placeholders/Player_Box.png";
        public const string MobPlaceholderPath = "Assets/Art/Placeholders/Mob_Box.png";
        public const string PortalClosedPlaceholderPath = "Assets/Art/Placeholders/Portal_Closed.png";
        public const string PortalOpenPlaceholderPath = "Assets/Art/Placeholders/Portal_Open.png";
        public const string ProjectilePlaceholderPath = "Assets/Art/Sprites/Projectiles/Arrow/Static.png";

        private static readonly Dictionary<string, Sprite> SpriteCache = new();
        private static readonly Dictionary<string, Sprite[]> FrameCache = new();
        private static Sprite fallbackSprite;

        public static Sprite GetSprite(string spritePath, string fallbackPath = PlayerPlaceholderPath)
        {
            var resolvedPath = string.IsNullOrWhiteSpace(spritePath) ? fallbackPath : spritePath;
            if (SpriteCache.TryGetValue(resolvedPath, out var cached) && cached != null)
            {
                return cached;
            }

            var sprite = LoadSprite(resolvedPath);
            if (sprite == null && resolvedPath != fallbackPath)
            {
                sprite = LoadSprite(fallbackPath);
            }

            if (sprite == null)
            {
                sprite = GetGeneratedFallbackSprite();
            }

            SpriteCache[resolvedPath] = sprite;
            return sprite;
        }

        public static Sprite[] GetAnimationFrames(VisualAnimationDefinition animation, string fallbackSpritePath)
        {
            if (animation == null)
            {
                return Array.Empty<Sprite>();
            }

            if (animation.frames != null && animation.frames.Count > 0)
            {
                var referencedFrames = new List<Sprite>();
                foreach (var frame in animation.frames)
                {
                    if (frame != null)
                    {
                        referencedFrames.Add(frame);
                    }
                }

                if (referencedFrames.Count > 0)
                {
                    return referencedFrames.ToArray();
                }
            }

            if (animation.framePaths != null && animation.framePaths.Count > 0)
            {
                var frames = new List<Sprite>();
                foreach (var framePath in animation.framePaths)
                {
                    var sprite = GetSprite(framePath, fallbackSpritePath);
                    if (sprite != null)
                    {
                        frames.Add(sprite);
                    }
                }

                return frames.ToArray();
            }

            if (string.IsNullOrWhiteSpace(animation.spriteSheetPath))
            {
                return Array.Empty<Sprite>();
            }

            return GetSpritesFromPath(animation.spriteSheetPath, fallbackSpritePath);
        }

        public static Sprite[] GetSpritesFromPath(string spritePath, string fallbackSpritePath)
        {
            var cacheKey = string.IsNullOrWhiteSpace(spritePath) ? fallbackSpritePath : spritePath;
            if (FrameCache.TryGetValue(cacheKey, out var cached) && cached != null && cached.Length > 0)
            {
                return cached;
            }

            var frames = LoadSprites(cacheKey);
            if ((frames == null || frames.Length == 0) && cacheKey != fallbackSpritePath)
            {
                frames = LoadSprites(fallbackSpritePath);
            }

            if (frames == null || frames.Length == 0)
            {
                frames = new[] { GetGeneratedFallbackSprite() };
            }

            FrameCache[cacheKey] = frames;
            return frames;
        }

        private static Sprite LoadSprite(string spritePath)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(spritePath) && spritePath.StartsWith("Assets/"))
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            }
#endif
            var sprite = Resources.Load<Sprite>(spritePath);
            return sprite != null ? sprite : LoadLooseSprite(spritePath);
        }

        private static Sprite[] LoadSprites(string spritePath)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrWhiteSpace(spritePath) && spritePath.StartsWith("Assets/"))
            {
                var sprites = new List<Sprite>();
                var main = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (main != null)
                {
                    sprites.Add(main);
                }

                foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(spritePath))
                {
                    if (asset is Sprite sprite)
                    {
                        sprites.Add(sprite);
                    }
                }

                sprites.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
                return sprites.ToArray();
            }
#endif
            var loaded = Resources.LoadAll<Sprite>(spritePath);
            if (loaded != null && loaded.Length > 0)
            {
                return loaded;
            }

            var looseSprite = LoadLooseSprite(spritePath);
            return looseSprite == null ? Array.Empty<Sprite>() : new[] { looseSprite };
        }

        private static Sprite LoadLooseSprite(string spritePath)
        {
            if (string.IsNullOrWhiteSpace(spritePath))
            {
                return null;
            }

            var resolvedPath = ResolveLooseAssetPath(spritePath);
            if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
            {
                return null;
            }

            var bytes = File.ReadAllBytes(resolvedPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static string ResolveLooseAssetPath(string assetPath)
        {
            if (Path.IsPathRooted(assetPath))
            {
                return assetPath;
            }

            var currentDirectoryPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
            if (File.Exists(currentDirectoryPath))
            {
                return currentDirectoryPath;
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static Sprite GetGeneratedFallbackSprite()
        {
            if (fallbackSprite != null)
            {
                return fallbackSprite;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return fallbackSprite;
        }
    }
}
