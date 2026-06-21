using System.Linq;
using IdleOff.Profiles;
using UnityEngine;
using UnityEngine.UI;

namespace IdleOff.Game
{
    public sealed class ItemInfoTooltip : MonoBehaviour
    {
        private const float Width = 280f;
        private const float Height = 170f;
        private const float SlotPadding = 0.05f;
        private static ItemInfoTooltip instance;

        private RectTransform panel;
        private Text nameText;
        private Text typeText;
        private Text descriptionText;
        private Text valueText;
        private Object currentOwner;

        public static ItemInfoTooltip EnsureExists()
        {
            if (instance != null)
            {
                return instance;
            }

            var tooltipObject = new GameObject("Item Info Tooltip");
            DontDestroyOnLoad(tooltipObject);
            instance = tooltipObject.AddComponent<ItemInfoTooltip>();
            instance.Build();
            return instance;
        }

        public static void Show(Item item, Vector2 screenPosition)
        {
            Show(null, item, screenPosition);
        }

        public static void Show(Object owner, Item item, Vector2 screenPosition)
        {
            if (item == null)
            {
                Hide(owner);
                return;
            }

            var tooltip = EnsureExists();
            tooltip.currentOwner = owner;
            tooltip.SetItem(item);
            tooltip.SetScreenPosition(screenPosition);
            tooltip.panel.gameObject.SetActive(true);
        }

        public static void Move(Vector2 screenPosition)
        {
            if (instance != null && instance.panel != null && instance.panel.gameObject.activeSelf)
            {
                instance.SetScreenPosition(screenPosition);
            }
        }

        public static void Hide()
        {
            Hide(null);
        }

        public static void Hide(Object owner)
        {
            if (instance != null && instance.panel != null)
            {
                if (owner != null && instance.currentOwner != null && instance.currentOwner != owner)
                {
                    return;
                }

                instance.currentOwner = null;
                instance.panel.gameObject.SetActive(false);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (panel == null)
            {
                Build();
            }
        }

        private void Build()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            if (gameObject.GetComponent<CanvasScaler>() == null)
            {
                gameObject.AddComponent<CanvasScaler>();
            }

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            panel = new GameObject("Item Info Tooltip Panel").AddComponent<RectTransform>();
            panel.SetParent(transform, false);
            panel.sizeDelta = new Vector2(Width, Height);
            panel.pivot = new Vector2(0f, 1f);
            var background = panel.gameObject.AddComponent<Image>();
            background.color = new Color32(20, 22, 30, 245);
            background.raycastTarget = false;

            nameText = CreateRowText("Item Info Name", 0.75f, 1f, TextAnchor.MiddleLeft, 15);
            typeText = CreateRowText("Item Info Type", 0.55f, 0.75f, TextAnchor.MiddleLeft, 13);
            descriptionText = CreateRowText("Item Info Description", 0.25f, 0.55f, TextAnchor.UpperLeft, 12);
            valueText = CreateRowText("Item Info Value Quantity", 0f, 0.25f, TextAnchor.MiddleLeft, 13);
            panel.gameObject.SetActive(false);
        }

        private Text CreateRowText(string objectName, float minY, float maxY, TextAnchor alignment, int fontSize)
        {
            var row = new GameObject(objectName + " Row").AddComponent<RectTransform>();
            row.SetParent(panel, false);
            row.anchorMin = new Vector2(0f, minY);
            row.anchorMax = new Vector2(1f, maxY);
            row.offsetMin = Vector2.zero;
            row.offsetMax = Vector2.zero;

            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(row, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(SlotPadding, SlotPadding);
            textRect.anchorMax = new Vector2(1f - SlotPadding, 1f - SlotPadding);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private void SetItem(Item item)
        {
            nameText.text = string.IsNullOrWhiteSpace(item.name) ? $"Item {item.itemID}" : item.name;
            typeText.text = item.tags == null || item.tags.Count == 0
                ? "untagged"
                : string.Join(", ", item.tags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
            descriptionText.text = string.IsNullOrWhiteSpace(item.description) ? string.Empty : item.description;
            valueText.text = $"Value: {FormatMoney(item.unitPrice)}    Qty: {Mathf.Max(1, item.quantity)}";
        }

        private void SetScreenPosition(Vector2 screenPosition)
        {
            var x = Mathf.Clamp(screenPosition.x + 16f, 0f, Mathf.Max(0f, Screen.width - Width));
            var y = Mathf.Clamp(screenPosition.y - 16f, Height, Mathf.Max(Height, Screen.height));
            panel.position = new Vector3(x, y, 0f);
        }

        private static string FormatMoney(Money money)
        {
            return $"{money.goldP}g {money.silverP}s {money.copperP}c";
        }
    }
}
