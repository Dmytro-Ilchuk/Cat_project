using UnityEngine;

namespace NineLives
{
    public enum RatAttackStyle { Claw, Spit }

    [RequireComponent(typeof(Health), typeof(Rigidbody2D))]
    public sealed class RatController : MonoBehaviour
    {
        private enum State { Waiting, Approaching, WindingUp, Recovering, Stunned, Dead }

        [SerializeField, Min(0f)] private float speed = 3f;
        [SerializeField, Min(0.1f)] private float engageRange = 6f;
        [SerializeField, Min(0f)] private float attackRange = 0.12f;
        [SerializeField, Min(0.01f)] private float windUpDuration = 0.45f;
        [SerializeField, Min(0.01f)] private float recoveryDuration = 0.6f;
        [SerializeField, Min(1)] private int attackDamage = 10;
        [SerializeField, Min(0f)] private float attackKnockback = 7f;
        [SerializeField, Range(0f, 1f)] private float controlResistance;
        [SerializeField] private float fallY = -5f;
        [SerializeField] private RatAttackStyle attackStyle;
        [SerializeField, Min(.5f)] private float rangedAttackRange = 4.25f;
        [SerializeField, Min(.1f)] private float rangedRetreatRange = 2.1f;
        [SerializeField, Min(1f)] private float projectileSpeed = 9f;
        [SerializeField, Min(1)] private int rangedDamage = 7;
        [SerializeField, Min(0f)] private float rangedKnockback = 3.5f;

        private Health health;
        private Rigidbody2D body;
        private PlayerController player;
        private Health playerHealth;
        private Collider2D playerHitbox;
        private State state;
        private float stateUntil;
        private SpriteRenderer visual;
        private Collider2D hitbox;
        private Color baseColor;
        private int groundMask;

        public bool IsAttacking => state == State.WindingUp || state == State.Recovering;
        public bool IsRanged => attackStyle == RatAttackStyle.Spit;
        public float AttackProgress => state == State.WindingUp
            ? .5f * Mathf.Clamp01(1 - (stateUntil - Time.time) / windUpDuration)
            : .5f + .5f * Mathf.Clamp01(1 - (stateUntil - Time.time) / recoveryDuration);

        private void Awake()
        {
            health = GetComponent<Health>();
            body = GetComponent<Rigidbody2D>();
            visual = GetComponent<SpriteRenderer>();
            hitbox = GetComponent<Collider2D>();
            PuddleReflection.Ensure(visual);
            baseColor = attackStyle == RatAttackStyle.Spit ? new Color(.55f, .9f, .72f) : visual.color;
            visual.color = baseColor;
            groundMask = 1 << LayerMask.NameToLayer("Ground");
            SnapToGround();
            player = FindAnyObjectByType<PlayerController>();
            playerHealth = player != null ? player.GetComponent<Health>() : null;
            playerHitbox = player != null ? player.GetComponent<Collider2D>() : null;
            health.Died += HandleDeath;
        }

        private void OnDestroy()
        {
            health.Died -= HandleDeath;
        }

        private void FixedUpdate()
        {
            if (state == State.Dead) return;
            if (body.position.y < fallY)
            {
                health.TakeDamage(health.MaximumHealth, Vector2.zero);
                return;
            }
            if (player == null || playerHealth == null || !playerHealth.IsAlive) return;
            if (Time.time < stateUntil) return;

            float distance = Mathf.Abs(player.transform.position.x - transform.position.x);
            float contactGap = HorizontalGapToPlayer();
            switch (state)
            {
                case State.Waiting:
                    if (distance <= engageRange) state = State.Approaching;
                    break;
                case State.Approaching:
                    if (distance > engageRange) { state = State.Waiting; body.linearVelocity = Vector2.zero; break; }
                    bool readyToAttack = attackStyle == RatAttackStyle.Spit
                        ? distance <= rangedAttackRange && CanHitPlayer()
                        : contactGap <= attackRange && CanHitPlayer();
                    if (readyToAttack) { state = State.WindingUp; stateUntil = Time.time + windUpDuration; body.linearVelocity = Vector2.zero; visual.color = new Color(1f, 0.8f, 0.55f); break; }
                    float direction = Mathf.Sign(player.transform.position.x - transform.position.x);
                    visual.flipX = direction < 0f;
                    if (attackStyle == RatAttackStyle.Spit && distance < rangedRetreatRange) direction = -direction;
                    if (!GroundBelow() || !GroundAhead(direction) || WallAhead(direction))
                    {
                        body.linearVelocity = new Vector2(0, body.linearVelocity.y);
                        break;
                    }
                    body.linearVelocity = new Vector2(direction * speed, body.linearVelocity.y);
                    break;
                case State.WindingUp:
                    Attack();
                    visual.color = baseColor;
                    state = State.Recovering;
                    stateUntil = Time.time + recoveryDuration;
                    break;
                case State.Recovering:
                    state = State.Approaching;
                    break;
                case State.Stunned:
                    state = State.Approaching;
                    break;
            }
        }

