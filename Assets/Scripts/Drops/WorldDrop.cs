using IdleOff.Combat;
using IdleOff.Profiles;
using UnityEngine;

namespace IdleOff.Drops
{
    public sealed class WorldDrop : MonoBehaviour
    {
        [SerializeField] private WorldDropPayload payload;

        public WorldDropPayload Payload => payload;

        public void Initialize(WorldDropPayload dropPayload)
        {
            payload = dropPayload;
        }

        public bool TryCollect(CharacterData character)
        {
            if (character == null || payload == null || payload.IsEmpty)
            {
                return false;
            }

            if (payload.isMoney)
            {
                if (!character.AddMoney(payload.money))
                {
                    return false;
                }

                Destroy(gameObject);
                return true;
            }

            if (!character.TryAddItem(payload.itemID, payload.quantity, out var leftover))
            {
                if (leftover > 0 && leftover < payload.quantity)
                {
                    payload.quantity = leftover;
                }

                return false;
            }

            Destroy(gameObject);
            return true;
        }

        private void OnMouseDown()
        {
            var player = FindFirstObjectByType<PlayerCombatant>();
            if (player != null)
            {
                TryCollect(player.Character);
            }
        }
    }
}
