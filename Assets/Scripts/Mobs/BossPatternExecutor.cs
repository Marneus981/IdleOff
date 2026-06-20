using System.Collections.Generic;
using IdleOff.Actions;
using IdleOff.Maps;
using UnityEngine;

namespace IdleOff.Mobs
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MobEntity), typeof(ActionController))]
    public sealed class BossPatternExecutor : MonoBehaviour
    {
        private readonly List<BossActionPattern> patterns = new();
        private readonly Dictionary<int, float> cooldownsByPatternID = new();
        private MobEntity mob;
        private ActionController actionController;
        private Transform target;
        private BossActionPattern activePattern;
        private int stepIndex;
        private float stepDelayRemaining;
        private int repeatIndex;
        private float repeatRemaining;

        public bool IsExecuting => activePattern != null;
        public Transform Target => target;
        public int LoadedPatternCount => patterns.Count;

        private void Awake()
        {
            mob = GetComponent<MobEntity>();
            actionController = GetComponent<ActionController>();
            LoadPatternsForMob();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void SetTarget(Transform nextTarget)
        {
            target = nextTarget;
        }

        public void LoadPatternsForMob()
        {
            patterns.Clear();
            if (mob == null)
            {
                mob = GetComponent<MobEntity>();
            }

            if (mob == null || mob.Template.bossPatternIDs == null)
            {
                return;
            }

            BossPatternCatalog.EnsureLoaded();
            foreach (var patternID in mob.Template.bossPatternIDs)
            {
                if (BossPatternCatalog.Patterns.TryGetValue(patternID, out var pattern))
                {
                    patterns.Add(pattern.Clone());
                }
            }
        }

        public void Tick(float deltaTime)
        {
            var step = Mathf.Max(0f, deltaTime);
            TickCooldowns(step);
            if (mob == null || !mob.IsAlive)
            {
                activePattern = null;
                return;
            }

            if (activePattern == null)
            {
                TryStartReadyPattern();
            }

            if (activePattern != null)
            {
                TickActivePattern(step);
            }
        }

        private void TickCooldowns(float deltaTime)
        {
            if (cooldownsByPatternID.Count == 0)
            {
                return;
            }

            var keys = new List<int>(cooldownsByPatternID.Keys);
            foreach (var key in keys)
            {
                cooldownsByPatternID[key] = Mathf.Max(0f, cooldownsByPatternID[key] - deltaTime);
            }
        }

        private void TryStartReadyPattern()
        {
            if (target == null || !IsTargetInAggroRange())
            {
                return;
            }

            foreach (var pattern in patterns)
            {
                if (pattern.steps == null || pattern.steps.Count == 0)
                {
                    continue;
                }

                if (cooldownsByPatternID.TryGetValue(pattern.patternID, out var cooldown) && cooldown > 0f)
                {
                    continue;
                }

                StartPattern(pattern);
                return;
            }
        }

        private bool IsTargetInAggroRange()
        {
            if (target == null)
            {
                return false;
            }

            var aggroRange = mob.Template.aggroRange;
            return aggroRange <= 0f || Vector2.Distance(transform.position, target.position) <= aggroRange;
        }

        private void StartPattern(BossActionPattern pattern)
        {
            activePattern = pattern;
            stepIndex = 0;
            repeatIndex = 0;
            repeatRemaining = 0f;
            stepDelayRemaining = Mathf.Max(0f, activePattern.steps[0].delay);
            SpawnTelegraphForStep(activePattern.steps[0]);
        }

        private void TickActivePattern(float deltaTime)
        {
            if (stepIndex >= activePattern.steps.Count)
            {
                FinishPattern();
                return;
            }

            var step = activePattern.steps[stepIndex];
            if (stepDelayRemaining > 0f)
            {
                stepDelayRemaining -= deltaTime;
                if (stepDelayRemaining > 0f)
                {
                    return;
                }
            }

            if (repeatRemaining > 0f)
            {
                repeatRemaining -= deltaTime;
                if (repeatRemaining > 0f)
                {
                    return;
                }
            }

            ExecuteStep(step);
            repeatIndex++;
            if (repeatIndex < Mathf.Max(1, step.repeatCount))
            {
                repeatRemaining = Mathf.Max(0f, step.repeatInterval);
                return;
            }

            stepIndex++;
            repeatIndex = 0;
            repeatRemaining = 0f;
            if (stepIndex < activePattern.steps.Count)
            {
                stepDelayRemaining = Mathf.Max(0f, activePattern.steps[stepIndex].delay);
                SpawnTelegraphForStep(activePattern.steps[stepIndex]);
            }
        }

        private void FinishPattern()
        {
            cooldownsByPatternID[activePattern.patternID] = Mathf.Max(0f, activePattern.cooldown);
            if (activePattern.loop && IsTargetInAggroRange())
            {
                StartPattern(activePattern);
                return;
            }

            activePattern = null;
        }

        public bool ExecuteStep(BossPatternStep step)
        {
            if (step == null || actionController == null)
            {
                return false;
            }

            ActionCatalog.EnsureLoaded();
            if (!ActionCatalog.MobActions.TryGetValue(step.actionID, out var action))
            {
                return false;
            }

            var spawnedAny = false;
            var count = Mathf.Max(1, step.count);
            for (var i = 0; i < count; i++)
            {
                var origin = ResolveOrigin(step, i, count);
                var direction = ResolveDirection(step, i, count, origin);
                spawnedAny |= actionController.TryUseActionAt(action.Clone(), origin, direction, true);
            }

            return spawnedAny;
        }

        public Vector2 ResolveOrigin(BossPatternStep step, int index = 0, int count = 1)
        {
            var origin = step.originMode switch
            {
                BossPatternOriginMode.TargetPosition => target != null ? (Vector2)target.position : (Vector2)transform.position,
                BossPatternOriginMode.AnchorID => ResolveAnchor(step.anchorID, transform.position),
                BossPatternOriginMode.OffsetFromBoss => (Vector2)transform.position + step.offset,
                BossPatternOriginMode.OffsetFromTarget => target != null ? (Vector2)target.position + step.offset : (Vector2)transform.position + step.offset,
                _ => (Vector2)transform.position
            };

            if (step.spacing != 0f && count > 1)
            {
                var centeredIndex = index - (count - 1) * 0.5f;
                origin += Vector2.right * centeredIndex * step.spacing;
            }

            return origin;
        }

        public Vector2 ResolveDirection(BossPatternStep step, int index = 0, int count = 1, Vector2 origin = default)
        {
            var baseDirection = step.directionMode switch
            {
                BossPatternDirectionMode.FixedDirection => step.fixedDirection == Vector2.zero ? Vector2.right : step.fixedDirection.normalized,
                BossPatternDirectionMode.HorizontalLine => Vector2.right,
                BossPatternDirectionMode.VerticalLine => Vector2.up,
                _ => target != null ? ((Vector2)target.position - origin).normalized : Vector2.right
            };

            if (step.directionMode == BossPatternDirectionMode.Radial && count > 1)
            {
                return Rotate(Vector2.right, 360f / count * index);
            }

            if (step.directionMode == BossPatternDirectionMode.SpreadTowardTarget && count > 1)
            {
                var angle = Mathf.Lerp(-step.spreadAngle * 0.5f, step.spreadAngle * 0.5f, count == 1 ? 0.5f : index / (float)(count - 1));
                return Rotate(baseDirection == Vector2.zero ? Vector2.right : baseDirection, angle);
            }

            return baseDirection == Vector2.zero ? Vector2.right : baseDirection.normalized;
        }

        private void SpawnTelegraphForStep(BossPatternStep step)
        {
            var duration = step.telegraphDurationOverride;
            if (duration <= 0f)
            {
                ActionCatalog.EnsureLoaded();
                if (ActionCatalog.MobActions.TryGetValue(step.actionID, out var action))
                {
                    duration = Mathf.Max(action.telegraphDuration, action.areaDelay);
                }
            }

            if (duration <= 0f)
            {
                return;
            }

            var origin = ResolveOrigin(step);
            var size = Vector2.one;
            if (ActionCatalog.MobActions.TryGetValue(step.actionID, out var actionForSize))
            {
                size = actionForSize.size.sqrMagnitude > 0f
                    ? actionForSize.size
                    : Vector2.one * Mathf.Max(0.25f, actionForSize.radius > 0f ? actionForSize.radius * 2f : actionForSize.range);
            }

            BossTelegraph.Spawn(origin, size, duration);
        }

        private static Vector2 ResolveAnchor(string anchorID, Vector2 fallback)
        {
            return MapManager.Instance != null
                && !string.IsNullOrWhiteSpace(anchorID)
                && MapManager.Instance.TryGetAnchor(anchorID, out var anchor)
                ? anchor
                : fallback;
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var sin = Mathf.Sin(radians);
            var cos = Mathf.Cos(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos).normalized;
        }
    }
}
