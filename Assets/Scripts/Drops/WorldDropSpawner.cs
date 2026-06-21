using System.Collections.Generic;
using IdleOff.Combat;
using UnityEngine;

namespace IdleOff.Drops
{
    public sealed class WorldDropSpawner : MonoBehaviour
    {
        [SerializeField] private WorldDrop dropPrefab;
        [SerializeField] private float scatterRadius = 0.35f;
        [SerializeField] private float dropDespawnSeconds = 300f;
        [SerializeField] private Sprite itemDropSprite;
        [SerializeField] private Sprite moneyDropSprite;

        public static WorldDropSpawner Instance { get; private set; }
        public event System.Action<WorldDrop> DropSpawned;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Configure(Sprite itemSprite, Sprite moneySprite)
        {
            itemDropSprite = itemSprite;
            moneyDropSprite = moneySprite;
        }

        public void SpawnDrops(IEnumerable<WorldDropPayload> drops, Vector3 origin)
        {
            if (drops == null)
            {
                return;
            }

            foreach (var drop in drops)
            {
                if (drop == null || drop.IsEmpty)
                {
                    continue;
                }

                SpawnDrop(drop, origin);
            }
        }

        public WorldDrop SpawnDrop(WorldDropPayload payload, Vector3 origin)
        {
            return SpawnDrop(payload, origin, true);
        }

        public WorldDrop SpawnDrop(WorldDropPayload payload, Vector3 origin, bool notify)
        {
            return SpawnDrop(payload, origin, notify, true);
        }

        public WorldDrop SpawnDropAt(WorldDropPayload payload, Vector3 worldPosition)
        {
            return SpawnDropAt(payload, worldPosition, true);
        }

        public WorldDrop SpawnDropAt(WorldDropPayload payload, Vector3 worldPosition, bool notify)
        {
            return SpawnDrop(payload, worldPosition, notify, false);
        }

        private WorldDrop SpawnDrop(WorldDropPayload payload, Vector3 origin, bool notify, bool scatter)
        {
            var offset = new Vector3(
                Random.Range(-scatterRadius, scatterRadius),
                Random.Range(0f, scatterRadius),
                0f);
            var worldPosition = scatter ? origin + offset : origin;
            var drop = dropPrefab != null
                ? Instantiate(dropPrefab, worldPosition, Quaternion.identity)
                : CreateFallbackDrop(worldPosition);
            EnsureDropPhysics(drop);
            drop.Initialize(payload, itemDropSprite, moneyDropSprite);
            drop.SetDespawnSeconds(dropDespawnSeconds);
            if (notify)
            {
                DropSpawned?.Invoke(drop);
            }

            return drop;
        }

        private static WorldDrop CreateFallbackDrop(Vector3 position)
        {
            var dropObject = new GameObject("World Drop");
            dropObject.transform.position = position;
            var collider = dropObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = false;
            collider.radius = 0.25f;
            dropObject.AddComponent<SpriteRenderer>();
            return dropObject.AddComponent<WorldDrop>();
        }

        private static void EnsureDropPhysics(WorldDrop drop)
        {
            if (drop == null)
            {
                return;
            }

            var physicalCollider = GetOrCreatePhysicalCollider(drop);
            physicalCollider.isTrigger = false;
            IgnorePlayerCollisions(physicalCollider);

            var pickupTrigger = GetOrCreatePickupTrigger(drop, physicalCollider);
            pickupTrigger.isTrigger = true;

            var body = drop.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = drop.gameObject.AddComponent<Rigidbody2D>();
            }

            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 1f;
            body.freezeRotation = true;
            body.linearVelocity = Vector2.zero;
        }

        private static CircleCollider2D GetOrCreatePhysicalCollider(WorldDrop drop)
        {
            foreach (var collider in drop.GetComponents<CircleCollider2D>())
            {
                if (!collider.isTrigger)
                {
                    return collider;
                }
            }

            var physicalCollider = drop.gameObject.AddComponent<CircleCollider2D>();
            physicalCollider.radius = 0.25f;
            return physicalCollider;
        }

        private static CircleCollider2D GetOrCreatePickupTrigger(WorldDrop drop, CircleCollider2D physicalCollider)
        {
            foreach (var collider in drop.GetComponents<CircleCollider2D>())
            {
                if (collider.isTrigger)
                {
                    return collider;
                }
            }

            var trigger = drop.gameObject.AddComponent<CircleCollider2D>();
            trigger.radius = Mathf.Max(0.3f, physicalCollider.radius * 1.2f);
            return trigger;
        }

        private static void IgnorePlayerCollisions(Collider2D dropCollider)
        {
            if (dropCollider == null)
            {
                return;
            }

            foreach (var player in FindObjectsByType<PlayerCombatant>(FindObjectsSortMode.None))
            {
                foreach (var playerCollider in player.GetComponents<Collider2D>())
                {
                    if (playerCollider != null)
                    {
                        Physics2D.IgnoreCollision(dropCollider, playerCollider, true);
                    }
                }
            }
        }
    }
}
