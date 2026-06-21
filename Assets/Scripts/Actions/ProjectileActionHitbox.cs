using System.Collections.Generic;
using IdleOff.Visuals;
using UnityEngine;

namespace IdleOff.Actions
{
    public sealed class ProjectileActionHitbox : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float fallbackRadius = 0.15f;

        private readonly HashSet<IdleOff.Combat.ICombatant> hitTargets = new();
        private ActionUseRequest request;
        private float lifetimeRemaining;
        private int piercesRemaining;
        private bool initialized;
        private bool destroyed;

        public Vector2 Direction => request.Direction;
        public float LifetimeRemaining => lifetimeRemaining;

        public void Initialize(ActionUseRequest actionRequest)
        {
            request = actionRequest;
            lifetimeRemaining = Mathf.Max(0.01f, request.Action.projectileLifetime);
            piercesRemaining = Mathf.Max(0, request.Action.projectilePierceCount);
            initialized = true;
            EnsureVisual();
            ActionRuntimeRegistry.Register(gameObject);
        }

        private void OnDestroy()
        {
            ActionRuntimeRegistry.Unregister(gameObject);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (!initialized || request.Owner == null || request.Action == null)
            {
                DestroySelf();
                return;
            }

            var previousPosition = (Vector2)transform.position;
            var step = Mathf.Max(0f, deltaTime);
            var distance = request.Action.projectileSpeed * step;
            var nextPosition = previousPosition + request.Direction * distance;

            if (CollidesWithBlockingWorld(previousPosition, request.Direction, distance))
            {
                DestroySelf();
                return;
            }

            transform.position = nextPosition;
            ResolveTargetHits();
            if (destroyed)
            {
                return;
            }

            lifetimeRemaining -= step;
            if (lifetimeRemaining <= 0f)
            {
                DestroySelf();
            }
        }

        private bool CollidesWithBlockingWorld(Vector2 origin, Vector2 direction, float distance)
        {
            if (distance <= 0f)
            {
                return false;
            }

            var hits = Physics2D.CircleCastAll(origin, GetRadius(), direction, distance);
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger || ActionHitResolver.FindCombatant(hit.collider) != null)
                {
                    continue;
                }

                if (IsPassThroughBoundary(hit.collider.gameObject))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool IsPassThroughBoundary(GameObject target)
        {
            return target != null
                && (target.name.Contains("Boundary Left Wall")
                    || target.name.Contains("Boundary Right Wall")
                    || target.name.Contains("Boundary Ceiling"));
        }

        private void ResolveTargetHits()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, GetRadius(), request.TargetLayerMask);
            foreach (var hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                if (!ActionHitResolver.TryResolveHit(request, hit, hitTargets))
                {
                    continue;
                }

                if (piercesRemaining <= 0)
                {
                    DestroySelf();
                    return;
                }

                piercesRemaining--;
            }
        }

        private float GetRadius()
        {
            return Mathf.Max(fallbackRadius, request.Action.radius > 0f ? request.Action.radius : request.Action.range * 0.1f);
        }

        private void EnsureVisual()
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (renderer.sprite == null)
            {
                renderer.sprite = CreateRuntimeSprite();
            }

            renderer.color = request.Owner != null && request.Owner.IsPlayerControlled
                ? new Color32(80, 180, 255, 255)
                : new Color32(255, 100, 80, 255);
            renderer.sortingOrder = 20;
            var visual = GetComponent<EntityVisualController>();
            if (visual == null)
            {
                visual = gameObject.AddComponent<EntityVisualController>();
            }

            visual.ApplyVisual(request.Action.projectileVisualID, VisualAssetResolver.ProjectilePlaceholderPath);
            transform.localScale = Vector3.one * GetRadius() * 2f;
        }

        private static Sprite CreateRuntimeSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private void DestroySelf()
        {
            if (destroyed)
            {
                return;
            }

            destroyed = true;
            ActionRuntimeRegistry.DestroyRuntimeObject(gameObject);
        }
    }
}
