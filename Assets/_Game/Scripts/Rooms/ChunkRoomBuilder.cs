using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NineLives
{
    public static class ChunkRoomBuilder
    {
        public static RoomLayout Build(RoomDefinition room, int seed, RoomChunk[] library)
        {
            if (room == null || room.Kind != RoomKind.Combat || library == null)
                throw new ArgumentException("A combat room and chunk library are required.");
            var pieces = SelectPieces(room, seed, library);
            int nominalWidth = pieces.Sum(c => c.Width);
            var root = new GameObject(room.StableId + " chunks " + seed);
            try
            {
                var layout = root.AddComponent<RoomLayout>();
                var enemyPoints = new List<Transform>();
                float seamX = -nominalWidth / 2f;
                float seamY = 0f;
                foreach (var piece in pieces)
                {
                    var instance = UnityEngine.Object.Instantiate(piece, root.transform);
                    instance.name = piece.StableId;
                    // Align this chunk's incoming seam to the previous outgoing seam.
                    instance.transform.localPosition = new Vector3(
                        seamX - instance.LeftSeam.localPosition.x,
                        seamY - instance.LeftSeam.localPosition.y);
                    if (Vector2.Distance(instance.LeftSeam.position, new Vector2(seamX, seamY)) > .001f)
                        throw new InvalidOperationException(piece.StableId + " has an incompatible seam.");
                    enemyPoints.AddRange(instance.EnemySpawns);
                    AddStreetLight(instance.transform, piece.Biome == RoomBiome.Canals);
                    seamX = instance.RightSeam.position.x;
                    seamY = instance.RightSeam.position.y;
                }
                float left = -nominalWidth / 2f;
                float width = seamX - left;
                var entrance = Marker(root.transform, "Entrance", left + 2, 1);
                var spawn = Marker(root.transform, "Safe spawn", left + 4, 1);
                var departure = Marker(root.transform, "Exit", seamX - 2, seamY + 1);
                var camera = Area(root.transform, "Camera bounds", (left + seamX) * .5f, 4, width - 2, 12);
                var arena = Area(root.transform, "Arena bounds", (left + seamX) * .5f, 3.5f, width - 3, 9);
                layout.Initialize(entrance, departure, spawn, enemyPoints.ToArray(), camera, arena);
                return layout;
            }
            catch
            {
                root.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(root);
                else UnityEngine.Object.DestroyImmediate(root);
                throw;
            }
        }

        public static string Signature(RoomDefinition room, int seed, RoomChunk[] library)
        {
            var pieces = SelectPieces(room, seed, library);
            return room.Biome + ":" + pieces[1].StableId + "/" + pieces[2].StableId;
        }

        private static RoomChunk[] SelectPieces(RoomDefinition room, int seed, RoomChunk[] library)
        {
            if (room == null || room.Kind != RoomKind.Combat || library == null)
                throw new ArgumentException("A combat room and chunk library are required.");
            var pool = library.Where(c => c != null && c.Biome == room.Biome)
                .OrderBy(c => c.StableId, StringComparer.Ordinal).ToArray();
            var entry = pool.Single(c => c.Role == ChunkRole.Entry);
            var exit = pool.Single(c => c.Role == ChunkRole.Exit);
            var encounters = pool.Where(c => c.Role == ChunkRole.Encounter).ToList();
            if (encounters.Count < 2) throw new InvalidOperationException("Two distinct encounter chunks are required.");
            // Local RNG: effects and decoration cannot perturb room geometry.
            var random = new System.Random(seed ^ StableHash(room.StableId));
            return new[] { entry, Take(encounters, random), Take(encounters, random), exit };
        }

        private static RoomChunk Take(List<RoomChunk> pool, System.Random random)
        {
            int index = random.Next(pool.Count);
            var result = pool[index];
            pool.RemoveAt(index);
            return result;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = (int)2166136261u;
                foreach (char c in value) hash = (hash ^ c) * 16777619;
                return hash;
            }
        }

        private static Transform Marker(Transform parent, string name, float x, float y)
        {
            var point = new GameObject(name).transform;
            point.SetParent(parent);
            point.localPosition = new Vector3(x, y);
            return point;
        }

        private static BoxCollider2D Area(Transform parent, string name, float x, float y, float width, float height)
        {
            var collider = Marker(parent, name, x, y).gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(width, height);
            return collider;
        }

        private static void AddStreetLight(Transform parent, bool canal)
        {
            var lamp = new GameObject("Rain lamp", typeof(Light2D), typeof(WorldLightFlicker));
            lamp.transform.SetParent(parent, false);
            lamp.transform.localPosition = new Vector3(0f, 4.2f, 0f);
            var light = lamp.GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = canal ? new Color(.25f, .7f, 1f) : new Color(1f, .56f, .24f);
            light.intensity = .7f;
            light.pointLightOuterRadius = 5.5f;
            light.pointLightInnerRadius = 1.3f;
        }
    }
}
