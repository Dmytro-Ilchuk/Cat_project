using UnityEngine;
using UnityEngine.InputSystem;

namespace NineLives
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private Transform visual;
        [SerializeField] private LayerMask groundLayers;
        [SerializeField, Min(0)] private float speed = 6f;
        [SerializeField, Min(0)] private float acceleration = 35f;
        [SerializeField, Min(0)] private float braking = 50f;
        [SerializeField, Min(0)] private float jumpSpeed = 14f;
        [SerializeField, Range(0f, 1f)] private float jumpCut = 0.45f;
        [SerializeField, Min(0)] private float coyoteTime = 0.12f;
        [SerializeField, Min(0)] private float jumpBuffer = 0.12f;
        [SerializeField, Min(0)] private float dashSpeed = 16f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.16f;
        [SerializeField, Min(0)] private float dashCooldown = 0.65f;
        [SerializeField] private float fallY = -6f;

        private Rigidbody2D body;
        private Health health;
        private ContactFilter2D groundFilter;
        private Vector3 visualScale;
        private InputAction jumpAction, dashAction;
        private Vector2 spawnPosition;
        private float gravity, lastGrounded = float.NegativeInfinity;
        private float jumpUntil = float.NegativeInfinity, dashUntil, nextDash;
        private float dashDirection;
        private bool jumpArmed, dashArmed, dashRequested, airDashUsed, cutJump;

        public bool IsGrounded => body != null && body.IsTouching(groundFilter);
        public bool IsDashing { get; private set; }
        public bool InputBlocked { get; private set; }
        public float FacingDirection => Mathf.Sign(visual.localScale.x == 0f ? 1f : visual.localScale.x);

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            spawnPosition = body.position;
            gravity = body.gravityScale;
            jumpAction = moveAction.action.actionMap.FindAction("Jump", true);
            dashAction = moveAction.action.actionMap.FindAction("Dash", true);
            visualScale = visual.localScale;
            groundFilter.SetLayerMask(groundLayers);
            groundFilter.SetNormalAngle(45f, 135f);
            groundFilter.useTriggers = false;
        }

        private void OnEnable()
        {
            moveAction.action.Enable();
            jumpAction.Enable();
            dashAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.action.Disable();
            jumpAction.Disable();
            dashAction.Disable();
            EndDash();
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        private void Update()
        {
            if (InputBlocked || Time.timeScale == 0f) return;
            if (!jumpAction.IsPressed()) jumpArmed = true;
            if (!dashAction.IsPressed()) dashArmed = true;
            if (jumpArmed && jumpAction.WasPressedThisFrame())
            {
                jumpUntil = Time.time + jumpBuffer;
                jumpArmed = false;
            }
            if (dashArmed && dashAction.WasPressedThisFrame())
            {
                dashRequested = true;
                dashArmed = false;
            }
            if (jumpAction.WasReleasedThisFrame()) cutJump = true;
        }

        public void SetInputBlocked(bool blocked)
        {
            InputBlocked = blocked || (health != null && !health.IsAlive);
            jumpUntil = float.NegativeInfinity;
            dashRequested = cutJump = jumpArmed = dashArmed = false;
            EndDash();
            body.linearVelocity = Vector2.zero;
        }

        public void Respawn()
        {
            SetInputBlocked(InputBlocked);
            body.position = spawnPosition;
            body.rotation = 0f;
            body.angularVelocity = 0f;
            lastGrounded = float.NegativeInfinity;
            nextDash = 0f;
            airDashUsed = false;
            visual.localScale = visualScale;
        }

        private void EndDash()
        {
            if (!IsDashing) return;
            IsDashing = false;
            body.gravityScale = gravity;
            body.linearVelocity = new Vector2(Mathf.Clamp(body.linearVelocity.x, -speed, speed), body.linearVelocity.y);
        }

        private void FixedUpdate()
        {
            if (body.position.y < fallY)
            {
                health?.TakeDamage(10, Vector2.zero);
                Respawn();
            }
            if (InputBlocked) return;
            bool grounded = IsGrounded;
            if (grounded && !IsDashing && body.linearVelocity.y <= 0.01f)
            {
                lastGrounded = Time.time;
                airDashUsed = false;
            }
            float input = Mathf.Clamp(moveAction.action.ReadValue<Vector2>().x, -1f, 1f);
            if (input != 0f && !IsDashing)
                visual.localScale = new Vector3(Mathf.Abs(visualScale.x) * Mathf.Sign(input), visualScale.y, visualScale.z);

            if (dashRequested && !IsDashing && Time.time >= nextDash && (grounded || !airDashUsed))
            {
                IsDashing = true;
                if (!grounded) airDashUsed = true;
                dashDirection = Mathf.Sign(visual.localScale.x);
                dashUntil = Time.time + dashDuration;
                nextDash = Time.time + dashCooldown;
                body.gravityScale = 0f;
                jumpUntil = float.NegativeInfinity;
            }
            dashRequested = false;
            if (IsDashing && Time.time >= dashUntil) EndDash();
            if (IsDashing)
            {
                body.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
                return;
            }

            Vector2 velocity = body.linearVelocity;
            float rate = input == 0f || input * velocity.x < 0f ? braking : acceleration;
            velocity.x = Mathf.MoveTowards(velocity.x, input * speed, rate * Time.fixedDeltaTime);
            if (Time.time <= jumpUntil && Time.time - lastGrounded <= coyoteTime)
            {
                velocity.y = jumpSpeed;
                lastGrounded = jumpUntil = float.NegativeInfinity;
                cutJump = !jumpAction.IsPressed();
            }
            if (cutJump && velocity.y > 0f) velocity.y *= jumpCut;
            cutJump = false;
            body.linearVelocity = velocity;
        }
    }
}
