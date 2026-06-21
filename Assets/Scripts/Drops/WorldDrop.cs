using IdleOff.Combat;
using IdleOff.Game;
using IdleOff.Profiles;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IdleOff.Drops
{
    public sealed class WorldDrop : MonoBehaviour
    {
        [SerializeField] private WorldDropPayload payload;
        [SerializeField] private Sprite itemSprite;
        [SerializeField] private Sprite moneySprite;
        [SerializeField] private float despawnSeconds = 300f;
        private float ageSeconds;
        private bool itemTooltipVisible;

        public WorldDropPayload Payload => payload;
        public event System.Action<WorldDrop> Collected;
        public event System.Action<WorldDrop> Expired;

        public void Initialize(WorldDropPayload dropPayload, Sprite itemSprite = null, Sprite moneySprite = null)
        {
            payload = dropPayload;
            this.itemSprite = itemSprite;
            this.moneySprite = moneySprite;
            ageSeconds = 0f;
            RefreshVisual();
        }

        public void SetDespawnSeconds(float seconds)
        {
            despawnSeconds = seconds;
            ageSeconds = 0f;
        }

        public void TickDespawn(float deltaTime)
        {
            if (despawnSeconds <= 0f || deltaTime <= 0f)
            {
                return;
            }

            ageSeconds += deltaTime;
            if (ageSeconds >= despawnSeconds)
            {
                Expired?.Invoke(this);
                DestroyDrop();
            }
        }

        private void Update()
        {
            TickDespawn(Time.deltaTime);
            UpdateItemTooltipHover();
        }

        public bool TryCollect(CharacterData character)
        {
            if (character == null || payload == null || payload.IsEmpty)
            {
                return false;
            }

            if (payload.isMoney)
            {
                if (!character.AddMoney(payload.money))
                {
                    return false;
                }

                Debug.Log($"[Pickup] Picked up money drop {payload.money.goldP}g {payload.money.silverP}s {payload.money.copperP}c.");
                Collected?.Invoke(this);
                DestroyDrop();
                return true;
            }

            if (!character.TryAddItem(payload.itemID, payload.quantity, out var leftover))
            {
                if (leftover > 0 && leftover < payload.quantity)
                {
                    Debug.Log($"[Pickup] Partially picked up item ID {payload.itemID}. Picked up {payload.quantity - leftover}, leftover {leftover}.");
                    payload.quantity = leftover;
                }

                return false;
            }

            Debug.Log($"[Pickup] Picked up item drop ID {payload.itemID} x{payload.quantity}.");
            Collected?.Invoke(this);
            DestroyDrop();
            return true;
        }

        private void OnMouseDown()
        {
            var player = FindFirstObjectByType<PlayerCombatant>();
            if (player != null)
            {
                TryCollect(player.Character);
            }
        }

        private void OnMouseEnter()
        {
            ShowItemTooltipAtDropPosition();
        }

        private void OnMouseExit()
        {
            HideItemTooltip();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerCombatant>();
            if (player != null)
            {
                TryCollect(player.Character);
            }
        }

        private void RefreshVisual()
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = payload != null && payload.isMoney
                ? moneySprite
                : itemSprite != null ? itemSprite : ItemIconResolver.GetIcon(payload?.itemID ?? 0);
            renderer.color = payload != null && payload.isMoney
                ? new Color32(240, 190, 60, 255)
                : new Color32(110, 180, 255, 255);
            gameObject.name = payload != null && payload.isMoney ? "Money Drop" : "Item Drop";
        }

        private bool TryCreateTooltipItem(out Item item)
        {
            item = null;
            if (payload == null || payload.isMoney || payload.itemID <= 0)
            {
                return false;
            }

            GlobalItemCatalog.EnsureLoaded();
            if (!GlobalItemCatalog.Items.TryGetValue(payload.itemID, out var template))
            {
                return false;
            }

            item = template.Clone(payload.quantity);
            return true;
        }

        private void UpdateItemTooltipHover()
        {
            if (payload == null || payload.isMoney)
            {
                HideItemTooltip();
                return;
            }

            var mouse = Mouse.current;
            var camera = Camera.main;
            if (mouse == null || camera == null)
            {
                HideItemTooltip();
                return;
            }

            var screenPosition = mouse.position.ReadValue();
            var worldPosition = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -camera.transform.position.z));
            var isHovering = IsWorldPositionOverDrop(worldPosition);
            if (!isHovering)
            {
                HideItemTooltip();
                return;
            }

            if (TryCreateTooltipItem(out var item))
            {
                itemTooltipVisible = true;
                ItemInfoTooltip.Show(this, item, screenPosition);
            }
        }

        private bool IsWorldPositionOverDrop(Vector2 worldPosition)
        {
            foreach (var collider in GetComponents<Collider2D>())
            {
                if (collider != null && collider.enabled && collider.OverlapPoint(worldPosition))
                {
                    return true;
                }
            }

            return false;
        }

        private void ShowItemTooltipAtDropPosition()
        {
            if (!TryCreateTooltipItem(out var item))
            {
                return;
            }

            var camera = Camera.main;
            var screenPosition = camera != null
                ? (Vector2)camera.WorldToScreenPoint(transform.position)
                : Vector2.zero;
            itemTooltipVisible = true;
            ItemInfoTooltip.Show(this, item, screenPosition);
        }

        private void HideItemTooltip()
        {
            if (!itemTooltipVisible)
            {
                return;
            }

            itemTooltipVisible = false;
            ItemInfoTooltip.Hide(this);
        }

        private void DestroyDrop()
        {
            HideItemTooltip();
            if (Application.isPlaying)
            {
                Destroy(gameObject);
                return;
            }

            DestroyImmediate(gameObject);
        }
    }
}
