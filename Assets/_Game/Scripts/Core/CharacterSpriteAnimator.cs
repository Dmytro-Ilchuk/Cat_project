using UnityEngine;

namespace NineLives
{
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class CharacterSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] idleFrames, walkFrames, runFrames, jumpFrames, attackFrames, dodgeFrames, hurtFrames, deathFrames;
        [SerializeField, Min(1)] private float framesPerSecond = 12;
        [SerializeField] private float walkSpeedThreshold = .1f, runSpeedThreshold = 5.5f;
        private SpriteRenderer visual;
        private PlayerController player;
        private PlayerCombat combat;
        private RatController rat;
        private Rigidbody2D body;
        private Health health;
        private Sprite[] current;
        private float elapsed, hurtUntil, ghostAt;
        private MaterialPropertyBlock properties;
        private static readonly int Flash = Shader.PropertyToID("_Flash");

        private void Awake()
        {
            visual = GetComponent<SpriteRenderer>();
            player = GetComponentInParent<PlayerController>();
            combat = GetComponentInParent<PlayerCombat>();
            rat = GetComponent<RatController>();
            body = GetComponentInParent<Rigidbody2D>();
            health = GetComponentInParent<Health>();
            properties = new MaterialPropertyBlock();
            if (health != null) health.Damaged += OnDamage;
        }

        private void OnDestroy() { if (health != null) health.Damaged -= OnDamage; }

        private void OnDamage(Health target, int amount)
        {
            hurtUntil = Time.time + .16f;
            CombatVfx.Burst(visual.bounds.center, player != null ? new Color(1, .25f, .4f) : new Color(.8f, .45f, 1));
            RoomCamera.Impulse(player != null ? .12f : .06f);
        }

        private void Update()
        {
            var frames = SelectFrames();
            if (frames == null || frames.Length == 0) return;
            if (frames != current) { current = frames; elapsed = 0; }
            // Death keeps playing under the result overlay; other states obey pause.
            elapsed += health != null && !health.IsAlive ? Time.unscaledDeltaTime : Time.deltaTime;
            int frame;
            if (frames == attackFrames && combat != null) frame = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(combat.AttackProgress * frames.Length));
            else if (frames == attackFrames && rat != null) frame = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(rat.AttackProgress * frames.Length));
            else if (frames == jumpFrames && body != null) frame = body.linearVelocity.y > 2 ? 2 : body.linearVelocity.y < -2 ? 4 : 3;
            else if (frames == deathFrames || frames == hurtFrames) frame = Mathf.Min(frames.Length - 1, (int)(elapsed * (frames == deathFrames ? 12 : 30)));
            else frame = (int)(elapsed * (frames == idleFrames ? 6 : framesPerSecond)) % frames.Length;
            visual.sprite = frames[Mathf.Clamp(frame, 0, frames.Length - 1)];
            if (properties == null) properties = new MaterialPropertyBlock();
            visual.GetPropertyBlock(properties);
            properties.SetFloat(Flash, Time.time < hurtUntil ? Mathf.Clamp01((hurtUntil - Time.time) / .16f) : 0);
            visual.SetPropertyBlock(properties);
            if (player != null && player.IsDashing && Time.time >= ghostAt)
            {
                ghostAt = Time.time + .045f;
                CombatVfx.Ghost(visual);
            }
        }

        private Sprite[] SelectFrames()
        {
            if (health != null && !health.IsAlive && deathFrames.Length > 0) return deathFrames;
            if (Time.time < hurtUntil && hurtFrames.Length > 0) return hurtFrames;
            if (player != null)
            {
                if (combat != null && combat.IsAttacking) return attackFrames;
                if (player.IsDashing) return dodgeFrames;
                if (!player.IsGrounded) return jumpFrames;
            }
            if (rat != null && rat.IsAttacking) return attackFrames;
            float speed = body != null ? Mathf.Abs(body.linearVelocity.x) : 0;
            if (speed >= runSpeedThreshold && runFrames.Length > 0) return runFrames;
            if (speed >= walkSpeedThreshold && walkFrames.Length > 0) return walkFrames;
            return idleFrames;
        }
    }
}
