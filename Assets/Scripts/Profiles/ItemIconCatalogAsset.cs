using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Profiles
{
    [CreateAssetMenu(menuName = "IdleOff/Item Icon Catalog")]
    public sealed class ItemIconCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<ItemIconRecord> icons = new();

        public IReadOnlyList<ItemIconRecord> Icons => icons;

        public void SetIcons(List<ItemIconRecord> records)
        {
            icons = records ?? new List<ItemIconRecord>();
        }
    }

    [Serializable]
    public sealed class ItemIconRecord
    {
        public int itemID;
        public string iconPath;
        public Sprite icon;
    }
}
