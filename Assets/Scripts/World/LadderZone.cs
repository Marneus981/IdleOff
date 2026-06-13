using UnityEngine;

namespace IdleOff.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class LadderZone : MonoBehaviour
    {
        private void Reset()
        {
            Collider2D ladderCollider = GetComponent<Collider2D>();
            ladderCollider.isTrigger = true;
        }

        private void OnValidate()
        {
            Collider2D ladderCollider = GetComponent<Collider2D>();
            if (ladderCollider != null)
            {
                ladderCollider.isTrigger = true;
            }
        }
    }
}
