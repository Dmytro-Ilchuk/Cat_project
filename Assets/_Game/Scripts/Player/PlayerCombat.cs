using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NineLives
{
    [RequireComponent(typeof(Health), typeof(PlayerController))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField, Min(1)] private int clawDamage = 10;
        [SerializeField, Min(0.01f)] private float clawCooldown = 0.35f;
        [SerializeField, Min(0.01f)] private float clawActiveTime = 0.12f;
        [SerializeField, Min(0.1f)] private float clawRange = 1.25f;
        [SerializeField, Min(0.1f)] private float clawRadius = 0.65f;
        [SerializeField, Min(0.01f)] private float hissCooldown = 6f;
        [SerializeField, Min(0.1f)] private float hissRange = 2f;
        [SerializeField, Range(0f, 180f)] private float hissAngle = 65f;
        [SerializeField, Min(0f)] private float hissControlDuration = 1f;
        [SerializeField, Min(0f)] private float hissForce = 8f;

        private readonly HashSet<Health> hitTargets = new HashSet<Health>();
        private Health health;
        private PlayerController controller;
        private InputAction attackAction;
        private InputAction hissAction;
        private float nextClawTime;
        private float nextHissTime;
        private Coroutine attackRoutine;
        private Sprite hissSprite;

        public bool IsAttacking => attackRoutine != null;
        public float HissCooldownRemaining => Mathf.Max(0f, nextHissTime - Time.time);

        private void Awake()
        {
            health = GetComponent<Health>();
            controller = GetComponent<PlayerController>();
            hissSprite = GetComponentInChildren<SpriteRenderer>()?.sprite;
            var gameplay = InputSystem.actions.FindActionMap("Gameplay", true);
            attackAction = gameplay.FindAction("Attack", true).Clone();
            hissAction = gameplay.FindAction("Hiss", true).Clone();
        }

        private void OnEnable()
        {
            attackAction?.Enable();
            hissAction?.Enable();
        }

        private void OnDisable()
        {
            attackAction?.Disable();
            hissAction?.Disable();
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        private void Update()
        {
            if (!health.IsAlive || controller.InputBlocked || Time.timeScale == 0f) return;
            if (attackAction.WasPressedThisFrame() && !IsAttacking && Time.time >= nextClawTime)
                attackRoutine = StartCoroutine(ClawAttack());
            if (hissAction.WasPressedThisFrame() && Time.time >= nextHissTime)
                Hiss();
        }

        private IEnumerator ClawAttack()
        {
            nextClawTime = Time.time + clawCooldown;
            hitTargets.Clear();
            var direction = Vector2.right * controller.FacingDirection;
            float finishTime = Time.time + clawActiveTime;
            while (Time.time < finishTime)
            {
                Vector2 attackDirection = Vector2.right * controller.FacingDirection;
                Vector2 centre = (Vector2)transform.position + attackDirection * clawRange;
                foreach (var hit in Physics2D.OverlapCircleAll(centre, clawRadius))
                {
                    var target = hit.GetComponentInParent<Health>();
                    if (target == null || target == health || !hitTargets.Add(target)) continue;
                    target.TakeDamage(clawDamage, attackDirection * 5f + Vector2.up * 1.5f);
                }
                yield return null;
            }
            attackRoutine = null;
        }

        private void Hiss()
        {
            nextHissTime = Time.time + hissCooldown;
            Vector2 direction = Vector2.right * controller.FacingDirection;
            CombatVfx.Spawn(hissSprite, (Vector2)transform.position + direction * 1.1f, new Vector3(2.2f, 0.22f, 1f), new Color(0.35f, 0.9f, 1f, 0.75f), 0.28f);
            foreach (var hit in Physics2D.OverlapCircleAll(transform.position, hissRange))
            {
                var rat = hit.GetComponentInParent<RatController>();
                if (rat == null) continue;
                Vector2 toTarget = ((Vector2)rat.transform.position - (Vector2)transform.position).normalized;
                if (Vector2.Angle(direction, toTarget) <= hissAngle * 0.5f)
                    rat.ApplyHiss(direction * hissForce, hissControlDuration);
            }
        }

        private void OnDestroy()
        {
            attackAction?.Dispose();
            hissAction?.Dispose();
        }
    }
}
