using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace NineLives.Editor
{
    public static class StageFiveRunSetup
    {
        private static bool running;

        [MenuItem("NineLives/Campaign/Configure Build Profiles")]
        public static void ConfigureBuildProfiles()
        {
            var scenes = new[] { "Bootstrap", "MainMenu", "Run" }
                .Select(name => new EditorBuildSettingsScene("Assets/_Game/Scenes/" + name + ".unity", true)).ToArray();
            EditorBuildSettings.scenes = scenes;
            foreach (var name in new[] { "Web Development", "Windows Development" })
            {
                var profile = BuildProfile.GetAllBuildProfiles().Single(p => p.name == name);
                profile.scenes = scenes;
                profile.overrideGlobalScenes = true;
                EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("NineLives/Campaign/Check Fixed Route")]
        public static void Check()
        {
            var definitions = AssetDatabase.FindAssets("t:RoomDefinition", new[] { "Assets/_Game/Rooms/Definitions" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<RoomDefinition>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
            var route = RunRoute.Build(definitions);
            var expected = new[] { "street_entrance", "street_crosswalk", "street_roofs", "canal_steps", "canal_boss" };
            Require(route.Select(room => room.StableId).SequenceEqual(expected), "wrong campaign order");
            Require(route.Length == RunRoute.RoomCount && route.Select(room => room.Prefab).Distinct().Count() == route.Length,
                "campaign needs distinct authored rooms");
            Require(route.SequenceEqual(RunRoute.Build(definitions.Reverse().ToArray())), "asset order changed the campaign");
            bool rejected = false;
            try { RunRoute.Build(definitions.Where(room => room.StableId != expected[0]).ToArray()); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected, "missing room was accepted");
            Debug.Log("Campaign route PASS: five authored rooms, fixed order and missing-room validation.");
        }

        [MenuItem("NineLives/Campaign/Check Complete Loop (Play Mode)")]
        private static async void CheckCompleteLoop()
        {
            if (running || !EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "MainMenu")
            {
                Debug.LogError("Start Bootstrap in Play Mode, then run the campaign check from MainMenu.");
                return;
            }
            running = true;
            var background = InputSystem.settings.backgroundBehavior;
            var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                UnityEngine.Object.FindAnyObjectByType<MainMenu>().Play();
                await Wait(() => SceneManager.GetActiveScene().name == "Run");
                var cycle = CycleController.Instance;
                var pause = UnityEngine.Object.FindAnyObjectByType<PauseMenu>();
                pause.SetPaused(false);
                var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                for (int i = 0; i < RunRoute.RoomCount; i++)
                {
                    Require(cycle.Run.Position == i, "room skipped or repeated");
                    pause.SetPaused(false);
                    var room = UnityEngine.Object.FindAnyObjectByType<RoomLayout>();
                    player.SetSpawn(room.Exit.position);
                    if (i > 0)
                    {
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                        await Task.Delay(120);
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        Require(cycle.Run.Position == i && cycle.Run.IsActive, "exit opened with living enemies");
                        foreach (var rat in room.GetComponentsInChildren<RatController>())
                            rat.GetComponent<Health>().TakeDamage(999, Vector2.zero);
                        await Wait(() => room.Exit.GetComponentInChildren<SpriteRenderer>().color.g > .8f);
                    }
                    await Task.Delay(80);
                    await PressExit(keyboard, () => i == RunRoute.RoomCount - 1 ? cycle.Cycle.IsVictory : cycle.Run.Position == i + 1);
                    await Task.Delay(80);
                }
                Require(pause.ShowingResult && pause.IsPaused && !cycle.Run.IsActive, "victory did not stop gameplay");
                Require(!cycle.CompleteRun(RunEndReason.Victory), "victory was issued twice");
                pause.MainMenu();
                await Wait(() => SceneManager.GetActiveScene().name == "MainMenu");
                UnityEngine.Object.FindAnyObjectByType<MainMenu>().Play();
                await Wait(() => SceneManager.GetActiveScene().name == "Run");
                Require(cycle.Run.Position == 0 && cycle.Run.IsActive && !cycle.Cycle.IsComplete, "replay did not start at the entrance");
                cycle.CompleteRun(RunEndReason.Victory);
                UnityEngine.Object.FindAnyObjectByType<PauseMenu>().MainMenu();
                await Wait(() => SceneManager.GetActiveScene().name == "MainMenu");
                Debug.Log("Campaign loop PASS: menu, five rooms, combat exits, victory once, menu and replay. Enemies were defeated by the test.");
            }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
                running = false;
            }
        }

        private static async Task Wait(Func<bool> condition)
        {
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            while (!condition())
            {
                if (!EditorApplication.isPlaying || timeout.Elapsed.TotalSeconds > 5)
                    throw new InvalidOperationException("Campaign transition timed out.");
                ResumeTestSession();
                await Task.Delay(10);
            }
        }

        private static async Task PressExit(Keyboard keyboard, Func<bool> condition)
        {
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            while (!condition())
            {
                if (!EditorApplication.isPlaying || timeout.Elapsed.TotalSeconds > 5)
                    throw new InvalidOperationException("Campaign exit input timed out.");
                ResumeTestSession();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                await Task.Delay(80);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                await Task.Delay(80);
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        }

        private static void ResumeTestSession()
        {
            var pause = UnityEngine.Object.FindAnyObjectByType<PauseMenu>();
            if (pause != null && pause.IsPaused && !pause.ShowingResult) pause.SetPaused(false);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Campaign: " + message);
        }
    }
}
