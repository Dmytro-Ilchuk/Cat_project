using UnityEngine;

namespace NineLives
{
    public sealed class CombatVfx : MonoBehaviour
    {
        private SpriteRenderer sprite;
        private LineRenderer line;
        private Vector2 velocity;
        private Color tint;
        private float lifetime, age, gravity;
        private Vector3 size;
        private static Sprite pixel;
        private static Material material;
        private static Material Material => material != null ? material : material = Resources.Load<Material>("CombatFx");

        private static Sprite Pixel
        {
            get
            {
                if (pixel != null) return pixel;
                var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                texture.SetPixel(0, 0, Color.white); texture.Apply();
                return pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            }
        }

        public static void Slash(Vector2 position, float facing, bool enemy = false)
        {
            Color color = enemy ? new Color(1, .3f, .42f) : new Color(.6f, .95f, 1);
            for (int claw = 0; claw < 3; claw++)
            {
                var go = new GameObject("Claw arc"); go.transform.position = position;
                var fx = go.AddComponent<CombatVfx>();
                fx.tint = color; fx.lifetime = .14f + claw * .025f;
                fx.line = go.AddComponent<LineRenderer>();
                fx.line.sharedMaterial = Material; fx.line.useWorldSpace = false;
                fx.line.sortingOrder = 30; fx.line.positionCount = 9;
                fx.line.startWidth = .13f; fx.line.endWidth = .015f;
                fx.line.startColor = Color.white; fx.line.endColor = color;
                for (int i = 0; i < 9; i++)
                {
                    float a = Mathf.Lerp(-1.05f, 1.05f, i / 8f);
                    fx.line.SetPosition(i, new Vector3(Mathf.Cos(a) * .75f * facing + claw * .12f * facing, Mathf.Sin(a) * .7f - claw * .14f));
                }
            }
        }

        public static void Burst(Vector2 position, Color color, int count = 12, float speed = 4)
        {
            for (int i = 0; i < count; i++)
            {
                var direction = Random.insideUnitCircle.normalized;
                var fx = Piece(Pixel, position, Vector3.one * Random.Range(.04f, .13f), i % 3 == 0 ? Color.white : color, Random.Range(.16f, .42f));
                fx.velocity = direction * Random.Range(speed * .35f, speed);
                fx.gravity = 5;
            }
        }

        public static void Death(Vector2 position)
        {
            Burst(position, new Color(.68f, .24f, 1), 24, 5);
            Slash(position, 1);
            RoomCamera.Impulse(.16f);
        }

        public static void Ghost(SpriteRenderer source)
        {
            if (source == null || source.sprite == null) return;
            var fx = Piece(source.sprite, source.transform.position, source.transform.lossyScale, new Color(.4f, .65f, 1, .36f), .16f);
            fx.sprite.flipX = source.flipX;
            fx.sprite.sortingOrder = source.sortingOrder - 1;
        }

        private static CombatVfx Piece(Sprite image, Vector2 position, Vector3 scale, Color color, float duration)
        {
            var go = new GameObject("Combat particle"); go.transform.position = position; go.transform.localScale = scale;
            var fx = go.AddComponent<CombatVfx>(); fx.tint = color; fx.lifetime = duration; fx.size = scale;
            fx.sprite = go.AddComponent<SpriteRenderer>(); fx.sprite.sprite = image; fx.sprite.sharedMaterial = Material;
            fx.sprite.color = color; fx.sprite.sortingOrder = 25;
            return fx;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / lifetime);
            var color = new Color(tint.r, tint.g, tint.b, tint.a * (1 - t));
            if (sprite != null)
            {
                velocity.y -= gravity * Time.deltaTime;
                transform.position += (Vector3)(velocity * Time.deltaTime);
                transform.localScale = size * Mathf.Lerp(1, .25f, t);
                sprite.color = color;
            }
            if (line != null) { line.startColor = color; line.endColor = new Color(color.r, color.g, color.b, 0); }
            if (t >= 1) Destroy(gameObject);
        }
    }
}
