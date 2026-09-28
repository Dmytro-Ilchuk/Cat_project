using UnityEngine;

namespace NineLives
{
    public sealed class WindblownDebris : MonoBehaviour
    {
        private System.Random random;
        private float left;
        private float right;
        private float rest;
        private float groundY;
        private Vector2 velocity;

        public void Initialize(float minX, float maxX, int seed)
        {
            left = minX;
            right = maxX;
            random = new System.Random(seed);
            groundY = transform.position.y;
            velocity = Vector2.zero;
            rest = .5f + (float)random.NextDouble() * 2f;
        }

        private void Update() => Step(Time.deltaTime);

        private void Step(float dt)
        {
            if (random == null) return;
            if (dt <= 0f) return;
            if (rest > 0f)
            {
                rest -= dt;
                if (rest > 0f) return;
                float direction = transform.position.x > right - .5f ||
                    (transform.position.x > left + .5f && random.NextDouble() < .25) ? -1f : 1f;
                velocity = new Vector2(direction * Mathf.Lerp(1f, 2.2f, (float)random.NextDouble()),
                    Mathf.Lerp(.55f, 1.1f, (float)random.NextDouble()));
            }
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, 1.6f * dt);
            velocity.y -= 4.5f * dt;
            Vector3 position = transform.position + (Vector3)(velocity * dt);
            if (position.x < left || position.x > right)
            {
                position.x = Mathf.Clamp(position.x, left, right);
                velocity.x *= -.3f;
            }
            if (position.y <= groundY)
            {
                position.y = groundY;
                velocity = Vector2.zero;
                rest = Mathf.Lerp(.7f, 2.5f, (float)random.NextDouble());
            }
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Clamp(velocity.x * (position.y - groundY) * 18f, -15f, 15f));
        }
    }
}
