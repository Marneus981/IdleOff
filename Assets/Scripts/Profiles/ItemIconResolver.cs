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
        private const string GeneratedCatalogResourcePath = "Generated/ItemIconCatalog";

        private static readonly Dictionary<string, Sprite> SpriteCache = new();
        private static Dictionary<int, Sprite> SpriteByItemID;
        private static Dictionary<string, Sprite> SpriteByIconPath;
        private static Sprite fallbackSprite;

        public static Sprite GetIcon(Item item)
        {
            if (item == null)
            {
                return GetFallbackIcon();
            }

#if !UNITY_EDITOR
            if (TryGetGeneratedIcon(item.itemID, item.iconPath, out var generatedIcon))
            {
                return generatedIcon;
            }
#endif
            return GetIcon(item.iconPath);
        }

        public static Sprite GetIcon(int itemID)
        {
#if !UNITY_EDITOR
            if (TryGetGeneratedIcon(itemID, null, out var generatedIcon))
            {
                return generatedIcon;
            }
#endif
            GlobalItemCatalog.EnsureLoaded();
            return GlobalItemCatalog.Items.TryGetValue(itemID, out var item)
                ? GetIcon(item)
                : GetFallbackIcon();
        }

        public static Sprite GetIcon(string iconPath)
        {
            var resolvedPath = string.IsNullOrWhiteSpace(iconPath) ? DefaultItemIconPath : iconPath;
#if !UNITY_EDITOR
            if (TryGetGeneratedIcon(0, resolvedPath, out var generatedIcon))
            {
                return generatedIcon;
            }
#endif
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

        private static bool TryGetGeneratedIcon(int itemID, string iconPath, out Sprite sprite)
        {
            EnsureGeneratedCatalogLoaded();
            if (itemID > 0 && SpriteByItemID != null && SpriteByItemID.TryGetValue(itemID, out sprite) && sprite != null)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(iconPath) && SpriteByIconPath != null && SpriteByIconPath.TryGetValue(iconPath, out sprite) && sprite != null)
            {
                return true;
            }

            sprite = null;
            return false;
        }

        private static void EnsureGeneratedCatalogLoaded()
        {
            if (SpriteByItemID != null && SpriteByIconPath != null)
            {
                return;
            }

            SpriteByItemID = new Dictionary<int, Sprite>();
            SpriteByIconPath = new Dictionary<string, Sprite>();
            var catalog = Resources.Load<ItemIconCatalogAsset>(GeneratedCatalogResourcePath);
            if (catalog == null || catalog.Icons == null)
            {
                return;
            }

            foreach (var record in catalog.Icons)
            {
                if (record == null || record.icon == null)
                {
                    continue;
                }

                if (record.itemID > 0)
                {
                    SpriteByItemID[record.itemID] = record.icon;
                }

                if (!string.IsNullOrWhiteSpace(record.iconPath))
                {
                    SpriteByIconPath[record.iconPath] = record.icon;
                }
            }
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

#if !UNITY_EDITOR
            if (TryGetGeneratedIcon(0, DefaultItemIconPath, out fallbackSprite) && fallbackSprite != null)
            {
                return fallbackSprite;
            }
#endif
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
