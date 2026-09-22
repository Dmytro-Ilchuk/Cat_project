using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NineLives.Editor
{
    public static class StageFiveChunkSetup
    {
        private const string Folder = "Assets/_Game/Rooms/Chunks";
        private static readonly string[] Names = { "entry", "crate", "awning", "steps", "exit" };

        [MenuItem("NineLives/Stage 5/Create Prototype Chunks")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Game/Rooms", "Chunks");
            var square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Prototype/Square.png");
            if (square == null) throw new InvalidOperationException("Prototype square is missing.");
            foreach (RoomBiome biome in Enum.GetValues(typeof(RoomBiome)))
                foreach (var name in Names)
                {
                    string id = (biome == RoomBiome.CityStreet ? "street_" : "canal_") + name;
                    string path = Folder + "/" + id + ".prefab";
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) continue;
                    var root = Build(id, biome, name, square);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    UnityEngine.Object.DestroyImmediate(root);
                }
            AssetDatabase.SaveAssets();
            Debug.Log("Stage 5: ten prototype chunks are ready.");
        }

        [MenuItem("NineLives/Stage 5/Check Chunks")]
        public static void Check()
        {
            var library = LoadLibrary();
            if (library.Length != 10 || library.Select(c => c.StableId).Distinct().Count() != library.Length)
                throw new InvalidOperationException("Expected ten unique chunk IDs.");
            foreach (RoomBiome biome in Enum.GetValues(typeof(RoomBiome)))
            {
                var group = library.Where(c => c.Biome == biome).ToArray();
                if (group.Count(c => c.Role == ChunkRole.Entry) != 1 ||
                    group.Count(c => c.Role == ChunkRole.Exit) != 1 ||
                    group.Count(c => c.Role == ChunkRole.Encounter) != 3)
                    throw new InvalidOperationException(biome + ": invalid chunk roles.");
                var room = AssetDatabase.LoadAssetAtPath<RoomDefinition>(
                    "Assets/_Game/Rooms/Definitions/" + (biome == RoomBiome.CityStreet ? "street_crosswalk" : "canal_steps") + ".asset");
                var variants = new HashSet<string>();
                for (int seed = 0; seed < 32; seed++)
                {
                    var first = ChunkRoomBuilder.Build(room, seed, library);
                    var second = ChunkRoomBuilder.Build(room, seed, library);
                    try
                    {
                        string sequence = string.Join("/", first.GetComponentsInChildren<RoomChunk>().Select(c => c.StableId));
                        string repeated = string.Join("/", second.GetComponentsInChildren<RoomChunk>().Select(c => c.StableId));
                        if (sequence != repeated) throw new InvalidOperationException("Seed is not repeatable: " + seed);
                        variants.Add(sequence);
                        var pieces = first.GetComponentsInChildren<RoomChunk>();
                        if (pieces.Length != 4 || pieces.Select(c => c.StableId).Distinct().Count() != 4 ||
                            first.EnemySpawns.Length != 2 ||
                            Vector2.Distance(first.SafeSpawn.position, first.EnemySpawns[0].position) < 6f)
                            throw new InvalidOperationException("Invalid assembled room: " + sequence);
                        Physics2D.SyncTransforms();
                        var floors = first.GetComponentsInChildren<BoxCollider2D>()
                            .Where(c => c.gameObject.layer == LayerMask.NameToLayer("Ground") &&
                                Mathf.Abs(c.bounds.max.y) < .1f)
                            .Select(c => c.bounds).OrderBy(b => b.min.x).ToArray();
                        if (floors.Length == 0 || floors[0].min.x > first.SafeSpawn.position.x ||
                            floors[floors.Length - 1].max.x < first.Exit.position.x)
                            throw new InvalidOperationException("Missing main floor: " + sequence);
                        for (int i = 1; i < floors.Length; i++)
                            if (floors[i].min.x - floors[i - 1].max.x > 4f)
                                throw new InvalidOperationException("Unsafe gap: " + sequence);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(first.gameObject);
                        UnityEngine.Object.DestroyImmediate(second.gameObject);
                    }
                }
                if (variants.Count < 3) throw new InvalidOperationException(biome + ": insufficient variation.");
            }
            Debug.Log("Stage 5 chunks PASS: 64 seeded rooms, stable choices, unique pieces and safe floor gaps.");
        }

        public static RoomChunk[] LoadLibrary() => AssetDatabase.FindAssets("t:Prefab", new[] { Folder })
            .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(g => g != null && g.TryGetComponent<RoomChunk>(out _))
            .Select(g => g.GetComponent<RoomChunk>()).OrderBy(c => c.StableId, StringComparer.Ordinal).ToArray();

        private static GameObject Build(string id, RoomBiome biome, string name, Sprite square)
        {
            bool canal = biome == RoomBiome.Canals;
            var root = new GameObject(id);
            var chunk = root.AddComponent<RoomChunk>();
            var left = Marker(root.transform, "Left seam", -5, 0);
            var right = Marker(root.transform, "Right seam", 5, 0);
            Color ground = canal ? new Color(.18f, .24f, .32f) : new Color(.27f, .29f, .34f);
            Color detail = canal ? new Color(.3f, .46f, .52f) : new Color(.42f, .37f, .39f);
            Block(root.transform, square, "Backdrop", 0, 4, 10, 12,
                canal ? new Color(.07f, .13f, .23f) : new Color(.11f, .13f, .23f), false, -10);
            if (canal)
            {
                Block(root.transform, square, "Water", 0, -1.7f, 10, 1, new Color(.1f, .3f, .43f), false, -4);
                Block(root.transform, square, "Pipe", 0, 5, 7, .3f, detail, false, -3);
            }
            else Block(root.transform, square, "House silhouette", 0, 6, 7, 7,
                new Color(.16f, .19f, .29f), false, -4);

            if (canal && name == "crate")
            {
                Block(root.transform, square, "Quay left", -3.25f, -.5f, 3.5f, 1, ground, true);
                Block(root.transform, square, "Quay right", 3.25f, -.5f, 3.5f, 1, ground, true);
            }
            else if (canal && name == "awning")
            {
                Block(root.transform, square, "Quay left", -3.5f, -.5f, 3, 1, ground, true);
                Block(root.transform, square, "Quay right", 3.5f, -.5f, 3, 1, ground, true);
                Block(root.transform, square, "Bridge", 0, 1.7f, 4, .35f, detail, true);
            }
            else Block(root.transform, square, canal ? "Quay" : "Road", 0, -.5f, 10, 1, ground, true);

            if (name == "crate" && !canal) Block(root.transform, square, "Crate", 0, .65f, 1.3f, 1.3f, detail, true);
            if (name == "awning" && !canal) Block(root.transform, square, "Awning", 1, 1.8f, 5, .35f, detail, true);
            if (name == "steps")
            {
                Block(root.transform, square, "Low ledge", -2, 1.4f, 4, .35f, detail, true);
                Block(root.transform, square, "High ledge", 2.5f, 2.6f, 4, .35f, detail, true);
            }
            var enemy = name == "entry" || name == "exit" ? Array.Empty<Transform>()
                : new[] { Marker(root.transform, "Enemy point", canal && name != "steps" ? -3.5f : -3, 1) };
            var data = new SerializedObject(chunk);
            data.FindProperty("stableId").stringValue = id;
            data.FindProperty("biome").enumValueIndex = (int)biome;
            data.FindProperty("role").enumValueIndex = name == "entry" ? (int)ChunkRole.Entry
                : name == "exit" ? (int)ChunkRole.Exit : (int)ChunkRole.Encounter;
            data.FindProperty("width").intValue = 10;
            data.FindProperty("leftSeam").objectReferenceValue = left;
            data.FindProperty("rightSeam").objectReferenceValue = right;
            var points = data.FindProperty("enemySpawns");
            points.arraySize = enemy.Length;
            for (int i = 0; i < enemy.Length; i++) points.GetArrayElementAtIndex(i).objectReferenceValue = enemy[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static Transform Marker(Transform parent, string name, float x, float y)
        {
            var point = new GameObject(name).transform;
            point.SetParent(parent);
            point.localPosition = new Vector3(x, y);
            return point;
        }

        private static void Block(Transform parent, Sprite sprite, string name, float x, float y,
            float width, float height, Color color, bool solid, int order = 0)
        {
            var block = new GameObject(name);
            block.transform.SetParent(parent);
            block.transform.localPosition = new Vector3(x, y);
            block.transform.localScale = new Vector3(width, height, 1);
            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (solid)
            {
                block.layer = LayerMask.NameToLayer("Ground");
                block.AddComponent<BoxCollider2D>();
            }
        }
    }
}
