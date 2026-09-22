using UnityEngine;

namespace NineLives
{
    [RequireComponent(typeof(PlayerController))]
    public sealed class CatMotionAnimator : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        private PlayerController controller;
        private Rigidbody2D body;
        private Vector3 baseScale;
        private Vector3 basePosition;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            body = GetComponent<Rigidbody2D>();
            baseScale = visual.localScale;
            basePosition = visual.localPosition;
        }

        private void LateUpdate()
        {
            float speed = Mathf.Abs(body.linearVelocity.x);
            float walk = Mathf.Sin(Time.time * 13f) * Mathf.Clamp01(speed / 6f);
            float facing = controller.FacingDirection;
            Vector3 facingScale = new Vector3(Mathf.Abs(baseScale.x) * facing, baseScale.y, baseScale.z);

            visual.localPosition = basePosition;
            visual.localRotation = Quaternion.Euler(0f, 0f, walk * 2.5f);
            visual.localScale = facingScale;

            if (!controller.IsGrounded)
                visual.localScale = Vector3.Scale(facingScale, new Vector3(0.88f, 1.15f, 1f));
            if (controller.IsDashing)
                visual.localScale = Vector3.Scale(facingScale, new Vector3(1.28f, 0.78f, 1f));
        }
    }
}
