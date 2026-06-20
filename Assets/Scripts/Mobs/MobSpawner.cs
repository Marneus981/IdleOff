using System.Collections.Generic;
using IdleOff.Actions;
using IdleOff.Maps;
using UnityEngine;

namespace IdleOff.Mobs
{
    public sealed class MobSpawner : MonoBehaviour
    {
        [SerializeField] private int mobID;
        [SerializeField] private int maxActive = 1;
        [SerializeField] private float respawnSeconds = 5f;
        [SerializeField] private Sprite mobSprite;

        private readonly List<MobEntity> activeMobs = new();
        private float respawnTimer;

        public void Initialize(MapMobSpawnerDefinition definition, Sprite sprite)
        {
            mobID = definition.mobID;
            maxActive = Mathf.Max(1, definition.maxActive);
            respawnSeconds = Mathf.Max(0f, definition.respawnSeconds);
            mobSprite = sprite;
            SpawnMissingMobs();
        }

        private void Update()
        {
            activeMobs.RemoveAll(mob => mob == null || !mob.IsAlive);
            if (activeMobs.Count >= maxActive)
            {
                return;
            }

            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                SpawnMissingMobs();
                respawnTimer = respawnSeconds;
            }
        }

        private void SpawnMissingMobs()
        {
            while (activeMobs.Count < maxActive)
            {
                activeMobs.Add(SpawnMob());
            }
        }

        private MobEntity SpawnMob()
        {
            MobCatalog.EnsureLoaded();
            if (!MobCatalog.Mobs.TryGetValue(mobID, out var template))
            {
                throw new KeyNotFoundException($"Mob ID {mobID} was not found.");
            }

            var mobObject = new GameObject("Mob - " + mobID);
            mobObject.transform.SetParent(transform);
            mobObject.transform.position = transform.position;
            mobObject.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

            var renderer = mobObject.AddComponent<SpriteRenderer>();
            renderer.sprite = mobSprite;
            renderer.color = new Color32(232, 90, 86, 255);
            mobObject.AddComponent<BoxCollider2D>();

            var body = mobObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;

            var mob = mobObject.AddComponent<MobEntity>();
            mob.Initialize(template);
            mob.Died += HandleMobDied;

            mobObject.AddComponent<ActionController>();
            mobObject.AddComponent<MobActionController>();
            AddAI(mobObject, template.mobType);
            return mob;
        }

        private void HandleMobDied(MobEntity mob)
        {
            MapManager.Instance?.RecordMobKilled(mob);
            activeMobs.Remove(mob);
            respawnTimer = respawnSeconds;
            if (mob != null)
            {
                Destroy(mob.gameObject);
            }
        }

        private static void AddAI(GameObject mobObject, MobType mobType)
        {
            switch (mobType)
            {
                case MobType.Miniboss:
                    mobObject.AddComponent<MobMinibossAI>();
                    break;
                case MobType.Boss:
                    mobObject.AddComponent<MobBossAI>();
                    break;
                default:
                    mobObject.AddComponent<MobBasicAI>();
                    break;
            }
        }
    }
}
