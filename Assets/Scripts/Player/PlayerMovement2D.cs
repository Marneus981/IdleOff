using IdleOff.Controls;
using IdleOff.Maps;
using IdleOff.Profiles;
using IdleOff.World;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOff.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PlayerMovement2D : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField] private CharacterProfile profile;
        [SerializeField, Min(0f)] private float fallbackSpeed = 5f;

        [Header("Climbing")]
        [SerializeField, Min(0f)] private float climbSpeed = 4f;
        [SerializeField, Min(0f)] private float climbPlatformCheckDistance = 0.12f;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float jumpVelocity = 8f;
        [SerializeField, Min(0f)] private float groundCheckDistance = 0.08f;

        [Header("Drop Down")]
        [SerializeField, Min(0f)] private float platformCheckDistance = 0.08f;
        [SerializeField, Min(0.01f)] private float dropThroughSeconds = 0.35f;
        [SerializeField, Min(0f)] private float dropNudgeVelocity = 2.5f;

        [Header("Respawn")]
        [SerializeField] private float fallRespawnY = -8f;
        [SerializeField, Min(0f)] private float respawnSurfaceOffset = 0.03f;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private float defaultGravityScale;
        private int ladderContacts;
        private float horizontalInput;
        private float verticalInput;
        private float dropTimer;
        private Collider2D ignoredPlatform;
        private readonly List<Collider2D> climbIgnoredPlatforms = new();
        private Vector2 lastSafePosition;

        public Vector2 FacingDirection { get; private set; } = Vector2.right;

        private float CurrentSpeed => profile != null && profile.ActiveCharacter != null
            ? profile.ActiveCharacter.Speed.GetValue()
            : fallbackSpeed;

        private bool IsOnLadder => ladderContacts > 0;
        private bool IsClimbing => IsOnLadder && Mathf.Abs(verticalInput) > 0.01f;

        public void SetProfile(CharacterProfile characterProfile)
        {
            profile = characterProfile;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            defaultGravityScale = body.gravityScale;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            lastSafePosition = transform.position;
        }

        private void Update()
        {
            ReadKeyboardInput();

            if (KeybindManager.WasPressedThisFrame(KeybindActions.Jump))
            {
                TryJump();
            }

            if (verticalInput < -0.01f && dropTimer <= 0f && !IsClimbing)
            {
                TryDropThroughPlatform();
            }

            TickDropTimer();
            RespawnIfFallen();
        }

        private void FixedUpdate()
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = horizontalInput * CurrentSpeed;
            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                FacingDirection = horizontalInput < 0f ? Vector2.left : Vector2.right;
            }

            if (IsClimbing)
            {
                body.gravityScale = 0f;
                velocity.y = verticalInput * climbSpeed;
                IgnorePlatformCollisionsForClimbing();
            }
            else
            {
                body.gravityScale = defaultGravityScale;
                RestoreClimbIgnoredPlatforms();
            }

            body.linearVelocity = velocity;
        }

        private void ReadKeyboardInput()
        {
            horizontalInput = 0f;
            if (KeybindManager.IsPressed(KeybindActions.MoveLeft))
            {
                horizontalInput -= 1f;
            }

            if (KeybindManager.IsPressed(KeybindActions.MoveRight))
            {
                horizontalInput += 1f;
            }

            verticalInput = 0f;
            if (KeybindManager.IsPressed(KeybindActions.MoveDown))
            {
                verticalInput -= 1f;
            }

            if (KeybindManager.IsPressed(KeybindActions.MoveUp))
            {
                verticalInput += 1f;
            }
        }

        private void TryJump()
        {
            if (IsClimbing || !IsGrounded())
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.y = jumpVelocity;
            body.linearVelocity = velocity;
        }

        private bool IsGrounded()
        {
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useTriggers = false;

            RaycastHit2D[] hits = new RaycastHit2D[4];
            int hitCount = bodyCollider.Cast(Vector2.down, contactFilter, hits, groundCheckDistance);
            for (int i = 0; i < hitCount; i++)
            {
                var collider = hits[i].collider;
                if (collider != null && collider != ignoredPlatform && collider.GetComponent<LadderZone>() == null)
                {
                    return true;
                }
            }

            return false;
        }

        private void TryDropThroughPlatform()
        {
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useTriggers = false;

            RaycastHit2D[] hits = new RaycastHit2D[4];
            int hitCount = bodyCollider.Cast(Vector2.down, contactFilter, hits, platformCheckDistance);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D platform = hits[i].collider;
                if (platform == null || platform.GetComponent<DropThroughPlatform>() == null)
                {
                    continue;
                }

                ignoredPlatform = platform;
                dropTimer = dropThroughSeconds;
                Physics2D.IgnoreCollision(bodyCollider, ignoredPlatform, true);

                Vector2 velocity = body.linearVelocity;
                velocity.y = -dropNudgeVelocity;
                body.linearVelocity = velocity;
                return;
            }
        }

        private void TickDropTimer()
        {
            if (dropTimer <= 0f)
            {
                return;
            }

            dropTimer -= Time.deltaTime;
            if (dropTimer > 0f || ignoredPlatform == null)
            {
                return;
            }

            Physics2D.IgnoreCollision(bodyCollider, ignoredPlatform, false);
            ignoredPlatform = null;
        }

        private void RespawnIfFallen()
        {
            var currentFallRespawnY = MapManager.Instance != null && MapManager.Instance.CurrentVoidRespawnY.HasValue
                ? MapManager.Instance.CurrentVoidRespawnY.Value
                : fallRespawnY;
            if (transform.position.y > currentFallRespawnY)
            {
                return;
            }

            RestoreClimbIgnoredPlatforms();
            if (ignoredPlatform != null)
            {
                Physics2D.IgnoreCollision(bodyCollider, ignoredPlatform, false);
                ignoredPlatform = null;
            }

            dropTimer = 0f;
            ladderContacts = 0;
            body.gravityScale = defaultGravityScale;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = lastSafePosition;
            transform.position = lastSafePosition;
        }

        private void CacheSafePlatformPosition(Collider2D platform)
        {
            Bounds platformBounds = platform.bounds;
            Bounds playerBounds = bodyCollider.bounds;
            float halfPlayerHeight = playerBounds.extents.y;
            float clampedX = Mathf.Clamp(transform.position.x, platformBounds.min.x, platformBounds.max.x);

            lastSafePosition = new Vector2(
                clampedX,
                platformBounds.max.y + halfPlayerHeight + respawnSurfaceOffset);
        }

        private void IgnorePlatformCollisionsForClimbing()
        {
            Collider2D[] overlaps = new Collider2D[8];
            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.useTriggers = false;

            int count = bodyCollider.Overlap(contactFilter, overlaps);
            for (int i = 0; i < count; i++)
            {
                IgnorePlatformForClimb(overlaps[i]);
            }

            Vector2 climbDirection = verticalInput >= 0f ? Vector2.up : Vector2.down;
            RaycastHit2D[] hits = new RaycastHit2D[8];
            int hitCount = bodyCollider.Cast(climbDirection, contactFilter, hits, climbPlatformCheckDistance);
            for (int i = 0; i < hitCount; i++)
            {
                IgnorePlatformForClimb(hits[i].collider);
            }
        }

        private void IgnorePlatformForClimb(Collider2D platform)
        {
            if (platform == null
                || platform.GetComponent<DropThroughPlatform>() == null
                || climbIgnoredPlatforms.Contains(platform))
            {
                return;
            }

            Physics2D.IgnoreCollision(bodyCollider, platform, true);
            climbIgnoredPlatforms.Add(platform);
        }

        private void RestoreClimbIgnoredPlatforms()
        {
            for (int i = climbIgnoredPlatforms.Count - 1; i >= 0; i--)
            {
                Collider2D platform = climbIgnoredPlatforms[i];
                if (platform != null)
                {
                    Physics2D.IgnoreCollision(bodyCollider, platform, false);
                }
            }

            climbIgnoredPlatforms.Clear();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryCacheSafePlatform(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryCacheSafePlatform(collision);
        }

        private void TryCacheSafePlatform(Collision2D collision)
        {
            if (collision.collider.isTrigger || collision.collider.GetComponent<LadderZone>() != null)
            {
                return;
            }

            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f)
                {
                    CacheSafePlatformPosition(collision.collider);
                    return;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<LadderZone>() != null)
            {
                ladderContacts++;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<LadderZone>() != null)
            {
                ladderContacts = Mathf.Max(0, ladderContacts - 1);
            }
        }
    }
}
