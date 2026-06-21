using System.Collections.Generic;
using IdleOff.Combat;
using UnityEngine;

namespace IdleOff.Actions
{
    public sealed class MeleeActionHitbox : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float lifetime = 0.08f;
        [SerializeField] private Vector2 fallbackSize = new(1.25f, 1f);

        private readonly HashSet<ICombatant> hitTargets = new();
        private ActionUseRequest request;
        private float timer;

        public void Initialize(ActionUseRequest actionRequest)
        {
            request = actionRequest;
            timer = lifetime;
            ActionRuntimeRegistry.Register(gameObject);
            ResolveImmediateHits();
        }

        private void OnDestroy()
        {
            ActionRuntimeRegistry.Unregister(gameObject);
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                ActionRuntimeRegistry.DestroyRuntimeObject(gameObject);
            }
        }

        private void ResolveImmediateHits()
        {
            if (request.Owner == null || request.Action == null)
            {
                return;
            }

            var direction = request.Direction.x < 0f ? Vector2.left : Vector2.right;
            var range = Mathf.Max(0.01f, request.Action.range);
            var size = new Vector2(Mathf.Max(fallbackSize.x, range), fallbackSize.y);
            var center = (Vector2)transform.position + new Vector2(direction.x * size.x * 0.5f, 0f);
            var hits = Physics2D.OverlapBoxAll(center, size, 0f, request.TargetLayerMask);

            foreach (var hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                TryResolveHit(hit, center);
            }
        }

        private void TryResolveHit(Collider2D collider, Vector2 hitboxCenter)
        {
            var impactPoint = collider != null ? collider.ClosestPoint(hitboxCenter) : hitboxCenter;
            ActionHitResolver.TryResolveHit(request, collider, hitTargets, impactPoint);
        }

        private void OnDrawGizmosSelected()
        {
            if (request.Action == null)
            {
                return;
            }

            var direction = request.Direction.x < 0f ? Vector2.left : Vector2.right;
            var size = new Vector2(Mathf.Max(fallbackSize.x, request.Action.range), fallbackSize.y);
            var center = (Vector2)transform.position + new Vector2(direction.x * size.x * 0.5f, 0f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
