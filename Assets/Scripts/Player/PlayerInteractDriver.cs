using IdleOff.Combat;
using IdleOff.Controls;
using IdleOff.Interactables;
using UnityEngine;

namespace IdleOff.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCombatant))]
    public sealed class PlayerInteractDriver : MonoBehaviour
    {
        private PlayerCombatant player;

        private void Awake()
        {
            player = GetComponent<PlayerCombatant>();
        }

        private void Update()
        {
            if (!KeybindManager.WasPressedThisFrame(KeybindActions.Interact))
            {
                return;
            }

            var nearest = FindNearestInteractable();
            nearest?.TryInteract(player);
        }

        private InteractableObjectEntity FindNearestInteractable()
        {
            InteractableObjectEntity nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var interactable in InteractableObjectEntity.All)
            {
                if (interactable == null || !interactable.CanInteract(player))
                {
                    continue;
                }

                var distance = Vector2.Distance(transform.position, interactable.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = interactable;
                }
            }

            return nearest;
        }
    }
}
