using IdleOff.Combat;
using UnityEngine;

namespace IdleOff.CameraTools
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;
        [SerializeField] private Vector2 offset;

        private Vector3 velocity;

        public void SetTarget(Transform nextTarget)
        {
            target = nextTarget;
        }

        public void SetOffset(Vector2 nextOffset)
        {
            offset = nextOffset;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = FindFirstObjectByType<PlayerCombatant>();
                if (player != null)
                {
                    target = player.transform;
                }
            }

            if (target == null)
            {
                return;
            }

            var desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            transform.position = smoothTime <= 0f
                ? desired
                : Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
