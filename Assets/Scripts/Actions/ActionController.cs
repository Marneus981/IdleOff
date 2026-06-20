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
            if (!CanUseAction(action))
            {
                return false;
            }

            var request = new ActionUseRequest(owner, action, direction, targetLayerMask);
            Execute(request);
            GetState(action).StartCooldown();
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
            hitbox.transform.position = transform.position;
            hitbox.Initialize(request);
        }

        private static MeleeActionHitbox CreateFallbackMeleeHitbox()
        {
            var hitboxObject = new GameObject("Melee Action Hitbox");
            return hitboxObject.AddComponent<MeleeActionHitbox>();
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
