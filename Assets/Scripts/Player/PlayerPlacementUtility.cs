using UnityEngine;

namespace IdleOff.Player
{
    public static class PlayerPlacementUtility
    {
        public static Vector2 GetPositionForFeetAt(GameObject playerObject, Vector2 feetPosition)
        {
            if (playerObject == null)
            {
                return feetPosition;
            }

            var collider = playerObject.GetComponent<Collider2D>();
            if (collider == null)
            {
                return feetPosition;
            }

            var localBottomCenter = collider.offset;
            switch (collider)
            {
                case BoxCollider2D box:
                    localBottomCenter.y -= box.size.y * 0.5f;
                    break;
                case CapsuleCollider2D capsule:
                    localBottomCenter.y -= capsule.size.y * 0.5f;
                    break;
                case CircleCollider2D circle:
                    localBottomCenter.y -= circle.radius;
                    break;
                default:
                    localBottomCenter = playerObject.transform.InverseTransformPoint(collider.bounds.center);
                    localBottomCenter.y -= collider.bounds.extents.y;
                    break;
            }

            return feetPosition - RotateAndScale(playerObject.transform, localBottomCenter);
        }

        public static void MoveFeetTo(GameObject playerObject, Vector2 feetPosition)
        {
            if (playerObject == null)
            {
                return;
            }

            var targetPosition = GetPositionForFeetAt(playerObject, feetPosition);
            playerObject.transform.position = targetPosition;

            var body = playerObject.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                return;
            }

            body.position = targetPosition;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        private static Vector2 RotateAndScale(Transform transform, Vector2 localPoint)
        {
            var scaled = new Vector3(
                localPoint.x * transform.lossyScale.x,
                localPoint.y * transform.lossyScale.y,
                0f);
            return transform.rotation * scaled;
        }
    }
}
