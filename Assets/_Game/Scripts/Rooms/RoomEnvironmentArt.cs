using UnityEngine;

namespace NineLives
{
    [DisallowMultipleComponent]
    public sealed class RoomEnvironmentArt : MonoBehaviour
    {
        [SerializeField] private Sprite wetStreetFloor;
        [SerializeField] private Sprite streetFoundation;
        [SerializeField] private Sprite supportedPlatform;
        [SerializeField] private Sprite[] cityProps;
        [SerializeField] private Sprite[] citySigns;
        [SerializeField] private Sprite exitDoor;
        [SerializeField] private Texture2D spikeStrip;
        // Retained for the older art-import menu and RoomLab serialization.
        [SerializeField, HideInInspector] private Texture2D hangingPlatform;
        [SerializeField, HideInInspector] private Texture2D streetProps;
        [SerializeField, HideInInspector] private Texture2D overheadProps;
        [SerializeField, HideInInspector] private Texture2D neonLamps;
        [SerializeField, HideInInspector] private Texture2D warmLamps;
        [SerializeField, HideInInspector] private Texture2D streetLitter;
        [SerializeField, HideInInspector] private Texture2D windDebris;
        [SerializeField, HideInInspector] private Texture2D infrastructureProps;
        private Sprite spikes;

        public void SetArt(Sprite floor, Texture2D ground, Texture2D upper)
        {
            wetStreetFloor = floor;
            streetProps = ground;
            overheadProps = upper;
        }

        public void DressRoom(RoomLayout room)
        {
            if (room == null || wetStreetFloor == null || streetFoundation == null) return;
            int roadIndex = 0;
            foreach (var source in room.GetComponentsInChildren<SpriteRenderer>())
            {
                string name = source.name.ToLowerInvariant();
                bool floor = name == "road" || name.Contains("quay");
                bool platform = name.Contains("ledge") || name.Contains("awning") || name == "bridge";
                if (name == "pipe") { source.enabled = false; continue; }
                if (!floor && !platform && name != "crate") continue;
                Bounds bounds = source.GetComponent<Collider2D>()?.bounds ?? source.bounds;
                source.enabled = false;
                float depth = floor ? 3f : bounds.size.y;
                Tile(room.transform, "Metal foundation", streetFoundation,
                    new Vector2(bounds.center.x, bounds.max.y - depth * .5f), new Vector2(bounds.size.x, depth), 0,
                    floor ? new Color(.5f, .58f, .66f) : new Color(.78f, .85f, .91f));
                Tile(room.transform, "Walkable edge", wetStreetFloor,
                    new Vector2(bounds.center.x, bounds.max.y - wetStreetFloor.bounds.extents.y),
                    new Vector2(bounds.size.x, wetStreetFloor.bounds.size.y), 1, Color.white);
                if (name == "bridge") AddSpikes(room, bounds);
                if (!floor) continue;
                DressStreet(room, bounds, roadIndex++);
            }
            if (exitDoor != null)
                Add(room.transform, "Door home", exitDoor,
                    new Vector2(room.Exit.position.x, room.Exit.position.y + exitDoor.bounds.extents.y - .5f), -1, Color.white);
        }

        private void DressStreet(RoomLayout room, Bounds road, int roadIndex)
        {
            if (cityProps == null || cityProps.Length == 0) return;
            int section = CycleController.Instance?.Run.Position ?? 0;
            int count = Mathf.Max(1, Mathf.FloorToInt(road.size.x / 8f));
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(road.min.x + 2f, road.max.x - 2f, (i + .5f) / count);
                if (Mathf.Abs(x - room.SafeSpawn.position.x) < 2f || Mathf.Abs(x - room.Exit.position.x) < 2f) continue;
                Sprite prop = cityProps[(i + roadIndex + section) % cityProps.Length];
                if (prop == null) continue;
                Add(room.transform, "City machinery", prop, new Vector2(x, road.max.y + prop.bounds.extents.y),
                    -2, new Color(.56f, .64f, .72f));
                if (citySigns == null || citySigns.Length == 0 || i % 2 != 0) continue;
                Sprite sign = citySigns[(section + i) % citySigns.Length];
                if (sign != null) Add(room.transform, "Neon landmark", sign,
                    new Vector2(x, road.max.y + 4.5f), -3, new Color(.85f, .85f, .9f));
            }
        }

        private static SpriteRenderer Add(Transform parent, string name, Sprite sprite, Vector2 position, int order, Color color)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(Mathf.Round(position.x * 16f) / 16f, Mathf.Round(position.y * 16f) / 16f, 0);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.color = color;
            return renderer;
        }

        private static void Tile(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, int order, Color color)
        {
            var renderer = Add(parent, name, sprite, position, order, color);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
        }

        private void AddSpikes(RoomLayout room, Bounds deck)
        {
            if (spikeStrip == null) return;
            if (spikes == null) spikes = Sprite.Create(spikeStrip, new Rect(0, 0, spikeStrip.width, spikeStrip.height),
                new Vector2(.5f, .5f), 16f);
            var hazard = new GameObject("Platform edge spikes", typeof(BoxCollider2D), typeof(SpikeHazard));
            hazard.transform.SetParent(room.transform, false);
            hazard.layer = LayerMask.NameToLayer("EnemyHitbox");
            hazard.transform.position = new Vector3(deck.max.x - .7f, deck.max.y, 0);
            var collider = hazard.GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(.95f, .38f);
            collider.offset = new Vector2(0, .2f);
            var art = Add(hazard.transform, "Spikes", spikes, (Vector2)hazard.transform.position + Vector2.up * .22f, 4, Color.white);
            art.transform.localScale = new Vector3(1.05f / spikes.bounds.size.x, .44f / spikes.bounds.size.y, 1);
        }
    }
}
