using UnityEngine;

namespace NineLives
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class SpikeHazard : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 6;
        [SerializeField, Min(.1f)] private float repeatDelay = .9f;
        private float nextHit;

        private void OnTriggerEnter2D(Collider2D other) => TryDamage(other);

        private void OnTriggerStay2D(Collider2D other) => TryDamage(other);

        private void TryDamage(Collider2D other)
        {
            if (Time.time < nextHit || !other.TryGetComponent<PlayerController>(out _)) return;
            var health = other.GetComponent<Health>();
            if (health == null || !health.IsAlive) return;
            float direction = Mathf.Sign(other.bounds.center.x - transform.position.x);
            if (health.TakeDamage(damage, new Vector2(direction * 2.5f, 2.5f)))
                nextHit = Time.time + repeatDelay;
        }
    }
}
