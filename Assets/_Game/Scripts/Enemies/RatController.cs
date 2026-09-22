using UnityEngine;

namespace NineLives
{
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

        public bool IsAttacking => state == State.WindingUp || state == State.Recovering;
        public float AttackProgress => state == State.WindingUp
            ? .5f * Mathf.Clamp01(1 - (stateUntil - Time.time) / windUpDuration)
            : .5f + .5f * Mathf.Clamp01(1 - (stateUntil - Time.time) / recoveryDuration);

        private void Awake()
        {
            health = GetComponent<Health>();
            body = GetComponent<Rigidbody2D>();
            visual = GetComponent<SpriteRenderer>();
            hitbox = GetComponent<Collider2D>();
            baseColor = visual.color;
            SnapToGround();
            // The prototype arena has pits. Rats move only horizontally, so they keep their authored lane.
            body.gravityScale = 0f;
            body.constraints |= RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
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
            if (state == State.Dead || player == null || playerHealth == null || !playerHealth.IsAlive) return;
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
                    if (contactGap <= attackRange) { state = State.WindingUp; stateUntil = Time.time + windUpDuration; body.linearVelocity = Vector2.zero; visual.color = new Color(1f, 0.8f, 0.55f); break; }
                    float direction = Mathf.Sign(player.transform.position.x - transform.position.x);
                    visual.flipX = direction < 0f;
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

        private void Attack()
        {
            if (HorizontalGapToPlayer() > attackRange) return;
            Vector2 direction = Mathf.Sign(player.transform.position.x - transform.position.x) * Vector2.right;
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
            foreach (var hit in Physics2D.RaycastAll(transform.position + Vector3.up * 0.5f, Vector2.down, 20f))
            {
                if (hit.collider == hitbox || hit.collider.isTrigger) continue;
                float halfHeight = hitbox.bounds.extents.y;
                transform.position = new Vector3(transform.position.x, hit.point.y + halfHeight, transform.position.z);
                return;
            }
        }

        private float HorizontalGapToPlayer()
        {
            if (playerHitbox == null) return Mathf.Abs(player.transform.position.x - transform.position.x);
            float centreDistance = Mathf.Abs(playerHitbox.bounds.center.x - hitbox.bounds.center.x);
            return Mathf.Max(0f, centreDistance - playerHitbox.bounds.extents.x - hitbox.bounds.extents.x);
        }
    }
}
