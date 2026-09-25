using UnityEngine;

namespace NineLives
{
    public sealed class RatProjectile : MonoBehaviour
    {
        private static Sprite dot;
        private Health target;
        private Vector2 velocity;
        private int damage;
        private float knockback;
        private float gravity;
        private float expiresAt;

        public Vector2 Velocity => velocity;

        public static void Launch(Vector2 position, Vector2 targetPosition, Health target, float horizontalSpeed,
            int damage, float knockback)
        {
            var projectile = new GameObject("Spitter rat projectile");
            projectile.transform.position = position;
            var renderer = projectile.AddComponent<SpriteRenderer>();
            renderer.sprite = Dot;
            renderer.color = new Color(.45f, 1f, .52f, .9f);
            renderer.sortingOrder = 18;
            projectile.transform.localScale = new Vector3(.24f, .14f, 1f);
            var behaviour = projectile.AddComponent<RatProjectile>();
            behaviour.target = target;
            behaviour.velocity = CalculateLaunchVelocity(position, targetPosition, horizontalSpeed,
                out behaviour.gravity, out float flightTime);
            behaviour.damage = damage;
            behaviour.knockback = knockback;
            behaviour.expiresAt = Time.time + flightTime + .8f;
        }

        public static Vector2 CalculateLaunchVelocity(Vector2 origin, Vector2 destination, float horizontalSpeed,
            out float gravity, out float flightTime)
        {
            Vector2 offset = destination - origin;
            flightTime = Mathf.Clamp(Mathf.Abs(offset.x) / Mathf.Max(1f, horizontalSpeed), .48f, 1.05f);
            gravity = -18f;
            return new Vector2(offset.x / flightTime,
                (offset.y - .5f * gravity * flightTime * flightTime) / flightTime);
        }

        private static Sprite Dot
        {
            get
            {
                if (dot != null) return dot;
                dot = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
                return dot;
            }
        }

        private void Update()
        {
            if (Time.time >= expiresAt || target == null || !target.IsAlive) { Destroy(gameObject); return; }
            Vector2 before = transform.position;
            velocity.y += gravity * Time.deltaTime;
            Vector2 after = before + velocity * Time.deltaTime;
            var hit = Physics2D.Linecast(before, after, 1 << LayerMask.NameToLayer("Ground"));
            if (hit.collider != null) { Destroy(gameObject); return; }
            transform.position = after;
            if (velocity.sqrMagnitude > .01f)
                transform.right = velocity.normalized;
            var targetCollider = target.GetComponent<Collider2D>();
            if (targetCollider != null && targetCollider.bounds.SqrDistance(after) < .06f)
            {
                target.TakeDamage(damage, velocity.normalized * knockback + Vector2.up * .1f);
                Destroy(gameObject);
            }
        }
    }
}
