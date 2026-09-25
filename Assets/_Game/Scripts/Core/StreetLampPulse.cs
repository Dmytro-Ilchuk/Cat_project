using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NineLives
{
    [RequireComponent(typeof(Light2D))]
    public sealed class StreetLampPulse : MonoBehaviour
    {
        private Light2D lightSource;
        private SpriteRenderer halo;
        private SpriteRenderer streetPool;
        private Color haloColor;
        private Color poolColor;
        private float intensity;
        private float seed;
        private bool neon;

        public void Initialize(SpriteRenderer haloRenderer, SpriteRenderer poolRenderer,
            bool neonLamp, float randomSeed)
        {
            lightSource = GetComponent<Light2D>();
            halo = haloRenderer;
            streetPool = poolRenderer;
            haloColor = halo.color;
            poolColor = streetPool.color;
            intensity = lightSource.intensity;
            neon = neonLamp;
            seed = randomSeed;
        }

        private void Update()
        {
            if (lightSource == null || halo == null || streetPool == null) return;
            float noise = Mathf.PerlinNoise(seed, Time.time * (neon ? 1.7f : .55f));
            float pulse = Mathf.Lerp(neon ? .8f : .91f, 1.05f, noise);
            if (neon && Mathf.Repeat(Time.time + seed * .13f, 7.1f) < .07f)
                pulse *= .28f;
            lightSource.intensity = intensity * pulse;
            halo.color = new Color(haloColor.r, haloColor.g, haloColor.b,
                haloColor.a * pulse);
            streetPool.color = new Color(poolColor.r, poolColor.g, poolColor.b,
                poolColor.a * pulse);
        }
    }
}
