using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NineLives.Editor
{
    public static class CombatHudSetup
    {
        private static readonly Color Gold = new Color(.74f, .62f, .41f);
        private static readonly Color Ink = new Color(.035f, .039f, .06f, .91f);
        private static readonly Color Ivory = new Color(.97f, .91f, .73f);
        private const string Art = "Assets/_Game/Art/Pixel/";

        [MenuItem("NineLives/UI/Apply concept HUD")]
        public static void Apply()
        {
            PixelArtSetup.Bake("HudIconsSource.png", "HudIcons128.png", 128, 128);
            PixelArtSetup.Import("HudIcons128.png", 128, 100, false, false);
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "HudIcons128.png");
            foreach (string path in new[] { "Assets/_Game/Scenes/Run.unity", "Assets/_Game/Scenes/Tests/RoomLab.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var hud = Object.FindAnyObjectByType<CombatHud>();
                if (hud == null) throw new System.InvalidOperationException(path + " has no CombatHud.");
                if (hud.transform.Find("Concept HUD") == null) Build(hud, atlas);
                Tune(hud);
                Wire(hud);
                EditorSceneManager.SaveScene(scene);
            }
            Check();
        }

        [MenuItem("NineLives/UI/Check concept HUD")]
        public static void Check()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "HudIcons128.png");
            if (atlas == null || atlas.width != 128 || atlas.height != 128 ||
                atlas.filterMode != FilterMode.Point)
                throw new System.InvalidOperationException("HUD icon atlas is missing or not pixel sharp.");
            var hud = Object.FindAnyObjectByType<CombatHud>();
            if (hud == null || hud.transform.Find("Concept HUD/Health Panel") == null ||
                hud.transform.Find("Concept HUD/Food Panel") == null ||
                hud.transform.Find("Concept HUD/Route Panel") == null)
                throw new System.InvalidOperationException("Concept HUD hierarchy is incomplete.");
            Debug.Log("Concept HUD PASS.");
        }

        private static void Build(CombatHud hud, Texture2D atlas)
        {
            var old = hud.transform.Find("Combat HP");
            var font = old.GetComponent<UnityEngine.UI.Text>().font;
            old.gameObject.SetActive(false);
            var title = hud.transform.Find("Title").GetComponent<RectTransform>();
            title.anchorMin = title.anchorMax = new Vector2(.5f, 0f);
            title.pivot = new Vector2(.5f, 0f);
            title.anchoredPosition = new Vector2(0f, 92f);
            title.sizeDelta = new Vector2(900f, 56f);
            title.GetComponent<UnityEngine.UI.Text>().fontSize = 18;

            var root = Rect(hud.transform, "Concept HUD", Vector2.zero, Vector2.zero,
                Vector2.zero, Vector2.zero);
            root.anchorMax = Vector2.one;
            root.SetAsFirstSibling();

            var health = Panel(root, "Health Panel", new Vector2(0f, 1f),
                new Vector2(20f, -18f), new Vector2(570f, 78f));
            Raw(health, "Cat Portrait", atlas, new Rect(0f, .5f, .5f, .5f),
                new Vector2(8f, -7f), new Vector2(64f, 64f));
            Label(health, "HP Label", "HP", font, 23, new Vector2(81f, -10f),
                new Vector2(44f, 25f));
            for (int i = 0; i < 10; i++)
                Raw(health, "Heart " + i, atlas, new Rect(.5f, .5f, .5f, .5f),
                    new Vector2(128f + i * 28f, -25f), new Vector2(24f, 24f));
            Label(health, "Health Value", "40/40", font, 28,
                new Vector2(423f, -22f), new Vector2(135f, 33f));

            var lives = Panel(root, "Lives Panel", new Vector2(0f, 1f),
                new Vector2(604f, -18f), new Vector2(122f, 78f));
            Raw(lives, "Life Icon", atlas, new Rect(.5f, 0f, .5f, .5f),
                new Vector2(8f, -12f), new Vector2(52f, 52f));
            Label(lives, "Lives Value", "× 9", font, 30,
                new Vector2(61f, -22f), new Vector2(58f, 32f));

            var food = Panel(root, "Food Panel", new Vector2(1f, 1f),
                new Vector2(-234f, -18f), new Vector2(160f, 78f));
            Raw(food, "Fish Icon", atlas, new Rect(0f, 0f, .5f, .5f),
                new Vector2(8f, -11f), new Vector2(54f, 54f));
            Label(food, "Food Value", "× 0", font, 30,
                new Vector2(66f, -22f), new Vector2(88f, 34f));

            var map = Panel(root, "Route Panel", new Vector2(1f, 1f),
                new Vector2(-18f, -18f), new Vector2(200f, 116f));
            var route = Rect(map, "Route Sketch", new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -14f), new Vector2(162f, 72f));
            Vector2[] points = {
                new Vector2(8, 9), new Vector2(27, 9), new Vector2(27, 27),
                new Vector2(51, 27), new Vector2(51, 48), new Vector2(79, 48),
                new Vector2(79, 30), new Vector2(113, 30), new Vector2(150, 53)
            };
            for (int i = 1; i < points.Length; i++)
                Line(route, "Route Link " + i, points[i - 1], points[i]);
            for (int i = 0; i < points.Length; i++)
            {
                var stop = Rect(route, "Route Stop " + i, Vector2.zero,
                    new Vector2(.5f, .5f), points[i], new Vector2(7f, 7f));
                var image = stop.gameObject.AddComponent<UnityEngine.UI.Image>();
                image.color = i == 0 ? Ivory : new Color(.47f, .46f, .53f);
                image.raycastTarget = false;
            }
            var marker = Raw(map.transform.Find("Route Sketch"), "Cat Marker", atlas,
                new Rect(.5f, 0f, .5f, .5f), points[0], new Vector2(23f, 23f),
                new Vector2(.5f, .5f), Vector2.zero);
            marker.transform.SetAsLastSibling();
            Label(map, "Route Value", "МАРШРУТ 1/9", font, 19,
                new Vector2(15f, -88f), new Vector2(176f, 24f));
            hud.GetComponent<Canvas>().pixelPerfect = true;
        }

        private static void Wire(CombatHud hud)
        {
            var root = hud.transform.Find("Concept HUD");
            var data = new SerializedObject(hud);
            data.FindProperty("healthLabel").objectReferenceValue =
                root.Find("Health Panel/Health Value").GetComponent<UnityEngine.UI.Text>();
            data.FindProperty("foodLabel").objectReferenceValue =
                root.Find("Food Panel/Food Value").GetComponent<UnityEngine.UI.Text>();
            data.FindProperty("livesLabel").objectReferenceValue =
                root.Find("Lives Panel/Lives Value").GetComponent<UnityEngine.UI.Text>();
            data.FindProperty("mapLabel").objectReferenceValue =
                root.Find("Route Panel/Route Value").GetComponent<UnityEngine.UI.Text>();
            var hearts = data.FindProperty("hearts");
            hearts.arraySize = 10;
            for (int i = 0; i < 10; i++)
                hearts.GetArrayElementAtIndex(i).objectReferenceValue =
                    root.Find("Health Panel/Heart " + i).GetComponent<UnityEngine.UI.RawImage>();
            var route = root.Find("Route Panel/Route Sketch");
            data.FindProperty("mapMarker").objectReferenceValue =
                route.Find("Cat Marker").GetComponent<RectTransform>();
            var stops = data.FindProperty("mapStops");
            stops.arraySize = 9;
            for (int i = 0; i < 9; i++)
                stops.GetArrayElementAtIndex(i).objectReferenceValue =
                    route.Find("Route Stop " + i).GetComponent<RectTransform>();
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Tune(CombatHud hud)
        {
            var root = hud.transform.Find("Concept HUD");
            root.Find("Route Panel").gameObject.SetActive(false);

            var health = root.Find("Health Panel");
            HideFrame(health);
            Place(health, new Vector2(-12f, 9f), new Vector2(480f, 110f));
            Place(health.Find("Cat Portrait"), new Vector2(0f, 0f), new Vector2(112f, 112f));
            health.Find("HP Label").gameObject.SetActive(false);
            for (int i = 0; i < 10; i++)
                Place(health.Find("Heart " + i),
                    new Vector2(116f + i % 5 * 32f, -10f - i / 5 * 42f),
                    new Vector2(42f, 42f));
            Place(health.Find("Health Value"), new Vector2(282f, -29f), new Vector2(160f, 50f));
            health.Find("Health Value").GetComponent<UnityEngine.UI.Text>().fontSize = 36;

            var lives = root.Find("Lives Panel");
            HideFrame(lives);
            Place(lives, new Vector2(104f, -98f), new Vector2(168f, 68f));
            Place(lives.Find("Life Icon"), new Vector2(0f, 0f), new Vector2(68f, 68f));
            Place(lives.Find("Lives Value"), new Vector2(70f, -7f), new Vector2(96f, 54f));
            lives.Find("Lives Value").GetComponent<UnityEngine.UI.Text>().fontSize = 36;

            var food = root.Find("Food Panel");
            HideFrame(food);
            var foodRect = food.GetComponent<RectTransform>();
            foodRect.anchorMin = foodRect.anchorMax = new Vector2(0f, 1f);
            foodRect.pivot = new Vector2(0f, 1f);
            Place(food, new Vector2(104f, -164f), new Vector2(168f, 68f));
            Place(food.Find("Fish Icon"), new Vector2(0f, 0f), new Vector2(68f, 68f));
            Place(food.Find("Food Value"), new Vector2(70f, -7f), new Vector2(96f, 54f));
            food.Find("Food Value").GetComponent<UnityEngine.UI.Text>().fontSize = 36;
        }

        private static void HideFrame(Transform panel)
        {
            panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
            foreach (string edge in new[] { "Top", "Bottom", "Left", "Right" })
                panel.Find(edge).gameObject.SetActive(false);
        }

        private static void Place(Transform target, Vector2 position, Vector2 size)
        {
            var rect = target.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 anchor,
            Vector2 position, Vector2 size)
        {
            var panel = Rect(parent, name, anchor,
                new Vector2(anchor.x, 1f), position, size);
            var image = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = Ink;
            image.raycastTarget = false;
            var top = new Vector2(0f, 1f);
            Border(panel, "Top", top, new Vector2(0f, 0f), new Vector2(size.x, 2f));
            Border(panel, "Bottom", top, new Vector2(0f, -size.y + 2f), new Vector2(size.x, 2f));
            Border(panel, "Left", top, new Vector2(0f, 0f), new Vector2(2f, size.y));
            Border(panel, "Right", top, new Vector2(size.x - 2f, 0f), new Vector2(2f, size.y));
            return panel;
        }

        private static void Border(Transform parent, string name, Vector2 anchor,
            Vector2 position, Vector2 size)
        {
            var rect = Rect(parent, name, anchor, new Vector2(0f, 1f), position, size);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = Gold;
            image.raycastTarget = false;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 anchor,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static UnityEngine.UI.RawImage Raw(Transform parent, string name, Texture2D atlas,
            Rect uv, Vector2 position, Vector2 size, Vector2? pivot = null, Vector2? anchor = null)
        {
            var rect = Rect(parent, name, anchor ?? new Vector2(0f, 1f),
                pivot ?? new Vector2(0f, 1f), position, size);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            image.texture = atlas;
            image.uvRect = uv;
            image.raycastTarget = false;
            return image;
        }

        private static UnityEngine.UI.Text Label(Transform parent, string name, string value,
            Font font, int size, Vector2 position, Vector2 dimensions)
        {
            var rect = Rect(parent, name, new Vector2(0f, 1f),
                new Vector2(0f, 1f), position, dimensions);
            var label = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
            label.font = font;
            label.fontSize = size;
            label.color = Ivory;
            label.text = value;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            return label;
        }

        private static void Line(Transform parent, string name, Vector2 start, Vector2 end)
        {
            var center = (start + end) * .5f;
            var line = Rect(parent, name, Vector2.zero, new Vector2(.5f, .5f),
                center, new Vector2(Vector2.Distance(start, end), 5f));
            line.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);
            var image = line.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(.53f, .53f, .6f, .78f);
            image.raycastTarget = false;
        }
    }
}
