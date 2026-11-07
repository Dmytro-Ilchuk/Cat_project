using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NineLives
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(100)]
    public sealed class RoomParallax : MonoBehaviour
    {
        [SerializeField] private Sprite distantCity;
        [SerializeField] private Sprite buildings;
        [SerializeField] private Sprite alternateBuildings;
        [SerializeField] private Sprite foreground;
        [SerializeField] private Sprite nightSky;
        [SerializeField] private Sprite lowerForeground;
        [SerializeField] private Sprite streetBackdrop;
        [SerializeField] private Sprite roofBackdrop;
        [SerializeField] private Sprite canalBackdrop;
        private readonly SpriteRenderer[] panoramas = new SpriteRenderer[3];
        private Sprite panorama;
        private float panoramaY;

        private readonly SpriteRenderer[,] tiles = new SpriteRenderer[3, 3];
        private readonly SpriteRenderer[] lowerTiles = new SpriteRenderer[9];
        private readonly float[] factors = { .12f, .35f, .62f };
        private readonly float[] bottomY = { -2f, -1f, -.25f };
        private float lowerBottomY = -3.1f;
        private static Sprite roofCable;

        public void SetArt(Sprite distant, Sprite middle, Sprite near)
        {
            distantCity = distant;
            buildings = middle;
            foreground = near;
        }

        public void SetAlternateBuildings(Sprite alternate) => alternateBuildings = alternate;

        public void SetRoom(RoomLayout room)
        {
            if (room == null) return;
            if (streetBackdrop != null)
            {
                bool canal = room.name.ToLowerInvariant().Contains("canal");
                panorama = canal ? canalBackdrop : room.name.ToLowerInvariant().Contains("roof") ? roofBackdrop : streetBackdrop;
                if (panorama == null) throw new System.InvalidOperationException("Campaign background missing.");
                panoramaY = room.SafeSpawn.position.y - 1f + (canal ? 6.1f : 3.5f);
                foreach (var renderer in panoramas) renderer.sprite = panorama;
            }
            lowerBottomY = room.SafeSpawn.position.y - 4f;
            foreach (var renderer in room.GetComponentsInChildren<SpriteRenderer>())
            {
                string name = renderer.gameObject.name;
                if (name.StartsWith("House ") || name == "Night backdrop" || name == "Backdrop")
                    renderer.enabled = false;
            }
        }

        private void Awake()
        {
            GetComponent<Camera>().backgroundColor = new Color(.018f, .025f, .043f);
            if (streetBackdrop != null)
            {
                for (int i = 0; i < panoramas.Length; i++)
                {
                    var go = new GameObject("Generated city panorama " + i, typeof(SpriteRenderer));
                    go.transform.SetParent(transform, false);
                    panoramas[i] = go.GetComponent<SpriteRenderer>();
                    panoramas[i].sortingOrder = -9;
                    panoramas[i].color = new Color(.82f, .86f, .95f);
                }
                return;
            }
            if (nightSky != null)
            {
                var sky = new GameObject("Clouded night sky", typeof(SpriteRenderer));
                sky.transform.SetParent(transform, false);
                sky.transform.localPosition = new Vector3(0f, 0f, 10f);
                var renderer = sky.GetComponent<SpriteRenderer>();
                renderer.sprite = nightSky;
                renderer.sortingOrder = -20;
                var pixels = GetComponent<PixelPerfectCamera>();
                sky.transform.localScale = new Vector3(
                    pixels.refResolutionX / (float)pixels.assetsPPU / nightSky.bounds.size.x * 1.05f,
                    pixels.refResolutionY / (float)pixels.assetsPPU / nightSky.bounds.size.y * 1.05f, 1f);
            }
            Sprite[] art = { distantCity, buildings, foreground };
            for (int layer = 0; layer < art.Length; layer++)
            {
                if (art[layer] == null) continue;
                for (int tile = 0; tile < 3; tile++)
                {
                    var part = new GameObject($"Parallax {layer + 1} tile {tile + 1}");
                    part.transform.SetParent(transform, false);
                    var renderer = part.AddComponent<SpriteRenderer>();
                    renderer.sprite = art[layer];
                    renderer.sortingOrder = -9 + layer;
                    renderer.color = layer == 0 ? new Color(.46f, .54f, .66f, .7f)
                        : layer == 1 ? new Color(.62f, .69f, .78f, .78f)
                        : new Color(.64f, .7f, .76f, .8f);
                    if (layer == 1)
                    {
                        var cable = new GameObject("Distant roof cables", typeof(SpriteRenderer));
                        cable.transform.SetParent(part.transform, false);
                        cable.transform.localPosition = new Vector3(tile == 1 ? 2.1f : -2.8f,
                            art[layer].bounds.size.y * .18f, 0f);
                        var cableRenderer = cable.GetComponent<SpriteRenderer>();
                        cableRenderer.sprite = RoofCable;
                        cableRenderer.sortingOrder = -7;
                        cableRenderer.color = new Color(.56f, .68f, .78f, .7f);
                    }
                    tiles[layer, tile] = renderer;
                }
            }
            if (lowerForeground != null)
                for (int i = 0; i < lowerTiles.Length; i++)
                {
                    var part = new GameObject("Lower foreground parallax " + i, typeof(SpriteRenderer));
                    part.transform.SetParent(transform, false);
                    var renderer = part.GetComponent<SpriteRenderer>();
                    renderer.sprite = lowerForeground;
                    renderer.sortingOrder = 24;
                    renderer.color = new Color(.78f, .83f, .84f, .78f);
                    lowerTiles[i] = renderer;
                }
            PositionTiles();
        }

        private void LateUpdate() => PositionTiles();

        private void PositionTiles()
        {
            if (streetBackdrop != null)
            {
                if (panorama == null) return;
                float width = panorama.bounds.size.x;
                int middle = Mathf.RoundToInt(transform.position.x * .65f / width);
                for (int i = 0; i < panoramas.Length; i++)
                    panoramas[i].transform.position = new Vector3(
                        Mathf.Round(((middle + i - 1) * width + transform.position.x * .35f) * 16f) / 16f,
                        panoramaY, 0);
                return;
            }
            Sprite[] art = { distantCity, buildings, foreground };
            for (int layer = 0; layer < art.Length; layer++)
            {
                if (art[layer] == null) continue;
                float width = art[layer].bounds.size.x;
                if (width <= 0f) continue;
                int middleIndex = Mathf.RoundToInt(transform.position.x * (1f - factors[layer]) / width);
                for (int tile = 0; tile < 3; tile++)
                {
                    var renderer = tiles[layer, tile];
                    if (renderer == null) continue;
                    int index = middleIndex + tile - 1;
                    if (layer == 1 && alternateBuildings != null)
                        renderer.sprite = (index & 1) == 0 ? buildings : alternateBuildings;
                    float centerY = bottomY[layer] + renderer.sprite.bounds.size.y * .5f;
                    renderer.flipX = (index & 1) != 0;
                    float shade = 1f - (Mathf.Abs(index * 13 + layer * 7) % 3) * .06f;
                    Color baseColor = layer == 0 ? new Color(.46f, .54f, .66f, .7f)
                        : layer == 1 ? new Color(.62f, .69f, .78f, .78f)
                        : new Color(.64f, .7f, .76f, .8f);
                    renderer.color = new Color(baseColor.r * shade, baseColor.g * shade,
                        baseColor.b * shade, baseColor.a);
                    if (layer == 1)
                    {
                        var cable = renderer.transform.Find("Distant roof cables");
                        if (cable != null)
                            cable.localPosition = new Vector3(tile == 1 ? 2.1f : -2.8f,
                                renderer.sprite.bounds.size.y * .18f, 0f);
                    }
                    renderer.transform.position = new Vector3(
                        index * width + transform.position.x * factors[layer],
                        centerY + transform.position.y * factors[layer] * .08f,
                        0f);
                }
            }
            if (lowerForeground == null) return;
            const float factor = -.15f;
            float tileWidth = lowerForeground.bounds.size.x;
            int middle = Mathf.RoundToInt(transform.position.x * (1f - factor) / tileWidth);
            for (int i = 0; i < lowerTiles.Length; i++)
            {
                int index = middle + i - lowerTiles.Length / 2;
                var renderer = lowerTiles[i];
                renderer.flipX = (index & 1) != 0;
                renderer.transform.position = new Vector3(
                    index * tileWidth + transform.position.x * factor,
                    lowerBottomY + lowerForeground.bounds.size.y * .5f, 0f);
            }
        }

        private static Sprite RoofCable
        {
            get
            {
                if (roofCable != null) return roofCable;
                const int width = 128, height = 48;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;
                var pixels = new Color32[width * height];
                for (int x = 0; x < width; x++)
                {
                    float phase = x / (width - 1f);
                    int y = Mathf.RoundToInt(36f - 12f * Mathf.Sin(phase * Mathf.PI));
                    for (int offset = 0; offset < 2; offset++)
                    {
                        pixels[(y + offset) * width + x] = new Color32(55, 76, 96, 230);
                        pixels[(y - 6 + offset) * width + x] = new Color32(37, 53, 72, 185);
                    }
                    if (x < 5 || x > width - 6)
                        for (int bolt = -2; bolt <= 2; bolt++)
                            pixels[(y + bolt) * width + x] = new Color32(78, 91, 103, 230);
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                roofCable = Sprite.Create(texture, new Rect(0f, 0f, width, height),
                    new Vector2(.5f, .5f), 16f);
                return roofCable;
            }
        }
    }
}
