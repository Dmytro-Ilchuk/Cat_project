using UnityEngine;

namespace NineLives
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CharacterSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] runFrames;
        [SerializeField] private Sprite[] jumpFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Sprite[] dodgeFrames;
        [SerializeField, Min(1f)] private float framesPerSecond = 9f;
        [SerializeField, Min(0.1f)] private float walkSpeedThreshold = 0.1f;
        [SerializeField, Min(0.1f)] private float runSpeedThreshold = 5.5f;

        private SpriteRenderer spriteRenderer;
        private PlayerController player;
        private PlayerCombat playerCombat;
        private RatController rat;
        private Rigidbody2D body;
        private Sprite[] activeFrames;
        private int activeFrame = -1;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            player = GetComponentInParent<PlayerController>();
            playerCombat = player != null ? player.GetComponent<PlayerCombat>() : null;
            rat = GetComponent<RatController>();
            body = GetComponentInParent<Rigidbody2D>();
        }

        private void Update()
        {
            var frames = SelectFrames();
            if (frames == null || frames.Length == 0) return;
            int frame = Mathf.FloorToInt(Time.time * framesPerSecond) % frames.Length;
            if (activeFrames == frames && activeFrame == frame) return;
            activeFrames = frames;
            activeFrame = frame;
            spriteRenderer.sprite = frames[frame];
        }

        private Sprite[] SelectFrames()
        {
            if (player != null)
            {
                if (playerCombat != null && playerCombat.IsAttacking) return attackFrames.Length > 0 ? attackFrames : walkFrames;
                if (player.IsDashing) return dodgeFrames.Length > 0 ? dodgeFrames : runFrames;
                if (!player.IsGrounded) return jumpFrames.Length > 0 ? jumpFrames : idleFrames;

                float speed = body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
                if (speed >= runSpeedThreshold && runFrames.Length > 0) return runFrames;
                if (speed >= walkSpeedThreshold && walkFrames.Length > 0) return walkFrames;
                return idleFrames.Length > 0 ? idleFrames : walkFrames;
            }

            if (rat != null)
                return rat.IsAttacking ? attackFrames : (body != null && Mathf.Abs(body.linearVelocity.x) > walkSpeedThreshold ? walkFrames : idleFrames);

            return idleFrames;
        }
    }
}
