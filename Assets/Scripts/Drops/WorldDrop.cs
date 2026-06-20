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

        public WorldDropPayload Payload => payload;
        public event System.Action<WorldDrop> Collected;

        public void Initialize(WorldDropPayload dropPayload, Sprite itemSprite = null, Sprite moneySprite = null)
        {
            payload = dropPayload;
            this.itemSprite = itemSprite;
            this.moneySprite = moneySprite;
            RefreshVisual();
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

                Collected?.Invoke(this);
                DestroyDrop();
                return true;
            }

            if (!character.TryAddItem(payload.itemID, payload.quantity, out var leftover))
            {
                if (leftover > 0 && leftover < payload.quantity)
                {
                    payload.quantity = leftover;
                }

                return false;
            }

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

            renderer.sprite = payload != null && payload.isMoney ? moneySprite : itemSprite;
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
