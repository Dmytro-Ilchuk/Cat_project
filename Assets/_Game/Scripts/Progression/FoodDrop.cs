using UnityEngine;

namespace NineLives
{
    public sealed class FoodDrop : MonoBehaviour
    {
        private static Sprite pelletSprite;
        private static readonly Color Glow = new Color(1f, .72f, .2f);

        private PlayerController target;
        private SpriteRenderer visual;
        private Vector2 launch;
        private Vector2 landing;
        private int amount;
        private float age;
        private float trailTimer;

        public static void Spawn(Vector2 position, PlayerController player, int reward)
        {
            if (reward <= 0 || player == null) return;
            var drop = new GameObject("Food drop", typeof(SpriteRenderer), typeof(FoodDrop));
            drop.transform.position = position;
            drop.GetComponent<FoodDrop>().Initialize(player, reward, position);
        }

        private void Initialize(PlayerController player, int reward, Vector2 position)
        {
            target = player;
            amount = reward;
            launch = position;
            landing = position + new Vector2(Random.Range(-.65f, .65f), -.45f);
            visual = GetComponent<SpriteRenderer>();
            visual.sprite = PelletSprite;
            visual.sortingOrder = 24;
            transform.localScale = Vector3.one * .85f;
            CombatVfx.Burst(position, Glow, 7, 2.2f);
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age < .42f)
            {
                float t = age / .42f;
                transform.position = Vector2.Lerp(launch, landing, t) + Vector2.up * (Mathf.Sin(t * Mathf.PI) * .7f);
                transform.localScale = Vector3.one * Mathf.Lerp(.55f, .85f, t);
                return;
            }

            if (target == null || !target.GetComponent<Health>().IsAlive)
            {
                if (age > 5f) Destroy(gameObject);
                return;
            }

            Vector2 destination = (Vector2)target.transform.position + Vector2.up * .65f;
            float speed = Mathf.Min(17f, 5f + (age - .42f) * 11f);
            transform.position = Vector2.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            transform.localScale = Vector3.one * (1f + .12f * Mathf.Sin(age * 22f));
            trailTimer -= Time.deltaTime;
            if (trailTimer <= 0f)
            {
                CombatVfx.Burst(transform.position, Glow, 2, .6f);
                trailTimer = .11f;
            }
            if (Vector2.Distance(transform.position, destination) > .28f) return;
            CycleController.Instance?.AddFood(amount);
            CombatVfx.Burst(destination, Glow, 9, 2.5f);
            Destroy(gameObject);
        }

        private static Sprite PelletSprite
        {
            get
            {
                if (pelletSprite != null) return pelletSprite;
                var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                for (int y = 0; y < 8; y++)
                    for (int x = 0; x < 8; x++)
                    {
                        float edge = Mathf.Abs(x - 3.5f) + Mathf.Abs(y - 3.5f);
                        Color color = edge > 4.8f ? Color.clear :
                            edge > 3.5f ? new Color(.26f, .12f, .07f) :
                            x < 3 && y > 3 ? new Color(1f, .94f, .55f) : Glow;
                        texture.SetPixel(x, y, color);
                    }
                texture.Apply();
                return pelletSprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), Vector2.one * .5f, 16f);
            }
        }
    }
}
