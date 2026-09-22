using UnityEngine;

namespace NineLives
{
    public sealed class CombatVfx : MonoBehaviour
    {
        private SpriteRenderer renderer;
        private Color color;
        private float duration;
        private float elapsed;
        private Vector3 initialScale;

        public static void Spawn(Sprite sprite, Vector2 position, Vector3 scale, Color color, float duration = 0.14f)
        {
            if (sprite == null) return;
            var effect = new GameObject("Combat VFX");
            effect.transform.position = position;
            effect.transform.localScale = scale;
            var spriteRenderer = effect.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 20;
            var vfx = effect.AddComponent<CombatVfx>();
            vfx.Initialize(spriteRenderer, color, duration);
        }

        private void Initialize(SpriteRenderer spriteRenderer, Color initialColor, float lifetime)
        {
            renderer = spriteRenderer;
            color = initialColor;
            duration = lifetime;
            initialScale = transform.localScale;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            renderer.color = new Color(color.r, color.g, color.b, Mathf.Lerp(color.a, 0f, progress));
            transform.localScale = Vector3.Lerp(initialScale, initialScale * 1.25f, progress);
            if (progress >= 1f) Destroy(gameObject);
        }
    }
}
