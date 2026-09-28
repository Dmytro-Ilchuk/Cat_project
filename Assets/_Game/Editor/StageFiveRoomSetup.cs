using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NineLives.Editor
{
    public static class StageFiveRoomSetup
    {
        private const string Root = "Assets/_Game/Rooms";
        private const string Prefabs = Root + "/Prefabs";
        private const string Definitions = Root + "/Definitions";

        private struct Spec
        {
            public string id;
            public RoomKind kind;
            public RoomBiome biome;
            public int difficulty;
            public Spec(string id, RoomKind kind, RoomBiome biome, int difficulty)
            { this.id = id; this.kind = kind; this.biome = biome; this.difficulty = difficulty; }
        }

        private static readonly Spec[] Rooms =
        {
            new Spec("street_entrance", RoomKind.Entrance, RoomBiome.CityStreet, 0),
            new Spec("street_crosswalk", RoomKind.Combat, RoomBiome.CityStreet, 1),
            new Spec("street_roofs", RoomKind.Combat, RoomBiome.CityStreet, 2),
            new Spec("canal_steps", RoomKind.Combat, RoomBiome.Canals, 2),
            new Spec("canal_trial", RoomKind.Trial, RoomBiome.Canals, 3),
            new Spec("street_merchant", RoomKind.Merchant, RoomBiome.CityStreet, 0),
            new Spec("canal_boss", RoomKind.Boss, RoomBiome.Canals, 5),
        };

        [MenuItem("NineLives/Stage 5/Create Prototype Rooms")]
        public static void Create()
        {
            EnsureFolders();
            var square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Prototype/Square.png");
            if (square == null) throw new InvalidOperationException("Prototype square is missing.");
            foreach (var spec in Rooms)
            {
                string prefabPath = Prefabs + "/" + spec.id + ".prefab";
                string definitionPath = Definitions + "/" + spec.id + ".asset";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                {
                    var root = Build(spec, square);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    UnityEngine.Object.DestroyImmediate(root);
                }
                if (AssetDatabase.LoadAssetAtPath<RoomDefinition>(definitionPath) == null)
                {
                    var definition = ScriptableObject.CreateInstance<RoomDefinition>();
                    var data = new SerializedObject(definition);
                    data.FindProperty("stableId").stringValue = spec.id;
                    data.FindProperty("kind").enumValueIndex = (int)spec.kind;
                    data.FindProperty("biome").enumValueIndex = (int)spec.biome;
                    data.FindProperty("difficulty").intValue = spec.difficulty;
                    data.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponent<RoomLayout>();
                    data.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.CreateAsset(definition, definitionPath);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Stage 5: seven prototype room prefabs and definitions are ready.");
        }

        [MenuItem("NineLives/Stage 5/Check Prototype Rooms")]
        public static void Check()
        {
            var definitions = AssetDatabase.FindAssets("t:RoomDefinition", new[] { Definitions })
                .Select(g => AssetDatabase.LoadAssetAtPath<RoomDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            if (Rooms.Any(spec => definitions.Count(r => r.StableId == spec.id) != 1) ||
                definitions.Select(r => r.StableId).Distinct().Count() != definitions.Length)
                throw new InvalidOperationException("Room IDs or room count are invalid.");
            foreach (var room in definitions)
            {
                if (room.Prefab == null) throw new InvalidOperationException(room.StableId + ": missing prefab.");
                var layout = UnityEngine.Object.Instantiate(room.Prefab);
                try
                {
                    if (layout.Entrance == null || layout.Exit == null || layout.SafeSpawn == null)
                        throw new InvalidOperationException(room.StableId + ": missing entrance, exit or spawn.");
                    if (layout.CameraBounds.size.x < 20 || layout.ArenaBounds.size.x < 20)
                        throw new InvalidOperationException(room.StableId + ": invalid bounds.");
                    if (!layout.ArenaBounds.Contains(layout.SafeSpawn.position))
                        throw new InvalidOperationException(room.StableId + ": spawn outside arena.");
                    if (room.Kind == RoomKind.Combat && layout.EnemySpawns.Length < 2)
                        throw new InvalidOperationException(room.StableId + ": insufficient enemy points.");
                    foreach (var spawn in layout.EnemySpawns)
                        if (spawn == null || !layout.ArenaBounds.Contains(spawn.position) ||
                            Vector2.Distance(spawn.position, layout.SafeSpawn.position) < 6f)
                            throw new InvalidOperationException(room.StableId + ": unsafe enemy point.");
                    Physics2D.SyncTransforms();
                    var floors = layout.GetComponentsInChildren<BoxCollider2D>()
                        .Where(c => c.gameObject.layer == LayerMask.NameToLayer("Ground") &&
                            Mathf.Abs(c.bounds.max.y) < .1f)
                        .Select(c => c.bounds).OrderBy(b => b.min.x).ToArray();
                    if (floors.Length == 0 || floors[0].min.x > layout.SafeSpawn.position.x ||
                        floors[floors.Length - 1].max.x < layout.Exit.position.x)
                        throw new InvalidOperationException(room.StableId + ": floor does not connect anchors.");
                    for (int i = 1; i < floors.Length; i++)
                        if (floors[i].min.x - floors[i - 1].max.x > 4f)
                            throw new InvalidOperationException(room.StableId + ": mandatory gap exceeds safe jump budget.");
                }
                finally { UnityEngine.Object.DestroyImmediate(layout.gameObject); }
            }
            Debug.Log("Stage 5 rooms PASS: unique IDs, seven prefabs, anchors and safe enemy points.");
        }

        [MenuItem("NineLives/Stage 5/Check Room Lab (Play Mode)")]
        public static void CheckLab()
        {
            if (!EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "RoomLab")
                throw new InvalidOperationException("Enter Play Mode in RoomLab first.");
            var lab = UnityEngine.Object.FindAnyObjectByType<RoomLab>();
            var load = typeof(RoomLab).GetMethod("Load", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var data = new SerializedObject(lab).FindProperty("rooms");
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var foreground = UnityEngine.Object.FindAnyObjectByType<RoomParallax>()
                .GetComponentsInChildren<SpriteRenderer>().Where(r => r.name.StartsWith("Lower foreground parallax")).ToArray();
            if (foreground.Length != 9 || foreground.Any(r => r.sortingOrder <= 10 || r.GetComponent<Collider2D>() != null))
                throw new InvalidOperationException("Room Lab: lower foreground parallax is missing or interactive.");
            for (int i = 0; i < data.arraySize; i++)
            {
                load.Invoke(lab, new object[] { i });
                var layouts = UnityEngine.Object.FindObjectsByType<RoomLayout>(FindObjectsSortMode.None);
                if (layouts.Length != 1 || Vector2.Distance(player.GetComponent<Rigidbody2D>().position, layouts[0].SafeSpawn.position) > .01f)
                    throw new InvalidOperationException("Room Lab: transition or player spawn failed at index " + i);
                int fog = layouts[0].GetComponentsInChildren<SpriteRenderer>().Count(renderer =>
                    renderer.name == "Distant drifting fog" || renderer.name == "Thin foreground mist");
                if (fog != 4)
                    throw new InvalidOperationException("Room Lab: fog is not attached to the active room at index " + i);
                foreach (var barrier in layouts[0].GetComponentsInChildren<Transform>().Where(t => t.name == "Street prop 2"))
                    if (barrier.GetComponent<Collider2D>() != null || barrier.localScale.x < .4f)
                        throw new InvalidOperationException("Room Lab: concrete barrier must be enlarged background art.");
                var shadow = layouts[0].GetComponentsInChildren<Transform>();
                if (shadow.Any(t => t.name == "Shadow street facade") ||
                    !shadow.Any(t => t.name.StartsWith("Dark city utility prop")) ||
                    shadow.Any(t => t.name.StartsWith("Dark city utility prop") && t.GetComponent<Collider2D>() != null))
                    throw new InvalidOperationException("Room Lab: old facade or colliding utility prop.");
                var debris = layouts[0].GetComponentsInChildren<WindblownDebris>();
                if (debris.Length == 0 || debris.Any(item => item.GetComponent<Collider2D>() != null))
                    throw new InvalidOperationException("Room Lab: missing or colliding wind debris.");
                if (i == 0)
                {
                    var sample = debris[0];
                    Vector3 start = sample.transform.position;
                    sample.Initialize(start.x - 1f, start.x + 1f, 7);
                    var step = typeof(WindblownDebris).GetMethod("Step",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    float travel = 0f;
                    for (int frame = 0; frame < 180; frame++)
                    {
                        step.Invoke(sample, new object[] { .02f });
                        Vector3 position = sample.transform.position;
                        travel = Mathf.Max(travel, Mathf.Abs(position.x - start.x));
                        if (position.x < start.x - 1.001f || position.x > start.x + 1.001f ||
                            position.y < start.y - .001f)
                            throw new InvalidOperationException("Room Lab: wind debris escaped its road or floor.");
                    }
                    if (travel < .1f)
                        throw new InvalidOperationException("Room Lab: wind debris did not move in gusts.");
                }
                int poles = layouts[0].GetComponentsInChildren<Transform>().Count(t =>
                    t.name == "Grounded street lamp" || t.name == "Grounded neon lamp");
                var cables = layouts[0].GetComponentsInChildren<LineRenderer>()
                    .Where(line => line.name == "Overhead power cable").ToArray();
                if (poles >= 2 && cables.Length != (poles - 1) * 2 ||
                    cables.Any(line => line.GetComponent<Collider2D>() != null))
                    throw new InvalidOperationException("Room Lab: disconnected or colliding power lines at index " + i);
            }
            var enemies = typeof(RoomLab).GetField("enemies", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            enemies.SetValue(lab, true);
            load.Invoke(lab, new object[] { 1 });
            if (UnityEngine.Object.FindObjectsByType<RatController>(FindObjectsSortMode.None).Length != 2)
                throw new InvalidOperationException("Room Lab: enemy spawn failed.");
            var chunkMode = typeof(RoomLab).GetField("chunkMode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            chunkMode.SetValue(lab, true);
            load.Invoke(lab, new object[] { 1 });
            var assembled = UnityEngine.Object.FindAnyObjectByType<RoomLayout>();
            var rats = UnityEngine.Object.FindObjectsByType<RatController>(FindObjectsSortMode.None);
            Physics2D.SyncTransforms();
            if (assembled.GetComponentsInChildren<RoomChunk>().Length != 4 || rats.Length != 2 ||
                rats.Any(r => Mathf.Abs(r.GetComponent<Collider2D>().bounds.min.y) > .1f ||
                    r.gameObject.layer != LayerMask.NameToLayer("Enemy") ||
                    r.GetComponent<Rigidbody2D>().gravityScale <= 0 ||
                    (r.GetComponent<Rigidbody2D>().constraints & RigidbodyConstraints2D.FreezePositionY) != 0))
                throw new InvalidOperationException("Room Lab: chunk assembly or enemy ground placement failed.");
            enemies.SetValue(lab, false);
            chunkMode.SetValue(lab, false);
            load.Invoke(lab, new object[] { 0 });
            Debug.Log("Room Lab PASS: seven transitions, one active room, chunk assembly and grounded enemies.");
        }

        private static GameObject Build(Spec spec, Sprite square)
        {
            var root = new GameObject(spec.id);
            var layout = root.AddComponent<RoomLayout>();
            bool canal = spec.biome == RoomBiome.Canals;
            Color floor = canal ? new Color(.18f, .24f, .32f) : new Color(.27f, .29f, .34f);
            Color detail = canal ? new Color(.3f, .46f, .52f) : new Color(.42f, .37f, .39f);
            Block(root.transform, square, "Night backdrop", 0, 4, 40, 12, canal ? new Color(.07f, .13f, .23f) : new Color(.11f, .13f, .23f), false, -10);
            if (canal)
            {
                Block(root.transform, square, "Canal water", 0, -1.7f, 40, 1, new Color(.1f, .3f, .43f), false, -4);
                if (spec.id == "canal_steps")
                {
                    Block(root.transform, square, "Left quay", -13, -.5f, 12, 1, floor, true);
                    Block(root.transform, square, "Middle quay", 0, -.5f, 10, 1, floor, true);
                    Block(root.transform, square, "Right quay", 13, -.5f, 12, 1, floor, true);
                    Block(root.transform, square, "Bridge", 0, 2.2f, 5, .35f, detail, true);
                }
                else Block(root.transform, square, "Quay", 0, -.5f, 38, 1, floor, true);
                Block(root.transform, square, "Pipe", 5, 5.4f, 15, .35f, detail, false, -2);
            }
            else
            {
                Block(root.transform, square, "Road", 0, -.5f, 38, 1, floor, true);
                Block(root.transform, square, "House left", -12, 4.8f, 9, 8, new Color(.16f, .19f, .29f), false, -4);
                Block(root.transform, square, "House right", 12, 5.8f, 10, 10, new Color(.15f, .18f, .27f), false, -4);
                if (spec.id == "street_roofs")
                {
                    Block(root.transform, square, "Low awning", -3, 1.6f, 6, .35f, detail, true);
                    Block(root.transform, square, "High awning", 6, 2.9f, 7, .35f, detail, true);
                }
                else if (spec.id == "street_crosswalk")
                {
                    Block(root.transform, square, "Crate", 0, .65f, 1.3f, 1.3f, detail, true);
                    Block(root.transform, square, "Low ledge", 8, 1.8f, 5, .35f, detail, true);
                }
            }
            Block(root.transform, square, "Left boundary", -19.5f, 3.5f, 1, 9, floor, true);
            Block(root.transform, square, "Right boundary", 19.5f, 3.5f, 1, 9, floor, true);
            var entrance = Marker(root.transform, "Entrance", -18, 1);
            var exit = Marker(root.transform, "Exit", 18, 1);
            var safe = Marker(root.transform, "Safe spawn", -16, 1);
            var spawns = spec.kind == RoomKind.Combat || spec.kind == RoomKind.Trial || spec.kind == RoomKind.Boss
                ? new[] { Marker(root.transform, "Enemy point A", -1, 1), Marker(root.transform, "Enemy point B", 10, 1) }
                : Array.Empty<Transform>();
            var camera = Area(root.transform, "Camera bounds", new Vector2(0, 4), new Vector2(38, 12));
            var arena = Area(root.transform, "Arena bounds", new Vector2(0, 3.5f), new Vector2(37, 9));
            var data = new SerializedObject(layout);
            data.FindProperty("entrance").objectReferenceValue = entrance;
            data.FindProperty("exit").objectReferenceValue = exit;
            data.FindProperty("safeSpawn").objectReferenceValue = safe;
            var points = data.FindProperty("enemySpawns");
            points.arraySize = spawns.Length;
            for (int i = 0; i < spawns.Length; i++) points.GetArrayElementAtIndex(i).objectReferenceValue = spawns[i];
            data.FindProperty("cameraBounds").objectReferenceValue = camera;
            data.FindProperty("arenaBounds").objectReferenceValue = arena;
            data.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static Transform Marker(Transform parent, string name, float x, float y)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent);
            marker.localPosition = new Vector3(x, y);
            return marker;
        }

        private static BoxCollider2D Area(Transform parent, string name, Vector2 center, Vector2 size)
        {
            var area = Marker(parent, name, center.x, center.y).gameObject.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = size;
            return area;
        }

        private static void Block(Transform parent, Sprite sprite, string name, float x, float y, float width, float height, Color color, bool solid, int order = 0)
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

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets/_Game", "Rooms");
            if (!AssetDatabase.IsValidFolder(Prefabs)) AssetDatabase.CreateFolder(Root, "Prefabs");
            if (!AssetDatabase.IsValidFolder(Definitions)) AssetDatabase.CreateFolder(Root, "Definitions");
        }
    }
}
