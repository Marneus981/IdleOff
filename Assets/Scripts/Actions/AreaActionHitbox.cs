using System.Collections.Generic;
using IdleOff.Visuals;
using UnityEngine;

namespace IdleOff.Actions
{
    public sealed class AreaActionHitbox : MonoBehaviour
    {
        private readonly HashSet<IdleOff.Combat.ICombatant> hitTargetsThisTick = new();
        private ActionUseRequest request;
        private float telegraphRemaining;
        private float activeRemaining;
        private float tickRemaining;
        private bool active;
        private bool initialized;
        private bool oneShotResolved;

        public bool IsActive => active;
        public float TelegraphRemaining => telegraphRemaining;

        public void Initialize(ActionUseRequest actionRequest)
        {
            request = actionRequest;
            telegraphRemaining = Mathf.Max(request.Action.telegraphDuration, request.Action.areaDelay);
            activeRemaining = Mathf.Max(0f, request.Action.areaDuration);
            tickRemaining = 0f;
            initialized = true;
            RefreshVisual();
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

            var step = Mathf.Max(0f, deltaTime);
            if (!active)
            {
                telegraphRemaining -= step;
                if (telegraphRemaining > 0f)
                {
                    return;
                }

                active = true;
                RefreshVisual();
                tickRemaining = 0f;
            }

            if (request.Action.areaTickInterval <= 0f)
            {
                if (!oneShotResolved)
                {
                    ResolveTick();
                    oneShotResolved = true;
                }

                DestroySelf();
                return;
            }

            tickRemaining -= step;
            while (tickRemaining <= 0f)
            {
                ResolveTick();
                tickRemaining += request.Action.areaTickInterval;
                if (request.Action.areaTickInterval <= 0f)
                {
                    break;
                }
            }

            activeRemaining -= step;
            if (activeRemaining <= 0f)
            {
                DestroySelf();
            }
        }

        private void ResolveTick()
        {
            hitTargetsThisTick.Clear();
            var size = GetSize();
            var hits = request.Action.hitboxType == ActionHitboxType.Circle || request.Action.radius > 0f
                ? Physics2D.OverlapCircleAll(transform.position, GetRadius(), request.TargetLayerMask)
                : Physics2D.OverlapBoxAll(transform.position, size, 0f, request.TargetLayerMask);

            foreach (var hit in hits)
            {
                if (hit != null)
                {
                    ActionHitResolver.TryResolveHit(request, hit, hitTargetsThisTick);
                }
            }
        }

        private float GetRadius()
        {
            return Mathf.Max(0.1f, request.Action.radius > 0f ? request.Action.radius : request.Action.range);
        }

        private Vector2 GetSize()
        {
            return request.Action.size.sqrMagnitude > 0f
                ? new Vector2(Mathf.Max(0.1f, request.Action.size.x), Mathf.Max(0.1f, request.Action.size.y))
                : Vector2.one * Mathf.Max(0.1f, request.Action.range);
        }

        private void RefreshVisual()
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.color = active
                ? new Color32(255, 80, 60, 150)
                : new Color32(255, 220, 40, 90);
            var visualID = active ? request.Action.areaVisualID : request.Action.telegraphVisualID;
            if (!string.IsNullOrWhiteSpace(visualID))
            {
                var visual = GetComponent<EntityVisualController>();
                if (visual == null)
                {
                    visual = gameObject.AddComponent<EntityVisualController>();
                }

                visual.ApplyVisual(visualID, VisualAssetResolver.PortalClosedPlaceholderPath);
            }

            var size = GetSize();
            transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        private void DestroySelf()
        {
            ActionRuntimeRegistry.DestroyRuntimeObject(gameObject);
        }
    }
}
