using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Drops
{
    public sealed class WorldDropSpawner : MonoBehaviour
    {
        [SerializeField] private WorldDrop dropPrefab;
        [SerializeField] private float scatterRadius = 0.35f;

        public static WorldDropSpawner Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
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
            var offset = new Vector3(
                Random.Range(-scatterRadius, scatterRadius),
                Random.Range(0f, scatterRadius),
                0f);
            var worldPosition = origin + offset;
            var drop = dropPrefab != null
                ? Instantiate(dropPrefab, worldPosition, Quaternion.identity)
                : CreateFallbackDrop(worldPosition);
            drop.Initialize(payload);
            return drop;
        }

        private static WorldDrop CreateFallbackDrop(Vector3 position)
        {
            var dropObject = new GameObject("World Drop");
            dropObject.transform.position = position;
            var collider = dropObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.25f;
            return dropObject.AddComponent<WorldDrop>();
        }
    }
}
