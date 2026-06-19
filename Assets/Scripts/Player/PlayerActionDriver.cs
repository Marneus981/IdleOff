using IdleOff.Actions;
using IdleOff.Controls;
using IdleOff.Profiles;
using UnityEngine;
using GameAction = IdleOff.Actions.Action;

namespace IdleOff.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ActionController))]
    public sealed class PlayerActionDriver : MonoBehaviour
    {
        [SerializeField] private CharacterProfile profile;
        [SerializeField] private int selectedActionID;

        private ActionController actionController;
        private PlayerMovement2D movement;

        public void SetProfile(CharacterProfile characterProfile)
        {
            profile = characterProfile;
        }

        private void Awake()
        {
            actionController = GetComponent<ActionController>();
            movement = GetComponent<PlayerMovement2D>();
        }

        private void Update()
        {
            if (!KeybindManager.WasPressedThisFrame(KeybindActions.AttackPrimary))
            {
                return;
            }

            var action = GetSelectedAction();
            if (action != null)
            {
                actionController.TryUseAction(action, GetFacingDirection());
            }
        }

        private GameAction GetSelectedAction()
        {
            var character = profile != null ? profile.ActiveCharacter : null;
            if (character == null)
            {
                return null;
            }

            if (selectedActionID > 0)
            {
                return character.CharacterClass.GetAction(selectedActionID);
            }

            var actions = character.CharacterClass.GetClassActions();
            return actions.Count > 0 ? actions[0] : null;
        }

        private Vector2 GetFacingDirection()
        {
            return movement != null ? movement.FacingDirection : Vector2.right;
        }
    }
}
