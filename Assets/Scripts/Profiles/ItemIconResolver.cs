using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IdleOff.Profiles
{
    public static class ItemIconResolver
    {
        public const string DefaultItemIconPath = "Assets/Art/Placeholders/Item_Drop.png";

        private static readonly Dictionary<string, Sprite> SpriteCache = new();
        private static Sprite fallbackSprite;

        public static Sprite GetIcon(Item item)
        {
            return item == null ? GetFallbackIcon() : GetIcon(item.iconPath);
        }

        public static Sprite GetIcon(int itemID)
        {
            GlobalItemCatalog.EnsureLoaded();
            return GlobalItemCatalog.Items.TryGetValue(itemID, out var item)
                ? GetIcon(item)
                : GetFallbackIcon();
        }

        public static Sprite GetIcon(string iconPath)
        {
            var resolvedPath = string.IsNullOrWhiteSpace(iconPath) ? DefaultItemIconPath : iconPath;
            if (SpriteCache.TryGetValue(resolvedPath, out var cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            var sprite = LoadSprite(resolvedPath);
            if (sprite == null)
            {
                sprite = GetFallbackIcon();
            }

            SpriteCache[resolvedPath] = sprite;
            return sprite;
        }

        private static Sprite LoadSprite(string iconPath)
        {
#if UNITY_EDITOR
            if (iconPath.StartsWith("Assets/"))
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            }
#endif
            return Resources.Load<Sprite>(iconPath);
        }

        private static Sprite GetFallbackIcon()
        {
            if (fallbackSprite != null)
            {
                return fallbackSprite;
            }

#if UNITY_EDITOR
            fallbackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultItemIconPath);
            if (fallbackSprite != null)
            {
                return fallbackSprite;
            }
#endif
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.SetPixel(0, 0, new Color32(110, 180, 255, 255));
            texture.Apply();
            fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return fallbackSprite;
        }
    }
}
