using System.Collections.Generic;
using IdleOff.Combat;
using UnityEngine;

namespace IdleOff.Actions
{
    [DisallowMultipleComponent]
    public sealed class ActionController : MonoBehaviour
    {
        [SerializeField] private LayerMask targetLayerMask = ~0;
        [SerializeField] private MeleeActionHitbox meleeHitboxPrefab;
        [SerializeField] private ProjectileActionHitbox projectileHitboxPrefab;
        [SerializeField] private AreaActionHitbox areaHitboxPrefab;

        private readonly Dictionary<int, ActionRuntimeState> statesByActionID = new();
        private ICombatant owner;

        private void Awake()
        {
            owner = GetComponent<ICombatant>();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            foreach (var state in statesByActionID.Values)
            {
                state.Tick(Mathf.Max(0f, deltaTime));
            }
        }

        public float GetCooldownRemaining(Action action)
        {
            return action == null ? 0f : GetState(action).CooldownRemaining;
        }

        public bool CanUseAction(Action action)
        {
            return owner != null
                && owner.IsAlive
                && action != null
                && GetState(action).IsReady;
        }

        public bool TryUseAction(Action action, Vector2 direction)
        {
            return TryUseActionAt(action, transform.position, direction);
        }

        public bool TryUseActionAt(Action action, Vector2 origin, Vector2 direction, bool ignoreCooldown = false)
        {
            if (!CanUseAction(action))
            {
                if (!ignoreCooldown || owner == null || !owner.IsAlive || action == null)
                {
                    return false;
                }
            }

            var request = new ActionUseRequest(owner, action, origin, direction, targetLayerMask);
            Execute(request);
            if (!ignoreCooldown)
            {
                GetState(action).StartCooldown();
            }

            return true;
        }

        private void Execute(ActionUseRequest request)
        {
            switch (request.Action.hitboxType)
            {
                case ActionHitboxType.Box:
                case ActionHitboxType.Circle:
                    SpawnMeleeHitbox(request);
                    break;
                case ActionHitboxType.Projectile:
                    SpawnProjectileHitbox(request);
                    break;
                case ActionHitboxType.Area:
                    SpawnAreaHitbox(request);
                    break;
                default:
                    SpawnMeleeHitbox(request);
                    break;
            }
        }

        private void SpawnMeleeHitbox(ActionUseRequest request)
        {
            var hitbox = meleeHitboxPrefab != null
                ? Instantiate(meleeHitboxPrefab)
                : CreateFallbackMeleeHitbox();
            hitbox.transform.position = request.Origin;
            hitbox.Initialize(request);
        }

        private void SpawnProjectileHitbox(ActionUseRequest request)
        {
            var hitbox = projectileHitboxPrefab != null
                ? Instantiate(projectileHitboxPrefab)
                : CreateFallbackProjectileHitbox();
            hitbox.transform.position = request.Origin;
            hitbox.Initialize(request);
        }

        private void SpawnAreaHitbox(ActionUseRequest request)
        {
            var hitbox = areaHitboxPrefab != null
                ? Instantiate(areaHitboxPrefab)
                : CreateFallbackAreaHitbox();
            hitbox.transform.position = request.Origin;
            hitbox.Initialize(request);
        }

        private static MeleeActionHitbox CreateFallbackMeleeHitbox()
        {
            var hitboxObject = new GameObject("Melee Action Hitbox");
            return hitboxObject.AddComponent<MeleeActionHitbox>();
        }

        private static ProjectileActionHitbox CreateFallbackProjectileHitbox()
        {
            var hitboxObject = new GameObject("Projectile Action Hitbox");
            return hitboxObject.AddComponent<ProjectileActionHitbox>();
        }

        private static AreaActionHitbox CreateFallbackAreaHitbox()
        {
            var hitboxObject = new GameObject("Area Action Hitbox");
            return hitboxObject.AddComponent<AreaActionHitbox>();
        }

        private ActionRuntimeState GetState(Action action)
        {
            if (!statesByActionID.TryGetValue(action.actionID, out var state))
            {
                state = new ActionRuntimeState(action);
                statesByActionID.Add(action.actionID, state);
            }

            return state;
        }
    }
}
