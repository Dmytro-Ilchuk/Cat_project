using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NineLives
{
    [RequireComponent(typeof(Light2D))]
    public sealed class WorldLightFlicker : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float variation = .18f;
        [SerializeField, Min(.1f)] private float speed = 2.2f;
        private Light2D lightSource;
        private float baseIntensity;
        private float seed;

        private void Awake()
        {
            lightSource = GetComponent<Light2D>();
            baseIntensity = lightSource.intensity;
            seed = transform.position.x * 17.73f + transform.position.y * 9.17f;
        }

        private void Update()
        {
            float noise = Mathf.PerlinNoise(seed, Time.time * speed);
            lightSource.intensity = baseIntensity * Mathf.Lerp(1f - variation, 1f + variation, noise);
        }
    }
}
