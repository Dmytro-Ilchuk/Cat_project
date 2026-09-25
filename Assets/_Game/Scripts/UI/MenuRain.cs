using UnityEngine;
using UnityEngine.UI;

namespace NineLives
{
    // One UI mesh, no per-drop GameObjects. Runs even when gameplay is paused.
    public sealed class MenuRain : MaskableGraphic
    {
        [SerializeField, Range(20, 420)] private int drops = 270;
        [SerializeField, Range(.5f, 3f)] private float speedMultiplier = 1.2f;
        protected override void Awake() { base.Awake(); raycastTarget = false; }
        private void Update() => SetVerticesDirty();

        public void ConfigureStorm(int dropCount, float speed)
        {
            drops = Mathf.Clamp(dropCount, 20, 420);
            speedMultiplier = Mathf.Clamp(speed, .5f, 3f);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (PlayerPrefs.GetInt("MenuRain", 1) == 0) return;
            var rect = rectTransform.rect;
            float now = Time.unscaledTime;
            float gust = Mathf.Sin(now * .63f) * 32f + Mathf.Sin(now * 1.71f + 1.2f) * 11f;
            for (int i = 0; i < drops; i++)
            {
                float depth = Mathf.Lerp(.22f, 1f, Hash(i, 1));
                float speed = Mathf.Lerp(330f, 900f, depth) * speedMultiplier * Mathf.Lerp(.82f, 1.18f, Hash(i, 2));
                float cycle = rect.height + 100f;
                float fall = Mathf.Repeat(Hash(i, 3) * cycle - now * speed, cycle);
                float y = fall - 50f + rect.yMin;
                float wind = Mathf.Lerp(34f, 88f, depth) + gust * (.35f + depth * .65f);
                float x = Mathf.Repeat(Hash(i, 4) * rect.width - now * wind, rect.width + 40f) - 20f + rect.xMin;
                float length = Mathf.Lerp(8f, 34f, depth) * Mathf.Lerp(.72f, 1.25f, Hash(i, 5));
                float width = Mathf.Lerp(.45f, 1.65f, depth);
                float slant = Mathf.Lerp(3f, 11f, depth) + gust * .035f;
                var color = new Color(.52f, .7f, .92f, Mathf.Lerp(.055f, .26f, depth));
                AddQuad(vh, new Vector2(x, y), new Vector2(x + slant, y + length), width, color);

                if (y < rect.yMin + 7f && depth > .72f)
                {
                    float splash = Mathf.Lerp(3f, 8f, depth);
                    AddQuad(vh, new Vector2(x - splash, rect.yMin + 2f),
                        new Vector2(x + splash, rect.yMin + 2f), .65f, color * .75f);
                }
            }
        }

        private static float Hash(int value, int salt)
        {
            float number = Mathf.Sin((value + 1) * (12.9898f + salt * 7.233f)) * 43758.5453f;
            return Mathf.Repeat(number, 1f);
        }

        private static void AddQuad(VertexHelper vh, Vector2 start, Vector2 end, float width, Color color)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * width * .5f;
            int index = vh.currentVertCount;
            vh.AddVert(start - normal, color, Vector2.zero);
            vh.AddVert(start + normal, color, Vector2.zero);
            vh.AddVert(end + normal, color, Vector2.zero);
            vh.AddVert(end - normal, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
