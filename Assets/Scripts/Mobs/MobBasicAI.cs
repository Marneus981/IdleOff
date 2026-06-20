using IdleOff.Combat;
using UnityEngine;

namespace IdleOff.Mobs
{
    [RequireComponent(typeof(MobEntity))]
    public class MobBasicAI : MonoBehaviour
    {
        [SerializeField] protected float directionChangeInterval = 2f;
        [SerializeField] protected Vector2 pauseDurationRange = new(0.5f, 1.25f);

        protected MobEntity mob;
        protected MobActionController actionController;
        protected Rigidbody2D body;
        protected Transform target;
        protected float timer;
        protected int moveDirection = 1;
        protected bool paused;

        protected virtual void Awake()
        {
            mob = GetComponent<MobEntity>();
            actionController = GetComponent<MobActionController>();
            body = GetComponent<Rigidbody2D>();
        }

        protected virtual void OnEnable()
        {
            if (mob != null)
            {
                mob.Damaged += HandleDamaged;
            }
        }

        protected virtual void OnDisable()
        {
            if (mob != null)
            {
                mob.Damaged -= HandleDamaged;
            }
        }

        protected virtual void Update()
        {
            if (mob == null || !mob.IsAlive)
            {
                return;
            }

            if (target != null)
            {
                if (actionController != null && actionController.IsTargetInRange(target))
                {
                    MoveHorizontal(0f);
                    actionController.TryAttack(target);
                    return;
                }

                MoveTowardTarget();
                return;
            }

            Wander();
        }

        public void SetTarget(Transform nextTarget)
        {
            target = nextTarget;
        }

        protected virtual void HandleDamaged(MobEntity damagedMob, ICombatant attacker)
        {
            if (attacker is PlayerCombatant player)
            {
                target = player.transform;
            }
        }

        protected virtual void Wander()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                paused = !paused;
                moveDirection = Random.value < 0.5f ? -1 : 1;
                timer = paused
                    ? Random.Range(pauseDurationRange.x, pauseDurationRange.y)
                    : directionChangeInterval;
            }

            MoveHorizontal(paused ? 0f : moveDirection);
        }

        protected virtual void MoveTowardTarget()
        {
            var delta = target.position.x - transform.position.x;
            MoveHorizontal(Mathf.Sign(delta));
        }

        protected void MoveHorizontal(float direction)
        {
            var speed = mob.Template.moveSpeed;
            if (body != null)
            {
                body.linearVelocity = new Vector2(direction * speed, body.linearVelocity.y);
                return;
            }

            transform.position += new Vector3(direction * speed * Time.deltaTime, 0f, 0f);
        }
    }
}
