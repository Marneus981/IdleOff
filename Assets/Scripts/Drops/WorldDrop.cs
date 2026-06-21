using IdleOff.Combat;
using IdleOff.Profiles;
using UnityEngine;

namespace IdleOff.Drops
{
    public sealed class WorldDrop : MonoBehaviour
    {
        [SerializeField] private WorldDropPayload payload;
        [SerializeField] private Sprite itemSprite;
        [SerializeField] private Sprite moneySprite;
        [SerializeField] private float despawnSeconds = 300f;
        private float ageSeconds;

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

        private void DestroyDrop()
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
                return;
            }

            DestroyImmediate(gameObject);
        }
    }
}