        public void ApplyHiss(Vector2 force, float duration)
        {
            if (state == State.Dead || controlResistance >= 1f) return;
            body.AddForce(force * (1f - controlResistance), ForceMode2D.Impulse);
            state = State.Stunned;
            stateUntil = Time.time + duration * (1f - controlResistance);
        }

        public void SetAttackStyle(RatAttackStyle style)
        {
            attackStyle = style;
            gameObject.name = style == RatAttackStyle.Spit ? "Spitter Rat" : "Rat";
            if (visual == null) return;
            baseColor = style == RatAttackStyle.Spit ? new Color(.55f, .9f, .72f) : Color.white;
            visual.color = baseColor;
        }

        private void Attack()
        {
            Vector2 direction = Mathf.Sign(player.transform.position.x - transform.position.x) * Vector2.right;
            if (attackStyle == RatAttackStyle.Spit)
            {
                if (playerHitbox == null || Vector2.Distance(playerHitbox.bounds.center, hitbox.bounds.center) > rangedAttackRange || !CanHitPlayer()) return;
                Vector2 origin = (Vector2)visual.bounds.center + direction * .55f;
                Vector2 destination = playerHitbox.bounds.center;
                RatProjectile.Launch(origin, destination, playerHealth, projectileSpeed, rangedDamage, rangedKnockback);
                return;
            }
            if (HorizontalGapToPlayer() > attackRange || !CanHitPlayer()) return;
            CombatVfx.Slash((Vector2)transform.position + direction * .6f, direction.x, true);
            playerHealth.TakeDamage(attackDamage, direction * attackKnockback + Vector2.up * 0.25f);
        }

        private void HandleDeath(Health _)
        {
            state = State.Dead;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            hitbox.enabled = false;
            visual.color = baseColor;
            CombatVfx.Death(visual.bounds.center);
            Destroy(gameObject, .65f);
        }

        private void SnapToGround()
        {
            foreach (var hit in Physics2D.RaycastAll(transform.position + Vector3.up * 0.5f, Vector2.down, 20f, groundMask))
            {
                if (hit.collider.isTrigger) continue;
                transform.position += Vector3.up * (hit.point.y - hitbox.bounds.min.y);
                return;
            }
        }

        private bool GroundBelow() => Physics2D.Raycast(
            new Vector2(hitbox.bounds.center.x, hitbox.bounds.min.y + .2f), Vector2.down, .45f, groundMask);

        private bool GroundAhead(float direction) => Physics2D.Raycast(
            new Vector2(hitbox.bounds.center.x + direction * (hitbox.bounds.extents.x + .18f), hitbox.bounds.min.y + .2f),
            Vector2.down, .65f, groundMask);

        private bool WallAhead(float direction) => Physics2D.Raycast(
            new Vector2(hitbox.bounds.center.x + direction * hitbox.bounds.extents.x, hitbox.bounds.min.y + .55f),
            Vector2.right * direction, .25f, groundMask);

        private bool CanHitPlayer()
        {
            if (playerHitbox == null ||
                playerHitbox.bounds.min.y > hitbox.bounds.max.y + .1f ||
                playerHitbox.bounds.max.y < hitbox.bounds.min.y - .1f) return false;
            return !Physics2D.Linecast(hitbox.bounds.center, playerHitbox.bounds.center, groundMask);
        }

        private float HorizontalGapToPlayer()
        {
            if (playerHitbox == null) return Mathf.Abs(player.transform.position.x - transform.position.x);
            float centreDistance = Mathf.Abs(playerHitbox.bounds.center.x - hitbox.bounds.center.x);
            return Mathf.Max(0f, centreDistance - playerHitbox.bounds.extents.x - hitbox.bounds.extents.x);
        }
    }
}
