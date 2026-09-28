using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NineLives
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PixelPerfectCamera))]
    public sealed class RoomAtmosphere : MonoBehaviour
    {
        private const int DropCount = 38;
        private readonly Transform[] drops = new Transform[DropCount];
        private readonly float[] speeds = new float[DropCount];
        private readonly SpriteRenderer[] fogLayers = new SpriteRenderer[4];
        private readonly float[] fogDrift = { .13f, -.09f, .20f, -.14f };
        private readonly float[] fogOpacity = { .16f, .13f, .07f, .055f };
        private Sprite rainSprite;
        private Sprite fogSprite;
        private Texture2D rainTexture;
        private Texture2D fogTexture;
        private float halfWidth;
        private float halfHeight;
        private float fogLeft;
        private float fogRight;

        private void Awake()
        {
            var pixels = GetComponent<PixelPerfectCamera>();
            halfWidth = pixels.refResolutionX / (2f * pixels.assetsPPU);
            halfHeight = pixels.refResolutionY / (2f * pixels.assetsPPU);

            fogTexture = new Texture2D(64, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float horizontal = Mathf.Sin(x / 63f * Mathf.PI);
                    float vertical = Mathf.Sin(y / 31f * Mathf.PI);
                    float wisps = .7f + .3f * Mathf.Sin(x * .19f + y * .11f);
                    fogTexture.SetPixel(x, y, new Color(1f, 1f, 1f,
                        Mathf.Pow(horizontal * vertical, 1.5f) * wisps));
                }
            }
            fogTexture.Apply();
            fogSprite = Sprite.Create(fogTexture, new Rect(0, 0, 64, 32), new Vector2(.5f, .5f), 16);

            rainTexture = new Texture2D(2, 10, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 2; x++)
                    rainTexture.SetPixel(x, y, x == (y < 5 ? 0 : 1)
                        ? new Color(1f, 1f, 1f, .35f + .3f * y / 10f) : Color.clear);
            rainTexture.Apply();
            rainSprite = Sprite.Create(rainTexture, new Rect(0, 0, 2, 10),
                new Vector2(.5f, .5f), 16);
            var random = new System.Random(61547);
            for (int i = 0; i < DropCount; i++)
            {
                var drop = new GameObject("Soft rain", typeof(SpriteRenderer));
                drop.transform.SetParent(transform, false);
                drop.transform.localPosition = new Vector3(
                    Mathf.Lerp(-halfWidth, halfWidth, (float)random.NextDouble()),
                    Mathf.Lerp(-halfHeight, halfHeight, (float)random.NextDouble()), 10f);
                var renderer = drop.GetComponent<SpriteRenderer>();
                renderer.sprite = rainSprite;
                renderer.sortingOrder = 36;
                renderer.color = new Color(.65f, .76f, .88f, .24f);
                drops[i] = drop.transform;
                speeds[i] = Mathf.Lerp(5f, 8f, (float)random.NextDouble());
            }
        }

        public void SetRoom(RoomLayout room)
        {
            if (room == null) return;
            for (int i = 0; i < fogLayers.Length; i++)
                if (fogLayers[i] != null) Destroy(fogLayers[i].gameObject);

            Bounds bounds = room.CameraBounds;
            fogLeft = bounds.min.x;
            fogRight = bounds.max.x;
            float spread = bounds.size.x * .22f;
            float width = Mathf.Max(18f, bounds.size.x * .7f);
            float center = bounds.center.x;
            float ground = room.SafeSpawn.position.y - 1f;
            AddFog(room.transform, 0, "Distant drifting fog", -3,
                center - spread, ground + 2.1f, width, 4.2f);
            AddFog(room.transform, 1, "Distant drifting fog", -3,
                center + spread, ground + 1.5f, width, 3.8f);
            AddFog(room.transform, 2, "Thin foreground mist", 35,
                center - spread, ground + .8f, width, 3.6f);
            AddFog(room.transform, 3, "Thin foreground mist", 35,
                center + spread, ground + .4f, width, 3.5f);
        }

        private void AddFog(Transform parent, int index, string name, int order, float x, float y,
            float width, float height)
        {
            var fog = new GameObject(name, typeof(SpriteRenderer));
            fog.transform.SetParent(parent, false);
            fog.transform.position = new Vector3(x, y, 0f);
            fog.transform.localScale = new Vector3(width / fogSprite.bounds.size.x,
                height / fogSprite.bounds.size.y, 1f);
            var renderer = fog.GetComponent<SpriteRenderer>();
            renderer.sprite = fogSprite;
            renderer.sortingOrder = order;
            renderer.color = new Color(.48f, .61f, .73f, fogOpacity[index]);
            fogLayers[index] = renderer;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            for (int i = 0; i < fogLayers.Length; i++)
            {
                var renderer = fogLayers[i];
                if (renderer == null) continue;
                var position = renderer.transform.position;
                position.x += fogDrift[i] * dt;
                float left = fogLeft - renderer.bounds.size.x * .5f;
                float right = fogRight + renderer.bounds.size.x * .5f;
                if (position.x > right) position.x = left;
                if (position.x < left) position.x = right;
                renderer.transform.position = position;
                var color = renderer.color;
                color.a = fogOpacity[i] * (.86f + .14f * Mathf.Sin(Time.time * .55f + i * 1.7f));
                renderer.color = color;
            }
            for (int i = 0; i < drops.Length; i++)
            {
                Vector3 position = drops[i].localPosition;
                position.x -= speeds[i] * .16f * dt;
                position.y -= speeds[i] * dt;
                if (position.y < -halfHeight - .5f)
                {
                    position.y = halfHeight + .5f;
                    position.x = Mathf.Repeat(position.x + halfWidth * 2f + i * .67f,
                        halfWidth * 2f) - halfWidth;
                }
                drops[i].localPosition = position;
            }
        }

        private void OnDestroy()
        {
            if (rainSprite != null) Destroy(rainSprite);
            if (fogSprite != null) Destroy(fogSprite);
            if (rainTexture != null) Destroy(rainTexture);
            if (fogTexture != null) Destroy(fogTexture);
        }
    }
}
