using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace NineLives.Editor
{
    public static class StageFiveRunSetup
    {
        private const string Scene = "Assets/_Game/Scenes/Run.unity";

        [MenuItem("NineLives/Stage 5/Check Run Route")]
        public static void Check()
        {
            var rooms = AssetDatabase.FindAssets("t:RoomDefinition", new[] { "Assets/_Game/Rooms/Definitions" })
                .Select(g => AssetDatabase.LoadAssetAtPath<RoomDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            var chunks = StageFiveChunkSetup.LoadLibrary();
            for (int seed = 0; seed < 100; seed++)
            {
                var normal = RunRoute.Build(seed, 0, rooms, chunks);
                var hard = RunRoute.Build(seed, 1, rooms, chunks);
                var again = RunRoute.Build(seed, 0, rooms.Reverse().ToArray(), chunks.Reverse().ToArray());
                if (normal.Length != 9 || hard.Length != 9 ||
                    normal[5].Room.Biome == hard[5].Room.Biome ||
                    normal[6].Room.StableId != hard[6].Room.StableId ||
                    normal.Where(s => s.Room.Kind == RoomKind.Combat).Select(s => s.LayoutId).Distinct().Count() != 5 ||
                    hard.Where(s => s.Room.Kind == RoomKind.Combat).Select(s => s.LayoutId).Distinct().Count() != 5 ||
                    !normal.Select(s => s.LayoutId + ":" + s.ChunkSeed)
                        .SequenceEqual(again.Select(s => s.LayoutId + ":" + s.ChunkSeed)))
                    throw new InvalidOperationException("Invalid route for seed " + seed);
            }
            Debug.Log("Stage 5 route PASS: 100 seeds, both branches, five unique combat layouts, stable order.");
        }

        [MenuItem("NineLives/Stage 5/Create Run Scene")]
        public static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene) != null) return;
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Tests/RoomLab.unity");
            var lab = UnityEngine.Object.FindAnyObjectByType<RoomLab>();
            var old = new SerializedObject(lab);
            var host = lab.gameObject;
            var rooms = Enumerable.Range(0, old.FindProperty("rooms").arraySize)
                .Select(i => old.FindProperty("rooms").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            var chunks = Enumerable.Range(0, old.FindProperty("chunks").arraySize)
                .Select(i => old.FindProperty("chunks").GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            var player = old.FindProperty("player").objectReferenceValue;
            var camera = old.FindProperty("roomCamera").objectReferenceValue;
            var rat = old.FindProperty("ratPrefab").objectReferenceValue;
            var status = (Text)old.FindProperty("status").objectReferenceValue;
            status.rectTransform.sizeDelta = new Vector2(900, 75);
            status.fontSize = 19;
            status.alignment = TextAnchor.UpperCenter;
            UnityEngine.Object.DestroyImmediate(lab);
            host.name = "Run Director";
            var director = host.AddComponent<RunDirector>();
            var data = new SerializedObject(director);
            Assign(data.FindProperty("rooms"), rooms);
            Assign(data.FindProperty("chunks"), chunks);
            data.FindProperty("player").objectReferenceValue = player;
            data.FindProperty("roomCamera").objectReferenceValue = camera;
            data.FindProperty("ratPrefab").objectReferenceValue = rat;
            data.FindProperty("status").objectReferenceValue = status;
            data.FindProperty("exitSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Prototype/Square.png");
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            data.FindProperty("trialButton").objectReferenceValue = Button(canvas.transform, status.font, "Почати випробування", 0);
            data.FindProperty("normalButton").objectReferenceValue = Button(canvas.transform, status.font, "Звичайний бій · 20 корму", -240);
            data.FindProperty("hardButton").objectReferenceValue = Button(canvas.transform, status.font, "Складніший бій · 30 корму", 240);
            data.ApplyModifiedPropertiesWithoutUndo();
            WirePause();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), Scene, true);
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.Add(new EditorBuildSettingsScene(Scene, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Stage 5: playable Run scene created.");
        }

        [MenuItem("NineLives/Stage 5/Wire Run Pause")]
        public static void WirePause()
        {
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            var panel = canvas.transform.Find("Pause Panel").gameObject;
            var pause = canvas.GetComponent<PauseMenu>();
            if (pause == null) pause = canvas.gameObject.AddComponent<PauseMenu>();
            var buttons = panel.GetComponentsInChildren<Button>(true);
            var resume = buttons.Single(b => b.name == "Продовжити");
            var restart = buttons.Single(b => b.name == "На початок кімнати");
            var menu = buttons.Single(b => b.name == "Головне меню");
            var data = new SerializedObject(pause);
            data.FindProperty("player").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            data.FindProperty("panel").objectReferenceValue = panel;
            data.FindProperty("resumeButton").objectReferenceValue = resume;
            data.FindProperty("restartButton").objectReferenceValue = restart;
            data.FindProperty("menuButton").objectReferenceValue = menu;
            data.FindProperty("title").objectReferenceValue = panel.GetComponentsInChildren<Text>(true).Single(t => t.name == "Pause Title");
            data.ApplyModifiedPropertiesWithoutUndo();
            Connect(resume, pause.Resume);
            Connect(restart, pause.Restart);
            Connect(menu, pause.MainMenu);
        }

        private static void Connect(Button button, UnityEngine.Events.UnityAction action)
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static void Assign(SerializedProperty property, UnityEngine.Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static Button Button(Transform canvas, Font font, string label, float x)
        {
            var root = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            root.transform.SetParent(canvas, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(x, x == 0 ? 155 : 95);
            rect.sizeDelta = new Vector2(x == 0 ? 300 : 420, 45);
            root.GetComponent<Image>().color = new Color(.08f, .13f, .2f, .85f);
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(root.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 15;
            text.color = new Color(.93f, .91f, .79f);
            return root.GetComponent<Button>();
        }
    }
}
