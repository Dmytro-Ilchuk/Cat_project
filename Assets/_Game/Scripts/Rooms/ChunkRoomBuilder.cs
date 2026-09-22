using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NineLives
{
    public static class ChunkRoomBuilder
    {
        public static RoomLayout Build(RoomDefinition room, int seed, RoomChunk[] library)
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
            var pieces = new[] { entry, Take(encounters, random), Take(encounters, random), exit };
            int width = pieces.Sum(c => c.Width);
            var root = new GameObject(room.StableId + " chunks " + seed);
            var layout = root.AddComponent<RoomLayout>();
            var enemyPoints = new List<Transform>();
            float left = -width / 2f;
            foreach (var piece in pieces)
            {
                var instance = UnityEngine.Object.Instantiate(piece, root.transform);
                instance.name = piece.StableId;
                instance.transform.localPosition = new Vector3(left + piece.Width / 2f, 0);
                if (Mathf.Abs(instance.LeftSeam.position.x - left) > .001f ||
                    Mathf.Abs(instance.LeftSeam.position.y) > .001f ||
                    Mathf.Abs(instance.RightSeam.position.x - left - piece.Width) > .001f ||
                    Mathf.Abs(instance.RightSeam.position.y) > .001f)
                    throw new InvalidOperationException(piece.StableId + " has an incompatible seam.");
                enemyPoints.AddRange(instance.EnemySpawns);
                left += piece.Width;
            }
            var entrance = Marker(root.transform, "Entrance", -width / 2f + 2, 1);
            var spawn = Marker(root.transform, "Safe spawn", -width / 2f + 4, 1);
            var departure = Marker(root.transform, "Exit", width / 2f - 2, 1);
            var camera = Area(root.transform, "Camera bounds", 0, 4, width - 2, 12);
            var arena = Area(root.transform, "Arena bounds", 0, 3.5f, width - 3, 9);
            layout.Initialize(entrance, departure, spawn, enemyPoints.ToArray(), camera, arena);
            return layout;
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
                int hash = 216613626;
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
    }
}
