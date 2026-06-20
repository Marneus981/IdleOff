using UnityEngine;

namespace IdleOff.Mobs
{
    public sealed class BossTelegraph : MonoBehaviour
    {
        private float lifetimeRemaining;

        public void Initialize(Vector2 position, Vector2 size, float lifetime, Color color)
        {
            transform.position = position;
            transform.localScale = new Vector3(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y), 1f);
            lifetimeRemaining = Mathf.Max(0f, lifetime);
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.color = color;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            lifetimeRemaining -= Mathf.Max(0f, deltaTime);
            if (lifetimeRemaining <= 0f)
            {
                DestroySelf();
            }
        }

        public static BossTelegraph Spawn(Vector2 position, Vector2 size, float lifetime)
        {
            var telegraphObject = new GameObject("Boss Telegraph");
            var telegraph = telegraphObject.AddComponent<BossTelegraph>();
            telegraph.Initialize(position, size, lifetime, new Color32(255, 70, 40, 90));
            return telegraph;
        }

        private void DestroySelf()
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
