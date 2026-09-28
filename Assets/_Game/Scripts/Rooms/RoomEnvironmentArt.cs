using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NineLives
{
    [DisallowMultipleComponent]
    public sealed class RoomEnvironmentArt : MonoBehaviour
    {
        [SerializeField] private Sprite wetStreetFloor;
        [SerializeField] private Sprite streetFoundation;
        [SerializeField] private Sprite supportedPlatform;
        [SerializeField] private Texture2D hangingPlatform;
        [SerializeField] private Texture2D spikeStrip;
        [SerializeField] private Texture2D streetProps;
        [SerializeField] private Texture2D overheadProps;
        [SerializeField] private Texture2D neonLamps;
        [SerializeField] private Texture2D warmLamps;
        [SerializeField] private Texture2D streetLitter;
        [SerializeField] private Texture2D windDebris;
        [SerializeField] private Texture2D infrastructureProps;

        private Sprite[] groundSprites;
        private Sprite[] upperSprites;
        private Sprite[] neonSprites;
        private Sprite[] warmSprites;
        private Sprite[] litterSprites;
        private Sprite[] windSprites;
        private Sprite[] infrastructureSprites;
        private Sprite hangingSprite;
        private Sprite spikeSprite;
        private static Sprite puddleShape;
        private static Sprite haloShape;
        private static Sprite rippleShape;

        public void SetArt(Sprite floor, Texture2D ground, Texture2D upper)
        {
            wetStreetFloor = floor;
            streetProps = ground;
            overheadProps = upper;
        }

        private void Awake()
        {
            groundSprites = CutGrid(streetProps);
            upperSprites = CutGrid(overheadProps);
            neonSprites = CutGrid(neonLamps);
            warmSprites = CutGrid(warmLamps);
            litterSprites = CutGrid(streetLitter);
            windSprites = CutGrid(windDebris);
            infrastructureSprites = CutGrid(infrastructureProps);
            hangingSprite = CutTransparent(hangingPlatform);
            spikeSprite = CutTransparent(spikeStrip);
        }

        public void DressRoom(RoomLayout room)
        {
            if (room == null || wetStreetFloor == null) return;
            int roadIndex = 0;
            var powerAnchors = new List<Vector2>();
            foreach (var source in room.GetComponentsInChildren<SpriteRenderer>())
            {
                string name = source.gameObject.name;
                if (name == "Pipe")
                {
                    source.enabled = false;
                    continue;
                }
                Bounds bounds = source.bounds;
                bool hanging = name == "Awning" || name == "Low ledge" || name == "High ledge";
                bool supported = name == "Bridge";
                bool raised = hanging || supported;
                bool floor = name == "Road" || name.ToLowerInvariant().Contains("quay");
                if (raised && (hanging ? hangingSprite != null : supportedPlatform != null))
                {
                    source.enabled = false;
                    if (hanging) AddHangingPlatform(room, bounds);
                    else AddSupportedPlatform(room, bounds);
                    if (name == "Awning" || supported) AddSpikeHazard(room, bounds);
                }
                else if (floor)
                {
                    if (streetFoundation != null)
                    {
                        var baseArt = AddSprite(room.transform, "Street foundation", streetFoundation,
                            new Vector2(bounds.center.x, bounds.max.y - 5f), Vector2.one, 0);
                        var baseRenderer = baseArt.GetComponent<SpriteRenderer>();
                        baseRenderer.drawMode = SpriteDrawMode.Tiled;
                        baseRenderer.size = new Vector2(bounds.size.x, 10f);
                    }
                    var floorArt = AddSprite(room.transform, "Wet street floor", wetStreetFloor,
                        new Vector2(bounds.center.x, bounds.max.y - .1875f), Vector2.one, 1);
                    var floorRenderer = floorArt.GetComponent<SpriteRenderer>();
                    floorRenderer.drawMode = SpriteDrawMode.Tiled;
                    floorRenderer.size = new Vector2(bounds.size.x, wetStreetFloor.bounds.size.y);
                }
                if (name == "Crate" && groundSprites != null)
                {
                    source.enabled = false;
                    var crate = AddSprite(room.transform, "Textured crate", groundSprites[1],
                        new Vector2(bounds.center.x,
                            bounds.min.y + groundSprites[1].bounds.size.y * .13f),
                        Vector2.one * .26f, 2);
                    PuddleReflection.Ensure(crate.GetComponent<SpriteRenderer>());
                }
                if ((name == "Awning" || supported) && upperSprites != null && upperSprites[1] != null &&
                    (StableHash(room.name + name) & 1) == 0)
                {
                    Sprite vent = upperSprites[1];
                    float scale = .28f;
                    var prop = AddSprite(room.transform, "Platform vent", vent,
                        new Vector2(bounds.center.x,
                            bounds.max.y + vent.bounds.size.y * scale * .48f),
                        Vector2.one * scale, 3);
                    prop.layer = LayerMask.NameToLayer("Ground");
                    prop.AddComponent<BoxCollider2D>().size =
                        new Vector2(vent.bounds.size.x * .85f, vent.bounds.size.y * .75f);
                }
                if (!floor) continue;

                AddCityUtilityProps(room, bounds, roadIndex);
                AddPuddles(room.transform, bounds, roadIndex);
                AddLitter(room, bounds, roadIndex);

                if (groundSprites != null)
                {
                    int seed = StableHash(room.name) ^ Mathf.RoundToInt(bounds.center.x * 37f);
                    int count = bounds.size.x > 20f ? 3 : (Mathf.Abs(seed % 4) == 0 ? 0 : 1);
                    for (int i = 0; i < count; i++)
                    {
                        int type = (seed & int.MaxValue) % groundSprites.Length;
                        type = (type + i * 3) % groundSprites.Length;
                        float x = Mathf.Lerp(bounds.min.x + 2.5f, bounds.max.x - 2.5f,
                            (i + .5f) / count);
                        if (!ClearOfSpawn(room, x)) continue;
                        Sprite sprite = groundSprites[type];
                        float scale = type == 2 ? .4f : .23f;
                        var prop = AddSprite(room.transform, "Street prop " + type, sprite,
                            new Vector2(x, bounds.max.y + sprite.bounds.size.y * scale * .48f),
                            Vector2.one * scale, type == 2 ? 1 : 2);
                        if (type == 2) prop.GetComponent<SpriteRenderer>().color = new Color(.52f, .55f, .59f);
                        else if (type == 3) prop.GetComponent<SpriteRenderer>().color = new Color(.54f, .58f, .63f);
                        else AddSolidTop(prop, type);
                    }
                }

                AddStreetFixtures(room, bounds, roadIndex,
                    name.ToLowerInvariant().Contains("quay"), powerAnchors);

                roadIndex++;
            }
            StreetPowerLines.Connect(room.transform, powerAnchors);
        }

        private void AddCityUtilityProps(RoomLayout room, Bounds road, int roadIndex)
        {
            if (infrastructureSprites == null) return;
            int count = Mathf.Max(1, Mathf.FloorToInt(road.size.x / 9f));
            int seed = StableHash(room.name + ":utility:" + roadIndex);
            for (int i = 0; i < count; i++)
            {
                int type = ((seed & int.MaxValue) % 2 + i) % 2;
                Sprite sprite = infrastructureSprites[type];
                if (sprite == null) continue;
                float x = Mathf.Lerp(road.min.x + 2f, road.max.x - 2f, (i + .5f) / count);
                float scale = .32f;
                var prop = AddSprite(room.transform, "Dark city utility prop " + type, sprite,
                    new Vector2(x, road.max.y + sprite.bounds.size.y * scale * .46f),
                    Vector2.one * scale, -4);
                prop.GetComponent<SpriteRenderer>().color = new Color(.64f, .69f, .73f, .82f);
            }
        }

        private static bool ClearOfSpawn(RoomLayout room, float x)
        {
            if (Mathf.Abs(room.SafeSpawn.position.x - x) < 2f ||
                Mathf.Abs(room.Exit.position.x - x) < 1.5f) return false;
            foreach (var spawn in room.EnemySpawns)
                if (spawn != null && Mathf.Abs(spawn.position.x - x) < 1.5f) return false;
            return true;
        }

        private void AddLitter(RoomLayout room, Bounds road, int roadIndex)
        {
            if (litterSprites == null || road.size.x < 5f) return;
            var random = new System.Random(StableHash(room.name + ":litter:" + roadIndex));
            int count = Mathf.FloorToInt(road.size.x / 4f);
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(road.min.x + .7f, road.max.x - .7f,
                    (i + .2f + (float)random.NextDouble() * .6f) / count);
                if (!ClearOfSpawn(room, x)) continue;
                Sprite sprite = litterSprites[random.Next(litterSprites.Length)];
                if (sprite == null) continue;
                float scale = .09f + (float)random.NextDouble() * .04f;
                var litter = AddSprite(room.transform, "Street litter", sprite,
                    new Vector2(x, road.max.y + sprite.bounds.size.y * scale * .35f),
                    Vector2.one * scale, 2);
                litter.GetComponent<SpriteRenderer>().color = new Color(.6f, .63f, .69f, .88f);
            }
            if (windSprites == null) return;
            int flyers = Mathf.Max(2, Mathf.FloorToInt(road.size.x / 5f));
            for (int i = 0; i < flyers; i++)
            {
                float left = Mathf.Lerp(road.min.x + 1f, road.max.x - 1f, i / (float)flyers);
                float right = Mathf.Lerp(road.min.x + 1f, road.max.x - 1f, (i + 1f) / flyers);
                float x = Mathf.Lerp(left, right, .3f + (float)random.NextDouble() * .4f);
                int type = (random.Next(windSprites.Length) + i) % windSprites.Length;
                Sprite sprite = windSprites[type];
                if (sprite == null) continue;
                var scrap = AddSprite(room.transform, "Windblown leaf, paper or newspaper", sprite,
                    new Vector2(x, road.max.y + .07f), Vector2.one * .07f, 3);
                scrap.GetComponent<SpriteRenderer>().color = new Color(.68f, .69f, .66f, .86f);
                scrap.AddComponent<WindblownDebris>().Initialize(left, right, random.Next());
            }
        }

        private void AddStreetFixtures(RoomLayout room, Bounds road, int roadIndex,
            bool canal, List<Vector2> powerAnchors)
        {
            if (neonSprites == null || warmSprites == null || road.size.x < 7f) return;
            int seed = StableHash(room.name + ":lights:" + roadIndex);
            int count = road.size.x >= 18f ? 2 : (roadIndex % 2 == 0 ? 1 : 0);
            for (int i = 0; i < count; i++)
            {
                float[] fractions = count == 2
                    ? (i == 0 ? new[] { .23f, .32f, .4f } : new[] { .77f, .68f, .6f })
                    : new[] { .54f, .3f, .7f };
                float x = 0f;
                bool found = false;
                foreach (float fraction in fractions)
                {
                    x = Mathf.Lerp(road.min.x, road.max.x, fraction);
                    if (!ClearOfSpawn(room, x) || !ClearOfProps(room, x)) continue;
                    found = true;
                    break;
                }
                if (!found) continue;
                bool neon = count == 2 ? i == 1 : ((seed >> (i * 5)) & 3) == 0;
                int variant = neon ? 3 : ((seed >> (i * 3 + 2)) & int.MaxValue) % 3;
                Sprite sprite = neon ? neonSprites[variant] : warmSprites[variant];
                if (sprite == null) continue;
                float height = canal ? 5.8f : 4.8f;
                float scale = height / sprite.bounds.size.y;
                var fixture = new GameObject(neon ? "Grounded neon lamp" : "Grounded street lamp");
                fixture.transform.SetParent(room.transform, false);
                fixture.transform.position = new Vector3(x, road.max.y, 0f);
                var body = AddSprite(fixture.transform, "Lamp body", sprite,
                    new Vector2(x, road.max.y + height * .5f), Vector2.one * scale, 2);
                body.GetComponent<SpriteRenderer>().color = new Color(.6f, .65f, .68f);
                powerAnchors.Add(new Vector2(x, road.max.y + height * .88f));

                Color tint = neon ? variant switch
                {
                    1 => new Color(1f, .26f, .79f),
                    2 => new Color(.68f, .35f, 1f),
                    3 => new Color(.32f, .85f, 1f),
                    _ => new Color(.2f, .85f, 1f)
                } : new Color(1f, .68f, .35f);
                float bulbY = height * .80f;
                var halo = AddSprite(fixture.transform, "Lamp halo", HaloShape,
                    new Vector2(x, road.max.y + bulbY),
                    Vector2.one * .85f, 2).GetComponent<SpriteRenderer>();
                halo.color = new Color(tint.r, tint.g, tint.b, .08f);
                var pool = AddSprite(fixture.transform, "Light on wet street", PuddleShape,
                    new Vector2(x, road.max.y - .22f), new Vector2(1.65f, .88f), 2)
                    .GetComponent<SpriteRenderer>();
                pool.color = new Color(tint.r, tint.g, tint.b, neon ? .09f : .06f);

                var lightObject = new GameObject("Animated lamp light", typeof(Light2D));
                lightObject.transform.SetParent(fixture.transform, false);
                lightObject.transform.position = new Vector3(x, road.max.y + bulbY, 0f);
                var light = lightObject.GetComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = tint;
                light.intensity = neon ? .38f : .46f;
                light.pointLightInnerRadius = .45f;
                light.pointLightOuterRadius = neon ? 2.6f : 2.9f;
                lightObject.AddComponent<StreetLampPulse>().Initialize(halo, pool, neon,
                    seed + i * 23.71f);
            }
        }

        private static bool ClearOfProps(RoomLayout room, float x)
        {
            foreach (var collider in room.GetComponentsInChildren<BoxCollider2D>())
            {
                string name = collider.gameObject.name;
                if ((name.StartsWith("Street prop") || name == "Crate" || name == "Platform vent") &&
                    Mathf.Abs(collider.bounds.center.x - x) < collider.bounds.extents.x + .7f)
                    return false;
            }
            return true;
        }

        private static Sprite HaloShape
        {
            get
            {
                if (haloShape != null) return haloShape;
                const int size = 32;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float radius = new Vector2(x - 15.5f, y - 15.5f).magnitude / 15.5f;
                        byte alpha = (byte)(Mathf.Pow(Mathf.Clamp01(1f - radius), 2f) * 170f);
                        pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                    }
                texture.SetPixels32(pixels);
                texture.Apply();
                haloShape = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                    new Vector2(.5f, .5f), 16f);
                return haloShape;
            }
        }

        private void AddSupportedPlatform(RoomLayout room, Bounds deck)
        {
            float groundTop = 0f;
            foreach (var collider in room.GetComponentsInChildren<BoxCollider2D>())
            {
                string name = collider.gameObject.name;
                if (name != "Road" && !name.ToLowerInvariant().Contains("quay")) continue;
                if (collider.bounds.min.x > deck.max.x || collider.bounds.max.x < deck.min.x) continue;
                groundTop = Mathf.Max(groundTop, collider.bounds.max.y);
            }

            // The art's feet and walking deck sit at 14.6% and 62% of its image height.
            const float feet = .146f;
            const float walkingDeck = .62f;
            float deckHeight = Mathf.Max(.7f, deck.max.y - groundTop);
            float scaleY = deckHeight / (supportedPlatform.bounds.size.y * (walkingDeck - feet));
            float pivotY = groundTop - (feet - .5f) * supportedPlatform.bounds.size.y * scaleY;
            var scaffold = AddSprite(room.transform, "Grounded metal catwalk", supportedPlatform,
                new Vector2(deck.center.x, pivotY),
                new Vector2(deck.size.x / supportedPlatform.bounds.size.x, scaleY), 1);
            scaffold.GetComponent<SpriteRenderer>().flipX = (Mathf.RoundToInt(deck.center.x) & 1) != 0;
        }

        private void AddHangingPlatform(RoomLayout room, Bounds deck)
        {
            const float height = .56f;
            float scaleY = height / hangingSprite.bounds.size.y;
            float pivotY = deck.max.y - height * .5f;
            AddSprite(room.transform, "Hanging masonry ledge", hangingSprite,
                new Vector2(deck.center.x, pivotY),
                new Vector2(deck.size.x / hangingSprite.bounds.size.x, scaleY), 1);
        }

        private void AddSpikeHazard(RoomLayout room, Bounds deck)
        {
            if (spikeSprite == null || deck.size.x < 3f) return;
            float x = deck.max.x - .7f;
            var hazard = new GameObject("Platform edge spikes", typeof(BoxCollider2D),
                typeof(SpikeHazard));
            hazard.transform.SetParent(room.transform, false);
            hazard.layer = LayerMask.NameToLayer("EnemyHitbox");
            hazard.transform.position = new Vector3(x, deck.max.y, 0f);
            var collider = hazard.GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(.95f, .38f);
            collider.offset = new Vector2(0f, .2f);
            AddSprite(hazard.transform, "Bolted spikes", spikeSprite,
                new Vector2(x, deck.max.y + .22f),
                new Vector2(1.05f / spikeSprite.bounds.size.x,
                    .44f / spikeSprite.bounds.size.y), 4);
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 17;
                foreach (char character in value) hash = hash * 31 + character;
                return hash;
            }
        }

        private static void AddSolidTop(GameObject prop, int type)
        {
            prop.layer = LayerMask.NameToLayer("Ground");
            var collider = prop.AddComponent<BoxCollider2D>();
            collider.size = type switch
            {
                0 => new Vector2(3.2f, 4.2f),
                1 => new Vector2(4.4f, 4.4f),
                2 => new Vector2(6.2f, 2.5f),
                _ => new Vector2(3.8f, 4.3f)
            };
            PuddleReflection.Ensure(prop.GetComponent<SpriteRenderer>());
        }

        private static void AddPuddles(Transform parent, Bounds road, int roadIndex)
        {
            for (int i = 0; i < 2; i++)
            {
                float x = road.min.x + road.size.x * (i == 0 ? .28f : .72f);
                var puddle = new GameObject("Reflective puddle", typeof(SpriteMask));
                puddle.transform.SetParent(parent, false);
                puddle.transform.position = new Vector3(x, road.max.y - .42f, 0f);
                puddle.transform.localScale = new Vector3(1.6f + (roadIndex % 2) * .35f, 1.3f, 1f);
                puddle.GetComponent<SpriteMask>().sprite = PuddleShape;
                var surface = new GameObject("Puddle surface", typeof(SpriteRenderer));
                surface.transform.SetParent(puddle.transform, false);
                var renderer = surface.GetComponent<SpriteRenderer>();
                renderer.sprite = PuddleShape;
                renderer.color = new Color(.22f, .39f, .54f, .32f);
                renderer.sortingOrder = 2;
                var ripple = new GameObject("Rain ripple", typeof(SpriteRenderer));
                ripple.transform.SetParent(puddle.transform, false);
                ripple.transform.localPosition = new Vector3(i == 0 ? -.28f : .24f, 0f, 0f);
                var rippleRenderer = ripple.GetComponent<SpriteRenderer>();
                rippleRenderer.sprite = RippleShape;
                rippleRenderer.sortingOrder = 4;
                rippleRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                ripple.AddComponent<PuddleRipple>().Initialize(roadIndex * .37f + i * .53f);
            }
        }

        private static Sprite RippleShape
        {
            get
            {
                if (rippleShape != null) return rippleShape;
                const int width = 24, height = 12;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;
                var pixels = new Color32[width * height];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        float nx = (x - 11.5f) / 11.5f;
                        float ny = (y - 5.5f) / 5.5f;
                        float distance = nx * nx + ny * ny;
                        pixels[y * width + x] = distance >= .57f && distance <= 1f
                            ? new Color32(166, 220, 255, 185) : new Color32(0, 0, 0, 0);
                    }
                texture.SetPixels32(pixels);
                texture.Apply();
                rippleShape = Sprite.Create(texture, new Rect(0f, 0f, width, height),
                    new Vector2(.5f, .5f), 16f);
                return rippleShape;
            }
        }

        private static Sprite PuddleShape
        {
            get
            {
                if (puddleShape != null) return puddleShape;
                const int width = 32, height = 8;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;
                var pixels = new Color32[width * height];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        float nx = (x - 15.5f) / 15.5f;
                        float ny = (y - 3.5f) / 3.5f;
                        pixels[y * width + x] = nx * nx + ny * ny <= 1f
                            ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                    }
                texture.SetPixels32(pixels);
                texture.Apply();
                puddleShape = Sprite.Create(texture, new Rect(0f, 0f, width, height),
                    new Vector2(.5f, .5f), 16f);
                return puddleShape;
            }
        }

        private static Sprite[] CutGrid(Texture2D atlas)
        {
            if (atlas == null) return null;
            int halfWidth = atlas.width / 2;
            int halfHeight = atlas.height / 2;
            Color32[] pixels = atlas.GetPixels32();
            var sprites = new Sprite[4];
            for (int i = 0; i < sprites.Length; i++)
            {
                int column = i % 2;
                int row = 1 - i / 2;
                int left = column * halfWidth;
                int bottom = row * halfHeight;
                int minX = left + halfWidth, minY = bottom + halfHeight;
                int maxX = left, maxY = bottom;
                for (int y = bottom; y < bottom + halfHeight; y++)
                    for (int x = left; x < left + halfWidth; x++)
                        if (pixels[y * atlas.width + x].a > 24)
                        {
                            minX = Mathf.Min(minX, x);
                            minY = Mathf.Min(minY, y);
                            maxX = Mathf.Max(maxX, x);
                            maxY = Mathf.Max(maxY, y);
                        }
                if (minX > maxX) continue;
                sprites[i] = Sprite.Create(atlas,
                    new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1),
                    new Vector2(.5f, .5f), atlas.width / 15.36f);
            }
            return sprites;
        }

        private static Sprite CutTransparent(Texture2D texture)
        {
            if (texture == null) return null;
            Color32[] pixels = texture.GetPixels32();
            int minX = texture.width, minY = texture.height, maxX = 0, maxY = 0;
            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                    if (pixels[y * texture.width + x].a > 24)
                    {
                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
            if (minX > maxX) return null;
            return Sprite.Create(texture, new Rect(minX, minY, maxX - minX + 1,
                    maxY - minY + 1), new Vector2(.5f, .5f), texture.width / 15.36f);
        }

        private static GameObject AddSprite(Transform parent, string name, Sprite sprite, Vector2 position,
            Vector2 scale, int order)
        {
            var decoration = new GameObject(name);
            decoration.transform.SetParent(parent, false);
            decoration.transform.position = position;
            decoration.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var renderer = decoration.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return decoration;
        }

        private void OnDestroy()
        {
            DestroySprites(groundSprites);
            DestroySprites(upperSprites);
            DestroySprites(neonSprites);
            DestroySprites(warmSprites);
            DestroySprites(litterSprites);
            DestroySprites(windSprites);
            DestroySprites(infrastructureSprites);
            if (hangingSprite != null) Destroy(hangingSprite);
            if (spikeSprite != null) Destroy(spikeSprite);
        }

        private static void DestroySprites(Sprite[] sprites)
        {
            if (sprites == null) return;
            foreach (var sprite in sprites)
                if (sprite != null) Destroy(sprite);
        }
    }
}
